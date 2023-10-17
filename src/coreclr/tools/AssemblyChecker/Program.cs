// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a simple console application that is designed to answer True or False
// questions about whether a given file is a managed assembly or not.
// You can also ask whether or not the assembly is debuggable.
// Return code of 0 indicates the file is a managed assembly.
// Return code of 1 indicates the file is not a managed assembly. No errors will be printed for this one.

using System.Text;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace AssemblyChecker
{
    public class Program
    {
        static bool IsAssembly(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // Try to read CLI metadata from the PE file.
            using var peReader = new PEReader(fs);

            if (!peReader.HasMetadata)
            {
                return false; // File does not have CLI metadata.
            }

            // Check that file has an assembly manifest.
            MetadataReader reader = peReader.GetMetadataReader();
            return reader.IsAssembly;
        }

        static bool HasDebuggableAttribute(MetadataReader reader, EntityHandle entity)
        {
            if (entity.IsNil)
                return false;

            switch (entity.Kind)
            {
                case HandleKind.MemberReference:
                    {
                        var memRef = reader.GetMemberReference((MemberReferenceHandle)entity);
                        switch (memRef.Parent.Kind)
                        {
                            case HandleKind.TypeReference:
                                {
                                    var tyRef = reader.GetTypeReference((TypeReferenceHandle)memRef.Parent);
                                    return reader.GetString(tyRef.Name) == "DebuggableAttribute";
                                }

                            default:
                                return false;
                        }
                }

                default:
                    return false;
            }

        }

        static bool IsDebug(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // Try to read CLI metadata from the PE file.
            using var peReader = new PEReader(fs);

            if (!peReader.HasMetadata)
            {
                return false; // File does not have CLI metadata.
            }

            // Check that file has an assembly manifest.
            MetadataReader reader = peReader.GetMetadataReader();

            var attrs = reader.GetAssemblyDefinition().GetCustomAttributes();
            return attrs.Any(x => HasDebuggableAttribute(reader, reader.GetCustomAttribute(x).Constructor));
        }

        static bool IsExe(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // Try to read CLI metadata from the PE file.
            using var peReader = new PEReader(fs);

            if (!peReader.HasMetadata)
            {
                return false; // File does not have CLI metadata.
            }

            return peReader.PEHeaders.IsExe;
        }

        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Expected assembly file-path.");
                return 2;
            }

            if (args.Length == 1)
            {
                if (IsAssembly(args[0]))
                    return 0;
                else
                    return 1;
            }

            if (args.Length == 2)
            {
                switch (args[0])
                {
                    case "--is-debug":
                        {
                            if (IsDebug(args[1]))
                                return 0;
                            else
                                return 1;
                        }

                    case "--is-exe":
                        {
                            if (IsExe(args[1]))
                                return 0;
                            else
                                return 1;
                        }
                }
                else
                {
                    Console.Error.WriteLine("Invalid option.");
                    return 2;
                }
            }

            Console.Error.WriteLine("Too many arguments.");
            return 2;
        }
    }
}
