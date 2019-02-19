using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Extensions;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class AssemblyChecker {
		readonly AssemblyDefinition originalAssembly, linkedAssembly;

		HashSet<string> linkedMembers;
		HashSet<string> verifiedGeneratedFields = new HashSet<string> ();
		HashSet<string> verifiedEventMethods = new HashSet<string>();
		HashSet<string> verifiedGeneratedTypes = new HashSet<string> ();

		public AssemblyChecker (AssemblyDefinition original, AssemblyDefinition linked)
		{
			this.originalAssembly = original;
			this.linkedAssembly = linked;
		}

		public void Verify ()
		{
			// TODO: Implement fully, probably via custom Kept attribute
			Assert.IsFalse (linkedAssembly.MainModule.HasExportedTypes);

			VerifyCustomAttributes (originalAssembly, linkedAssembly);
			VerifySecurityAttributes (originalAssembly, linkedAssembly);

			foreach (var originalModule in originalAssembly.Modules)
				VerifyModule (originalModule, linkedAssembly.Modules.FirstOrDefault (m => m.Name == originalModule.Name));

			VerifyResources (originalAssembly, linkedAssembly);
			VerifyReferences (originalAssembly, linkedAssembly);

			linkedMembers = new HashSet<string> (linkedAssembly.MainModule.AllMembers ().Select (s => {
				return s.FullName;
			}), StringComparer.Ordinal);

			var membersToAssert = originalAssembly.MainModule.Types;
			foreach (var originalMember in membersToAssert) {
				var td = originalMember as TypeDefinition;
				if (td != null) {
					if (td.Name == "<Module>") {
						linkedMembers.Remove (td.Name);
						continue;
					}

					TypeDefinition linkedType = linkedAssembly.MainModule.GetType (originalMember.FullName);
					VerifyTypeDefinition (td, linkedType);
					linkedMembers.Remove (td.FullName);

					continue;
				}

				throw new NotImplementedException ($"Don't know how to check member of type {originalMember.GetType ()}");
			}

			Assert.IsEmpty (linkedMembers, "Linked output includes unexpected member");
		}

		protected virtual void VerifyModule (ModuleDefinition original, ModuleDefinition linked)
		{
			// We never link away a module today so let's make sure the linked one isn't null
			if (linked == null)
				Assert.Fail ($"Linked assembly `{original.Assembly.Name.Name}` is missing module `{original.Name}`");

			VerifyCustomAttributes (original, linked);
		}

		protected virtual void VerifyTypeDefinition (TypeDefinition original, TypeDefinition linked)
		{
			if (linked != null && verifiedGeneratedTypes.Contains (linked.FullName))
				return;
			
			ModuleDefinition linkedModule = linked?.Module;

			//
			// Little bit complex check to allow easier test writting to match
			// - It has [Kept] attribute or any variation of it
			// - It contains Main method
			// - It contains at least one member which has [Kept] attribute (not recursive)
			//
			bool expectedKept =
				original.HasAttributeDerivedFrom (nameof (KeptAttribute)) ||
				(linked != null && linkedModule.Assembly.EntryPoint?.DeclaringType == linked) ||
				original.AllMembers ().Any (l => l.HasAttribute (nameof (KeptAttribute)));

			if (!expectedKept) {
				if (linked != null)
					Assert.Fail ($"Type `{original}' should have been removed");

				return;
			}

			VerifyTypeDefinitionKept (original, linked);
		}

		protected virtual void VerifyTypeDefinitionKept (TypeDefinition original, TypeDefinition linked)
		{
			if (linked == null)
				Assert.Fail ($"Type `{original}' should have been kept");

			if (!original.IsInterface)
				VerifyBaseType (original, linked);

			VerifyInterfaces (original, linked);
			VerifyPseudoAttributes (original, linked);
			VerifyGenericParameters (original, linked);
			VerifyCustomAttributes (original, linked);
			VerifySecurityAttributes (original, linked);

			VerifyFixedBufferFields (original, linked);

			foreach (var td in original.NestedTypes) {
				VerifyTypeDefinition (td, linked?.NestedTypes.FirstOrDefault (l => td.FullName == l.FullName));
				linkedMembers.Remove (td.FullName);
			}

			// Need to check properties before fields so that the KeptBackingFieldAttribute is handled correctly
			foreach (var p in original.Properties) {
				VerifyProperty (p, linked?.Properties.FirstOrDefault (l => p.Name == l.Name), linked);
				linkedMembers.Remove (p.FullName);
			}
			// Need to check events before fields so that the KeptBackingFieldAttribute is handled correctly
			foreach (var e in original.Events) {
				VerifyEvent (e, linked?.Events.FirstOrDefault (l => e.Name == l.Name), linked);
				linkedMembers.Remove (e.FullName);
			}

			// Need to check delegate cache fields before the normal field check
			VerifyDelegateBackingFields (original, linked);

			foreach (var f in original.Fields) {
				if (verifiedGeneratedFields.Contains (f.FullName))
					continue;
				VerifyField (f, linked?.Fields.FirstOrDefault (l => f.Name == l.Name));
				linkedMembers.Remove (f.FullName);
			}

			foreach (var m in original.Methods) {
				if (verifiedEventMethods.Contains (m.FullName))
					continue;
				VerifyMethod (m, linked?.Methods.FirstOrDefault (l => m.GetSignature () == l.GetSignature ()));
				linkedMembers.Remove (m.FullName);
			}
		}

		void VerifyBaseType (TypeDefinition src, TypeDefinition linked)
		{
			string expectedBaseName;
			var expectedBaseGenericAttr = src.CustomAttributes.FirstOrDefault (w => w.AttributeType.Name == nameof (KeptBaseTypeAttribute) && w.ConstructorArguments.Count > 1);
			if (expectedBaseGenericAttr != null) {
				StringBuilder builder = new StringBuilder ();
				builder.Append (expectedBaseGenericAttr.ConstructorArguments [0].Value);
				builder.Append ("<");
				bool separator = false;
				foreach (var caa in (CustomAttributeArgument[])expectedBaseGenericAttr.ConstructorArguments [1].Value) {
					if (separator)
						builder.Append (",");
					else
						separator = true;

					var arg = (CustomAttributeArgument)caa.Value;
					builder.Append (arg.Value);
				}

				builder.Append (">");
				expectedBaseName = builder.ToString ();
			} else {
				var defaultBaseType = src.IsEnum ? "System.Enum" : src.IsValueType ? "System.ValueType" : "System.Object";
				expectedBaseName = GetCustomAttributeCtorValues<object> (src, nameof (KeptBaseTypeAttribute)).FirstOrDefault ()?.ToString () ?? defaultBaseType;
			}
			Assert.AreEqual (expectedBaseName, linked.BaseType?.FullName, $"Incorrect base type on : {linked.Name}");
		}

		void VerifyInterfaces (TypeDefinition src, TypeDefinition linked)
		{
			var expectedInterfaces = new HashSet<string> (GetCustomAttributeCtorValues<object> (src, nameof (KeptInterfaceAttribute)).Select (val => val.ToString ()));
			if (expectedInterfaces.Count == 0) {
				Assert.IsFalse (linked.HasInterfaces, $"Type `{src}' has unexpected interfaces");
			} else {
				foreach (var iface in linked.Interfaces) {
					Assert.IsTrue (expectedInterfaces.Remove (iface.InterfaceType.FullName), $"Type `{src}' interface `{iface.InterfaceType.FullName}' should have been removed");
				}

				Assert.IsEmpty (expectedInterfaces, $"Unexpected interfaces on {src}");
			}
		}

		void VerifyField (FieldDefinition src, FieldDefinition linked)
		{
			bool expectedKept = ShouldBeKept (src);

			if (!expectedKept) {
				if (linked != null)
					Assert.Fail ($"Field `{src}' should have been removed");

				return;
			}

			VerifyFieldKept (src, linked);
		}

		void VerifyFieldKept (FieldDefinition src, FieldDefinition linked)
		{
			if (linked == null)
				Assert.Fail ($"Field `{src}' should have been kept");

			Assert.AreEqual (src?.Constant, linked?.Constant, $"Field `{src}' value");

			VerifyPseudoAttributes (src, linked);
			VerifyCustomAttributes (src, linked);
		}

		void VerifyProperty (PropertyDefinition src, PropertyDefinition linked, TypeDefinition linkedType)
		{
			VerifyMemberBackingField (src, linkedType);

			bool expectedKept = ShouldBeKept (src);

			if (!expectedKept) {
				if (linked != null)
					Assert.Fail ($"Property `{src}' should have been removed");

				return;
			}

			if (linked == null)
				Assert.Fail ($"Property `{src}' should have been kept");

			Assert.AreEqual (src?.Constant, linked?.Constant, $"Property `{src}' value");

			VerifyPseudoAttributes (src, linked);
			VerifyCustomAttributes (src, linked);
		}

		void VerifyEvent (EventDefinition src, EventDefinition linked, TypeDefinition linkedType)
		{
			VerifyMemberBackingField (src, linkedType);

			bool expectedKept = ShouldBeKept (src);

			if (!expectedKept) {
				if (linked != null)
					Assert.Fail ($"Event `{src}' should have been removed");

				return;
			}

			if (linked == null)
				Assert.Fail ($"Event `{src}' should have been kept");

			if (src.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (KeptEventAddMethodAttribute))) {
				VerifyMethodInternal (src.AddMethod, linked.AddMethod, true);
				verifiedEventMethods.Add (src.AddMethod.FullName);
				linkedMembers.Remove (src.AddMethod.FullName);
			}

			if (src.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (KeptEventRemoveMethodAttribute))) {
				VerifyMethodInternal (src.RemoveMethod, linked.RemoveMethod, true);
				verifiedEventMethods.Add (src.RemoveMethod.FullName);
				linkedMembers.Remove (src.RemoveMethod.FullName);
			}

			VerifyPseudoAttributes (src, linked);
			VerifyCustomAttributes (src, linked);
		}

		void VerifyMethod (MethodDefinition src, MethodDefinition linked)
		{
			bool expectedKept = ShouldMethodBeKept (src);
			VerifyMethodInternal (src, linked, expectedKept);
		}


		void VerifyMethodInternal (MethodDefinition src, MethodDefinition linked, bool expectedKept)
		{
			if (!expectedKept) {
				if (linked != null)
					Assert.Fail ($"Method `{src.FullName}' should have been removed");

				return;
			}

			VerifyMethodKept (src, linked);
		}

		void VerifyMemberBackingField (IMemberDefinition src, TypeDefinition linkedType)
		{
			var keptBackingFieldAttribute = src.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.Name == nameof (KeptBackingFieldAttribute));
			if (keptBackingFieldAttribute == null)
				return;

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

			if (srcField == null)
				Assert.Fail ($"{src.MetadataToken.TokenType} `{src}', could not locate the expected backing field {backingFieldName}");

			VerifyFieldKept (srcField, linkedType?.Fields.FirstOrDefault (l => srcField.Name == l.Name));
			verifiedGeneratedFields.Add (srcField.FullName);
			linkedMembers.Remove (srcField.FullName);
		}

		protected virtual void VerifyMethodKept (MethodDefinition src, MethodDefinition linked)
		{
			if (linked == null)
				Assert.Fail ($"Method `{src.FullName}' should have been kept");

			VerifyPseudoAttributes (src, linked);
			VerifyGenericParameters (src, linked);
			VerifyCustomAttributes (src, linked);
			VerifyParameters (src, linked);
			VerifySecurityAttributes (src, linked);
			VerifyArrayInitializers (src, linked);
			VerifyMethodBody (src, linked);
		}

		protected virtual void VerifyMethodBody (MethodDefinition src, MethodDefinition linked)
		{
			if (!src.HasBody)
				return;

			VerifyExceptionHandlers (src, linked);
			VerifyInstructions (src, linked);
			VerifyLocals (src, linked);
		}
		
		protected static void VerifyInstructions (MethodDefinition src, MethodDefinition linked)
		{
			VerifyBodyProperties (
				src,
				linked,
				nameof (ExpectedInstructionSequenceAttribute),
				nameof (ExpectBodyModifiedAttribute),
				"instructions",
				m => m.Body.Instructions.Select (ins => ins.OpCode.ToString ().ToLower()).ToArray (),
				attr => GetStringArrayAttributeValue (attr).Select (v => v.ToLower ()).ToArray ());
		}
		
		static void VerifyExceptionHandlers (MethodDefinition src, MethodDefinition linked)
		{
			VerifyBodyProperties (
				src,
				linked,
				nameof (ExpectedExceptionHandlerSequenceAttribute),
				nameof (ExpectExceptionHandlersModifiedAttribute),
				"exception handlers",
				m => m.Body.ExceptionHandlers.Select (h => h.HandlerType.ToString ().ToLower ()).ToArray (),
				attr => GetStringArrayAttributeValue (attr).Select (v => v.ToLower ()).ToArray ());
		}

		static void VerifyLocals (MethodDefinition src, MethodDefinition linked)
		{
			VerifyBodyProperties (
				src,
				linked,
				nameof (ExpectedLocalsSequenceAttribute),
				nameof (ExpectLocalsModifiedAttribute),
				"locals",
				m => m.Body.Variables.Select (v => v.VariableType.ToString ()).ToArray (),
				attr => GetStringOrTypeArrayAttributeValue (attr).ToArray ());
		}

		protected static void VerifyBodyProperties (MethodDefinition src, MethodDefinition linked, string sequenceAttributeName, string expectModifiedAttributeName,
			string propertyDescription,
			Func<MethodDefinition, string []> valueCollector,
			Func<CustomAttribute, string []> getExpectFromSequenceAttribute)
		{
			var expectedSequenceAttribute = src.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.Name == sequenceAttributeName);
			var linkedValues = valueCollector (linked);
			var srcValues = valueCollector (src);

			if (src.CustomAttributes.Any (attr => attr.AttributeType.Name == expectModifiedAttributeName)) {
				Assert.That (
					linkedValues,
					Is.Not.EquivalentTo (srcValues),
					$"Expected method `{src} to have {propertyDescription} modified, however, the {propertyDescription} were the same as the original\n{FormattingUtils.FormatSequenceCompareFailureMessage (linkedValues, srcValues)}");
			} else if (expectedSequenceAttribute != null) {
				var expected = getExpectFromSequenceAttribute(expectedSequenceAttribute).ToArray();
				Assert.That(
					linkedValues,
					Is.EquivalentTo (expected),
					$"Expected method `{src} to have it's {propertyDescription} modified, however, the sequence of {propertyDescription} does not match the expected value\n{FormattingUtils.FormatSequenceCompareFailureMessage2 (linkedValues, expected, srcValues)}");
			} else {
				Assert.That(
					linkedValues,
					Is.EquivalentTo (srcValues),
					$"Expected method `{src} to have it's {propertyDescription} unchanged, however, the {propertyDescription} differ from the original\n{FormattingUtils.FormatSequenceCompareFailureMessage (linkedValues, srcValues)}");
			}
		}

		void VerifyReferences (AssemblyDefinition original, AssemblyDefinition linked)
		{
			var expected = original.MainModule.AllDefinedTypes ()
				.SelectMany (t => GetCustomAttributeCtorValues<string> (t, nameof (KeptReferenceAttribute)))
				.Select (ReduceAssemblyFileNameOrNameToNameOnly)
				.ToArray ();

			/*
			 - The test case will always need to have at least 1 reference.
			 - Forcing all tests to define their expected references seems tedious
			 
			 Given the above, let's assume that when no [KeptReference] attributes are present,
			 the test case does not want to make any assertions regarding references.
			 
			 Once 1 kept reference attribute is used, the test will need to define all of of it's expected references
			*/
			if (expected.Length == 0)
				return;

			var actual = linked.MainModule.AssemblyReferences
				.Select (name => name.Name)
				.ToArray ();

			Assert.That (actual, Is.EquivalentTo (expected));
		}

		string ReduceAssemblyFileNameOrNameToNameOnly (string fileNameOrAssemblyName)
		{
			if (fileNameOrAssemblyName.EndsWith (".dll") || fileNameOrAssemblyName.EndsWith (".exe") || fileNameOrAssemblyName.EndsWith (".winmd"))
				return System.IO.Path.GetFileNameWithoutExtension (fileNameOrAssemblyName);
			
			// It must already be just the assembly name
			return fileNameOrAssemblyName;
		}

		void VerifyResources (AssemblyDefinition original, AssemblyDefinition linked)
		{
			var expectedResources = original.MainModule.AllDefinedTypes ()
				.SelectMany (t => GetCustomAttributeCtorValues<string> (t, nameof (KeptResourceAttribute)));

			Assert.That (linked.MainModule.Resources.Select (r => r.Name), Is.EquivalentTo (expectedResources));
		}

		protected virtual void VerifyPseudoAttributes (MethodDefinition src, MethodDefinition linked)
		{
			var expected = (MethodAttributes) GetExpectedPseudoAttributeValue(src, (uint) src.Attributes);
			Assert.AreEqual (expected, linked.Attributes, $"Method `{src}' pseudo attributes did not match expected");
		}

		protected virtual void VerifyPseudoAttributes (TypeDefinition src, TypeDefinition linked)
		{
			var expected = (TypeAttributes) GetExpectedPseudoAttributeValue(src, (uint) src.Attributes);
			Assert.AreEqual (expected, linked.Attributes, $"Type `{src}' pseudo attributes did not match expected");
		}

		protected virtual void VerifyPseudoAttributes (FieldDefinition src, FieldDefinition linked)
		{
			var expected = (FieldAttributes) GetExpectedPseudoAttributeValue(src, (uint) src.Attributes);
			Assert.AreEqual (expected, linked.Attributes, $"Field `{src}' pseudo attributes did not match expected");
		}

		protected virtual void VerifyPseudoAttributes (PropertyDefinition src, PropertyDefinition linked)
		{
			var expected = (PropertyAttributes) GetExpectedPseudoAttributeValue(src, (uint) src.Attributes);
			Assert.AreEqual (expected, linked.Attributes, $"Property `{src}' pseudo attributes did not match expected");
		}

		protected virtual void VerifyPseudoAttributes (EventDefinition src, EventDefinition linked)
		{
			var expected = (EventAttributes) GetExpectedPseudoAttributeValue(src, (uint) src.Attributes);
			Assert.AreEqual (expected, linked.Attributes, $"Event `{src}' pseudo attributes did not match expected");
		}

		protected virtual void VerifyCustomAttributes (ICustomAttributeProvider src, ICustomAttributeProvider linked)
		{
			var expectedAttrs = GetExpectedAttributes (src).ToList ();
			var linkedAttrs = FilterLinkedAttributes (linked).ToList ();

			Assert.That (linkedAttrs, Is.EquivalentTo (expectedAttrs), $"Custom attributes on `{src}' are not matching");
		}

		protected virtual void VerifySecurityAttributes (ICustomAttributeProvider src, ISecurityDeclarationProvider linked)
		{
			var expectedAttrs = GetCustomAttributeCtorValues<object> (src, nameof (KeptSecurityAttribute))
				.Select (attr => attr.ToString ())
				.ToList ();

			var linkedAttrs = FilterLinkedSecurityAttributes (linked).ToList ();

			Assert.That (linkedAttrs, Is.EquivalentTo (expectedAttrs), $"Security attributes on `{src}' are not matching");
		}

		protected virtual void VerifyArrayInitializers (MethodDefinition src, MethodDefinition linked)
		{
			var expectedIndicies = GetCustomAttributeCtorValues<object> (src, nameof (KeptInitializerData))
				.Cast<int> ()
				.ToArray ();

			var expectKeptAll = src.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (KeptInitializerData) && !attr.HasConstructorArguments);

			if (expectedIndicies.Length == 0 && !expectKeptAll)
				return;
			
			if (!src.HasBody)
				Assert.Fail ($"`{nameof (KeptInitializerData)}` cannot be used on methods that don't have bodies");

			var srcImplementationDetails = src.Module.Types.FirstOrDefault (t => string.IsNullOrEmpty (t.Namespace) && t.Name.StartsWith ("<PrivateImplementationDetails>"));
			
			if (srcImplementationDetails == null)
				Assert.Fail ("Could not locate <PrivateImplementationDetails> in the original assembly.  Does your test use initializers?");

			var linkedImplementationDetails = linked.Module.Types.FirstOrDefault (t => string.IsNullOrEmpty (t.Namespace) && t.Name.StartsWith ("<PrivateImplementationDetails>"));
			
			if (linkedImplementationDetails == null)
				Assert.Fail ("Could not locate <PrivateImplementationDetails> in the linked assembly");
			
			var possibleInitializerFields = src.Body.Instructions
				.Where (ins => IsLdtokenOnPrivateImplementationDetails (srcImplementationDetails, ins))
				.Select (ins => ((FieldReference)ins.Operand).Resolve ())
				.ToArray ();
			
			if (possibleInitializerFields.Length == 0)
				Assert.Fail ($"`{src}` does not make use of any initializers");

			if (expectKeptAll) {
				foreach (var srcField in possibleInitializerFields) {
					var linkedField = linkedImplementationDetails.Fields.FirstOrDefault (f => f.Name == srcField.Name);
					VerifyInitializerField (srcField, linkedField);
				}
			} else {
				foreach (var index in expectedIndicies) {
					if (index < 0 || index > possibleInitializerFields.Length)
						Assert.Fail($"Invalid expected index `{index}` in {src}.  Value must be between 0 and {expectedIndicies.Length}");

					var srcField = possibleInitializerFields[index];
					var linkedField = linkedImplementationDetails.Fields.FirstOrDefault (f => f.Name == srcField.Name);

					VerifyInitializerField (srcField, linkedField);
				}
			}
		}

		void VerifyInitializerField (FieldDefinition src, FieldDefinition linked)
		{
			VerifyFieldKept (src, linked);
			verifiedGeneratedFields.Add (linked.FullName);
			linkedMembers.Remove (linked.FullName);
			VerifyTypeDefinitionKept (src.FieldType.Resolve (), linked.FieldType.Resolve ());
			linkedMembers.Remove (linked.FieldType.FullName);
			linkedMembers.Remove (linked.DeclaringType.FullName);
			verifiedGeneratedTypes.Add (linked.DeclaringType.FullName);
		}

		static bool IsLdtokenOnPrivateImplementationDetails (TypeDefinition privateImplementationDetails, Instruction instruction)
		{
			if (instruction.OpCode.Code == Code.Ldtoken && instruction.Operand is FieldReference field)
			{
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
				var name = srcDefinition.Name.Substring (1, srcDefinition.Name.IndexOf('>') - 1);
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
						continue;

					// When mcs is used to compile the test cases, backing fields end up with this attribute on them
					case "System.Diagnostics.DebuggerBrowsableAttribute":
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

		void VerifyFixedBufferFields (TypeDefinition src, TypeDefinition linked)
		{
			var fields = src.Fields.Where (f => f.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (KeptFixedBufferAttribute)));

			foreach (var field in fields) {
				// The name of the generated fixed buffer type is a little tricky.
				// Some versions of csc name it `<fieldname>e__FixedBuffer0`
				// while mcs and other versions of csc name it `<fieldname>__FixedBuffer0`
				var originalCompilerGeneratedBufferType = src.NestedTypes.FirstOrDefault (t => t.FullName.Contains ($"<{field.Name}>") && t.FullName.Contains ("__FixedBuffer"));
				if (originalCompilerGeneratedBufferType == null)
					Assert.Fail ($"Could not locate original compiler generated fixed buffer type for field {field}");

				var linkedCompilerGeneratedBufferType = linked.NestedTypes.FirstOrDefault (t => t.Name == originalCompilerGeneratedBufferType.Name);
				if (linkedCompilerGeneratedBufferType == null)
					Assert.Fail ($"Missing expected type {originalCompilerGeneratedBufferType}");
				
				// Have to verify the field before the type
				var originalElementField = originalCompilerGeneratedBufferType.Fields.FirstOrDefault (f => f.Name == "FixedElementField");
				if (originalElementField == null)
					Assert.Fail ($"Could not locate original compiler generated FixedElementField on {originalCompilerGeneratedBufferType}");
				
				var linkedField = linkedCompilerGeneratedBufferType?.Fields.FirstOrDefault (l => l.Name == originalElementField.Name);
				VerifyFieldKept (originalElementField, linkedField);
				verifiedGeneratedFields.Add (originalElementField.FullName);
				linkedMembers.Remove (originalElementField.FullName);
				
				VerifyTypeDefinitionKept(originalCompilerGeneratedBufferType, linkedCompilerGeneratedBufferType);
				verifiedGeneratedTypes.Add(originalCompilerGeneratedBufferType.FullName);
			}
		}

		void VerifyDelegateBackingFields (TypeDefinition src, TypeDefinition linked)
		{
			var expectedFieldNames = GetCustomAttributeCtorValues<string> (src, nameof (KeptDelegateCacheFieldAttribute))
				.Select (unique => $"<>f__mg$cache{unique}")
				.ToList ();

			if (expectedFieldNames.Count == 0)
				return;

			foreach (var srcField in src.Fields) {
				if (!expectedFieldNames.Contains (srcField.Name))
					continue;

				var linkedField = linked?.Fields.FirstOrDefault (l => l.Name == srcField.Name);
				VerifyFieldKept (srcField, linkedField);
				verifiedGeneratedFields.Add (srcField.FullName);
				linkedMembers.Remove (srcField.FullName);
			}
		}

		void VerifyGenericParameters (IGenericParameterProvider src, IGenericParameterProvider linked)
		{
			Assert.AreEqual (src.HasGenericParameters, linked.HasGenericParameters);
			if (src.HasGenericParameters) {
				for (int i = 0; i < src.GenericParameters.Count; ++i) {
					// TODO: Verify constraints
					VerifyCustomAttributes (src.GenericParameters [i], linked.GenericParameters [i]);
				}
			}
		}

		void VerifyParameters (IMethodSignature src, IMethodSignature linked)
		{
			Assert.AreEqual (src.HasParameters, linked.HasParameters);
			if (src.HasParameters) {
				for (int i = 0; i < src.Parameters.Count; ++i) {
					VerifyCustomAttributes (src.Parameters [i], linked.Parameters [i]);
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
			if (member.HasAttribute (nameof (KeptAttribute)))
				return true;

			ICustomAttributeProvider cap = (ICustomAttributeProvider)member.DeclaringType;
			if (cap == null)
				return false;

			return GetCustomAttributeCtorValues<string> (cap, nameof (KeptMemberAttribute)).Any (a => a == (signature ?? member.Name));
		}

		protected static uint GetExpectedPseudoAttributeValue (ICustomAttributeProvider provider, uint sourceValue)
		{
			var removals = provider.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (RemovedPseudoAttributeAttribute)).ToArray ();
			var adds = provider.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (AddedPseudoAttributeAttribute)).ToArray ();

			return removals.Aggregate (sourceValue, (accum, item) => accum & ~((uint) item.ConstructorArguments [0].Value)) |
				adds.Aggregate ((uint)0, (acum, item) => acum | (uint) item.ConstructorArguments [0].Value);
		}

		protected static IEnumerable<T> GetCustomAttributeCtorValues<T> (ICustomAttributeProvider provider, string attributeName) where T : class
		{
			return provider.CustomAttributes.
							Where (w => w.AttributeType.Name == attributeName && w.Constructor.Parameters.Count == 1).
							Select (l => l.ConstructorArguments [0].Value as T);
		}
		
		protected static IEnumerable<string> GetStringOrTypeArrayAttributeValue (CustomAttribute attribute)
		{
			foreach (var arg in ((CustomAttributeArgument[]) attribute.ConstructorArguments [0].Value)) {
				if (arg.Value is TypeReference tRef)
					yield return tRef.ToString ();
				else
					yield return (string) arg.Value;
			}
		}
		
		protected static IEnumerable<string> GetStringArrayAttributeValue (CustomAttribute attribute)
		{
			return ((CustomAttributeArgument[]) attribute.ConstructorArguments [0].Value)?.Select (arg => arg.Value.ToString ());
		}
	}
}
