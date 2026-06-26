# temporal-nexus-demo

Demo for how to use Temporal Nexus in .NET via the Temporal .NET SDK. Preview feature.

## AI agent setup

- Root guidance: `AGENTS.MD`
- Minimal Copilot router: `.github/copilot-instructions.md`
- Scoped instructions: `.github/instructions/*.instructions.md`
- Modern skill location: `.agents/skills`
- Copilot cloud setup workflow: `.github/workflows/copilot-setup-steps.yml`

The setup workflow installs and verifies:
- Aspire CLI (`aspire`)
- Squad CLI (`squad`)
- Skill sources: Temporal Developer, Aspire Skills, Awesome Copilot, Squad, Superpowers
