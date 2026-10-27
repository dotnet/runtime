# macOS native tool metadata

NativeAOT publishing projects set `HostBundleIdentifier` and `HostBundleName` to
embed an Info.plist in macOS executables. Identifiers use
`com.microsoft.dotnet.<executable-name>` in lowercase; `CFBundleName` is the plain
executable name. Native CMake hosts use the same convention and the same
`eng/native/Info.plist.in` template.

`eng/toolAot.targets` uses `Microsoft.DotNet.Build.Tasks.Templating` to render
the plist. It writes the linking input only when its contents change, so an
unchanged build does not relink. Optional bundle version keys are omitted because
assembly versions do not follow Apple's bundle version format.

This metadata does not create an app bundle. The linker-generated ad hoc signature
does not automatically use the embedded identifier. `AdHocSignHost` applies the
identifier after linking and symbol stripping, before publishing copies the
executable. Existing entitlements, requirements, signing flags, hardened runtime
version, and launch and library constraints are preserved while explicitly setting
the identifier, without adding JIT entitlements. Official signing must preserve the
identifier and security settings or explicitly supply the same values.
