Project NuGet Dependencies
==========================

In CoreFX, dependencies on NuGet packages are stored in `project.json` files. `project.lock.json` files shouldn't be checked in to this repository.

A dependency must be for a specific version of a package: no floating (\*) dependencies are used. This ensures builds are repeatable. CoreFX also has some rules for certain groups of packages that need to stay in sync: for example, all prerelease `System.*` packages must use the same prerelease version. These rules are validated during a build step, so a local build or continuous integration will catch any mistakes.

As a historical note, `project.json` files in CoreFX previously had floating dependencies and `project.lock.json` files were checked in to lock the version for repeatable package restore. This was changed during pull request [#4894](https://github.com/dotnet/corefx/pull/4894).

Dependency version validation
-----------------------------

The dependencies in each CoreFX project.json file are validated by a few rules in `dependencies.props` to ensure package versions across the repository stay in sync.

Errors you can expect from failed dependency version validation are like the following:

    C:\git\corefx\Tools\VersionTools.targets(47,5): error : Dependency verification errors detected. To automatically fix based on dependency rules, run the msbuild target 'UpdateDependencies' [C:\git\corefx\build.proj]
    C:\git\corefx\Tools\VersionTools.targets(47,5): error : Dependencies invalid: In 'C:\git\corefx\src\Common\test-runtime\project.json', 'Microsoft.DotNet.BuildTools.TestSuite 1.0.0-prerelease-00704-04' must be '1.0.0-prerelease-00704-05' (Microsoft.DotNet.BuildTools.TestSuite) [C:\git\corefx\build.proj]
    C:\git\corefx\Tools\VersionTools.targets(47,5): error : Dependencies invalid: In 'C:\git\corefx\src\Common\tests\project.json', 'Microsoft.xunit.netcore.extensions 1.0.0-prerelease-00704-04' must be '1.0.0-prerelease-00704-05' (Microsoft.xunit.netcore.extensions) [C:\git\corefx\build.proj]

To fix these, you can manually modify the `project.json` files mentioned or automatically fix them using the `UpdateDependencies` target described in the next section.

Upgrading a package dependency
------------------------------

To update a package that isn't validated by a rule, simply change the project.json file.

To update a package that is validated, follow these steps:

1. Edit the versions repo commit hashes in `(CoreFx|CoreClr|External)CurrentRef` and `StaticDependency` versions in `dependencies.props` in the CoreFX root.
2. Run the dependency update target in the repository root using this command:

        build-managed.cmd -- /t:UpdateDependencies

3. Commit the automated updates in an independent commit, isolating them from other changes. This makes pull requests easier to review.

The `UpdateDependencies` target looks through all dependencies, using the validation rules to update any invalid versions. On `/verbosity:Normal` or higher, it logs which files were changed.
