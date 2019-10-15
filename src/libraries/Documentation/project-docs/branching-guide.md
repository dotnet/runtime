Branching Guide
===============

We will have the following branches in the corefx repository:

* **master**
 * Where most development happens
 * Submit your PRs here unless you are adding API to a type that exists in the full .NET Framework

* **release/[name]**
 * Release branches snapped from master.
 * Do not submit pull requests to these branches
 * Fixes here do not flow to follow-up releases
 * Generally, fixes after a snap needing to make it in to a release will go in to master and get cherry-picked to the release branch.

* **dev/[name]**
 * Features (aka topics) under active development by more than one developer.
 * Submit PRs here only if you've made prior arrangements to work on something in one of these branches.
 * It is up to the developers creating these branches to decide what level of review is required
 * These features will only ship if they are successfully pulled to master or future via the standard PR and API review process.
