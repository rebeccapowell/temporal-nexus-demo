---
description: Repository setup and AI agent ergonomics for this project.
applyTo: "{AGENTS.MD,.github/workflows/copilot-setup-steps.yml,.agents/**/*.md,.agents/**/*.json}"
---

# Agent setup rules

- Keep root guidance minimal and route details to scoped instruction files.
- Treat `.agents/skills` as the modern default skill location.
- Keep skill source links explicit and version-friendly.
- Use deterministic setup steps in `copilot-setup-steps.yml` for CLI/tool installation.
