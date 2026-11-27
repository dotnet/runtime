# Output Format

The triage report uses these markers for status indicators:

| Marker | Meaning |
|--------|---------|
| `[ok]` | Positive result -- check passed, item confirmed |
| `[x]` | Negative result -- check did not pass (e.g., could not reproduce, not a regression) |
| `[!]` | Warning or action needed -- something requires attention |
| `[i]` | Informational -- neutral context, no action needed |

## Report Template

```markdown
# Triage Report: #{issue_number}

**Issue:** {title}
**Author:** @{author} | **Created:** {date} | **Last Activity:** {date}
**Current Labels:** {labels}
**Type:** {Bug | API Proposal | Enhancement | Performance | Question | Documentation | Other}

---

## Safety Concerns {only if Step 0b flagged issues; omit entirely if clean}

{List each concern with specifics, e.g.:}
- [!] **Suspicious reproduction code** -- repro calls `HttpClient` to fetch from `http://{suspicious-domain}`. Reproduction skipped.
- [!] **Binary attachment** -- issue includes a `.zip` file. Not downloaded; requested inline repro instead.

## Label Check

{One of:}
- [ok] **Label is correct.** The `{area-label}` label correctly maps to this issue's subject.
- [!] **Mislabeled.** This issue concerns {subject}, which maps to `{correct-area-label}` (not `{current-label}`). Reason: {explanation referencing area-owners.md}.
- [!] **Wrong repo.** This issue belongs in `{correct-repo}`. Reason: {explanation}.
- [!] **Transfer to dotnet/dotnet-api-docs.** This is an API documentation issue.
- [!] **Transfer to dotnet/docs.** This is a conceptual documentation issue.
- [!] **Convert to Discussion.** This is a question/support request, not a bug or feature request.
- [!] **Missing area label.** Suggested: `{area-label}`. Reason: {explanation}.
- [x] **Off-topic / Spam.** {explanation}.

## Duplicate Search

{One of:}
- [ok] **No duplicates found.**
- [!] **Potential duplicate(s) found:**
  - #{number} -- {title} ({state}) -- {why it's related}
  - #{number} -- {title} ({state}) -- {why it's related}
- [i] **Related issues (not duplicates):**
  - #{number} -- {title} -- {how it relates}

## Prior Art & Ecosystem

{Brief summary of ecosystem research: existing .NET packages, how other
languages handle it, relevant prior discussions. 1-3 paragraphs max.}

## Reproduction {only for bug reports}

{One of:}
- [ok] **Reproduced** on .NET {version}, {OS} {arch}. {Details.}
- [x] **Could not reproduce** on .NET {version}, {OS} {arch}. {Details.}
- [!] **Unable to verify** -- {reason, e.g., "macOS-only issue, current env is Windows"}.
- [i] **No repro steps provided.**

**Regression check:**
{One of:}
- [ok] **Confirmed regression** from .NET {old} → .NET {new}.
- [x] **Not a regression** -- also fails on .NET {old}.
- [!] **Unable to verify regression** -- {reason}.
- [i] **Not claimed as regression.**

**Minimal reproduction:** {only if a minimal repro was derived; omit if the
issue already provided one or if reproduction was not attempted}

{Self-contained code block that can be copy-pasted into a `dotnet new console`
project. Must include all types, input data inline, and expected vs. actual
output in comments.}

## Root Cause Analysis {only for reproduced bug reports; omit if not attempted}

**Likely mechanism:** {1-3 sentence hypothesis of what goes wrong}
**Related code changes:** {link to relevant commit if found, or "N/A"}

## Answer {only for questions/support requests}

{Provide a helpful answer to the question, with code examples where appropriate.}

**Answer verified:** {Yes -- tested in temp project | No -- based on documentation/inference}

## Assessment

{2-4 bullet points covering feasibility, impact, community interest, risks.}

**Suggested Priority:** {High | Normal | Low} {only for KEEP recommendations}
**Estimated Complexity:** {S | M | L | XL} -- {1-sentence rationale} {only for API proposals and enhancements}

## Label Recommendations

Recommend the **complete set of labels** that should be applied to the issue
after triage. This includes:
- The `area-*` label (confirm the existing one or recommend a change)
- The `untriaged` label should be **removed** (triage is complete)
- A primary type label if missing (`bug`, `api-suggestion`, `enhancement`, etc.)
- Any applicable supplementary labels from
  [supplementary-labels.md](supplementary-labels.md):
  tenet labels, runtime/technology labels, qualifier labels, workflow labels
  (e.g., `needs-author-action` for NEEDS INFO outcomes), and test labels
- For **NEEDS INFO** outcomes, always recommend adding `needs-author-action`
  to trigger the auto-close workflow if the author does not respond

List every label action needed:

- + **Add:** `{label}` -- {reason}
- - **Remove:** `{label}` -- {reason}
- [ok] **Keep:** `{label}` -- {reason, if non-obvious}

## Recommendation: **{KEEP | CLOSE | NEEDS INFO}**

**Confidence:** {High | Medium | Low} -- {1-sentence rationale, e.g., "Bug reproduced locally on .NET 10" or "Could not verify due to environment mismatch"}
**Reason:** {1-2 sentence summary of the recommendation.}

**Suggested response:**

```markdown
{Draft a response appropriate for the recommendation. Use markdown formatting
suitable for a GitHub comment. See formatting instructions below.}
```
```

## Formatting Rules for Suggested and Finalized Responses

The suggested response (and any finalized response produced later) MUST be
rendered inside a **fenced code block** (triple backticks with the `markdown`
language tag) so it displays as literal code in the terminal. This is critical
because:

- Blockquote (`>`) formatting gets rendered as styled text and cannot be copied.
- Plain markdown gets word-wrapped by the terminal, inserting spurious line
  breaks that corrupt the text when pasted.
- A fenced code block preserves the text exactly as written and renders it
  as code, making it safe to copy-paste into a GitHub comment.

Example of the correct format:

````
**Suggested response:**

```markdown
Thanks for filing this, @user. This is a duplicate of #12345...

Closing as duplicate of #12345.
```
````

## Conditional Sections

Not every section appears in every report. Use this guide:

| Section | When to include |
|---------|----------------|
| Safety Concerns | Only if Step 0b flagged issues |
| Reproduction | Only for bug reports |
| Regression check | Only for bug reports |
| Minimal reproduction | Only if a minimal repro was derived and the recommendation is KEEP |
| Root Cause Analysis | Only for reproduced bug reports |
| Answer | Only for questions/support requests |
| Suggested Priority | Only for KEEP recommendations |
| Estimated Complexity | Only for API proposals and enhancements |
