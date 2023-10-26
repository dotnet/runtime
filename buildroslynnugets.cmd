setlocal ENABLEEXTENSIONS
pushd %~dp0

set ASYNC_SUFFIX=async-3

cd ..
pushd dotnet-roslyn

git fetch AzDo dev/vsadov/a2
rem when updating this, make sure to update the ASYNC_SUFFIX above and the versions.props file
git checkout 0d06d1f93092dc3112fcefdf8957e11ed3ea4e31

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

