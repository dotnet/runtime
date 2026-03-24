# Build-IssueComment.ps1
# Reads a breaking change issue draft markdown file, URL-encodes the title,
# body, and labels, and produces a PR comment markdown file containing:
#   - A header
#   - The full draft for inline review
#   - A clickable link that pre-fills a new issue in dotnet/docs
#   - An email reminder
#
# Usage:
#   pwsh .github/skills/breaking-change-doc/Build-IssueComment.ps1 `
#       -IssueDraftPath issue-draft.md `
#       -Title "[Breaking change]: Something changed" `
#       -OutputPath pr-comment.md
#
# The issue draft file should contain only the issue body markdown (no title).

param(
    [Parameter(Mandatory = $true)]
    [string]$IssueDraftPath,

    [Parameter(Mandatory = $true)]
    [string]$Title,

    [string]$OutputPath = "pr-comment.md",

    [string]$Labels = "breaking-change,Pri1,doc-idea",

    [string]$DocsRepo = "dotnet/docs"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $IssueDraftPath)) {
    Write-Error "Issue draft file not found: $IssueDraftPath"
    exit 1
}

$issueBody = Get-Content -Path $IssueDraftPath -Raw -Encoding UTF8

# URL-encode using .NET Uri class (same technique the old script used)
$encodedTitle = [Uri]::EscapeDataString($Title)
$encodedBody = [Uri]::EscapeDataString($issueBody)
$encodedLabels = [Uri]::EscapeDataString($Labels)

$issueUrl = "https://github.com/$DocsRepo/issues/new?title=$encodedTitle&body=$encodedBody&labels=$encodedLabels"

$comment = @"
## Breaking Change Documentation

$issueBody

---

> [!NOTE]
> This documentation was generated with AI assistance from Copilot.

:point_right: **[Click here to create the issue in dotnet/docs]($issueUrl)**

After creating the issue, please email a link to it to
[.NET Breaking Change Notifications](mailto:dotnetbcn@microsoft.com).
"@

$comment | Out-File -FilePath $OutputPath -Encoding UTF8 -NoNewline

Write-Host "Wrote PR comment to $OutputPath"
Write-Host "Issue URL length: $($issueUrl.Length) characters"
if ($issueUrl.Length -gt 8192) {
    Write-Warning "URL exceeds 8192 characters. Some browsers may truncate it. Consider shortening the issue body."
}
