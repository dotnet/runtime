# ILLink.Tasks

ILLink.Tasks is a package containing MSBuild tasks and targets that
will run the linker to run during publish of a .NET Core app.

ILLink.Tasks provides an MSBuild task called ILLink that makes it easy
to run the linker from an MSBuild project file:

```xml
<ILLink AssemblyPaths="@(AssemblyFilesToLink)"
        RootAssemblyNames="@(LinkerRootAssemblies)"
        RootDescriptorFiles="@(LinkerRootDescriptors)"
        OutputDirectory="output"
        ExtraArgs="-t -c link -l none" />
```

For a description of the options that this task supports, see the
comments in [LinkTask.cs](LinkTask.cs).


In addition, ILLink.Tasks contains MSBuild logic that makes the linker
run automatically during `dotnet publish` for .NET Core apps. This
will:

- Determine the assemblies and options to pass to illink.
- Remove unused native files from the publish output.

The full set of options is described below.

## Building

```
linker> dotnet restore illink.sln
linker> dotnet pack illink.sln
```

To produce a package:
```
linker> ./eng/dotnet.{sh/ps1} pack illink.sln
```

## Using ILLink.Tasks

Add a package reference to the linker. Ensure that either the
[dotnet-core](https://dotnet.myget.org/gallery/dotnet-core) myget feed
or the path to the locally-built linker package path exists in the
project's nuget.config. If using myget, you probably want to ensure
that you're using the latest version available at
https://dotnet.myget.org/feed/dotnet-core/package/nuget/ILLink.Tasks.

After adding the package, linking will be turned on during `dotnet
publish`. The publish output will contain the linked assemblies.

## Default behavior

By default, the linker will operate in a conservative mode that keeps
all managed assemblies that aren't part of the framework (they are
kept intact, and the linker simply copies them). It also analyzes all
non-framework assemblies to find and keep code used by them (they are
roots for the analysis). This means that unanalyzed reflection calls
within the app should continue to work after linking. Reflection calls
to code in the framework can potentially break when using the linker,
if the target of the call is removed.

For portable publish, framework assemblies usually do not get
published with the app. In this case they will not be analyzed or
linked.

For self-contained publish, framework assemblies are part of the
publish output, and are analyzed by the linker. Any framework
assemblies that aren't predicted to be used at runtime based on the
linker analysis will be removed from the publish output. Used
framework assemblies will be kept, and any used code within these
assemblies will be compiled to native code. Unused parts of used
framework assemblies are kept as IL, so that reflection calls will
continue to work, with runtime JIT compilation.

Native dependencies that aren't referenced by any of the kept managed
assemblies will be removed from the publish output as well.

## Caveats

You should make sure to test the publish output before deploying your
code, because the linker can potentially break apps that use
reflection.

The linker does not analyze reflection calls, so any reflection
targets outside of the kept assemblies will need to be rooted
explicitly using either `LinkerRootAssemblies` or
`LinkerRootDescriptors` (see below).

Sometimes an application may include multiple versions of the same
assembly. This may happen when portable apps include platform-specific
managed code, which gets placed in the `runtimes` directory of the
publish output. In such cases, the linker will pick one of the
duplicate assemblies to analyze. This means that dependencies of the
un-analyzed duplicates may not be included in the application, so you
may need to root such dependencies manually.

## Options

The following MSBuild properties can be used to control the behavior
of the linker, from the command-line (via `dotnet publish
/p:PropertyName=PropertyValue`), or from the .csproj file (via
`<PropertyName>PropertyValue</PropertyName>`). They are defined and
used in
[ILLink.Tasks.targets](ILLink.Tasks.targets).

- `LinkDuringPublish` (default `true`) - Set to `false` to disable
  linking.

- `ShowLinkerSizeComparison` (default `false`) - Set to `true` to
  print out a table showing the size impact of the linker.

- `RootAllApplicationAssemblies` (default `true`) - If `true`, all
  application assemblies are rooted by the linker. This means they are
  kept in their entirety, and analyzed for dependencies. If `false`,
  only the app dll's entry point is rooted.

- `LinkerRootAssemblies` - The set of assemblies to root. The default
  depends on the value of `RootAllApplicationAssemblies`. Additional
  assemblies can be rooted by adding them to this ItemGroup.

- `LinkerRootDescriptors` - The set of [xml descriptors](../linker#syntax-of-xml-descriptor)
  specifying additional roots within assemblies. The default is to
  include a generated descriptor that roots everything in the
  application assembly if `RootAllApplicationAssemblies` is
  `true`. Additional roots from descriptors can be included by adding
  the descriptor files to this ItemGroup.

- `ExtraLinkerArgs` - Extra arguments to pass to the linker. The
  default sets some flags that output symbols, tolerate resolution
  errors, log warnings, skip mono-specific localization assemblies,
  and keep type-forwarder assemblies. See
  [ILLink.Tasks.targets](ILLink.Tasks.targets).
  Setting this will override the defaults.

- Assembly actions: illink has the ability to specify an [action](../linker#actions-on-the-assemblies) to
  take per-assembly. ILLink.Tasks provides high-level switches that
  control the action to take for a set of assemblies. The set of
  managed files that make up the application are split into
  "application" and "platform" assemblies. The "platform" represents
  the .NET framework, while the "application" represents the rest of
  the application and its other dependencies. The assembly action can
  be set for each of these groups independently, for assemblies that
  are analyzed as used and as unused, with the following switches:

  - `UsedApplicationAssemblyAction` - The default is to `Copy` any used
    application assemblies to the output, leaving them as-is.
  - `UnusedApplicationAssemblyAction` - The default is to `Delete` (not
    publish) unused application assemblies.
  - `UsedPlatformAssemblyAction` - For self-contained publish, the
    default is `AddBypassNGen`, which will add the BypassNGenAttribute
    to unused code in used platform assemblies. This causes the native
    compilation step to compile only parts of these assemblies that
    are used. For portable publish, the default is to `Skip` these,
    because the platform assemblies are generally not published with
    the app.
  - `UnusedPlatformAssemblyAction` - For self-contained publish, the
    default is to `Delete` (not publish) unused platform
    assemblies. For portable publish, the default is to `Skip`.

  The full list of assembly actions is described in
  [AssemblyAction.cs](../linker/Linker/AssemblyAction.cs) Some
  combinations of actions may be disallowed if they do not make
  sense. For more details, see
  [SetAssemblyActions.cs](SetAssemblyActions.cs).

- `LinkerTrimNativeDeps` (default `true`) - If `true`, enable
  detection and removal of unused native dependencies. If `false`, all
  native dependencies are kept.
