---
name: backport-email
description: Generate a backport email from a PR for sending to Tactics. Use when asked to create or generate a backport email.
---

# Backport Email Generation

Generate a formatted email for requesting backport approval from Tactics.

## Required Input

Ask the user for the **backport PR URL** if not provided. Example:
- `https://github.com/dotnet/runtime/pull/124058`

## Process

1. **Fetch the backport PR** using the GitHub MCP tools to get:
   - PR number and title
   - Target release branch (e.g., `release/9.0`)
   - PR description containing the servicing template sections

2. **Extract from the PR description:**
   - Link to the original main PR
   - Link to the issue being fixed
   - DESCRIPTION section
   - CUSTOMER IMPACT section (including checkboxes)
   - REGRESSION section (including checkboxes)
   - TESTING section
   - RISK section

3. **Generate the email** following the template at `.github/BACKPORT_EMAIL_TEMPLATE.md`

## Output Format

Output the email as **plain text** (not markdown) since email clients don't render markdown.

```
Subject: [release/X.0] Backport request: <TITLE> (#<PR_NUMBER>)

Hello Tactics,

Please consider https://github.com/dotnet/runtime/pull/<PR_NUMBER> for backporting into release/X.0.

Fixes https://github.com/dotnet/runtime/issues/<ISSUE_NUMBER>

main PR: <MAIN_PR_LINK>

**DESCRIPTION**

<description text>

**CUSTOMER IMPACT**

- [ ] Customer reported
- [ ] Found internally

<customer impact text>

**REGRESSION**

- [ ] Yes
- [ ] No

<regression text>

**TESTING**

<testing text>

**RISK**

<risk text>
```

## Important Notes

- **Do NOT attempt to open the email in Outlook or any email client.** Just output the formatted text for the user to copy.
- Preserve the `**bold**` section headers as they appear reasonably well in plain-text emails.
- Preserve the checkbox format `- [ ]` or `- [x]` from the PR description.
- Extract the release branch version (e.g., `9.0`) from the PR's base branch.
- If any section is missing from the PR description, note it and leave a placeholder.
