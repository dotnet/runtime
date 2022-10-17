// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;
using Mono.Linker.Tests.TestCasesRunner;
using NUnit.Framework;

namespace Mono.Linker.Tests.Tests
{
	[TestFixture]
	public class TestFrameworkRulesAndConventions
	{
		[Test]
		public void OnlyAttributeTypesInExpectations ()
		{
			foreach (var expectationsAssemblyPath in ExpectationAssemblies ()) {
				using (var assembly = AssemblyDefinition.ReadAssembly (expectationsAssemblyPath)) {
					var nonAttributeTypes = assembly.MainModule.AllDefinedTypes ().Where (t => !IsAcceptableExpectationsAssemblyType (t)).ToArray ();

					Assert.That (nonAttributeTypes, Is.Empty);
				}
			}
		}

		[Test]
		public void CanFindATypeForAllCsFiles ()
		{
			var collector = CreateCollector ();

			var missing = collector.AllSourceFiles ().Where (path => {
				return collector.Collect (path) == null;
			})
				.ToArray ();

			Assert.That (missing, Is.Empty, $"Could not locate a type for the following files.  Verify the type name and file name match and that the type is not excluded by a #if");
		}

		/// <summary>
		/// Virtual to allow a derived test suite to setup the test case collector differently
		/// </summary>
		/// <returns></returns>
		protected virtual TestCaseCollector CreateCollector ()
		{
			return TestDatabase.CreateCollector ();
		}

		/// <summary>
		/// Virtual to allow creation of a derived test suite for a different expectations assembly, or multiple
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerable<NPath> ExpectationAssemblies ()
		{
			yield return PathUtilities.GetTestAssemblyPath ("Mono.Linker.Tests.Cases.Expectations").ToNPath ();
		}

		static bool IsAcceptableExpectationsAssemblyType (TypeDefinition type)
		{
			if (type.Name == "<Module>")
				return true;

			// Helpers - these should few and very simple
			if (type.Namespace == "Mono.Linker.Tests.Cases.Expectations.Helpers")
				return true;

			// Simple types like enums are OK and needed for certain attributes
			if (type.IsEnum)
				return true;

			// Attributes are OK because that is the purpose of the Expectations assembly, to provide attributes for annotating test cases
			if (IsAttributeType (type))
				return true;

			// Anything else is not OK and should probably be defined in Mono.Linker.Tests.Cases and use SandboxDependency in order to be included
			// with the tests that need it
			return false;
		}

		static bool IsAttributeType (TypeDefinition type)
		{
			if (type.IsTypeOf (WellKnownType.System_Attribute))
				return true;

			if (type.BaseType == null)
				return false;

			return IsAttributeType (type.BaseType.Resolve ());
		}
	}
}
