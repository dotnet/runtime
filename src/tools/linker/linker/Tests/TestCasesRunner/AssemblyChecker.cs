using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Extensions;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner {
	class AssemblyChecker {
		readonly AssemblyDefinition originalAssembly, linkedAssembly;

		HashSet<string> linkedMembers;
		HashSet<string> verifiedBackingFields = new HashSet<string> ();
		HashSet<string> verifiedEventMethods = new HashSet<string>();

		public AssemblyChecker (AssemblyDefinition original, AssemblyDefinition linked)
		{
			this.originalAssembly = original;
			this.linkedAssembly = linked;
		}

		public void Verify ()
		{
			// TODO: Implement fully, probably via custom Kept attribute
			Assert.IsFalse (linkedAssembly.MainModule.HasExportedTypes);

			VerifyCustomAttributes (linkedAssembly, originalAssembly);

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

		protected virtual void VerifyTypeDefinition (TypeDefinition original, TypeDefinition linked)
		{
			ModuleDefinition linkedModule = linked?.Module;

			//
			// Little bit complex check to allow easier test writting to match
			// - It has [Kept] attribute or any variation of it
			// - It contains Main method
			// - It contains at least one member which has [Kept] attribute (not recursive)
			//
			bool expectedKept =
				original.HasAttributeDerivedFrom (nameof (KeptAttribute)) ||
				(linked != null && linkedModule.Assembly.EntryPoint.DeclaringType == linked) ||
				original.AllMembers ().Any (l => l.HasAttribute (nameof (KeptAttribute)));

			if (!expectedKept) {
				if (linked != null)
					Assert.Fail ($"Type `{original}' should have been removed");

				return;
			}

			if (linked == null)
				Assert.Fail ($"Type `{original}' should have been kept");

			if (!original.IsInterface)
				VerifyBaseType (original, linked);

			VerifyInterfaces (original, linked);

			VerifyGenericParameters (original, linked);
			VerifyCustomAttributes (original, linked);

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

			foreach (var f in original.Fields) {
				if (verifiedBackingFields.Contains (f.FullName))
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
				var defaultBaseType = src.IsValueType ? "System.ValueType" : "System.Object";
				expectedBaseName = GetCustomAttributeCtorValues<object> (src, nameof (KeptBaseTypeAttribute)).FirstOrDefault ()?.ToString () ?? defaultBaseType;
			}
			Assert.AreEqual (expectedBaseName, linked.BaseType?.FullName);
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

				Assert.IsEmpty (expectedInterfaces);
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

			Assert.AreEqual (src?.Attributes, linked?.Attributes, $"Field `{src}' attributes");
			Assert.AreEqual (src?.Constant, linked?.Constant, $"Field `{src}' value");

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

			Assert.AreEqual (src?.Attributes, linked?.Attributes, $"Property `{src}' attributes");
			Assert.AreEqual (src?.Constant, linked?.Constant, $"Property `{src}' value");

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
			} else {
				var keptBackingFieldAttribute = src.CustomAttributes
					.FirstOrDefault (attr => attr.AttributeType.Name == nameof (KeptBackingFieldAttribute));

				// If we have KeepBackingFieldAttribute set, 
				// then we expect having 'add' and 'remove' accessors marked as 'kept' implicitly.
				if (keptBackingFieldAttribute != null)
				{
					VerifyMethodInternal (src.AddMethod, linked.AddMethod, true);
					verifiedEventMethods.Add (src.AddMethod.FullName);
					linkedMembers.Remove (src.AddMethod.FullName);

					VerifyMethodInternal (src.RemoveMethod, linked.RemoveMethod, true);
					verifiedEventMethods.Add (src.RemoveMethod.FullName);
					linkedMembers.Remove (src.RemoveMethod.FullName);
				}
			}

			if (linked == null)
				Assert.Fail ($"Event `{src}' should have been kept");

			Assert.AreEqual (src?.Attributes, linked?.Attributes, $"Event `{src}' attributes");

			VerifyCustomAttributes (src, linked);
		}

		void VerifyMethod (MethodDefinition src, MethodDefinition linked)
		{
			var srcSignature = src.GetSignature ();
			bool expectedKept = ShouldBeKept (src, srcSignature) || (linked != null && linked.DeclaringType.Module.EntryPoint == linked);

			VerifyMethodInternal (src, linked, expectedKept);
		}


		void VerifyMethodInternal (MethodDefinition src, MethodDefinition linked, bool expectedKept)
		{
			if (!expectedKept) {
				if (linked != null)
					Assert.Fail ($"Method `{src.FullName}' should have been removed");

				return;
			}

			if (linked == null)
				Assert.Fail ($"Method `{src.FullName}' should have been kept");

			Assert.AreEqual (src?.Attributes, linked?.Attributes, $"Method `{src}' attributes");

			VerifyGenericParameters (src, linked);
			VerifyCustomAttributes (src, linked);
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
			verifiedBackingFields.Add (srcField.FullName);
			linkedMembers.Remove (srcField.FullName);
		}

		static void VerifyCustomAttributes (ICustomAttributeProvider src, ICustomAttributeProvider linked)
		{
			var expectedAttrs = new List<string> (GetCustomAttributeCtorValues<string> (src, nameof (KeptAttributeAttribute)));
			var linkedAttrs = new List<string> (FilterLinkedAttributes (linked));

			// FIXME: Linker unused attributes removal is not working
			// Assert.That (linkedAttrs, Is.EquivalentTo (expectedAttrs), $"Custom attributes on `{src}' are not matching");
		}

		static IEnumerable<string> FilterLinkedAttributes (ICustomAttributeProvider linked)
		{
			foreach (var attr in linked.CustomAttributes) {
				switch (attr.AttributeType.FullName) {
				case "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute":
					continue;
				}

				yield return attr.AttributeType.FullName;
			}
		}

		static void VerifyGenericParameters (IGenericParameterProvider src, IGenericParameterProvider linked)
		{
			Assert.AreEqual (src.HasGenericParameters, linked.HasGenericParameters);
			if (src.HasGenericParameters) {
				for (int i = 0; i < src.GenericParameters.Count; ++i) {
					// TODO: Verify constraints
					VerifyCustomAttributes (src.GenericParameters [i], linked.GenericParameters [i]);
				}
			}
		}

		static bool ShouldBeKept<T> (T member, string signature = null) where T : MemberReference, ICustomAttributeProvider
		{
			if (member.HasAttribute (nameof (KeptAttribute)))
				return true;

			ICustomAttributeProvider cap = (ICustomAttributeProvider)member.DeclaringType;
			if (cap == null)
				return false;

			return GetCustomAttributeCtorValues<string> (cap, nameof (KeptMemberAttribute)).Any (a => a == (signature ?? member.Name));
		}

		static IEnumerable<T> GetCustomAttributeCtorValues<T> (ICustomAttributeProvider provider, string attributeName) where T : class
		{
			return provider.CustomAttributes.
							Where (w => w.AttributeType.Name == attributeName && w.Constructor.Parameters.Count == 1).
							Select (l => l.ConstructorArguments [0].Value as T);
		}
	}
}
