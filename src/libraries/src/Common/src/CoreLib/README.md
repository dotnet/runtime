# System.Private.CoreLib Shared Sources

This directory contains the shared sources for System.Private.CoreLib. These are shared between [dotnet/corert](https://github.com/dotnet/corert/tree/master/src/System.Private.CoreLib/shared), [dotnet/coreclr](https://github.com/dotnet/coreclr/tree/master/src/mscorlib/shared) and [dotnet/corefx](https://github.com/dotnet/corefx/tree/master/src/Common/src/CoreLib).

The sources are synchronized with a mirroring tool that watches for new commits on either side and creates new pull requests (as @dotnet-bot) in the other repository.

## Conventions

Code in the shared directory should have no code specific to CoreCLR, CoreRT or CoreFX. Parts of classes that need to have different implementations on different runtimes should use partial classes and &#42;.CoreRT.cs/&#42;.CoreCLR.cs/&#42;.CoreFX.cs files in the non shared portion. Code that is different based on platform (Windows/Unix) is fine to leave in the shared portion. Remember to follow the [style guidelines](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md).

## Getting clean CI and merging the mirror PRs

Once the mirror PR is created there is a chance that the new code will require changes to get a clean CI. Any changes can be added to the PR by checking out the PR branch and adding new commits. Please follow the following guidelines for modifying these PRs.

 - **DO NOT** modify the commits made by @dotnet-bot in any way.
 - **TRY** to only make changes outside of shared.
   - Changes made in the shared folder in additional commits will get mirrored properly if the mirror PR is merged with a **REBASE**
 - **ALWAYS** Merge the mirror PR with the **REBASE** option.
   - Using one of the other options will cause the mirror to miss commits
