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
   - PR description (filled in from the `pr_description_template` in `.github/workflows/backport.yml`)

2. **Read the email template** at `.github/skills/backport-email/templates/BACKPORT_EMAIL_TEMPLATE.md`

3. **Generate the email** by:
   - Filling in the subject line with the release branch, PR title, and PR number
   - Extracting the issue link and main PR link from the PR description
   - Copying the PR description verbatim as the email body

## Output Format

Output the email as **plain text** (not markdown) since email clients don't render markdown.

```
Subject: [release/X.0] Backport request: <TITLE> (#<PR_NUMBER>)

Hello Tactics,

Please consider https://github.com/dotnet/runtime/pull/<PR_NUMBER> for backporting into release/X.0.

Fixes https://github.com/dotnet/runtime/issues/<ISSUE_NUMBER>

main PR: <MAIN_PR_LINK>

<PR description verbatim>
```

## Important Notes

- **Do NOT attempt to open the email in Outlook or any email client.** Just output the formatted text for the user to copy.
- Copy the PR description verbatim — do not rewrite or restructure it.
- Extract the release branch version (e.g., `9.0`) from the PR's base branch.
- If any section is missing from the PR description, note it and leave a placeholder.
