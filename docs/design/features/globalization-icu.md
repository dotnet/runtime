# .NET Globalization ICU on all runtimes

Author: [Santiago Fernandez Madero](https://github.com/safern)

In .NET Core our globalization APIs behavior used different underlying libraries, in Unix, we used [International Components for Unicode (ICU)](http://site.icu-project.org/home) and in Windows, we used [National Language Support (NLS)](https://docs.microsoft.com/en-us/windows/win32/intl/national-language-support). This caused some behavior differences in a handful number of Globalization APIs:

- Cultures and culture data
- String casing
- String sorting and searching
- Sort keys
- String Normalization
- Internationalized Domain Names (IDN) support
- Time Zone display name on Linux

## ICU on Windows

Windows 10 May 2019 Update and later versions started including [icu.dll](https://docs.microsoft.com/en-us/windows/win32/intl/international-components-for-unicode--icu-) as part of the OS, so in .NET 5.0 we changed the default behavior for Globalization APIs when running in Windows, which tries to load `icu.dll` and if we can load it, we use ICU as the underlying native APIs, if we fail to load it, we use the legacy behavior (NLS), this will be the case for older versions of Windows.

*Note: CurrentCulture, CurrentUICulture and CurrentRegion still use Windows OS APIs to honor user settings*

### Using NLS instead of ICU

Given that this is a change in the Globalization behavior, we added support for disabling this feature, so that applications can use legacy behavior which relies on: [National Language Support (NLS)](https://docs.microsoft.com/en-us/windows/win32/intl/national-language-support).

Applications can enable NLS mode by either of the following:

1. in project file:

```xml
<ItemGroup>
  <RuntimeHostConfigurationOption Include="System.Globalization.UseNls" Value="true" />
</ItemGroup>
```

2. in `runtimeconfig.json` file:

```json
{
  "runtimeOptions": {
     "configProperties": {
       "System.Globalization.UseNls": true
      }
  }
}
```

3. setting environment variable value `DOTNET_SYSTEM_GLOBALIZATION_USENLS` to `true` or `1`.

*Note: value set in project file or `runtimeconfig.json` has higher priority than the environment variable.*

## App-local ICU

In order to provide Common Locale Data Repository (CLDR) and behavior customization, we introduced another feature for applications to be able to carry their own copy of ICU as part of their closure. 

Applications can enable NLS mode by either of the following:

1. in project file:

```xml
<ItemGroup>
  <RuntimeHostConfigurationOption Include="System.Globalization.AppLocalIcu" Value="<suffix>:<version> or <version>" />
</ItemGroup>
```

2. in `runtimeconfig.json` file:

```json
{
  "runtimeOptions": {
     "configProperties": {
       "System.Globalization.AppLocalIcu": "<suffix>:<version> or <version>"
      }
  }
}
```

3. setting environment variable value `DOTNET_SYSTEM_GLOBALIZATION_APPLOCALICU` to `<suffix>:<version>` or `<version>`. 

`<suffix>`: this is optional in the config property, but this follows the public ICU packaging conventions as when building a custom ICU you can customize it to produce the lib names and exported symbol names to contain a suffix. i.e: `libicuucmyapp` where `myapp` is the suffix. This can't be greater than 35 chars in length for the config switch.
`<version>`: this has to be a valid ICU version, i.e: 67.1. This version will be used to load the binaries and to get the exported symbols.

To load ICU when the app-local switch is set, we use `NativeLibrary.TryLoad` api which does probing in different paths, first it tries to find the library in `NATIVE_DLL_SEARCH_DIRECTORIES` property which is created by the dotnet host based on the `deps.json` file for the app. More details [here](https://docs.microsoft.com/en-us/dotnet/core/dependency-loading/default-probing)

For self contained apps, the user doesn't really need to do anything special, other than making sure ICU is side by side in the APP directory, this is because for self-contained apps, it's work directory is by default in `NATIVE_DLL_SEARCH_DIRECTORIES`. 

If you're consuming ICU via a NuGet package, this will work in framework-dependent apps as NuGet will resolve the native assets and include them in the `deps.json` file and in the output directory for the app under the `runtimes` dir, then we will load it from there.

The tricky part comes whenever it is a framework-dependent app (not self contained) and ICU is consumed from a local build. The SDK doesn't yet have a feature for "loose" native binaries to land into `deps.json`: https://github.com/dotnet/sdk/issues/11373. 

However, there is a workaround by adding something like this to the csproj:

```xml
<ItemGroup>
  <IcuAssemblies Include="icu\*.so*" />
  <RuntimeTargetsCopyLocalItems Include="@(IcuAssemblies)" AssetType="native" CopyLocal="true" DestinationSubDirectory="runtimes/linux-x64/native/" DestinationSubPath="%(FileName)%(Extension)" RuntimeIdentifier="linux-x64" NuGetPackageId="System.Private.Runtime.UnicodeData" />
</ItemGroup>
```

Note that this will have to be done for all the ICU binaries for the supported runtimes. Also, the `NuGetPackageId` metadata in the `RuntimeTargetsCopyLocalItems` item group, needs to match a NuGet package that the project actually references, it can't just be a dummy NuGet package.

### macOS behavior

`MacOS` has a different behavior for resolving dependent dynamic libraries from the load commands specified in the `match-o` file than the Linux loader. In the Linux loader, we could just load `libicudata` first, then `libicuuc` and last `libicui18n` in that order to satisfy ICU dependency graph.

However, in MacOS this doesn't work. When building ICU in MacOS, you by default get a dynamic library with these load commands in libicuuc for example:
```sh
~/ % otool -L /Users/santifdezm/repos/icu-build/icu/install/lib/libicuuc.67.1.dylib
/Users/santifdezm/repos/icu-build/icu/install/lib/libicuuc.67.1.dylib:
 libicuuc.67.dylib (compatibility version 67.0.0, current version 67.1.0)
 libicudata.67.dylib (compatibility version 67.0.0, current version 67.1.0)
 /usr/lib/libSystem.B.dylib (compatibility version 1.0.0, current version 1281.100.1)
 /usr/lib/libc++.1.dylib (compatibility version 1.0.0, current version 902.1.0)
```

These commands are just referencing the name of the dependent libraries for the other components of ICU, so the loader will do the search following the `dlopen` conventions. Which involve having these libraries in the system directories or setting the `LD_LIBRARY_PATH` env vars, or having ICU at the app level directory. So if you can't set `LD_LIBRARY_PATH` or make sure that ICU binaries are at the app level directory, you will need to do some extra work.

There are some directives for the loader, like `@loader_path` which tells the loader to search for that dependency in the same directory as the binary with that load command. So there's 2 ways to achieve this:

##### instal_name_tool -change
Running:
```
install_name_tool -change "libicudata.67.dylib" "@loader_path/libicudata.67.dylib" /path/to/libicuuc.67.1.dylib
install_name_tool -change "libicudata.67.dylib" "@loader_path/libicudata.67.dylib" /path/to/libicui18n.67.1.dylib
install_name_tool -change "libicuuc.67.dylib" "@loader_path/libicuuc.67.dylib" /path/to/libicui18n.67.1.dylib
```

##### patching ICU to produce the install names with @loader_path
Before running autoconf (`./runConfigureICU`), you need to change [these lines](https://github.com/unicode-org/icu/blob/ef91cc3673d69a5e00407cda94f39fcda3131451/icu4c/source/config/mh-darwin#L32-L37) to: 
```
LD_SONAME = -Wl,-compatibility_version -Wl,$(SO_TARGET_VERSION_MAJOR) -Wl,-current_version -Wl,$(SO_TARGET_VERSION) -install_name @loader_path/$(notdir $(MIDDLE_SO_TARGET))
```
