# Question / Support Request Triage

Guidance for triaging issues classified as questions or support requests in
dotnet/runtime. Referenced from the main [SKILL.md](../SKILL.md) during Step 5.

For issues that are questions rather than bugs or feature requests, attempt to
provide a helpful answer before recommending closure. This adds value beyond
just closing the issue -- the author gets an answer, and the response demonstrates
that the issue was thoughtfully reviewed.

## Classify the Question

Questions in dotnet/runtime fall into several subcategories. Identifying the
subcategory helps determine the best research strategy and response format.

| Subcategory | Indicators | Examples |
|-------------|-----------|----------|
| **API usage** | Asks how to use a specific API, what parameters to pass, or what return values to expect | "How do I configure JsonSerializerOptions for case-insensitive matching?" |
| **Behavior explanation** | Asks why an API behaves a certain way, or whether observed behavior is by-design | "Why does HttpClient throw TaskCanceledException on timeout instead of TimeoutException?" |
| **Migration / upgrade** | Asks how to migrate from .NET Framework or an older .NET version, or how to handle breaking changes | "How do I replace BinaryFormatter in .NET 8?", "What's the .NET 8 equivalent of X?" |
| **Configuration / setup** | Asks about runtime configuration, environment variables, or deployment settings | "How do I set the GC mode?", "What's the correct way to configure System.Globalization invariant mode?" |
| **Best practices** | Asks for guidance on the recommended approach to a problem, or which API to choose | "Should I use Span or Memory here?", "What's the best way to parse large JSON files?" |
| **Debugging help** | Asks for help diagnosing a problem in their own code rather than reporting a product issue | "My app crashes with this stack trace -- what's wrong?" |

## Check for Hidden Bugs

Before treating an issue as a pure question, check whether it reveals an
actual product issue. These patterns indicate the issue may be a bug
masquerading as a question:

- **"Why does X throw Y?"** -- If the answer is "it shouldn't," the issue is
  a bug. Check whether the described behavior matches the API's documented
  contract.
- **"Is this behavior expected?"** -- If the behavior contradicts the API docs
  or is inconsistent with similar APIs, flag it as a potential bug.
- **"This worked in .NET X but not .NET Y"** -- This is a regression report,
  not a question. Reclassify as a bug and follow the bug triage guide.
- **"The documentation says X but I observe Y"** -- If the docs are correct and
  the behavior is wrong, it's a bug. If the behavior is correct and the docs
  are wrong, it's a documentation issue (redirect to dotnet/dotnet-api-docs or
  dotnet/docs).
- **Version-specific behavior** -- If the question involves behavior that
  changed between versions and the change wasn't documented as a breaking
  change, it may be an unintentional regression.

When a hidden bug is detected, reclassify the issue in your triage report:
flag the mislabeling in Step 2 and follow the bug triage guide for Step 5
instead.

## How to Answer

1. **Research the question** -- Search the .NET documentation, API reference, and
   existing GitHub issues/discussions for relevant information.
2. **Draft an answer** -- Write a clear, concise answer with code examples where
   appropriate.
3. **Assess your confidence** in the answer:
   - **High confidence** -- The answer is based on well-documented behavior, you've
     used this API before, or the docs clearly cover this scenario.
   - **Low confidence** -- The answer is based on inference, you're unsure about
     edge cases, or the behavior isn't clearly documented.

### Answer strategies by subcategory

| Subcategory | Research strategy | Response format |
|-------------|-------------------|-----------------|
| **API usage** | Check API reference docs, search for usage examples in dotnet/runtime tests or samples | Code example showing correct usage |
| **Behavior explanation** | Read the source code for the relevant API, check for design docs or comments explaining the behavior | Explanation of why the behavior exists, with a link to relevant source or docs |
| **Migration / upgrade** | Check breaking changes docs, migration guides, and the compat label on issues | Step-by-step migration instructions, or a link to the relevant migration guide |
| **Configuration / setup** | Check runtime configuration docs, search for related environment variables | Configuration example with explanation of what each setting controls |
| **Best practices** | Check performance guidelines, API design guidelines, and existing patterns in the BCL | Comparison of approaches with trade-offs, recommending the most appropriate one |
| **Debugging help** | Analyze the stack trace or error message to identify the likely cause | Explanation of the probable issue with their code, with a corrected example if possible |

## Verify Low-Confidence Answers

If your confidence in the answer is **low**, verify it by running a test:

1. Create a temporary directory
2. `dotnet new console -n TriageAnswer`
3. Write a small program that demonstrates the answer
4. `dotnet run` to verify the answer produces the expected result
5. Include the verified output in the triage report
6. Clean up the temporary directory when done

If the answer cannot be verified (e.g., requires specific environment, external
services, or complex setup), note that in the report.

## Include the Answer in the Triage Report

Regardless of recommendation (CLOSE as question → discussion, or NEEDS INFO),
include the answer in the suggested response. This way the author gets help even
if the issue is closed.

## Question-Specific Recommendation Criteria

### CLOSE (most common)

Questions should typically be recommended as **CLOSE** with a suggestion to
convert to a [GitHub Discussion](https://github.com/dotnet/runtime/discussions).
Include the answer in the suggested response so the author gets immediate value.

### NEEDS INFO

Use when the question is too vague to answer. Ask the author to clarify:
- What specific API or behavior they're asking about
- What .NET version they're using
- What they've already tried

### KEEP (rare)

In rare cases, a question may reveal a genuine documentation gap or a
confusing API surface that warrants improvement. If the question is one that
many developers would ask and the current docs/API don't adequately address
it, recommend KEEP with a suggestion to improve the documentation or API
ergonomics.
