// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace AssemblyChecker
{
    /// <summary>
    /// This is a simple console application that is designed to answer True or False
    /// questions about whether a given file is a managed assembly or not.
    /// You can also ask whether or not the assembly is debuggable.
    /// Return code of 0 indicates the file is a managed assembly.
    /// Return code of 1 indicates the file is not a managed assembly. No errors will be printed for this one.
    /// </summary>
    public class Program
    {
        private const string HelpText = @"
Usage:
    <filePath>: Check if the file-path is a managed assembly.
    --is-debug <filePath>: Check if the file-path is a managed assembly that is built with debuggability.
    --is-exe <filePath>: Check if the file-path is a managed assembly that is an executable.
";

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

        static bool IsDebug(string path)
        {
            return
                Assembly.LoadFrom(path)
                .GetCustomAttributes(typeof(DebuggableAttribute), false)
                .OfType<DebuggableAttribute>().Any(x => x.IsJITOptimizerDisabled);
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
                Console.WriteLine(HelpText);
                Console.Error.WriteLine("\nExpected assembly file-path.");
                return 2;
            }

            // Help
            if (args.Contains("-h"))
            {
                Console.WriteLine(HelpText);
                return 0;
            }

            if (args.Length == 1)
            {
                return IsAssembly(args[0]) ? 0 : 1;
            }

            if (args.Length == 2)
            {
                switch (args[0])
                {
                    case "--is-debug":
                        {
                            return IsDebug(args[1]) ? 0 : 1;
                        }

                    case "--is-exe":
                        {
                            return IsExe(args[1]) ? 0 : 1;
                        }

                    default:
                        {
                            Console.WriteLine(HelpText);
                            Console.Error.WriteLine("\nInvalid option.");
                            return 2;
                        }
                }
            }

            Console.WriteLine(HelpText);
            Console.Error.WriteLine("\nToo many arguments.");
            return 2;
        }
    }
}
