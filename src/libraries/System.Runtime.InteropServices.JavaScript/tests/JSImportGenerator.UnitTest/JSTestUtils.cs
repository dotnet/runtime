// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis;
using Xunit;

namespace JSImportGenerator.Unit.Tests
{
    public static class JSTestUtils
    {
        public static MetadataReference GetJavaScriptReference()
        {
            var attrAssem = typeof(JSObject).GetTypeInfo().Assembly;
            return MetadataReference.CreateFromFile(attrAssem.Location);
        }

        public static void AssertMessages(ImmutableArray<Diagnostic> diagnostics, string[] additionalAllowedDiagnostics)
        {
            var allowedDiagnostics = new HashSet<string>(additionalAllowedDiagnostics);
            foreach (Diagnostic diagnostic in diagnostics)
            {
                var message = diagnostic.GetMessage();
                Assert.True(allowedDiagnostics.Contains(message), message);
            }
        }

        public static void DumpCode(string source, Compilation compilation, ImmutableArray<Diagnostic> generatorDiags)
        {
            string prefix = Guid.NewGuid().ToString();
            ImmutableArray<Diagnostic> compDiag = compilation.GetDiagnostics();
            Directory.CreateDirectory(prefix);

            using (StreamWriter writer = File.CreateText("./" + prefix + "/" + "src.cs"))
            {
                writer.Write(source);
            }

            var generatedSyntaxTrees = compilation.SyntaxTrees.ToList();
            for (int i = 0; i < generatedSyntaxTrees.Count; i++)
            {
                SyntaxTree? tree = generatedSyntaxTrees[i];
                var sourceText = tree.GetText();
                using (StreamWriter writer = File.CreateText("./" + prefix + "/" + i + "gen.cs"))
                {
                    sourceText.Write(writer);
                }
            }

            using (StreamWriter writer = File.CreateText("./" + prefix + "/" + "gerr.txt"))
            {
                foreach (var diag in generatorDiags)
                {
                    writer.WriteLine(diag.GetMessage());
                }
            }

            using (StreamWriter writer = File.CreateText("./" + prefix + "/" + "gferr.txt"))
            {
                foreach (var diag in generatorDiags)
                {
                    writer.WriteLine(diag.ToString());
                }
            }

            using (StreamWriter writer = File.CreateText("./" + prefix + "/" + "cerr.txt"))
            {
                foreach (var diag in compDiag)
                {
                    writer.WriteLine(diag.GetMessage());
                }
            }
            using (StreamWriter writer = File.CreateText("./" + prefix + "/" + "cferr.txt"))
            {
                foreach (var diag in compDiag)
                {
                    writer.WriteLine(diag.ToString());
                }
            }
        }
    }
}
