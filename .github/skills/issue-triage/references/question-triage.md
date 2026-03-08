# Question / Support Request Triage

Guidance for triaging issues classified as questions or support requests in
dotnet/runtime. Referenced from the main [SKILL.md](../SKILL.md) during Step 5.

For issues that are questions rather than bugs or feature requests, attempt to
provide a helpful answer before recommending closure. This adds value beyond
just closing the issue — the author gets an answer, and the response demonstrates
that the issue was thoughtfully reviewed.

## How to Answer

1. **Research the question** — Search the .NET documentation, API reference, and
   existing GitHub issues/discussions for relevant information.
2. **Draft an answer** — Write a clear, concise answer with code examples where
   appropriate.
3. **Assess your confidence** in the answer:
   - **High confidence** — The answer is based on well-documented behavior, you've
     used this API before, or the docs clearly cover this scenario.
   - **Low confidence** — The answer is based on inference, you're unsure about
     edge cases, or the behavior isn't clearly documented.

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

Questions should typically be recommended as **CLOSE** (convert to Discussion)
or **NEEDS INFO** (if the question is unclear). Include the answer in the
suggested response for either outcome.
