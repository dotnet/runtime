We take compatibility in the .NET Framework and .NET Core extremely seriously.

Although .NET Core can be deployed app local, we are engineering it such that portable libraries can target it and still run on the full desktop framework as well. This means that the behavior of the full .NET Framework constrains the implementation of any overlapping API in .NET Core.

Below is a summary of some documentation we have internally about what kinds of things constitute breaking changes, how we categorize them, and how we decide what we're willing to take.

Note that these rules only apply to API that have shipped in a previous RTM release. New API still under development can be modified but we are still cautious not to disrupt the ecosystem unnecessarily when prerelease API change.

To help triage breaking changes, we classify them in to four buckets:

1. Public Contract
2. Reasonable Grey Area
3. Unlikely Grey Area
4. Clearly Non-Public

### Bucket 1: Public Contract

Clear violation of public contract.

Examples:
* throwing a new exception type in an existing common scenario
* renaming a public type, member, or parameter
* decreasing the range of accepted values within a given parameter
* changing the value of a public constant or enum member

### Bucket 2: Reasonable Grey Area
Change of behavior that customers would have reasonably depended on.

Examples:
* change in timing/order of events (even when not specified in docs)
* change in parsing of input and throwing new errors (even if parsing behavior is not specified in the docs)

These require judgment: how predictable, obvious, consistent was the behavior?

##Bucket 3: Unlikely Grey Area
*Change of behavior that customers could have depended on, but probably wouldn't.*

**Examples:**
* correcting behavior in a subtle corner case

As with type 2 changes, these require judgment: what is reasonable and what’s not?

## Bucket 4: Clearly Non-Public
*Changes to surface area or behavior that is clearly internal or non-breaking in theory, but breaks an app.*

**Examples:**
* Changes to internal API that break private reflection

It is impossible to evolve a code base without making such changes, so we don't require up-front approval for these, but we will sometimes have to go back and revisit such change if there's too much pain inflicted on the ecosystem through a popular app or library.

This bucket is painful for the machine-wide .NET Framework, but we do have much more latitude here in .NET Core.

## What This Means for Contributors
* All bucket 1, 2, and 3 breaking changes require talking to the repo owners first.
* If you're not sure in which bucket applies to a given change, contact us as well.
* It doesn't matter if the old behavior is "wrong", we still need to think through the implications.
* If a change is deemed too breaking, we can help identify alternatives such as introducing a new API and obsoleting the old one.