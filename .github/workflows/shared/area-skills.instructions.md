# Area → skill directory

A single source of truth mapping a `dotnet/runtime` area / pipeline to the
Copilot skill that carries the domain knowledge for it. Agents and skills that
need to route a failure, a review, or a change to the right area knowledge
should reference this table instead of re-declaring their own copy, so the
mapping stays consistent as skills are added or renamed.

Skill paths in the table below are relative to `.github/skills/`, so
`mobile-platforms/SKILL.md` means `.github/skills/mobile-platforms/SKILL.md`.
The one non-skill entry (NativeAOT) is given as a repo-root-relative path and is
called out as such. Area-owner handles are **not** listed here; resolve owners
from [`docs/area-owners.md`](../../../docs/area-owners.md) by the issue's
`area-*` label.

<a id="area-skill-table"></a>

## Area → skill table

Skill paths are relative to `.github/skills/`.

| Area / pipeline | Skill(s) | What the skill covers |
|---|---|---|
| Mobile (ios / tvos / maccatalyst / android / wasm / wasi) | `mobile-platforms/SKILL.md` | Mobile/wasm test, csproj, and platform-condition knowledge. |
| JIT / GC / PGO stress (codegen) | `jit-regression-test/SKILL.md`; `ci-pipeline-monitor/SKILL.md` | JIT codegen, assertion triage, regression-test extraction. |
| `System.Net.*` | `system-net-review/SKILL.md` | Networking stack review and conventions. |
| `Microsoft.Extensions.*` | `extensions-review/SKILL.md` | Extensions (DI, config, logging, hosting, caching) review. |
| NativeAOT outer loop | repo-root `eng/testing/tests.*aot*.targets` + the test `.csproj` | NativeAOT test wiring (no dedicated skill; read the targets). |
| Generic / unmapped | `ci-pipeline-monitor/SKILL.md` | Cross-cutting CI pipeline monitoring and triage. |

When more than one skill is listed, load them in order; the first carries the
primary domain knowledge and the rest add CI/triage context.

<a id="area-mention-conventions"></a>

## Area-owner mention conventions

When an agent loops a human in (a PR body or an issue comment) based on an
`area-*` label, apply these rules so notifications stay proportionate:

- Resolve owners from [`docs/area-owners.md`](../../../docs/area-owners.md) by the
  `area-*` label.
- Live-mention **at most one or two** individual owners.
- **Never live-mention a GitHub team.** Render a team handle (`@dotnet/<team>`)
  as inline code `` `@dotnet/<team>` `` so it does not notify the whole team.
- Never mention bots (`dotnet-maestro`, `github-actions`, codeflow accounts).
- Put contacts under a non-accusatory heading framed as "loop-in for triage",
  not blame.
