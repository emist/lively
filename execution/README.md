# Execution Scripts (Layer 3)

Deterministic scripts that do the actual work. Rules:

- **No LLM logic here** — pure code, no probabilistic reasoning
- **Well-commented** — another developer should understand it without context
- **Testable** — every script has a corresponding test in `tests/`
- **Single-purpose** — one script, one job
- **Environment vars** — secrets loaded from `.env`, never hardcoded
