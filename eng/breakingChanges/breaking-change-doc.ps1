# Breaking Change Documentation Tool - All-in-One Script
# This script automates the creation of breaking change documentation for .NET runtime PRs
# Combines all functionality into a single, easy-to-use script

param(
    [switch]$CollectOnly = $false,      # Only collect PR data, don't create issues
    [switch]$CreateIssues = $false,     # Create GitHub issues directly
    [switch]$Comment = $false,          # Add comments with links to create issues
    [switch]$Clean = $false,            # Clean previous data before starting
    [string]$PrNumber = $null,          # Process only specific PR number
    [string]$Query = $null,             # GitHub search query for PRs
    [switch]$Help = $false              # Show help
)

# Show help
if ($Help) {
    Write-Host @"
Breaking Change Documentation Workflow

DESCRIPTION:
    Automates the creation of high-quality breaking change documentation
    for .NET runtime PRs using an LLM to analyze and author docs.

    DEFAULT BEHAVIOR: Analyzes PRs and generates documentation drafts without
    making any changes to GitHub. Use -CreateIssues or -Comment to execute actions.

USAGE:
    .\breaking-change-doc.ps1 [parameters]

PARAMETERS:
    -CollectOnly    Only collect PR data, don't create documentation
    -CreateIssues   Create GitHub issues directly
    -Comment        Add comments with links to create issues
    -Clean          Clean previous data before starting
    -PrNumber       Process only specific PR number
    -Query          GitHub search query for PRs (required if no -PrNumber)
    -Help           Show this help

EXAMPLES:
    .\breaking-change-doc.ps1 -PrNumber 114929                                                        # Process specific PR
    .\breaking-change-doc.ps1 -Query "state:closed label:needs-breaking-change-doc-created is:merged merged:>2024-09-16 -milestone:11.0.0"
    .\breaking-change-doc.ps1 -Query "state:closed label:needs-breaking-change-doc-created is:merged" -Comment
    .\breaking-change-doc.ps1 -PrNumber 114929 -CreateIssues                                         # Create issues directly
    .\breaking-change-doc.ps1 -Query "your-search-query" -CollectOnly                                # Only collect data

QUERY EXAMPLES:
    # PRs merged after specific date, excluding milestone:
    "state:closed label:needs-breaking-change-doc-created is:merged merged:>2024-09-16 -milestone:11.0.0"

    # All PRs with the target label:
    "state:closed label:needs-breaking-change-doc-created is:merged"

    # PRs from specific author:
    "state:closed label:needs-breaking-change-doc-created is:merged author:username"

SETUP:
    1. Install GitHub CLI and authenticate: gh auth login
    2. Choose LLM provider:
       - For GitHub Models: gh extension install github/gh-models (optional: set GITHUB_MODELS_API_KEY)
       - For GitHub Copilot: Install GitHub Copilot CLI from https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli (optional: set GITHUB_COPILOT_API_KEY)
       - For OpenAI: `$env:OPENAI_API_KEY = "your-key"
       - For Azure OpenAI: `$env:AZURE_OPENAI_API_KEY = "your-key" and set LlmBaseUrl in config.ps1
       - For others: Set appropriate API key
    3. Edit config.ps1 to customize settings
"@
    exit 0
}

Write-Host "🤖 Breaking Change Documentation Tool" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan

# Load configuration
if (Test-Path ".\config.ps1") {
    . ".\config.ps1"
} else {
    Write-Error "config.ps1 not found. Please create configuration file."
    exit 1
}

# Ensure powershell-yaml module is available for GitHub Models
if ($Config.LlmProvider -eq "github-models") {
    if (-not (Get-Module -ListAvailable -Name "powershell-yaml")) {
        Write-Host "📦 Installing powershell-yaml module for GitHub Models support..." -ForegroundColor Yellow
        try {
            Install-Module -Name "powershell-yaml" -Scope CurrentUser -Force -AllowClobber
            Write-Host "✅ powershell-yaml module installed successfully" -ForegroundColor Green
        }
        catch {
            Write-Error "❌ Failed to install powershell-yaml module: $($_.Exception.Message)"
            Write-Error "   Please install manually: Install-Module -Name powershell-yaml -Scope CurrentUser"
            exit 1
        }
    }

    # Import the module
    Import-Module powershell-yaml -ErrorAction Stop
}

# Validate prerequisites
Write-Host "`n🔍 Validating prerequisites..." -ForegroundColor Yellow

# Check GitHub CLI
if (-not (Get-Command "gh" -ErrorAction SilentlyContinue)) {
    Write-Error "❌ GitHub CLI not found. Install from https://cli.github.com/"
    exit 1
}

try {
    gh auth status | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ GitHub CLI not authenticated. Run 'gh auth login'"
        exit 1
    }
    Write-Host "✅ GitHub CLI authenticated" -ForegroundColor Green
} catch {
    Write-Error "❌ GitHub CLI error: $($_.Exception.Message)"
    exit 1
}

# Check LLM API key or GitHub CLI for GitHub Models/Copilot
$llmProvider = $Config.LlmProvider
$apiKey = switch ($llmProvider) {
    "openai" { $env:OPENAI_API_KEY }
    "anthropic" { $env:ANTHROPIC_API_KEY }
    "azure-openai" { $env:AZURE_OPENAI_API_KEY }
    "github-models" { $env:GITHUB_MODELS_API_KEY }  # Optional API key for GitHub Models
    "github-copilot" { $env:GITHUB_COPILOT_API_KEY }  # Optional API key for GitHub Copilot CLI
    default { $env:OPENAI_API_KEY }
}

