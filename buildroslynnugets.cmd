setlocal ENABLEEXTENSIONS
pushd %~dp0
set ASYNC_ROSLYN_COMMIT=fa251ef06afcc9d1c79761a112b89c6341c0ccfa
set ASYNC_SUFFIX=async-10
set ASYNC_ROSLYN_BRANCH=demos/async2-experiment1

cd ..
if not exist async-roslyn-repo git clone -b %ASYNC_ROSLYN_BRANCH% -o async_roslyn_remote https://github.com/dotnet/roslyn.git async-roslyn-repo

pushd async-roslyn-repo

git fetch async_roslyn_remote %ASYNC_ROSLYN_BRANCH%
rem when updating this, make sure to update the ASYNC_SUFFIX above and the versions.props file
git checkout %ASYNC_ROSLYN_COMMIT%

call restore.cmd
call build.cmd -c release

call dotnet pack src\NuGet\Microsoft.Net.Compilers.Toolset\AnyCpu\Microsoft.Net.Compilers.Toolset.Package.csproj --version-suffix %ASYNC_SUFFIX%
call dotnet pack src\Workspaces\CSharp\Portable\Microsoft.CodeAnalysis.CSharp.Workspaces.csproj --version-suffix %ASYNC_SUFFIX%
call dotnet pack src\Compilers\CSharp\Portable\Microsoft.CodeAnalysis.CSharp.csproj --version-suffix %ASYNC_SUFFIX%
call dotnet pack src\Compilers\Core\Portable\Microsoft.CodeAnalysis.csproj --version-suffix %ASYNC_SUFFIX%
call dotnet pack src\Workspaces\Core\Portable\Microsoft.CodeAnalysis.Workspaces.csproj --version-suffix %ASYNC_SUFFIX%

pushd %~dp0

md roslynpackages

copy ..\async-roslyn-repo\artifacts\packages\Release\Shipping\Microsoft.Net.Compilers.Toolset.4.13.0-%ASYNC_SUFFIX%.nupkg roslynpackages
copy ..\async-roslyn-repo\artifacts\packages\Release\Shipping\Microsoft.CodeAnalysis.Workspaces.Common.4.13.0-%ASYNC_SUFFIX%.nupkg roslynpackages
copy ..\async-roslyn-repo\artifacts\packages\Release\Shipping\Microsoft.CodeAnalysis.CSharp.Workspaces.4.13.0-%ASYNC_SUFFIX%.nupkg roslynpackages
copy ..\async-roslyn-repo\artifacts\packages\Release\Shipping\Microsoft.CodeAnalysis.CSharp.4.13.0-%ASYNC_SUFFIX%.nupkg roslynpackages
copy ..\async-roslyn-repo\artifacts\packages\Release\Shipping\Microsoft.CodeAnalysis.Common.4.13.0-%ASYNC_SUFFIX%.nupkg roslynpackages

