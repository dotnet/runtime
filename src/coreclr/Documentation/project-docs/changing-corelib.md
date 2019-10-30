Changing or adding new public APIs to System.Private.CoreLib
============================================================

### Context 
Many of the CoreFX libraries type-forward their public APIs to the implementations in `System.Private.CoreLib`.
- The CoreFX build uses `System.Private.CoreLib` via a NuGet package named `Microsoft.TargetingPack.Private.CoreCLR`
- Some of the CoreFX libraries are not built in the CoreFX repository. For example, `System.Runtime.Loader.dll` is purely a facade and type-forwards everything to `System.Private.CoreLib`. These libraries are built and published through a separate process.
- Hence, when adding a new public API to `System.Private.CoreLib` or changing the behavior of the existing public APIs in `System.Private.CoreLib`, you have to follow the sequence below to stage your changes so that new prerequisites are published before they are used.

## How to stage your change

### (1) Make the changes in both CoreCLR and CoreFX
- `System.Private.CoreLib` implementation changes should be made in CoreCLR repo
- Test and public API contract changes should be made in CoreFX repo
- [Build and test](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md#testing-with-private-coreclr-bits) both changes together

### (2) Submit PR to both CoreCLR and CoreFX
- Link the two PRs together via comment in PR description, and link both to the issue itself.
- Both PRs will reviewed together by the project maintainers
- CoreCLR runs CoreFX tests but they are behind CoreFX. You may need to disable the outdated tests in https://github.com/dotnet/coreclr/blob/master/tests/CoreFX/CoreFX.issues.rsp to make your PR green.

### (3) What happens next
- We will merge the CoreCLR PR first
- Note: if your change is under [System.Private.CoreLib Shared Sources](https://github.com/dotnet/coreclr/tree/master/src/System.Private.CoreLib/shared), it will get mirrored to other repos that are reusing the CoreLib sources. This is a one-way mirror of sources for code reuse purposes: it does not bring your new API to CoreFX so it is not relevant to this staging process.
- The CoreCLR changes will be consumed by CoreFX via an automatically created PR that updates a hash in the CoreFX repo. These PR's [look like this](https://github.com/dotnet/corefx/pulls?utf8=%E2%9C%93&q=is%3Apr+sort%3Aupdated-desc+coreclr++base%3Amaster+author%3Adotnet-maestro-bot+)
- Depending on the nature of the change, we may cherry-pick your CoreFX PR into this automatically created PR; or, we may merge your PR after we merge the automatically created PR.
- You are done! Thank you for contributing.
