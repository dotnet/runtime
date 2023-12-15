# ILLink.Tasks

This library contains MSBuild tasks that run the ILLink as part of the .NET Core toolchain. It uses the same code as ILLink but exposes the command line arguments as MSBuild properties.

More details about how to use the task is in [docs/](/docs/tools/illink/illink-tasks.md) folder.

## Building

To build ILLink.Tasks:

```sh
$ dotnet restore illink.sln
$ dotnet pack illink.sln
```

To produce a package:
```sh
$ ./eng/dotnet.{sh/ps1} pack illink.sln
```
