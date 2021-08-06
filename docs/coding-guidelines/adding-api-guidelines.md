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

`net5.0` is the target framework version currently under development and the new apis
should be added to `net5.0`. [More Information on TargetFrameworks](https://docs.microsoft.com/en-us/dotnet/standard/frameworks)

## Making the changes in repo

**Update tests**
  - Add new `TargetFramework` to the ```TargetFrameworks```.
  - Add new test code following [conventions](project-guidelines.md#code-file-naming-conventions) for new files to that are specific to the new target framework.
  - To run just the new test targetFramework run `dotnet build <Library>.csproj -f <TargetFramework> /t:Test`. TargetFramework should be chosen only from supported TargetFrameworks.

## Documentation

New public APIs must be documented with triple-slash comments on top of them. Visual Studio automatically generates the structure for you when you type `///`.

If your new API or the APIs it calls throw any exceptions, those need to be manually documented by adding the `<exception></exception>` elements.

After your change is merged, we will eventually port them to the dotnet-api-docs repo, where we will review them for language and proper style (For more information, see the [API writing guidelines](https://github.com/dotnet/dotnet-api-docs/wiki)).

Once the dotnet-api-docs change is merged, your comments will start showing up in the official API documentation at http://docs.microsoft.com/, and later they'll appear in IntelliSense in Visual Studio and Visual Studio Code.
Once the documentation is official, any subsequent updates to it must be made directly in https://github.com/dotnet/dotnet-api-docs/. It's fine to make updates to the triple slash comments later, they just won't automatically flow into the official docs.

## FAQ

_**What to do if you are moving types down into a lower contract?**_

If you are moving types down you need to version both contracts at the same time and temporarily use
project references across the projects. You also need to be sure to leave type-forwards in the places
where you removed types in order to maintain back-compat.