if ($llmProvider -eq "github-models") {
    # Check if gh-models extension is installed
    try {
        $modelsExtension = gh extension list 2>$null | Select-String "gh models"
        if (-not $modelsExtension) {
            Write-Error "❌ GitHub Models extension not found. Install with: gh extension install github/gh-models"
            exit 1
        }
        Write-Host "✅ GitHub Models extension found" -ForegroundColor Green
    } catch {
        Write-Error "❌ Could not check GitHub Models extension: $($_.Exception.Message)"
        exit 1
    }
} elseif ($llmProvider -eq "github-copilot") {
    # Check if standalone GitHub Copilot CLI is installed
    try {
        $copilotVersion = copilot --version 2>$null
        if (-not $copilotVersion -or $LASTEXITCODE -ne 0) {
            Write-Error "❌ GitHub Copilot CLI not found. Install from: https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli"
            exit 1
        }
        Write-Host "✅ GitHub Copilot CLI found (version: $($copilotVersion.Split("`n")[0]))" -ForegroundColor Green
    } catch {
        Write-Error "❌ Could not check GitHub Copilot CLI: $($_.Exception.Message)"
        Write-Error "   Install from: https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli"
        exit 1
    }
} elseif (-not $apiKey) {
    Write-Error "❌ No LLM API key found. Set environment variable:"
    Write-Host "   For OpenAI: `$env:OPENAI_API_KEY = 'your-key'"
    Write-Host "   For Anthropic: `$env:ANTHROPIC_API_KEY = 'your-key'"
    Write-Host "   For Azure OpenAI: `$env:AZURE_OPENAI_API_KEY = 'your-key'"
    Write-Host "   For GitHub Models: Use 'github-models' provider (no key needed, or set GITHUB_MODELS_API_KEY for different account)"
    Write-Host "   For GitHub Copilot: Install GitHub Copilot CLI from https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli (no key needed, or set GITHUB_COPILOT_API_KEY for different account)"
    exit 1
} else {
    Write-Host "✅ LLM API key found ($llmProvider)" -ForegroundColor Green
}

# Determine repository root and set up output paths
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptPath)
$outputRoot = Join-Path $repoRoot "artifacts\docs\breakingChanges"

# Define output directories
$dataDir = Join-Path $outputRoot "data"
$issueDraftsDir = Join-Path $outputRoot "issue-drafts"
$commentDraftsDir = Join-Path $outputRoot "comment-drafts"
$promptsDir = Join-Path $outputRoot "prompts"

if ($Clean) {
    Write-Host "`n🧹 Cleaning previous data..." -ForegroundColor Yellow
    if (Test-Path $outputRoot) { Remove-Item $outputRoot -Recurse -Force }
    Write-Host "✅ Cleanup completed" -ForegroundColor Green

    if (-not $PrNumber -and -not $Query) {
        exit 0
    }
}

New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
New-Item -ItemType Directory -Path $issueDraftsDir -Force | Out-Null
New-Item -ItemType Directory -Path $commentDraftsDir -Force | Out-Null
New-Item -ItemType Directory -Path $promptsDir -Force | Out-Null

# Validate parameters
if (-not $PrNumber -and -not $Query) {
    Write-Error @"
❌ Either -PrNumber or -Query must be specified.

EXAMPLES:
  Process specific PR:
    .\breaking-change-doc.ps1 -PrNumber 114929

  Query for PRs (example - customize as needed):
    .\breaking-change-doc.ps1 -Query "state:closed label:needs-breaking-change-doc-created is:merged merged:>2024-09-16 -milestone:11.0.0"

Use -Help for more examples and detailed usage information.
"@
    exit 1
}

if ($PrNumber -and $Query) {
    Write-Error "❌ Cannot specify both -PrNumber and -Query. Choose one."
    exit 1
}

# Determine action mode - default to analysis only if no action specified
$executeActions = $CreateIssues -or $Comment

# Validate parameter combinations
if (($CreateIssues -and $Comment) -or ($CollectOnly -and ($CreateIssues -or $Comment))) {
    Write-Error "❌ Cannot combine -CollectOnly, -CreateIssues, and -Comment. Choose one action mode."
    exit 1
}

$actionMode = if ($CollectOnly) { "Collect Only" }
              elseif ($CreateIssues) { "Create Issues" }
              elseif ($Comment) { "Add Comments" }
              else { "Analysis Only" }

Write-Host "   Action Mode: $actionMode" -ForegroundColor Cyan
if (-not $executeActions -and -not $CollectOnly) {
    Write-Host "   📝 Will generate drafts without making changes to GitHub" -ForegroundColor Yellow
}

# Function to safely truncate text
function Limit-Text {
    param([string]$text, [int]$maxLength = 2000)

    if (-not $text -or $text.Length -le $maxLength) {
        return $text
    }

    $truncated = $text.Substring(0, $maxLength)
    $lastPeriod = $truncated.LastIndexOf('.')
    $lastNewline = $truncated.LastIndexOf("`n")

    $cutPoint = [Math]::Max($lastPeriod, $lastNewline)
    if ($cutPoint -gt ($maxLength * 0.8)) {
        $truncated = $truncated.Substring(0, $cutPoint + 1)
    }

    return $truncated + "`n`n[Content truncated for length]"
}

# Function to execute a script block with a temporary GITHUB_TOKEN
function Enter-GitHubSession {
    param([string]$ApiKey)

    # Store original token
    $originalGitHubToken = $env:GH_TOKEN

    if ($ApiKey) {
        # Set temporary token
        $env:GH_TOKEN = $ApiKey
    }

    return $originalGitHubToken
}

function Exit-GitHubSession {
    param([string]$OriginalGitHubToken)

    # Restore original token
    if ($OriginalGitHubToken) {
        $env:GH_TOKEN = $OriginalGitHubToken
    } else {
        Remove-Item env:GH_TOKEN -ErrorAction SilentlyContinue
    }
}

