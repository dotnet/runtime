# WASM Globalization Icu

In WASM applications when [globalization invariant mode(design/features/globalization-invariant-mode.md)) is switched off we loading internalization data files. There are four basic types of these files:
- `icudt.dat` - full data
- `icudt_EFIGS.dat` - data for locales: "en-*" "fr-FR" "es-ES" "it-IT" "de-DE"
- `icudt_CJK.dat` - for locales: "en" "ja" "ko" "zh"
- `icudt_no_CJK.dat` - all locales from `icudt.dat`, excluding "ja" "ko" "zh".

## Wasm Console, Wasm Browser

We can specify the file we want to load, e.g. `icudt_no_CJK.dat` by adding to .csproj:
```
<IcuFileName>icudt_no_CJK.dat</IcuFileName>
```
IcuFileName can also be a custom file, created by the developer. To create a custom ICU file, see `Custom ICU` section below.
If no IcuFileName was specified, the application's culture will be checked and corresponding file will be loaded, e.g. for `en-US` file `icudt_EFIGS.dat` and for `zh-CN` - `icudt_CJK.dat`.

## Custom ICU

Clone https://github.com/dotnet/icu. See files in `./icu-filters`, read https://unicode-org.github.io/icu/userguide/icu_data/buildtool.html#locale-slicing. Build your own filter or edit the existing file. Choose what filters to build in `eng/icu.mk`. Choose the platform:

- Building for Browser:
  For prerequisites run `.devcontainer/postCreateCommand.sh`.
  Building:
    ```
    ./build.sh /p:TargetOS=Browser /p:TargetArchitecture=wasm /p:IcuTracing=true
  Output is located in `artifacts/bin/icu-browser-wasm`.

  ```
- Building for Mobiles:

  For prerequisites run:
	> ```bash
    > export ANDROID_NDK_ROOT=$PWD/artifacts/ndk/
    > mkdir  $ANDROID_NDK_ROOT
    > wget https://dl.google.com/android/repository/android-ndk-r25b-linux.zip
    > unzip android-ndk-r25b-linux.zip -d $ANDROID_NDK_ROOT
    > rm android-ndk-r25b-linux.zip
    > mv $ANDROID_NDK_ROOT/*/* $ANDROID_NDK_ROOT
    > rmdir $ANDROID_NDK_ROOT/android-ndk-r25b
    > ```

  Building:
  > ```bash
  > ./build.sh /p:TargetOS=Android /p:TargetArchitecture=x64 /p:IcuTracing=true
  > ```

Output from both builds will be located in subdirectories of `artifacts/bin`. Copy the result `.dat` files to a suitable location and provide the full path to it in .csproj, e.g.:
```
<IcuFileName>C:\Users\wasmUser\icuSources\customIcu.dat</IcuFileName>
```

## Blazor

In Blazor we are loading the file based on the applications's culture.
To force full data to be loaded add to your .csproj:
```
<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>
```
Custom files loading for Blazor is not possible.
