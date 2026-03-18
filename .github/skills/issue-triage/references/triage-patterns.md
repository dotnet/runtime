# Triage Response Patterns

Examples of maintainer response patterns observed in dotnet/runtime, distilled
into templates for each recommendation type.

## KEEP Responses

### Bug -- confirmed regression, high priority

> Thanks for reporting this. I was able to reproduce the issue on .NET {version}.
> This looks like a regression introduced in {version/commit}. Moving to the
> {milestone} milestone for further investigation.
>
> **Priority:** High -- confirmed regression affecting a common scenario.
> **Confidence:** High -- reproduced locally.

### Bug -- confirmed, normal priority

> Thanks for reporting this. I was able to reproduce the issue on .NET {version}.
> Moving to the {milestone} milestone for investigation.
>
> **Priority:** Normal -- valid bug with moderate impact.
> **Confidence:** High -- reproduced locally.

### Bug -- valid but low priority

> This is a valid bug, but the impact appears limited to {specific scenario}.
> Moving to Future milestone. Contributions welcome -- this would be a good
> `help wanted` candidate.
>
> **Priority:** Low -- edge case with adequate workaround.
> **Confidence:** Medium -- plausible from the description but could not independently reproduce.

### API Proposal -- promising, needs refinement

> Interesting proposal! The motivation makes sense, and {language/platform} has
> similar functionality via {reference}. A few things to consider before this
> moves to API review:
>
> - {Specific feedback on the proposed API shape}
> - {Edge cases or design questions}
>
> Once those are addressed, we can mark this `api-ready-for-review`.
>
> **Priority:** Normal -- well-motivated proposal.
> **Complexity:** M -- moderate API surface with some design questions to resolve.
> **Confidence:** High -- clear precedent in other ecosystems.

### API Proposal -- accepted direction

> This aligns with work we've been considering for {area}. The proposed API
> surface looks reasonable. Let's move this forward -- marking as
> `api-ready-for-review`.
>
> **Priority:** High -- aligns with planned work, high community demand.
> **Complexity:** L -- cross-cutting change touching {components}, will need API review.
> **Confidence:** High -- well-formed proposal with strong motivation.

### API Proposal -- clear gap, no existing workaround

> This addresses a real gap in the BCL. {Brief summary of the problem and why
> existing APIs don't cover it.} The proposed approach is feasible and doesn't
> introduce breaking changes.
>
> A few naming/design observations:
> - {Any FDG naming issues or API shape feedback, if applicable}
>
> The **api-proposal** Copilot skill can help refine this into a complete
> proposal with a working prototype when you're ready.
>
> **Priority:** Normal -- genuine gap with no adequate workaround.
> **Complexity:** {S|M|L} -- {rationale}.
> **Confidence:** {High|Medium} -- {rationale}.

### Enhancement -- valid, Future milestone

> Good idea. This would be a nice improvement to {component}. Moving to Future
> milestone. If you'd like to contribute a PR, we'd be happy to review it.
>
> **Priority:** Normal -- nice improvement with moderate impact.
> **Complexity:** S -- isolated change to one component.
> **Confidence:** Medium -- reasonable enhancement but impact is hard to gauge.

### Bug -- reproduced with minimal reproduction derived

> Thanks for reporting this. I was able to reproduce the issue on .NET {version}.
> I derived a minimal reproduction that isolates the problem:
>
> ```csharp
> {Minimal self-contained reproduction}
> ```
>
> Moving to the {milestone} milestone for investigation.
>
> **Priority:** {High|Normal|Low} -- {rationale}.
> **Confidence:** High -- reproduced and minimized locally.

### Performance regression -- bisect completed

> Thanks for reporting this regression. I was able to reproduce the performance
> degradation and bisected it to {commit SHA / PR link}:
>
> - **Regressing commit:** {SHA} ({PR title})
> - **Test/Base ratio:** {ratio} ({benchmark name})
> - **Root cause:** {1-2 sentence explanation of why the change caused the regression}
>
> Moving to the {milestone} milestone for investigation.
>
> **Priority:** High -- confirmed performance regression with identified culprit.
> **Confidence:** High -- bisect completed with statistical significance.

## CLOSE Responses

### Duplicate

> This is a duplicate of #{number}, which tracks the same {bug/feature request}.
> Closing in favor of that issue -- please add your +1 there to help us prioritize.

### Already fixed

> This issue has been addressed by {PR link / commit reference}. The fix is
> available in .NET {version}. If you're still seeing this behavior on the
> latest version, please let us know and we can reopen.

### Spam / off-topic

> Closing this issue as it does not appear to be a bug report or feature request
> related to the .NET runtime.

### Won't fix -- by design

> The behavior you're seeing is by design. {Brief explanation of why the current
> behavior is correct.} {Optional: link to documentation or design rationale.}
>
> If you have a scenario where this design causes problems, we'd be interested
> in hearing more in a new issue focused on that specific scenario.

