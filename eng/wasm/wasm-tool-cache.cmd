@if not defined _echo @echo off
rem
rem Resolves a tool in the shared wasm tool cache described in eng/wasm/WasmToolCache.props.
rem Kept in sync with that file: the cache root, the main-checkout anchoring, and the
rem <tool>/<version>-<rid> layout are duplicated here because the native build scripts cannot
rem evaluate MSBuild properties.
rem
rem Usage: call wasm-tool-cache.cmd <tool name> <version file> <repo root>
rem Sets WASM_TOOL_CACHE_RESULT to the resolved directory, or clears it if not provisioned.

setlocal enabledelayedexpansion

set "__Tool=%~1"
set "__VersionFile=%~2"
set "__RepoRoot=%~3"

rem Anchor at the main checkout when this is a git worktree so all worktrees share one cache
rem (its <root>\.git is a FILE 'gitdir: <main>/.git/worktrees/<name>'; a normal clone's is a
rem DIRECTORY). Only an ABSOLUTE gitdir is trusted; a relative gitdir or any parse difficulty
rem falls back to the repo root, matching eng/wasm/WasmToolCache.props.
set "__Anchor=%__RepoRoot%"
if not exist "%__RepoRoot%\.git" goto :haveAnchor
if exist "%__RepoRoot%\.git\" goto :haveAnchor
set "__GitLine="
set /p __GitLine=<"%__RepoRoot%\.git"
if not defined __GitLine goto :haveAnchor
set "__GitDir=!__GitLine:*gitdir: =!"
set "__GitDir=!__GitDir:/=\!"
set "__Split=!__GitDir:\.git\worktrees\=|!"
if "!__Split!" == "!__GitDir!" goto :haveAnchor
for /f "tokens=1 delims=|" %%q in ("!__Split!") do set "__Cand=%%q"
rem Trust only a rooted path, matching MSBuild's IsPathRooted: a drive-letter colon in
rem position 2 (C:\...) or a UNC prefix (\\server\share\...).
if "!__Cand:~1,1!" == ":" set "__Anchor=!__Cand!"
if "!__Cand:~0,2!" == "\\" set "__Anchor=!__Cand!"
:haveAnchor

set "__Root=%DOTNET_WASM_TOOL_CACHE_DIR%"
if "%__Root%" == "" set "__Root=%__Anchor%\.dotnet\wasm-tools"

rem Normalize to the same names MSBuild's $(BuildArchitecture) uses (x86 / x64 / arm64).
rem A 32-bit process on 64-bit Windows reports x86 in PROCESSOR_ARCHITECTURE and the real
rem architecture in PROCESSOR_ARCHITEW6432, so consult that first.
set "__Arch=x64"
if /i "%PROCESSOR_ARCHITECTURE%" == "ARM64" set "__Arch=arm64"
if /i "%PROCESSOR_ARCHITEW6432%" == "ARM64" set "__Arch=arm64"
if /i "%PROCESSOR_ARCHITECTURE%" == "x86" if not defined PROCESSOR_ARCHITEW6432 set "__Arch=x86"
set "__Rid=windows-%__Arch%"

set "__Resolved="
if not exist "%__VersionFile%" goto :done

set /p __Version=<"%__VersionFile%"
for /f "tokens=* delims= " %%v in ("%__Version%") do set "__Version=%%v"
if "%__Version%" == "" goto :done

set "__Candidate=%__Root%\%__Tool%\%__Version%-%__Rid%"
if exist "%__Candidate%.complete" set "__Resolved=%__Candidate%"

:done
endlocal & set "WASM_TOOL_CACHE_RESULT=%__Resolved%"
exit /B 0
