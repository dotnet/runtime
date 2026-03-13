# Configuration for Breaking Change Documentation Workflow

$Config = @{
    # LLM Settings
    LlmProvider = "github-models"       # openai, anthropic, azure-openai, github-models, github-copilot
    LlmModel = "openai/gpt-4o"          # For GitHub Models: openai/gpt-4o, openai/gpt-4o-mini, microsoft/phi-4, etc.
                                        # For Azure OpenAI: deployment name (e.g., "gpt-4o", "gpt-35-turbo")
    LlmApiKey = $null                   # Uses environment variables by default (not needed for github-models or github-copilot)
    LlmBaseUrl = $null                  # For Azure OpenAI: https://your-resource.openai.azure.com
    AzureApiVersion = "2024-02-15-preview"  # Azure OpenAI API version (optional, defaults to 2024-02-15-preview)

    # GitHub Settings
    SourceRepo = "dotnet/runtime"
    DocsRepo = "dotnet/docs"
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
