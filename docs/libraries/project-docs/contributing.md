Contributing to CoreFX
======================

This document describes contribution guidelines that are specific to CoreFX. Please read [.NET Core Guidelines](https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/contributing.md) for more general .NET Core contribution guidelines.

Coding Style Changes
--------------------

We intend to bring dotnet/corefx into full conformance with the style guidelines described in [Coding Style](../coding-guidelines/coding-style.md). We plan to do that with tooling, in a holistic way. In the meantime, please:

* **DO NOT** send PRs for style changes. For example, do not send PRs that are focused on changing usage of ```Int32``` to ```int```.
* **DO NOT** send PRs for upgrading code to use newer language features, though it's ok to use newer language features as part of new code that's written.  For example, it's ok to use expression-bodied members as part of new code you write, but do not send a PR focused on changing existing properties or methods to use the feature.
* **DO** give priority to the current style of the project or file you're changing even if it diverges from the general guidelines.

API Changes
-----------

* **DO NOT** submit to the *master* branch API additions to any type that has shipped in the full .NET framework. Instead, use the *future* branch. See [Branching Guide](branching-guide.md). Further, do not submit such PRs until the APIs have been approved via the [API Review Process](api-review-process.md).
