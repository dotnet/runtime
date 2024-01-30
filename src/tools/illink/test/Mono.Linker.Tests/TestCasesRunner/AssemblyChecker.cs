// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Dataflow;
using Mono.Linker.Tests.Cases.CppCLI;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Extensions;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner
{
	partial class AssemblyChecker
	{
		readonly AssemblyDefinition originalAssembly, linkedAssembly;
		readonly TrimmedTestCaseResult linkedTestCase;

		HashSet<string> linkedMembers;
		readonly HashSet<string> verifiedGeneratedFields = new HashSet<string> ();
		readonly HashSet<string> verifiedEventMethods = new HashSet<string> ();
		readonly HashSet<string> verifiedGeneratedTypes = new HashSet<string> ();
		bool checkNames;

		public AssemblyChecker (AssemblyDefinition original, AssemblyDefinition linked, TrimmedTestCaseResult linkedTestCase)
		{
			this.originalAssembly = original;
			this.linkedAssembly = linked;
			this.linkedTestCase = linkedTestCase;

			checkNames = original.MainModule.GetTypeReferences ().Any (attr =>
				attr.Name == nameof (RemovedNameValueAttribute));
		}

		public void Verify ()
		{
			var failures = GetFailures ().ToList ();
			if (failures.Count > 0)
				Assert.Fail (string.Join (Environment.NewLine, failures));

			IEnumerable<string> GetFailures ()
			{
				foreach (var err in VerifyExportedTypes (originalAssembly, linkedAssembly)) yield return err;
				foreach (var err in VerifyCustomAttributes (originalAssembly, linkedAssembly)) yield return err;
				foreach (var err in VerifySecurityAttributes (originalAssembly, linkedAssembly)) yield return err;

				foreach (var originalModule in originalAssembly.Modules)
					foreach (var err in VerifyModule (originalModule, linkedAssembly.Modules.FirstOrDefault (m => m.Name == originalModule.Name))) yield return err;

				foreach (var err in VerifyResources (originalAssembly, linkedAssembly)) yield return err;
				foreach (var err in VerifyReferences (originalAssembly, linkedAssembly)) yield return err;
				foreach (var err in VerifyKeptByAttributes (originalAssembly, originalAssembly.FullName)) yield return err;

				linkedMembers = new HashSet<string> (linkedAssembly.MainModule.AllMembers ().Select (s => {
					return s.FullName;
				}), StringComparer.Ordinal);

				// Workaround for compiler injected attribute to describe the language version
				linkedMembers.Remove ("System.Void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor()");
				linkedMembers.Remove ("System.Int32 System.Runtime.CompilerServices.RefSafetyRulesAttribute::Version");
				linkedMembers.Remove ("System.Void System.Runtime.CompilerServices.RefSafetyRulesAttribute::.ctor(System.Int32)");

				// Workaround for compiler injected attribute to describe the language version
				verifiedGeneratedTypes.Add ("Microsoft.CodeAnalysis.EmbeddedAttribute");
				verifiedGeneratedTypes.Add ("System.Runtime.CompilerServices.RefSafetyRulesAttribute");

				var membersToAssert = originalAssembly.MainModule.Types;
				foreach (var originalMember in membersToAssert) {
					if (originalMember is TypeDefinition td) {
						if (td.Name == "<Module>") {
							linkedMembers.Remove (td.Name);
							continue;
						}

						TypeDefinition linkedType = linkedAssembly.MainModule.GetType (originalMember.FullName);
						foreach (var err in VerifyTypeDefinition (td, linkedType)) yield return err;
						linkedMembers.Remove (td.FullName);

						continue;
					}

					yield return $"Don't know how to check member of type {originalMember.GetType ()}";
				}

				if (linkedMembers.Any ())
					foreach (var err in linkedMembers.Select (m => $"Member `{m}' was not expected to be kept")) yield return err;
			}
		}

		static bool IsBackingField (FieldDefinition field) => field.Name.StartsWith ("<") && field.Name.EndsWith (">k__BackingField");

		protected virtual IEnumerable<string> VerifyModule (ModuleDefinition original, ModuleDefinition linked)
		{
			// We never link away a module today so let's make sure the linked one isn't null
			if (linked == null)
				yield return $"Linked assembly `{original.Assembly.Name.Name}` is missing module `{original.Name}`";

			var expected = original.Assembly.MainModule.AllDefinedTypes ()
				.SelectMany (t => GetCustomAttributeCtorValues<string> (t, nameof (KeptModuleReferenceAttribute)))
				.ToHashSet ();

			var actual = linked.ModuleReferences
				.Select (name => name.Name)
				.ToHashSet ();

			if (!expected.SetEquals (actual))
				yield return $"In module {original.FileName} Expected module references `{string.Join (", ", expected)}` but got `{string.Join (", ", actual)}`";

			foreach (var err in VerifyCustomAttributes (original, linked)) yield return err;
		}

		protected virtual IEnumerable<string> VerifyTypeDefinition (TypeDefinition original, TypeDefinition linked)
		{
			if (linked != null && verifiedGeneratedTypes.Contains (linked.FullName))
				yield break;

			ModuleDefinition linkedModule = linked?.Module;

			//
			// Little bit complex check to allow easier test writing to match
			// - It has [Kept] attribute or any variation of it
			// - It contains Main method
			// - It contains at least one member which has [Kept] attribute (not recursive)
			//
			bool expectedKept =
				HasActiveKeptDerivedAttribute (original) ||
				(linked != null && linkedModule.Assembly.EntryPoint?.DeclaringType == linked) ||
				original.AllMembers ().Any (HasActiveKeptDerivedAttribute);

			if (!expectedKept) {
				if (linked == null)
					yield break;

				// Compiler generated members can't be annotated with `Kept` attributes directly
				// For some of them we have special attributes (backing fields for example), but it's impractical to define
				// special attributes for all types of compiler generated members (there are quite a few of them and they're
				// going to change/increase over time).
				// So we're effectively disabling Kept validation on compiler generated members
				// Note that we still want to go "inside" each such member, as it might have additional attributes
				// we do want to validate. There's no specific use case right now, but I can easily imagine one
				// for more detailed testing of for example custom attributes on local functions, or similar.
				if (!IsCompilerGeneratedMember (original))
					yield return $"Type `{original}' should have been removed";
			}

			bool prev = checkNames;
			checkNames |= original.HasAttribute (nameof (VerifyMetadataNamesAttribute));

			foreach (var err in VerifyTypeDefinitionKept (original, linked)) yield return err;

			checkNames = prev;

			if (original.HasAttribute (nameof (CreatedMemberAttribute))) {
				foreach (var attr in original.CustomAttributes.Where (l => l.AttributeType.Name == nameof (CreatedMemberAttribute))) {
					var newName = original.FullName + "::" + attr.ConstructorArguments[0].Value.ToString ();

					// Assert.AreEqual (1, linkedMembers.RemoveWhere (l => l.Contains (newName)), $"Newly created member '{newName}' was not found");
					var asdf = linkedMembers.Where (l => l.Contains (newName)).ToList ();
					if (1 != linkedMembers.RemoveWhere (l => l.Contains (newName))) {
						yield return $"Newly created member '{newName}' was not found";
					}
				}
			}
		}

		/// <summary>
		/// Validates that all <see cref="KeptByAttribute"/> instances on a member are valid (i.e. ILLink recorded a marked dependency described in the attribute)
		/// </summary>
		IEnumerable<string> VerifyKeptByAttributes (IMemberDefinition src, IMemberDefinition linked)
		{
			return src.CustomAttributes.Where (ca => ca.AttributeType.IsTypeOf<KeptByAttribute> ()).SelectMany (keptByAttribute => VerifyKeptByAttribute (linked.FullName, keptByAttribute));
		}

		/// <summary>
		/// Validates that all <see cref="KeptByAttribute"/> instances on an attribute provider are valid (i.e. ILLink recorded a marked dependency described in the attribute)
		/// <paramref name="src"/> is the attribute provider that may have a <see cref="KeptByAttribute"/>, and <paramref name="attributeProviderFullName"/> is the 'FullName' of <paramref name="src"/>.
		/// </summary>
		IEnumerable<string> VerifyKeptByAttributes (ICustomAttributeProvider src, string attributeProviderFullName)
		{
			return src.CustomAttributes.Where (ca => ca.AttributeType.IsTypeOf<KeptByAttribute> ()).SelectMany (attr => VerifyKeptByAttribute (attributeProviderFullName, attr));
		}

		IEnumerable<string> VerifyKeptByAttribute (string keptAttributeProviderName, CustomAttribute attribute)
		{
			// public KeptByAttribute (string dependencyProvider, string reason) { }
			// public KeptByAttribute (Type dependencyProvider, string reason) { }
			// public KeptByAttribute (Type dependencyProvider, string memberName, string reason) { }

			Assert.AreEqual (nameof (KeptByAttribute), attribute.AttributeType.Name);

			// Create the expected TestDependencyRecorder.Dependency that should be in the recorded dependencies
			TestDependencyRecorder.Dependency expectedDependency = new ();
			expectedDependency.Target = keptAttributeProviderName;
			expectedDependency.Marked = true;
			if (attribute.ConstructorArguments.Count == 2) {
				// public KeptByAttribute (string dependencyProvider, string reason) { }
				// public KeptByAttribute (Type dependencyProvider, string reason) { }
				if (attribute.ConstructorArguments[0].Type.IsTypeOf<string> ())
					expectedDependency.Source = (string) attribute.ConstructorArguments[0].Value;
				else if (attribute.ConstructorArguments[0].Type.IsTypeOf<Type> ())
					expectedDependency.Source = ((TypeDefinition) attribute.ConstructorArguments[0].Value).FullName;
				else
					throw new NotImplementedException ("Unexpected KeptByAttribute ctor variant");

				expectedDependency.DependencyKind = (string) attribute.ConstructorArguments[1].Value;
			} else if (attribute.ConstructorArguments.Count == 3) {
				// public KeptByAttribute (Type dependencyProvider, string memberName, string reason) { }
				if (!attribute.ConstructorArguments[0].Type.IsTypeOf<Type> ())
					throw new NotImplementedException ("Unexpected KeptByAttribute ctor variant");
				var type = (TypeDefinition) attribute.ConstructorArguments[0].Value;
				string memberName = (string) attribute.ConstructorArguments[1].Value;
				var memberDefinition = type.AllMembers ().Where (m => m.Name == memberName).Single ();
				expectedDependency.Source = memberDefinition.FullName;
				expectedDependency.DependencyKind = (string) attribute.ConstructorArguments[2].Value;
			} else {
				throw new NotImplementedException ("Unexpected KeptByAttribute ctor variant");
			}

			foreach (var dep in this.linkedTestCase.Customizations.DependencyRecorder.Dependencies) {
				if (dep == expectedDependency) {
					yield break;
				}
			}
			yield return $"{keptAttributeProviderName} was expected to be kept by {expectedDependency.Source} with reason {expectedDependency.DependencyKind.ToString ()}.";
		}

		protected virtual IEnumerable<string> VerifyTypeDefinitionKept (TypeDefinition original, TypeDefinition linked)
		{
			if (linked == null) {
				yield return $"Type `{original}' should have been kept";
				yield break;
			}

			// Skip verification of type metadata for compiler generated types (we don't currently need it yet)
			if (!IsCompilerGeneratedMember (original)) {
				foreach (var err in VerifyKeptByAttributes (original, linked)) yield return err;
				if (!original.IsInterface)
					foreach (var err in VerifyBaseType (original, linked)) yield return err;

				foreach (var err in VerifyInterfaces (original, linked)) yield return err;
				foreach (var err in VerifyPseudoAttributes (original, linked)) yield return err;
				foreach (var err in VerifyGenericParameters (original, linked, compilerGenerated: false)) yield return err;
				foreach (var err in VerifyCustomAttributes (original, linked)) yield return err;
				foreach (var err in VerifySecurityAttributes (original, linked)) yield return err;

				foreach (var err in VerifyFixedBufferFields (original, linked)) yield return err;
			}

			// Need to check delegate cache fields before the normal field check
			foreach (var err in VerifyDelegateBackingFields (original, linked)) yield return err;
			foreach (var err in VerifyPrivateImplementationDetails (original, linked)) yield return err;

			foreach (var td in original.NestedTypes) {
				foreach (var err in VerifyTypeDefinition (td, linked?.NestedTypes.FirstOrDefault (l => td.FullName == l.FullName))) yield return err;
				linkedMembers.Remove (td.FullName);
			}

			// Need to check properties before fields so that the KeptBackingFieldAttribute is handled correctly
			foreach (var p in original.Properties) {
				foreach (var err in VerifyProperty (p, linked?.Properties.FirstOrDefault (l => p.Name == l.Name), linked)) yield return err;
				linkedMembers.Remove (p.FullName);
			}
			// Need to check events before fields so that the KeptBackingFieldAttribute is handled correctly
			foreach (var e in original.Events) {
				foreach (var err in VerifyEvent (e, linked?.Events.FirstOrDefault (l => e.Name == l.Name), linked)) yield return err;
				linkedMembers.Remove (e.FullName);
			}

			foreach (var f in original.Fields) {
				if (verifiedGeneratedFields.Contains (f.FullName))
					continue;
				foreach (var err in VerifyField (f, linked?.Fields.FirstOrDefault (l => f.Name == l.Name))) yield return err;
				linkedMembers.Remove (f.FullName);
			}

			foreach (var m in original.Methods) {
				if (verifiedEventMethods.Contains (m.FullName))
					continue;
				var msign = m.GetSignature ();
				foreach (var err in VerifyMethod (m, linked?.Methods.FirstOrDefault (l => msign == l.GetSignature ()))) yield return err;
				linkedMembers.Remove (m.FullName);
			}
		}

		IEnumerable<string> VerifyBaseType (TypeDefinition src, TypeDefinition linked)
		{
			string expectedBaseName;
			var expectedBaseGenericAttr = src.CustomAttributes.FirstOrDefault (w => w.AttributeType.Name == nameof (KeptBaseTypeAttribute) && w.ConstructorArguments.Count > 1);
			if (expectedBaseGenericAttr != null) {
				expectedBaseName = FormatBaseOrInterfaceAttributeValue (expectedBaseGenericAttr);
			} else {
				var defaultBaseType = src.IsEnum ? "System.Enum" : src.IsValueType ? "System.ValueType" : "System.Object";
				expectedBaseName = GetCustomAttributeCtorValues<object> (src, nameof (KeptBaseTypeAttribute)).FirstOrDefault ()?.ToString () ?? defaultBaseType;
			}
			if (expectedBaseName != linked.BaseType?.FullName)
				yield return $"Incorrect base type on : {linked.Name}";
		}

		IEnumerable<string> VerifyInterfaces (TypeDefinition src, TypeDefinition linked)
		{
			var expectedInterfaces = new HashSet<string> (src.CustomAttributes
				.Where (w => w.AttributeType.Name == nameof (KeptInterfaceAttribute))
				.Select (FormatBaseOrInterfaceAttributeValue));
			if (expectedInterfaces.Count == 0) {
				if (linked.HasInterfaces) {
					yield return $"Type `{src}' has unexpected interfaces";
				}
			} else {
				foreach (var iface in linked.Interfaces) {
					if (!expectedInterfaces.Remove (iface.InterfaceType.FullName)) {
						if (!expectedInterfaces.Remove (iface.InterfaceType.Resolve ().FullName)) {
							yield return $"Type `{src}' interface `{iface.InterfaceType.FullName}' should have been removed";
						}
					}
				}

				if (expectedInterfaces.Any ()) {
					yield return $"Expected interfaces were not found on {src}: {string.Join (", ", expectedInterfaces.Select(i => i.Split('.', '/').Last()))}";
				}
			}
		}

		void VerifyOverrides (MethodDefinition original, MethodDefinition linked)
		{
			if (linked is null)
				return;
			var expectedBaseTypesOverridden = new HashSet<string> (original.CustomAttributes
				.Where (ca => ca.AttributeType.Name == nameof (KeptOverrideAttribute))
				.Select (ca => (ca.ConstructorArguments[0].Value as TypeReference).FullName));
			var originalBaseTypesOverridden = new HashSet<string> (original.Overrides.Select (ov => ov.DeclaringType.FullName));
			var linkedBaseTypesOverridden = new HashSet<string> (linked.Overrides.Select (ov => ov.DeclaringType.FullName));
			foreach (var expectedBaseType in expectedBaseTypesOverridden) {
				Assert.IsTrue (originalBaseTypesOverridden.Contains (expectedBaseType),
					$"Method {linked.FullName} was expected to keep override {expectedBaseType}::{linked.Name}, " +
					 "but it wasn't in the unlinked assembly");
				Assert.IsTrue (linkedBaseTypesOverridden.Contains (expectedBaseType),
					$"Method {linked.FullName} was expected to override {expectedBaseType}::{linked.Name}");
			}

			var expectedBaseTypesNotOverridden = new HashSet<string> (original.CustomAttributes
				.Where (ca => ca.AttributeType.Name == nameof (RemovedOverrideAttribute))
				.Select (ca => (ca.ConstructorArguments[0].Value as TypeReference).FullName));
			foreach (var expectedRemovedBaseType in expectedBaseTypesNotOverridden) {
				Assert.IsTrue (originalBaseTypesOverridden.Contains (expectedRemovedBaseType),
					$"Method {linked.FullName} was expected to remove override {expectedRemovedBaseType}::{linked.Name}, " +
					$"but it wasn't in the unlinked assembly");
				Assert.IsFalse (linkedBaseTypesOverridden.Contains (expectedRemovedBaseType),
					$"Method {linked.FullName} was expected to not override {expectedRemovedBaseType}::{linked.Name}");
			}

			foreach (var overriddenMethod in linked.Overrides) {
				if (overriddenMethod.Resolve () is not MethodDefinition overriddenDefinition) {
					Assert.Fail ($"Method {linked.GetDisplayName ()} overrides method {overriddenMethod} which does not exist");
				} else if (overriddenDefinition.DeclaringType.IsInterface) {
					Assert.True (linked.DeclaringType.Interfaces.Select (i => i.InterfaceType).Contains (overriddenMethod.DeclaringType),
						$"Method {linked} overrides method {overriddenMethod}, but {linked.DeclaringType} does not implement interface {overriddenMethod.DeclaringType}");
				} else {
					TypeDefinition baseType = linked.DeclaringType;
					TypeReference overriddenType = overriddenMethod.DeclaringType;
					while (baseType is not null) {
						if (baseType.Equals (overriddenType))
							break;
						if (baseType.Resolve ()?.BaseType is null)
							Assert.Fail ($"Method {linked} overrides method {overriddenMethod} from, but {linked.DeclaringType} does not inherit from type {overriddenMethod.DeclaringType}");
					}
				}
			}
		}

		static string FormatBaseOrInterfaceAttributeValue (CustomAttribute attr)
		{
			if (attr.ConstructorArguments.Count == 1)
				return attr.ConstructorArguments[0].Value.ToString ();

			StringBuilder builder = new StringBuilder ();
			builder.Append (attr.ConstructorArguments[0].Value);
			builder.Append ("<");
			bool separator = false;
			foreach (var caa in (CustomAttributeArgument[]) attr.ConstructorArguments[1].Value) {
				if (separator)
					builder.Append (",");
				else
					separator = true;

				var arg = (CustomAttributeArgument) caa.Value;
				builder.Append (arg.Value);
			}

			builder.Append (">");
			return builder.ToString ();
		}

		IEnumerable<string> VerifyField (FieldDefinition src, FieldDefinition linked)
		{
			bool compilerGenerated = IsCompilerGeneratedMember (src);
			bool expectedKept = ShouldBeKept (src) ||
				(compilerGenerated ? !IsBackingField (src) : false);

			if (!expectedKept) {
				if (linked != null)
					yield return $"Field `{src}' should have been removed";

				yield break;
			}

			foreach (var err in VerifyFieldKept (src, linked, compilerGenerated)) yield return err;
		}

		IEnumerable<string> VerifyFieldKept (FieldDefinition src, FieldDefinition linked, bool compilerGenerated)
		{
			if (linked == null) {
				yield return $"Field `{src}' should have been kept";
				yield break;
			}

			if (!src?.Constant?.Equals (linked?.Constant) == true)
				yield return $"Field `{src}' value was expected to be {src?.Constant} but was {linked?.Constant}";

			foreach (var err in VerifyKeptByAttributes (src, linked)) yield return err;
			VerifyPseudoAttributes (src, linked);
			if (!compilerGenerated)
				foreach (var err in VerifyCustomAttributes (src, linked)) yield return err;
		}

		IEnumerable<string> VerifyProperty (PropertyDefinition src, PropertyDefinition linked, TypeDefinition linkedType)
		{
			foreach (var err in VerifyMemberBackingField (src, linkedType)) yield return err;

			bool compilerGenerated = IsCompilerGeneratedMember (src);
			bool expectedKept = ShouldBeKept (src) || compilerGenerated;

			if (!expectedKept) {
				if (linked != null)
					yield return $"Property `{src}' should have been removed";

				yield break;
			}

			if (linked == null) {
				yield return $"Property `{src}' should have been kept";
				yield break;
			}

			if (src?.Constant != linked?.Constant)
				yield return $"Property `{src}' value";

			foreach (var err in VerifyKeptByAttributes (src, linked)) yield return err;
			foreach (var err in VerifyPseudoAttributes (src, linked)) yield return err;
			if (!compilerGenerated)
				foreach (var err in VerifyCustomAttributes (src, linked)) yield return err;
		}

		IEnumerable<string> VerifyEvent (EventDefinition src, EventDefinition linked, TypeDefinition linkedType)
		{
			foreach (var err in VerifyMemberBackingField (src, linkedType)) yield return err;

			bool compilerGenerated = IsCompilerGeneratedMember (src);
			bool expectedKept = ShouldBeKept (src) || compilerGenerated;

			if (!expectedKept) {
				if (linked != null)
					yield return $"Event `{src}' should have been removed";

				yield break;
			}

			if (linked == null) {
				yield return $"Event `{src}' should have been kept";
				yield break;
			}

			if (src.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (KeptEventAddMethodAttribute))) {
				foreach (var err in VerifyMethodInternal (src.AddMethod, linked.AddMethod, true, compilerGenerated)) yield return err;
				verifiedEventMethods.Add (src.AddMethod.FullName);
				linkedMembers.Remove (src.AddMethod.FullName);
			}

			if (src.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (KeptEventRemoveMethodAttribute))) {
				foreach (var err in VerifyMethodInternal (src.RemoveMethod, linked.RemoveMethod, true, compilerGenerated)) yield return err;
				verifiedEventMethods.Add (src.RemoveMethod.FullName);
				linkedMembers.Remove (src.RemoveMethod.FullName);
			}

			foreach (var err in VerifyKeptByAttributes (src, linked)) yield return err;
			foreach (var err in VerifyPseudoAttributes (src, linked)) yield return err;
			if (!compilerGenerated)
				foreach (var err in VerifyCustomAttributes (src, linked)) yield return err;
		}

		IEnumerable<string> VerifyMethod (MethodDefinition src, MethodDefinition linked)
		{
			bool compilerGenerated = IsCompilerGeneratedMember (src);
			bool expectedKept = ShouldMethodBeKept (src);
			foreach (var err in VerifyMethodInternal (src, linked, expectedKept, compilerGenerated)) yield return err;
		}

		IEnumerable<string> VerifyMethodInternal (MethodDefinition src, MethodDefinition linked, bool expectedKept, bool compilerGenerated)
		{
			if (!expectedKept) {
				if (linked == null)
					yield break;

				// Similar to comment on types, compiler-generated methods can't be annotated with Kept attribute directly
				// so we're not going to validate kept/remove on them. Note that we're still going to go validate "into" them
				// to check for other properties (like parameter name presence/removal for example)
				if (!compilerGenerated)
					yield return $"Method `{src.FullName}' should have been removed";
			}

			foreach (var err in VerifyMethodKept (src, linked, compilerGenerated)) yield return err;
		}

		IEnumerable<string> VerifyMemberBackingField (IMemberDefinition src, TypeDefinition linkedType)
		{
			var keptBackingFieldAttribute = src.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.Name == nameof (KeptBackingFieldAttribute));
			if (keptBackingFieldAttribute == null)
				yield break;

			var backingFieldName = src.MetadataToken.TokenType == TokenType.Property
				? $"<{src.Name}>k__BackingField" : src.Name;
			var srcField = src.DeclaringType.Fields.FirstOrDefault (f => f.Name == backingFieldName);

			if (srcField == null) {
				// Can add more here if necessary
				backingFieldName = backingFieldName.Replace ("System.Int32", "int");
				backingFieldName = backingFieldName.Replace ("System.String", "string");
				backingFieldName = backingFieldName.Replace ("System.Char", "char");

				srcField = src.DeclaringType.Fields.FirstOrDefault (f => f.Name == backingFieldName);
			}

			if (srcField == null) {
				yield return $"{src.MetadataToken.TokenType} `{src}', could not locate the expected backing field {backingFieldName}";
				yield break;
			}

			foreach (var err in VerifyFieldKept (srcField, linkedType?.Fields.FirstOrDefault (l => srcField.Name == l.Name), compilerGenerated: true)) yield return err;
			verifiedGeneratedFields.Add (srcField.FullName);
			linkedMembers.Remove (srcField.FullName);
		}

		protected virtual IEnumerable<string> VerifyMethodKept (MethodDefinition src, MethodDefinition linked, bool compilerGenerated)
		{
			if (linked == null) {
				yield return $"Method `{src.FullName}' should have been kept";
				yield break;
			}

			foreach (var err in VerifyPseudoAttributes (src, linked)) yield return err;
			foreach (var err in VerifyGenericParameters (src, linked, compilerGenerated)) yield return err;
			if (!compilerGenerated) {
				foreach (var err in VerifyCustomAttributes (src, linked)) yield return err;
				foreach (var err in VerifyCustomAttributes (src.MethodReturnType, linked.MethodReturnType)) yield return err;

			}
			foreach (var err in VerifyParameters (src, linked, compilerGenerated)) yield return err;
			foreach (var err in VerifySecurityAttributes (src, linked)) yield return err;
			foreach (var err in VerifyArrayInitializers (src, linked)) yield return err;
			foreach (var err in VerifyMethodBody (src, linked)) yield return err;
			foreach (var err in VerifyKeptByAttributes (src, linked)) yield return err;
		}

		protected virtual IEnumerable<string> VerifyMethodBody (MethodDefinition src, MethodDefinition linked)
		{
			if (!src.HasBody)
				yield break;

			foreach (var err in VerifyInstructions (src, linked)) yield return err;
			foreach (var err in VerifyLocals (src, linked)) yield return err;
		}

		protected static IEnumerable<string> VerifyInstructions (MethodDefinition src, MethodDefinition linked)
		{
			foreach (var err in VerifyBodyProperties (
				src,
				linked,
				nameof (ExpectedInstructionSequenceAttribute),
				nameof (ExpectBodyModifiedAttribute),
				"instructions",
				m => FormatMethodBody (m.Body),
				attr => GetStringArrayAttributeValue (attr).ToArray ())) yield return err;
		}

		public static string[] FormatMethodBody (MethodBody body)
		{
			List<(Instruction, string)> result = new List<(Instruction, string)> (body.Instructions.Count);
			for (int index = 0; index < body.Instructions.Count; index++) {
				var instruction = body.Instructions[index];
				result.Add ((instruction, FormatInstruction (instruction)));
			}

			HashSet<(Instruction, Instruction)> existingTryBlocks = new HashSet<(Instruction, Instruction)> ();
			foreach (var exHandler in body.ExceptionHandlers) {
				if (existingTryBlocks.Add ((exHandler.TryStart, exHandler.TryEnd))) {
					InsertBeforeInstruction (exHandler.TryStart, ".try");
					if (exHandler.TryEnd != null)
						InsertBeforeInstruction (exHandler.TryEnd, ".endtry");
					else
						Append (".endtry");
				}

				if (exHandler.HandlerStart != null)
					InsertBeforeInstruction (exHandler.HandlerStart, ".catch");

				if (exHandler.HandlerEnd != null)
					InsertBeforeInstruction (exHandler.HandlerEnd, ".endcatch");
				else
					Append (".endcatch");

				if (exHandler.FilterStart != null)
					InsertBeforeInstruction (exHandler.FilterStart, ".filter");
			}

			return result.Select (i => i.Item2).ToArray ();

			void InsertBeforeInstruction (Instruction instruction, string text) =>
				result.Insert (result.FindIndex (i => i.Item1 == instruction), (null, text));

			void Append (string text) =>
				result.Add ((null, text));
		}

		static string FormatInstruction (Instruction instr)
		{
			switch (instr.OpCode.FlowControl) {
			case FlowControl.Branch:
			case FlowControl.Cond_Branch:
				if (instr.Operand is Instruction target)
					return $"{instr.OpCode.ToString ()} il_{target.Offset.ToString ("x")}";

				if (instr.Operand is Instruction[] targets) {
					string stargets = string.Join (", ", targets.Select (l => $"il_{l.Offset.ToString ("x")}"));
					return $"{instr.OpCode.ToString ()} ({stargets})";
				}

				break;
			}

			switch (instr.OpCode.Code) {
			case Code.Ldc_I4:
				if (instr.Operand is int ivalue)
					return $"{instr.OpCode.ToString ()} 0x{ivalue.ToString ("x")}";

				throw new NotImplementedException (instr.Operand.GetType ().ToString ());
			case Code.Ldc_I4_S:
				if (instr.Operand is sbyte bvalue)
					return $"{instr.OpCode.ToString ()} 0x{bvalue.ToString ("x")}";

				throw new NotImplementedException (instr.Operand.GetType ().ToString ());
			case Code.Ldc_I8:
				if (instr.Operand is long lvalue)
					return $"{instr.OpCode.ToString ()} 0x{lvalue.ToString ("x")}";

				throw new NotImplementedException (instr.Operand.GetType ().ToString ());

			case Code.Ldc_R4:
				if (instr.Operand is float fvalue)
					return $"{instr.OpCode.ToString ()} {fvalue.ToString (CultureInfo.InvariantCulture)}";

				throw new NotImplementedException (instr.Operand.GetType ().ToString ());

			case Code.Ldc_R8:
				if (instr.Operand is double dvalue)
					return $"{instr.OpCode.ToString ()} {dvalue.ToString (CultureInfo.InvariantCulture)}";

				throw new NotImplementedException (instr.Operand.GetType ().ToString ());

			case Code.Ldstr:
				if (instr.Operand is string svalue)
					return $"{instr.OpCode.ToString ()} '{svalue}'";

				throw new NotImplementedException (instr.Operand.GetType ().ToString ());

			default: {
					string operandString = null;
					switch (instr.OpCode.OperandType) {
					case OperandType.InlineField:
					case OperandType.InlineMethod:
					case OperandType.InlineType:
					case OperandType.InlineTok:
						operandString = instr.Operand switch {
							FieldReference fieldRef => fieldRef.FullName,
							MethodReference methodRef => methodRef.FullName,
							TypeReference typeRef => typeRef.FullName,
							_ => null
						};
						break;
					}

					if (operandString != null)
						return $"{instr.OpCode.ToString ()} {operandString}";
					else
						return instr.OpCode.ToString ();
				}
			}
		}

		static IEnumerable<string> VerifyLocals (MethodDefinition src, MethodDefinition linked)
		{
			foreach (var err in VerifyBodyProperties (
				src,
				linked,
				nameof (ExpectedLocalsSequenceAttribute),
				nameof (ExpectLocalsModifiedAttribute),
				"locals",
				m => m.Body.Variables.Select (v => v.VariableType.ToString ()).ToArray (),
				attr => GetStringOrTypeArrayAttributeValue (attr).ToArray ())) yield return err;
		}

		public static IEnumerable<string> VerifyBodyProperties (MethodDefinition src, MethodDefinition linked, string sequenceAttributeName, string expectModifiedAttributeName,
			string propertyDescription,
			Func<MethodDefinition, string[]> valueCollector,
			Func<CustomAttribute, string[]> getExpectFromSequenceAttribute)
		{
			var expectedSequenceAttribute = src.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.Name == sequenceAttributeName);
			var linkedValues = valueCollector (linked);
			var srcValues = valueCollector (src);

			if (src.CustomAttributes.Any (attr => attr.AttributeType.Name == expectModifiedAttributeName)) {
				if (linkedValues.ToHashSet ().SetEquals (srcValues.ToHashSet ())) {
					yield return $"Expected method `{src} to have it's {propertyDescription} modified, however, the {propertyDescription} were the same as the original\n{FormattingUtils.FormatSequenceCompareFailureMessage (linkedValues, srcValues)}";
				}
			} else if (expectedSequenceAttribute != null) {
				var expected = getExpectFromSequenceAttribute (expectedSequenceAttribute).ToArray ();
				if (!linkedValues.ToHashSet ().SetEquals (expected.ToHashSet ())) {
					yield return $"Expected method `{src} to have it's {propertyDescription} modified, however, the sequence of {propertyDescription} does not match the expected value\n{FormattingUtils.FormatSequenceCompareFailureMessage2 (linkedValues, expected, srcValues)}";
				}
			} else {
				if (!linkedValues.ToHashSet ().SetEquals (srcValues.ToHashSet ())) {
					yield return $"Expected method `{src} to have it's {propertyDescription} unchanged, however, the {propertyDescription} differ from the original\n{FormattingUtils.FormatSequenceCompareFailureMessage (linkedValues, srcValues)}";
				}
			}
		}

		IEnumerable<string> VerifyReferences (AssemblyDefinition original, AssemblyDefinition linked)
		{
			var expected = original.MainModule.AllDefinedTypes ()
				.SelectMany (t => GetCustomAttributeCtorValues<string> (t, nameof (KeptReferenceAttribute)))
				.Select (ReduceAssemblyFileNameOrNameToNameOnly)
				.ToHashSet ();

			/*
			 - The test case will always need to have at least 1 reference.
			 - Forcing all tests to define their expected references seems tedious

			 Given the above, let's assume that when no [KeptReference] attributes are present,
			 the test case does not want to make any assertions regarding references.

			 Once 1 kept reference attribute is used, the test will need to define all of of it's expected references
			*/
			if (expected.Count == 0)
				yield break;

			var actual = linked.MainModule.AssemblyReferences
				.Select (name => name.Name)
				.ToHashSet ();

			if (!expected.SetEquals (actual))
				yield return $"Expected references `{string.Join (", ", expected)}` do not match actual references `{string.Join (", ", actual)}`";
		}

		string ReduceAssemblyFileNameOrNameToNameOnly (string fileNameOrAssemblyName)
		{
			if (fileNameOrAssemblyName.EndsWith (".dll") || fileNameOrAssemblyName.EndsWith (".exe") || fileNameOrAssemblyName.EndsWith (".winmd"))
				return System.IO.Path.GetFileNameWithoutExtension (fileNameOrAssemblyName);

			// It must already be just the assembly name
			return fileNameOrAssemblyName;
		}

		IEnumerable<string> VerifyResources (AssemblyDefinition original, AssemblyDefinition linked)
		{
			var expectedResourceNames = original.MainModule.AllDefinedTypes ()
				.SelectMany (t => GetCustomAttributeCtorValues<string> (t, nameof (KeptResourceAttribute)))
				.ToList ();

			foreach (var resource in linked.MainModule.Resources) {
				if (!expectedResourceNames.Remove (resource.Name))
					yield return $"Resource '{resource.Name}' should be removed.";

				EmbeddedResource embeddedResource = (EmbeddedResource) resource;

				var expectedResource = (EmbeddedResource) original.MainModule.Resources.First (r => r.Name == resource.Name);

				if (!embeddedResource.GetResourceData ().SequenceEqual (expectedResource.GetResourceData ()))
					yield return $"Resource '{resource.Name}' data doesn't match.";
			}

			if (expectedResourceNames.Any ()) yield return $"Resource '{expectedResourceNames.FirstOrDefault ()}' should be kept.";
		}

		IEnumerable<string> VerifyExportedTypes (AssemblyDefinition original, AssemblyDefinition linked)
		{
			var expectedTypes = original.MainModule.AllDefinedTypes ()
				.SelectMany (t => GetCustomAttributeCtorValues<TypeReference> (t, nameof (KeptExportedTypeAttribute)).Select (l => l.FullName));

			if (!linked.MainModule.ExportedTypes.Select (l => l.FullName).ToHashSet ().SetEquals (expectedTypes.ToHashSet ()))
				yield return $"Exported types do not match expected.";
		}

		protected virtual IEnumerable<string> VerifyPseudoAttributes (MethodDefinition src, MethodDefinition linked)
		{
			var expected = (MethodAttributes) GetExpectedPseudoAttributeValue (src, (uint) src.Attributes);
			if (expected != linked.Attributes)
				yield return $"Method `{src}' pseudo attributes did not match expected";
		}

		protected virtual IEnumerable<string> VerifyPseudoAttributes (TypeDefinition src, TypeDefinition linked)
		{
			var expected = (TypeAttributes) GetExpectedPseudoAttributeValue (src, (uint) src.Attributes);
			if (expected == linked.Attributes)
				yield break;

			yield return $"Type `{src}' pseudo attributes did not match expected";
		}

		protected virtual IEnumerable<string> VerifyPseudoAttributes (FieldDefinition src, FieldDefinition linked)
		{
			var expected = (FieldAttributes) GetExpectedPseudoAttributeValue (src, (uint) src.Attributes);
			if (expected != linked.Attributes) yield return $"Field `{src}' pseudo attributes did not match expected";
		}

		protected virtual IEnumerable<string> VerifyPseudoAttributes (PropertyDefinition src, PropertyDefinition linked)
		{
			var expected = (PropertyAttributes) GetExpectedPseudoAttributeValue (src, (uint) src.Attributes);
			if (expected != linked.Attributes) yield return $"Property `{src}' pseudo attributes did not match expected";
		}

		protected virtual IEnumerable<string> VerifyPseudoAttributes (EventDefinition src, EventDefinition linked)
		{
			var expected = (EventAttributes) GetExpectedPseudoAttributeValue (src, (uint) src.Attributes);
			if (expected != linked.Attributes) yield return $"Event `{src}' pseudo attributes did not match expected";
		}

		protected virtual IEnumerable<string> VerifyCustomAttributes (ICustomAttributeProvider src, ICustomAttributeProvider linked)
		{
			var expectedAttrs = GetExpectedAttributes (src).ToHashSet ();
			var linkedAttrs = FilterLinkedAttributes (linked).ToHashSet ();
			if (!linkedAttrs.SetEquals (expectedAttrs)) {
				var missing = $"Missing: {string.Join (", ", expectedAttrs.Except (linkedAttrs))}";
				var extra = $"Extra: {string.Join (", ", linkedAttrs.Except (expectedAttrs))}";

				yield return string.Join (Environment.NewLine, $"Custom attributes on `{src}' are not matching:", missing, extra);
			}
		}

		protected virtual IEnumerable<string> VerifySecurityAttributes (ICustomAttributeProvider src, ISecurityDeclarationProvider linked)
		{
			var expectedAttrs = GetCustomAttributeCtorValues<object> (src, nameof (KeptSecurityAttribute))
				.Select (attr => attr.ToString ())
				.ToHashSet ();

			var linkedAttrs = FilterLinkedSecurityAttributes (linked).ToHashSet ();

			if (!linkedAttrs.SetEquals (expectedAttrs)) {
				var missing = $"Missing: {string.Join (", ", expectedAttrs.Except (linkedAttrs))}";
				var extra = $"Extra: {string.Join (", ", linkedAttrs.Except (expectedAttrs))}";
				yield return string.Join ($"Security attributes on `{src}' are not matching:", missing, extra);
			}
		}

		IEnumerable<string> VerifyPrivateImplementationDetails (TypeDefinition original, TypeDefinition linked)
		{
			var expectedImplementationDetailsMethods = GetCustomAttributeCtorValues<string> (original, nameof (KeptPrivateImplementationDetailsAttribute))
				.Select (attr => attr.ToString ())
				.ToList ();

			if (expectedImplementationDetailsMethods.Count == 0)
				yield break;

			TypeDefinition srcImplementationDetails;
			TypeDefinition linkedImplementationDetails;
			foreach (var err in VerifyPrivateImplementationDetailsType (original.Module, linked.Module, out srcImplementationDetails, out linkedImplementationDetails)) yield return err;
			foreach (var methodName in expectedImplementationDetailsMethods) {
				var originalMethod = srcImplementationDetails.Methods.FirstOrDefault (m => m.Name == methodName);
				if (originalMethod == null)
					yield return $"Could not locate original private implementation details method {methodName}";

				var linkedMethod = linkedImplementationDetails.Methods.FirstOrDefault (m => m.Name == methodName);
				foreach (var erro in VerifyMethodKept (originalMethod, linkedMethod, compilerGenerated: true)) yield return erro;
				linkedMembers.Remove (linkedMethod.FullName);
			}
			verifiedGeneratedTypes.Add (srcImplementationDetails.FullName);
		}

		static IEnumerable<string> VerifyPrivateImplementationDetailsType (ModuleDefinition src, ModuleDefinition linked, out TypeDefinition srcImplementationDetails, out TypeDefinition linkedImplementationDetails)
		{
			srcImplementationDetails = src.Types.FirstOrDefault (t => IsPrivateImplementationDetailsType (t));

			linkedImplementationDetails = linked.Types.FirstOrDefault (t => IsPrivateImplementationDetailsType (t));
			const string srcMissingMessage = "Could not locate <PrivateImplementationDetails> in the original assembly. Does your test use initializers?";
			const string linkedMissingMessage = "Could not locate <PrivateImplementationDetails> in the linked assembly";
			return (srcImplementationDetails, linkedImplementationDetails) switch {
				(null, null) => [srcMissingMessage, linkedMissingMessage],
				(null, _) => [srcMissingMessage],
				(_, null) => [linkedMissingMessage],
				_ => Enumerable.Empty<string> ()
			};
		}

		protected virtual IEnumerable<string> VerifyArrayInitializers (MethodDefinition src, MethodDefinition linked)
		{
			var expectedIndices = GetCustomAttributeCtorValues<object> (src, nameof (KeptInitializerData))
				.Cast<int> ()
				.ToArray ();

			var expectKeptAll = src.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (KeptInitializerData) && !attr.HasConstructorArguments);

			if (expectedIndices.Length == 0 && !expectKeptAll)
				yield break;

			if (!src.HasBody)
				yield return $"`{nameof (KeptInitializerData)}` cannot be used on methods that don't have bodies";
			TypeDefinition srcImplementationDetails;
			TypeDefinition linkedImplementationDetails;
			foreach (var err in VerifyPrivateImplementationDetailsType (src.Module, linked.Module, out srcImplementationDetails, out linkedImplementationDetails)) yield return err;

			var possibleInitializerFields = src.Body.Instructions
				.Where (ins => IsLdtokenOnPrivateImplementationDetails (srcImplementationDetails, ins))
				.Select (ins => ((FieldReference) ins.Operand).Resolve ())
				.ToArray ();

			if (possibleInitializerFields.Length == 0)
				yield return $"`{src}` does not make use of any initializers";

			if (expectKeptAll) {
				foreach (var srcField in possibleInitializerFields) {
					var linkedField = linkedImplementationDetails.Fields.FirstOrDefault (f => f.InitialValue.SequenceEqual (srcField.InitialValue));
					foreach (var err in VerifyInitializerField (srcField, linkedField)) yield return err;
				}
			} else {
				foreach (var index in expectedIndices) {
					if (index < 0 || index > possibleInitializerFields.Length)
						yield return $"Invalid expected index `{index}` in {src}.  Value must be between 0 and {expectedIndices.Length}";

					var srcField = possibleInitializerFields[index];
					var linkedField = linkedImplementationDetails.Fields.FirstOrDefault (f => f.InitialValue.SequenceEqual (srcField.InitialValue));

					foreach (var err in VerifyInitializerField (srcField, linkedField)) yield return err;
				}
			}
		}

		IEnumerable<string> VerifyInitializerField (FieldDefinition src, FieldDefinition linked)
		{
			foreach (var err in VerifyFieldKept (src, linked, compilerGenerated: true)) yield return err;
			verifiedGeneratedFields.Add (linked.FullName);
			linkedMembers.Remove (linked.FullName);
			foreach (var err in VerifyTypeDefinitionKept (src.FieldType.Resolve (), linked.FieldType.Resolve ())) yield return err;
			linkedMembers.Remove (linked.FieldType.FullName);
			linkedMembers.Remove (linked.DeclaringType.FullName);
			verifiedGeneratedTypes.Add (linked.DeclaringType.FullName);
		}

		static bool IsLdtokenOnPrivateImplementationDetails (TypeDefinition privateImplementationDetails, Instruction instruction)
		{
			if (instruction.OpCode.Code == Code.Ldtoken && instruction.Operand is FieldReference field) {
				return field.DeclaringType.Resolve () == privateImplementationDetails;
			}

			return false;
		}

		protected static IEnumerable<string> GetExpectedAttributes (ICustomAttributeProvider original)
		{
			foreach (var expectedAttrs in GetCustomAttributeCtorValues<object> (original, nameof (KeptAttributeAttribute)))
				yield return expectedAttrs.ToString ();

			// The name of the generated fixed buffer type is a little tricky.
			// Some versions of csc name it `<fieldname>e__FixedBuffer0`
			// while mcs and other versions of csc name it `<fieldname>__FixedBuffer0`
			if (original is TypeDefinition srcDefinition && srcDefinition.Name.Contains ("__FixedBuffer")) {
				var name = srcDefinition.Name.Substring (1, srcDefinition.Name.IndexOf ('>') - 1);
				var fixedField = srcDefinition.DeclaringType.Fields.FirstOrDefault (f => f.Name == name);
				if (fixedField == null)
					Assert.Fail ($"Could not locate original fixed field for {srcDefinition}");

				foreach (var additionalExpectedAttributesFromFixedField in GetCustomAttributeCtorValues<object> (fixedField, nameof (KeptAttributeOnFixedBufferTypeAttribute)))
					yield return additionalExpectedAttributesFromFixedField.ToString ();
			}
		}

		/// <summary>
		/// Filters out some attributes that should not be taken into consideration when checking the linked result against the expected result
		/// </summary>
		/// <param name="linked"></param>
		/// <returns></returns>
		protected virtual IEnumerable<string> FilterLinkedAttributes (ICustomAttributeProvider linked)
		{
			foreach (var attr in linked.CustomAttributes) {
				switch (attr.AttributeType.FullName) {
				case "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute":
				case "System.Runtime.CompilerServices.CompilerGeneratedAttribute":
				case "System.Runtime.CompilerServices.IsReadOnlyAttribute":
				case "System.Runtime.CompilerServices.RefSafetyRulesAttribute":
					continue;

				// When mcs is used to compile the test cases, backing fields end up with this attribute on them
				case "System.Diagnostics.DebuggerBrowsableAttribute":
					continue;

				// When compiling with roslyn, assemblies get the DebuggableAttribute by default.
				case "System.Diagnostics.DebuggableAttribute":
					continue;

				case "System.Runtime.CompilerServices.CompilationRelaxationsAttribute":
					if (linked is AssemblyDefinition)
						continue;
					break;
				}

				yield return attr.AttributeType.FullName;
			}
		}

		protected virtual IEnumerable<string> FilterLinkedSecurityAttributes (ISecurityDeclarationProvider linked)
		{
			return linked.SecurityDeclarations
				.SelectMany (d => d.SecurityAttributes)
				.Select (attr => attr.AttributeType.ToString ());
		}

		IEnumerable<string> VerifyFixedBufferFields (TypeDefinition src, TypeDefinition linked)
		{
			var fields = src.Fields.Where (f => f.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (KeptFixedBufferAttribute)));

			foreach (var field in fields) {
				// The name of the generated fixed buffer type is a little tricky.
				// Some versions of csc name it `<fieldname>e__FixedBuffer0`
				// while mcs and other versions of csc name it `<fieldname>__FixedBuffer0`
				var originalCompilerGeneratedBufferType = src.NestedTypes.FirstOrDefault (t => t.FullName.Contains ($"<{field.Name}>") && t.FullName.Contains ("__FixedBuffer"));
				if (originalCompilerGeneratedBufferType == null)
					yield return $"Could not locate original compiler generated fixed buffer type for field {field}";

				var linkedCompilerGeneratedBufferType = linked.NestedTypes.FirstOrDefault (t => t.Name == originalCompilerGeneratedBufferType.Name);
				if (linkedCompilerGeneratedBufferType == null)
					yield return $"Missing expected type {originalCompilerGeneratedBufferType}";

				// Have to verify the field before the type
				var originalElementField = originalCompilerGeneratedBufferType.Fields.FirstOrDefault ();
				if (originalElementField == null)
					yield return $"Could not locate original compiler generated FixedElementField on {originalCompilerGeneratedBufferType}";

				var linkedField = linkedCompilerGeneratedBufferType?.Fields.FirstOrDefault ();
				foreach (var err in VerifyFieldKept (originalElementField, linkedField, compilerGenerated: true)) yield return err;
				verifiedGeneratedFields.Add (originalElementField.FullName);
				linkedMembers.Remove (linkedField.FullName);

				foreach (var err in VerifyTypeDefinitionKept (originalCompilerGeneratedBufferType, linkedCompilerGeneratedBufferType)) yield return err;
				verifiedGeneratedTypes.Add (originalCompilerGeneratedBufferType.FullName);
			}
		}

		IEnumerable<string> VerifyDelegateBackingFields (TypeDefinition src, TypeDefinition linked)
		{
			var expectedFieldNames = src.CustomAttributes
				.Where (a => a.AttributeType.Name == nameof (KeptDelegateCacheFieldAttribute))
				.Select (a => (a.ConstructorArguments[0].Value as string, a.ConstructorArguments[1].Value as string))
				.Select (indexAndField => $"<{indexAndField.Item1}>__{indexAndField.Item2}")
				.ToList ();

			if (expectedFieldNames.Count == 0)
				yield break;

			foreach (var nestedType in src.NestedTypes) {
				if (!IsDelegateBackingFieldsType (nestedType))
					continue;

				var linkedNestedType = linked.NestedTypes.FirstOrDefault (t => t.Name == nestedType.Name);
				foreach (var expectedFieldName in expectedFieldNames) {
					var originalField = nestedType.Fields.FirstOrDefault (f => f.Name == expectedFieldName);
					if (originalField is null)
						yield return $"Invalid expected delegate backing field {expectedFieldName} in {src}. This member was not in the unlinked assembly";

					var linkedField = linkedNestedType?.Fields.FirstOrDefault (f => f.Name == expectedFieldName);
					foreach (var err in VerifyFieldKept (originalField, linkedField, compilerGenerated: true)) yield return err;
					verifiedGeneratedFields.Add (linkedField.FullName);
					linkedMembers.Remove (linkedField.FullName);
				}

				foreach (var err in VerifyTypeDefinitionKept (nestedType, linkedNestedType)) yield return err;
				verifiedGeneratedTypes.Add (linkedNestedType.FullName);
			}
		}

		IEnumerable<string> VerifyGenericParameters (IGenericParameterProvider src, IGenericParameterProvider linked, bool compilerGenerated)
		{
			Assert.AreEqual (src.HasGenericParameters, linked.HasGenericParameters);
			if (src.HasGenericParameters) {
				for (int i = 0; i < src.GenericParameters.Count; ++i) {
					// TODO: Verify constraints
					var srcp = src.GenericParameters[i];
					var lnkp = linked.GenericParameters[i];

					if (!compilerGenerated) {
						foreach (var err in VerifyCustomAttributes (srcp, lnkp)) yield return err;
					}

					if (checkNames) {
						if (srcp.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (RemovedNameValueAttribute))) {
							string name = (src.GenericParameterType == GenericParameterType.Method ? "!!" : "!") + srcp.Position;
							if (name != lnkp.Name) {
								yield return $"Expected empty generic parameter name. Parameter {i} of {(src.ToString ())}";
							}
						} else {
							if (srcp.Name != lnkp.Name) {
								yield return $"Mismatch in generic parameter name. Parameter {i} of {(src.ToString ())}";
							}
						}
					}
				}
			}
		}

		IEnumerable<string> VerifyParameters (IMethodSignature src, IMethodSignature linked, bool compilerGenerated)
		{
			if (src.HasParameters != linked.HasParameters)
				yield return $"Mismatch in parameters. {src} has parameters: {src.HasParameters}, {linked} has parameters: {linked.HasParameters}";
			if (src.HasParameters) {
				for (int i = 0; i < src.Parameters.Count; ++i) {
					var srcp = src.Parameters[i];
					var lnkp = linked.Parameters[i];

					if (!compilerGenerated) {
						foreach (var err in VerifyCustomAttributes (srcp, lnkp)) yield return err;
					}

					if (checkNames) {
						if (srcp.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (RemovedNameValueAttribute))) {
							if (lnkp.Name != string.Empty) yield return $"Expected empty parameter name. Parameter {i} of {(src as MethodDefinition)}";
						} else {
							if (srcp.Name != lnkp.Name) yield return $"Mismatch in parameter name. Parameter {i} of {(src as MethodDefinition)}";
						}
					}
				}
			}
		}

		protected virtual bool ShouldMethodBeKept (MethodDefinition method)
		{
			var srcSignature = method.GetSignature ();
			return ShouldBeKept (method, srcSignature) || method.DeclaringType.Module.EntryPoint == method;
		}

		protected virtual bool ShouldBeKept<T> (T member, string signature = null) where T : MemberReference, ICustomAttributeProvider
		{
			if (HasActiveKeptAttribute (member) || member.HasAttribute (nameof (KeptByAttribute)))
				return true;

			ICustomAttributeProvider cap = (ICustomAttributeProvider) member.DeclaringType;
			if (cap == null)
				return false;

			return GetActiveKeptAttributes (cap, nameof (KeptMemberAttribute)).Any (ca => {
				if (ca.Constructor.Parameters.Count != 1 ||
					ca.ConstructorArguments[0].Value is not string a)
					return false;

				return a == (signature ?? member.Name);
			});
		}

		private static IEnumerable<CustomAttribute> GetActiveKeptAttributes (ICustomAttributeProvider provider, string attributeName)
		{
			return provider.CustomAttributes.Where (ca => {
				if (ca.AttributeType.Name != attributeName) {
					return false;
				}

				object keptBy = ca.GetPropertyValue (nameof (KeptAttribute.By));
				return keptBy is null ? true : ((Tool) keptBy).HasFlag (Tool.Trimmer);
			});
		}

		private static bool HasActiveKeptAttribute (ICustomAttributeProvider provider)
		{
			return GetActiveKeptAttributes (provider, nameof (KeptAttribute)).Any ();
		}

		private static IEnumerable<CustomAttribute> GetActiveKeptDerivedAttributes (ICustomAttributeProvider provider)
		{
			return provider.CustomAttributes.Where (ca => {
				if (!ca.AttributeType.Resolve ().DerivesFrom (nameof (KeptAttribute))) {
					return false;
				}

				object keptBy = ca.GetPropertyValue (nameof (KeptAttribute.By));
				return keptBy is null ? true : ((Tool) keptBy).HasFlag (Tool.Trimmer);
			});
		}

		private static bool HasActiveKeptDerivedAttribute (ICustomAttributeProvider provider)
		{
			return GetActiveKeptDerivedAttributes (provider).Any ();
		}

		protected static uint GetExpectedPseudoAttributeValue (ICustomAttributeProvider provider, uint sourceValue)
		{
			var removals = provider.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (RemovedPseudoAttributeAttribute)).ToArray ();
			var adds = provider.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (AddedPseudoAttributeAttribute)).ToArray ();

			return removals.Aggregate (sourceValue, (accum, item) => accum & ~(uint) item.ConstructorArguments[0].Value) |
				adds.Aggregate ((uint) 0, (acum, item) => acum | (uint) item.ConstructorArguments[0].Value);
		}

		protected static IEnumerable<T> GetCustomAttributeCtorValues<T> (ICustomAttributeProvider provider, string attributeName) where T : class
		{
			return provider.CustomAttributes.
							Where (w => w.AttributeType.Name == attributeName && w.Constructor.Parameters.Count == 1).
							Select (l => l.ConstructorArguments[0].Value as T);
		}

		protected static IEnumerable<string> GetStringOrTypeArrayAttributeValue (CustomAttribute attribute)
		{
			foreach (var arg in (CustomAttributeArgument[]) attribute.ConstructorArguments[0].Value) {
				if (arg.Value is TypeReference tRef)
					yield return tRef.ToString ();
				else
					yield return (string) arg.Value;
			}
		}

		protected static IEnumerable<string> GetStringArrayAttributeValue (CustomAttribute attribute)
		{
			return ((CustomAttributeArgument[]) attribute.ConstructorArguments[0].Value)?.Select (arg => arg.Value.ToString ());
		}
	}
}
