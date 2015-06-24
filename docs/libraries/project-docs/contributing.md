Contributing to CoreFX
======================

This document describes contribution guidelines that are specific to CoreFX. Please read [.NET Core Guidelines](https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/contributing.md) for more general .NET Core contribution guidelines.

Coding Style Changes
--------------------

We intend to bring dotnet/corefx in to full conformance with the style guidelines described in [Coding Style](coding-style.md). We plan to do that with tooling, in a holistic way. In the meantime, please:

* **DO NOT** send PRs for style changes.
* **DO** give priority to the current style of the project or file you're changing even if it diverges from the general guidelines.

API Changes
-----------

* **DON'T** submit API additions to any type that has shipped in the full .NET framework to the *master* branch. Instead, use the *future* branch. See [Branching Guide](branching-guide.md).
