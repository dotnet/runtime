# Breaking Change Documentation Automation

This script automates the creation of high-quality breaking change documentation for .NET runtime PRs using AI-powered analysis.

## Key Features

- **GitHub Models Integration**: Uses GitHub's AI models (no API keys required) with fallback to other providers
- **Dynamic Template Fetching**: Automatically fetches the latest breaking change issue template from dotnet/docs
- **Example-Based Learning**: Analyzes recent breaking change issues to improve content quality
- **Version Detection**: Analyzes GitHub tags to determine accurate .NET version information for proper milestone assignment
- **Flexible Workflow**: Multiple execution modes (CollectOnly, Comment, CreateIssues) with DryRun overlay
- **Comprehensive Data Collection**: Gathers PR details, related issues, merge commits, review comments, and closing issues
- **Area Label Detection**: Automatically detects feature areas from GitHub labels (area-*) with file path fallback
- **Individual File Output**: Creates separate JSON files per PR for easy examination

## Quick Setup

1. **Install Prerequisites:**
   - GitHub CLI: `gh auth login`
   - Choose LLM provider:
     - **GitHub Models** (recommended): `gh extension install github/gh-models`
     - **OpenAI**: Set `$env:OPENAI_API_KEY = "your-key"`
     - **Others**: See configuration section below

2. **Configure:**
   ```powershell
   # Edit config.ps1 to set:
   # - LlmProvider = "github-models" (or other provider)
   ```

3. **Run the workflow:**
   ```powershell
   .\breaking-change-doc.ps1 -DryRun
   ```

4. **Choose your workflow:**
   ```powershell
   # Default: Add comments with create issue links (recommended)
   .\breaking-change-doc.ps1

   # Create issues directly
   .\breaking-change-doc.ps1 -CreateIssues

   # Just collect data
   .\breaking-change-doc.ps1 -CollectOnly
   ```

## Commands

```powershell
# Dry run (inspect commands)
.\breaking-change-doc.ps1 -DryRun

# Default workflow (add comments with links)
.\breaking-change-doc.ps1

# Create issues directly
.\breaking-change-doc.ps1 -CreateIssues

# Data collection only
.\breaking-change-doc.ps1 -CollectOnly

# Single PR mode
.\breaking-change-doc.ps1 -PRNumber 123456

# Clean start
.\breaking-change-doc.ps1 -CleanStart

# Help
.\breaking-change-doc.ps1 -Help
```

## Configuration

Edit `config.ps1` to customize:
- **LLM provider**: GitHub Models, OpenAI, Anthropic, Azure OpenAI
- **Search parameters**: Date ranges, labels, excluded milestones
- **Output settings**: Labels, assignees, notification emails

## LLM Providers

**GitHub Models** (recommended - no API key needed):
```powershell
gh extension install github/gh-models
# Set provider in config.ps1: LlmProvider = "github-models"
```

**OpenAI**:
```powershell
$env:OPENAI_API_KEY = "your-key"
# Set provider in config.ps1: LlmProvider = "openai"
```

**Anthropic Claude**:
```powershell
$env:ANTHROPIC_API_KEY = "your-key"
# Set provider in config.ps1: LlmProvider = "anthropic"
```

**Azure OpenAI**:
```powershell
$env:AZURE_OPENAI_API_KEY = "your-key"
# Configure endpoint in config.ps1: LlmProvider = "azure-openai"
```

## Output

- **Data Collection**: `(repoRoot)\artifacts\docs\breakingChanges\data\summary_report.md`, `(repoRoot)\artifacts\docs\breakingChanges\data\pr_*.json`
- **Issue Drafts**: `(repoRoot)\artifacts\docs\breakingChanges\issue-drafts\*.md`
- **Comment Drafts**: `(repoRoot)\artifacts\docs\breakingChanges\comment-drafts\*.md`
- **GitHub Issues**: Created automatically (unless -DryRun)

## Workflow Steps

1. **Fetch PRs** - Downloads PR data from dotnet/runtime with comprehensive details
2. **Version Detection** - Analyzes GitHub tags to determine accurate .NET version information
3. **Template & Examples** - Fetches latest issue template and analyzes recent breaking change issues
4. **AI Analysis** - Generates high-quality breaking change documentation using AI
5. **Create Issues/Comments** - Adds comments with issue creation links or creates issues directly

## Version Detection

The script automatically determines accurate .NET version information using the local git repository:
- **Fast and reliable**: Uses `git describe` commands on the repository
- **No API rate limits**: Avoids GitHub API calls for version detection
- **Accurate timing**: Analyzes actual commit ancestry and tag relationships
- **Merge commit analysis**: For merged PRs, finds the exact merge commit and determines version context
- **Branch-aware**: For unmerged PRs, uses target branch information

## Manual Review

AI generates 90%+ ready documentation, but review for:
- Technical accuracy
- API completeness
- Edge cases

## Cleanup

Between runs:
```powershell
.\breaking-change-doc.ps1 -CleanStart
```

## Troubleshooting

**GitHub CLI**: `gh auth status` and `gh auth login`
**API Keys**: Verify environment variables are set for non-GitHub Models providers
**Rate Limits**: Use -DryRun for testing, script includes delays
**Git Operations**: Ensure git is in PATH and repository is up to date (`git fetch --tags`)