# Function to fetch issue template from GitHub
function Get-IssueTemplate {
    try {
        Write-Host "     📋 Fetching issue template..." -ForegroundColor DarkGray
        # Use public GitHub API (no auth required for public repos)
        $response = Invoke-RestMethod -Uri "https://api.github.com/repos/$($Config.DocsRepo)/contents/$($Config.IssueTemplatePath)" -Headers @{ 'User-Agent' = 'dotnet-runtime-breaking-change-tool' }
        $templateContent = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($response.content))
        return $templateContent
    }
    catch {
        Write-Error "❌ Failed to fetch issue template from $($Config.DocsRepo)/$($Config.IssueTemplatePath): $($_.Exception.Message)"
        Write-Error "   Template is required for high-quality documentation generation. Please check repository access and template path."
        exit 1
    }
}

# Function to fetch example breaking change issues
function Get-ExampleBreakingChangeIssues {
    try {
        Write-Host "     📚 Fetching example breaking change issues..." -ForegroundColor DarkGray
        # Use public GitHub API for issues
        $response = Invoke-RestMethod -Uri "https://api.github.com/repos/$($Config.DocsRepo)/issues?labels=breaking-change&state=all&per_page=3" -Headers @{ 'User-Agent' = 'dotnet-runtime-breaking-change-tool' }

        if ($response.Count -eq 0) {
            Write-Error "❌ No example breaking change issues found in $($Config.DocsRepo) with label 'breaking-change'"
            Write-Error "   Examples are required for high-quality documentation generation. Please check repository and label."
            exit 1
        }

        $examples = @()
        foreach ($issue in $response) {
            $examples += @"
**Example #$($issue.number)**: $($issue.title)
URL: $($issue.html_url)
Body: $(Limit-Text -text $issue.body -maxLength 800)
"@
        }
        return $examples -join "`n`n---`n`n"
    }
    catch {
        Write-Error "❌ Failed to fetch example breaking change issues from $($Config.DocsRepo): $($_.Exception.Message)"
        Write-Error "   Examples are required for high-quality documentation generation. Please check repository access."
        exit 1
    }
}

# Function to parse a .NET runtime tag into its components
function ConvertFrom-DotNetTag {
    param([string]$tagName)

    if (-not $tagName -or $tagName -eq "Unknown") {
        return $null
    }

    # Parse v(major).(minor).(build)(-prerelease)
    if ($tagName -match '^v(\d+)\.(\d+)\.(\d+)(?:-(.+))?$') {
        $major = [int]$matches[1]
        $minor = [int]$matches[2]
        $build = [int]$matches[3]
        $prerelease = if ($matches[4]) { $matches[4] } else { $null }

        # Parse prerelease into type and number using single regex
        $prereleaseType = $null
        $prereleaseNumber = $null

        if ($prerelease -and $prerelease -match '^([a-zA-Z]+)\.(\d+)') {
            $rawType = $matches[1]
            $prereleaseNumber = [int]$matches[2]

            # Normalize prerelease type casing
            if ($rawType -ieq "rc") {
                $prereleaseType = "RC"
            } else {
                # Capitalize first letter for other types
                $prereleaseType = $rawType.Substring(0,1).ToUpper() + $rawType.Substring(1).ToLower()
            }
        }

        return @{
            Major = $major
            Minor = $minor
            Build = $build
            Prerelease = $prerelease
            PrereleaseType = $prereleaseType
            PrereleaseNumber = $prereleaseNumber
            IsRelease = $null -eq $prerelease
        }
    }

    return $null
}

# Function to format a parsed tag as a readable .NET version
function Format-DotNetVersion {
    param($parsedTag)

    if (-not $parsedTag) {
        return "Next release"
    }

    $baseVersion = ".NET $($parsedTag.Major).$($parsedTag.Minor)"

    if ($parsedTag.IsRelease) {
        return $baseVersion
    }

    if ($parsedTag.PrereleaseType -and $parsedTag.PrereleaseNumber) {
        return "$baseVersion $($parsedTag.PrereleaseType) $($parsedTag.PrereleaseNumber)"
    }

    # Fallback for unknown prerelease formats
    return "$baseVersion ($($parsedTag.Prerelease))"
}

# Function to estimate the next version based on current tag and branch
function Get-EstimatedNextVersion {
    param($parsedTag, [string]$baseRef)

    if (-not $parsedTag) {
        return "Next release"
    }

    $isMainBranch = $baseRef -eq "main"

    # If this is a release version
    if ($parsedTag.IsRelease) {
        if ($isMainBranch) {
            # Assume changes to main when last tag is release go to next release.
            $nextMajor = $parsedTag.Major + 1
            return ".NET $nextMajor.0 Preview 1"
        } else {
            # Next patch/build
            return ".NET $($parsedTag.Major).$($parsedTag.Minor)"
        }
    }

    # If this is a prerelease version
    if ($isMainBranch -and $parsedTag.PrereleaseType -eq "RC") {
        # Assume changes to main when last tag is RC go to next release.
        $nextMajor = $parsedTag.Major + 1
        return ".NET $nextMajor.0 Preview 1"
    } else {
        # Next preview
        $nextPreview = $parsedTag.PrereleaseNumber + 1
        return ".NET $($parsedTag.Major).$($parsedTag.Minor) $($parsedTag.PrereleaseType) $nextPreview"
    }
}

# Function to find the closest tag by commit distance
function Find-ClosestTagByDistance {
    param([string]$targetCommit, [int]$maxTags = 10)

    $recentTags = git tag --sort=-version:refname 2>$null | Select-Object -First $maxTags
    $closestTag = $null
    $minDistance = [int]::MaxValue

    foreach ($tag in $recentTags) {
        # Check if this tag contains the target commit (skip if it does for merged PRs)
        if ($targetCommit -match '^[a-f0-9]{40}$') {
            # This is a commit hash, check if tag contains it
            git merge-base --is-ancestor $targetCommit $tag 2>$null
            if ($LASTEXITCODE -eq 0) {
                # This tag contains our commit, skip it
                continue
            }
        }

        # Calculate commit distance between tag and target
        $distance = git rev-list --count "$tag..$targetCommit" 2>$null
        if ($LASTEXITCODE -eq 0 -and $distance -match '^\d+$') {
            $distanceNum = [int]$distance
            if ($distanceNum -lt $minDistance) {
                $minDistance = $distanceNum
                $closestTag = $tag
            }
        }
    }

    return $closestTag
}

