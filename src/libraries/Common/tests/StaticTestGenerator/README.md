# Overview

This is a .NET Core utility that reflects over a corefx xunit test assembly and generates
a simple console app that invokes tests with minimal ceremony and without any reflection.
Each test invocation is emitted as a call into the public test method, with one invocation
generated per theory input.

# Usage

## Prereqs

- Build corefx from root to ensure that the testhost directory of assemblies is published, e.g. `cd d:\repos\corefx & build.cmd`.
- Build the test assembly desired, e.g `cd src\System.Runtime\tests & dotnet msbuild`.

## Running the StaticTestGenerator utility

From within the utility directory, run the utility with the arguments:
- The path to the output directory into which the resulting .cs and .csproj files are written.
- The path to a directory containing all of the corefx test host assemblies.
- The path to the test assembly to be analyzed.
- Any additional xunit options to be provided.  If none are provided, it defaults to the same inputs that are passed when testing inner loop on Windows.

For example:
```
dotnet run d:\output "d:\repos\corefx\artifacts\bin\testhost\netcoreapp-Windows_NT-Debug-x64\shared\Microsoft.NETCore.App\9.9.9" "d:\repos\corefx\artifacts\bin\System.Runtime.Tests\netcoreapp-Windows_NT-Debug\System.Runtime.Tests.dll"
```
This will run the tool and result in output written to the console like:
```
3/27/2019 10:55:37 PM | Test assembly path    : d:\repos\corefx\artifacts\bin\System.Runtime.Tests\netcoreapp-Windows_NT-Debug\System.Runtime.Tests.dll
3/27/2019 10:55:37 PM | Helper assemblies path: d:\repos\corefx\artifacts\bin\testhost\netcoreapp-Windows_NT-Debug-x64\shared\Microsoft.NETCore.App\9.9.9\
3/27/2019 10:55:37 PM | Output path           : d:\output\System.Runtime.Tests\
3/27/2019 10:55:37 PM | Xunit arguments       : d:\repos\corefx\artifacts\bin\System.Runtime.Tests\netcoreapp-Windows_NT-Debug\System.Runtime.Tests.dll -notrait category=nonnetcoreapptests -notrait category=nonwindowstests -notrait category=IgnoreForCI -notrait category=failing -notrait category=OuterLoop
3/27/2019 10:55:37 PM |
3/27/2019 10:55:37 PM | Loaded System.Runtime.Tests from d:\repos\corefx\artifacts\bin\System.Runtime.Tests\netcoreapp-Windows_NT-Debug\System.Runtime.Tests.dll
3/27/2019 10:55:37 PM | Found 5322 test methods.
3/27/2019 10:55:38 PM | Found 3469 InlineDatas / 949 MethodDatas across 5322 test methods.
3/27/2019 10:55:38 PM |
3/27/2019 10:55:38 PM | Unsupported InvokeRefReturnNetcoreTests.TestRefReturnPropertyGetValue. Generic method requires reflection invoke.
3/27/2019 10:55:38 PM | Unsupported InvokeRefReturnNetcoreTests.TestRefReturnMethodInvoke. Generic method requires reflection invoke.
3/27/2019 10:55:38 PM | Unsupported InvokeRefReturnNetcoreTests.TestNullRefReturnInvoke. Generic method requires reflection invoke.
3/27/2019 10:55:38 PM | Unsupported ArrayTests.Sort_Array_Array_Generic. Generic method requires reflection invoke.
3/27/2019 10:55:38 PM | Unsupported ArrayTests.Fill_Generic. Generic method requires reflection invoke.
3/27/2019 10:55:38 PM | Unsupported ArrayTests.IndexOf_SZArray. Generic method requires reflection invoke.
3/27/2019 10:55:38 PM | Unsupported ArrayTests.LastIndexOf_SZArray. Generic method requires reflection invoke.
3/27/2019 10:55:38 PM | Unsupported ArrayTests.Sort_Array_Generic. Generic method requires reflection invoke.
3/27/2019 10:55:38 PM | Unsupported ArrayTests.Sort_NotComparable_ThrowsInvalidOperationException. Generic method requires reflection invoke.
3/27/2019 10:55:38 PM | Unsupported ArrayTests.BinarySearch_SZArray. Generic method requires reflection invoke.
3/27/2019 10:55:38 PM | Unsupported ArrayTests.BinarySearch_TypesNotIComparable_ThrowsInvalidOperationException. Generic method requires reflection invoke.
3/27/2019 10:55:38 PM | Unsupported EnumTests.Parse. Generic method requires reflection invoke.
3/27/2019 10:55:38 PM | Unsupported EnumTests.Parse_NetCoreApp11. Generic method requires reflection invoke.
3/27/2019 10:55:38 PM |
3/27/2019 10:55:38 PM | Num unsupported: 13
3/27/2019 10:55:38 PM | Num calls written: 8289
3/27/2019 10:55:38 PM |
3/27/2019 10:55:40 PM | Wrote d:\output\System.Runtime.Tests\Program.cs
3/27/2019 10:55:40 PM | Wrote d:\output\System.Runtime.Tests\System.Runtime.Tests-runner.csproj
```
(The "Unsupported" lines are calling out tests that could not be emitted, along with the reason why.)

