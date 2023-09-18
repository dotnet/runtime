## About

<!-- A description of the package and where one can find more documentation -->

This package provides a low-level .NET (ECMA-335) metadata reader and writer. It's geared for performance and is the ideal choice for building higher-level libraries that intend to provide their own object model, such as compilers. The metadata format is defined by the [ECMA-335 - Common Language Infrastructure (CLI)](http://www.ecma-international.org/publications/standards/Ecma-335.htm) specification and [its amendments](https://github.com/dotnet/runtime/blob/main/docs/design/specs/Ecma-335-Augments.md).

The `System.Reflection.Metadata` library is included in the .NET Runtime shared framework. The package can be installed when you need to use it in other target frameworks.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

The following example shows how to read assembly information using PEReader and MetadataReader.

```cs
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

class Program
{
    static void Main()
    {
        // Open the Portable Executable (PE) file
        using var fs = new FileStream("Example.dll", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var peReader = new PEReader(fs);

        // Display PE header information
        PEHeader header = peReader.PEHeaders.PEHeader;
        Console.WriteLine($"Image base:     0x{header.ImageBase:X}");
        Console.WriteLine($"File alignment: 0x{header.FileAlignment:X}");
        Console.WriteLine($"Subsystem:      {header.Subsystem}");

        // Display .NET metadata information
        if (!peReader.HasMetadata)
        {
            Console.WriteLine("Image does not contain .NET metadata");
            return;
        }

        MetadataReader mr = peReader.GetMetadataReader();
        AssemblyDefinition ad = mr.GetAssemblyDefinition();
        Console.WriteLine($"Assembly name:  {ad.GetAssemblyName().ToString()}");
        Console.WriteLine();
        Console.WriteLine("Assembly attributes:");

        foreach (CustomAttributeHandle attrHandle in ad.GetCustomAttributes())
        {
            CustomAttribute attr = mr.GetCustomAttribute(attrHandle);

            // Display the attribute type full name
            if (attr.Constructor.Kind == HandleKind.MethodDefinition)
            {
                MethodDefinition mdef = mr.GetMethodDefinition((MethodDefinitionHandle)attr.Constructor);
                TypeDefinition tdef = mr.GetTypeDefinition(mdef.GetDeclaringType());
                Console.WriteLine($"{mr.GetString(tdef.Namespace)}.{mr.GetString(tdef.Name)}");
            }
            else if (attr.Constructor.Kind == HandleKind.MemberReference)
            {
                MemberReference mref = mr.GetMemberReference((MemberReferenceHandle)attr.Constructor);

                if (mref.Parent.Kind == HandleKind.TypeReference)
                {
                    TypeReference tref = mr.GetTypeReference((TypeReferenceHandle)mref.Parent);
                    Console.WriteLine($"{mr.GetString(tref.Namespace)}.{mr.GetString(tref.Name)}");
                }
                else if (mref.Parent.Kind == HandleKind.TypeDefinition)
                {
                    TypeDefinition tdef = mr.GetTypeDefinition((TypeDefinitionHandle)mref.Parent);
                    Console.WriteLine($"{mr.GetString(tdef.Namespace)}.{mr.GetString(tdef.Name)}");
                }
            }
        }
    }
}

```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Reflection.Metadata.MetadataReader`
* `System.Reflection.PortableExecutable.PEReader`
* `System.Reflection.Metadata.Ecma335.MetadataBuilder`
* `System.Reflection.PortableExecutable.PEBuilder`
* `System.Reflection.PortableExecutable.ManagedPEBuilder`

## Additional Documentation

<!-- Links to further documentation -->

* [System.Reflection.Metadata.MetadataReader](https://docs.microsoft.com/dotnet/api/system.reflection.metadata.metadatareader)
* [System.Reflection.PortableExecutable.PEReader](https://docs.microsoft.com/dotnet/api/system.reflection.portableexecutable.pereader)
* [System.Reflection.Metadata.Ecma335.MetadataBuilder](https://docs.microsoft.com/dotnet/api/system.reflection.metadata.ecma335.metadatabuilder)
* [System.Reflection.PortableExecutable.PEBuilder](https://docs.microsoft.com/dotnet/api/system.reflection.portableexecutable.pebuilder)
* [System.Reflection.PortableExecutable.ManagedPEBuilder](https://docs.microsoft.com/dotnet/api/system.reflection.portableexecutable.managedpebuilder)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Reflection.Metadata is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).