---
title: "Breaking change: NativeAOT uses `lib` prefix by default on Unix for native library outputs"
description: "Learn about the breaking change in .NET 10 where NativeAOT uses the `lib` prefix by default when producing shared or static native library outputs on non-Windows platforms."
ms.date: 03/16/2026
ai-usage: ai-assisted
---
# NativeAOT uses `lib` prefix by default on Unix for native library outputs

Starting in .NET 10, NativeAOT automatically prepends the `lib` prefix to the output file name when publishing a native shared or static library on non-Windows platforms (for example, `libfoo.so`, `libfoo.dylib`, or `libfoo.a`). This matches the standard Unix naming convention for shared and static libraries.

## Version introduced

.NET 10 Preview 3

## Previous behavior

Previously, NativeAOT did not add the `lib` prefix to native library outputs on Unix by default. For a project named `foo` with `<NativeLib>Shared</NativeLib>`, the output file was `foo.so` (Linux) or `foo.dylib` (macOS).

## New behavior

Starting in .NET 10, NativeAOT adds the `lib` prefix to native library output names on non-Windows platforms by default. For a project named `foo` with `<NativeLib>Shared</NativeLib>`, the output file is now `libfoo.so` (Linux) or `libfoo.dylib` (macOS).

The same prefix is also applied to static library outputs (`.a` files).

## Type of breaking change

This change is a [behavioral change](../../categories.md#behavioral-change).

## Reason for change

The `lib` prefix is the standard naming convention for shared and static libraries on Unix. Adopting it by default makes NativeAOT library outputs consistent with native libraries produced by other toolchains and avoids issues for consumers (such as the Android linker) that expect the `lib` prefix.

## Recommended action

If you relied on the previous behavior and do not want the `lib` prefix, opt out by setting the `UseNativeLibPrefix` MSBuild property to `false` in your project file:

```xml
<PropertyGroup>
  <UseNativeLibPrefix>false</UseNativeLibPrefix>
</PropertyGroup>
```

If you were previously setting the private `_UseNativeLibPrefix` property, you can remove that setting because the prefix is now applied by default.

## Affected APIs

None. This is an MSBuild property change only.
