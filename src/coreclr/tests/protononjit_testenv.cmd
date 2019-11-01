@REM -------------------------------------------------------------------------
@REM 
@REM  This script provides test environment settings for prototype/cross-targeting JITs.
@REM
@REM -------------------------------------------------------------------------

set COMPLUS_AltJit=*
set COMPLUS_AltJitNgen=*
set COMPLUS_AltJitName=protononjit.dll
set COMPLUS_NoGuiOnAssert=1
set COMPLUS_AltJitAssertOnNYI=1

@REM -------------------------------------------------------------------------
@REM A JitFuncInfoLogFile is a per-function record of which functions were
@REM compiled, and what asserts and NYI were hit.
@REM
@REM If you wish to collect FuncInfo, choose one of the following collection
@REM methods to use, by uncommenting the appropriate option.

@REM Option 1: collect a single FuncInfoLogFile for all tests.
@REM TO DO: set a single, fully-qualified pathname here.
@REM ==== Uncomment below
@REM set COMPLUS_JitFuncInfoLogFile=%TEMP%\JitFuncInfoLogFile.txt
@REM ==== Uncomment above

@REM Option #2: collect one FuncInfoLogFile per test, and put it in
@REM the same directory as the test. Note that each tests lives in
@REM its own directory, and the current directory is set to the unique
@REM test directory before this script is invoked.
@REM ==== Uncomment below
@REM set __TestDir=%CD%
@REM if %__TestDir:~-1%==\ set __TestDir=%__TestDir:~0,-1%
@REM set COMPLUS_JitFuncInfoLogFile=%__TestDir%\FuncInfo.txt
@REM ==== Uncomment above

@REM Option #3: collect one FuncInfoLogFile per test, and put all of
@REM them in a separate directory tree rooted at %__FuncInfoRootDir%.
@REM If that is set globally already, then use it. Otherwise, use a
@REM default set here. The directory tree will mirror the test binary tree.
@REM Note that the current directory is set to the unique test directory
@REM before this script is invoked.
@REM ==== Uncomment below
@REM if not defined __FuncInfoRootDir set __FuncInfoRootDir=c:\FuncInfos
@REM set __TestDir=%CD%
@REM if %__TestDir:~-1%==\ set __TestDir=%__TestDir:~0,-1%
@REM if %__TestDir:~1,1%==: set __TestDir=%__TestDir:~3%
@REM set __FuncInfoDir=%__FuncInfoRootDir%\%__TestDir%
@REM if not exist %__FuncInfoDir% mkdir %__FuncInfoDir%
@REM set COMPLUS_JitFuncInfoLogFile=%__FuncInfoDir%\FuncInfo.txt
@REM ==== Uncomment above

@REM -------------------------------------------------------------------------
