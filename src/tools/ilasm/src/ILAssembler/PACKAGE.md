## About

<!-- A description of the package and where one can find more documentation -->

Microsoft.DotNet.ILAssembler compiles Common Intermediate Language (CIL) source text into .NET assemblies. It exposes the managed assembler as a library for tools that generate or process IL, without requiring a separate `ilasm` process.

## Key Features

<!-- The key features of this package -->

* Assemble IL source text into DLLs or executables.
* Report compilation diagnostics with source locations.
* Generate Portable PDB debugging information.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

The following example compiles an IL method into a DLL and writes the resulting image to disk.

```csharp
using System;
using System.IO;
using System.Reflection.Metadata;
using Microsoft.DotNet.ILAssembler;

string source = """
    .assembly Example { }
    .module Example.dll

    .method public static int32 Add(int32 left, int32 right) cil managed
    {
        .maxstack 2
        ldarg.0
        ldarg.1
        add
        ret
    }
    """;

string outputPath = "Example.dll";
var compiler = new DocumentCompiler();
var (diagnostics, result) = compiler.Compile(
    new SourceText(source, "Example.il"),
    path => new SourceText(File.ReadAllText(path), path),
    path => File.ReadAllBytes(path),
    new Options { Dll = true, OutputFileName = outputPath });

foreach (Diagnostic diagnostic in diagnostics)
{
    Console.Error.WriteLine(diagnostic.Message);
}

if (result is null)
{
    throw new InvalidOperationException("Assembly failed.");
}

var image = new BlobBuilder();
result.Serialize(image);
using FileStream output = File.Create(outputPath);
image.WriteContentTo(output);
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.DotNet.ILAssembler.DocumentCompiler`
* `Microsoft.DotNet.ILAssembler.SourceText`
* `Microsoft.DotNet.ILAssembler.Options`
* `Microsoft.DotNet.ILAssembler.CompilationResult`
* `Microsoft.DotNet.ILAssembler.Diagnostic`

## Additional Documentation

<!-- Links to further documentation -->

* [ECMA-335 - Common Language Infrastructure (CLI)](https://www.ecma-international.org/publications-and-standards/standards/ecma-335)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.DotNet.ILAssembler is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
