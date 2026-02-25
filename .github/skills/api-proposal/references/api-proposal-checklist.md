# API Proposal Quality Checklist

Use this checklist to validate an API proposal before publishing. Items are ordered by importance.

## Background and Motivation

- [ ] **DO** state the concrete user problem with at least one real-world scenario
- [ ] **DO** evaluate existing workarounds and explain why they are insufficient (performance, boilerplate, bad practices)
- [ ] **DO** reference prior art in other ecosystems where relevant (e.g., Python, Java, Rust equivalents)
- [ ] **DO NOT** assume the reviewers are subject matter experts of the library being augmented (even though they're .NET experts)
- [ ] **DO NOT** make unsubstantiated claims (e.g., "this is commonly needed" without showing who needs it)
- [ ] **DO NOT** write motivation text that is longer than what the design complexity warrants

## API Surface

- [ ] **DO** extract the exact API surface from `GenerateReferenceAssemblySource` output
- [ ] **DO** use clean declaration format for new self-contained types
- [ ] **DO** use `diff` blocks showing relevant context (sibling overloads) for additions to existing types
- [ ] **DO** validate all names against the [Framework Design Guidelines](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/framework-design-guidelines-digest.md)
- [ ] **DO** verify naming consistency with existing APIs in the target namespace
- [ ] **DO NOT** include implementation code in the API surface
- [ ] **DO NOT** include extensive XML documentation in the proposal diff — comments only as brief clarifications

## Scope Completeness

- [ ] **DO** consider whether neighboring APIs need the same treatment (e.g., adding to `ToDictionary`? what about `ToHashSet`?)
- [ ] **DO** consider whether APIs require common parameters (e.g., `CancellationToken`, `IEqualityComparer<T>`)
- [ ] **DO** consider async counterparts if adding sync APIs, and vice versa
- [ ] **DO** include `System.Linq.Queryable` and `System.Linq.AsyncEnumerable` equivalents when proposing new `System.Linq` methods
- [ ] **DO** consider overload consistency with existing method families
- [ ] **DO NOT** propose a narrow addition without evaluating the broader scope — reviewers will ask about it

## Prototype Validation

- [ ] **DO** verify the prototype builds for all target frameworks in the library's `.csproj`
- [ ] **DO** verify .NET Core APIs form a superset of netstandard/netfx APIs (if the library ships both)
- [ ] **DO** generate reference assembly source and commit the `ref/` changes
- [ ] **DO** build the test project separately to catch source breaking changes (overload resolution ambiguity, wrong method binding)
- [ ] **DO** verify all tests pass with zero failures
- [ ] **DO NOT** skip ApiCompat validation — binary compatibility must be maintained

## Usage Examples

- [ ] **DO** provide realistic, compilable code examples
- [ ] **DO** demonstrate the primary scenarios the API is designed to address
- [ ] **DO** match the number of examples to the novelty of the API, not just its size
- [ ] **DO NOT** provide only trivial/toy examples that don't demonstrate real usage

## Design Decisions

- [ ] **DO** explain the reasoning for nontrivial design choices
- [ ] **DO** list explicit trade-offs that were made (e.g., "no update support for 2-3x better performance")
- [ ] **DO NOT** explain self-evident decisions — this wastes reviewer time

## Alternatives and Risks

- [ ] **DO** demonstrate that alternatives were genuinely considered
- [ ] **DO** evaluate risks specifically: binary breaking changes, source breaking changes, performance, TFM compatibility
- [ ] **DO NOT** write "N/A" without demonstrating you've actually evaluated — it signals lack of research
- [ ] **DO NOT** dismiss risks with hand-wavy language ("low risk", "unlikely to cause issues")

## Open Questions

- [ ] **DO** list unresolved design questions with tentative answers
- [ ] **DO** surface uncertainty rather than hiding it — surprises during review are worse
- [ ] **DO NOT** leave fundamental design tensions unresolved without data to back a position

## Common Reviewer Feedback Patterns (DO NOT)

The following patterns consistently result in proposals being sent back for rework:

- **DO NOT** state claims as facts without backing scenarios ("this is commonly needed" → show who and how)
- **DO NOT** propose a narrow scope without evaluating the full picture (reviewers will ask "what about X?")
- **DO NOT** use imprecise naming that violates Framework Design Guidelines conventions
- **DO NOT** propose APIs without checking for prior art ("has this been done before?" is a standard reviewer question)
- **DO NOT** oversimplify a complex design space — if the problem is nuanced, the proposal should acknowledge the nuance
- **DO NOT** hide open design questions — acknowledge them upfront with tentative answers
- **DO NOT** propose an API without a working prototype — speculation is not evidence
