# Localization: .NET Core host and runtime

The .NET Core host and runtime contain messages that can be displayed to both end-users and developers. Currently, all such messages are displayed in English.

Other managed components built on top of .NET Runtime (e.g. SDK, WinForms, WPF) have already been localized, so a process already exists for handling translation and localizing assets, while the runtime handles satellite assembly loading. The host and runtime are different in that they have messages that originate from native components and must continue to do so. While the runtime does contain some managed resources, this document focuses on localization of native resources.

The goal is to support:

  - Windows, Linux, and OSX
    - Windows was identified as the highest priority due to WPF and WinForms scenarios
  - 14 languages (same languages supported by Visual Studio): `cs`, `de`, `en`, `es`, `fr`, `it`, `ja`, `ko`, `pl`, `pt-BR`, `ru`, `tr`, `zh-Hans`, `zh-Hant`
    - All the target languages are left-to-right

## String localization on different platforms

### Windows

On Windows, [resource script (.rc) files](https://docs.microsoft.com/cpp/windows/resource-files-visual-studio) are used to create resources that will be embedded into a binary. These files define [`STRINGTABLE`](https://docs.microsoft.com/windows/win32/menurc/stringtable-resource) resources containing the resource strings. Each string has a resource identifier - a symbol name mapped to an integer value - which can be used to look up the string value.

The [`LoadString`](https://docs.microsoft.com/windows/win32/api/winuser/nf-winuser-loadstringw) and [`FormatMessage`](https://docs.microsoft.com/windows/win32/api/winbase/nf-winbase-formatmessage) APIs retrieve a string resources based on a specified identifier (the integer value of the resource identifier) from a specified module. These APIs leave it to their consumer to find and load the appropriate module containing the desired resources. While resources for all languages can be included in the main binary itself, it is common to separate language-specific resources into resource-only libraries.

### Linux

The [GNU `gettext` APIs and tools](https://www.gnu.org/software/gettext/manual/gettext.html) are the standard for internationalization and localization on Linux. The tools provide a way to extract strings from C/C++ sources into separate source string ([.po](https://www.gnu.org/software/gettext/manual/gettext.html#PO-Files)) files (which could then be translated) and produce binary ([.mo](https://www.gnu.org/software/gettext/manual/gettext.html#MO-Files)) files from those source string files. The APIs allow retrieval of the translated strings through a `msgid` (string), where the convention is to use the untranslated string as the `msgid`.

The `gettext` API looks for the binary files in a folder of the format:

    <directory_name>/<locale>/LC_MESSAGES/<domain_name>.mo

The `<directory_name>` and `<domain_name>` can be [configured](https://www.gnu.org/software/gettext/manual/gettext.html#Ambiguities) via the `dgettext` and `bindtextdomain` APIs. The `<locale>` is that of the current process. Users can configure the locale through [environment variables](https://www.gnu.org/software/gettext/manual/gettext.html#Locale-Environment-Variables).

### OSX

For OSX bundles, separate [strings resource (.strings) files](https://developer.apple.com/library/archive/documentation/Cocoa/Conceptual/LoadingResources/Strings/Strings.html) are used for string localization. The platform provides a tool to extract strings from sources into .strings files and APIs for retrieving the strings from the .strings files. The .strings files are a mapping key strings to corresponding value strings, where it is common to use the untranslated string as the key.

The Core Foundation framework provides [`CFCopyLocalizedString*` macros](https://developer.apple.com/documentation/corefoundation/cfcopylocalizedstring) for loading string resources. They will look for the strings files in a folder of the format:

    <bundle_folder>/<locale>.lproj/<table_name>.strings

The `<bundle_folder>` and `<table_name>` are based on the bundle specified in the API call. The `<locale>` is that of the system.

Bundles are a concept applied to directories laid out in a known structure. Without an actual bundle, [`CFBundleCreate`](https://developer.apple.com/documentation/corefoundation/1537154-cfbundlecreate) can still be used to create a bundle from any specified directory and [`CFCopyLocalizedStringFromTableInBundle`](https://developer.apple.com/documentation/corefoundation/cfcopylocalizedstringfromtableinbundle) can be used to retrieve the localized strings.

## Current state

### Host

All strings are currently hard-coded in the hosting components directly where they will be displayed. There are no utilities or infrastructure around resource strings. All strings that would require localization are from native components.

#### Hosting components

The host has multiple components that are deployed in different ways, live in separate places, and can be of different versions. This means that there will need to be separation between their resources as well. The approach to localization may also vary based on the different use cases for each component.

### Runtime

On Windows, the English resource strings are in separate `mscorrc.debug` and `mscorrc` libraries. On Linux and OSX, the English resource strings are compiled into `coreclr` itself (as string constants, not an embedded resource). String resources exist in both native (.rc) and managed (.resx).

Some infrastructure is in place for loading of resources on Windows, but is not fully tested. Infrastructure for resource loading on Linux existed (recently removed), but was also untested. There was never any attempt at support for resource loading on OSX.

## Proposed

### Host

Each host component will include English resource strings by default. If the resource for the appropriate locale could not be found, the host components can always fall back to English.

`dotnet`, `hostfxr`, and `hostpolicy` will each have separate resources that will be installed by the .NET runtime. These components can be different versions, so their resources must be separate.

`apphost` will have a single localized message. All languages will be included in the executable itself. The message will direct the user to a URL that will contain localized content.

`ijwhost`, and `nethost` intentionally do not show messages by default. They will not be localized.

`comhost` also intentionally does not show messages, but it does populate an `IErrorInfo` with an error string, allowing consumers to access any error messages. This can take the same approach as `apphost`, but would be a lower priority for localization.

#### Hosting components

`dotnet`, `hostfxr`, and `hostpolicy` are all included as part of a .NET Core install. They can each carry their own separate resources in a known path relative next to their current install locations.

The other entry-point hosts (`apphost`, `comhost`, `ijwhost`, `nethost`) add some complication as they represent and ship as part of the developer's application or component. They are also the most impactful in terms of file size, as they are not shared across applications the way that other components can be.

The messaging coming from the hosts themselves is a small portion of the host messaging. They are mostly around:
- Failure to find `hostfxr` (all hosts)
- Failure to load `hostfxr` (all hosts)
- Single-file bundle reading and extraction issues (`apphost` only)

Possible options for hosts:

*Deploy resources with each host*

  - Every host comes with its own resources, so compatibility will not be a problem
  - Hosts will still be localized when there is no runtime
  - Issues:
    - SDK/deployment logic around including the appropriate resources and gestures for choosing those resources

  - Options:
    1. Embedded resources for hosts
        - Size bloat could be impactful, since it would be for each host and one app can be comprised of multiple hosts
        - Deployment would not strictly have to change if everything is always embedded, but it would likely be desirable to have at least two versions (only English and all languages embedded) and allow users to choose
        - The canonical way of doing native localization is having separate resource files, so embedding everything would mean having to come up with a different, special method
        - This would also be necessary for [single-file](#single-file) support

    2. Separate resource for each host
        - Number of files on disk would increase greatly (each host x each language)
        - Deployment becomes very complicated, since the hosts represent the user's app/component and are acquired through building with the SDK (`nethost` adds more complication as it is up to the users to acquire and deploy it)
        - Developers would need some way of choosing languages to include

*Install resources with .NET Core*

  - If the runtime is not installed/found, hosts will not have localized resources
  - If the hosts are newer than installed runtime, new messages would not be localized
  - Issues:
    - Compatibility concerns for resource IDs / format values
    - Awkward split deployment since `*host` components are not normally part of the .NET runtime install

  - Options:
    1. Separate resource for each host
        - Resource for each host would still need to be backwards compatible, since there is no guarantee that the host is the same version as the resource installed with the runtime
        - Some messages are used for all hosts, so they would be duplicated across resource libraries

    2. Option: Shared resource for all hosts (except `dotnet`)
        - Compatibility requirement for resource shared between hosts

`comhost`, `ijwhost`, and `nethost` are designed to be consumed by a component that .NET Core does not own and intentionally do not show messages to the user. As such, they sit at a low priority for localization support.

`apphost` is the end-user-facing host. The amount of logic and messaging in `apphost` is intentionally limited. The most important message it contains for an end-user is for the missing .NET runtime scenario, so it should not rely on resources installed via the .NET runtime.

Embedding resources in the `apphost` would make for the most stream-lined user experience (particularly around deployment). Since the `apphost` is sensitive to size, the number of messages will be pared down to one generic localized message which directs the user to a URL.

Options:

  1. No localization
      - Main user-facing message already has a URL, which could be updated to include user locale such that it would direct to a page based on the user locale
  2. Single localized message pointing to a URL for error details
      - Show English error message. Show localized message pointing the user to some URL to get details (which would direct to a page based on the user locale).
      - If this experience is acceptable, it would be applicable for all future error messages in the host
      - Need to own or partner more closely with server / docs
  3. Single localized message indicating an error and pointing to a URL for error details
      - Show localized message indicating error occurred running the application and pointing to a URL (which would direct to a page based on the user locale). Additional error details in English.
      - Need to own or partner more closely with server / docs

Both (2) and (3) represent similar amounts of work. (2) would ensure that the single message would not need to change and no other messages would need to be added in the future. (3) provides a slightly nicer user experience. In all cases, the user would be shown a URL that would direct to localized content.

### Runtime

The `mscorrc.debug` and `mscorrc` resource libraries will be combined into one. All native components use resources from `mscorrc`.

`System.Private.CoreLib` will have `System.Private.CoreLib.resources` satellite assemblies and rely on the satellite assembly loading infrastructure in .NET Core to work.

### Locale

Localization for native components will be based on the user's locale.

Standard methods for native localization use the user's locale. However, managed satellite assemblies also respect the thread's current culture. If the managed thread's current culture is not the same as the user's locale, this could result in mixed languages. Attempting to have the native components also follow the managed thread's culture would introduce issues and add significant complexity:
  - Managed code may not be callable at the time a message needs to be displayed
  - Some messages propagate up from the host (e.g. errors in `AssemblyDependencyResolver`) and host components do not have a simple way to access the managed thread's culture
  - Not all platforms have a standard way to choose a specific language outside of process-wide configuration (e.g. `gettext` on Linux always uses the locale based on environment variables)

### Translated assets

All localizable resources need to be in the XLIFF file format (.xlf). New tooling will be required to convert from an untranslated base format to language-specific .xlf files and from the language-specific .xlf files to a format (.rc/.po/.strings, UTF-8/UTF-16) that will be compiled into resource libraries (or deployed directly) for each platform.

The existing [xliff-tasks](https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.XliffTasks/README.md) tooling supports conversion between managed resource files (.resx) and .xlf files and building satellite resource libraries. It is all MSBuild-based and has no concept of native resources or build processes. Extending it in a way that works naturally with CMake builds across platforms would be non-trivial.

It is also an option to create tooling directly integrated in the dotnet/runtime repo itself. This would not be a generic and reusable component outside of the dotnet/runtime repo and its build system.

### Platforms

Each platform has its own standard way and file formats for handling localization. There are two main approaches that can be taken here:
  1. Convert a base format to platform-specific localization formats and use platform-specific localization support
      - Requires a tool/script to convert file formats at build time
      - Uses platform support and standard localization methods that the .NET team will not need to maintain
      - Different method for handling localized resources on each platform
  2. Create a custom solution: storage format for string resources, tools for generation from the [translated assets (.xlf)](#translated-assets), and implementation for reading
      - Requires design of completely custom support that will need to be maintained by the .NET team on all platforms
      - Allows support for both string resources from file or memory
      - All platforms could use the same method for handling localized resources

For [single-file](#single-file) support without any separate files, (1) would not be sufficient and a custom solution (2) would be required. For single-file support where localized resources can be included as separate files, the platform-specific solutions could be used.

#### Windows-specific

Resource script (.rc) files will be used as the main source of string values written in the development language (English). These base .rc files will be used to update [.xlf files](#translated-assets). Those which will then be compiled into language-specific resource libraries.

The host and runtime will follow the typical [Windows method](#windows) for localization of native string resources. Resources for each language will be compiled into a resource-only library, laid out in a language-specific subfolder. For example:

```
host/fxr/<version>
  hostfxr.dll
  fr
    hostfxr.resources.dll
shared/Microsoft.NETCore.App/<version>
  coreclr.dll
  hostpolicy.dll
  fr
    hostpolicy.resources.dll
    mscorrc.dll
```

At run time, the .NET Core host/runtime will find and load the resource DLL from the subfolder corresponding to the user's current locale. If the resource cannot be found, English will be the fallback.

#### Linux-specific

The development language (English) strings will be compiled into the host and runtime directly. The language-specific .xlf files will be converted into .po files and then .mo files. Those binary .mo files will be laid out in a language-specific subfolder. For example:

```
host/fxr/<version>
  libhostfxr.so
  fr
    LC_MESSAGES
      hostfxr.mo
shared/Microsoft.NETCore.App/<version>
  libcoreclr.so
  libhostpolicy.so
  fr
    LC_MESSAGES
      hostpolicy.mo
      mscorrc.mo
```

The `gettext` APIs will be used to retrieve the appropriate message using the development language strings as the `msgid`.

#### OSX-specific

The development language (English) strings will be compiled into the host and runtime directly. The language-specific .xlf files will be converted into .strings files. Those .strings files will be laid out in a language-specific subfolder. For example:

```
host/fxr/<version>
  libhostfxr.dylib
  fr.lproj
    hostfxr.strings
shared/Microsoft.NETCore.App/<version>
  libcoreclr.dylib
  libhostpolicy.dylib
  fr.lproj
    hostpolicy.strings
    mscorrc.strings
```

The `CFCopyLocalizedStringFromTableInBundle` API will be used to retrieve the appropriate message using the development language strings as the key.

#### Custom solution

The development language (English) strings will be compiled into the host and runtime directly. The language-specific `.xlf` files will be converted into a chosen storage format. Those files will be laid out in a language-specific subfolder. For example:

```
host/fxr/<version>
  hostfxr.dll (.so/.dylib)
  fr
    hostfxr.resources
shared/Microsoft.NETCore.App/<version>
  coreclr.dll (.so/.dylib)
  hostpolicy.dll (.so/.dylib)
  fr
    hostpolicy.resources
    mscorrc.resources
```

For single file, the language-specific resources will be bundled into the application's executable.

Cross-platform utilities will be created for resource loading. The reader/parser will support both reading from a file and memory.

### Shared utilities between host and runtime

Both the host and runtime require support for native localization. Since (with the exception of `apphost`) they would use the same approach, rather than each having their own copy, it would make sense for them to share utilities around finding and loading resources.

Ideally, the host and runtime could use the same static lib. However, even though they are now in the same repo, their builds are still fairly partitioned. A reasonable middle ground could be to have source files that are compiled into both the host and runtime components. This does have the complication that the host and runtime have separate PALs, so any shared code would need to work properly with both sides.

### Packaging and deployment

Installers and packages will need to include the language-specific resource files. This could involve updating all existing installers or could mean the creation of multiple new installers.

Exactly how resources should be delivered is an [open question](#packaging-and-deployment-1).

## Open questions

### Language override

Messages from the host and runtime can be user-facing or developer-facing. Some developers do not want to have localized messages, so there should be some way to override localizing to the user's locale. This would need to be a setting that both the host and runtime could easily check.

On Windows, the native component fully controls which resource library to load, so it would be able to check for an override (like an environment variable). The `gettext` APIs on Linux essentially allow this kind of override through environment variables. There is not a clear way to configure the APIs on OSX to override the locale; the .strings files can be loaded/read as a dictionary directly through the [`CFPropertyListCreateWithStream`](https://developer.apple.com/documentation/corefoundation/1430023-cfpropertylistcreatewithstream) API.

The SDK already allows overriding of the locale through the `DOTNET_CLI_UI_LANGUAGE` environment variable.

Any automated testing would likely also require some form of language override.

### Single-file

The standard way of doing native localization is based on having separate resources files. On Windows, it is possible to embed resources for multiple languages into one library and use [`FormatMessage`](https://docs.microsoft.com/windows/win32/api/winbase/nf-winbase-formatmessage) or a combination of [`FindResourceEx`](https://docs.microsoft.com/windows/win32/api/winbase/nf-winbase-findresourceexa) and [`LoadResource`](https://docs.microsoft.com/windows/win32/api/libloaderapi/nf-libloaderapi-loadresource) to load the resource for a specific language. On Linux and OSX, no such platform support exists.

Extracting files to disk has proven to be extremely problematic across all platforms (permissions, anti-virus, clean up). Adding native resources to that extraction would only exacerbate the existing issues. This means that localized resources would need be read from memory. A custom solution would need to be created and maintained:
  - Format for storing identifiers and their corresponding strings for every language
  - Tooling to generate the format based on [translated assets](#translation-and-localized-assets)
  - Reader/parser for the custom format to retrieve the appropriate string

If support for localization of native components in single-file scenarios without separate resource files is a priority, it would make sense to just use the custom solution (that could handle both reading from files and memory) for non-single-file scenarios as well.

Since localization would not be needed by all applications, localized resources could also be considered an add-on to single-file and not part of the single-file itself. To support localization, the developer would need to include separate localized resources alongside the single-file executable. In this case, the platform-specific solution would be used. Since Windows does provide a supported way to handle multiple embedded localized resources, the experience for localization could also be improved on Windows such that it does embed all resources into one library.

### Packaging and deployment

WPF and WinForms always include all languages in an install. Does the host/runtime do the same or have separate language packs? How are they delivered (e.g. single installer with runtime and options for different languages, separate installer for languages with options for different languages, separate installer per language)?

### SDK

Self-contained applications would need to include resources for all languages they support. Some developer input would be required to specify the desired language support and the SDK would need to be updated to handle the different options:
  - All supported languages
  - One specific language
  - A subset of supported languages (more than one)

Note: Building WPF self-contained applications currently includes resources for all languages.
