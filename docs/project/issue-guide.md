Issue Guide
===========

This page outlines how the CoreFx team thinks about and handles issues.  For us, issues on GitHub represent actionable work that should be done at some future point.  It may be as simple as a small product or test bug or as large as the work tracking the design of a new feature.  However, it should be work that falls under the charter of CoreFx, which is a collection of foundational libraries that make up the .NET Core development stack.  We will keep issues open even if the CoreFx team internally has no plans to address them in an upcoming release, as long as we consider the issue to fall under our purview.

### When we close issues
As noted above, we don't close issues just because we don't plan to address them in an upcoming release.  So why do we close issues?  There are few major reasons:

1. Issues unrelated to CoreFx.  When possible, we'll try to find a better home for the issue and point you to it.
2. Cross cutting work better suited for another team.  Sometimes the line between the framework, languages and runtime blurs.  For some issues, we may feel that the work is better suited for the runtime team, language team or other partner.  In these cases, we'll close the issue and open it with the partner team.  If they end up not deciding to take on the issue, we can reconsider it here.
3. Nebulous and Large open issues.  Large open issues are sometimes better suited for [User Voice](http://visualstudio.uservoice.com/forums/121579-visual-studio/category/31481--net), especially when the work will cross the boundaries of the framework, language and runtime.  A good example of this is the SIMD support we recently added to CoreFx.  This started as a [User Voice request](https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/2212443-c-and-simd), and eventually turned into work for both the core libraries and runtime.

Sometimes after debate, we'll decide an issue isn't a good fit for CoreFx.  In that case, we'll also close it.  Because of this, we ask that you don't start working on an issue until it's tagged with [up-for-grabs](https://github.com/dotnet/runtime/labels/up-for-grabs) or [api-approved](https://github.com/dotnet/runtime/labels/api-approved).  Both you and the team will be unhappy if you spend time and effort working on a change we'll ultimately be unable to take. We try to avoid that.

### Labels
We use GitHub [labels](https://github.com/dotnet/runtime/labels) on our issues in order to classify them.  We have the following categories per issue:
* **Area**: These area-&#42; labels (e.g. [area-System.Collections](https://github.com/dotnet/runtime/labels/area-System.Collections)) call out the assembly or assemblies the issue applies to. In addition to labels per assembly, we have a few other area labels: [area-Infrastructure](https://github.com/dotnet/runtime/labels/area-Infrastructure), for issues that relate to our build or test infrastructure, and [area-Meta](https://github.com/dotnet/runtime/labels/area-Meta) for issues that deal with the repository itself, the direction of the .NET Core Platform, our processes, etc. See [full list of areas](#areas).
* **Issue Type**: These labels classify the type of issue.  We use the following types:
  * [bug](https://github.com/dotnet/runtime/labels/bug): Bugs in an assembly.
  * api-&#42; ([api-approved](https://github.com/dotnet/runtime/labels/api-approved), [api-needs-work](https://github.com/dotnet/runtime/labels/api-needs-work), [api-ready-for-review](https://github.com/dotnet/runtime/labels/api-ready-for-review)): Issues which would add APIs to an assembly (see [API Review process](api-review-process.md) for details).
  * [enhancement](https://github.com/dotnet/runtime/labels/enhancement): Improvements to an assembly which do not add new APIs (e.g. performance improvements, code cleanup).
  * [test bug](https://github.com/dotnet/runtime/labels/test%20bug): Bugs in the tests for a specific assembly.
  * [test enhancement](https://github.com/dotnet/runtime/labels/test%20enhancement): Improvements in the tests for a specific assembly (e.g. improving test coverage).
  * [documentation](https://github.com/dotnet/runtime/labels/documentation): Issues related to documentation (e.g. incorrect documentation, enhancement requests).
  * [question](https://github.com/dotnet/runtime/labels/question): Questions about the product, source code, etc.
* **Other**:
  * [easy](https://github.com/dotnet/runtime/issues?utf8=%E2%9C%93&q=is%3Aissue%20is%3Aopen%20label%3Aeasy%20no%3Aassignee) - Good for starting contributors.
  * [up-for-grabs](https://github.com/dotnet/runtime/labels/up-for-grabs): Small sections of work which we believe are well scoped.  These sorts of issues are a good place to start if you are new.  Anyone is free to work on these issues.
  * [needs more info](https://github.com/dotnet/runtime/labels/needs%20more%20info): Issues which need more information to be actionable.  Usually this will be because we can't reproduce a reported bug.  We'll close these issues after a little bit if we haven't gotten actionable information, but we welcome folks who have acquired more information to reopen the issue.
 * [wishlist](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3Awishlist) - Issues on top of our backlog we won't likely get to. Warning: Might not be easy.

In addition to the above, we have a handful of other labels we use to help classify our issues.  Some of these tag cross cutting concerns (e.g. [tenet-performance](https://github.com/dotnet/runtime/labels/tenet-performance)), whereas others are used to help us track additional work needed before closing an issue (e.g. [api-needs-exposed](https://github.com/dotnet/runtime/labels/api-needs-exposed)).

### Milestones
We use [milestones](https://github.com/dotnet/runtime/milestones) to prioritize work for each upcoming release to NuGet.org.

### Assignee
We assign each issue to assignee, when the assignee is ready to pick up the work and start working on it.  If the issue is not assigned to anyone and you want to pick it up, please say so - we will assign the issue to you.  If the issue is already assigned to someone, please coordinate with the assignee before you start working on it.

### Areas
Areas are tracked by labels area-&#42; (e.g. area-System.Collections). Each area typically corresponds to one or more contract assemblies. To view owners for each area in this repository check out the [area-owners.md](https://github.com/dotnet/runtime/blob/main/docs/area-owners.md) page.

### Community Partner Experts

| Area        | Owners / experts | Description |
|-------------|------------------|-------------|
| Tizen | [@alpencolt](https://github.com/alpencolt), [@gbalykov](https://github.com/gbalykov) | For issues around Tizen CI and build issues |

### Triage rules - simplified

1. Each issue has exactly one **area-&#42;** label
1. Issue has no **Assignee**, unless someone is working on the issue at the moment
1. Use **up-for-grabs** as much as possible, ideally with a quick note about next steps / complexity of the issue
1. Set milestone to **Future**, unless you can 95%-commit you can fund the issue in specific milestone
1. Each issue has exactly one "*issue type*" label (**bug**, **enhancement**, **api-needs-work**, **test bug**, **test enhancement**, **question**, **documentation**, etc.)
1. Don't be afraid to say no, or close issues - just explain why and be polite
1. Don't be afraid to be wrong - just be flexible when new information appears

Feel free to use other labels if it helps your triage efforts (e.g. **needs more info**, **Design Discussion**, **blocked**, **blocking-partner**, **tenet-performance**, **tenet-compatibility**, **os-linux**/**os-windows**/**os-mac-os-x**/**os-unsupported**, **os-windows-uwp**/**os-windows-wsl**, **arch-arm32**/**arch-arm64**)

#### Motivation for triage rules

1. Each issue has exactly one **area-\*** label
    * Motivation: Issues with multiple areas have loose responsibility (everyone blames the other side) and issues are double counted in reports.
1. Issue has no **Assignee**, unless someone is working on the issue at the moment
    * Motivation: Observation is that contributors are less likely to grab assigned issues, no matter what the repo rules say.
1. Use **up-for-grabs** as much as possible, ideally with a quick note about next steps / complexity of the issue
    * Note: Per http://up-for-grabs.net, such issues should be no longer than few nights' worth of work. They should be actionable (i.e. no mysterious CI failures or UWP issues that can't be tested in the open).
1. Set milestone to **Future**, unless you can 95%-commit you can fund the issue in specific milestone
    * Motivation: Helps communicate desire/timeline to community. Can spark further priority/impact discussion.
1. Each issue has exactly one "*issue type*" label (**bug**, **enhancement**, **api-needs-work**, **test bug**, **test enhancement**, **question**, **documentation**, etc.)
    * Don't be afraid to be wrong when deciding 'bug' vs. 'test bug' (flip a coin if you must). The most useful values for tracking are 'api-&#42;' vs. 'enhancement', 'question', and 'documentation'.
    * Note: The **api-\*** labels are important for tracking API approvals, the other *issue type* labels are in practice optional.
1. Don't be afraid to say no, or close issues - just explain why and be polite
1. Don't be afraid to be wrong - just be flexible when new information appears

#### PR rules

1. Each PR has exactly one **area-\*** label
    * Movitation: Area owners will get email notification about new issue in their area.
1. PR has **Assignee** set to author of the PR, if it is non-CoreFX engineer, then area owners are co-assignees
    * Motivation #1: Area owners are responsible to do code reviews for PRs from external contributors. CoreFX engineers know how to get code reviews from others.
    * Motivation #2: Assignees will get notifications for anything happening on the PR.
1. [Optional] Set milestone according to the branch the PR is against (main = 6.0, release/5.0 = 5.0)
    * Motivation: Easier to track and audit where which fix ended up and if it needs to be ported into another branch (hence reflecting branch the specific PR ended up and not the abstract issue).
    * Note: This is easily done after merge via simple queries & bulk-edits, you don't have to do this one.
1. Any other labels on PRs are superfluous and not needed (exceptions: **blocked**, **NO MERGE**)
    * Motivation: All the important info (*issue type* label, api approval label, OS label, etc.) is already captured on the associated issue.
1. Push PRs forward, don't let them go stale (response every 5+ days, ideally no PRs older than 2 weeks)
1. Stuck or long-term blocked PRs (e.g. due to missing API approval, etc.) should be closed and reopened once they are unstuck
    * Motivation: Keep only active PRs. WIP (work-in-progress) PRs should be rare and should not become stale (2+ weeks old). If a PR is stale and there is not immediate path forward, consider closing the PR until it is unblocked/unstuck.
1. PR should be linked to related issue, use [auto-closing](https://help.github.com/articles/closing-issues-via-commit-messages/) (add "Fixes #12345" into your PR description)
