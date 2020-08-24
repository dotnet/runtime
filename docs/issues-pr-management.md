# Issue and Pull Request Management
The purpose of this document is to help establish norms and best practices for dotnet/runtime.  The outcomes from this document should be translated into the respective documentation within dotnet/runtime, though initially it will be a standalone document.
# Principles
Here are a guiding set of principles of how to successfully combine the communities and teams which work together in dotnet/runtime.
- Retain a 'one community/team' feel for dotnet/runtime
- Leverage automation to label incoming/inflight to enable accountability
- area-* labels should align with a specific community/team for accountability
- Within an area-* there is leeway for community/team specific practices
- Leverage the best ideas from each community/team and find a common pattern for dotnet/runtime
# Details
dotnet/runtime issues and pull requests are a shared resource.  As such, it will likely be one of the first places where coming together as 'one community/team' will be first felt.  In alignment with the Principles the goal is to find a set of norms and best practices which enable the joint community within dotnet/runtime to successfully merge, understand what is expected, act as 'one community/team', and provide leeway at the area level.

Here are a few of the most salient components of working well together, and the FAQ has much more detail.
## Scenarios where we all have to work together:
- All incoming issues and pull requests will be automatically labeled with an `area-*` label. The bot will also assign the `untriaged` label to only issues, once they get created.
- All issues and pull requests should have exactly 1 `area-*` label.
- Issues are considered triaged when the `untriaged` label has been removed.
- When issues have `area-*` labels switched, the `untriaged` label must be added. This prevents issues being lost in a `triaged` state when they have not actually been triaged by the area owner. In the future, a bot may automatically ensure this happens.
- The central repository owner is accountable for triaging issues and pull requests without `area-*` labels.  This occurs when automation is not able to determine the appropriate area.
- Any area-* label that has overlap with merged technologies will be appended with the src/subfolder name: eg. `area-Infrastructure` will also have an `area-Infrastructure-libraries`, `area-Infrastructure-coreclr`, and `area-Infrastructure-installer`.
- There are shared resources in dotnet/runtime which we should be conscientious of use:
- Labels/Milestones – all `area-*` labels are shared, if you are updating/adding keep everyone in dotnet/runtime in mind.  All labels and milestones are shared, be a conscientious citizen when updating/adding.
- ProjectBoards/ZenHub – some aspects of these project boards are shared across all users.
- Wikis/GitActions – these resources are global and are disabled
## Scenarios where area owners will be asked to manage their issues and pull requests:
- All issues with the `untriaged` label are considered untriaged and close to product release, teams will be asked to triage them.
- During a release endgame and for servicing, issues and pull requests targeting a particular release will be asked to have a milestone set.
# FAQ
## What designates a 'triaged' issue?
By default, all incoming issues will be labeled with an `untriaged` label.  All issues with this label require action from the area owner to triage.  At certain times in the release, area owners may be asked to triage their issues.  Triaging an issue may be as simple as removing the `untriaged` label, but for most communities/teams this means assigning an appropriate milestone where the issue is intended to be addressed.

As an aside, all incoming are also expected to be marked with an `area-*` label.  Any issue that fails to receive an `area-*` is also considered untriaged.

As a best practice, as issues move from one area to another the `untriaged` label should be added to the issue to indicate that it needs to be reconsidered within the new context.
## How are milestones handled?
Marking issues with milestones is necessary during release endgame and servicing.  As the release enters an issue burndown, the repository owner may ask area owners to mark issues that should be considered for the current release.

Pull requests for servicing should add the appropriate `major.minor.x` milestone (eg. `3.0.x`).  Once a specific servicing version is determined, the specific milestone will be added to the pull request (eg. `3.0.2`).

It is generally acceptable to have issues without milestones, though this is left to the area owners to decide.  Said another way, not having a milestone does not mean that it is not triaged, see comment above.
## How do you request a review for an issue/pull request if only 1 `area-*` label is applied?
Labeling issues with more than 1 `area-*` label has been used to bring attention to the issue or pull request from multiple teams.  In order to ensure accountability we strive to only have 1 `area-*` label per issue and pull request.  In the event you need to bring the issue or pull request to multiple teams attention, please add them for review as opposed to adding their `area-*` label.
## How will notifications work in dotnet/runtime?
The default github notification system will be used for watching and tracking issue changes.  We will also be using GitHubIssues (https://github.com/karelz/GitHubIssues) to provide notifications as issues enter and exit areas that you subscribe to.
## How are pull requests marked with labels and milestones?
Given the scope of dotnet/runtime, all pull requests will automatically be assigned an `area-*` label.  In addition, some pull requests may have milestones applied according to release endgame and servicing requirements.
## How do you do ongoing management for your repo?
One team manager (M2) (perhaps rotating) will have accountability to ensuring the following global health activities will be accounted for:
- Triaging incoming and assigning area-* labels to those that were not able to do automatically
- Common infrastructure tracking
- Service Level Agreement tracking for responsiveness and a healthy repo
- Release issue burn down

Area level owners will then manage their own pull requests and issues as they see fit.
## How will labels be managed?
There are few access controls, so in general everyone will have access - be a good global citizen.  When in doubt ask the team manager that is responsible.
## What will be dotnet/runtime's branch policy?
General guidance is to rarely create a direct branch within the repository and instead fork and create a branch.  If any branch is created temporarily, it should be deleted as soon as the associated pull request is merged or closed.  Any non-release branch is subject to deletion at any time.

Branches are made for servicing releases and are managed centrally.  Merging into these branches is monitored and managed centrally.

The repositories in dotnet/runtime represent the bottom of the stack for .NET Core.  As such, these repositories often lock down before the rest of .NET Core at the end of a release.  The general policy will be that all code within dotnet/runtime will align in their lockdown dates and policies.
## What is dotnet/runtime's mirror policy?
No specific policy.  But please use common sense if the mirror will have any potential impact on the broader community.
## What is dotnet/runtime's project boards and ZenHub policy?
The portion of ZenHub that are shared across the entire repository is the names of the pipelines (eg. the column names).  As adding and editing these pipelines, it is best to communicate the broadly and build consensus.
## What is dotnet/runtime's policy on Wikis?
Wikis will be disabled for this repository.
## What is dotnet/runtime's policy on GitActions?
GitActions will be disabled for this repository.


