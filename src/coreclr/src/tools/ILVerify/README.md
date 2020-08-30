# ILVerify

## Intention of this project:

The goal is to create a standalone, cross platform, open-source tool that is capable of verifying MSIL code based on [ECMA-335](https://www.ecma-international.org/publications/standards/Ecma-335.htm).

The main users of this tool are people working on software that emits MSIL code. These are typically compiler and profiler writers.

## How to use ILVerify

ILVerify is published as a global tool in the .NET runtime nightly feed. Install it by running:

```
dotnet new tool-manifest
dotnet tool install dotnet-ilverify --version 5.0.0-preview* --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json
```

Example of use:

```
C:\test>dotnet ilverify hello.dll -r "c:\Program Files\dotnet\shared\Microsoft.NETCore.App\2.1.12\*.dll"
All Classes and Methods in C:\test\hello.dll Verified.
```

Note that `ILVerify` requires all dependencies of assembly that is being verified to be explicitly specified on the command line.

## Other tools
Historically on .NET Framework IL generators used PEVerify to make sure that they generated correct IL. PEVerify has some major limitations (e.g. it is tied to the .NET Framework, it cannot verify mscorlib.dll, etc.), which initiated this project.

## Main properties of ILVerify:
- No coupling with CoreLib: ILVerify can point to any assembly and verify it. This also includes the .NET Framework base assemblies (especially mscorlib).
- Cross-platform, Open-Source
- It should be easy to add new verification rules
- Fast spin up/tear down.

## The codebase
The code is split into three projects:
- ILVerification is the library with the core verification logic,
- ILVerification.Tests contains the tests for ILVerification,
- ILVerify is an application that provides a command-line interface on top of ILVerification.

## Tests

To test the ILVerification library we have small methods checked in as .il files testing specific verification scenarios. These tests live under [src/ILVerification/tests/ILTests](../ILVerification/tests/ILTests). Tests are grouped into .il files based on functionalities they test. There is no strict policy here, the goal is to have a few dozen .il files instead of thousands containing each only a single method.

The test project itself is under [src/coreclr/tests/src/ilverify](../../../tests/src/ilverify)

 Method names in the .il files must follow the following naming convention:

### Methods with Valid IL:

```
[FriendlyName]_Valid
```
The method must contain 1 '`_`'.
 - The part before the `_` is a friendly name describing what the method does.
 - The word after the `_` must be 'Valid' (Case sensitive)

E.g.: ```SimpleAdd_Valid```

### Methods with Invalid IL:
```
[FriendlyName]_Invalid_[ExpectedVerifierError1].[ExpectedVerifierError2]....[ExpectedVerifierErrorN]
```

The method name must contain 2 '`_`' characters.
 1. part: a friendly name
 2. part: must be the word 'Invalid' (Case sensitive)
 3. part: the expected [VerifierErrors](../ILVerification/src/VerifierError.cs) as string separated by '.'. We assert on these errors; the test fails if ILVerify does not report these errors.

 E.g.: ```SimpleAdd_Invalid_ExpectedNumericType```

### Methods with special names:

In order to test methods with special names (e.g. '.ctor'), the specialname method is defined as usual and a separate empty method is added to the type:
```
special.[FriendlyName].[SpecialName]_[Valid | Invalid]_[ExpectedVerifierError1].[ExpectedVerifierError2]....[ExpectedVerifierErrorN]
```

The format of the special test method is equal to normal valid or invalid tests, except that the first part must contain 3 sub-parts separated by '`.`':
 1. part: the '`special`' prefix
 2. part: a friendly name
 3. part: the name of the specialname method to actually test

Additionally the method signature of the special test method must be equal to the signature of the method that shall be tested.

 E.g.: In order to test a specific invalid constructor method the specialname `.ctor` method is defined as usual, while an additional method ```'special.SimpleAdd..ctor_Invalid_StackUnexpected'``` is defined.


The methods are automatically fed into appropriate XUnit theories based on the naming convention. Methods not following this naming conventions are ignored by the test scaffolding system.

You can run the tests either in Visual Studio (in Test Explorer) or with the ```dotnet test ``` command from the command line.

## How to contribute
All ILVerify issues are labeled with [area-ILVerification](https://github.com/search?utf8=%E2%9C%93&q=label%3Aarea-ILVerification&type=). You can also look and fix TODOs in the source code.

Useful sources:
 - [PEVerify source code](https://github.com/lewischeng-ms/sscli/blob/master/clr/src/jit64/newverify.cpp)
 - [RyuJIT source code](https://github.com/dotnet/runtime/tree/master/src/coreclr/src/jit), specifically: [exception handling specific part](https://github.com/dotnet/runtime/blob/master/src/coreclr/src/jit/jiteh.cpp), [importer.cpp](https://github.com/dotnet/runtime/blob/master/src/coreclr/src/jit/importer.cpp) (look for `Compiler::ver`, `Verify`, `VerifyOrReturn`, and `VerifyOrReturnSpeculative`), [_typeinfo.h](https://github.com/dotnet/runtime/blob/master/src/coreclr/src/jit/_typeinfo.h), [typeinfo.cpp](https://github.com/dotnet/runtime/blob/master/src/coreclr/src/jit/typeinfo.cpp)
 - [ECMA-335 standard](https://www.ecma-international.org/publications/standards/Ecma-335.htm)
 - [Expert .NET 2.0 IL Assembler book](http://www.apress.com/us/book/9781590596463) by Serge Lidin
