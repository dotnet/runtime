# Breaking change process

Breaking changes make it difficult for people to adopt new versions of .NET. We strive to avoid making them. In the rare case when a breaking change is unavoidable or the best option when all factors are considered, you should follow the process outlined in this document. The intent of the process is to get feedback. This might result in previously unseen opportunities to achieve your goals without breaks, clever ways to limit breaks to more niche scenarios, and (most importantly) better understand the scope of impact to .NET users.

This process does not result in approval or denial of a breaking change request. It results in feedback, which might be varied or uniform. In the best case, the feedback is a call to action to you, that provides a direction on how you should proceed. The people that are responsible for reviewing your PR are expected to use the information collected by this process as part of their determination of whether your change is appropriate to merge.

You should start this process as soon as possible, preferably before any code is written, in the design phase. This isn't always possible, since the change in behavior might not be discovered until you're almost done building a feature. In the case of a significant breaking change, you should provide reviewers with a design document so that they can understand the broader scope, goals and constraints of your change.

There are people on the team that can help you work through a breaking change, if you want to engage with them before starting this more formal process. They are part of the [dotnet/compat team](https://github.com/orgs/dotnet/teams/compat) . Feel free to @mention them on issues to start a dialogue.

## Process

1. Create or link to an issue that describes the breaking change, including the following information:
   * Mark with the [breaking-change](https://github.com/dotnet/runtime/labels/breaking-change) label
   * Goals and motivation for the change
   * Pre-change behavior
   * Post-change behavior
   * Versions of the product this change affects
   * Errors or other behavior you can expect when running old code that breaks
   * Workarounds and mitigations, including [AppContext switches](https://docs.microsoft.com/dotnet/api/system.appcontext)
   * Link to the issue for the feature or bug fix that the breaking change is associated with.
   * Reference this issue from associated PRs.
2. Share your issue with whomever you see as stakeholders
   * Please @mention [dotnet/compat team](https://github.com/orgs/dotnet/teams/compat) team
   * Engage with people commenting on the issue, with the goal of deriving the most feedback on your proposed breaking change
   * This may involve significant explanation on your part. A well-written design doc can ease the burden here.
3. Mark associated PRs with the [breaking-change](https://github.com/dotnet/runtime/labels/breaking-change) label, and link to your breaking change issue.
4. Once the PR is merged, create a [docs issue](https://github.com/dotnet/docs/issues/new?template=dotnet-breaking-change.md).
   * Clarify which .NET preview the break will be first released in.
5. Breaking change issues can be closed at any time after the PR is merged. Best practice is waiting until the change has been released in a public preview.

Notes:

* We add quirk switches to .NET Core re-actively, only once we get feedback that they are really needed for something important. We do not add them proactively like in .NET Framework just because they might be needed in theory.
* In terms of product versions that the change affects, consider both .NET Core and .NET Framework (see: [.NET Framework compatibility mode](https://docs.microsoft.com/dotnet/standard/net-standard#net-framework-compatibility-mode)). Consider source and binary compatibility.

## Examples

The following are good examples of breaking change issues

* https://github.com/dotnet/runtime/issues/28788
* https://github.com/dotnet/runtime/issues/37672

## Resources

There are additional documents that you should consult about breaking changes:

* [Breaking changes](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/breaking-changes.md)
* [Breaking change definitions](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/breaking-change-definitions.md)
* [Breaking change rules](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/breaking-change-rules.md)