# Function to get version information using local git repository
function Get-VersionInfo {
    param([string]$prNumber, [string]$mergedAt, [string]$baseRef = "main")

    try {
        # Change to the repository directory (we're already in the repo)
        Push-Location $repoRoot

        try {
            # Ensure we have latest info
            git fetch --tags 2>$null | Out-Null

            # Determine the target commit for version analysis
            $targetCommit = $null
            $firstTagWith = "Not yet released"

            if ($prNumber -and $mergedAt) {
                # For merged PRs, try to get the merge commit
                $targetCommit = gh pr view $prNumber --repo $Config.SourceRepo --json mergeCommit --jq '.mergeCommit.oid' 2>$null

                if ($targetCommit) {
                    # Get the first tag that includes this commit
                    $firstTagWith = git describe --tags --contains $targetCommit 2>$null
                    if ($firstTagWith -and $firstTagWith -match '^([^~^]+)') {
                        $firstTagWith = $matches[1]
                    }
                }
            }

            # If no target commit yet (unmerged PR or failed to get merge commit), use branch head
            if (-not $targetCommit) {
                $targetCommit = git rev-parse "origin/$baseRef" 2>$null
            }

            # Find the last tag before this commit
            $lastTagBefore = "Unknown"
            if ($targetCommit) {
                $closestTag = Find-ClosestTagByDistance -targetCommit $targetCommit
                if ($closestTag) {
                    $lastTagBefore = $closestTag
                } else {
                    # Fallback strategies
                    if ($baseRef -eq "main") {
                        # Try git describe on the target branch
                        $lastTagBefore = git describe --tags --abbrev=0 "origin/$baseRef" 2>$null
                        if (-not $lastTagBefore) {
                            # Final fallback: most recent tag overall
                            $lastTagBefore = git tag --sort=-version:refname | Select-Object -First 1 2>$null
                        }
                    } else {
                        $lastTagBefore = git describe --tags --abbrev=0 "origin/$baseRef" 2>$null
                    }
                }
            }

            # Clean up tag names and estimate version
            $lastTagBefore = if ($lastTagBefore) { $lastTagBefore.Trim() } else { "Unknown" }
            $firstTagWith = if ($firstTagWith -and $firstTagWith -ne "Not yet released") { $firstTagWith.Trim() } else { "Not yet released" }

            # Determine the estimated version using new tag parsing logic
            $estimatedVersion = "Next release"

            if ($firstTagWith -ne "Not yet released") {
                # If we know the first tag that contains this change, use it directly
                $parsedFirstTag = ConvertFrom-DotNetTag $firstTagWith
                $estimatedVersion = Format-DotNetVersion $parsedFirstTag
            } else {
                # Estimate based on the last tag before this change
                $parsedLastTag = ConvertFrom-DotNetTag $lastTagBefore
                $estimatedVersion = Get-EstimatedNextVersion $parsedLastTag $baseRef
            }

            return @{
                LastTagBeforeMerge = $lastTagBefore
                FirstTagWithChange = $firstTagWith
                EstimatedVersion = $estimatedVersion
            }
        }
        finally {
            Pop-Location
        }
    }
    catch {
        Write-Warning "Could not get version information using git: $($_.Exception.Message)"
        return @{
            LastTagBeforeMerge = "Unknown"
            FirstTagWithChange = "Not yet released"
            EstimatedVersion = "Next release"
        }
    }
}

