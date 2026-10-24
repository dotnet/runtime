// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Xml;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Extensions;
using Xunit;

namespace Mono.Linker.Tests.TestCasesRunner;

public partial class ResultChecker
{
    private static void VerifyExpectedXmlOutput(TrimmedTestCaseResult result, AssemblyDefinition original)
    {
        var documents = new Dictionary<string, XmlDocument>(StringComparer.Ordinal);
        foreach (CustomAttribute attribute in result.TestCase.FindTypeDefinition(original).CustomAttributes)
        {
            if (attribute.AttributeType.Name != nameof(ExpectedXmlElementAttribute))
                continue;

            string fileName = (string)attribute.ConstructorArguments[0].Value;
            string xpath = (string)attribute.ConstructorArguments[1].Value;
            int expectedCount = (int)attribute.ConstructorArguments[2].Value;
            if (!documents.TryGetValue(fileName, out XmlDocument? document))
            {
                NPath path = result.Sandbox.OutputDirectory.Combine(fileName);
                Assert.True(path.FileExists(), $"Expected XML output was not found at '{path}'.");
                document = new XmlDocument();
                document.Load(path.ToString());
                documents.Add(fileName, document);
            }

            XmlNodeList? matches = document.SelectNodes(xpath);
            Assert.NotNull(matches);
            Assert.True(matches.Count == expectedCount,
                $"Expected {expectedCount} XML nodes matching '{xpath}' in '{fileName}', but found {matches.Count}.");
        }
    }
}
