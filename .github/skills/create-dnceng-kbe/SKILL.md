---
name: create-dnceng-kbe
description: >
  Draft or publish a caller-requested dotnet/dnceng Known Build Error outcome.
  The caller chooses the action; the skill reuses supplied lookup evidence or
  completes missing dnceng lookup context without changing that action.
---

# Draft a dotnet/dnceng Infrastructure Known Build Error

Use this skill after a caller has completed dnceng ownership and duplicate
triage and explicitly selected a draft or publication action.

This skill executes the selected action. It does not decide whether a dnceng KBE
is warranted or whether the result should be draft-only or live.

## Authoritative guidance

Read the current versions of these sources before drafting:

1. [dnceng infrastructure Known Issue form](https://github.com/dotnet/dnceng/blob/main/.github/ISSUE_TEMPLATE/z-build-break-infrastructure-issue-template.yml)
2. [Arcade Known Issues guidance](https://github.com/dotnet/arcade/blob/main/Documentation/Build%20Analysis/KnownIssues.md)
3. [Arcade Known Issue JSON steps](https://github.com/dotnet/arcade/blob/main/Documentation/Build%20Analysis/KnownIssueJsonStepByStep.md)
4. [dnceng issue tracking guidance](https://github.com/dotnet/dnceng/blob/main/Documentation/TeamProcess/DevGuide/IssueTrackingGuidance.md)

Do not copy or recreate the dnceng issue template in this repository. Load its
current field order, labels, default content, and validation requirements. If
the authoritative form or guidance cannot be read, stop and report the blocked
draft instead of inventing a substitute. Record the resolved repository commit
SHA for each external source in the local draft so the artifact is reproducible.

## Input contract

The caller supplies:

- one concrete candidate already classified for a dnceng draft,
- a stable candidate ID, failure name or family, and build leg,
- the originating repository and PR,
- the failing build, leg, pipeline, queue, logs, and stable signature evidence,
- the Build Analysis infrastructure issue link when available,
- the requested action:
  `draft-review`, `draft-new-dnceng`, `draft-recurrence-dnceng`,
  `publish-new-dnceng`, or `publish-recurrence-dnceng`,
- dnceng duplicate-search and recurrence evidence when available,
- lookup mode: `reuse-supplied` or `complete-if-needed`,
- why repository ownership was rejected or remains uncertain,
- any missing evidence for a review-only draft,
- for publication actions: `caller=pr-failure-scan`, live mode, the prepared
  draft path and fingerprint, explicit user permission to publish to
  `dotnet/dnceng`, and the authenticated GitHub account.

Treat query parameters in a Build Analysis link as hints. Verify the destination
repository, issue-form name, labels, build, leg, and PR against the selected
check and authoritative form.

The requested action is mandatory. Reject contradictory action inputs. Do not
select a different action.

## Publication boundary

- Publication is allowed only for `publish-new-dnceng` or
  `publish-recurrence-dnceng` requested by a live `pr-failure-scan` run.
- Explicit user permission for publication to `dotnet/dnceng` is mandatory.
  Runtime or workflow publication authorization does not transfer.
- `dryrun`, `draft-review`, and caller-selected draft-only outcomes never
  publish and never trigger a permission request.
- Reject publication when the prepared draft, fingerprint, authenticated
  account, explicit permission, or caller identity is missing.

## Execute the requested action

1. Before using any candidate signature, error text, logs, or other supplied
   evidence in an external lookup, stop when the candidate contains potential
   security-sensitive material. Do not create any GitHub issue, AzDO work item,
   comment, draft, lookup query, or other external artifact.
2. Verify the requested action and apply the requested lookup mode:
   - With `reuse-supplied`, require a conclusive dnceng lookup.
   - With `complete-if-needed`, reuse conclusive supplied evidence; otherwise
     run the dnceng duplicate and recurrence lookup before drafting.
   - A direct user request with an explicit action and no lookup context uses
     `complete-if-needed`.
   The lookup may return an existing issue or block an unsafe duplicate, but it
   must not select a different draft action.
3. Load the authoritative external form and guidance. Verify that the requested
   action and supplied evidence are compatible with the current form.
4. Write one local Markdown review artifact. Prefer the session
   artifact directory; otherwise use a temporary directory outside the runtime
   checkout.
5. Render the current dnceng issue-form fields in their authoritative order and
   fill them with verified evidence. Use Arcade's current Known Issue guidance
   for the JSON signature. Preserve form-owned default content that maintainers
   are expected to retain. If a form-owned link is broken, record that fact and
   use the verified current guidance for analysis rather than silently
   rewriting the external template.
6. Include the proposed title, current form labels, destination repository,
   issue-form URL, prefilled Build Analysis link when available, and any
   ownership uncertainty requiring maintainer review.
7. Clearly mark `draft-review` as incomplete and non-publishable. Include
   recurrence evidence for `draft-recurrence-dnceng`.
8. For `publish-new-dnceng` or `publish-recurrence-dnceng`, verify the matching
   prepared draft and fingerprint, perform a final narrow dnceng duplicate
   safety check, verify the publication boundary above, append the required AI
   disclosure, and publish exactly that draft.
9. Report both a clickable local-file link when supported and the plain
   absolute path for drafts. Return the created issue URL for publication.
10. Never change the caller-selected action.

## Outcome contract

```json
{
  "status": "created | drafted | existing | blocked | failed",
  "requestedAction": "draft-review | draft-new-dnceng | draft-recurrence-dnceng | publish-new-dnceng | publish-recurrence-dnceng",
  "lookupMode": "reuse-supplied | complete-if-needed",
  "candidateId": "stable caller-provided identifier",
  "failure": "failure name or family",
  "buildLeg": "failing build leg",
  "signature": "concise stable signature summary",
  "proposedTitle": "draft title, or empty string",
  "issueUrl": "verified existing dotnet/dnceng issue URL, or null",
  "draftPath": "absolute local path or null",
  "missingEvidence": ["ownership or verification items still required"],
  "message": "concise evidence-based result"
}
```
