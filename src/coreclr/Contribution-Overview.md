## Contribution Overview ##

This repository contains source-code that produces CoreCLR runtime used by number of projects (e.g. Windows Phone, ASP.NET 5, to name a few) to enable execution of managed code. Equally important is to understand that both CoreCLR and Desktop CLR (part of the .NET Framework) are built from the same source code and this repository contains a subset of that source code - targeted to build CoreCLR. As a result, any contributions made to this repository can have a tremendous impact - within Microsoft and to its customers across the globe - and will be reviewed very closely.

**Workflow**

At a high-level, this is how the contribution workflow looks like:

1. Create a fork of CoreCLR repository
2. Create a branch off the master in your fork. Name it something that that makes sense, such as issue-123 or githubhandle-issue. This makes it easy for everyone to figure out what the branch is used for. It also makes it easier to isolate your change from incoming changes from the origin.
3. For all the [architectures and build configurations on the supported OS platforms](https://github.com/dotnet/coreclr/wiki/Developer-Guide):
	1. Make sure that the builds are clean.
	2. Make sure that the tests are are passing.
4. Add a new test corresponding to your change, if applicable, and making sure that it passes.
5. Commit your changes and push your changes to your branch on GitHub
6. Create a pull request against the **dotnet/coreclr** repository's **master** branch
		
The pull-request (PR) will go through the usual review process. Once PR request comes in, our CI (continuous integration) system will perform the required builds and run tests (including the ones you are expected to run) against it. If the builds are clean, tests are passing and **at-least 2 developers** (could be more, depending on the context and nature of the change) from the .NET Runtime team have signed off on the change, the PR will be approved and merged.

If the CI build fails for any reason, the PR issue will be updated with a link that can be used to determine the cause of the failure.

_Note:_ As we are working to bring up the Linux port of the repo, our CI system will also gate the PRs against Linux builds to ensure that they are not broken. Usually, a PR will not break the Linux build but this is to identify the one-off change that may. This will also enable us to bring the Linux developers the same engineering experience, as on other platforms, sooner.

**Understanding the TFS-Git Mirror**

As mentioned above, CoreCLR and Desktop CLR share the same source code. We want to ensure that innovations made in one can flow successfully to the other. It also means that they should flow out to our repo and likewise, any changes committed to the repo should flow back to our internal branches. To facilitate this, we have a TFS-Git bidirectional mirroring infrastructure that will propagate any changes from our Desktop CLR branches to the Git repo and vice-versa.

The mirroring infrastructure uses the following hint files to mirror a given TFS folder into GitHub and back:

1. `.gitmirror` - any folder containing this file will **only** have its contained files mirrored. Subfolders are **not** mirrored.
2. `.gitmirrorall` - any folder containing this file will have all of its files **and** subfolders mirrored recursively. The subfolders do not need to have any hint files.

Thus, if you add a new folder to be included as part of the CoreCLR build, it will also need to have one of the two hint files mentioned above.

In summary, the mirror enables your contribution to make its way into concrete products produced by the .NET team like CoreCLR, ASP.NET v5, Desktop CLR, to name a few.

## Why CoreCLR (or, why not Desktop CLR)? ##

Given that the sources in this repository only support building CoreCLR but are common to building both CoreCLR and Desktop CLR, it is intuitive to wonder why we do not build Desktop CLR in this repository as well.

At a high-level, Desktop CLR is a way more complex product - it's build is very complex and produces way more artifacts (compared to CoreCLR) that are required for it to function completely and validate its feature set. Additionally, it also tightly integrated with the Desktop OS to enable various features. Simply porting the Desktop CLR build to happen in the open would be a multi-man year effort and unless all dependencies are ported, simply being able to build will not be of much practical use to the developer community.

We have few key goals with making CoreCLR sources open:

1. Address the curiosity of folks who are keen to understand the internals of managed code execution (and in turn, learn more about the guts of a virtual machine)
2. Deliver a managed runtime that can support a variety of managed code execution scenarios, especially those that are required to support our modern web stack (i.e. ASP.NET 5).

CoreCLR is a very self-contained managed runtime that helps address these goals, while at the same time, deliver a high degree of fidelity to support managed code execution across the board. It is quicker to port to different architectures and/or platforms (especially when compared with Desktop CLR) and does not have a complex install/validation process (it can be xcopied and executed). And given that it is built from the same sources as the Desktop CLR, folks who are interested to know learn more about low-level components (e.g. JIT, GC, TypeSystem, Assembly Binder/Loader to name a few), which are common to both, can use the sources in this repository to do so.

**How to confirm if your change could regress Desktop CLR?**

This is a work in progress. For now, we will review the changes closely to ensure that any potential for regressing the Desktop CLR is minimized. For you, the developer, it means that your change should be under a `#ifdef FEATURE_CORECLR`. 

Long term, we are working on augmenting our CI system to perform Desktop CLR validation against the PR as well, in addition to PR reviews.