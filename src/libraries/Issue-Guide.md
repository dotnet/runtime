### Overview
This page outlines how the CoreFx team thinks about and handles issues.  For us, issues on GitHub represent actionable work that should be done at some future point.  It may be as simple as a small product or test bug or as large as the work tracking the design of a new feature.  However, it should be work that falls under the charter of CoreFx, which is a collection of foundational libraries that make up the .NET Core development stack.  We will keep issues open even if the CoreFx team internally has no plans to address them in an upcoming release, as long as we consider the issue to fall under our purview.

### When we close issues
As noted above, we don't close issues just because we don't plan to address them in an upcoming release.  So why do we close issues?  There are few major reasons:

1. Issues unrelated to CoreFx.  When possible, we'll try to find a better home for the issue and open it there on your behalf.
2. Cross cutting work better suited for another team.  Sometimes the line between the framework, languages and runtime blurs.  For some issues, we may feel that the work is better suited for the runtime team, language team or other partner.  In these cases, we'll close the issue and open it with the partner team.  If they end up not deciding to take on the issue, we can reconsider it here.
3. Nebulous and Large open issues.  Large open issues are sometimes better suited for [User Voice](http://visualstudio.uservoice.com/forums/121579-visual-studio/category/31481--net), especially when the work will cross the boundaries of the framework, language and runtime.  A good example of this is the SIMD support we recently added to CoreFx.  This started as a User Voice request, and eventually turned into work for both the core libraries and runtime.

Sometimes after debate, we'll decide an issue isn't a good fit for CoreFx.  In that case, we'll also close it.  Because of this, we ask that you don't start working on an issue until it's tagged with "up for grabs" or "feature approved".  We'd hate for you to spend time and effort working on a change we'll ultimately be unable to take.

### Labels
We use GitHub labels on our issues in order to classify them.  We have the following categories per issue:
* **Area**: These labels call out the assembly or assemblies the issue applies to. In addition to tags per assembly, we have a few other tags: Infrastructure, for issues that relate to our build or test infrastructure, and Meta for issues that deal with the repository itself, the direction of the .NET Core Platform, our processes, etc.
* **Type**: These labels classify the type of issue.  We use the following types:
 * [api addition](https://github.com/dotnet/corefx/labels/api%20addition): Issues which would add APIs to an assembly.
 * [bug](https://github.com/dotnet/corefx/labels/bug): Issues for bugs in an assembly.
 * [documentation](https://github.com/dotnet/corefx/labels/documentation): Issues relating to documentation (e.g. incorrect documentation, enhancement requests)
 * [enhancement](https://github.com/dotnet/corefx/labels/enhancement): Issues related to an assembly that improve it, but do not add new APIs (e.g performance improvements, code cleanup)
 * [test bug](https://github.com/dotnet/corefx/labels/test%20bug): Issues for bugs in the tests for a specific assembly.
* **Ownership**: These labels are used to specify who owns specific issue. Issues without an ownership tag are still considered "up for discussion" and haven't been approved yet. We have the following different types of ownership:
 * [up for grabs](https://github.com/dotnet/corefx/labels/up%20for%20grabs): Small sections of work which we believe are well scoped. These sorts of issues are a good place to start if you are new.  Anyone is free to work on these issues.
 * [feature approved](https://github.com/dotnet/corefx/labels/feature%20approved): Larger scale issues.  Like up for grabs, anyone is free to work on these issues, but they may be tricker or require more work.
 * [grabbed by community](https://github.com/dotnet/corefx/labels/grabbed%20by%20community): Someone outside the CoreFx team has assumed responsibility for addressing this issue and is working on a fix.  The comments for the issue will call out who is working on it.  You shouldn't try to address the issue without coordinating with the owner.
 * [grabbed by assignee](https://github.com/dotnet/corefx/labels/grabbed%20by%20assignee): Like grabbed by community, except the person the issue is assigned to is making a fix.  This will be someone on the CoreFx team.
* **Project Management**: These labels are used to facilitate the team's [Kanban Board](https://huboard.com/dotnet/corefx).  Labels indicate the current status and swim lane.  
 * [0 - Backlog](https://github.com/dotnet/corefx/issues?q=is%3Aopen+is%3Aissue+label%3A%220+-+Backlog%22): Tasks that are not yet ready for development or are not yet prioritized for the current development cycle.
 * [1 - Up Next](https://github.com/dotnet/corefx/issues?q=is%3Aopen+is%3Aissue+label%3A%221+-+Up+Next%22): Tasks that are ready for development and prioritized above the rest of the backlog.
 * [2 - In Progress](https://github.com/dotnet/corefx/issues?q=is%3Aopen+is%3Aissue+label%3A%222+-+In+Progress%22): Tasks that are under active development.
 * [3 - Done](https://github.com/dotnet/corefx/issues?q=is%3Aopen+is%3Aissue+label%3A%223+-+Done%22): Tasks that are finished.  There should be no open issues in the Done stage.
 * [:Community](https://github.com/dotnet/corefx/issues?q=is%3Aopen+is%3Aissue+label%3A%3ACommunity): Community Engagement & Open Development Swim Lane. :swimmer: 
 * [:Port to GitHub](https://github.com/dotnet/corefx/issues?q=is%3Aopen+is%3Aissue+label%3A%22%3APort+to+GitHub%22): Swim lane :swimmer: tracking the work remaining to open source .NET Core Framework
 * [:Infrastructure](https://github.com/dotnet/corefx/issues?q=is%3Aopen+is%3Aissue+label%3A%3AInfrastructure): Swim lane :swimmer: tracking OSS Engineering and Infrastructure
 * [:X-Plat](https://github.com/dotnet/corefx/issues?q=is%3Aopen+is%3Aissue+label%3A%3AX-Plat): Swim lane :swimmer: for Cross Platform Support

In addition to the above, we have a handful of other labels we use to help classify our issues.  Some of these tag cross cutting concerns (e.g. cross platform, performance, serialization impact) where as others are used to help us track additional work needed before closing an issue (e.g. needs api review). Finally, we have the "needs more info" label.  We use this label to mark issues where we need more information in order to proceed.  Usually this will be because we can't reproduce a reported bug.  We'll close these issues after a little bit if we haven't gotten actionable information, but we welcome folks who have acquired more information to reopen the issue.

### Assignee
We assign each issue to a CoreFx team member.  In most cases, the assignee will not be the one who ultimately fixes the issue (that only happens in the case where the issue is tagged "grabbed by assignee"). The purpose of the assignee is to act as a point of contact between the CoreFx team and the community for the issue and make sure it's driven to resolution.  If you're working on an issue and get stuck, please reach out to the assignee (just at mention them)  and they will work to help you out.