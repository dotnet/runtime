// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

#nullable enable

namespace ILLink.Tasks.Tests
{
    public class MicrosoftNETILLinkTargetsTests
    {
        [Fact]
        public void EntryPointRootIsNotAddedForLibraryProjects()
        {
            XDocument targets = XDocument.Load(GetTargetsFilePath());

            XElement entryPointRoot = targets.Descendants()
                .Where(element => element.Name.LocalName == "TrimmerRootAssembly")
                .Single(element =>
                    GetAttributeValue(element, "Include") == "@(IntermediateAssembly->'%(Filename)')" &&
                    GetAttributeValue(element, "RootMode") == "EntryPoint");

            Assert.Equal("'$(OutputType)' != 'Library'", GetAttributeValue(entryPointRoot, "Condition"));
        }

        private static string GetTargetsFilePath()
        {
            DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null)
            {
                string targetsFilePath = Path.Combine(directory.FullName, "src", "tools", "illink", "src", "ILLink.Tasks", "build", "Microsoft.NET.ILLink.targets");
                if (File.Exists(targetsFilePath))
                    return targetsFilePath;

                directory = directory.Parent;
            }

            throw new FileNotFoundException("Could not locate Microsoft.NET.ILLink.targets.");
        }

        private static string GetAttributeValue(XElement element, string attributeName)
            => element.Attribute(attributeName)?.Value ?? throw new InvalidDataException($"Missing '{attributeName}' attribute.");
    }
}
