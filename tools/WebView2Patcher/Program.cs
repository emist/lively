using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Linq;

var targetPath = args[0];
var outputPath = args[1];

Console.WriteLine($"Loading: {targetPath}");

// Load with options to preserve as much as possible
var modCtx = ModuleDef.CreateModuleContext();
var opts = new ModuleCreationOptions(modCtx);
var mod = ModuleDefMD.Load(targetPath, opts);

var form1 = mod.Types.First(t => t.Name == "Form1");
var stateMachine = form1.NestedTypes.First(t => t.Name.Contains("ProcessMessage"));
var moveNext = stateMachine.Methods.First(m => m.Name == "MoveNext");
var body = moveNext.Body;
var instrs = body.Instructions;

// Fields we need
var thisField = stateMachine.Fields.First(f => f.Name.Contains("4__this"));
var webViewField = form1.Fields.First(f => f.Name == "webView");

// Find existing ExecuteScriptAsync reference - already used by TryPauseMedia/TryPlayMedia
// It's on CoreWebView2: Task<string> CoreWebView2.ExecuteScriptAsync(string script)
// But wait - those are called on webView (WebView2 control) not CoreWebView2.
// Let's find what's actually referenced
Console.WriteLine("Looking for ExecuteScriptAsync references...");
MemberRef executeScriptRef = null;
foreach (var mr in mod.GetMemberRefs())
{
    if (mr.Name == "ExecuteScriptAsync")
    {
        Console.WriteLine($"  Found: {mr.FullName} on {mr.DeclaringType.FullName}");
        executeScriptRef = mr;
    }
}

// Also find get_CoreWebView2
var getCoreWebView2 = (MemberRef)mod.GetMemberRefs().First(m => 
    m.Name == "get_CoreWebView2" && m.DeclaringType.Name == "WebView2");
Console.WriteLine($"get_CoreWebView2: {getCoreWebView2.FullName}");

// Find Switch #2
int switchCount = 0;
Instruction[] switchTargets = null;
for (int i = 0; i < instrs.Count; i++)
{
    if (instrs[i].OpCode == OpCodes.Switch)
    {
        switchCount++;
        if (switchCount == 2) { switchTargets = (Instruction[])instrs[i].Operand; break; }
    }
}

var endTarget = switchTargets[0]; // cmd_reload currently -> cleanup code
int insertIdx = instrs.IndexOf(endTarget);
Console.WriteLine($"cmd_reload target at index {insertIdx}");

// Strategy: Insert BEFORE endTarget:
//   ldarg.0                           // state machine this
//   ldfld <>4__this                   // Form1
//   ldfld webView                     // WebView2 control
//   brfalse.s endTarget               // null check -> skip if null
//   ldarg.0                           // state machine this
//   ldfld <>4__this                   // Form1
//   ldfld webView                     // WebView2 control
//   callvirt get_CoreWebView2         // CoreWebView2
//   ldstr "location.reload()"         // JS string
//   callvirt ExecuteScriptAsync       // execute it
//   pop                               // discard Task<string> result
//                                     // falls through to endTarget (cleanup)

// Create the instructions
var newInstrs = new Instruction[]
{
    OpCodes.Ldarg_0.ToInstruction(),
    OpCodes.Ldfld.ToInstruction(thisField),
    OpCodes.Ldfld.ToInstruction(webViewField),
    OpCodes.Brfalse.ToInstruction(endTarget),  // if webView == null, skip
    OpCodes.Ldarg_0.ToInstruction(),
    OpCodes.Ldfld.ToInstruction(thisField),
    OpCodes.Ldfld.ToInstruction(webViewField),
    new Instruction(OpCodes.Ldstr, "location.reload()"),
    OpCodes.Callvirt.ToInstruction(executeScriptRef),
    OpCodes.Pop.ToInstruction(),  // discard the Task<string>
};

// Insert all new instructions before endTarget
for (int i = 0; i < newInstrs.Length; i++)
    instrs.Insert(insertIdx + i, newInstrs[i]);

// Redirect switch case 0 to our first new instruction
switchTargets[0] = newInstrs[0];

// Update max stack (add some headroom)
body.MaxStack = (ushort)Math.Max((int)body.MaxStack, 8);

// Fix all branches: first convert all short branches to long form, then optimize back
body.SimplifyBranches();   // convert all br.s/brfalse.s to br/brfalse
body.OptimizeBranches();   // convert back to short form where possible

Console.WriteLine($"Inserted {newInstrs.Length} instructions at index {insertIdx}");
Console.WriteLine($"Saving to: {outputPath}");

// Save
mod.Write(outputPath);
Console.WriteLine("Done!");
