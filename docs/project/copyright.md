Copyright
=========

The .NET project copyright is held by ".NET Foundation and Contributors".

The [.NET Foundation](http://www.dotnetfoundation.org/) is an independent organization that encourages open development and collaboration around the .NET ecosystem.

Source License
--------------

The .NET project uses multiple licenses for the various project repositories.

- The [MIT License](https://opensource.org/licenses/MIT) is used for [code](https://github.com/dotnet/runtime/).
- The [Creative Commons Attribution 4.0 International Public License (CC-BY)](https://creativecommons.org/licenses/by/4.0/) is used for [documentation](https://github.com/dotnet/docs/) and [swag](https://github.com/dotnet/swag).

Binary License
--------------

.NET distributions are licensed with a variety of licenses, dependent on the content. By default, the MIT license is used, the exact same as the [source license](https://github.com/dotnet/core/blob/master/LICENSE.TXT), with the same copyright holder. There are some cases where that isn't possible because a given component includes a proprietary Microsoft binary. This is typically only the case for Windows distributions.

The following rules are used for determining the binary license:

- .NET binary distributions (zips, nuget packages, …) are licensed as MIT (identical to the [.NET source license](https://github.com/dotnet/core/blob/master/LICENSE.TXT)).
- The license link (if there is one) should point to the repository where the file came from, for example: [dotnet/runtime](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT).
- If the contained binaries are built from multiple .NET repositories, the license should point to [dotnet/core](https://github.com/dotnet/core/blob/master/LICENSE.TXT).
- If the contents are not 100% open source, the distribution should be licensed with the [.NET Library license](https://www.microsoft.com/net/dotnet_library_license.htm).
- It is OK for licensing to be asymmetric for a single distribution type. For example, it’s possible that the .NET SDK distribution might be fully open source for Linux but include a closed-source component on Windows. In this case, the SDK would be licensed as MIT on Linux and use the .NET Library License on Windows. It is better to have more open licenses than less.
- It is OK for the source and binary licenses not to match. For example, the source might be Apache 2 but ships as an MIT binary.  The third party notices file should capture the Apache 2 license. This only works for a permissive licenses, however, we have limited the project to that class of licenses already. The value of this approach is that binary licenses are uniform.

Patents
-------

Microsoft has issued a [Patent Promise for .NET Libraries and Runtime Components](/PATENTS.TXT).
