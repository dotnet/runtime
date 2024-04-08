// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Loader;
using ILCompiler;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class MemberAssertionsCollector : IEnumerable<object[]>
	{
		public struct CustomAttribute
		{
			public MetadataType AttributeType;
			public CustomAttributeValue<TypeDesc> Value;

			public override string ToString () => AttributeType.ToString ();
		}

		public MemberAssertionsCollector(Type type)
		{
			this.type = type;
			TypeSystemContext = CreateTypeSystemContext();
		}

		private CompilerTypeSystemContext TypeSystemContext;
		private readonly Type type;

		private CompilerTypeSystemContext CreateTypeSystemContext ()
		{
			TrimmingDriver.ComputeDefaultOptions (out var targetOS, out var targetArchitecture);
			var targetDetails = new TargetDetails (targetArchitecture, targetOS, TargetAbi.NativeAot);
			CompilerTypeSystemContext typeSystemContext =
				new CompilerTypeSystemContext (targetDetails, SharedGenericsMode.CanonicalReferenceTypes, DelegateFeature.All);
			typeSystemContext.InputFilePaths = new Dictionary<string, string> () {
				{ type.Assembly.GetName().Name!, type.Assembly.Location }
			};
			Dictionary<string, string> references = new Dictionary<string, string> ();
			foreach (Assembly assembly in AssemblyLoadContext.Default.Assemblies) {
				references.Add (assembly.GetName ().Name!, assembly.Location!);
			}
			typeSystemContext.ReferenceFilePaths = references;
			typeSystemContext.SetSystemModule (typeSystemContext.GetModuleForSimpleName (TrimmingDriver.DefaultSystemModule));

			return typeSystemContext;
		}

		internal IEnumerable<(TypeSystemEntity member, CustomAttribute ca)> GetMemberAssertions (Type type)
		{
			var module = TypeSystemContext.GetModuleFromPath (type.Assembly.Location);
			var t = module.GetType (type.Namespace, type.Name, throwIfNotFound: false);
			if (t == null)
				throw new InvalidOperationException ($"type {type} not found in {module}");
			var results = new List<(TypeSystemEntity, CustomAttribute)> ();
			CollectMemberAssertions (t, results);
			return results;
		}

		public IEnumerable<object[]> GetMemberAssertionsData ()
		{
			return GetMemberAssertions (type).Select (v => new object[] { v.member, v.ca });
		}

		public IEnumerator<object[]> GetEnumerator () => GetMemberAssertionsData ().GetEnumerator ();
		IEnumerator IEnumerable.GetEnumerator () => throw new NotImplementedException ();

		private static bool IsMemberAssertion (MetadataType attributeType)
		{
			if (attributeType == null)
				return false;

			if (attributeType.Namespace != "Mono.Linker.Tests.Cases.Expectations.Assertions")
				return false;

			MetadataType t = attributeType;
			while (t != null) {
				if (t.Name == nameof (BaseMemberAssertionAttribute))
					return true;

				t = t.MetadataBaseType;
			}

			return false;
		}

		private static void CollectMemberAssertions (MetadataType metadataType, List<(TypeSystemEntity, CustomAttribute)> results)
		{
			if (metadataType is not EcmaType type)
				return;

			foreach (var ca in GetCustomAttributes (type)) {
				if (!IsMemberAssertion (ca.AttributeType))
					continue;
				results.Add ((type, ca));
			}

			foreach (var md in type.GetMethods ()) {
				if (md is not EcmaMethod m)
					continue;

				foreach (var ca in GetCustomAttributes(m)) {
					if (!IsMemberAssertion (ca.AttributeType))
						continue;
					results.Add ((m, ca));
				}
			}

			foreach (var fd in type.GetFields()) {
				if (fd is not EcmaField f)
					continue;

				foreach (var ca in GetCustomAttributes(f)) {
					if (!IsMemberAssertion (ca.AttributeType))
						continue;
					results.Add ((f, ca));
				}
			}

			foreach (var propertyHandle in type.MetadataReader.GetTypeDefinition (type.Handle).GetProperties ()) {
				var p = new PropertyPseudoDesc (type, propertyHandle);
				foreach (var ca in GetCustomAttributes(type, p)) {
					if (!IsMemberAssertion (ca.AttributeType))
						continue;
					results.Add ((p, ca));
				}
			}

			foreach (var eventHandle in type.MetadataReader.GetTypeDefinition (type.Handle).GetEvents ()) {
				var e = new EventPseudoDesc (type, eventHandle);
				foreach (var ca in GetCustomAttributes(type, e)) {
					if (!IsMemberAssertion (ca.AttributeType))
						continue;
					results.Add ((e, ca));
				}
			}

			foreach (var nested in type.GetNestedTypes()) {
				CollectMemberAssertions (nested, results);
			}
		}

		private static IEnumerable<CustomAttribute> GetCustomAttributes (EcmaType type)
		{
			var metadataReader = type.MetadataReader;
			return GetCustomAttributes (metadataReader.GetTypeDefinition (type.Handle).GetCustomAttributes (), metadataReader, type.EcmaModule);
		}

		private static IEnumerable<CustomAttribute> GetCustomAttributes (EcmaMethod method)
		{
			var metadataReader = method.MetadataReader;
			return GetCustomAttributes (metadataReader.GetMethodDefinition (method.Handle).GetCustomAttributes (), metadataReader, method.Module);
		}

		private static IEnumerable<CustomAttribute> GetCustomAttributes (EcmaField field)
		{
			var metadataReader = field.MetadataReader;
			return GetCustomAttributes (metadataReader.GetFieldDefinition (field.Handle).GetCustomAttributes (), metadataReader, field.Module);
		}

		private static IEnumerable<CustomAttribute> GetCustomAttributes (EcmaType type, PropertyPseudoDesc prop)
		{
			return GetCustomAttributes (prop.GetCustomAttributes, type.MetadataReader, type.EcmaModule);
		}

		private static IEnumerable<CustomAttribute> GetCustomAttributes (EcmaType type, EventPseudoDesc @event)
		{
			return GetCustomAttributes (@event.GetCustomAttributes, type.MetadataReader, type.EcmaModule);
		}

		private static IEnumerable<CustomAttribute> GetCustomAttributes(CustomAttributeHandleCollection attributeHandles, MetadataReader metadataReader, EcmaModule module)
		{
			foreach (var attributeHandle in attributeHandles) {
				if (!metadataReader.GetAttributeTypeAndConstructor (attributeHandle, out var attributeType, out _))
					continue;

				if (module.GetType (attributeType) is not MetadataType attributeMetadataType)
					continue;

				yield return
					new CustomAttribute () {
						AttributeType = attributeMetadataType,
						Value = metadataReader.GetCustomAttribute (attributeHandle).DecodeValue (new CustomAttributeTypeProvider (module))
					};
			}
		}
	}
}
