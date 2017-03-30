## dotnet/CoreFx
### Libraries in NETStandard
- ref
  - Default targetgroup should be NETCoreApp build
  - P2P references to other reference assembly CSProjs.
  - System.Runtime core assembly.
  - Cross-compiles for concrete frameworks (if different)
    - EG: exposes types/members not in NETStandard on NETCoreApp, but not on UWP
- src
  - Default targetgroup should be NETCoreApp build
  - depends on System.Runtime?
    - Yes: P2P to ref CSProjs
    - No: P2P to implementation projects
  - **Issue:** what if dependency is not in NETStandard?  EG: Buffers
    - P2P reference to pkgproj
    - Reference to NETStandard.dll facade for NETCoreApp
- pkg
  - No individual package builds.
  - We will have a single package for all of NETCore.App's netstandard.library implementation.  Below TBD.
    - framework split packages
      - ref\netcoreappN - all refs for NETCoreApp,Version=vN
    - runtime split packages
      - runtime\{RID}\lib\netcoreappN - all impl for NETCoreApp,Version=vN, cross-gen'ed for RID
- tests
  - By default get all of NETStandard.Library for a specific version that they target (auto-referenced by targets)
  - Use P2P references to pkgproj for things not in NETStandard.Library
  - Implementation is automatically injected by targets.

### Libraries above NETStandard
- ref
  - Only required if component is inbox somewhere or has multiple implementations for same NETStandard version.
  - Build against NETStandard.Library package
  - P2P references to pkgproj for things not in NETStandard.Library
  - For builds that support older platforms (eg: netstandard1.0-1.6) we'll be building against the older contract-based NETStandard, we will need a story for this for build-from-source.
- src
  - Build against NETStandard.Library package
  - P2P references to pkgproj for things not in NETStandard.Library
  - For builds that support older platforms (eg: netstandard1.0-1.6) we'll be building against the older contract-based NETStandard, we will need a story for this for build-from-source.
- pkg
  - Not in NETCore.App: as today
  - In NETCore.App: package in NETCore.App package as above
    - If the library also ships in a package, it will also build a package as today.  This package may or may-not include the same binaries as are used by NETCore.App, for instance if the library builds against older NETStandard versions.
- tests
  - By default get all of NETStandard.Library for a specific version that they target (auto-referenced by targets)
  - Use P2P references for things not in NETStandard.Library
  - Implementation is automatically injected by targets.

### NETStandard  compatibility facade
Provides compatibility between NETCore.App and libraries built against NETStandard.
- ref
  - Should adapt supported NETStandard.dll to contract reference assemblies.
  - EG: `GenFacades -contracts:<netstandard.dll> -seeds:<allNetCoreAppReferenceAssemblies>`
- src
  - Should adapt supported NETStandard.dll to implementation assemblies.
  - EG: `GenFacades -contracts:<netstandard.dll> -seeds:<allNetCoreAppImplementationAssemblies>`
- pkg
  - No individual package builds.
  - Should be included in NETCoreApp package as above

### Desktop compatibility facades
- ref
  - Should adapt latest desktop surface to contract reference assemblies for anything that has type-overlap with desktop, including assemblies like Microsoft.Win32.Registry which are not in NETStandard.Library.
  - EG: `GenFacades -contracts:<desktopReferenceAssemblies> -seeds:<allNetCoreAppReferenceAssemblies>`
- src
  - Should adapt latest desktop surface to netcore app implementation for anything that has type-overlap with desktop, including assemblies like Microsoft.Win32.Registry which are not in NETStandard.Library.
  - EG: `GenFacades -contracts:<desktopReferenceAssemblies> -seeds:<allNetCoreAppImplementationAssemblies>`
- pkg
  - No individual package builds.
  - Should be included in NETCoreApp package as above

### Native shims
- pkg
  - No individual package builds.
  - As with libraries in NETStandard the shims will be included in the runtime specific packages for NETCoreApp

## Transition

### End goal

- CoreFx does not build any reference assemblies for NETStandard.
- For every library in NETStandard.Library, the only configurations in CoreFx are framework-specific.  EG: NETCoreApp1.2, UAP10.1
- For every library in NETCore.App but not in NETStandard.Library there must be a framework-specific configuration for NETCoreApp1.2.  Other configurations may exist to ship in a package, but those will not be built by folks building just NETCore.App.

### Getting there (WIP)

Folks still consume our current packages so we need to keep building those until we transition.

1. Create a new NETCore.App package: Microsoft.Private.CoreFx.NETCore.App.  This will be an identity package with every ref that targets NETCore.App and runtime-specific packages that have all runtime impl's that apply to NETCore.App.
2. Filter the content of Microsoft.Private.CoreFx.NETCore.App to just the things that are part of NETCore, and their closure.
3. Transition tests to use Microsoft.Private.CoreFx.NETCore.App.
4. Delete packages for things that are only part of Microsoft.Private.CoreFx.NETCore.App and don't ship independently.
  - Delete configurations for libraries that are no longer used
  - As packages are deleted we'll need to opt-in to Microsoft.Private.CoreFx.NETCore.App in some way.
    - proposal:
      - each CSProj is evaluated for layout path in the context of all of its build configurations.
      - We'll determine applicability similar to how we do for pkgprojs to identify which config to binplace.
