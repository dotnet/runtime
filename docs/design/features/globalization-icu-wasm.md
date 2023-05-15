# WASM Globalization Icu

In WASM applications when [globalization invariant mode](globalization-invariant-mode.md) is switched off, internalization data file is loaded. There are four basic types of these files:
- `icudt.dat` - full data
- `icudt_EFIGS.dat` - data for locales: "en-*", "fr-FR", "es-ES", "it-IT", and "de-DE".
- `icudt_CJK.dat` - for locales: "en" "ja", "ko", and "zh".
- `icudt_no_CJK.dat` - all locales from `icudt.dat`, excluding "ja", "ko", and "zh".

## Wasm Console, Wasm Browser

We can specify the file we want to load, e.g. `icudt_no_CJK.dat` by adding to .csproj:
```
<WasmIcuDataFileName>icudt_no_CJK.dat</WasmIcuDataFileName>
```
Only one value for `WasmIcuDataFileName` can be set. It can also be a custom file, created by the developer. To create a custom ICU file, see `Custom ICU` section below. If no `WasmIcuDataFileName` was specified, the application's culture will be checked and the corresponding file will be loaded, e.g. for `en-US` file `icudt_EFIGS.dat`, and for `zh-CN` - `icudt_CJK.dat`.

## Custom ICU

The easiest way to build ICU is to open https://github.com/dotnet/icu/ it in [Codespaces](docs\workflow\Codespaces.md). See files in https://github.com/dotnet/icu/tree/dotnet/main/icu-filters, and read https://unicode-org.github.io/icu/userguide/icu_data/buildtool.html#locale-slicing. Build your own filter or edit the existing file.
We advise to edit the filters **only by adding/removing locales** from the `localeFilter/includelist` to avoid removing important data. We recommend not to remove "en-US" locale from the localeFilter/includelist because it is used as a fallback. Removing it for when
- `<PredefinedCulturesOnly>true</PredefinedCulturesOnly>`: results in `Encountered infinite recursion while looking for resource in System.Private.Corelib.` exception
- when predefined cultures only is not set: results in resolving data from ICU's `root.txt` files, e.g. `CultureInfo.DateTimeFormat.GetDayName(DateTime.Today.DayOfWeek)` will return an abbreviated form: `Mon` instead of `Monday`.
Removing specific feature data might result in an exception that starts with `[CultureData.IcuGetLocaleInfo(LocaleStringData)] Failed`. It means you removed data necessary to extract basic information about the locale.

 In the file `eng/icu.mk`, you can choose what filters to build. Choose the platform:

### Building for Browser:
* For prerequisites run `.devcontainer/postCreateCommand.sh` (it is run automatically on creation if using Codespaces)
* Building:
    ```
    ./build.sh /p:TargetOS=Browser /p:TargetArchitecture=wasm /p:IcuTracing=true
    ```
  Output is located in `artifacts/bin/icu-browser-wasm`.

### Building for Mobiles:
* For prerequisites run:
    ```bash
    export ANDROID_NDK_ROOT=$PWD/artifacts/ndk/
    mkdir  $ANDROID_NDK_ROOT
    wget https://dl.google.com/android/repository/android-ndk-r25b-linux.zip
    unzip android-ndk-r25b-linux.zip -d $ANDROID_NDK_ROOT
    rm android-ndk-r25b-linux.zip
    mv $ANDROID_NDK_ROOT/*/* $ANDROID_NDK_ROOT
    rmdir $ANDROID_NDK_ROOT/android-ndk-r25b
    ```
* Building:
 ```bash
  ./build.sh /p:TargetOS=Android /p:TargetArchitecture=x64 /p:IcuTracing=true
  ```

Output from both builds will be located in subdirectories of `artifacts/bin`. Copy the generated `.dat` files to your project location and provide the path to it in the `.csproj`, e.g.:

```xml
<!-- relative path -->
<WasmIcuDataFileName>icudt_custom.dat</WasmIcuDataFileName>

<!-- OR absolute -->
<WasmIcuDataFileName>$(MSBuildThisFileDirectory)icudt_custom.dat</WasmIcuDataFileName>
```

## Blazor

In Blazor we are loading the file based on the applications's culture.
To force the full data to be loaded, add this to your `.csproj`:
```xml
<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>
```
Custom files loading for Blazor is supported **only** for files with names starting with `icudt`, e.g. `icudt_custom.dat`. To load the file use `BlazorIcuDataFileName` property with either a path relative to your project or a full path, like for `WasmIcuDataFileName`.
