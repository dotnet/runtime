# Breaking Changes

We take compatibility in the .NET Framework and .NET Core extremely seriously.

Although .NET Core can be deployed app local, we are engineering it such that
portable libraries can target it and still run on the full desktop framework as
well. This means that the behavior of the full .NET Framework constrains the
implementation of any overlapping API in .NET Core.

Below is a summary of some documentation we have internally about what kinds of
things constitute breaking changes, how we categorize them, and how we decide
what we're willing to take.

Note that these rules only apply to API that have shipped in a previous RTM
release. New API still under development can be modified but we are still
cautious not to disrupt the ecosystem unnecessarily when prerelease API change.

To help triage breaking changes, we classify them in to four buckets:

1. Public Contract
2. Reasonable Grey Area
3. Unlikely Grey Area
4. Clearly Non-Public

## Bucket 1: Public Contract
*Clear [violation of public contract][breaking-change].*

Examples:
* Renaming or removing of a public type, member, or parameter
* Changing the value of a public constant or enum member
* Sealing a type that wasn't sealed
* Making a virtual member abstract
* Adding an interface to the set of base types of an interfaces
* Removing a type or interface from the the of base types
* Changing the return type of a member
* ...or any other [incompatible change][breaking-change] to the shape of an API

[breaking-change]: breaking-change-rules.md#source-and-binary-compatibility-changes

## Bucket 2: Reasonable Grey Area
*[Change of behavior][behavioral-changes] that customers would have reasonably
depended on.*

Examples:

* Throwing a new/different exception type in an existing common scenario
* An exception is no longer thrown
* A different behavior is observed after the change for an input
* decreasing the range of accepted values within a given parameter
* A new instance field is added to a type (impacts serialization)
* Change in timing/order of events (even when not specified in docs)
* Change in parsing of input and throwing new errors (even if parsing behavior
  is not specified in the docs)

These require judgment: how predictable, obvious, consistent was the behavior?

[behavioral-changes]: breaking-change-rules.md#behavioral-changes

## Bucket 3: Unlikely Grey Area
*Change of behavior that customers could have depended on, but probably
wouldn't.*

Examples:

* Correcting behavior in a subtle corner case

As with type 2 changes, these require judgment: what is reasonable and what’s
not?

## Bucket 4: Clearly Non-Public
*Changes to surface area or behavior that is clearly internal or non-breaking
in theory, but breaks an app.*

Examples:

* Changes to internal API that break private reflection

It is impossible to evolve a code base without making such changes, so we don't
require up-front approval for these, but we will sometimes have to go back and
revisit such change if there's too much pain inflicted on the ecosystem through
a popular app or library.

This bucket is painful for the machine-wide .NET Framework, but we do have much
more latitude here in .NET Core.

## What This Means for Contributors

* All bucket 1, 2, and 3 breaking changes require talking to the repo owners
  first:
    - We generally **don't accept** change proposals that are in bucket #1.
    - We **might accept** change proposals that are in #2 and #3 after a
      risk-benefit analysis. See below for more details.
    - We **usually accept** changes that are in bucket #4
* If you're not sure in which bucket applies to a given change, contact us as
  well.

### Risk-Benefit Analysis

For buckets #2 and #3 we apply a risk-benefit analysis. It doesn't matter if the
old behavior is "wrong", we still need to think through the implications. This
can result in one of the following outcomes: 

* **Accepted with compat switch**. Depending on the estimated customer impact,
  we may decide to add a compat switch that allows consumers to bring back the
  old behavior if neessary.

* **Accepted**. In some minor cases, we may decide to accept the change if the
  the benefit is large and the risk is super low or if the risk is moderate and
  quirking isn't viable.

* **Rejected**. If the risk is too high and/or the improvement too minor, we may
  decide not to accept the change proposal at all. We can help identify
  alternatives such as introducing a new API and obsoleting the old one.