# Function to call LLM API
function Invoke-LlmApi {
    param([string]$Prompt, [string]$SystemPrompt = "", [int]$MaxTokens = 3000, [string]$PrNumber = "unknown")

    switch ($Config.LlmProvider) {
        "github-models" {
            # Use GitHub CLI with models extension
            try {
                # Create prompt file in YAML format for GitHub Models
                $promptFile = Join-Path $promptsDir "pr_${PrNumber}_prompt.yml"

                # Create YAML structure for GitHub Models
                $messages = @()

                if ($SystemPrompt) {
                    $messages += @{
                        role = "system"
                        content = $SystemPrompt
                    }
                }

                $messages += @{
                    role = "user"
                    content = $Prompt
                }

                $promptYaml = @{
                    name = "Breaking Change Documentation"
                    description = "Generate breaking change documentation for .NET runtime PR"
                    model = $Config.LlmModel
                    modelParameters = @{
                        temperature = 0.1
                        max_tokens = $MaxTokens
                    }
                    messages = $messages
                }

                # Convert to YAML and save to file
                $promptYaml | ConvertTo-Yaml | Out-File -FilePath $promptFile -Encoding UTF8

                try {
                    $gitHubSession = Enter-GitHubSession $apiKey
                    $output = gh models run --file $promptFile
                    $exitCode = $LASTEXITCODE
                } finally {
                    Exit-GitHubSession $gitHubSession
                }

                if ($exitCode -ne 0) {
                    throw "gh models run failed with exit code $exitCode"
                }

                # Join the output lines with newlines to preserve formatting
                return $output -join "`n"
            }
            catch {
                Write-Error "GitHub Models API call failed: $($_.Exception.Message)"
                return $null
            }
        }
        "github-copilot" {
            # Use GitHub Copilot CLI in programmatic mode
            try {
                # Create prompt file for GitHub Copilot CLI
                $promptFile = Join-Path $promptsDir "pr_${PrNumber}_copilot_prompt.txt"

                # Combine system prompt and user prompt, emphasizing text-only response
                $fullPrompt = if ($SystemPrompt) {
                    "$SystemPrompt`n`nIMPORTANT: Please respond with only the requested text content. Do not create, modify, or execute any files. Just return the text response.`n`n$Prompt"
                } else {
                    "IMPORTANT: Please respond with only the requested text content. Do not create, modify, or execute any files. Just return the text response.`n`n$Prompt"
                }

                # Write prompt to file
                $fullPrompt | Out-File -FilePath $promptFile -Encoding UTF8

                try {
                    $gitHubSession = Enter-GitHubSession $apiKey
                    # Add --allow-all-tools for non-interactive mode and --allow-all-paths to avoid file access prompts
                    $rawResponse = copilot -p "@$promptFile" --log-level none --allow-all-tools --allow-all-paths
                } finally {
                    Exit-GitHubSession $gitHubSession
                }

                # Parse the response to extract just the content, removing usage statistics
                # The response format typically includes usage stats at the end starting with "Total usage est:"
                $lines = $rawResponse -split "`n"
                $contentLines = @()
                $foundUsageStats = $false

                foreach ($line in $lines) {
                    if ($line -match "^Total usage est:" -or $line -match "^Total duration") {
                        $foundUsageStats = $true
                        break
                    }
                    if (-not $foundUsageStats) {
                        $contentLines += $line
                    }
                }

                # Join the content lines and trim whitespace
                $response = ($contentLines -join "`n").Trim()
                return $response
            }
            catch {
                Write-Error "GitHub Copilot CLI call failed: $($_.Exception.Message)"
                return $null
            }
        }
        "openai" {
            # OpenAI API
            $endpoint = if ($Config.LlmBaseUrl) { "$($Config.LlmBaseUrl)/chat/completions" } else { "https://api.openai.com/v1/chat/completions" }
            $headers = @{
                'Content-Type' = 'application/json'
                'Authorization' = "Bearer $apiKey"
            }

            $messages = @()
            if ($SystemPrompt) { $messages += @{ role = "system"; content = $SystemPrompt } }
            $messages += @{ role = "user"; content = $Prompt }

            $body = @{
                model = $Config.LlmModel
                messages = $messages
                max_tokens = $MaxTokens
                temperature = 0.1
            }

            try {
                $requestJson = $body | ConvertTo-Json -Depth 10
                $response = Invoke-RestMethod -Uri $endpoint -Method POST -Headers $headers -Body $requestJson
                return $response.choices[0].message.content
            }
            catch {
                Write-Error "OpenAI API call failed: $($_.Exception.Message)"
                return $null
            }
        }
        "anthropic" {
            # Anthropic API
            $endpoint = if ($Config.LlmBaseUrl) { "$($Config.LlmBaseUrl)/messages" } else { "https://api.anthropic.com/v1/messages" }
            $headers = @{
                'Content-Type' = 'application/json'
                'x-api-key' = $apiKey
                'anthropic-version' = "2023-06-01"
            }

            $fullPrompt = if ($SystemPrompt) { "$SystemPrompt`n`nHuman: $Prompt`n`nAssistant:" } else { "Human: $Prompt`n`nAssistant:" }

            $body = @{
                model = $Config.LlmModel
                max_tokens = $MaxTokens
                messages = @(@{ role = "user"; content = $fullPrompt })
                temperature = 0.1
            }

            try {
                $requestJson = $body | ConvertTo-Json -Depth 10
                $response = Invoke-RestMethod -Uri $endpoint -Method POST -Headers $headers -Body $requestJson
                return $response.content[0].text
            }
            catch {
                Write-Error "Anthropic API call failed: $($_.Exception.Message)"
                return $null
            }
        }
        "azure-openai" {
            # Azure OpenAI API
            # Endpoint format: https://{resource}.openai.azure.com/openai/deployments/{deployment}/chat/completions?api-version={api-version}
            if (-not $Config.LlmBaseUrl) {
                Write-Error "Azure OpenAI requires LlmBaseUrl to be set in config (e.g., 'https://your-resource.openai.azure.com')"
                return $null
            }

            $apiVersion = if ($Config.AzureApiVersion) { $Config.AzureApiVersion } else { "2024-02-15-preview" }
            $endpoint = "$($Config.LlmBaseUrl)/openai/deployments/$($Config.LlmModel)/chat/completions?api-version=$apiVersion"

            $headers = @{
                'Content-Type' = 'application/json'
                'api-key' = $apiKey
            }

            $messages = @()
            if ($SystemPrompt) { $messages += @{ role = "system"; content = $SystemPrompt } }
            $messages += @{ role = "user"; content = $Prompt }

            $body = @{
                messages = $messages
                max_tokens = $MaxTokens
                temperature = 0.1
            }

            try {
                $requestJson = $body | ConvertTo-Json -Depth 10
                $response = Invoke-RestMethod -Uri $endpoint -Method POST -Headers $headers -Body $requestJson
                return $response.choices[0].message.content
            }
            catch {
                Write-Error "Azure OpenAI API call failed: $($_.Exception.Message)"
                return $null
            }
        }
        default {
            Write-Error "Unknown LLM provider: $($Config.LlmProvider)"
            return $null
        }
    }
}

# STEP 1: Collect PR data
Write-Host "`n📥 Step 1: Collecting comprehensive PR data..." -ForegroundColor Green

if ($PrNumber) {
    # Single PR mode - fetch only the specified PR
    Write-Host "   Mode: Single PR #$PrNumber"
    $prs = @(@{ number = $PrNumber })
} else {
    # Query mode - fetch all PRs matching criteria
    Write-Host "   Mode: Query - $Query"

    try {
        $prsJson = gh pr list --repo $Config.SourceRepo --search $Query --limit $Config.MaxPRs --json number
        $prs = $prsJson | ConvertFrom-Json
    } catch {
        Write-Error "Failed to fetch PRs: $($_.Exception.Message)"
        exit 1
    }

    Write-Host "   Found $($prs.Count) PRs to collect data for"
}

