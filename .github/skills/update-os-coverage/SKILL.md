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

## When to use

- A new OS version is released and should be added to Helix testing
- An OS version is approaching or has reached EOL and should be replaced
- Periodic audit to ensure Helix coverage matches the supported-os matrix (for example, [`release-notes/11.0/supported-os.json`](https://github.com/dotnet/core/blob/main/release-notes/11.0/supported-os.json); update the version segment to match your target)
- Upgrading "oldest" or "latest" version slots for a distro

## When NOT to use

- Creating new container images → file an issue or PR at [dotnet-buildtools-prereqs-docker](https://github.com/dotnet/dotnet-buildtools-prereqs-docker)
- Updating `supported-os.json` / `supported-os.md` → use the `update-supported-os` skill in [dotnet/core](https://github.com/dotnet/core)
- Adding entirely new distros or architectures to Helix (requires pipeline template changes beyond version bumps)
- Requesting new Helix VM queues → file an issue at [dotnet/dnceng](https://github.com/dotnet/dnceng)
- Updating Windows or macOS Helix queues — these use VM-based queues with a simpler format (e.g. `Windows.11.Amd64.Client.Open`) and version updates typically require dnceng coordination

## Key files

The [OS onboarding guide](/docs/project/os-onboarding.md) is the authoritative reference for how OS versions are managed in this repo. Read it first for context on policies and processes.

OS version references appear in these pipeline files:

| File | Purpose |
|------|---------|
| `eng/pipelines/helix-platforms.yml` | **Primary target.** Central platform definitions — defines `latest` and `oldest` version variables for all OS/arch combinations |
| `eng/pipelines/libraries/helix-queues-setup.yml` | Libraries Helix queue assignments — inline OS version references per platform |
| `eng/pipelines/coreclr/templates/helix-queues-setup.yml` | CoreCLR Helix queue assignments — inline OS version references |
| `eng/pipelines/installer/helix-queues-setup.yml` | Installer Helix queue assignments |
| `eng/pipelines/common/templates/pipeline-with-resources.yml` | Build container definitions (not Helix queues, but OS version references for build images) |
| `docs/workflow/using-docker.md` | Documents the official build/test Docker images — update only when build image versions change (cross-compilation images, not Helix test images) |

### helix-platforms.yml structure

This file defines named variables following a consistent pattern:

```yaml
# <Distro> <arch>
# Latest: <version>
- name: helix_linux_x64_<distro>_latest
  value: (<QueueName>)<HostQueue>@<container-image>

# Oldest: <version>
- name: helix_linux_x64_<distro>_oldest
  value: (<QueueName>)<HostQueue>@<container-image>
```

The value format for container-based queues is:
```
(<QueueName>)<HostQueue>@mcr.microsoft.com/dotnet-buildtools/prereqs:<image-tag>
```

Where:
- `<QueueName>` — Helix queue identifier, e.g. `Fedora.44.Amd64.Open`
- `<HostQueue>` — physical host queue, e.g. `AzureLinux.3.Amd64.Open`
- `<image-tag>` — container image tag, e.g. `fedora-44-helix-amd64`

Some platform variables have `_internal` counterparts (e.g. `helix_linux_x64_oldest_internal`, `helix_linux_musl_arm32_latest_internal`) that use the same queue/image but drop the `.Open` suffix. When an `_internal` counterpart exists, update both the `.Open` and `_internal` entries.

### helix-queues-setup.yml files

These files reference OS versions directly (not via variables) in conditional blocks per platform. Each inline reference follows the same `(<QueueName>)<HostQueue>@<image>` format.

## Inputs

The user provides one or more of:

- **Distro and version** — e.g. "Update Fedora to 44", "Replace Alpine 3.20 with 3.22"
- **Slot** — whether to update `latest`, `oldest`, or both
- **Branch** — defaults to current branch; may also need release branch updates
- **Audit mode** — "check all OS versions against supported-os.json"

If the user provides only a distro name without specifying slots, determine the correct updates based on:
- If adding a brand-new version: it typically becomes the new `latest`, and the previous `latest` becomes `oldest`
- If replacing an EOL version: update `oldest` to the next supported version
- If the distro currently has `latest == oldest`: only update `latest` to the new version

## Process

Requires `curl` and `jq` for the verification and audit steps below.

### 1. Verify container image availability

Before making any changes, confirm the target container image exists. Check the [image-info JSON](https://github.com/dotnet/versions/blob/main/build-info/docker/image-info.dotnet-dotnet-buildtools-prereqs-docker-main.json) for published images:

```bash
curl -sL https://github.com/dotnet/versions/raw/refs/heads/main/build-info/docker/image-info.dotnet-dotnet-buildtools-prereqs-docker-main.json \
  | jq '[.repos[].images[].platforms[].simpleTags[]] | map(select(startswith("<distro>-<version>"))) | sort | .[]'
```

Examples:
```bash
# Check for Fedora 44 images
curl -sL https://github.com/dotnet/versions/raw/refs/heads/main/build-info/docker/image-info.dotnet-dotnet-buildtools-prereqs-docker-main.json \
  | jq '[.repos[].images[].platforms[].simpleTags[]] | map(select(startswith("fedora-44"))) | sort | .[]'

# Check for Ubuntu 26.04 images
curl -sL https://github.com/dotnet/versions/raw/refs/heads/main/build-info/docker/image-info.dotnet-dotnet-buildtools-prereqs-docker-main.json \
  | jq '[.repos[].images[].platforms[].simpleTags[]] | map(select(startswith("ubuntu-26.04"))) | sort | .[]'
```

If the image is **not found**, stop and inform the user. The image must be created first at [dotnet/dotnet-buildtools-prereqs-docker](https://github.com/dotnet/dotnet-buildtools-prereqs-docker). Check if an issue or PR already exists:

```bash
gh search issues "<distro> <version>" --repo dotnet/dotnet-buildtools-prereqs-docker --state open
```

If `gh` is not authenticated, use the GitHub API directly:

```bash
curl -s "https://api.github.com/search/issues?q=repo:dotnet/dotnet-buildtools-prereqs-docker+state:open+<distro>+<version>" \
  | jq '.items[] | {title, html_url}'
```

### 2. Check EOL dates

Look up the distro's lifecycle to confirm the version change makes sense:

```bash
curl -s https://endoflife.date/api/<distro-id>.json | jq '.[] | select(.cycle == "<version>") | {cycle, eol, releaseDate}'
```

The `<distro-id>` matches [endoflife.date](https://endoflife.date) product IDs (e.g. `fedora`, `alpine`, `debian`, `opensuse`, `ubuntu`, `centos-stream`).

### 3. Scan current references

Search for all current references to the distro being updated:

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

1. **Update `helix-platforms.yml`** — this is the primary file
   - Update the version comment (e.g. `# Latest: 43` → `# Latest: 44`)
   - Update the variable value — adjust the queue name and image tag to use the new version
   - Preserve the existing host queue (e.g. `AzureLinux.3.Amd64.Open`) — this does not change with distro version updates

2. **Update `helix-queues-setup.yml` files** — libraries, coreclr, and installer templates
   - Search for inline references to the old version and update them
   - These are direct queue strings, not variable references
   - Not all distros appear in all files — only update references that exist

3. **Version naming conventions** — follow existing patterns exactly:

   | Distro | Queue name pattern | Image tag pattern |
   |--------|--------------------|-------------------|
   | Alpine | `Alpine.<ver-no-dots>.Amd64.Open` | `alpine-<ver>-helix-amd64` |
   | Alpine (edge) | `Alpine.edge.Amd64.Open` | `alpine-edge-helix-amd64` |
   | CentOS Stream | `Centos.<ver>.Amd64.Open` | `centos-stream-<ver>-helix-amd64` |
   | Debian | `Debian.<ver>.Amd64.Open` | `debian-<ver>-helix-amd64` |
   | Fedora | `Fedora.<ver>.Amd64.Open` | `fedora-<ver>-helix-amd64` |
   | openSUSE | `openSUSE.<ver>.Amd64.Open` | `opensuse-<ver>-helix-amd64` |
   | Ubuntu | `Ubuntu.<ver-no-dots>.Amd64.Open` | `ubuntu-<ver>-helix-amd64` |

   AzureLinux (e.g. `AzureLinux.3.Amd64.Open`) appears as both a standalone VM queue and the primary host queue for container-based distros. It does not follow the container image pattern above.

   Architecture suffixes vary: `Amd64`, `Arm64`, `ArmArch`, `Arm32` for queue names; `amd64`, `arm64v8`, `arm32v7` for image tags.

   For ARM-based queues, host queues are often `Ubuntu.2204.ArmArch.Open`, but some queues (for example `helix_linux_arm64_oldest`) use AzureLinux-based host queues such as `AzureLinux.3.Arm64.Open`. Follow the existing pattern for the specific queue in `eng/pipelines/helix-platforms.yml` when updating versions.

### 5. Validate changes

After editing, verify:

1. **No stale references remain:**
   ```bash
   grep -rn "<old-version-pattern>" \
     eng/pipelines/helix-platforms.yml \
     eng/pipelines/libraries/helix-queues-setup.yml \
     eng/pipelines/coreclr/templates/helix-queues-setup.yml \
     eng/pipelines/installer/helix-queues-setup.yml \
     eng/pipelines/common/templates/pipeline-with-resources.yml \
     docs/workflow/using-docker.md
   ```
   Stale references to the old version are acceptable only if intentionally kept (e.g. the version is still used for `oldest`).

2. **All new references are syntactically consistent** — compare with adjacent entries in the same file to verify formatting.

3. **Variable names are unchanged** — only the `value` fields change, never the `name` fields.

### 6. Check other branches

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

Note: Release branch updates should be done in separate PRs.

### 7. Cross-reference with supported-os

Check if the relevant `supported-os.json` in dotnet/core needs corresponding updates. The file lives under the pattern `release-notes/<version>/supported-os.json` (for example, `release-notes/8.0/supported-os.json`) in the [release-notes directory](https://github.com/dotnet/core/tree/main/release-notes). If a new version is being added to Helix but isn't yet in supported-os, inform the user to run the `update-supported-os` skill in dotnet/core.

### 8. Create PR

1. Create a branch:
   ```bash
   git checkout -b update-helix-<distro>-<version>
   ```

2. Commit changes:
   ```bash
   git add eng/pipelines/
   git commit -m "Update <distro> Helix queues to <version>"
   ```

3. The PR description should include:
   - Table of changes (old version → new version, which slots)
   - EOL dates for old and new versions
   - Confirmation that container images are available
   - Link to the [os-onboarding guide](https://github.com/dotnet/runtime/blob/main/docs/project/os-onboarding.md)
   - Link to tracking issue if applicable (e.g. [dotnet/core#9638](https://github.com/dotnet/core/issues/9638))

> 📝 **AI-generated content disclosure:** When posting any content to GitHub (PR descriptions, comments) under a user's credentials — i.e., the account is **not** a dedicated "copilot" or "bot" account/app — you **MUST** include a concise, visible note (e.g. a `> [!NOTE]` alert) indicating the content was AI/Copilot-generated. Skip this if the user explicitly asks you to omit it.

## Audit mode

When asked to audit all OS coverage:

1. Fetch the current supported-os.json for the target .NET version:
   ```bash
   curl -sL https://github.com/dotnet/core/raw/refs/heads/main/release-notes/<version>/supported-os.json
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

- [OS onboarding guide](/docs/project/os-onboarding.md)
- [.NET OS Support Tracking](https://github.com/dotnet/core/issues/9638)
- [Prereq container image lifecycle](https://github.com/dotnet/dotnet-buildtools-prereqs-docker/blob/main/lifecycle.md)
- [Container image registry (image-info)](https://github.com/dotnet/versions/blob/main/build-info/docker/image-info.dotnet-dotnet-buildtools-prereqs-docker-main.json)
- [endoflife.date](https://endoflife.date/) for OS lifecycle data
- [PR #125991](https://github.com/dotnet/runtime/pull/125991) — example EOL OS version replacement
- [PR #111768](https://github.com/dotnet/runtime/pull/111768) — example new OS version onboarding
