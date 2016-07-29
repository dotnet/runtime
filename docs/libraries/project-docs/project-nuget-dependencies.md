Project NuGet Dependencies
==========================

In CoreFX, dependencies on NuGet packages are stored in `project.json` files. `project.lock.json` files shouldn't be checked in to this repository.

A dependency must be for a specific version of a package: no floating (\*) dependencies are used. This ensures builds are repeatable. CoreFX also has some rules for certain groups of packages that need to stay in sync: for example, all prerelease `System.*` packages must use the same prerelease version. These rules are validated during a build step, so a local build or continuous integration will catch any mistakes.

As a historical note, `project.json` files in CoreFX previously had floating dependencies and `project.lock.json` files were checked in to lock the version for repeatable package restore. This was changed during pull request [#4894](https://github.com/dotnet/corefx/pull/4894).

Dependency version validation
-----------------------------

The dependencies in each CoreFX project.json file are validated by a few rules to ensure package versions across the repository stay in sync. These rules are in `dir.props`, as `ValidationPattern` items. Each `ValidationPattern` item includes a regular expression that matches a set of package identities and metadata describing which version is expected for that regular expression. The metadata must be one of these:

* `ExpectedPrerelease`: a prerelease version is expected, but any major, minor, and patch versions are fine. For example, an `ExpectedVersion` of `rc-12345` is valid for `1.0.0-rc-12345`, `5.4.3-rc-12345`, or any other `*-rc-12345`
* `ExpectedVersion`: the full version string needs to match exactly. An example is `1.0.0-prerelease`. 

Errors you can expect from failed dependency version validation are like the following:

    error : Dependency validation error: for System.IO 4.0.10-rc2-10000 in src\System.Collections\tests\project.json package prerelease is 'rc2-10000', but expected 'rc2-23604' for packages matching '^((System\..*)|(Microsoft\.CSharp)|(Microsoft\.NETCore.*)|(Microsoft\.Win32\..*)|(Microsoft\.VisualBasic))(?<!TestData)$'

    error : Dependency validation error: for System.Linq 4.0.0-beta-* in src\System.Collections\tests\project.json package prerelease is 'beta-', but expected 'rc2-23604' for packages matching '^((System\..*)|(Microsoft\.CSharp)|(Microsoft\.NETCore.*)|(Microsoft\.Win32\..*)|(Microsoft\.VisualBasic))(?<!TestData)$'
    error : Floating dependency detected: System.Linq 4.0.0-beta-* in src\System.Collections\tests\project.json

    error : Dependency validation error: for xunit 2.0.0 in src\System.Collections\tests\project.json package version is '2.0.0' but expected '2.1.0' for packages matching '^xunit$'

To fix these, you can manually modify the `project.json` files mentioned or automatically fix them using the `UpdateInvalidPackageVersions` target described in the next section.

Upgrading a package dependency
------------------------------

To update a package that isn't validated by a rule, simply change the project.json file.

To update a package that is validated, follow these steps:

1. Edit `ValidationPattern` item(s) in `dir.props` in the CoreFX root.
2. Run the dependency update target in the repository root using this command:

        build.cmd -UpdateInvalidPackageVersions

3. Commit the automated updates in an independent commit, isolating them from other changes. This makes pull requests easier to review.

The `UpdateInvalidPackageVersions` target looks through all dependencies, using the validation rules to update any invalid versions. The build prints out which dependencies are updated and which project.json files are written.

Recovering from a non-existent prerelease dependency
----------------------------------------------------

Sometimes upgrading the prerelease version can make `project.json` files contain dependencies that don't exist. For example, if you update a rule's valid prerelease from `beta-100` to `beta-200`, but around `beta-150` a stable version of `System.Foo` was released and prereleases changed from `1.0.0-beta-...` to `1.1.0-beta-...`, automatic package updating would result in a dependency on `1.0.0-beta-200`. However, only `1.1.0-beta-200` exists.

To fix this, use the `UpdatePackageDependencyVersion` target:

    msbuild /t:UpdatePackageDependencyVersion /p:PackageId=System.Foo;OldVersion=1.0.0-beta-200;NewVersion=1.1.0-beta-200

This updates the `System.Foo` version in all `project.json` files in CoreFX.
