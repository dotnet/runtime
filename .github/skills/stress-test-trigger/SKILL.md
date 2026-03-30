---
name: stress-test-trigger
description: Automatically trigger HTTP and/or SSL stress test pipelines when a PR modifies stress-test-related files. Posts `/azp run` comments on the PR to queue the appropriate pipelines.
---

# Stress Test Pipeline Trigger

Automatically detect which stress test pipelines need to run based on the files changed in a pull request, and post `/azp run` comments to trigger them.

> 📝 **AI-generated content disclosure:** When posting any content to GitHub under a user's credentials — i.e., the account is **not** a dedicated "copilot" or "bot" account/app — you **MUST** include a concise, visible note (e.g. a `> [!NOTE]` alert) indicating the content was AI/Copilot-generated. Skip this if the user explicitly asks you to omit it.

## When to Use This Skill

This skill is triggered automatically by the `stress-test-trigger` agentic workflow when a PR is opened or updated with changes to HTTP or SSL stress-test-related files.

## Pipeline Names

| Area | Azure Pipelines command |
|------|------------------------|
| HTTP Stress | `/azp run runtime-libraries stress-http` |
| SSL Stress | `/azp run runtime-libraries stress-ssl` |

## File-to-Pipeline Mapping

### HTTP Stress Pipeline (`runtime-libraries stress-http`)

Trigger when **any** of these paths are modified:

- `src/libraries/System.Net.Http/tests/StressTests/HttpStress/**`
- `eng/pipelines/libraries/stress/http.yml`

### SSL Stress Pipeline (`runtime-libraries stress-ssl`)

Trigger when **any** of these paths are modified:

- `src/libraries/System.Net.Security/tests/StressTests/SslStress/**`
- `eng/pipelines/libraries/stress/ssl.yml`

### Both Pipelines

Trigger **both** pipelines when shared infrastructure files are modified:

- `src/libraries/Common/tests/System/Net/StressTests/**`
- `eng/pipelines/libraries/stress/` (files other than `http.yml` / `ssl.yml`, e.g. shared templates)

## Procedure

1. **Get the list of changed files** in the PR using the GitHub API.
2. **Classify each changed file** against the mapping above to determine which pipelines to trigger.
3. **Post one comment per pipeline** on the PR with the `/azp run` command.
   - If only HTTP stress files changed → post `/azp run runtime-libraries stress-http`
   - If only SSL stress files changed → post `/azp run runtime-libraries stress-ssl`
   - If shared files changed, or both HTTP and SSL files changed → post both commands
4. **Do not post duplicate commands.** Before posting, check the PR comments to see if a `/azp run` command for the same pipeline was already posted on the current head commit. If so, skip it.

## Output Format

Post each `/azp run` command as a **separate** PR comment so Azure Pipelines processes them independently. Each comment should contain only the `/azp run` command on its own line (no additional text), for example:

```
/azp run runtime-libraries stress-http
```
