// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Extensions.Logging.Generators.Tests
{
    public class CompilationHelper
    {
        public static Compilation CreateCompilation(
            string source,
            MetadataReference[]? additionalReferences = null,
            string assemblyName = "TestAssembly")
        {
            string corelib = Assembly.GetAssembly(typeof(object))!.Location;
            string runtimeDir = Path.GetDirectoryName(corelib)!;

            var refs = new List<MetadataReference>();
            refs.Add(MetadataReference.CreateFromFile(corelib));
            refs.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "netstandard.dll")));
            refs.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")));
            refs.Add(MetadataReference.CreateFromFile(typeof(ILogger).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(LoggerMessageAttribute).Assembly.Location));

            if (additionalReferences != null)
            {
                foreach (MetadataReference reference in additionalReferences)
                {
                    refs.Add(reference);
                }
            }

            CSharpParseOptions options = CSharpParseOptions.Default;

#if ROSLYN4_0_OR_GREATER
            // workaround https://github.com/dotnet/roslyn/pull/55866. We can remove "LangVersion=Preview" when we get a Roslyn build with that change.
            options = options.WithLanguageVersion(LanguageVersion.Preview);
#endif

            return CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source, options) },
                references: refs.ToArray(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );
        }

        public static byte[] CreateAssemblyImage(Compilation compilation)
        {
            MemoryStream ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);
            if (!emitResult.Success)
            {
                throw new InvalidOperationException();
            }
            return ms.ToArray();
        }
    }
}