# Collect detailed data for each PR
$analysisData = @()
foreach ($pr in $prs) {
    Write-Host "   Collecting data for PR #$($pr.number): $($pr.title)" -ForegroundColor Gray

    # Get comprehensive PR details including comments, reviews, and commits
    try {
        $prDetails = gh pr view $pr.number --repo $Config.SourceRepo --json number,title,author,url,baseRefName,closedAt,mergedAt,mergeCommit,labels,files,state,body,comments,reviews,closingIssuesReferences,commits
        $prDetailData = $prDetails | ConvertFrom-Json
    } catch {
        Write-Warning "Could not fetch detailed PR data for #$($pr.number)"
        continue
    }

    # Extract commits from the PR details
    $commits = @()
    if ($prDetailData.commits) {
        foreach ($commit in $prDetailData.commits) {
            $commitMessage = $commit.messageHeadline
            if ($commit.messageBody -and $commit.messageBody.Trim() -ne "") {
                $commitMessage += "`n`n" + $commit.messageBody
            }
            $commits += $commitMessage
        }
    }

    # Get closing issues with full details and comments
    $closingIssues = @()
    foreach ($issueRef in $prDetailData.closingIssuesReferences) {
        if ($issueRef.number) {
            try {
                Write-Host "     Fetching issue #$($issueRef.number)..." -ForegroundColor DarkGray
                $issueDetails = gh issue view $issueRef.number --repo $Config.SourceRepo --json number,title,body,comments,labels,state,createdAt,closedAt,url
                $issueData = $issueDetails | ConvertFrom-Json
                $closingIssues += @{
                    Number = $issueData.number
                    Title = $issueData.title
                    Body = $issueData.body
                    Comments = $issueData.comments
                    Labels = $issueData.labels | ForEach-Object { $_.name }
                    State = $issueData.state
                    CreatedAt = $issueData.createdAt
                    ClosedAt = $issueData.closedAt
                    Url = $issueData.url
                }
            }
            catch {
                Write-Warning "Could not fetch issue #$($issueRef.number)"
            }
        }
    }

    # Create merge commit URL
    $mergeCommitUrl = if ($prDetailData.mergeCommit.oid) {
        "https://github.com/$($Config.SourceRepo)/commit/$($prDetailData.mergeCommit.oid)"
    } else {
        $null
    }

    # Get version information using local git repository
    Write-Host "     🏷️ Getting version info..." -ForegroundColor DarkGray
    $versionInfo = Get-VersionInfo -prNumber $prDetailData.number -mergedAt $prDetailData.closedAt -baseRef $prDetailData.baseRefName

    # Check for existing docs issues
    $hasDocsIssue = $false
    try {
        $searchResult = gh issue list --repo $Config.DocsRepo --search "Breaking change $($prDetailData.number)" --json number,title
        $existingIssues = $searchResult | ConvertFrom-Json
        $hasDocsIssue = $existingIssues.Count -gt 0
    } catch {
        Write-Warning "Could not check for existing docs issues for PR #$($prDetailData.number)"
    }

    # Get feature areas from area- labels first, then fall back to file paths
    $featureAreas = @()

    # First try to get feature areas from area- labels
    foreach ($label in $prDetailData.labels) {
        if ($label.name -match "^area-(.+)$") {
            $featureAreas += $matches[1]
        }
    }

    $featureAreas = $featureAreas | Select-Object -Unique
    if ($featureAreas.Count -eq 0) {
        Write-Error "Unable to determine feature area for PR #$($prDetailData.Number).  Please set an 'area-' label."
    }

    $analysisData += @{
        Number = $prDetailData.number
        Title = $prDetailData.title
        Url = $prDetailData.url
        Author = $prDetailData.author.login
        BaseRef = $prDetailData.baseRefName
        ClosedAt = $prDetailData.closedAt
        MergedAt = $prDetailData.mergedAt
        MergeCommit = @{
            Sha = $prDetailData.mergeCommit.oid
            Url = $mergeCommitUrl
        }
        Commits = $commits
        Body = $prDetailData.body
        Comments = $prDetailData.comments
        Reviews = $prDetailData.reviews
        ClosingIssues = $closingIssues
        HasDocsIssue = $hasDocsIssue
        ExistingDocsIssues = if ($hasDocsIssue) { $existingIssues } else { @() }
        FeatureAreas = $featureAreas -join ", "
        ChangedFiles = $prDetailData.files | ForEach-Object { $_.path }
        Labels = $prDetailData.labels | ForEach-Object { $_.name }
        VersionInfo = $versionInfo
    }

    # Save individual PR data file
    $prFileName = Join-Path $dataDir "pr_$($prDetailData.number).json"
    $analysisData[-1] | ConvertTo-Json -Depth 10 | Out-File $prFileName -Encoding UTF8
    Write-Host "     💾 Saved: $prFileName" -ForegroundColor DarkGray

    Start-Sleep -Seconds $Config.RateLimiting.DelayBetweenCalls
}

# Save combined data with comprehensive details (for overview)
$analysisData | ConvertTo-Json -Depth 10 | Out-File (Join-Path $dataDir "combined.json") -Encoding UTF8

# Create summary report
$queryInfo = if ($PrNumber) {
    "Single PR #$PrNumber"
} else {
    "Query: $Query"
}

$summaryReport = @"
# Breaking Change Documentation Collection Report

**Generated**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**Mode**: $queryInfo

## Summary

- **Total PRs collected**: $($analysisData.Count)
- **PRs with existing docs issues**: $($analysisData | Where-Object HasDocsIssue | Measure-Object).Count
- **PRs needing docs issues**: $($analysisData | Where-Object { -not $_.HasDocsIssue } | Measure-Object).Count

## PRs Needing Documentation

$($analysisData | Where-Object { -not $_.HasDocsIssue } | ForEach-Object {
    "- **PR #$($_.Number)**: $($_.Title)`n  - URL: $($_.Url)`n  - Feature Areas: $($_.FeatureAreas)`n"
} | Out-String)

## PRs With Existing Documentation

$($analysisData | Where-Object HasDocsIssue | ForEach-Object {
    "- **PR #$($_.Number)**: $($_.Title)`n  - URL: $($_.Url)`n"
} | Out-String)
"@

