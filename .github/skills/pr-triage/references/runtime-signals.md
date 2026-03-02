# dotnet/runtime Signals Reference

Runtime-specific signals, labels, area ownership, and automation behavior for PR triage.

---

## Label Taxonomy

| Label | Meaning | Applied by | Triage impact |
|-------|---------|-----------|--------------|
| `needs-author-action` | Reviewer requested changes, waiting on author | Maintainers, or auto when review has `Changes_requested` | ❌ Blocking — author must act |
| `needs-further-triage` | Author responded to `needs-author-action`, needs maintainer re-review | Auto (when author comments on non-untriaged issue) | ⚠️ Maintainer should re-engage |
| `untriaged` | PR hasn't been categorized by area yet | Auto on creation | ❌ Missing area routing |
| `no-recent-activity` | No activity for 14 days | Auto (resourceManagement.yml) | ⚠️ Will auto-close in 14 more days |
| `backlog-cleanup-candidate` | Issue inactive for 1644 days | Auto | Ignore for PR triage |
| `community-contribution` | PR from external contributor | Maintainers | Flagged for visibility so maintainers can prioritize |
| `area-*` (e.g., `area-System.Net`) | Component area label | Maintainers/auto | Used to find area owners |
| `api-ready-for-review` | Public API changes pending review | Author/maintainers | ⚠️ Needs API review before merge |
| `api-approved` | API review completed | API review board | ✅ API review done |
| `Known Build Error` | CI failure matched to known issue | Build Analysis automation | Used by ci-analysis skill |

---

## Area Owners Lookup

The file `docs/area-owners.md` contains a table mapping area labels to leads and owners. Format:

```
| Area | Lead | Owners (area experts to tag in PRs and issues) | Notes |
|------|------|------------------------------------------------|-------|
| area-System.Net.Http | @dotnet/ncl | @dotnet/ncl | |
```

### How to look up owners for a PR

1. Get the PR's area label(s) from `gh pr view --json labels`
2. Parse the area-owners.md table (the script does this automatically at startup)
3. Match the area label to find the Lead and Owners columns
4. These are the people whose APPROVED review counts as "maintainer review"

**Note**: The script parses `docs/area-owners.md` dynamically at startup to get
the full owner table (~138 areas). Do not rely on hardcoded owner lists.

**Fallback**: If the PR has no area label, use `.github/CODEOWNERS` to match file paths to reviewers.

---

## Approval Authority Levels

Not all approvals carry equal weight for merge decisions. This reflects dotnet/runtime's
governance model where area owners have merge authority — it is not a judgment of review quality.

| Level | Who | How to detect | Merge weight |
|-------|-----|--------------|--------------|
| **1. Area owner/lead** | Listed in Owners/Lead column of `docs/area-owners.md` for the PR's area label | Parse area-owners.md, match reviewer login against Owners column | Has merge authority for the area |
| **2. Community triager** | Trusted community members with triage permissions, listed in area-owners.md | Parse the "Community Triagers" section at the bottom of `docs/area-owners.md` | Strong signal — deeply familiar with the repo. Does not have merge authority but review is highly valued. |
| **3. Frequent contributor** | Has many merged PRs or commits in the touched area | `gh api "repos/dotnet/runtime/commits?author={login}&path={dir}&per_page=5"` — 3+ hits = frequent | Valuable domain expertise. Weight 1× (standard). |
| **4. New contributor** | First-time or infrequent contributor to this area | No commits found in touched paths | Review appreciated — every contributor's feedback helps improve the PR. Weight 0.5×. |

### Known Community Triagers

These are listed in the "Community Triagers" section of `docs/area-owners.md` and have triage permissions:

@a74nh, @am11, @clamp03, @Clockwork-Muse, @filipnavara, @huoyaoyuan, @martincostello, @omajid, @Sergio0694, @shushanhf, @SingleAccretion, @teo-tsirpanis, @tmds, @vcsjones, @xoofx

A Community Triager's APPROVED review is a stronger signal than a new contributor's approval. They are deeply familiar with the repo, its conventions, and its quality bar — but they are not area owners and their approval alone does not satisfy the "maintainer review" dimension.

### Detecting reviewer level at runtime

1. Get the PR's area label(s)
2. Parse `docs/area-owners.md` — match the area label row to get Owners
3. For each reviewer: check if their login appears in the Owners column (tier 1), the Community Triagers list (tier 2), or has recent commits in the area (tier 3). Otherwise tier 4.

---

## resourceManagement.yml Automation

The `.github/policies/resourceManagement.yml` file defines automated label management.

### PR automation

- PRs with `needs-author-action` + 14 days no activity → `no-recent-activity` label added
- PRs with `no-recent-activity` + 14 more days → auto-closed
- Draft PRs with 30 days no activity → auto-closed
- Pushing to PR branch → removes `needs-author-action`
- Author commenting → removes `needs-author-action`
- Review with `changes_requested` by non-read-only user → adds `needs-author-action`

### What this means for triage

- `needs-author-action` is a reliable "ball is in author's court" signal
- `no-recent-activity` means the PR is at risk of auto-closure
- Absence of `needs-author-action` does NOT mean feedback is addressed — check unresolved review threads too (the label system has gaps)

---

## Community Contributions

PRs with `community-contribution` label are flagged for visibility so maintainers can prioritize:

- **Note in output** — the `is_community` flag lets maintainers filter and prioritize as they see fit
- **Be patient** with response times — community contributors have other commitments
- **Check author familiarity** — returning community contributors vs first-time contributors have different needs
- Timely feedback — even a quick "not right now" — respects contributors' time

---

## Bot PRs

| Bot | Label/Detection | Behavior |
|-----|----------------|----------|
| `dotnet-maestro[bot]` | `area-codeflow` label, author is `app/dotnet-maestro` | Dependency updates. Missing packages ≠ infrastructure. Different evaluation criteria. Exclude by default. |
| `copilot-swe-agent` | Author is `app/copilot-swe-agent` | Invoked by maintainers. Apply normal triage criteria. **Include by default.** Note agent authorship in output. |

**Default**: Exclude `dotnet-maestro` PRs. Include `copilot-swe-agent` PRs (they are maintainer-initiated and follow normal review workflows).

---

## Perf-Sensitive Paths

Files in these paths should flag the perf-sensitivity dimension:

- `src/libraries/System.Private.CoreLib/` — core runtime library
- `src/coreclr/jit/` — JIT compiler
- `src/coreclr/gc/` — garbage collector
- `src/coreclr/vm/` — VM runtime
- `src/libraries/System.Runtime/` — core types
- Any file with `Span`, `Memory`, `Buffer`, `Vector` in the path
- `src/libraries/System.Collections/` — hot collection types
- `src/libraries/System.Text.Json/` — high-perf serialization

Check for @EgorBot benchmark comments to see if perf was already validated.

---

## API Surface Paths

Files matching these patterns indicate public API changes:

- `src/libraries/*/ref/*.cs` — API reference assemblies
- `src/libraries/*/ref/*.csproj` — ref project files

If changed, check for `api-ready-for-review` or `api-approved` labels.
