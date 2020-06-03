# ILVerify

### This directory contains the implementation of ILVerify

## Intention of this project:
The goal is to create a standalone, cross platform, open-source tool that is capable of verifying MSIL code based on [ECMA-335](https://www.ecma-international.org/publications/standards/Ecma-335.htm).

The main users of this tool are people working on software that emits MSIL code. These are typically compiler and profiler writers.

## Other tools
Historically on Full Framework IL generators used PEVerify to make sure that they generated correct IL. PEVerify has some major limitations (e.g. it is tied to the Full Framework, it cannot verify mscorlib.dll, etc.), which initiated this project.

## Main properties of ILVerify:
- No coupling with CoreLib: ILVerify can point to any assembly and verify it. This also includes the full framework base assemblies (especially mscorlib).
- Cross-platform, Open-Source
- It should be easy to add new verification rules
- Fast spin up/tear down.

## The codebase
The project targets netcoreapp2.1 and uses the new .csproj based project format. If you want to open and compile it with Visual Studio then you need a version, which supports .NET Core 2.1 tooling. This is supported in Visual Studio 2017 Version 15.8 or later. The other option is to use command (with .NET Core 2.1 tooling).
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
All ILVerify issues are labeled with [area-ILVerification](https://github.com/search?utf8=%E2%9C%93&q=label%3Aarea-ILVerification&type=).

ILVerify basically runs through the IL commands in an assembly and does all the verification steps that are specified in ECMA-335.

Currently every IL command falls into one of these categories:

 - Not implemented: the implementation is completely missing. The easiest way is to pick one of them (look for NotImplentedException in the code) and implement it. First you should 100% understand the spec. (see [ECMA-335](https://www.ecma-international.org/publications/standards/Ecma-335.htm)), then try to port an existing implementation (sources below).
 - Partially implemented: These are typically methods with TODOs in it. As the first phase we want to make sure that for every command the stack is correctly maintained, therefore for some commands we either have no verification or we have only a not complete verification. You can also pick one of these and finish it.
 - Implemented: find and fix bugs ;) .

Another option to contribute is to write tests (see Tests section).

Useful sources:
 - [PEVerify source code](https://github.com/lewischeng-ms/sscli/blob/master/clr/src/jit64/newverify.cpp)
 - [RyuJIT source code](https://github.com/dotnet/runtime/tree/master/src/coreclr/src/jit), specifically: [exception handling specific part](https://github.com/dotnet/runtime/blob/master/src/coreclr/src/jit/jiteh.cpp), [importer.cpp](https://github.com/dotnet/runtime/blob/master/src/coreclr/src/jit/importer.cpp) (look for `Compiler::ver`, `Verify`, `VerifyOrReturn`, and `VerifyOrReturnSpeculative`), [_typeinfo.h](https://github.com/dotnet/runtime/blob/master/src/coreclr/src/jit/_typeinfo.h), [typeinfo.cpp](https://github.com/dotnet/runtime/blob/master/src/coreclr/src/jit/typeinfo.cpp)
 - [ECMA-335 standard](https://www.ecma-international.org/publications/standards/Ecma-335.htm)
 - [Expert .NET 2.0 IL Assembler book](http://www.apress.com/us/book/9781590596463) by Serge Lidin
