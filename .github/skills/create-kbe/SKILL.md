---
name: create-kbe
description: Create or update a Known Build Error issue in dotnet/runtime from a concrete CI failure. Use when a failure is actionable, a Build Analysis result is not yet known, or a workflow needs a repo-specific KBE issue for an outer-loop or PR-targeted failure.
---

# Create a Known Build Error for dotnet/runtime

Use this skill when the workflow has a failure candidate that may need a `Known Build Error` issue in `dotnet/runtime`.

This skill is the repo-specific entry point for KBE creation. It delegates to the shared logic in `.github/workflows/shared/create-kbe.instructions.md`, which owns the detailed search rules, body template, signature reasoning, verification steps, and duplicate detection.

## When to use this skill

- the scheduled outer-loop CI scanner identified a failure and needs to file a KBE
- a PR-targeted scan found an actionable failure that Build Analysis has not already recognized as known
- a workflow needs to decide whether a candidate should become a KBE, be skipped, or be deferred for human review

## Required workflow

Read and follow these in order:

1. `.github/workflows/shared/create-kbe.instructions.md`
2. the caller workflow or tool instructions that selected this failure and decided it is in scope
3. any workflow-specific formatting or dry-run requirements for the current caller

## Core rules

- One failure shape = one KBE outcome.
- Do not create duplicate KBEs.
- Search existing open and recent closed KBEs before filing a new one.
- If the failure is not caused by the PR, do not rerun CI; file or update a `Known Build Error` instead.
- Do not comment on existing KBEs; Build Analysis tracks occurrence data in the issue body.
- Only emit a KBE when the signature is stable and actionable.
- If the issue is already known or effectively handled by existing triage, skip rather than filing a duplicate.
