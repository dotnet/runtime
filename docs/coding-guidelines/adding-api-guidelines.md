Recommended reading to better understand this document:
[.NET Standard](https://github.com/dotnet/standard/blob/master/docs/faq.md)
| [Project-Guidelines](project-guidelines.md)
| [Package-Projects](package-projects.md)

# Add APIs

- [Add APIs](#add-apis)
    - [Determine what library](#determine-what-library)
    - [Determine target framework](#determine-target-framework)
    - [Determine library version](#determine-library-version)
  - [Making the changes in repo](#making-the-changes-in-repo)
  - [Documentation](#documentation)
  - [FAQ](#faq)

### Determine what library

- Propose a library for exposing it as part of the [API review process](https://aka.ms/apireview).
- Keep in mind the API might be exposed in a reference assembly that
doesn't match the identity of the implementation. There are many reasons for this but
the primary reason is to abstract the runtime assembly identities across
different platforms while sharing a common API surface and allowing us to refactor
the implementation without compat concerns in future releases.

### Determine target framework

`net11.0` is the target framework version currently under development and the new apis
should be added to `net11.0`. [More Information on TargetFrameworks](https://learn.microsoft.com/dotnet/standard/frameworks)

## Making the changes in repo

**Implement your API modification**
  - Implement your API modification in the appropriate library project.

**Update the reference source**
  - [Update the reference source](updating-ref-source.md) for the library.

**Update tests**
  - Add new `TargetFramework` to the ```TargetFrameworks```.
  - Add new test code following [conventions](project-guidelines.md#code-file-naming-conventions) for new files to that are specific to the new target framework.
  - To run just the new test targetFramework run `dotnet build <Library>.csproj -f <TargetFramework> /t:Test`. TargetFramework should be chosen only from supported TargetFrameworks.

## Documentation

New public APIs must be documented with triple-slash comments on top of them. Visual Studio automatically generates the structure for you when you type `///`.

[API writing guidelines](https://github.com/dotnet/dotnet-api-docs/wiki) has information about language and proper style for writing API documentation.
If your new API or the APIs it calls throw any exceptions, those need to be manually documented by adding the `<exception></exception>` elements.

After your change is merged, we will eventually port them to the [dotnet-api-docs](https://github.com/dotnet/dotnet-api-docs) repo. The tools used for this port live in [api-docs-sync](https://github.com/dotnet/api-docs-sync) repo. Once the dotnet-api-docs change
is merged, your comments will start showing up in the official API documentation at https://learn.microsoft.com, and later they'll appear in IntelliSense
in Visual Studio and Visual Studio Code.

The rest of the documentation workflow depends on whether the assembly has the `UseCompilerGeneratedDocXmlFile` property set in its project file:

**For libraries without this property (or with it set to `true`, which is the default):**
- Source comments in this repo are the source of truth for documentation.
- Triple-slash comments in source code are synced to dotnet-api-docs periodically (every preview).
- More recently introduced libraries typically follow this workflow.

**For libraries with `<UseCompilerGeneratedDocXmlFile>false</UseCompilerGeneratedDocXmlFile>`:**
- The [dotnet-api-docs](https://github.com/dotnet/dotnet-api-docs) repo is the source of truth for documentation.
- Triple-slash comments in source code are synced to dotnet-api-docs **only once** for newly introduced APIs. After the initial sync, all subsequent documentation
updates must be made directly in the dotnet-api-docs repo.
- It's fine to make updates to the triple-slash comments later to aid local development, they just won't automatically flow into the official docs. Copilot can help with porting small changes
in triple-slash comments to dotnet-api-docs. [PortToDocs](https://github.com/dotnet/api-docs-sync/blob/main/docs/PortToDocs.md) tool works better for ports of large changes.
- Older libraries typically follow this workflow. Libraries in this mode can work towards a better workflow in the future by using api-docs-sync tools to port back docs to source, then removing the `UseCompilerGeneratedDocXmlFile` property.

## FAQ

_**What to do if you are moving types down into a lower contract?**_

If you are moving types down you need to version both contracts at the same time and temporarily use
project references across the projects. You also need to be sure to leave type-forwards in the places
where you removed types in order to maintain back-compat.
