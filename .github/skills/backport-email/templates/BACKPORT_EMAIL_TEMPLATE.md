# Backport Email Template

Use this template when emailing Tactics to request approval for a backport to a release branch.
The body of the email should be the PR description verbatim (based on the `pr_description_template` in `.github/workflows/backport.yml`).

---

**Subject:** [release/X.0] Backport request: <BRIEF_DESCRIPTION> (#<PR_NUMBER>)

---

Hello Tactics,

Please consider https://github.com/dotnet/runtime/pull/<PR_NUMBER> for backporting into release/X.0.

Fixes https://github.com/dotnet/runtime/issues/<ISSUE_NUMBER>

main PR: <MAIN_PR_LINK>

<Copy the PR description verbatim>
