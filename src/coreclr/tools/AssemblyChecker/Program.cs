// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentException("Expected assembly file-path.");
            }

            if (args.Length > 2)
            {
                throw new ArgumentException("Too many arguments.");
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
                if (args[0] == "--is-debug")
                {
                    if (IsDebug(args[1]))
                        return 0;
                    else
                        return 1;
                }
                else
                {
                    throw new ArgumentException("Invalid option.");
                }
            }

            return -1;
        }
    }
}
