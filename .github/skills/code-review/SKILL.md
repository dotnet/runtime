---
name: code-review
description: 'Review code changes in dotnet/runtime for correctness, performance, safety, API design, testing, and consistency with project conventions. Use when reviewing a PR or diff, critiquing or giving feedback on a change, checking code before submitting, or when asked to "review my PR", "review this code", "look over these changes", or "is this ready to merge". Loads area-specific rule references on demand.'
---

# dotnet/runtime Code Review

Review code changes against conventions and patterns established by dotnet/runtime maintainers. These rules were extracted from 43,000+ maintainer review comments across 6,600+ PRs and represent the actual standards enforced in practice.

**Reviewer mindset:** Be polite but very skeptical. Your job is to help speed the review process for maintainers, which includes not only finding problems the PR author may have missed but also questioning the value of the PR in its entirety. Treat the PR description and linked issues as claims to verify, not facts to accept. Question the stated direction, probe edge cases, and don't hesitate to flag concerns even when unsure.

## When to Use This Skill

Use this skill when:
- Reviewing a PR or code change in dotnet/runtime
- Checking code for correctness, performance, style, or consistency issues before submitting a PR
- Asked to review, critique, or provide feedback on code changes
- Validating that a change follows dotnet/runtime conventions

## Review Process

### Step 0: Load Relevant Instructions

Before analyzing anything, load any and all instructions under `.github/instructions` that are relevant to the code changes, as indicated by the frontmatter. If conflict arises between said custom instructions and the instructions in this skill, the custom instructions supersede instructions in this skill.

### Step 1: Gather Code Context (No PR Narrative Yet)

Before analyzing anything, collect as much relevant **code** context as you can. **Critically, do NOT read the PR description, linked issues, or existing review comments yet.** You must form your own independent assessment of what the code does, why it might be needed, what problems it has, and whether the approach is sound — before being exposed to the author's framing. Reading the author's narrative first anchors your judgment and makes you less likely to find real problems.

1. **Diff and file list**: Fetch the full diff and the list of changed files.
2. **Full source files**: For every changed file, read the **entire source file** (not just the diff hunks). You need the surrounding code to understand invariants, locking protocols, call patterns, and data flow. Diff-only review is the #1 cause of false positives and missed issues.
3. **Consumers and callers**: If the change modifies a public/internal API, a type that others depend on, or a virtual/interface method, search for how consumers use the functionality. Grep for callers, usages, and test sites. Understanding how the code is consumed reveals whether the change could break existing behavior or violate caller assumptions.
4. **Sibling types and related code**: If the change fixes a bug or adds a pattern in one type, check whether sibling types (e.g., other abstraction implementations, other collection types, platform-specific variants) have the same issue or need the same fix. Fetch and read those files too.
5. **Key utility/helper files**: If the diff calls into shared utilities, read those to understand the contracts (thread-safety, idempotency, etc.).
6. **Git history**: Check recent commits to the changed files (`git log --oneline -20 -- <file>`). Look for related recent changes, reverts, or prior attempts to fix the same problem. This reveals whether the area is actively churning, whether a similar fix was tried and reverted, or whether the current change conflicts with recent work.
7. **Detect new public API surface**: Check whether the PR introduces new public API surface. Look for:
   - Changes to `ref/` assembly source files (the strongest signal — these define the public API contract)
   - New `public` members (methods, properties, types, enum values) in `src/` files
   - Note whether new public API was detected. If it was, you **MUST** load and execute the API approval verification procedure during Step 4. Read the file `.github/skills/code-review/api-approval-check.md` (relative to the repository root) and follow its instructions. Do not skip this step — it is blocking.
  
### Step 2: Discover Area-Specific Agents
- Study **review** agents available in  `.github/agents` folder that are capable of reviewing specific areas of the codebase. Their yaml frontmatter description tells when they apply.
- When performing the review, invoke sub-agents to perform those area-specific reviews as subtasks during all subsequent steps, integrating those results.
- Depending on the PR, more subagents might be launched. Launch them in parallel. Always continue regular review described here as well - the subagents are addons, not replacements.

### Step 3: Form an Independent Assessment

Based **only** on the code context gathered above (without the PR description or issue), answer these questions:

