// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public class TestCaseCollector
    {
        private readonly NPath _rootDirectory;
        private readonly NPath _testCaseAssemblyRoot;

        public TestCaseCollector(string rootDirectory, string testCaseAssemblyPath)
            : this(rootDirectory.ToNPath(), testCaseAssemblyPath.ToNPath())
        {
        }

        public TestCaseCollector(NPath rootDirectory, NPath testCaseAssemblyRoot)
        {
            _rootDirectory = rootDirectory;
            _testCaseAssemblyRoot = testCaseAssemblyRoot;
        }

        public IEnumerable<TestCase> Collect()
        {
            return Collect(AllSourceFiles());
        }

        public TestCase? Collect(NPath sourceFile)
        {
            return Collect(new[] { sourceFile }).FirstOrDefault();
        }

        public IEnumerable<TestCase> Collect(IEnumerable<NPath> sourceFiles)
        {
            _rootDirectory.DirectoryMustExist();
            _testCaseAssemblyRoot.DirectoryMustExist();

            foreach (var file in sourceFiles)
            {
                var testCaseAssemblyPath = FindTestCaseAssembly(file);
                testCaseAssemblyPath.FileMustExist();
                if (CreateCase(testCaseAssemblyPath, file, out TestCase? testCase))
                    yield return testCase;
            }
        }

        NPath FindTestCaseAssembly(NPath sourceFile)
        {
            if (!sourceFile.IsChildOf(_rootDirectory))
                throw new ArgumentException($"{sourceFile} is not a child of {_rootDirectory}");

            var current = sourceFile;
            do
            {
                if (current.Parent.Files("*.csproj").FirstOrDefault() is NPath csproj)
                {
                    var relative = csproj.Parent.RelativeTo(_rootDirectory);
                    return _testCaseAssemblyRoot.Combine(relative).Combine(csproj.ChangeExtension("dll").FileName);
                }

                current = current.Parent;
            } while (current != _rootDirectory);

            throw new InvalidOperationException($"Could not find a .csproj file for {sourceFile}");
        }

        public IEnumerable<NPath> AllSourceFiles()
        {
            _rootDirectory.DirectoryMustExist();

            foreach (var file in _rootDirectory.Files("*.cs"))
            {
                yield return file;
            }

            foreach (var subDir in _rootDirectory.Directories())
            {
                if (subDir.FileName == "bin" || subDir.FileName == "obj" || subDir.FileName == "Properties")
                    continue;

                foreach (var file in subDir.Files("*.cs", true))
                {
                    var relativeParents = file.RelativeTo(_rootDirectory);

                    if (relativeParents.RecursiveParents.Any(p => p.Elements.Any() && p.FileName == "Dependencies"))
                        continue;

                    if (relativeParents.RecursiveParents.Any(p => p.Elements.Any() && p.FileName == "Individual"))
                        continue;

                    yield return file;
                }
            }
        }

        public TestCase? CreateIndividualCase(Type testCaseType)
        {
            _rootDirectory.DirectoryMustExist();

            var pathRelativeToAssembly = $"{testCaseType.FullName?.Substring(testCaseType.Module.Name.Length - 3).Replace('.', '/')}.cs";
            var fullSourcePath = _rootDirectory.Combine(pathRelativeToAssembly).FileMustExist();
            var testCaseAssemblyPath = FindTestCaseAssembly(fullSourcePath);

            if (!CreateCase(testCaseAssemblyPath, fullSourcePath, out TestCase? testCase))
                throw new ArgumentException($"Could not create a test case for `{testCaseType}`.  Ensure the namespace matches it's location on disk");

            return testCase;
        }

        private bool CreateCase(NPath caseAssemblyPath, NPath sourceFile, [NotNullWhen(true)] out TestCase? testCase)
        {
            using AssemblyDefinition caseAssemblyDefinition = AssemblyDefinition.ReadAssembly(caseAssemblyPath.ToString());

            var potentialCase = new TestCase(sourceFile, _rootDirectory, caseAssemblyPath);
            var typeDefinition = potentialCase.TryFindTypeDefinition(caseAssemblyDefinition);

            testCase = null;

            if (typeDefinition is null)
            {
                Console.WriteLine($"Could not find the matching type for test case {sourceFile}.  Ensure the file name and class name match");
                return false;
            }

            if (typeDefinition.HasAttribute(nameof(NotATestCaseAttribute)))
            {
                return false;
            }

            var mainMethod = typeDefinition.Methods.FirstOrDefault(m => m.Name ==
                (typeDefinition.FullName == "Program"
                    ? "<Main>$"
                    : "Main"));

            if (mainMethod is null)
            {
                Console.WriteLine($"{typeDefinition} in {sourceFile} is missing a Main() method");
                return false;
            }

            if (!mainMethod.IsStatic)
            {
                Console.WriteLine($"The Main() method for {typeDefinition} in {sourceFile} should be static");
                return false;
            }

            testCase = potentialCase;
            return true;
        }
    }
}