$summaryReport | Out-File (Join-Path $dataDir "summary_report.md") -Encoding UTF8

Write-Host "✅ Data collection completed" -ForegroundColor Green
Write-Host "   📊 Summary: $(Join-Path $dataDir "summary_report.md")"
Write-Host "   📋 Combined: $(Join-Path $dataDir "combined.json")"
Write-Host "   📄 Individual: $(Join-Path $dataDir "pr_*.json") ($($analysisData.Count) files)"

if ($CollectOnly) {
    exit 0
}

# STEP 2: Generate breaking change issues
Write-Host "`n📝 Step 2: Generating breaking change documentation..." -ForegroundColor Green

$prsNeedingDocs = $analysisData | Where-Object { -not $_.HasDocsIssue }

if ($prsNeedingDocs.Count -eq 0) {
    if ($PrNumber) {
        Write-Host "   PR #$PrNumber already has documentation or doesn't need it." -ForegroundColor Yellow
    } else {
        Write-Host "   No PRs found that need documentation issues." -ForegroundColor Yellow
    }
    exit 0
}

if ($PrNumber) {
    Write-Host "   Processing PR #$PrNumber for issue generation"
} else {
    Write-Host "   Processing $($prsNeedingDocs.Count) PRs for issue generation"
}

foreach ($pr in $prsNeedingDocs) {
    Write-Host "   🔍 Processing PR #$($pr.Number): $($pr.Title)" -ForegroundColor Cyan

    # Use commits data already collected in Step 1
    $commits = $pr.Commits

    # Prepare data for LLM
    $comments = if ($pr.Comments -and $pr.Comments.Count -gt 0) {
        ($pr.Comments | ForEach-Object { "**@$($_.author.login)**: $(Limit-Text -text $_.body -maxLength 300)" }) -join "`n`n"
    } else { "No comments" }

    $reviews = if ($pr.Reviews -and $pr.Reviews.Count -gt 0) {
        ($pr.Reviews | ForEach-Object { "**@$($_.author.login)** ($($_.state)): $(Limit-Text -text $_.body -maxLength 200)" }) -join "`n`n"
    } else { "No reviews" }

    $closingIssuesInfo = if ($pr.ClosingIssues -and $pr.ClosingIssues.Count -gt 0) {
        $issuesList = $pr.ClosingIssues | ForEach-Object {
            $issueComments = if ($_.Comments -and $_.Comments.Count -gt 0) {
                "Comments: $($_.Comments.Count) comments available"
            } else {
                "No comments"
            }
            @"
**Issue #$($_.Number)**: $($_.Title)
$($_.Url)
$(if ($_.Body) { "$(Limit-Text -text $_.Body -maxLength 500)" })
$issueComments
"@
        }
        "`n## Related Issues`n" + ($issuesList -join "`n`n")
    } else { "" }

    # Fetch issue template and examples (required for quality)
    $issueTemplate = Get-IssueTemplate
    $exampleIssues = Get-ExampleBreakingChangeIssues

    # Use version information collected in Step 1
    $versionInfo = $pr.VersionInfo

    # Create LLM prompt
    $systemPrompt = @"
You are an expert .NET developer and technical writer. Create high-quality breaking change documentation for Microsoft .NET.

**CRITICAL: Generate clean markdown content following the structure shown in the examples. Do NOT output YAML or fill in template forms. The template is provided only as a reference for sections and values.**

Focus on:
1. Clear, specific descriptions of what changed
2. Concrete before/after behavior with examples
3. Actionable migration guidance for developers
4. Appropriate breaking change categorization
5. Professional tone for official Microsoft documentation

Use the provided template as a reference for structure only, and follow the examples for the actual output format.
Pay special attention to the version information provided to ensure accuracy.
"@

    $templateSection = @"
## Issue Template Structure Reference
The following GitHub issue template shows the required sections and possible values for breaking change documentation.
**IMPORTANT: This is NOT the expected output format. Use this only as a reference for what sections to include and what values are available. Generate clean markdown content, not YAML.**

```yaml
$issueTemplate
```
"@

    $exampleSection = @"

## Examples of Good Breaking Change Documentation
Here are recent examples of well-written breaking change documentation:

$exampleIssues
"@

    $versionSection = @"

## Version Information
**Last GitHub tag before this PR was merged**: $($versionInfo.LastTagBeforeMerge)
**First GitHub tag that includes this change**: $($versionInfo.FirstTagWithChange)
**Estimated .NET version for this change**: $($versionInfo.EstimatedVersion)

Use this version information to accurately determine when this breaking change was introduced.
"@

    $userPrompt = @"
Analyze this .NET runtime pull request and create breaking change documentation.

## PR Information
**Number**: #$($pr.Number)
**Title**: $($pr.Title)
**URL**: $($pr.Url)
**Author**: $($pr.Author)
**Base Branch**: $($pr.BaseRef)
**Merged At**: $($pr.MergedAt)
**Feature Areas**: $($pr.FeatureAreas)

$(if ($pr.MergeCommit.Url) { "**Merge Commit**: $($pr.MergeCommit.Url)" })

**PR Body**:
$(Limit-Text -text $pr.Body -maxLength 1500)

## Commits
$(if ($commits -and $commits.Count -gt 0) {
    $commitInfo = $commits | ForEach-Object {
        "$(Limit-Text -text $_ -maxLength 300)"
    }
    $commitInfo -join "`n`n"
} else {
    "No commit information available"
})))

## Changed Files
$($pr.ChangedFiles -join "`n")

## Comments
$comments

## Reviews
$reviews
$closingIssuesInfo

$templateSection
$exampleSection
$versionSection

**OUTPUT FORMAT: Generate a complete breaking change issue in clean markdown format following the structure and style of the examples above. Do NOT output YAML template syntax.**

