# Build-IssueComment.ps1
# Reads a breaking change issue draft markdown file and produces a PR comment
# markdown file that links to a pre-filled "new issue" page in dotnet/docs.
#
# Hybrid link strategy:
#   * The link always targets `issues/new` directly (NOT `?template=`), which opens
#     a blank markdown editor rather than the structured issue form. Short fields
#     (title, labels, assignees) are always pre-filled from the query string.
#   * If the full URL -- including the URL-encoded body -- stays under the GitHub
#     request-line limit (~8192 bytes; we use a conservative 8000-byte budget on the
#     full URL string), the body is pre-filled too, so the issue is one click away.
#   * Otherwise the body is dropped from the URL and included in the comment inside a
#     <details> block for the user to copy-paste into the opened (empty) editor.
#
# Labels and assignees are supplied by the caller and should come from the
# dotnet/docs breaking-change issue template (.github/ISSUE_TEMPLATE/02-breaking-change.yml).
# The runtime PR's assignees are passed separately as -CcMentions
# and only used for /cc notifications.
#
# Usage:
#   pwsh .github/skills/breaking-change-doc/Build-IssueComment.ps1 `
#       -IssueDraftPath issue-draft.md `
#       -Title "[Breaking change]: Something changed" `
#       -Labels "breaking-change" `
#       -IssueAssignees "gewarren" `
#       -CcMentions "@user1 @user2" `
#       -OutputPath pr-comment.md
#
# The issue draft file should contain only the issue body markdown (no title).

param(
    [Parameter(Mandatory = $true)]
    [string]$IssueDraftPath,

    [Parameter(Mandatory = $true)]
    [string]$Title,

    # Comma-separated labels, from the docs issue template's `labels:` field.
    [string]$Labels = "",

    # Comma-separated GitHub usernames for the issue `assignees=` prefill,
    # from the docs issue template's `assignees:` field.
    [string]$IssueAssignees = "",

    # Space-separated @mentions (the runtime PR's assignees) for the /cc line.
    [string]$CcMentions = "",

    [string]$OutputPath = "pr-comment.md",

    [string]$DocsRepo = "dotnet/docs",

    # Conservative budget on the full URL string length in bytes. GitHub's front end
    # rejects a request line over ~8192 bytes with HTTP 414; 8000 leaves headroom.
    [int]$MaxUrlBytes = 8000
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $IssueDraftPath)) {
    Write-Error "Issue draft file not found: $IssueDraftPath"
    exit 1
}

$issueBody = Get-Content -Path $IssueDraftPath -Raw -Encoding UTF8

# Build the query string for the short (always pre-filled) fields.
# [Uri]::EscapeDataString produces ASCII-only output, so the resulting URL string's
# character count equals its UTF-8 byte count.
$queryParts = @("title=$([Uri]::EscapeDataString($Title))")

if (-not [string]::IsNullOrWhiteSpace($Labels)) {
    $queryParts += "labels=$([Uri]::EscapeDataString($Labels.Trim()))"
}

if (-not [string]::IsNullOrWhiteSpace($IssueAssignees)) {
    # GitHub expects a comma-separated list for the assignees prefill.
    $assigneesValue = ($IssueAssignees.Trim() -replace '\s*,\s*', ',')
    $queryParts += "assignees=$([Uri]::EscapeDataString($assigneesValue))"
}

$baseUrl = "https://github.com/$DocsRepo/issues/new?" + ($queryParts -join '&')

# Try to also pre-fill the body if the full URL fits within the budget.
$encodedBody = [Uri]::EscapeDataString($issueBody)
$fullUrl = "$baseUrl&body=$encodedBody"
$bodyInUrl = $fullUrl.Length -le $MaxUrlBytes

$issueUrl = if ($bodyInUrl) { $fullUrl } else { $baseUrl }

# Build the /cc mention line (from the runtime PR's assignees).
$mentionLine = ""
if (-not [string]::IsNullOrWhiteSpace($CcMentions)) {
    $mentionLine = "`n`n/cc $($CcMentions.Trim())"
}

# When the body isn't pre-filled, include it in the comment for copy-paste.
# The body is wrapped in a fenced code block so the raw markdown can be copied
# verbatim. A four-backtick fence is used because the body itself contains
# three-backtick code fences. (Built from a single-quoted variable so PowerShell's
# backtick escaping doesn't collapse it.)
$fence = '````'
$copyPasteSection = ""
if (-not $bodyInUrl) {
    $copyPasteSection = @"


The issue body is too long to pre-fill in the link above, so the link opens a
**blank issue** with only the title, labels, and assignees set. Copy the body
below into the description field before submitting.

<details>
<summary>Issue body (click to expand, then copy)</summary>

$($fence)md
$issueBody
$fence

</details>
"@
}

$comment = @"
## Breaking Change Documentation

A breaking change draft has been prepared for this PR.

:point_right: **[Click here to create the issue in $DocsRepo]($issueUrl)**$copyPasteSection

After creating the issue, please email a link to it to the
.NET Breaking Change Notifications alias (dotnetbcn@microsoft.com).$mentionLine

> [!NOTE]
> This documentation was generated with AI assistance from Copilot.
"@

# GitHub comment body limit is 65536 characters.
$maxCommentLength = 65000
if ($comment.Length -gt $maxCommentLength) {
    Write-Warning "Comment body ($($comment.Length) chars) exceeds GitHub's comment length limit. The issue body may be too large to embed."
}

$comment | Out-File -FilePath $OutputPath -Encoding UTF8 -NoNewline

Write-Host "Wrote PR comment to $OutputPath ($($comment.Length) characters)"
Write-Host "Body pre-filled in URL: $bodyInUrl"
Write-Host "Issue URL length: $($issueUrl.Length) bytes (budget: $MaxUrlBytes)"
