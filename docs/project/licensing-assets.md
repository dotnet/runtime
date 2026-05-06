# .NET Asset Licensing Model

> The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

The [MIT](https://github.com/dotnet/core/blob/main/LICENSE.TXT) license and others like it have a provision that distributions of software should include the license. This is the model we should be following.

Each .NET binary distribution must carry:

- Its license
- Third party notice

Binary distributions include: compressed archives, runtime packs, installers, container images, packages, and anything else where we deliver "substantial portions of the Software".

Note: Distributions must contain and display the correct license. For example, the [Microsoft.NETCore.App.Runtime.win-x64](https://www.nuget.org/packages/Microsoft.NETCore.App.Runtime.win-x64/) runtime pack must contain the correct license and the correct license metadata, displayed in the NuGet gallery. Installers have a similar model with license UI.

## Product distributions

For product distributions, the following licenses should be used.

- On Linux and macOS, the license should be the [.NET MIT license](https://github.com/dotnet/core/blob/main/LICENSE.TXT).
- On Windows, the license should be the [.NET Library License](https://dotnet.microsoft.com/dotnet_library_license.htm) per [Windows license information](https://github.com/dotnet/core/blob/main/license-information-windows.md).

Product distributions include [downloadable assets](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) and NuGet runtime packs.

## Package distributions

Library packages, like [System.Text.Json](https://www.nuget.org/packages/System.Text.Json), should use the .NET MIT license.

"Packages" do not include runtime packs.
