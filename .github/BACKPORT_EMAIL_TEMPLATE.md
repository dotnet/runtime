# Backport Email Template

Use this template when emailing Tactics to request approval for a backport to a release branch.

> **Note:** Most email clients (Outlook, Gmail, etc.) don't render Markdown. Copy the template below and the section headers will display as bold text. If your email client supports rich text, you can manually increase the header font size.

---

**Subject:** [release/X.0] Backport request: <Brief description> (#<PR number>)

---

Hello Tactics,

Please consider https://github.com/dotnet/runtime/pull/<PR_NUMBER> for backporting into release/X.0.

**CUSTOMER IMPACT**

- [ ] Customer reported
- [ ] Found internally

<Describe the impact to customers. What functionality is broken? What scenarios are affected?>

Fixes https://github.com/dotnet/runtime/issues/<ISSUE_NUMBER>

**REGRESSION**

- [ ] Yes
- [ ] No

<If yes, specify when the regression was introduced. Provide the PR or commit if known.>

**TESTING**

<How was the fix verified? How was the issue missed previously? What tests were added?>

**RISK**

<High/Medium/Low>

<Justify the indication by mentioning how risks were measured and addressed.>
