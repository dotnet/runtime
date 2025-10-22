# Configuration for Breaking Change Documentation Workflow

$Config = @{
    # LLM Settings
    LlmProvider = "github-models"       # openai, anthropic, azure-openai, github-models
    LlmModel = "openai/gpt-4o"          # For GitHub Models: openai/gpt-4o, openai/gpt-4o-mini, microsoft/phi-4, etc.
    LlmApiKey = $null                   # Uses environment variables by default (not needed for github-models)
    LlmBaseUrl = $null                  # For Azure OpenAI: https://your-resource.openai.azure.com

    # GitHub Settings
    SourceRepo = "dotnet/runtime"
    DocsRepo = "dotnet/docs"
    LocalRepoPath = "c:\src\dotnet\runtime"  # Path to local clone of the runtime repository
    TargetLabel = "needs-breaking-change-doc-created"
    IssueTemplatePath = ".github/ISSUE_TEMPLATE/02-breaking-change.yml"  # Path to issue template in DocsRepo

    # Analysis Settings
    MaxPRs = 100

    # Output Settings
    IssueTemplate = @{
        Labels = @("breaking-change", "Pri1", "doc-idea")
        Assignee = "gewarren"
        NotificationEmail = "dotnetbcn@microsoft.com"
    }

    # Rate Limiting
    RateLimiting = @{
        DelayBetweenCalls = 2          # seconds
        DelayBetweenIssues = 3         # seconds
    }
}
