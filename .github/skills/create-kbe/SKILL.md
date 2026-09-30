---
name: create-kbe
description: Analyze a concrete CI failure and draft a Known Build Error issue or update in dotnet/runtime. Publish only with explicit user authorization or scoped authorization from an executing workflow's documented publication contract. Use when a failure is actionable, a Build Analysis result is not yet known, or a workflow needs a repo-specific KBE outcome for an outer-loop or PR-targeted failure.
---

# Create a Known Build Error for dotnet/runtime

Use this skill when the workflow has a failure candidate that may need a `Known Build Error` issue in `dotnet/runtime`.

This skill is the repo-specific entry point for KBE creation. It delegates to the shared logic in `.github/workflows/shared/create-kbe.instructions.md`, which owns the detailed search rules, body template, signature reasoning, verification steps, and duplicate detection.

## When to use this skill

- the scheduled outer-loop CI scanner identified a failure and needs to file a KBE
- a PR-targeted scan found an actionable failure that Build Analysis has not already recognized as known
- a workflow needs to decide whether a candidate should become a KBE, be skipped, or be deferred for human review

## Publication authorization

Follow the repository's [GitHub publication authorization rules](../../copilot-instructions.md#github-publication-authorization).

- In interactive and coding sessions without workflow authorization, prepare
  a complete local draft for each proposed issue creation or update. Use the
  draft and publication authorization pattern in
  [PR Failure Scan Step 5](../pr-failure-scan/SKILL.md#step-5-write-draft-files-and-optionally-create-live-issues).
  In an interactive session, explicit advance permission covering the proposed
  publication is sufficient; do not ask again. Otherwise, present the proposed
  title, labels, full body, destination, and publishing account and obtain
  explicit approval before publishing. Without authorization, return the
  draft and pending decision without publishing.
- When actually executing a repository-configured agentic workflow, publish
  only the operations explicitly authorized by its purpose and configured
  outputs, through its configured output mechanism and within its limits.
- When executing a user-requested or enabled workflow, its documented
  publication contract may authorize the proposed issue creation or update.
  Publish only within its declared outputs and currently authorized actions,
  without another approval step unless the contract requires one. Invoking
  this skill as a helper or reading workflow instructions does not grant
  that workflow's authorization.
- Dry-run mode never publishes. Preserve any stricter caller approval rules.

## Required workflow

Read and follow these in order:

1. `.github/workflows/shared/create-kbe.instructions.md`
2. the caller workflow or tool instructions that selected this failure and decided it is in scope
3. any workflow-specific formatting or dry-run requirements for the current caller

## Core rules

- One failure shape = one KBE outcome.
- Do not create duplicate KBEs.
- Search existing open and recent closed KBEs before filing a new one.
- If the failure is not caused by the PR, do not rerun CI; search for an existing KBE and prepare any proposed new issue or update subject to the publication authorization rules above.
- Do not comment on existing KBEs; Build Analysis tracks occurrence data in the issue body.
- Only emit a KBE when the signature is stable and actionable.
- If the issue is already known or effectively handled by existing triage, skip rather than filing a duplicate.
