// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Extensions;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public static class MemberAssertionsCollector
	{
		internal static IEnumerable<(IMemberDefinition member, CustomAttribute ca)> GetMemberAssertions (Type type)
		{
			var resolver = new DefaultAssemblyResolver ();
			resolver.AddSearchDirectory (Path.GetDirectoryName (type.Assembly.Location));
			var assembly = resolver.Resolve (new AssemblyNameReference (type.Assembly.GetName ().Name, null));
			var t = assembly.MainModule.GetType (type.Namespace + "." + type.Name);
			if (t == null)
				throw new InvalidOperationException ($"type {type} not found in {assembly}");
			var results = new List<(IMemberDefinition, CustomAttribute)> ();
			CollectMemberAssertions (t, results);
			return results;
		}

		public static IEnumerable<TestCaseData> GetMemberAssertionsData (Type type)
		{
			return GetMemberAssertions (type).Select (v => {
				var testCaseData = new TestCaseData (v.member, v.ca);
				// Sanitize test names to work around https://github.com/nunit/nunit3-vs-adapter/issues/691.
				testCaseData.SetName ($"{{m}}({v.member.Name},{v.ca.AttributeType.Name})");
				return testCaseData;
			});
		}

		private static bool IsMemberAssertion (TypeReference attributeType)
		{
			if (attributeType == null)
				return false;

			if (attributeType.Namespace != "Mono.Linker.Tests.Cases.Expectations.Assertions")
				return false;

			return attributeType.Resolve ().DerivesFrom (nameof (BaseMemberAssertionAttribute));
		}

		private static void CollectMemberAssertions (TypeDefinition type, List<(IMemberDefinition, CustomAttribute)> results)
		{
			if (type.HasCustomAttributes) {
				foreach (var ca in type.CustomAttributes) {
					if (!IsMemberAssertion (ca.AttributeType))
						continue;
					results.Add ((type, ca));
				}
			}

			foreach (var m in type.Methods) {
				if (!m.HasCustomAttributes)
					continue;

				foreach (var ca in m.CustomAttributes) {
					if (!IsMemberAssertion (ca.AttributeType))
						continue;
					results.Add ((m, ca));
				}
			}

			foreach (var f in type.Fields) {
				if (!f.HasCustomAttributes)
					continue;

				foreach (var ca in f.CustomAttributes) {
					if (!IsMemberAssertion (ca.AttributeType))
						continue;
					results.Add ((f, ca));
				}
			}

			foreach (var p in type.Properties) {
				if (!p.HasCustomAttributes)
					continue;

				foreach (var ca in p.CustomAttributes) {
					if (!IsMemberAssertion (ca.AttributeType))
						continue;
					results.Add ((p, ca));
				}
			}

			foreach (var e in type.Events) {
				if (!e.HasCustomAttributes)
					continue;

				foreach (var ca in e.CustomAttributes) {
					if (!IsMemberAssertion (ca.AttributeType))
						continue;
					results.Add ((e, ca));
				}
			}

			if (!type.HasNestedTypes)
				return;

			foreach (var nested in type.NestedTypes) {
				CollectMemberAssertions (nested, results);
			}
		}
	}
}