### Won't fix -- maintenance burden / scope

> We appreciate the suggestion, but this falls outside the scope of what we want
> to maintain in the BCL. The .NET ecosystem has community packages that address
> this scenario. We generally prefer to keep the BCL focused on {foundational
> primitives / broad scenarios}.

### Wrong repo

> Thanks for filing this! However, this issue is better tracked in
> [{repo-name}]({repo-url}). That team owns {component} and will be able to
> help. Please re-file the issue there.
>
> Closing this issue, but feel free to open a new one if you believe there's
> a runtime-specific aspect we should address.

### Not reproducible

> We were unable to reproduce this issue on .NET {version}, {OS} {arch}. This
> may indicate the issue has been fixed in a recent version, or that it's
> specific to your environment.
>
> Could you please verify whether the issue still occurs on the latest .NET
> {version}? If it does, a minimal reproduction
> (ideally a small console app we can `dotnet run`) would help us investigate
> further. Feel free to reopen if you can provide additional details.

### API documentation -- transfer to dotnet/dotnet-api-docs

> Thanks for reporting this documentation issue! API reference docs are managed
> in the [`dotnet/dotnet-api-docs`](https://github.com/dotnet/dotnet-api-docs)
> repository. Please file this issue there so the docs team can address it.
>
> Closing this issue -- feel free to reopen if there's a runtime behavior aspect
> we should look at separately.

### Conceptual documentation -- transfer to dotnet/docs

> Thanks for the documentation feedback! Conceptual docs, tutorials, and how-to
> guides are managed in the [`dotnet/docs`](https://github.com/dotnet/docs)
> repository. Please file this issue there so the docs team can address it.
>
> Closing this issue -- feel free to reopen if there's a runtime behavior aspect
> we should look at separately.

### Question / support request -- convert to discussion

> This looks like a usage question rather than a bug report or feature request.
> We use [GitHub Discussions](https://github.com/dotnet/runtime/discussions) for
> Q&A -- you'll likely get a faster response from the community there.
>
> {Include answer if one was provided in the triage report.}
>
> Closing this issue, but feel free to reopen if you believe there's a bug here.

### Not actionable -- missing information

> We haven't been able to make progress on this issue due to missing
> information. If you can provide {specific missing info -- repro steps, .NET
> version, OS details}, please feel free to reopen or file a new issue.

### .NET Framework (not .NET Core)

> This issue appears to be about .NET Framework, which is maintained
> separately. For .NET Framework issues, please file a report at
> [Developer Community](https://developercommunity.visualstudio.com/home).
>
> If you're also seeing this issue on modern .NET (.NET 6+), please let us
> know and we can reopen.

### API Proposal -- rejected

> We discussed this and don't believe this API addition is the right direction
> for .NET. {Brief explanation.} The recommended approach is {alternative}.
>
> If you'd like to provide this as a NuGet package for the community, that
> would be a great option.

### API Proposal -- existing workaround covers the scenario

> Thanks for the suggestion! We looked into this and found that existing APIs
> and community packages already cover this scenario. Here's how you can achieve
> what you described:
>
> ```csharp
> {Concrete, functional code workaround using existing BCL APIs}
> ```
>
> Since this is achievable today without a new API, we don't think this clears
> the bar for BCL inclusion. Closing, but feel free to reopen if the workaround
> above doesn't cover your specific scenario -- we may be missing context.

### API Proposal -- targeting obsolescent technology

> Thanks for the proposal! Unfortunately, we're hesitant to add this to the BCL
> because {technology/format/protocol} {hasn't reached stable status / shows
> signs of being superseded by {replacement} / has declining adoption}.
>
> APIs added to the BCL are permanent -- no breaking changes once shipped -- so we
> need high confidence that the underlying technology will remain relevant
> long-term. A community NuGet package would be a better fit for now, and we'd
> be happy to reconsider if {technology} stabilizes.

### API Proposal -- concept duplication

> Thanks for the suggestion! The BCL already provides {existing capability} via
> {existing API/pattern}, which covers this scenario:
>
> ```csharp
> {Example showing the existing approach}
> ```
>
> Adding another way to accomplish the same thing increases conceptual overhead
> for .NET developers without proportionate benefit. Closing in favor of the
> existing approach. If the existing API has specific limitations that affect
> your scenario, we'd be interested in hearing about those in a new issue
> focused on improving {existing API}.

## NEEDS INFO Responses

### Bug -- missing repro

> Thanks for reporting this! To help us investigate, could you please provide:
>
> 1. A minimal reproduction (ideally a small console app we can `dotnet run`)
> 2. The exact .NET version (`dotnet --info` output)
> 3. Your OS and architecture
>
> Without a reproduction, we won't be able to diagnose the issue.

### Bug -- unclear expected behavior

> Thanks for the report. Could you clarify what behavior you expected vs. what
> you observed? The current behavior {description} appears to match the
> documented contract for {API}. If you believe this is incorrect, please
> explain the scenario where it causes a problem.

### API Proposal -- insufficient motivation

> Interesting idea! Before we can evaluate this, could you provide:
>
> 1. Concrete usage examples showing how this API would be consumed
> 2. What workaround you're currently using (and why it's insufficient)
> 3. How common this scenario is in your experience
>
> This helps us assess the breadth of impact and prioritize accordingly.

### API Proposal -- needs concrete API shape

> The motivation makes sense, but the proposal needs a more concrete API
> design. Could you update the issue description with:
>
> 1. Specific API signatures (namespace, class, method signatures)
> 2. Code examples showing typical usage
> 3. Consideration of edge cases ({specific edge cases})
>
> See our [API review process](../../../../docs/project/api-review-process.md) for
> guidelines on writing a strong proposal.

### API Proposal -- speculative, no inferable scenario

> Thanks for the suggestion! We'd like to understand the problem better. The
> proposal describes {what the API would do}, but we couldn't identify a
> concrete scenario where a developer would need this. Could you share:
>
> 1. A specific situation in your codebase where you needed this functionality
> 2. What you're currently doing instead (the workaround)
> 3. Why the workaround is inadequate (performance, correctness, ergonomics)
>
> This helps us distinguish between "nice to have" and "genuinely needed" --
> any API added to the BCL is a permanent commitment, so we need to be
> confident it solves a real problem.

### API Proposal -- existing workaround may suffice

> Thanks for the proposal! We found that existing APIs may already cover your
> scenario. Here's one approach:
>
> ```csharp
> {Concrete workaround using existing BCL APIs or patterns}
> ```
>
> Have you tried this approach? If it doesn't work for your scenario, could
> you describe what specific limitations you encountered? That context would
> help us evaluate whether a new BCL API is warranted.
