# WebView2 Player IL Patcher

The upstream `Lively.Player.WebView2.exe` has its `cmd_reload` IPC handler commented out
due to a past "ConnectionAborted" bug. This patcher uses [dnlib](https://github.com/0xd4d/dnlib)
to inject `webView.ExecuteScriptAsync("location.reload()")` into the `cmd_reload` case of
the `ProcessMessage` state machine, enabling seamless in-process page reloading that preserves
sessions, cookies, and localStorage.

## Why IL Patching?

The WebView2 player project targets .NET Framework 4.7.2 and uses legacy-style MSBuild tooling
that cannot be built with `dotnet build` on modern .NET 9 SDK without Visual Studio's full
MSBuild toolchain. IL patching the existing binary is the pragmatic solution.

## Usage

```bash
# From the repo root
dotnet run --project tools/WebView2Patcher -- <input.exe> <output.exe>

# Example: patch the Store-installed player
dotnet run --project tools/WebView2Patcher -- \
  "plugins/webview2/Lively.Player.WebView2.exe" \
  "plugins/webview2/Lively.Player.WebView2.patched.exe"
```

## What It Does

1. Loads the binary with dnlib
2. Finds the `ProcessMessage` async state machine (`MoveNext` method)
3. Locates the message-type switch (second `switch` instruction)
4. Finds `case 0` (`cmd_reload`) which currently jumps to cleanup (no-op)
5. Inserts IL instructions equivalent to:
   ```csharp
   if (this.<>4__this.webView != null)
       this.<>4__this.webView.ExecuteScriptAsync("location.reload()");
   ```
6. Fixes all branch targets with `SimplifyBranches()`/`OptimizeBranches()`
7. Saves the patched binary
