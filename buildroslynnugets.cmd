setlocal ENABLEEXTENSIONS
pushd %~dp0

set ASYNC_SUFFIX=async-2

cd ..
pushd dotnet-roslyn

git fetch AzDo dev/vsadov/a2
rem when updating this, make sure to update the ASYNC_SUFFIX above and the versions.props file
git checkout 1c35e4f42732cfd58bb2a56bbbd85d8200497fa2

call restore.cmd
call build.cmd -c release

call dotnet pack src\NuGet\Microsoft.Net.Compilers.Toolset\AnyCpu\Microsoft.Net.Compilers.Toolset.Package.csproj --version-suffix %ASYNC_SUFFIX%
call dotnet pack src\Workspaces\CSharp\Portable\Microsoft.CodeAnalysis.CSharp.Workspaces.csproj --version-suffix %ASYNC_SUFFIX%
call dotnet pack src\Compilers\CSharp\Portable\Microsoft.CodeAnalysis.CSharp.csproj --version-suffix %ASYNC_SUFFIX%
call dotnet pack src\Compilers\Core\Portable\Microsoft.CodeAnalysis.csproj --version-suffix %ASYNC_SUFFIX%
call dotnet pack src\Workspaces\Core\Portable\Microsoft.CodeAnalysis.Workspaces.csproj --version-suffix %ASYNC_SUFFIX%

pushd %~dp0

md roslynpackages

copy ..\dotnet-roslyn\artifacts\packages\Release\Shipping\Microsoft.Net.Compilers.Toolset.4.9.0-%ASYNC_SUFFIX%.nupkg roslynpackages
copy ..\dotnet-roslyn\artifacts\packages\Release\Shipping\Microsoft.CodeAnalysis.Workspaces.Common.4.9.0-%ASYNC_SUFFIX%.nupkg roslynpackages
copy ..\dotnet-roslyn\artifacts\packages\Release\Shipping\Microsoft.CodeAnalysis.CSharp.Workspaces.4.9.0-%ASYNC_SUFFIX%.nupkg roslynpackages
copy ..\dotnet-roslyn\artifacts\packages\Release\Shipping\Microsoft.CodeAnalysis.CSharp.4.9.0-%ASYNC_SUFFIX%.nupkg roslynpackages
copy ..\dotnet-roslyn\artifacts\packages\Release\Shipping\Microsoft.CodeAnalysis.Common.4.9.0-%ASYNC_SUFFIX%.nupkg roslynpackages