Generate the complete issue following the template structure and using the examples as guidance for quality and style.
"@

    # Call LLM API
    Write-Host "     🤖 Generating content..." -ForegroundColor Gray
    $llmResponse = Invoke-LlmApi -SystemPrompt $systemPrompt -Prompt $userPrompt -PrNumber $pr.Number

    if (-not $llmResponse) {
        Write-Error "Failed to get LLM response for PR #$($pr.Number)"
        continue
    }

    # Parse response
    if ($llmResponse -match '(?s)\*\*Issue Title\*\*:\s*(.+?)\s*\*\*Issue Body\*\*:\s*(.+)$') {
        $issueTitle = $matches[1].Trim()
        $issueBody = $matches[2].Trim()
    } else {
        $issueTitle = "[Breaking change]: $($pr.Title -replace '^\[.*?\]\s*', '')"
        $issueBody = $llmResponse
    }

    # Save issue draft
    $issueFile = Join-Path $issueDraftsDir "issue_pr_$($pr.Number).md"
    @"
# $issueTitle

$issueBody

---
*Generated by Breaking Change Documentation Tool*
*PR: $($pr.Url)*
*Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')*
"@ | Out-File $issueFile -Encoding UTF8

    Write-Host "     📄 Draft saved: $issueFile" -ForegroundColor Gray

     # Add comment with link to create issue using GitHub's issue creation URL
    $commentFile = Join-Path $commentDraftsDir "comment_pr_$($pr.Number).md"

    # URL encode the title and full issue body
    $encodedTitle = [Uri]::EscapeDataString($issueTitle)
    $encodedBody = [Uri]::EscapeDataString($issueBody)
    $encodedLabels = [Uri]::EscapeDataString($Config.IssueTemplate.Labels -join ",")

    # Create GitHub issue creation URL with full content and labels
    $createIssueUrl = "https://github.com/$($Config.DocsRepo)/issues/new?title=$encodedTitle&body=$encodedBody&labels=$encodedLabels"

    $commentBody = @"
## 📋 Breaking Change Documentation Required

[Create a breaking change issue with AI-generated content]($createIssueUrl)

*Generated by Breaking Change Documentation Tool - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')*
"@

    # Save comment draft
    $commentBody | Out-File $commentFile -Encoding UTF8
    Write-Host "     💬 Comment draft saved: $commentFile" -ForegroundColor Gray

    # Handle different action modes
    if (-not $executeActions) {
        # Draft only mode, just log the commands that could be run.
        Write-Host "     📝 To create an issue use command:" -ForegroundColor Yellow
        Write-Host "       gh issue create --repo $($Config.DocsRepo) --title `"$issueTitle`" --body `"...[content truncated]...`" --label `"$($Config.IssueTemplate.Labels -join ',')`" --assignee `"$($Config.IssueTemplate.Assignee)`"" -ForegroundColor Gray

        Write-Host "     💬 To add a comment use command:" -ForegroundColor Yellow
        Write-Host "       gh pr comment $($pr.Number) --repo $($Config.SourceRepo) --body-file `"$commentFile`"" -ForegroundColor Gray

    } elseif ($CreateIssues) {
        # Create GitHub issue directly
        try {
            Write-Host "     🚀 Creating GitHub issue..." -ForegroundColor Gray

            $result = gh issue create --repo $Config.DocsRepo --title $issueTitle --body $issueBody --label ($Config.IssueTemplate.Labels -join ",") --assignee $Config.IssueTemplate.Assignee

            if ($LASTEXITCODE -eq 0) {
                Write-Host "     ✅ Issue created: $result" -ForegroundColor Green

                # Add comment to original PR
                $prComment = "Breaking change documentation issue created: $result"
                gh pr comment $pr.Number --repo $Config.SourceRepo --body $prComment | Out-Null
            } else {
                Write-Error "Failed to create issue for PR #$($pr.Number)"
            }
        }
        catch {
            Write-Error "Error creating issue for PR #$($pr.Number): $($_.Exception.Message)"
        }
    } elseif ($Comment) {
       # Add a comment to the PR to allow the author to create the issue
        try {
            Write-Host "     💬 Adding comment to PR..." -ForegroundColor Gray

            $result = gh pr comment $pr.Number --repo $Config.SourceRepo --body-file $commentFile

            if ($LASTEXITCODE -eq 0) {
                Write-Host "     ✅ Comment added to PR #$($pr.Number)" -ForegroundColor Green
            } else {
                Write-Error "Failed to add comment to PR #$($pr.Number)"
            }
        }
        catch {
            Write-Error "Error adding comment to PR #$($pr.Number): $($_.Exception.Message)"
        }
    }

    Start-Sleep -Seconds $Config.RateLimiting.DelayBetweenIssues
}

# Final summary
Write-Host "`n🎯 Workflow completed!" -ForegroundColor Green

if (-not $executeActions -and -not $CollectOnly) {
    Write-Host "   📝 Analysis completed - drafts generated without making changes" -ForegroundColor Yellow
    Write-Host "   💡 Use -CreateIssues or -Comment to execute actions on GitHub"
} elseif ($CreateIssues) {
    Write-Host "   ✅ Issues created in: $($Config.DocsRepo)" -ForegroundColor Green
    Write-Host "   📧 Email issue links to: $($Config.IssueTemplate.NotificationEmail)" -ForegroundColor Yellow
} elseif ($Comment) {
    Write-Host "   💬 Comments added to PRs with create issue links" -ForegroundColor Green
    Write-Host "   📝 Issue drafts saved in: $issueDraftsDir"
    Write-Host "   🔗 Click the links in PR comments to create issues when ready"
} else {
    Write-Host "   📝 Issue drafts saved in: $issueDraftsDir"
}

Write-Host "`n📁 Output files:"
Write-Host "   📊 Summary: $(Join-Path $dataDir "summary_report.md")"
Write-Host "   📋 Combined: $(Join-Path $dataDir "combined.json")"
Write-Host "   📄 Individual: $(Join-Path $dataDir "pr_*.json")"
Write-Host "   📝 Drafts: $(Join-Path $issueDraftsDir "*.md")"
