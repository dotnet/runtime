// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public abstract class BaseMetadataProvider
	{
		protected readonly TestCase _testCase;
		protected readonly TypeDefinition _testCaseTypeDefinition;

		protected BaseMetadataProvider (TestCase testCase, AssemblyDefinition fullTestCaseAssemblyDefinition)
		{
			_testCase = testCase;
			// The test case types are never nested so we don't need to worry about that
			_testCaseTypeDefinition = fullTestCaseAssemblyDefinition.MainModule.GetType (_testCase.ReconstructedFullTypeName);

			if (_testCaseTypeDefinition == null)
				throw new InvalidOperationException ($"Could not find the type definition for {_testCase.Name} in {_testCase.SourceFile}");
		}

		protected T? GetOptionAttributeValue<T> (string attributeName, T? defaultValue)
		{
			var attribute = _testCaseTypeDefinition.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.Name == attributeName);
			if (attribute != null)
				return (T?) attribute.ConstructorArguments.First ().Value;

			return defaultValue;
		}

		protected NPath MakeSourceTreeFilePathAbsolute (string value)
		{
			return _testCase.SourceFile.Parent.Combine (value);
		}

		protected SourceAndDestinationPair GetSourceAndRelativeDestinationValue (CustomAttribute attribute)
		{
			var fullSource = SourceFileForAttributeArgumentValue (attribute.ConstructorArguments.First ().Value);
			var destinationFileName = (string) attribute.ConstructorArguments[1].Value;
			return new SourceAndDestinationPair {
				Source = fullSource,
				DestinationFileName = string.IsNullOrEmpty (destinationFileName) ? fullSource.FileName : destinationFileName
			};
		}


		protected virtual NPath SourceFileForAttributeArgumentValue (object value)
		{
			if (value is TypeReference valueAsTypeRef) {
				// Use the parent type for locating the source file
				var parentType = ParentMostType (valueAsTypeRef);
				var pathRelativeToAssembly = $"{parentType.FullName.Substring (parentType.Module.Name.Length - 3).Replace ('.', '/')}.cs".ToNPath ();
				var pathElements = pathRelativeToAssembly.Elements.ToArray ();
				var topMostDirectoryName = pathElements[0];
				var topMostDirectory = _testCase.SourceFile.RecursiveParents.Reverse ().FirstOrDefault (d => !d.IsRoot && d.FileName == topMostDirectoryName);

				if (topMostDirectory == null) {
					// Before giving up, try and detect the naming scheme for tests that use a dot in the top level directory name.
					// Ex:
					// Attributes.Debugger
					// + 1 because the file name is one of the elements
					if (pathElements.Length >= 3) {
						topMostDirectoryName = $"{pathElements[0]}.{pathElements[1]}";
						topMostDirectory = _testCase.SourceFile.RecursiveParents.Reverse ().FirstOrDefault (d => !d.IsRoot && d.FileName == topMostDirectoryName);
						pathRelativeToAssembly = topMostDirectoryName.ToNPath ().Combine (pathElements.Skip (2).Aggregate (new NPath (string.Empty), (path, s) => path.Combine (s)));
					}

					if (topMostDirectory == null)
						throw new ArgumentException ($"Unable to locate the source file for type {valueAsTypeRef}.  Could not locate directory {topMostDirectoryName}.  Ensure the type name matches the file name.  And the namespace match the directory structure on disk");
				}

				var fullPath = topMostDirectory.Parent.Combine (pathRelativeToAssembly);

				if (!fullPath.Exists ())
					throw new ArgumentException ($"Unable to locate the source file for type {valueAsTypeRef}.  Expected {fullPath}.  Ensure the type name matches the file name.  And the namespace match the directory structure on disk");

				return fullPath;
			}

			return MakeSourceTreeFilePathAbsolute (value.ToString ()!);
		}

		private static TypeReference ParentMostType (TypeReference type)
		{
			if (!type.IsNested)
				return type;

			return ParentMostType (type.DeclaringType);
		}
	}
}
