## About

Provides a library for verifying .NET intermediate language (IL) against the ECMA-335 specification. For a command-line interface, use the [dotnet-ilverify](https://www.nuget.org/packages/dotnet-ilverify/) tool.

## Key Features

* Reports invalid or non-verifiable IL in method bodies, such as stack underflow, type mismatches and invalid branch targets.
* Reports invalid type metadata, such as missing interface method implementations.
* Reads assemblies through `System.Reflection.Metadata` without executing them.

## How to Use

Implement `IResolver` to supply a `PEReader` for each assembly the verifier needs. Set the system module (the assembly that defines core types such as `System.Object`), then enumerate the results of `Verify`. This example verifies every method body of an assembly, resolving references from that assembly's directory and the directory of the current .NET runtime:

```cs
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ILVerify;

string path = args[0];
var resolver = new DirectoryResolver(Path.GetDirectoryName(Path.GetFullPath(path))!, Path.GetDirectoryName(typeof(object).Assembly.Location)!);
var verifier = new Verifier(resolver, new VerifierOptions { IncludeMetadataTokensInErrorMessages = true });
verifier.SetSystemModuleName(new AssemblyNameInfo("System.Private.CoreLib"));

PEReader reader = resolver.ResolveAssembly(new AssemblyNameInfo(Path.GetFileNameWithoutExtension(path)))!;
foreach (VerificationResult result in verifier.Verify(reader))
{
    Console.WriteLine($"{result.Code}: {result.Message}");
}

// Returns the same PEReader each time the same file is requested.
class DirectoryResolver(params string[] directories) : IResolver
{
    private readonly Dictionary<string, PEReader> _readers = new(StringComparer.OrdinalIgnoreCase);

    public PEReader? ResolveAssembly(AssemblyNameInfo assemblyName) => Open(assemblyName.Name + ".dll");

    public PEReader? ResolveModule(AssemblyNameInfo referencingAssembly, string fileName) => Open(fileName);

    private PEReader? Open(string fileName)
    {
        if (_readers.TryGetValue(fileName, out PEReader? reader))
        {
            return reader;
        }

        foreach (string directory in directories)
        {
            string candidate = Path.Combine(directory, fileName);
            if (File.Exists(candidate))
            {
                reader = new PEReader(File.OpenRead(candidate));
                _readers.Add(fileName, reader);
                return reader;
            }
        }

        return null;
    }
}
```

## Main Types

The main types provided by this library are:

* `ILVerify.Verifier`
* `ILVerify.IResolver`
* `ILVerify.VerifierOptions`
* `ILVerify.VerificationResult`
* `ILVerify.VerifierError`

## Additional Documentation

* [ILVerify source and documentation](https://github.com/dotnet/runtime/tree/main/src/coreclr/tools/ILVerify)
* [ECMA-335 - Common Language Infrastructure (CLI)](https://www.ecma-international.org/publications-and-standards/standards/ecma-335)

## Feedback & Contributing

Microsoft.ILVerification is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
