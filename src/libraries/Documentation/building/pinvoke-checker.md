# PInvoke Analyzer

During the build of any product library in CoreFX we use a Roslyn code analyzer to look for disallowed native calls (PInvokes). When there is a violation, it will fail the build. To fix the build, either find an alternative to the PInvoke or baseline the failure temporarily. To baseline it, add the function name in the format `module!entrypoint` to a file named PInvokeAnalyzerExceptionList.analyzerdata in the same folder as the project. [Here](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.Process/src/PInvokeAnalyzerExceptionList.analyzerdata) is an example. 

If you baseline a violation, please open an issue to fix it because the library likely cannot ship in this situation. It is better to not introduce the violation. We want to clean out any baselines. There are situations where a violation may be acceptable. One situation is where we are shipping the native implementation of the API. An example of this situation is `sni.dll` which is used by SqlClient.

Each project is analyzed against one of two possible lists we maintain.

## Legal UWP API's

### Applies to
This applies to product libraries that are being built for use in a modern Windows app (aka UWP app, or app running on UAP). When building the `uapaot` or `uap` configurations we will apply this check. If the library does not have a `uap` or `uapaot` configuration explicitly listed in `Configuration.props` in the project folder, when targeting `uap` or `uapaot` we will build the `netstandard` configuration, and apply this check.

We do not currently apply this check to test binaries. Although when testing UWP libraries the tests must run within a test app, they do not need to pass through the store validation process. It is still possible they may call an API that does not work correctly within an app's security boundary and that call would have to be avoided.

### Motivation
Not all PInvokes are legal within a UWP app. An allow-list is enforced when the Windows store ingests an app, and also in a build step in the Visual Studio build process for apps. If we produce a library for UWP use, any PInvokes it performs must be to API's that are on the allow-list or the app using the library will fail validation.

### Implementation
To enforce this the analyzer consults the list [here](https://github.com/dotnet/buildtools/blob/master/src/Microsoft.DotNet.CodeAnalysis/PackageFiles/PinvokeAnalyzer_Win32UWPApis.txt). 

The analyzer is enabled by default in the configurations below by the setting of the MSBuild property `UWPCompatible`. We aim to make all our `netstandard` compliant libraries work within a UWP app, but in rare cases where a library cannot, the check can be disabled with `<UWPCompatible>false</UWPCompatible>` in the project file.

There is also a more fine grained property `<EnablePInvokeUWPAnalyzer>false</EnablePInvokeUWPAnalyzer>` for temporary use.

## Legal OneCore API's

### Applies to
This applies to all other product libraries in all other configurations targeted at Windows.

We do not currently apply this check to test binaries as they do not need to run on Windows Nano.

### Motivation
.NET Core supports execution on Windows Nano, which has a reduced API surface area known as OneCore. To run on Windows Nano we cannot invoke any platform API that is not available on OneCore.

### Implementation
To enforce this the analyzer consults the list [here](https://github.com/dotnet/buildtools/blob/master/src/Microsoft.DotNet.CodeAnalysis/PackageFiles/PinvokeAnalyzer_Win32Apis.txt). 

The analyzer is enabled by default when building for Windows, not a test, and not building for UWP. We aim to make all such configurations OneCore compilant, but in the rare cases where a library cannot be, the check can be disabled with `<EnablePInvokeAnalyzer>false<EnablePInvokeAnalyzer>` in the project file.
