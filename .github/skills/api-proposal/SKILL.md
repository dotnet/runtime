---
name: api-proposal
description: Create prototype backed API proposals for dotnet/runtime. Use when asked to draft an API proposal or to refine a vague API idea into a complete proposal.
---

# API Proposal Skill

Create complete, terse, and empirically grounded API proposals for dotnet/runtime. The output should have a high chance of passing the [API review process](https://github.com/dotnet/runtime/blob/main/docs/project/api-review-process.md).

> 🚨 **NEVER** submit a proposal without a working prototype. The prototype is the evidence that the API works. Proposals without prototypes are speculative.

> 🚨 **NEVER** use `gh pr review --approve` or `--request-changes`. Only `--comment` is allowed.

## When to Use This Skill

Use this skill when:
- Asked to propose a new API for dotnet/runtime
- Given a vague API idea or incomplete sketch that needs to be turned into a complete proposal
- Given an existing underdeveloped `api-suggestion` issue to refine
- Asked to prototype an API and draft a proposal
- Asked to "write an API proposal", "draft an api-suggestion", or "improve this proposal"

## Core Principles

1. **TERSENESS**: Proposals are reviewed live by humans during API review meetings who often lack prior context. Long text is counterproductive unless warranted by design complexity. Focus on WHAT problem and HOW to solve it.

2. **Empirically grounded**: Build and test a working prototype BEFORE writing the proposal. The prototype validates the design, surfaces edge cases, and produces the exact API surface via ref source generation.

3. **Claims backed by evidence**: Every motivating claim must have at least one concrete scenario. "This is useful" without showing *who* needs it and *how* they'd use it is the #1 reason proposals get sent back.

4. **Context-driven depth**: The amount of supporting text should be proportional to how much **new information** the proposal introduces — not just API surface size. A small API introducing a novel concept needs more justification than a large API adding async counterparts to existing sync methods.

## Modular Phases

This skill has 6 phases. Each can run independently (e.g., "just draft the proposal from my existing prototype"). When running the full pipeline, execute in order.

---

### Phase 0: Gather Input & Assess Context

1. **Accept input** in any form: issue URL, text description, API sketch, or vague idea.

2. **If the input is an existing GitHub issue**, read it in full and identify:
   - What sections are missing or underdeveloped
   - Whether the proposed API surface is concrete or still vague
   - Any reviewer feedback in comments (especially if `api-needs-work` label is present)

3. **Identify the target**: namespace, area label, affected types, target library under `src/libraries/`.

4. **Assess novelty**: Is this a well-understood pattern extension (async variant, new overload, casing option) or something introducing a novel concept? This determines the depth of the proposal.

5. **Evaluate existing workarounds**: Before proceeding, research what users can do TODAY without this API.
   - Present the workaround(s) to the user for evaluation
   - Explain trade-offs: performance penalty? excessive boilerplate? bad practices?
   - **This is a checkpoint**: If a workaround is acceptable, the user may decide to shelve the proposal
   - Only proceed to prototyping if workarounds are genuinely insufficient

6. **Search for prior proposals** on the same topic:
   - Search dotnet/runtime issues for related proposals (e.g., `api-suggestion`, `api-needs-work`, or closed issues)
   - If duplicates exist, surface them — don't block work, but note them for linking later
   - Look for clues in reviewer feedback: what caused a prior proposal to be marked `api-needs-work`? Why was it closed or stalled? Learn from that history to avoid repeating the same mistakes

7. Ask clarifying questions if the proposal is too vague to prototype.

---

### Phase 1: Research

The skill contains baked-in examples and guidelines for writing good proposals (see [references/proposal-examples.md](references/proposal-examples.md) and [references/api-proposal-checklist.md](references/api-proposal-checklist.md)). The agent does NOT need to search for `api-approved` issues as templates — the baked-in references are sufficient. Phase 0's search for *related* issues on the same topic is a separate concern and is still required.

**What the agent DOES at runtime:**

1. **Read the Framework Design Guidelines digest** at `docs/coding-guidelines/framework-design-guidelines-digest.md`. Validate that proposed names follow the conventions.

2. **Read existing APIs in the target namespace** to ensure consistency:
   - Naming patterns (e.g., `TryX` pattern, `XAsync` pattern, overload shapes)
   - Type hierarchies and interface implementations
   - Parameter ordering conventions

3. **Read the reference documentation for updating ref source** at `docs/coding-guidelines/updating-ref-source.md`.

---

### Phase 2: Prototype

> **If the user already has a prototype**, ask for the published branch link and skip to Phase 3.

1. Create a new branch: `api-proposal/<short-name>`. The prototype must be pushed as a **single commit** on this branch.

2. Implement the API surface with:
   - Complete triple-slash XML documentation on all public members
   - Proper `#if` guards for TFM-specific APIs

3. Write comprehensive tests:
   - Use `[Theory]` with `[InlineData]`/`[MemberData]` where applicable
   - Cover edge cases, null inputs, boundary conditions
   - Test any interaction with existing APIs

#### Prototype Validation (all steps required)

> **Prerequisite:** Follow the build and test workflow in [`copilot-instructions.md`](/.github/copilot-instructions.md) — complete the baseline build, configure the environment, and use the component-specific workflow for the target library. All build and test steps below assume the baseline build has already succeeded.

**Step 1: Build and test**

Build the src and test projects, then run all tests for the target library using the workflow described in `copilot-instructions.md`. All tests must pass with zero failures.

Building the test project separately is critical for detecting **source breaking changes** that ApiCompat won't catch:
- New overloads/extension methods causing wrong method binding in existing code
- New generic overloads causing overload resolution ambiguity
- Pay attention to compilation **warnings**, not just errors

**Step 2: Check TFM compatibility**

Inspect the library's `.csproj` for `TargetFrameworks`. If it ships netstandard2.0 or net462 artifacts:
- Verify the prototype compiles for ALL target frameworks, not just `$(NetCoreAppCurrent)`
- Ensure .NET Core APIs form a **superset** of netstandard/netfx APIs
- Use `#if` guards where types like `DateOnly`, `IParsable<T>` restrict parts of the surface to .NET Core
- Failure to maintain superset relationship risks breaking changes on upgrade/type-forward

**Step 3: Generate reference assembly source**

```bash
cd src/libraries/<LibraryName>/src
dotnet msbuild /t:GenerateReferenceAssemblySource
```

For System.Runtime, use `dotnet build --no-incremental /t:GenerateReferenceAssemblySource`.

This:
- Produces the **exact public API diff** to use in the proposal
- Validates that only intended APIs were added (no accidental public surface leakage)
- The `ref/` folder changes **must be committed** as part of the prototype

**The flow is**: vague input → working prototype → extract exact API surface from ref source → write the proposal. The prototype comes BEFORE the exact API proposal.

---

### Phase 3: Review (encapsulates code-review skill) — BLOCKING

1. Invoke the **code-review** skill against the prototype diff.

2. **All errors and warnings must be fixed** before proceeding to the draft phase.

3. If the API change could affect performance (hot paths, allocations, new collection types), suggest running the **performance-benchmark** skill.

4. Re-run tests after any review-driven changes to confirm nothing regressed.

---

### Phase 4: Draft Proposal

**Core principle: TERSENESS.** Focus on WHAT problem and HOW to solve it. Do not generate long text unless the design complexity warrants it.

Write the proposal matching the spirit of the [issue template](https://github.com/dotnet/runtime/blob/main/.github/ISSUE_TEMPLATE/02_api_proposal.yml). Skip inapplicable fields rather than filling them with "N/A".

#### Proposal Structure

**1. Background and motivation**

- WHAT concrete user problem are we solving? Show scenario(s).
- Reference prior art in other ecosystems where relevant.
- Briefly summarize existing workarounds and why they are insufficient, but do not repeat the full Phase 0 analysis. Keep this section focused on the problem and the high-level rationale for a new API.

**2. API Proposal**

The exact API surface, extracted from the `GenerateReferenceAssemblySource` output:

- **New self-contained types**: Clean declaration format (no diff markers). Example:

```csharp
namespace System.Collections.Generic;

public class PriorityQueue<TElement, TPriority>
{
    public PriorityQueue();
    public PriorityQueue(IComparer<TPriority>? comparer);
    public int Count { get; }
    public void Enqueue(TElement element, TPriority priority);
    public TElement Dequeue();
    // ...
}
```

- **Additions to existing types**: Prefer `csharp` blocks when the proposal only adds new members and doesn't need to show existing APIs for context. Mark the containing type `partial` to emphasize it has other public members:

```csharp
namespace System.Text.Json;

public partial class JsonNamingPolicy
{
    public static JsonNamingPolicy SnakeLowerCase { get; }
    public static JsonNamingPolicy SnakeUpperCase { get; }
}
```

When existing members ARE needed for context (e.g., to show sibling overloads), use `diff` blocks instead:

```diff
namespace System.Text.Json;

public partial class JsonNamingPolicy
{
     public static JsonNamingPolicy CamelCase { get; }
+    public static JsonNamingPolicy SnakeLowerCase { get; }
+    public static JsonNamingPolicy SnakeUpperCase { get; }
}
```

Rules:
- **No implementation code.** Ever.
- **No extensive XML docs.** Comments only as brief clarifications for the review board.
- Naming must follow the [Framework Design Guidelines](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/framework-design-guidelines-digest.md).

**3. API Usage**

Realistic, compilable code examples demonstrating the primary scenarios. Number and depth should match the novelty of the API, not just its size. A simple new overload may need one example; a new collection type may need several showing different use patterns.

**4. Design Decisions** (nontrivial only)

For any decision where reasonable alternatives exist, briefly explain the reasoning. Omit for self-evident decisions. List format works well:

- "Uses a quaternary heap instead of binary for better cache locality"
- "Does not implement `IEnumerable` because elements cannot be efficiently enumerated in priority order"

**5. Alternative Designs**

The agent has the burden of proof when claiming no viable alternatives exist. Show that alternatives were genuinely considered and explain why the proposed design is preferred.

**6. Risks**

The agent has the burden of proof when claiming absence of risks. Evaluate:
- Binary breaking changes (caught by ApiCompat)
- Source breaking changes (overload resolution, method binding)
- Performance implications
- TFM compatibility

**7. Open Questions** (if any)

List unresolved design questions with tentative answers. Surfacing uncertainty is a feature, not a weakness. Example from PriorityQueue:
- "Should we use `KeyValuePair` instead of tuples?"

**8. Scope considerations** (if applicable)

If the proposal could naturally extend to neighboring APIs (e.g., "should this also apply to `ToHashSet`?"), flag it as an open question.

**9. Related issues**

Link any related/duplicate proposals found during Phase 0 research.

**10. Prototype**

Link to the prototype commit (e.g., `https://github.com/<owner>/<repo>/commit/<sha>`).

#### After Drafting

Present the complete draft to the user for review. Iterate based on feedback before publishing.

---

### Phase 5: Publish

> **Agent disclaimer:** When publishing to GitHub on behalf of a user account, prepend the following disclaimer to the proposal body:
>
> ```markdown
> > [!NOTE]
> > This proposal was drafted with the help of an AI agent. Please review for accuracy and remove this notice once you're satisfied with the content.
> ```

#### Step 1: Push and capture commit URL

Commit prototype changes and push the branch to the user's fork (default) or ask for an alternative remote. Capture the commit URL for inclusion in the proposal (e.g., `https://github.com/<owner>/<repo>/commit/<sha>`).

#### Step 2: Non-interactive mode (Copilot Coding Agent)

If the agent cannot prompt the user for input (e.g., running as Copilot Coding Agent), automatically post the API proposal as a comment on the associated pull request:

```bash
gh pr comment <pr-number> --body-file proposal.md
```

Skip the interactive options below.

#### Step 3: Interactive mode — offer publishing options

Present the user with the following options. Which options appear depends on context:

> **Note:** Always write the proposal text to a temporary file (e.g., `proposal.md`) and use `--body-file` instead of `--body` to avoid shell quoting/escaping issues with multi-line text.

1. **Post as comment on existing issue/PR** — Only offer this when the user explicitly referenced an issue or PR in their original prompt.
   ```bash
   gh issue comment <number> --body-file proposal.md
   # or
   gh pr comment <number> --body-file proposal.md
   ```

2. **Create a new issue** — Always offer this option.
   ```bash
   gh issue create --label api-suggestion --title "[API Proposal]: <title>" --body-file proposal.md
   ```
   No area label — repo automation handles that.

3. **Create a new draft PR with proposal in OP** — Always offer this option.
   ```bash
   gh pr create --draft --title "[API Proposal]: <title>" --body-file proposal.md
   ```

Include related issue links in the body for all options.
