The `Utf8String` and `Char8` types are now available for experimentation. They currently exist in the package __System.Utf8String.Experimental__. Because this is an experimental package, it is unsupported for use in production workloads.

To install:

```ps 
install-package System.Utf8String.Experimental -prerelease -source https://dotnetfeed.blob.core.windows.net/dotnet-core/index.json
```

This package can only be installed into a project targeting a __nightly__ build of coreclr or corefx. Anything under the _master_ column of https://github.com/dotnet/core-sdk would work, as would any coreclr + corefx built from your own dev box (as long as you're building from _master_ instead of _release/..._). Installing this onto a project targeting an official Preview build would not work, as official Preview builds come from the _release_ branch.

It's possible that installing the package might fail with an error similar to that seen below.

```txt
install-package : NU1605: Detected package downgrade: Microsoft.NETCore.Platforms from 3.0.0-preview6.19251.6 to 3.0.0-preview6.19223.2. Reference the package directly from the project to select a different version.
```

This can occur if the NuGet client attempts to install a newer version of the package than allowed by the coreclr / corefx your application is targeting. For now you can work around this error by specifying the explicit package version in the install command. Match the version passed to the NuGet client (shown below) to the version specified in the error message (shown above).

```ps
install-package System.Utf8String.Experimental -prerelease -source https://dotnetfeed.blob.core.windows.net/dotnet-core/index.json -version 3.0.0-preview6.19223.2
```

Not all of the APIs are hooked up yet, but we have some preliminary APIs that allow experimentation with the feature, including basic creation and inspection of `Utf8String` instances, wrapping a `ReadOnlySpan<byte>` or a `ReadOnlyMemory<byte>` around a `Utf8String` instance, and passing a `Utf8String` instance through `HttpClient`. Full list of APIs available at https://github.com/dotnet/corefx/blob/master/src/System.Utf8String.Experimental/ref/System.Utf8String.cs.

Certain language features also work as expected.

```cs
Utf8String s1 = new Utf8String(/* ... */);

// range indexers work
Utf8String s2 = s1[2..5];

// as does pinning
fixed (byte* pUtf8 = s1) { /* use 'pUtf8' here */ }

// and allocating a GCHandle
GCHandle handle = GCHandle.Alloc(s1, GCHandleType.Pinned);
```

For more information on the feature, see https://github.com/dotnet/corefx/issues/30503.
