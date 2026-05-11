# Build-IssueComment.ps1
# Reads a breaking change issue draft markdown file, URL-encodes the title,
# body, and labels, and produces a brief PR comment markdown file containing:
#   - A short header with instructions
#   - @mentions of the PR assignees so they get notified
#   - A clickable link that pre-fills a new issue in dotnet/docs
#   - An email reminder
#
# Usage:
#   pwsh .github/skills/breaking-change-doc/Build-IssueComment.ps1 `
#       -IssueDraftPath issue-draft.md `
#       -Title "[Breaking change]: Something changed" `
#       -Assignees "@user1 @user2" `
#       -OutputPath pr-comment.md
#
# The issue draft file should contain only the issue body markdown (no title).

param(
    [Parameter(Mandatory = $true)]
    [string]$IssueDraftPath,

    [Parameter(Mandatory = $true)]
    [string]$Title,

    [string]$Assignees = "",

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
$encodedEmailSubject = [Uri]::EscapeDataString("[Breaking Change] $Title")

$issueUrl = "https://github.com/$DocsRepo/issues/new?title=$encodedTitle&body=$encodedBody&labels=$encodedLabels"
$notificationEmailUrl = "mailto:dotnetbcn@microsoft.com?subject=$encodedEmailSubject"

# Build a brief assignee mention line (if any)
$mentionLine = ""
$trimmedAssignees = ""
if (-not [string]::IsNullOrWhiteSpace($Assignees)) {
    $trimmedAssignees = $Assignees.Trim()
    $mentionLine = "`n`n/cc $trimmedAssignees"
}

$comment = @"
## Breaking Change Documentation

A breaking change draft has been prepared for this PR.

:point_right: **[Click here to create the issue in $DocsRepo]($issueUrl)**

After creating the issue, please email a link to it to
[.NET Breaking Change Notifications]($notificationEmailUrl).$mentionLine

> [!NOTE]
> This documentation was generated with AI assistance from Copilot.
"@

# GitHub comment body limit is 65536 characters. The comment is now brief,
# but the URL itself can be very long. Warn if the comment exceeds the
# configured comment-length threshold; the URL length is checked separately below.
$maxCommentLength = 65000
if ($comment.Length -gt $maxCommentLength) {
    Write-Warning "Comment body ($($comment.Length) chars) exceeds GitHub limit."
}

$comment | Out-File -FilePath $OutputPath -Encoding UTF8 -NoNewline

Write-Host "Wrote PR comment to $OutputPath ($($comment.Length) characters)"
Write-Host "Issue URL length: $($issueUrl.Length) characters"
if ($issueUrl.Length -gt 8192) {
    Write-Warning "URL exceeds 8192 characters. Some browsers may truncate it. Consider shortening the issue body."
}
