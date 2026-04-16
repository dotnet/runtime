---
name: update-os-coverage
description: >
  Update OS version references in Helix queue definitions to add new versions,
  replace EOL versions, or audit coverage against the supported-os matrix.
  USE FOR: adding new OS versions to Helix queues, replacing EOL OS versions,
  upgrading "oldest" or "latest" version references, auditing Helix coverage.
  DO NOT USE FOR: creating new container images (that's dotnet-buildtools-prereqs-docker),
  updating supported-os.json (that's the update-supported-os skill in dotnet/core).
---

# Update OS Coverage

Update OS version references in Helix queue definition files. These files control which operating system versions are used for CI/CD testing via Helix.

## Prerequisites

> **Baseline build not required:** This skill is for YAML/docs-style queue and image reference updates, not product code changes. Do **not** start with the repo-wide baseline build workflow from [`copilot-instructions.md`](../../copilot-instructions.md) unless the task expands beyond image / queue metadata into code changes that actually need build or test validation.

## When to use

- An OS version is approaching or has reached EOL and should be replaced
- A new OS version is released and should be added to Helix testing for coverage
- We take a more proactive approach on `main`. If a distro version will be EOL before our annual November release, we should update it to a newer version. If a distro version is expected to ship within one quarter (3 months) and a `prereqs` container image already exists, we should add it to Helix testing.
- The availability of an image in the `prereqs` container repo is a strong signal that the OS version is approved for Helix testing, at least on `main`.
- Helix coverage does not match the supported-os matrix (for example, the relevant `release-notes/<dotnet-version>/supported-os.json` file in [dotnet/core](https://github.com/dotnet/core/tree/main/release-notes)).
- Upgrading "oldest" or "latest" version slots for a distro

For servicing / `release/*` branches, be more conservative: only update to GA and already-supported distro versions unless the user explicitly asks for a forward-looking change.

## When NOT to use

- Creating new container images → file an issue or PR at [dotnet-buildtools-prereqs-docker](https://github.com/dotnet/dotnet-buildtools-prereqs-docker)
- Updating `supported-os.json` / `supported-os.md` → file an issue in [dotnet/core](https://github.com/dotnet/core)
- Adding entirely new distros or architectures to Helix (requires pipeline template changes beyond version bumps)
- Requesting new Helix VM queues → file an issue at [dotnet/dnceng](https://github.com/dotnet/dnceng)
- Updating Windows or macOS Helix queues — these use VM-based queues with a simpler format (e.g. `Windows.11.Amd64.Client.Open`) and version updates typically require dnceng coordination

## Key files

OS version references appear in these pipeline files:

| File | Purpose |
|------|---------|
| `eng/pipelines/helix-platforms.yml` | Central platform definitions — a useful starting point for many `latest` and `oldest` OS version variables, but not the sole source of truth |
| `eng/pipelines/libraries/helix-queues-setup.yml` | Libraries Helix queue assignments — inline OS version references per platform |
| `eng/pipelines/coreclr/templates/helix-queues-setup.yml` | CoreCLR Helix queue assignments — inline OS version references |
| `eng/pipelines/installer/helix-queues-setup.yml` | Installer Helix queue assignments |
| `eng/pipelines/common/templates/pipeline-with-resources.yml` | Build container definitions (not Helix queues, but OS version references for build images) |
| `docs/workflow/using-docker.md` | Documents the official build/test Docker images — update only when build image versions change (cross-compilation images, not Helix test images) |

The [OS onboarding guide](../../../docs/project/os-onboarding.md) is the authoritative reference for how OS versions are managed in this repo. Read it if more context is needed on our policies.

### helix-platforms.yml structure

Many Linux container-backed entries in this file use a pattern like:

```yaml
# <Distro> <arch>
# Latest: <distro-version>
- name: helix_linux_x64_<distro>_latest
  value: (<QueueName>)<HostQueue>@mcr.microsoft.com/dotnet-buildtools/prereqs:<image-tag>
```

Where `<QueueName>` is the Helix queue identifier (e.g. `Fedora.44.Amd64.Open`), `<HostQueue>` is the physical host queue (e.g. `AzureLinux.3.Amd64.Open`), and `<image-tag>` is the container image tag (e.g. `fedora-44-helix-amd64`).

Other entries in the same file are plain queue values (for example Windows, macOS, and some Linux VM queues) rather than `(<QueueName>)<HostQueue>@<image>`. When the target entry is queue-only, preserve that format and update only the versioned queue string.

Some platform variables have `_internal` counterparts (e.g. `helix_linux_x64_oldest_internal`, `helix_linux_musl_arm32_latest_internal`) that use the same queue/image but drop the `.Open` suffix. When an `_internal` counterpart exists, update both the `.Open` and `_internal` entries.

### helix-queues-setup.yml files

These files reference OS versions directly (not via variables) in conditional blocks per platform. Most Linux container-backed inline references follow the `(<QueueName>)<HostQueue>@<image>` format, while a few entries are plain queue values (for example, some AzureLinux-only queues). Preserve the existing format for the specific entry you are updating.

## Inputs

The user provides one or more of:

- **Distro and version** — e.g. "Update Fedora to 44", "Replace Alpine 3.20 with 3.22"
- **Slot** — whether to update `latest`, `oldest`, or both
- **Branch** — defaults to current branch; may also need release branch updates
- **Audit mode** — "check all OS versions against supported-os.json"

If the user provides only a distro name without specifying slots, either ask or determine with basic logic which slots to update (for example, if the current `latest` is EOL, update `latest`; if the current `oldest` is EOL, update `oldest`).

## Process

Use the repo tools that fit the environment. The shell snippets below are reference commands, not a required literal script; equivalent `gh`, `git`, or search-based workflows are fine.

### 1. Verify container image availability

Before making any changes, confirm the **exact target container tag** exists in the [image-info JSON](https://github.com/dotnet/versions/blob/main/build-info/docker/image-info.dotnet-dotnet-buildtools-prereqs-docker-main.json). This file is more authoritative than probing the registry directly and should be the primary source of truth for published `dotnet-buildtools/prereqs` tags:

```bash
TARGET_TAG="<exact-image-tag>"
curl -sL https://github.com/dotnet/versions/raw/refs/heads/main/build-info/docker/image-info.dotnet-dotnet-buildtools-prereqs-docker-main.json \
  | jq -r --arg tag "$TARGET_TAG" '[.repos[].images[].platforms[].simpleTags[]] | unique | map(select(. == $tag)) | .[]'
```

If the exact tag is **not found in `image-info`**, stop and inform the user. Treat that as authoritative even if a registry lookup appears to work. The image must be created first at [dotnet/dotnet-buildtools-prereqs-docker](https://github.com/dotnet/dotnet-buildtools-prereqs-docker). Check if an open issue or PR already exists, for example:

```bash
gh search issues "<distro> <distro-version>" --repo dotnet/dotnet-buildtools-prereqs-docker --state open
```

### 2. Check support policy first, then EOL dates if needed

First, inspect the relevant `supported-os.json` entry in `dotnet/core` to see whether the distro/version is already supported for the target release and to find its official lifecycle link:

```bash
curl -sL https://github.com/dotnet/core/raw/refs/heads/main/release-notes/<dotnet-version>/supported-os.json \
  | jq '.families[] | select(.name == "Linux") | .distributions[] | select(.id == "<distro-id>") | {name, lifecycle, supportedVersions: ."supported-versions", unsupportedVersions: ."unsupported-versions"}'
```

If the target distro version is already listed in `supportedVersions`, that is the primary signal that the change is appropriate for the corresponding release line. On servicing branches, prefer versions that are already GA and present there.

If you need an independent lifecycle check, or if `supported-os.json` does not yet reflect the situation clearly, use [endoflife.date](https://endoflife.date) as a fallback:

```bash
curl -s https://endoflife.date/api/<distro-id>.json | jq '.[] | select(.cycle == "<distro-version>") | {cycle, eol, releaseDate}'
```

The `<distro-id>` values typically match across both sources (e.g. `fedora`, `alpine`, `debian`, `opensuse`, `ubuntu`, `centos-stream`).

### 3. Scan current references

Search for all current references to the distro being updated. For example:

```bash
grep -rn -i "<distro>" \
  eng/pipelines/helix-platforms.yml \
  eng/pipelines/libraries/helix-queues-setup.yml \
  eng/pipelines/coreclr/templates/helix-queues-setup.yml \
  eng/pipelines/installer/helix-queues-setup.yml \
  eng/pipelines/common/templates/pipeline-with-resources.yml \
  docs/workflow/using-docker.md
```

Note every occurrence — the same distro may appear in multiple sections (x64, arm32, arm64) and in multiple files.

### 4. Apply changes

For each reference found in step 3:

1. **Start with `helix-platforms.yml`** — it is a convenient central catalog for many Linux container-backed entries, but it is not the source of truth by itself
   - Update the version comment (e.g. `# Latest: 43` → `# Latest: 44`)
   - Update the variable value — adjust the queue name and image tag to use the new version when the entry uses the container-backed format
   - Preserve the existing host queue (e.g. `AzureLinux.3.Amd64.Open`) — this does not change with distro version updates
   - Then continue through the other files until every matching reference is updated consistently

2. **Update `helix-queues-setup.yml` files** — libraries, coreclr, and installer templates
   - Search for inline references to the old version and update them
   - These are direct queue strings, not variable references

3. **Version naming conventions** — follow existing patterns exactly:

   | Distro | Queue name pattern | Image tag pattern |
   |--------|--------------------|-------------------|
   | Alpine | `Alpine.<ver-no-dots>.Amd64.Open` | `alpine-<ver>-helix-amd64` |
   | Alpine (edge) | `Alpine.edge.Amd64.Open` (casing varies by file; see note below) | `alpine-edge-helix-amd64` |
   | CentOS Stream | `Centos.<ver>.Amd64.Open` | `centos-stream-<ver>-helix-amd64` |
   | Debian | `Debian.<ver>.Amd64.Open` | `debian-<ver>-helix-amd64` |
   | Fedora | `Fedora.<ver>.Amd64.Open` | `fedora-<ver>-helix-amd64` |
   | openSUSE | `openSUSE.<ver>.Amd64.Open` | `opensuse-<ver>-helix-amd64` |
   | Ubuntu | `Ubuntu.<ver-no-dots>.Amd64.Open` | `ubuntu-<ver>-helix-amd64` |

   For Alpine edge queues, the casing of `edge` is not consistent across the repo (`Alpine.edge.Amd64.Open` in `helix-platforms.yml` vs `Alpine.Edge.Amd64.Open` in `libraries/helix-queues-setup.yml`). When updating queue strings, **preserve the existing casing used in each file** rather than normalizing to a single pattern.

   AzureLinux (e.g. `AzureLinux.3.Amd64.Open`) appears as both a standalone VM queue and the primary host queue for container-based distros. It does not follow the container image pattern above.

   Architecture suffixes vary: `Amd64`, `Arm64`, `ArmArch`, `Arm32` for queue names; `amd64`, `arm64v8`, `arm32v7` for image tags.

   When both generic and processor-specific aliases exist in `image-info` (for example, `ubuntu-26.04-helix-webassembly` and `ubuntu-26.04-helix-webassembly-amd64`), **prefer the processor-specific tag** when the queue/environment is processor-specific:

   - `...Amd64...` queue → prefer `*-amd64`
   - `...Arm64...` / `...ArmArch...` queue → prefer `*-arm64v8`
   - `...Arm32...` queue → prefer `*-arm32v7`

   Use the generic alias only when the surrounding environment is intentionally architecture-agnostic or when no processor-specific tag exists in `image-info`.

   For ARM-based queues, host queues are often `Ubuntu.2204.ArmArch.Open`, but some queues (for example `helix_linux_arm64_oldest`) use AzureLinux-based host queues such as `AzureLinux.3.Arm64.Open`. Follow the existing pattern for the specific queue in `eng/pipelines/helix-platforms.yml` when updating versions.

### 5. Validate changes

After editing, verify:

1. **No stale references remain** — re-run the grep from step 3, replacing the distro name with the old version pattern. Stale references are acceptable only if intentionally kept (e.g. the version is still used for `oldest`).

2. **All new references are syntactically consistent** — compare with adjacent entries in the same file to verify formatting.

3. **Updated image tags are present in `image-info`** — verify that each new tag you used appears in `image-info.dotnet-dotnet-buildtools-prereqs-docker-main.json`.

4. **Variable names are unchanged** — only the `value` fields change, never the `name` fields.

### 6. CI pipeline coverage

Not all Helix queues run in the default PR pipeline (`runtime`). Some distros are only exercised by the **extra-platforms** pipeline (`runtime-extra-platforms`), which runs on a daily schedule and can be triggered on PRs with `/azp run runtime-extra-platforms`.

**Distros behind the `isExtraPlatformsBuild` guard** — generally need `runtime-extra-platforms` for coverage that is not already exercised by the default `runtime` pipeline:

| Distro | Platform | File |
|--------|----------|------|
| Debian* | linux_x64, linux_arm | `libraries/helix-queues-setup.yml` |
| Fedora | linux_x64 | `libraries/helix-queues-setup.yml` |
| openSUSE | linux_x64 | `libraries/helix-queues-setup.yml` |
| Alpine edge | linux_musl_x64 | `libraries/helix-queues-setup.yml` |
| Ubuntu | linux_arm64 | `libraries/helix-queues-setup.yml` |
| Alpine (versioned) | linux_musl_arm64 | `libraries/helix-queues-setup.yml` |

> **Note:** Debian `linux_x64` is also exercised in the default `runtime` pipeline for some libraries scenarios (for example, when `interpreter: true` or `isSingleFile: true`). Use `runtime-extra-platforms` when you need Debian coverage outside those default-pipeline cases; Debian `linux_arm` remains extra-platforms-only here.

**Distros in the default PR pipeline** — no extra pipeline needed:

| Distro | Platform | File |
|--------|----------|------|
| Ubuntu | linux_x64 | `libraries/helix-queues-setup.yml` |
| AzureLinux | linux_x64, linux_arm64 | `libraries/helix-queues-setup.yml`, `coreclr/templates/helix-queues-setup.yml` |
| CentOS Stream | linux_x64 | `libraries/helix-queues-setup.yml` |
| Alpine (versioned) | linux_musl_x64, linux_musl_arm64 | `libraries/helix-queues-setup.yml`, `coreclr/templates/helix-queues-setup.yml` |

> **Note:** Alpine versioned linux_musl_arm64 is behind `isExtraPlatformsBuild` in `libraries/helix-queues-setup.yml`, but runs unconditionally in `coreclr/templates/helix-queues-setup.yml`. It is listed here because coreclr provides default pipeline coverage.

**Decision:** After creating a PR, cross-reference the distro/platform combinations you changed against the tables above. Trigger `runtime-extra-platforms` when you changed coverage that is only, or primarily, exercised by the first table. If every changed distro/platform is already covered by the default `runtime` pipeline, no extra pipeline run is needed.

When extra-platforms is needed, post the trigger comment on the PR:

```bash
gh pr comment <pr-number> --body "/azp run runtime-extra-platforms"
```

Tell the user you've triggered the pipeline and which distros required it.

> **Note:** The `outerloop` pipeline (`libraries/outerloop.yml`) does not add Linux distro coverage beyond the default pipeline for normal PR validation. The extra-platforms distros (Fedora, Debian, openSUSE) are only brought in for specific non-PR / rolling-build-style cases there, so **do not** trigger outerloop for Linux distro version changes unless the user explicitly requests it.

### 7. Check other branches

After updating `main`, check whether release branches also need updates:

```bash
for branch in $(git branch -r | grep -E 'origin/release/'); do
  echo "=== $branch ==="
  git show "$branch:eng/pipelines/helix-platforms.yml" 2>/dev/null | grep -n -i "<distro>" || echo "(no matches or file not found)"
done
```

> **Note**: In shallow clones, remote release branches may not be visible. Run `git fetch origin 'refs/heads/release/*:refs/remotes/origin/release/*'` first, or check release branches via GitHub.

Release branches should be updated when:
- The old version is EOL or approaching EOL
- The release branch will be serviced for longer than the old version's remaining support

On servicing branches, prefer GA and already-supported distro versions. Do not pre-stage not-yet-GA or merely upcoming versions there unless the user explicitly requests it.

Note: Release branch updates should be done in separate PRs.

### 8. Cross-reference with supported-os

Check if the relevant `supported-os.json` in dotnet/core needs corresponding updates. The file lives under the pattern `release-notes/<dotnet-version>/supported-os.json` (for example, `release-notes/8.0/supported-os.json`) in the [release-notes directory](https://github.com/dotnet/core/tree/main/release-notes). If a new distro version is being added to Helix but isn't yet in supported-os, inform the user to run the `update-supported-os` skill in dotnet/core.

### 9. Create PR

The PR description should include:
   - Table of changes (old version → new version, which slots)
   - EOL dates for old and new versions
   - Confirmation that the exact container image tags are available in `image-info.dotnet-dotnet-buildtools-prereqs-docker-main.json`
   - Which CI pipeline(s) need to run (see [step 6](#6-ci-pipeline-coverage))
   - Link to the [os-onboarding guide](https://github.com/dotnet/runtime/blob/main/docs/project/os-onboarding.md)
   - Link to tracking issue if applicable (e.g. [dotnet/core#9638](https://github.com/dotnet/core/issues/9638))

> 📝 **AI-generated content disclosure:** When posting any content to GitHub (PR descriptions, comments) under a user's credentials — i.e., the account is **not** a dedicated "copilot" or "bot" account/app — you **MUST** include a concise, visible note (e.g. a `> [!NOTE]` alert) indicating the content was AI/Copilot-generated. Skip this if the user explicitly asks you to omit it.

## Audit mode

When asked to audit all OS coverage:

1. Fetch the current supported-os.json for the target .NET version:
   ```bash
   curl -sL https://github.com/dotnet/core/raw/refs/heads/main/release-notes/<dotnet-version>/supported-os.json
   ```

2. For each Linux distro in supported-os, check:
   - The `latest` version in `helix-platforms.yml` should be the newest supported version
   - The `oldest` version should be the oldest still-supported version
   - No EOL versions should remain

3. Check EOL dates for all referenced versions:
   ```bash
   curl -s https://endoflife.date/api/<distro-id>.json | jq '[.[] | select(.eol != false) | select(.eol < "YYYY-MM-DD")] | .[].cycle'
   ```

4. Report findings as a table:

   | Distro | Slot | Current | Recommended | Reason |
   |--------|------|---------|-------------|--------|
   | Fedora | latest | 43 | 44 | 44 now GA |
   | Fedora | oldest | 42 | 43 | 42 EOL 2026-05-13 |

## Reference

- [OS onboarding guide](../../../docs/project/os-onboarding.md)
- [.NET OS Support Tracking](https://github.com/dotnet/core/issues/9638)
- [Prereq container image lifecycle](https://github.com/dotnet/dotnet-buildtools-prereqs-docker/blob/main/lifecycle.md)
- [Container image registry (image-info)](https://github.com/dotnet/versions/blob/main/build-info/docker/image-info.dotnet-dotnet-buildtools-prereqs-docker-main.json)
- [endoflife.date](https://endoflife.date/) for OS lifecycle data
- [PR #125991](https://github.com/dotnet/runtime/pull/125991) — example EOL OS version replacement
- [PR #111768](https://github.com/dotnet/runtime/pull/111768) — example new OS version onboarding