1. **What does this change actually do?** Describe the behavioral change in your own words by reading the diff and surrounding code. What was the old behavior? What is the new behavior?
2. **Why might this change be needed?** Infer the motivation from the code itself. What bug, gap, or improvement does it appear to address?
3. **Is this the right approach?** Would a simpler alternative be more consistent with the codebase? Could the goal be achieved with existing functionality? Are there correctness, performance, or safety concerns?
4. **What problems do you see?** Identify bugs, edge cases, missing validation, thread-safety issues, performance regressions, API design problems, test gaps, and anything else that concerns you.

Write down your independent assessment before proceeding. You must produce a holistic assessment (see [Holistic PR Assessment](#holistic-pr-assessment)) at this stage.

### Step 4: Incorporate PR Narrative and Reconcile

Now read the PR description, labels, linked issues (in full), author information, existing review comments, and any related open issues in the same area. Treat all of this as **claims to verify**, not facts to accept.

1. **PR metadata**: Fetch the PR description, labels, linked issues, and author. Read linked issues in full — they often contain the repro, root cause analysis, and constraints the fix must satisfy.
2. **Related issues**: Search for other open issues in the same area (same labels, same component). This can reveal known problems the PR should also address, or constraints the author may not be aware of.
3. **Existing review comments**: Check if there are already review comments on the PR to avoid duplicating feedback.
4. **Reconcile your assessment with the author's claims.** Where your independent reading of the code disagrees with the PR description or issue, investigate further — but do not simply defer to the author's framing. If the PR claims a bug fix, a performance improvement, or a behavioral correction, verify those claims against the code and any provided evidence. If your independent assessment found problems the PR narrative doesn't acknowledge, those problems are more likely to be real, not less.
5. **Update your holistic assessment** if the additional context reveals information that genuinely changes your evaluation (e.g., a linked issue proves the bug is real, or an existing review comment already identified the same concern). But do not soften findings just because the PR description sounds reasonable.
6. **API Approval Verification.** If Step 1 detected new public API surface, you **MUST** now load the file `.github/skills/code-review/api-approval-check.md` (relative to the repository root) and execute the full procedure described there. Use the `view` tool, `cat`, or equivalent to read the file contents into your context, then follow every step. This is a **blocking** gate — if any check in that procedure fails, the review verdict must be ❌ Reject or ❌ Needs Changes regardless of other findings. Do not proceed without completing this step when new public API is detected. **If the file cannot be loaded for any reason**, report ❌ error — "Unable to load API approval verification procedure; cannot verify new public API surface" — and set the verdict to ❌ Needs Changes.

### Step 5: Detailed Analysis

**Load the relevant rule references first.** Based on what the PR changes, load the applicable reference file(s) from the [Rule Reference Index](#rule-reference-index) before flagging issues — they hold the specific dotnet/runtime conventions. Then apply these principles:

1. **Focus on what matters.** Prioritize bugs, performance regressions, safety issues, race conditions, resource management problems, incorrect assumptions about data or state, and API design problems. Do not comment on trivial style issues unless they violate an explicit rule in the loaded references.
2. **Consider collateral damage.** For every changed code path, actively brainstorm: what other scenarios, callers, or inputs flow through this code? Could any of them break or behave differently after this change? If you identify any plausible risk — even one you can't fully confirm — surface it so the author can evaluate. Do not dismiss behavioral changes because you believe the fix justifies them. The tradeoff is the author's decision — your job is to make it visible.
3. **Be specific and actionable.** Every comment should tell the author exactly what to change and why. Reference the relevant convention. Include evidence of how you verified the issue is real, e.g., "looked at all callers and none of them validate this parameter".
4. **Flag severity clearly:**
   - ❌ **error** — Must fix before merge. Bugs, security issues, API violations, test gaps for behavior changes.
   - ⚠️ **warning** — Should fix. Performance issues, missing validation, inconsistency with established patterns.
   - 💡 **suggestion** — Consider changing. Style improvements, minor readability wins, optional optimizations.
5. **Don't pile on.** If the same issue appears many times, flag it once on the primary file with a note listing all affected files. Do not leave separate comments for each occurrence.
6. **Respect existing style.** When modifying existing files, the file's current style takes precedence over general guidelines.
7. **Don't flag what CI catches.** Do not flag issues that a linter, typechecker, compiler, analyzer, or CI build step would catch, e.g., missing usings, unsupported syntax, formatting. Assume CI will run separately.
8. **Avoid false positives.** Before flagging any issue:
   - **Verify the concern actually applies** given the full context, not just the diff. Open the surrounding code to check. Confirm the issue isn't already handled by a caller, callee, or wrapper layer before claiming something is missing.
   - **Skip theoretical concerns with negligible real-world probability.** "Could happen" is not the same as "will happen."
   - **If you're unsure, either investigate further until you're confident, or surface it explicitly as a low-confidence question rather than a firm claim.** Do not speculate about issues you have no concrete basis for. Every comment should be worth the reader's time.
   - **Trust the author's context.** The author knows their codebase. If a pattern seems odd but is consistent with the repo, assume it's intentional.
   - **Never assert that something "does not exist," "is deprecated," or "is unavailable" based on training data alone.** Your knowledge has a cutoff date. When uncertain, ask rather than assert.
9. **Ensure code suggestions are valid.** Any code you suggest must be syntactically correct and complete. Ensure any suggestion would result in working code.
10. **Label in-scope vs. follow-up.** Distinguish between issues the PR should fix and out-of-scope improvements. Be explicit when a suggestion is a follow-up rather than a blocker.

## Multi-Model Review

When the environment supports launching sub-agents with different models (e.g., the `task` tool with a `model` parameter), run the review in parallel across multiple model families to get diverse perspectives. Different models catch different classes of issues. If the environment does not support this, proceed with a single-model review.

**How to execute (when supported):**
1. Inspect the available model list and select models from 2-3 distinct model families, up to 3 sub-agent models total. If fewer than 2 eligible families are available, use what is available. **Model selection rules:**
   - Pick only from models explicitly listed as available in the environment. Do not guess or assume model names.
   - From each selected family, pick the model with the highest capability tier (prefer "premium" or "standard" over "fast/cheap").
   - Never pick models labeled "mini", "fast", or "cheap" for code review.
   - Do not select the same model that is already running the primary review (i.e., your own model). The goal is diverse perspectives from different model families.
   - **Do not use `gpt-5.4`** — it has known reliability issues causing sub-agent timeouts in >90% of affected runs. For the OpenAI/GPT family, prefer `gpt-5.3-codex` if it is explicitly listed as available; otherwise, fall back to the highest-version non-blocked GPT model that satisfies the other rules here.
   - If multiple standard-tier models exist in the same family (excluding blocked models above), pick the one with the highest version number. Prefer "-codex" variants over general-purpose for code review tasks.
2. Launch a sub-agent for each selected model in parallel, giving each the same review prompt: the PR diff, the review rules from this skill, and instructions to produce findings in the severity format defined above.
3. Wait for all agents to complete, then synthesize: deduplicate findings that appear across models, elevate issues flagged by multiple models (higher confidence), and include unique findings from individual models that meet the confidence bar. **Timeout handling:** If a sub-agent has not completed after 10 minutes and you have results from other agents, proceed with the results you have. Do not block the review indefinitely waiting for a single slow model. Note in the output which models contributed.
4. Present a single unified review to the user, noting when an issue was flagged by multiple models. **After posting the review, immediately exit.** Do not wait for any remaining sub-agents. Do not attempt retries if the comment was posted successfully. The review is complete once the post operation succeeds or returns a comment URL.


---

## Review Output Format

When presenting the final review (whether as a PR comment or as output to the user), use the following structure. This ensures consistency across reviews and makes the output easy to scan.

> 📝 **AI-generated content disclosure:** When posting review content to GitHub (PR review comments, PR comments) under a user's credentials — i.e., the account is **not** a dedicated "copilot" or "bot" account/app (e.g., `github-actions[bot]`, `copilot`) — you **MUST** include a concise, visible note (e.g. a `> [!NOTE]` alert) at the bottom of the content indicating the content was AI/Copilot-generated. Skip this if the user explicitly asks you to omit it.

### Structure

```
## Copilot Code Review

### Holistic Assessment

**Motivation**: <1-2 sentences on whether the PR is justified and the problem is real>

**Approach**: <1-2 sentences on whether the fix/change takes the right approach>

**Summary**: <✅ LGTM / ⚠️ Needs Human Review / ⚠️ Needs Changes / ❌ Reject>. <2-3 sentence summary of the overall verdict and key points. If "Needs Human Review," explicitly state which findings you are uncertain about and what a human reviewer should focus on.>

---
<details>
  <summary>Detailed Findings</summary>

### Detailed Findings

#### ✅/⚠️/❌ <Category Name> — <Brief description>

<Explanation with specifics. Reference code, line numbers, interleavings, etc.>

(Repeat for each finding category. Group related findings under a single heading.)

</details>

<!-- AI disclosure note: place any AI-generated content disclosure below this line. -->
<!-- Example: > [!NOTE] This review was created by GitHub Copilot. -->
```

### Guidelines

- **Holistic Assessment** comes first and covers Motivation, Approach, and Summary.
- **Detailed Findings** uses emoji-prefixed category headers:
  - ✅ for things that are correct / look good (use to confirm important aspects were verified)
  - ⚠️ for warnings or impactful suggestions (should fix, or follow-up)
  - ❌ for errors (must fix before merge)
  - 💡 for minor suggestions or observations (nice-to-have)
- **Cross-cutting analysis** should be included when relevant: check whether related code (sibling types, callers, other platforms) is affected by the same issue or needs a similar fix.
- **Test quality** should be assessed as its own finding when tests are part of the PR.
- **Summary** gives a clear verdict: LGTM (no blocking issues — use only when confident), Needs Human Review (code may be correct but you have unresolved concerns or uncertainty that require human judgment), Needs Changes (with blocking issues listed), or Reject (explaining why this should be closed outright). **Never give a blanket LGTM when you are unsure.** When in doubt, use "Needs Human Review" and explain what a human should focus on.
- Keep the review concise but thorough. Every claim should be backed by evidence from the code.

### Verdict Consistency Rules

The summary verdict **must** be consistent with the findings in the body. Follow these rules:

1. **The verdict must reflect your most severe finding.** If you have any ⚠️ findings, the verdict cannot be "LGTM." Use "Needs Human Review" or "Needs Changes" instead. Only use "LGTM" when all findings are ✅ or 💡 and you are confident the change is correct and complete.

2. **When uncertain, always escalate to human review.** If you are unsure whether a concern is valid, whether the approach is sufficient, or whether you have enough context to judge, the verdict must be "Needs Human Review" — not LGTM. Your job is to surface concerns for human judgment, not to give approval when uncertain. A false LGTM is far worse than an unnecessary escalation.

3. **Separate code correctness from approach completeness.** A change can be correct code that is an incomplete approach. If you believe the code is right for what it does but the approach is insufficient (e.g., treats symptoms without investigating root cause, silently masks errors that should be diagnosed, fixes one instance but not others), the verdict must reflect the gap — do not let "the code itself looks fine" collapse into LGTM.

4. **Classify each ⚠️ and ❌ finding as merge-blocking or advisory.** Before writing your summary, decide for each finding: "Would I be comfortable if this merged as-is?" If any answer is "no," the verdict must be "Needs Changes." If any answer is "I'm not sure," the verdict must be "Needs Human Review."

5. **Devil's advocate check before finalizing.** Re-read all your ⚠️ findings. For each one, ask: does this represent an unresolved concern about the approach, scope, or risk of masking deeper issues? If so, the verdict must reflect that tension. Do not default to optimism because the diff is small or the code is obviously correct at a syntactic level.

---

## Holistic PR Assessment

Before reviewing individual lines of code, evaluate the PR as a whole. Consider whether the change is justified, whether it takes the right approach, and whether it will be a net positive for the codebase.

### Motivation & Justification

- **Every PR must articulate what problem it solves and why.** Don't accept vague or absent motivation. Ask "What's the rationale?" and block progress until the contributor provides a clear answer.
- **Challenge every addition with "Do we need this?"** New code, APIs, abstractions, and flags must justify their existence. If an addition can be avoided without sacrificing correctness or meaningful capability, it should be.
- **Demand real-world use cases and customer scenarios.** Hypothetical benefits are insufficient motivation for expanding API surface area or adding features. Require evidence that real users need this.

### Evidence & Data

- **Require measurable performance data before accepting optimization PRs.** Demand BenchmarkDotNet results or equivalent proof — never accept performance claims at face value.
- **Distinguish real performance wins from micro-benchmark noise.** Trivial benchmarks with predictable inputs overstate gains from jump tables, branch elimination, and similar tricks. Require evidence from realistic, varied inputs.
- **Investigate and explain regressions before merging.** Even if a PR shows a net improvement, regressions in specific scenarios must be understood and explicitly addressed — not hand-waved.

### Approach & Alternatives

- **Check whether the PR solves the right problem at the right layer.** Look for whether it addresses root cause or applies a band-aid. Prefer fixing the actual source of an issue over adding workarounds to production code.
- **When a PR takes a fundamentally wrong approach, redirect early.** Don't iterate on implementation details of a flawed design. Push back on the overall direction before the contributor invests more time.
- **Ask "Why not just X?" — always prefer the simplest solution.** When a PR uses a complex approach, challenge it with the simplest alternative that could work. The burden of proof is on the complex solution.

### Cost-Benefit & Complexity

- **Explicitly weigh whether the change is a net positive.** A performance trade-off that shifts costs around is not automatically beneficial. Demand clarity that the change is a win in the typical configuration, not just in a narrow scenario.
- **Reject overengineering — complexity is a first-class cost.** Unnecessary abstraction, extra indirections, and elaborate solutions for marginal gains are actively rejected.
- **Every addition creates a maintenance obligation.** Long-term maintenance cost outweighs short-term convenience. Code that is hard to maintain, increases surface area, or creates technical debt needs stronger justification.

### Scope & Focus

- **Require large or mixed PRs to be split into focused changes.** Each PR should address one concern. Mixed concerns make review harder and increase regression risk.
- **Defer tangential improvements to follow-up PRs.** Police scope creep by asking contributors to separate concerns. Even good ideas should wait if they're not part of the PR's core purpose.

### Risk & Compatibility

- **Flag breaking changes and require formal process.** Any behavioral change that could affect downstream consumers needs documentation, API review, and explicit approval — even when the change improves the codebase internally.
- **Assess regression risk proportional to the change's blast radius.** High-risk changes to stable code need proportionally higher value and more thorough validation.

### Codebase Fit & History

- **Ensure new code matches existing patterns and conventions.** Deviations from established patterns create confusion and inconsistency. If a rename or restructuring is warranted, do it uniformly in a dedicated PR — not piecemeal.
- **Check whether a similar approach has been tried and rejected before.** If a prior attempt didn't work, require a clear explanation of what's different this time.

## Rule Reference Index

The detailed dotnet/runtime review conventions live in reference files under [`references/`](./references/). They were extracted from 43,000+ maintainer review comments across 6,600+ PRs and represent the standards enforced in practice. **During [Step 4](#step-4-detailed-analysis), load only the reference file(s) relevant to what the PR changes** — most reviews need two or three. When in doubt, load more rather than fewer.

| If the PR involves... | Load |
|---|---|
| Error handling, thread safety, security, general or JIT correctness (**most PRs**) | [correctness-and-safety.md](./references/correctness-and-safety.md) |
| Hot paths, allocations, benchmarks, or any performance claim | [performance.md](./references/performance.md) |
| New or changed public/internal API surface | [api-design.md](./references/api-design.md) |
| Naming, formatting, or language idioms (**most PRs**) | [code-style.md](./references/code-style.md) |
| Refactors, code reuse, conventions, PR hygiene, runtime-specific patterns | [codebase-consistency.md](./references/codebase-consistency.md) |
| Any bug fix or behavioral change (test requirements) | [testing.md](./references/testing.md) |
| Comments, XML docs, or breaking-change documentation | [documentation.md](./references/documentation.md) |
| Native C/C++, interop, P/Invoke, cross-platform, or intrinsics | [platform-and-native.md](./references/platform-and-native.md) |

When new **public API** surface is detected, also execute the blocking [API approval verification](./api-approval-check.md) procedure (see [Step 0](#step-0-gather-code-context-no-pr-narrative-yet) and [Step 3](#step-3-incorporate-pr-narrative-and-reconcile)).
