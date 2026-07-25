# API Proposal Triage

Detailed guidance for triaging API proposals (`api-suggestion`) in
dotnet/runtime. Referenced from the main [SKILL.md](../SKILL.md) during
Steps 4–7.

## The Immortality Threshold

Any API added to the BCL crosses a threshold of immortality: once shipped, no
breaking changes or major revisions are permitted. This means every addition
must clear a high bar for long-term value. The triage question is:
**"Is this idea worth committing to forever?"**

## Research Prior Art

In addition to the universal research in Step 4 of the main workflow, API
proposals require deeper investigation.

### .NET ecosystem

1. **Check the API review backlog** -- Search for related `api-ready-for-review`
   or `api-approved` issues.
2. **Research usage volume** -- Search [grep.app](https://grep.app) for .NET
   code patterns related to the proposed API to gauge real-world demand:
   - How many codebases manually implement the workaround the proposal would
     replace
   - How widely the affected APIs are used today
   - Common consumption patterns that would benefit from the proposed API
3. **Document concrete workarounds** -- If an existing package or BCL pattern
   covers the proposed scenario, write a concrete, functional code workaround
   using it -- not a generic "use package X" dismissal. This workaround is
   included in the triage report and may determine the recommendation.

### Other ecosystems (limited web search)

Do a brief web search to see how other platforms handle similar functionality:

- **Java** -- JDK standard library, Guava
- **Python** -- standard library, popular packages
- **Rust** -- standard library, popular crates
- **Go** -- standard library

This provides useful context (e.g., "Java's `java.util.X` provides similar
functionality, which suggests community demand"). Keep it brief -- 1–2 paragraphs
max.

## Merit Evaluation

Evaluate the **merits of the idea**, not just the quality of the proposal
document. The goal is to determine whether the initial request can inspire an
actual workable solution worth maintaining.

- **Concrete motivation** -- Can a real-world user problem be inferred from the
  issue? At least one concrete scenario (who needs it, how they'd use it) must
  be present or inferable. Speculative "this would be nice" proposals without a
  demonstrable use case should lean toward NEEDS INFO or CLOSE.
- **Existing workarounds** -- If research found an existing package or BCL
  pattern that covers the scenario, evaluate whether it genuinely falls short.
  - Workaround is fully adequate → lean CLOSE (share the workaround)
  - Workaround might suffice but uncertain → lean NEEDS INFO (ask user)
  - Workaround has clear limitations that matter → lean KEEP
- **Concept count and API cruft** -- Does the BCL already provide multiple ways
  to accomplish the same thing? Adding yet another approach increases
  conceptual overhead for all .NET developers. Evaluate whether the proposal
  introduces a genuinely distinct capability or merely duplicates existing
  patterns.
- **Triviality of user implementation** -- Can the user trivially implement this
  themselves (e.g., a simple extension method, a thin wrapper)? Whether this
  counts against a proposal depends on breadth of impact: if thousands of
  codebases would write the same extension method independently, there's value
  in standardizing; if only a handful would need it, it doesn't clear the bar.
  Use the grep.app usage data to inform this assessment.
- **Obsolescence risk** -- Is the proposal targeting a technology, format, or
  protocol with strong signals of being obsoleted or superseded? APIs targeting
  unstable standards (pre-RFC, draft specifications) or technologies losing
  adoption should lean toward CLOSE. The BCL should not enshrine transient
  technology in a permanent API.
- **Naming check** -- Validate proposed API names against the
  [Framework Design Guidelines](../../../../docs/coding-guidelines/framework-design-guidelines-digest.md).
  Flag violations (wrong casing, abbreviations, missing patterns like `TryX`
  or `XAsync`), but don't let naming issues overshadow a good idea -- names can
  be fixed later.
- **Breaking changes** -- Only consider breaking changes a reason to CLOSE if
  they are fundamentally unavoidable. Focus on the higher-level intent: users
  may be inexperienced with API proposals and may not realize that adding a
  parameter to an existing method is breaking when a new overload would work.
  The question is whether the underlying idea can be shaped into something that
  doesn't break existing code.
- **Prior art** -- Ecosystem precedent (Java, Python, Rust, Go) is useful
  context but should not drive the recommendation in either direction.
- **Microsoft vs. community maintenance** -- Does this need to be maintained by
  the dotnet/runtime team, or is it better served by a community package? APIs
  that require runtime internals, JIT intrinsics, or deep framework integration
  need to be Microsoft-maintained. Domain-specific APIs with narrow audiences
  are typically better as community packages.

## Decision Signals

### Green Flags (lean toward KEEP)

| Signal | Example |
|--------|---------|
| **Concrete, demonstrable user problem** | "Parsing ISO 8601 durations requires 40+ lines of manual code" with real scenario |
| **Broad impact** | Affects a wide range of app developers or library authors, not a niche domain |
| **No adequate existing solution** | No community package covers the scenario, or existing packages have significant limitations |
| **Requires runtime/framework integration** | Needs access to internals, JIT intrinsics, or deep framework hooks that community packages can't provide |
| **Eliminates a common source of bugs** | Current workarounds are error-prone and the proposed API makes the correct usage the easy path |
| **Ecosystem precedent** | Java, Python, Rust, Go standard libraries all provide equivalent functionality (useful context, not determinative) |

### Red Flags (lean toward CLOSE)

| Signal | Example |
|--------|---------|
| **Existing solution covers the scenario** | A well-maintained package (Microsoft or community) already does this; share a concrete functional workaround |
| **Targeting obsolescent technology** | Format/protocol that hasn't reached stable/RFC status, or has strong signals of being superseded |
| **Concept cruft / duplication** | The BCL already provides multiple ways to accomplish the same thing; another approach adds confusion |
| **Unavoidable breaking changes** | The idea fundamentally cannot be implemented without breaking existing API contracts |
| **Highly domain-specific** | Useful only within a narrow industry or application type; better served by a domain-specific package |
| **Low impact at high maintenance cost** | Adds significant API surface (and perpetual maintenance burden) for a scenario that affects few users |

### Gray Areas (lean toward NEEDS INFO)

| Signal | Action |
|--------|--------|
| **Speculative motivation** -- no concrete scenario can be inferred | Ask for concrete usage scenarios showing who needs it and how |
| **Existing package might suffice** -- unclear if it covers the specific scenario | Share the workaround and ask the user if they've tried it |
| **Trivial to implement in user code** -- but unclear how many people need it | Ask about the breadth of the scenario; how common is the pattern? |
| **Motivation is reasonable but vague** -- the "what" is clear but the "how" is not | Ask for a more concrete API sketch or usage examples |

## Workaround Evaluation

When an existing solution is found (community or Microsoft package, simple
extension method, existing BCL pattern), the triage report should document it
with a **concrete, functional code workaround** covering the specific scenario
from the issue -- not a generic "use package X" dismissal.

**Important:** The maintainer report body (Prior Art & Ecosystem section) may
name specific third-party packages. However, the author-facing "Suggested
response" must refer to alternatives generically (e.g., "community packages
exist that address this scenario") and prefer BCL-based workarounds in code
examples. See the content rules in the main SKILL.md Step 8.

- **Clear-cut**: The workaround fully addresses the scenario → CLOSE with the
  workaround in the response.
- **Uncertain**: The workaround might address it but edge cases are unclear →
  NEEDS INFO asking the user if they've tried it and if it meets their needs.
- **Insufficient**: The workaround exists but has documented limitations that
  matter for the scenario (performance, correctness, ergonomics) → KEEP.

## Triviality Assessment

Some proposals request APIs that are trivial for users to implement themselves
(e.g., a simple extension method, a thin wrapper). Whether triviality should
count against a proposal depends on **breadth of impact**:

- If thousands of codebases would independently write the same extension method,
  there's value in standardizing it.
- If it's a utility only a handful of developers would need, it doesn't clear
  the immortality bar.
- When in doubt, search grep.app for the pattern to estimate how many codebases
  implement the workaround today.

## Naming Quick-Check

Flag proposed names that violate the
[Framework Design Guidelines](../../../../docs/coding-guidelines/framework-design-guidelines-digest.md):

- PascalCase for types, methods, properties, events
- camelCase for parameters
- `TryX` pattern for methods that return success/failure
- `XAsync` suffix for async methods
- No abbreviations or acronyms in identifiers (unless universally understood:
  `IO`, `XML`, `Http`)
- No underscores in identifiers
- Verb or verb phrase for method names; noun or noun phrase for types

Note: flag violations but focus on the substance of the idea. A great idea
with a bad name is still a great idea -- the name can be fixed later.

## Complexity Estimation

Estimate implementation complexity using t-shirt sizes:

| Size | Criteria |
|------|----------|
| **S** | Isolated change to one component, small API surface, no breaking changes, straightforward tests |
| **M** | Touches multiple files/components, moderate API surface, some edge cases, may need design discussion |
| **L** | Cross-cutting change across multiple areas, large API surface, potential breaking changes, significant test matrix |
| **XL** | Fundamental architectural change, new subsystem, extensive API review rounds, multi-release effort |

Factors to consider: API surface area, number of components affected, breaking
change risk, cross-cutting concerns (serialization, threading, platform
differences), and test coverage needed.

## API-Specific Recommendation Criteria

### KEEP

KEEP signals that the idea has merit and could inspire a workable API design:

- The motivation is clear and backed by real-world scenarios
- No existing workaround adequately covers the scenario
- The API requires framework-level integration (runtime internals, JIT
  intrinsics, deep platform hooks) that community packages cannot provide
- The concept is genuinely distinct from what the BCL already offers
- The idea clears the immortality bar: it's worth committing to permanently

### CLOSE

- **Existing workaround covers the scenario** -- A concrete workaround using an
  existing package or BCL pattern fully addresses the stated problem. Include the
  functional workaround code in the response so the author gets immediate value.
- **Targeting obsolescent technology** -- The proposal depends on a format,
  protocol, or technology with strong signals of being obsoleted or superseded
  (e.g., pre-RFC draft, declining adoption). The BCL should not enshrine
  transient technology in a permanent API.
- **Concept duplication / API cruft** -- The BCL already provides functionally
  equivalent capability. Adding another way to do the same thing increases
  conceptual overhead without proportionate benefit.
- **Unavoidable breaking changes** -- The idea fundamentally cannot be
  implemented without breaking existing API contracts. (Only apply this when the
  break is inherent to the concept, not when the proposal's specific approach
  happens to be breaking but a different approach could work.)

### NEEDS INFO

- **Speculative motivation** -- No concrete user scenario can be inferred from
  the issue. Ask for specific examples of who needs this and how they'd use it.
- **Existing workaround might suffice** -- An existing package or pattern was
  found that may cover the scenario, but it's unclear whether it meets the
  author's needs. Share the workaround and ask if they've tried it.
- **Motivation is reasonable but vague** -- The problem area is clear but the
  desired API behavior is too ambiguous to evaluate feasibility.
