---
name: update-os-coverage
description: >
  Audit and update OS version references in CI pipeline files. Scans Helix
  queue definitions and build container references for EOL or approaching-EOL
  OS versions, checks for newer versions to add, and aligns release branch
  coverage with supported-os.json in dotnet/core.
  USE FOR: replacing EOL OS references, adding new OS version coverage,
  periodic OS version audits, aligning release branches with supported-os.json,
  updating helix and build queues to match supported OSes.
  DO NOT USE FOR: supported-os.json changes (use update-supported-os skill
  in dotnet/core), VM/hardware requests (file dnceng issues),
  container image creation (file issues at dotnet-buildtools-prereqs-docker).
---

# Update OS Coverage

Audit and update OS version references in CI pipeline files. This repo's pipeline files define which OS versions are used for Helix testing and container-based builds. These references must be kept current — EOL versions create compliance risk and provide no benefit.

This skill is the CI-infrastructure counterpart to the [`update-supported-os`](https://github.com/dotnet/core/blob/main/.github/skills/update-supported-os/SKILL.md) skill in dotnet/core. That skill manages the public support matrix (`supported-os.json`). This skill manages what we actually test against.

See also: [OS onboarding guide](docs/project/os-onboarding.md) for broader context on OS version management in this repo.

## When to use

- An OS version has reached or is approaching end-of-life
- A new OS version is available and should be added to CI coverage
- Periodic audit to ensure pipeline files don't reference EOL versions
- A `supported-os.json` change in dotnet/core requires corresponding release branch updates
- An issue like [dotnet/runtime#125690](https://github.com/dotnet/runtime/issues/125690) identifies stale OS references

## Inputs and gates

Work in this skill is driven by two **inputs** and gated by one **prerequisite**:

### Inputs — what triggers the work

1. **supported-os.json** — A distro version is listed (or removed) in [`supported-os.json`](https://github.com/dotnet/core/tree/main/release-notes) in dotnet/core. Match each release branch to its corresponding dotnet/core directory (`release/9.0` ↔ `release-notes/9.0/supported-os.json`, etc.). When the support matrix changes, CI coverage must follow.

2. **Ad hoc request** — A user requests specific coverage changes (e.g., "add Fedora 44 to main", "please add extra platform coverage for Ubuntu 26.10"). This is the typical path for proactive additions in `main`, including pre-release distros.

### Gate — what must be true before acting

- **Container image availability** — A container image for the target OS version must exist at [dotnet-buildtools-prereqs-docker](https://github.com/dotnet/dotnet-buildtools-prereqs-docker). No image, no change. See [Step 5](#5-check-container-image-availability) for how to check. If the gate fails, search [open issues](https://github.com/dotnet/dotnet-buildtools-prereqs-docker/issues) for the missing image. If no tracking issue exists, file one requesting the image (with the distro, version, and purpose — e.g., "helix testing, amd64").

### How the pieces fit together

```
main (proactive, often ad hoc)
  → validates new distro version in CI
    → enables listing in supported-os.json (dotnet/core)
      → triggers release branch coverage here
```

1. **main leads.** We proactively adopt distro versions — including pre-release versions — in `main`. Testing here is a prerequisite for listing a version in `supported-os.json`.
2. **Release branches follow.** After a distro version is listed in `supported-os.json` for a GA .NET release, we add coverage to the corresponding release branch at best speed.
3. **EOL remediation is universal.** EOL references must be replaced in all branches — `main` and release branches alike.

## Process

### 1. Determine context

Identify the branch and its .NET version:

| Branch | .NET Version | Posture |
| ------ | ------------ | ------- |
| `main` | Next release (pre-GA) | Proactive — bleeding edge |
| `release/X.0` | X.0 (GA) | Reactive — follows supported-os.json |

Check the .NET version's support phase:

```bash
curl -sL https://github.com/dotnet/core/raw/refs/heads/main/release-notes/releases-index.json \
  | jq '.releases[] | select(.channel == "<version>") | {channel, "support-phase", "release-date", "eol-date"}'
```

For pre-GA .NET versions, determine the GA date. .NET releases GA in November:

- Even versions (10, 12, 14) GA in odd years — LTS
- Odd versions (11, 13, 15) GA in even years — STS

### 2. Scan pipeline files for OS references

Scan these files for OS version references:

| File | Contains |
| ---- | -------- |
| `eng/pipelines/helix-platforms.yml` | Centralized Helix queue variable definitions (latest/oldest) |
| `eng/pipelines/coreclr/templates/helix-queues-setup.yml` | CoreCLR test queue assignments (mix of hardcoded and variables) |
| `eng/pipelines/libraries/helix-queues-setup.yml` | Libraries test queue assignments (mostly hardcoded) |
| `eng/pipelines/installer/helix-queues-setup.yml` | Installer test queue assignments (mostly hardcoded) |
| `eng/pipelines/common/templates/pipeline-with-resources.yml` | Build container image definitions (all hardcoded) |

OS versions appear in two formats:

**Container image tags:**

```
mcr.microsoft.com/dotnet-buildtools/prereqs:{distro}-{version}-{purpose}-{arch}
```

Examples: `ubuntu-22.04-helix-amd64`, `alpine-3.23-helix-arm64v8`, `debian-13-helix-arm32v7`

**Helix queue names (PascalCase):**

```
(QueueId)HostId@mcr.microsoft.com/dotnet-buildtools/prereqs:{image-tag}
```

Queue identifiers encode the OS version: `Ubuntu.2204.Amd64.Open`, `Alpine.323.Amd64.Open`, `Debian.12.Arm32.Open`

> **Note:** Ubuntu and Alpine versions are concatenated in queue names (e.g., `2204` = 22.04, `323` = 3.23). Other distros use dot notation (`Debian.12`, `Fedora.42`, `openSUSE.15.5`).

To extract all OS references:

```bash
# Container image references
grep -rn 'dotnet-buildtools/prereqs:' eng/pipelines/ | grep -oP 'prereqs:\K[^ "]+' | sort -u

# Helix queue OS identifiers
grep -rn -oP '(?:Ubuntu|Debian|Fedora|openSUSE|Alpine|Centos|AzureLinux|OSX|Windows)[\w.]+' \
  eng/pipelines/ | sort -u
```

### 3. Check lifecycle status

Query [endoflife.date](https://endoflife.date) for each distro+version:

```bash
# Check a specific version
curl -s https://endoflife.date/api/{product}/{version}.json | jq '{eol, releaseDate, latest, lts}'

# List all versions for a product
curl -s https://endoflife.date/api/{product}.json | jq '.[] | {cycle, eol, releaseDate}'
```

Product ID mapping for distros used in this repo:

| Distro | endoflife.date ID |
| ------ | ----------------- |
| Alpine | `alpine` |
| Azure Linux | `azure-linux` |
| CentOS Stream | `centos-stream` |
| Debian | `debian` |
| Fedora | `fedora` |
| FreeBSD | `freebsd` |
| openSUSE | `opensuse` |
| Ubuntu | `ubuntu` |
| macOS | `macos` |
| Windows | `windows` |
| Windows Server | `windowsserver` |
| Android | `android` |
| iOS | `ios` |

Categorize each OS version:

| Category | Condition | Action |
| -------- | --------- | ------ |
| **EOL** | `eol` date is in the past | Must replace — all branches |
| **Approaching EOL** | `eol` date is within 3 months | Plan replacement |
| **Active** | `eol` date is >3 months away | No action needed |

> **Note:** The `eol` field can be a date string (`"2026-06-10"`) or a boolean (`false`). A value of `false` means no EOL date has been announced — treat as active.

### 4. Check for newer versions to add

For each distro in our coverage, check if newer versions are available that we aren't testing against:

```bash
# All active versions for a distro
curl -s https://endoflife.date/api/{product}.json \
  | jq '[.[] | select(.eol == false or (.eol | strings | . > (now | strftime("%Y-%m-%d"))))] | .[].cycle'
```

For `main` — also check for pre-release distro versions. See [Rule 3](#rule-3--add-pre-release-distros-proactively-in-main).

### 5. Check container image availability

Before recommending a new OS version, verify a container image exists at [dotnet-buildtools-prereqs-docker](https://github.com/dotnet/dotnet-buildtools-prereqs-docker):

```bash
# Check published images (preferred — shows currently active images)
curl -sL https://github.com/dotnet/versions/raw/refs/heads/main/build-info/docker/image-info.dotnet-dotnet-buildtools-prereqs-docker-main.json \
  | jq '[.repos[].images[].platforms[].simpleTags[]] | map(select(startswith("{distro}-{version}"))) | .[]'

# Examples
curl -sL <same-url> | jq '[.repos[].images[].platforms[].simpleTags[]] | map(select(startswith("fedora-44"))) | .[]'
curl -sL <same-url> | jq '[.repos[].images[].platforms[].simpleTags[]] | map(select(startswith("debian-13"))) | .[]'
```

If no image exists:

- Check if Dockerfiles are in preparation: `gh search code "{distro}/{version}" --repo dotnet/dotnet-buildtools-prereqs-docker`
- Search [open issues](https://github.com/dotnet/dotnet-buildtools-prereqs-docker/issues) for the missing image
- If no tracking issue exists, file one requesting the image (with the distro, version, and purpose — e.g., "helix testing, amd64")
- Do **not** add the OS version to pipeline files until an image is published

### 6. Cross-reference with supported-os.json (release branches)

For release branches, fetch the support matrix and check alignment:

```bash
curl -sL https://github.com/dotnet/core/raw/refs/heads/main/release-notes/{version}/supported-os.json
```

For each distro version listed in `supported-versions`:

- Check whether a corresponding Helix queue or build container exists in the release branch's pipeline files
- Flag gaps — these need coverage added at best speed

### 7. Present findings

Present findings organized by urgency. Use this format:

```markdown
## OS Coverage Audit — {branch} ({.NET version})

### EOL — replace now

- {distro} {version} (EOL {date}) → {replacement version}
  Files: {list of files with references}

### Approaching EOL — plan replacement

- {distro} {version} (EOL {date}) → {replacement version}

### New versions available

- {distro} {version} — container image: {available/missing}

### supported-os.json alignment gaps (release branches only)

- {distro} {version} — listed as supported, no CI coverage in this branch
```

Present recommendations to the user before making changes.

### 8. Apply changes

For each confirmed change:

1. Update all pipeline files that reference the old version
2. Replace both container image tags and Helix queue names
3. Update comments (e.g., `# Oldest: Debian 12` → `# Oldest: Debian 13`)
4. When updating `helix-platforms.yml`, update both the variable value and its comment

**When replacing an "oldest" version:** `helix-platforms.yml` uses a `latest`/`oldest` naming convention to test both the newest and oldest supported versions. When the "oldest" goes EOL:

- The current "latest" typically becomes the new "oldest"
- A newer version becomes the new "latest"

**Oldest and newest may be the same version.** This is expected and acceptable. It happens most often during preview .NET releases — aggressive EOL pruning (Rules 1–2) can leave only one active version for a distro. For pre-GA releases, set the "oldest" to the oldest version we expect to support at GA, even if that's also the newest. A second version can be added later when one becomes available.

### 9. Validate

Re-scan to confirm no EOL references remain:

```bash
grep -rn '{old-distro-version}' eng/pipelines/
```

## Branch strategy

### main — proactive

> Being _active_ in `main` enables being _lazy_ in `release/`. — [OS onboarding guide](docs/project/os-onboarding.md)

- **Add new versions early**, including pre-release distros (see [Rule 3](#rule-3--add-pre-release-distros-proactively-in-main))
- **Remove EOL versions** — they provide no benefit and create compliance risk
- **Keep at the bleeding edge** — references that start current in `main` rarely need remediation once they reach release branches
- Apply [preview .NET release rules](#preview-net-release-rules) when the .NET version in `main` is pre-GA

### Release branches — reactive

- **Follow supported-os.json** — add coverage after a distro version is [listed as supported](https://github.com/dotnet/core/tree/main/release-notes) for the corresponding GA .NET release
- **Wait for GA distros** — unlike `main`, release branches typically wait for distros to reach GA before adding coverage
- **Remediate EOL references** — replace with newer versions, same as `main`
- **Late in .NET support period** — if the .NET version is <6 months from EOL, skip non-critical OS updates. Don't upset what's working.

## Preview .NET release rules

When `main` targets a pre-GA .NET release, apply these rules. They mirror the [preview release rules](https://github.com/dotnet/core/blob/main/.github/skills/update-supported-os/SKILL.md#preview-release-rules) in the dotnet/core `update-supported-os` skill.

### Determining GA date

.NET releases GA in November. Even versions (10, 12) GA in odd years (LTS). Odd versions (11, 13) GA in even years (STS). Confirm via [`releases-index.json`](https://github.com/dotnet/core/raw/refs/heads/main/release-notes/releases-index.json).

### Rule 1 — Remove versions that won't survive to GA

Remove OS versions from CI coverage if they are already EOL or will reach EOL before the GA date. There's no value in testing against an OS that won't be supported when the .NET release ships.

**Windows ESU exception:** Windows versions covered by [Extended Security Updates (ESU)](https://learn.microsoft.com/windows-server/get-started/extended-security-updates-overview) are not considered EOL while ESU is active — unless the ESU program itself expires before GA.

### Rule 2 — Only add versions that survive GA + 6 months

When adding a new OS version to coverage, check whether it will still be active at **GA + 6 months**:

- EOL **after** GA + 6 months → **add it**
- EOL **before** GA + 6 months → **do not add it**
- No known EOL date → **add it**

This prevents adding coverage for OS versions that would require immediate remediation after the .NET release ships.

### Rule 3 — Add pre-release distros proactively in main

If a distro version is currently in preview but expected to GA **before** the .NET GA date, add it to `main` coverage if **all** of the following are true:

1. The distro version is expected to GA before the .NET GA date
2. It passes the [Rule 2](#rule-2--only-add-versions-that-survive-ga--6-months) EOL check
3. Preview builds of the distro are publicly available
4. A container image exists at [dotnet-buildtools-prereqs-docker](https://github.com/dotnet/dotnet-buildtools-prereqs-docker) (see [Step 5](#5-check-container-image-availability))

If conditions 1–3 are met but no container image exists, do not add it yet. Check for or file a tracking issue at dotnet-buildtools-prereqs-docker.

### Examples

.NET 11 (STS) GAs November 2026. GA + 6 months = May 2027.

**Rule 1 — Remove versions EOL before GA:**

- openSUSE Leap 15.5 (EOL) → **remove**, replace with 16.0
- Fedora 42 (EOL 2026-05-13) → **remove**, replace with 43+
- Debian 12 (EOL 2026-06-10) → **remove**, replace with 13

**Rule 2 — GA + 6 months gate:**

- Alpine 3.21 (EOL 2026-11-01) → **do not add** (EOL before May 2027)
- Alpine 3.23 (EOL 2027-11-01) → **add** (EOL after May 2027)

**Rule 3 — Pre-release distros:**

- Fedora 44 (expected ~April 2026, EOL ~May 2027) → **add** if container image exists
- Ubuntu 26.04 (expected ~April 2026, EOL ~April 2031) → **add** if container image exists

## Key facts

- `helix-platforms.yml` is the centralized variable file, but several pipeline files still have hardcoded OS references that must be updated independently
- Container images must exist at [dotnet-buildtools-prereqs-docker](https://github.com/dotnet/dotnet-buildtools-prereqs-docker) before an OS version can be added to pipeline files
- The [`update-supported-os`](https://github.com/dotnet/core/blob/main/.github/skills/update-supported-os/SKILL.md) skill in dotnet/core manages the public support matrix — this skill manages CI infrastructure
- Testing in `main` is a prerequisite for declaring support in `supported-os.json`
- Alpine `edge` is a rolling release with no EOL date — it is always appropriate for "latest" coverage
- OS version strings in Helix queue names use PascalCase with version-specific concatenation rules (Ubuntu `2204`, Alpine `323`, but Debian `12`, Fedora `42`)

## Tips

- Start with `helix-platforms.yml` — it's the central definition file. Then check the other pipeline files for hardcoded references to the same versions.
- When replacing an "oldest" version, check what the current "latest" is — the old "latest" typically becomes the new "oldest".
- For `main`, err on the side of adding newer versions. For release branches, err on the side of stability.
- Check the [.NET OS Support Tracking](https://github.com/dotnet/core/issues/9638) issue for broader context on OS lifecycle decisions.
- Use `git grep` across branches to audit release branches without switching: `git grep 'opensuse-15.5' release/9.0 -- eng/pipelines/`
- The [OS onboarding guide](docs/project/os-onboarding.md) has example PRs showing past OS version updates.