This will create a `System.Runtime.Tests` directory under `d:\output`, and in that directory it'll place two files,
a Program.cs containing the generated code and a System.Runtime.Tests-runner.csproj containing a minimal csproj for the solution.
The .csproj contains the necessary assembly references to build the tests.

## Tests Runner Build

Having run the utility to generate the test runner, change into the specified output directory from the above steps and do:
```
dotnet build
```
This will build the test binary.  If there are any compilation failures, there are bugs in the utility that need to be fixed.

## Tests Execution

With the test runner built, you can now execute tests. However, the tests themselves may try to load additional
assemblies, and any such assemblies should be in the same directory as the test binary in order for them to be
loadable.  One approach is to simply copy everything from the shared testhost into a folder, copy the test runner
binary into that same folder, and copy any additional dependencies (e.g. RemoteExecutorConsoleApp.exe) into that
same directory.  For example:
```
d:\CoreClrTest>corerun System.Runtime.Tests-runner.dll
System.Tests.ExceptionTests.ThrowStatementDoesNotResetExceptionStackLineSameMethod [FAIL]
Xunit.Sdk.TrueException: Assert.True() Failure
Expected: True
Actual:   False
   at Xunit.Assert.True(Nullable`1 condition, String userMessage) in C:\projects\xunit\src\xunit.assert\Asserts\BooleanAsserts.cs:line 95
   at System.Tests.ExceptionTests.VerifyCallStack(ValueTuple`3 expectedStackFrame, String reportedCallStack, Int32 skipFrames)
   at System.Tests.ExceptionTests.ThrowStatementDoesNotResetExceptionStackLineSameMethod()
   at Test.<>c.<Main>b__0_3213() in d:\tmpoutput\System.Runtime.Tests\Program.cs:line 11552
   at Test.Execute(String name, Action action, Int32& succeeded, Int32& failed) in d:\tmpoutput\System.Runtime.Tests\Program.cs:line 25179
System.Tests.ExceptionTests.ThrowStatementDoesNotResetExceptionStackLineOtherMethod [FAIL]
Xunit.Sdk.TrueException: Assert.True() Failure
Expected: True
Actual:   False
   at Xunit.Assert.True(Nullable`1 condition, String userMessage) in C:\projects\xunit\src\xunit.assert\Asserts\BooleanAsserts.cs:line 95
   at System.Tests.ExceptionTests.VerifyCallStack(ValueTuple`3 expectedStackFrame, String reportedCallStack, Int32 skipFrames)
   at System.Tests.ExceptionTests.ThrowStatementDoesNotResetExceptionStackLineOtherMethod()
   at Test.<>c.<Main>b__0_3214() in d:\tmpoutput\System.Runtime.Tests\Program.cs:line 11556
   at Test.Execute(String name, Action action, Int32& succeeded, Int32& failed) in d:\tmpoutput\System.Runtime.Tests\Program.cs:line 25179
System.Tests.ArgIteratorTests.ArgIterator_Throws_PlatformNotSupportedException [FAIL]
Xunit.Sdk.ThrowsException: Assert.Throws() Failure
Expected: typeof(System.PlatformNotSupportedException)
Actual:   typeof(System.ArgumentException): Handle is not initialized.
   at System.ArgIterator..ctor(IntPtr arglist)
   at System.ArgIterator..ctor(RuntimeArgumentHandle arglist) in F:\vsagent\80\s\src\System.Private.CoreLib\src\System\ArgIterator.cs:line 39
   at System.Tests.ArgIteratorTests.<>c.<ArgIterator_Throws_PlatformNotSupportedException>b__6_0()
   at Xunit.Assert.RecordException(Action testCode) in C:\projects\xunit\src\xunit.assert\Asserts\Record.cs:line 28

Total : 31509
Passed: 31506 (99.99%)
Failed: 3 (0.01%)
```
