Changing or adding new public APIs to System.Private.CoreLib
============================================================

Many of the CoreFX libraries type-forward their public APIs to the implementations in System.Private.CoreLib.
- The CoreFX build uses System.Private.CoreLib via Microsoft.TargetingPack.Private.CoreCLR Nuget package.
- Some of the CoreFX libraries are not built in the CoreFX repository. For example, System.Runtime.Loader.dll is purely a facade and type-forwards everything to System.Private.CoreLib. These libraries are built and published through a separate process.
- Hence, when adding a new public API to System.Private.CoreLib or changing the behavior of the existing public APIs in System.Private.CoreLib, changes must be staged to ensure that new prerequisites are published before they are used.

**Staging the changes**

Make the changes in both CoreCLR and CoreFX
- System.Private.CoreLib implementation changes should should be made in CoreCLR repo
- Test and public API contract changes should be made in CoreFX repo
- [Build and test](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md#testing-with-private-coreclr-bits) both changes together

Submit PR to both CoreCLR and CoreFX
- Link the two PRs together via comment in PR description
- Both PRs will reviewed together by the project maintainers
- CoreFX tests run on CoreCLR PRs are using old published build. You may need to disable the outdated tests in https://github.com/dotnet/coreclr/blob/master/tests/CoreFX/CoreFX.issues.json to make your PR green.
- CoreCLR PR will be merged first
- If your change is under [System.Private.CoreLib Shared Sources](https://github.com/dotnet/coreclr/tree/master/src/System.Private.CoreLib/shared), it will get mirrored to other repos that are reusing the CoreLib sources.
- Once updated CoreCLR is consumed by CoreFX repo, CoreFX PR will be merged second.
