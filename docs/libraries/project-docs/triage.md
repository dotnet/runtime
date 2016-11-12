# Triaging process

The nice thing about open source and GitHub in particular is that there is a very low barrier of entry for participation, which includes filing feature requests and bugs.

## The challenge

The tricky thing is that we need to strike a balance:

* We don't want to close all issues that represent work we're currently not resourced to do. In the end, a healthy open source project requires a well maintained backlog so that we can plan the next version in a transparent fashion.

* On the other hand, we don't want to have so many pending issues that we can't see the forest for the trees. Of course, what is *too many* is highly subjective. As a general rule, it's not so much the sheer number of issues, it's how diverse they are.

At Microsoft, we've a lot of experience on how to run big, multi-year releases with thousands of engineers. I think it's fair to say that we're still learning on how to adjust our processes and tools to deal with short releases, especially around open source. It's interesting to note that the number of engineers and products have pretty stayed the same. What has changed is that we pushed the release management down to the individual teams so that decisions are more localized and thus can be made faster with fewer expensive round trips across large organizational boundaries. This is one of the reasons we introduced the .NET Core platform. Since its release cycle is decoupled from the .NET Framework (and thus Windows) we can iterate much faster.

However, another key thing hasn't changed either: our customers continue to expect a well integrated set of products. For .NET Core, this means we need to align runtime work, language innovation, designer integration, and IDE investments in order to deliver the great development experience .NET is known for.

So while we strive to move faster at each of these components, we still have to be mindful of the integration points and ensure we deliver a set of components that work well with each other.

## Themes for alignment

In the multi year release cycles we've learned that organizational alignment is much simpler when releases have a set of themes and key scenarios we want to enable. This ensures that components that need to integrate commit resources and prioritize issues in a similar fashion. This reduces confusion and avoids constant fire drills around integration points.

Of course, teams aren't hostage to the set of themes and scenarios. But they provide the goal post to judge what is considered critical, important and merely nice to have. Depending on cost and complexity, certain features can be quickly discarded as out of scope while others can be grouped together in order to streamline planning and execution.

## What this means for CoreFx

Our goal is to become a bit more trigger happy when it comes to closing issues. We've currently over 1,400 open issues on the corefx repo alone. We've noticed that our design review process is a major bottleneck for adding new features, in particular when we're adding APIs to types that are shared with the .NET Framework: we need to think about how we can reconcile that difference in the future so that you can still author libraries that can run on both platforms.

We owe our customers -- and this includes our open source community -- that we don't lose track of what's important. We do, however, want to make sure we can continuously improve small things as well. We strongly believe that one of the key reasons open source is so successful is because it encourages a mindset of small changes over time.

## Triaging approach

Our current approach to triage looks as follows (the higher the number the more likely we're going to close it):

1. Is it helping with [porting .NET Framework code to .NET Core](porting.md)?
2. Is it aligned with the [roadmap of .NET Core vNext](roadmap.md)?
3. Is it a separate type that we can deliver as a standalone library for both, .NET Framework and .NET Core?
4. Is it going to require modifying the .NET Framework?

Of course, this list isn't meant to be comprehensive, as this is generally impossible to write up. However, it should give you an idea of how we prioritize and what goes through our head when triaging.

In particular, we strive to follow these guidelines:

* **Do** favor issues that impact our ability to deliver the current release
* **Do** prioritize expensive work items that is strongly aligned with the roadmap for the upcoming release
* **Avoid** working on a large number of unrelated issues
* **Do** close issues that represent work we don't think makes sense for the current or upcoming release
* **Do** close issues that provide little or questionable value to APIs that are included with the .NET Framework
* **Do** favor issues that do not require modifying types that are included with the .NET Framework.
* **Avoid** franken design just to avoid adding APIs to types that are included with the .NET Framework

## Porting to .NET Framework

### Constraints for shipping in the .NET Framework
 
Each release of .NET Framework ships to over one billion machines world-wide and installs as an *in-place* update, requiring each fix to meet a high level of quality and compatibility.  Each fix is reviewed extensively to determine which release if any is most appropriate.

Here are some of the factors that are considered:

* **Backward compatibility**. We do not want to break existing applications.
* **Risk and/or size of the change**. We need to consider whether the change is on a common code path or causes a lot of churn.
* **Measurable quality of the change**. We need to pay attention to our ability to test the change in an complete end-to-end fashion.
* **Value of the change**. All the risks outlined above need to be balanced against how many customers the change will help and how much the change will help customers.
 
### How you can tell whether a change will be ported
 
Generally, all changes are expected to be tracked as GitHub issues. We are going to introduce the following three labels on GitHub to track and report progress around porting:

* [**netfx-port-consider**](https://github.com/dotnet/corefx/labels/netfx-port-consider): A given issue should be considered for inclusion in the .NET Framework. Once an issue has this label, the team will consider it for inclusion in the .NET Framework.
* [**netfx-port-approved**](https://github.com/dotnet/corefx/labels/netfx-port-approved): If the proposed change is shown to add value and not introduce unmitigated risk then the issue will be checked in and ship in a future release of the .NET Framework. Marking an issue with this label does not imply that it will ship in the next release or any specific release of the .NET Framework.
* [**netfx-port-declined**](https://github.com/dotnet/corefx/labels/netfx-port-declined): If the proposed change does not align with our business goals or introduces unmitigated risk, the issue will not be included in the .NET Framework. In the event of this scenario, we will add a comment on the issue.
