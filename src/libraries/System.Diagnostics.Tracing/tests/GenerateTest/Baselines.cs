// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace System.Diagnostics.Tracing.Tests.GenerateTest
{
    [ExcludeFromCodeCoverage]
    internal static class Baselines
    {
        public static SourceText GetBaselineNode(string fileName)
        {
            var text = GetBaseline(fileName);
            return SourceText.From(text, Encoding.UTF8);
        }

        public static SyntaxTree GetBaselineTree(string fileName)
        {
            var sourceText = GetBaselineNode(fileName);
            return CSharpSyntaxTree.ParseText(sourceText, path: fileName);
        }

        public static string GetBaseline(string fileName)
        {
            using (var stream = typeof(Baselines).Assembly.GetManifestResourceStream($"System.Diagnostics.Tracing.Tests.GenerateTest.Baselines.{fileName}"))
            {
                if (stream == null)
                {
                    throw new ArgumentException($"No base line file {fileName}");
                }
                return new StreamReader(stream).ReadToEnd();
            }
        }
    }
}
