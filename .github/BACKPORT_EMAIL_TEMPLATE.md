# Backport Email Template

Use this template when emailing Tactics to request approval for a backport to a release branch.

> **Important:** The email content should come directly from your backport PR description
> (see `servicing_pull_request_template.md`). Do not write different text for the email —
> copy the sections verbatim from the PR to ensure consistency.

> **Note:** Most email clients (Outlook, Gmail, etc.) don't render Markdown. The section
> headers use `**bold**` syntax which appears with asterisks in plain-text emails.

---

**Subject:** [release/X.0] Backport request: <BRIEF_DESCRIPTION> (#<PR_NUMBER>)

---

Hello Tactics,

Please consider https://github.com/dotnet/runtime/pull/<PR_NUMBER> for backporting into release/X.0.

Fixes https://github.com/dotnet/runtime/issues/<ISSUE_NUMBER>

main PR: <MAIN_PR_LINK>

<!-- Copy the following sections verbatim from your backport PR description -->

**DESCRIPTION**

<Copy from PR: Description section>

**CUSTOMER IMPACT**

- [ ] Customer reported
- [ ] Found internally

<Copy from PR: Customer Impact section>

**REGRESSION**

- [ ] Yes
- [ ] No

<Copy from PR: Regression section>

**TESTING**

<Copy from PR: Testing section>

**RISK**

<Copy from PR: Risk section>
