## About

This package provides a low-level .NET (ECMA-335) metadata reader and writer. It's geared for performance and is the ideal choice for building higher-level libraries that intend to provide their own object model, such as compilers. The metadata format is defined by the [ECMA-335 - Common Language Infrastructure (CLI)](http://www.ecma-international.org/publications/standards/Ecma-335.htm) specification and [its amendments](https://github.com/dotnet/runtime/blob/main/docs/design/specs/Ecma-335-Augments.md).

The `System.Reflection.Metadata` library is built-in as part of the shared framework in .NET Runtime. The package can be installed when you need to use it in other target frameworks.

For more information, see the documentation:

- [System.Reflection.Metadata.MetadataReader](https://docs.microsoft.com/dotnet/api/system.reflection.metadata.metadatareader)
- [System.Reflection.PortableExecutable.PEReader](https://docs.microsoft.com/dotnet/api/system.reflection.portableexecutable.pereader)
- [System.Reflection.Metadata.Ecma335.MetadataBuilder](https://docs.microsoft.com/dotnet/api/system.reflection.metadata.ecma335.metadatabuilder)
- [System.Reflection.PortableExecutable.PEBuilder](https://docs.microsoft.com/dotnet/api/system.reflection.portableexecutable.pebuilder)
- [System.Reflection.PortableExecutable.ManagedPEBuilder](https://docs.microsoft.com/dotnet/api/system.reflection.portableexecutable.managedpebuilder)

## Example

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
        Console.WriteLine($"Image base:     0x{header.ImageBase.ToString("X")}");
        Console.WriteLine($"File alignment: 0x{header.FileAlignment.ToString("X")}");
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
