// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCases
{
    public class TestCase
    {
        public TestCase(NPath sourceFile, NPath rootCasesDirectory, NPath originalTestCaseAssemblyPath)
        {
            SourceFile = sourceFile;
            RootCasesDirectory = rootCasesDirectory;
            OriginalTestCaseAssemblyPath = originalTestCaseAssemblyPath;
            Name = sourceFile.FileNameWithoutExtension;
            var fullyRelative = sourceFile.RelativeTo(rootCasesDirectory);
            var displayNameRelative = fullyRelative.RelativeTo(new NPath(fullyRelative.Elements.First()));
            string displayNameBase = displayNameRelative.Depth == 1 ? "" : displayNameRelative.Parent.ToString(SlashMode.Forward).Replace('/', '.');
            DisplayName = sourceFile.FileNameWithoutExtension == "Program" && sourceFile.Parent.FileName == originalTestCaseAssemblyPath.FileNameWithoutExtension
                ? displayNameBase
                : $"{displayNameBase}.{sourceFile.FileNameWithoutExtension}";
            if (DisplayName.StartsWith("."))
                DisplayName = DisplayName.Substring(1);

            ReconstructedFullTypeName = $"Mono.Linker.Tests.Cases.{fullyRelative.Parent.ToString(SlashMode.Forward).Replace('/', '.')}.{sourceFile.FileNameWithoutExtension}";

            var firstParentRelativeToRoot = SourceFile.RelativeTo(rootCasesDirectory).Elements.First();
            TestSuiteDirectory = rootCasesDirectory.Combine(firstParentRelativeToRoot);
        }

        public NPath RootCasesDirectory { get; }

        public string Name { get; }

        public string DisplayName { get; }

        public NPath SourceFile { get; }

        public NPath OriginalTestCaseAssemblyPath { get; }

        public string ReconstructedFullTypeName { get; }

        public bool HasLinkXmlFile
        {
            get { return SourceFile.ChangeExtension("xml").FileExists(); }
        }

        public NPath LinkXmlFile
        {
            get
            {
                if (!HasLinkXmlFile)
                    throw new InvalidOperationException("This test case does not have a link xml file");

                return SourceFile.ChangeExtension("xml");
            }
        }

        public NPath TestSuiteDirectory { get; }

        public TypeDefinition FindTypeDefinition(AssemblyDefinition assemblyDefinition)
            => TryFindTypeDefinition(assemblyDefinition) is TypeDefinition typeDefinition
                ? typeDefinition
                : throw new InvalidOperationException($"Could not find the type definition for {Name} in {assemblyDefinition.Name}");

        public TypeDefinition? TryFindTypeDefinition(AssemblyDefinition caseAssemblyDefinition)
        {
            var typeDefinition = caseAssemblyDefinition.MainModule.GetType(ReconstructedFullTypeName);

            if (typeDefinition is not null)
                return typeDefinition;

            foreach (var type in caseAssemblyDefinition.MainModule.Types)
            {
                if (type.Name == "Program" &&
                    type.CustomAttributes.Any(attr => attr.AttributeType.Name == nameof(CompilerGeneratedAttribute)))
                    return type;

                if (string.IsNullOrEmpty(type.Namespace))
                    continue;

                if (type.Name == Name)
                {
                    if (!SourceFile.ReadAllText().Contains($"namespace {type.Namespace}"))
                        continue;

                    return type;
                }
            }

            return null;
        }
    }
}
