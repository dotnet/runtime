// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using ILCompiler;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Extensions;
using Xunit;
using MetadataType = Internal.TypeSystem.MetadataType;

namespace Mono.Linker.Tests.TestCasesRunner
{
	partial class AssemblyChecker
	{
		class LinkedEntity
		{
			public TypeSystemEntity Entity { get; init; }

			public LinkedEntity(TypeSystemEntity entity) => Entity = entity;
		}

		class LinkedMethodEntity : LinkedEntity
		{
			public bool IsReflected { get; init; }

			public MethodDesc Method { get => (MethodDesc) Entity; }

			public LinkedMethodEntity (MethodDesc method, bool isReflected) : base (method) => IsReflected = isReflected;
		}

		private readonly BaseAssemblyResolver originalsResolver;
		private readonly ReaderParameters originalReaderParameters;
		private readonly AssemblyDefinition originalAssembly;
		private readonly TrimmedTestCaseResult testResult;

		private readonly Dictionary<AssemblyQualifiedToken, LinkedEntity> linkedMembers;
		private readonly HashSet<string> verifiedGeneratedFields = new HashSet<string> ();
		private readonly HashSet<string> verifiedEventMethods = new HashSet<string> ();
		private readonly HashSet<string> verifiedGeneratedTypes = new HashSet<string> ();
		private bool checkNames;

		// Note: It's enough to exclude the type name, all of its members will also be excluded then
		private static readonly HashSet<string> ExcludeDisplayNames = new () {
				// Ignore compiler injected attributes to describe language version
				"Microsoft.CodeAnalysis.EmbeddedAttribute",
				"System.Runtime.CompilerServices.RefSafetyRulesAttribute",

				// Ignore NativeAOT injected members
				"<Module>.StartupCodeMain(Int32,IntPtr)",
				"<Module>.MainMethodWrapper()",
				"<Module>.MainMethodWrapper(String[])",

				// Ignore compiler generated code which can't be reasonably matched to the source method
				"<PrivateImplementationDetails>",
			};

		public AssemblyChecker (
			BaseAssemblyResolver originalsResolver,
			ReaderParameters originalReaderParameters,
			AssemblyDefinition original,
			TrimmedTestCaseResult testResult)
		{
			this.originalsResolver = originalsResolver;
			this.originalReaderParameters = originalReaderParameters;
			this.originalAssembly = original;
			this.testResult = testResult;
			this.linkedMembers = new ();

			checkNames = original.MainModule.GetTypeReferences ().Any (attr =>
				attr.Name == nameof (RemovedNameValueAttribute));
		}

		public void Verify ()
		{
			var errors = VerifyImpl().ToList();
			if (errors.Any())
			{
				Assert.Fail(string.Join(Environment.NewLine, errors));
			}

		}

		IEnumerable<string> VerifyImpl()
		{
			// There are no type forwarders left after compilation in Native AOT
			// VerifyExportedTypes (originalAssembly, linkedAssembly);

			// TODO
			// VerifyCustomAttributes (originalAssembly, linkedAssembly);
			// VerifySecurityAttributes (originalAssembly, linkedAssembly);

			// TODO - this is mostly attribute verification
			// foreach (var originalModule in originalAssembly.Modules)
			//   VerifyModule (originalModule, linkedAssembly.Modules.FirstOrDefault (m => m.Name == originalModule.Name));

			// TODO
			// VerifyResources (originalAssembly, linkedAssembly);

			// There are no assembly reference in Native AOT
			// VerifyReferences (originalAssembly, linkedAssembly);

			PopulateLinkedMembers ();

			var membersToAssert = originalAssembly.MainModule.Types;
			foreach (var originalMember in membersToAssert) {
				if (originalMember is TypeDefinition td) {
					AssemblyQualifiedToken token = new (td);

					if (td.Name == "<Module>") {
						linkedMembers.Remove (token);
						continue;
					}

					linkedMembers.TryGetValue (
						token,
						out LinkedEntity? linkedMember);

					foreach(var err in VerifyTypeDefinition (td, linkedMember))
						yield return err;
					linkedMembers.Remove (token);

					continue;
				}

				throw new NotImplementedException ($"Don't know how to check member of type {originalMember.GetType ()}");
			}

			// Verify anything not in the main assembly
			foreach(var err in VerifyLinkingOfOtherAssemblies(this.originalAssembly))
				yield return err;

			// Filter out all members which are not from the main assembly
			// The Kept attributes are "optional" for non-main assemblies
			string mainModuleName = originalAssembly.Name.Name;
			List<AssemblyQualifiedToken> externalMembers = linkedMembers.Where (m => GetModuleName (m.Value.Entity) != mainModuleName).Select (m => m.Key).ToList ();
			foreach (var externalMember in externalMembers) {
				linkedMembers.Remove (externalMember);
			}

			if (linkedMembers.Count != 0)
				yield return "Linked output includes unexpected member:\n  " +
					string.Join ("\n  ", linkedMembers.Values.Select (e => e.Entity.GetDisplayName ()));
		}

		private void PopulateLinkedMembers ()
		{
			foreach (TypeDesc type in testResult.TrimmingResults.AllEETypes) {
				AddType (type);
			}

			foreach (MethodDesc method in testResult.TrimmingResults.CompiledMethodBodies) {
				AddMethod (method);
			}

			foreach (MethodDesc method in testResult.TrimmingResults.ReflectedMethods) {
				AddMethod (method, isReflected: true);
			}

			void AddMethod (MethodDesc method, bool isReflected = false)
			{
				MethodDesc methodDef = method.GetTypicalMethodDefinition ();

				if (!ShouldIncludeMethod (methodDef))
					return;

				TypeDesc owningType = methodDef.OwningType;

				// Skip any methods on a delegate - we handle those in the AddType
				// (AOT generates different methods for delegates compared to IL/metadata shapes)
				if (owningType?.IsDelegate == true)
					return;

				if (!AddTrimmedMethod (methodDef, isReflected))
					return;

				if (owningType is not null) {
					AddType (owningType);
				}

				if (methodDef.GetPropertyForAccessor () is { } property)
					AddProperty (property);

				if (methodDef.GetEventForAccessor () is { } @event)
					AddEvent (@event);
			}

			void AddType (TypeDesc type)
			{
				TypeDesc typeDef = type.GetTypeDefinition ();

				if (!ShouldIncludeType (typeDef))
					return;

				if (!AddMember (typeDef))
					return;

				if (typeDef is MetadataType { ContainingType: { } containingType }) {
					AddType (containingType);
				}

				if (typeDef.IsDelegate) {
					// AOT's handling of delegates is very different from the IL/metadata picture
					// So to simplify this, we're going to automatically "mark" all of the delegate's methods
					foreach (MethodDesc m in typeDef.GetMethods ()) {
						if (ShouldIncludeEntityByDisplayName (m)) {
							AddTrimmedMethod (m, isReflected: false);
						}
					}
				}
			}

			void AddProperty (PropertyPseudoDesc property)
			{
				// Note that this is currently called from AddMethod which will exit if the owning type is excluded
				// and also add the owning type if necessary
				if (!ShouldIncludeEntityByDisplayName (property))
					return;

				AddMember (property);
			}

			void AddEvent (EventPseudoDesc @event)
			{
				// Note that this is currently called from AddMethod which will exit if the owning type is excluded
				// and also add the owning type if necessary
				if (!ShouldIncludeEntityByDisplayName (@event))
					return;

				AddMember (@event);
			}

			bool AddMember (TypeSystemEntity entity)
			{
				Assert.False (entity is MethodDesc, "Use AddTrimmedMethod for all methods instead");
				return linkedMembers.TryAdd (new AssemblyQualifiedToken (entity), new LinkedEntity(entity));
			}

			bool AddTrimmedMethod (MethodDesc method, bool isReflected = false)
			{
				var token = new AssemblyQualifiedToken (method);
				bool addedNew = true;
				if (linkedMembers.TryGetValue(token, out var existingValue)) {
					addedNew = false;
					LinkedMethodEntity existingMethod = (LinkedMethodEntity) existingValue;
					if (existingMethod.IsReflected || !isReflected)
						return addedNew;

					linkedMembers.Remove (token);
				}

				linkedMembers.Add (token, new LinkedMethodEntity (method, isReflected));
				return addedNew;
			}

			static bool ShouldIncludeEntityByDisplayName (TypeSystemEntity entity) => !ExcludeDisplayNames.Contains (entity.GetDisplayName ());

			static bool ShouldIncludeType (TypeDesc type)
			{
				if (type is MetadataType metadataType) {
					if (metadataType.ContainingType is { } containingType) {
						if (!ShouldIncludeType (containingType))
							return false;
					}

					if (metadataType.Namespace.StartsWith ("Internal"))
						return false;

					// Simple way to filter out system assemblies - the best way would be to get a list
					// of input/reference assemblies and filter on that, but it's tricky and this should work for basically everything
					if (metadataType.Namespace.StartsWith ("System"))
						return false;


					return ShouldIncludeEntityByDisplayName (type);
				}

				return false;
			}

			static bool ShouldIncludeMethod (MethodDesc method) => ShouldIncludeType (method.OwningType) && ShouldIncludeEntityByDisplayName (method);
		}

		private static MetadataType? GetOwningType (TypeSystemEntity? entity)
		{
			return entity switch
			{
				MetadataType type => type.ContainingType as MetadataType,
				MethodDesc method => method.OwningType as MetadataType,
				PropertyPseudoDesc prop => prop.OwningType,
				EventPseudoDesc e => e.OwningType,
				_ => null
			};
		}

		private static string? GetModuleName (TypeSystemEntity entity)
		{
			return entity switch {
				MetadataType type => type.Module.ToString (),
				_ => GetOwningType(entity)?.Module.ToString()
			};
		}

		protected virtual IEnumerable<string> VerifyModule (ModuleDefinition original, ModuleDefinition? linked)
		{
			// We never link away a module today so let's make sure the linked one isn't null
			if (linked == null) {
				yield return $"Linked assembly `{original.Assembly.Name.Name}` is missing module `{original.Name}`";
				yield break;
			}

			var expected = original.Assembly.MainModule.AllDefinedTypes ()
				.SelectMany (t => GetCustomAttributeCtorValues<string> (t, nameof (KeptModuleReferenceAttribute)))
				.ToArray ();

			var actual = linked.ModuleReferences
				.Select (name => name.Name)
				.ToArray ();

			if (!expected.Equals(actual))
				yield return "Module references do not match";

			foreach(var err in VerifyCustomAttributes (original, linked))
				yield return err;
		}

		IEnumerable<string> VerifyTypeDefinition (TypeDefinition original, LinkedEntity? linkedEntity)
		{
			TypeDesc? linked = linkedEntity?.Entity as TypeDesc;
			if (linked != null && NameUtils.GetActualOriginDisplayName (linked) is string linkedDisplayName && verifiedGeneratedTypes.Contains (linkedDisplayName))
				yield break;

			EcmaModule? linkedModule = (linked as MetadataType)?.Module as EcmaModule;

			//
			// Little bit complex check to allow easier test writing to match
			// - It has [Kept] attribute or any variation of it
			// - It contains Main method
			// - It contains at least one member which has [Kept] attribute (not recursive)
			//
			bool expectedKept =
				HasActiveKeptDerivedAttribute (original) ||
				(linked != null && linkedModule?.EntryPoint?.OwningType == linked) ||
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

			foreach(var err in VerifyTypeDefinitionKept (original, linked))
				yield return err;

			checkNames = prev;

			if (original.HasAttribute (nameof (CreatedMemberAttribute))) {
				// For now always fail on this attribute since we don't know how to validate it
				throw new NotSupportedException ("CreatedMemberAttribute is not yet supported by the test infra");
#if false
				foreach (var attr in original.CustomAttributes.Where (l => l.AttributeType.Name == nameof (CreatedMemberAttribute))) {
					var newName = original.FullName + "::" + attr.ConstructorArguments[0].Value.ToString ();

					var linkedMemberName = linkedMembers.Keys.FirstOrDefault (l => l.Contains (newName));
					if (linkedMemberName == null)
						yield return $"Newly created member '{newName}' was not found";

					linkedMembers.Remove (linkedMemberName);
				}
#endif
			}
		}

		protected virtual IEnumerable<string> VerifyTypeDefinitionKept (TypeDefinition original, TypeDesc? linked)
		{
			// NativeAOT will not keep delegate backing field type information, it's compiled down to a set of static fields
			// this infra currently doesn't track fields in any way.
			// Same goes for private implementation detail type.
			if (IsDelegateBackingFieldsType (original) || IsPrivateImplementationDetailsType(original))
				yield break;

			if (linked == null) {
				yield return $"Type `{original}' should have been kept";
				yield break;
			}

#if false
			// Skip verification of type metadata for compiler generated types (we don't currently need it yet)
			if (!IsCompilerGeneratedMember (original)) {
				foreach(var err in VerifyKeptByAttributes (original, linked))
					yield return err;
				if (!original.IsInterface)
				{
					foreach(var err in VerifyBaseType (original, linked))
						yield return err;
				}

				foreach(var err in VerifyInterfaces (original, linked))
					yield return err;
				foreach(var err in VerifyPseudoAttributes (original, linked))
					yield return err;
				foreach(var err in VerifyGenericParameters (original, linked))
					yield return err;
				foreach(var err in VerifyCustomAttributes (original, linked))
					yield return err;
				foreach(var err in VerifySecurityAttributes (original, linked))
					yield return err;

				foreach(var err in VerifyFixedBufferFields (original, linked))
					yield return err;
			}
#endif

			foreach (var td in original.NestedTypes) {
				AssemblyQualifiedToken token = new (td);
				linkedMembers.TryGetValue (
					token,
					out LinkedEntity? linkedMember);

				foreach(var err in VerifyTypeDefinition (td, linkedMember))
					yield return err;
				linkedMembers.Remove (token);
			}

			//// Need to check properties before fields so that the KeptBackingFieldAttribute is handled correctly
			foreach (var p in original.Properties) {
				AssemblyQualifiedToken token = new AssemblyQualifiedToken (p);

				linkedMembers.TryGetValue (
					token,
					out LinkedEntity? linkedMember);
				foreach(var err in VerifyProperty (p, linkedMember, linked))
					yield return err;
				linkedMembers.Remove (token);
			}
			// Need to check events before fields so that the KeptBackingFieldAttribute is handled correctly
			foreach (var e in original.Events) {
				AssemblyQualifiedToken token = new AssemblyQualifiedToken (e);

				linkedMembers.TryGetValue (
					token,
					out LinkedEntity? linkedMember);
				foreach(var err in VerifyEvent (e, linkedMember, linked))
					yield return err;
				linkedMembers.Remove (token);
			}

#if false
			// Need to check delegate cache fields before the normal field check
			VerifyDelegateBackingFields (original, linked);

			foreach (var f in original.Fields) {
				if (verifiedGeneratedFields.Contains (f.FullName))
					continue;
				VerifyField (f, linked.Fields.FirstOrDefault (l => f.Name == l.Name));
				linkedMembers.Remove (f.FullName);
			}
#endif

			foreach (var m in original.Methods) {
				if (verifiedEventMethods.Contains (m.FullName))
					continue;

				AssemblyQualifiedToken token = new (m);
				linkedMembers.TryGetValue (
					token,
					out LinkedEntity? linkedMember);

				foreach(var err in VerifyMethod (m, linkedMember))
					yield return err;
				linkedMembers.Remove (token);
			}
		}

		private IEnumerable<string> VerifyBaseType (TypeDefinition src, TypeDefinition linked)
		{
			string expectedBaseName;
			var expectedBaseGenericAttr = src.CustomAttributes.FirstOrDefault (w => w.AttributeType.Name == nameof (KeptBaseTypeAttribute) && w.ConstructorArguments.Count > 1);
			if (expectedBaseGenericAttr != null) {
				expectedBaseName = FormatBaseOrInterfaceAttributeValue (expectedBaseGenericAttr);
			} else {
				var defaultBaseType = src.IsEnum ? "System.Enum" : src.IsValueType ? "System.ValueType" : "System.Object";
				expectedBaseName = GetCustomAttributeCtorValues<object> (src, nameof (KeptBaseTypeAttribute)).FirstOrDefault ()?.ToString () ?? defaultBaseType;
			}

			if (expectedBaseName != linked.BaseType?.FullName) {
				yield return $"Incorrect base type on : {linked.Name}. Expected {expectedBaseName}, actual {linked.BaseType?.FullName}";
			}
		}

		private IEnumerable<string> VerifyInterfaces (TypeDefinition src, TypeDefinition linked)
		{
			var expectedInterfaces = new HashSet<string> (src.CustomAttributes
				.Where (w => w.AttributeType.Name == nameof (KeptInterfaceAttribute))
				.Select (FormatBaseOrInterfaceAttributeValue));
			if (expectedInterfaces.Count == 0) {
				if (linked.HasInterfaces)
				yield return $"Type `{src}' has unexpected interfaces";
			} else {
				foreach (var iface in linked.Interfaces) {
					if (!expectedInterfaces.Remove (iface.InterfaceType.FullName)) {
						if (true != expectedInterfaces.Remove (iface.InterfaceType.Resolve ().FullName))
							yield return $"Type `{src}' interface `{iface.InterfaceType.Resolve ().FullName}' should have been removed";
					}
				}

				if (expectedInterfaces.Count != 0)
					yield return $"Expected interfaces were not found on {src}";
			}
		}

		private static string FormatBaseOrInterfaceAttributeValue (CustomAttribute attr)
		{
			if (attr.ConstructorArguments.Count == 1)
				return attr.ConstructorArguments[0].Value.ToString ()!;

			StringBuilder builder = new StringBuilder ();
			builder.Append (attr.ConstructorArguments[0].Value);
			builder.Append ('<');
			bool separator = false;
			foreach (var caa in (CustomAttributeArgument[]) attr.ConstructorArguments[1].Value) {
				if (separator)
					builder.Append (',');
				else
					separator = true;

				var arg = (CustomAttributeArgument) caa.Value;
				builder.Append (arg.Value);
			}

			builder.Append ('>');
			return builder.ToString ();
		}

		private IEnumerable<string> VerifyField (FieldDefinition src, FieldDesc? linked)
		{
			bool compilerGenerated = IsCompilerGeneratedMember (src);
			bool expectedKept = ShouldBeKept (src) | compilerGenerated;

			if (!expectedKept) {
				if (linked != null)
					yield return $"Field `{src}' should have been removed";

				yield break;
			}

			foreach(var err in VerifyFieldKept (src, linked, compilerGenerated))
				yield return err;
		}

		private static IEnumerable<string> VerifyFieldKept (FieldDefinition src, FieldDesc? linked, bool compilerGenerated)
		{
			if (linked == null) {
				yield return $"Field `{src}' should have been kept";
				yield break;
			}


			if (src.HasConstant)
				throw new NotImplementedException ("Constant value for a field is not yet supported by the test infra.");
#if false
			if (!Equals (src.Constant, linked.Constant)) {
				yield return $"Field '{src}' value doesn's match. Expected {src.Constant}, actual {linked.Constant}";
			}
#endif

#if false
			foreach(var err in VerifyPseudoAttributes (src, linked))
				yield return err;
			if (!compilerGenerated)
				foreach(var err in VerifyCustomAttributes (src, linked))
					yield return err;
#endif
		}

		private IEnumerable<string> VerifyProperty (PropertyDefinition src, LinkedEntity? linkedEntity, TypeDesc linkedType)
		{
			PropertyPseudoDesc? linked = linkedEntity?.Entity as PropertyPseudoDesc;
			VerifyMemberBackingField (src, linkedType);

			bool compilerGenerated = IsCompilerGeneratedMember (src);
			bool expectedKept = ShouldBeKept (src) || compilerGenerated;

			if (!expectedKept) {
				if (linked is not null)
					yield return $"Property `{src}' should have been removed";

				yield break;
			}

			if (linked is null) {
				yield return $"Property `{src}' should have been kept";
				yield break;
			}

			if (src.HasConstant)
				throw new NotSupportedException ("Constant value for a property is not yet supported by the test infra.");
#if false
			if (src.Constant != linked.Constant) {
				yield return $"Property '{src}' value doesn's match. Expected {src.Constant}, actual {linked.Constant}";
			}
#endif

#if false
			foreach(var err in VerifyPseudoAttributes (src, linked))
					yield return err;
			if (!compilerGenerated)
			{
				foreach(var err in VerifyCustomAttributes (src, linked))
					yield return err;
			}
#endif
		}

		private IEnumerable<string> VerifyEvent (EventDefinition src, LinkedEntity? linkedEntity, TypeDesc linkedType)
		{
			EventPseudoDesc? linked = linkedEntity?.Entity as EventPseudoDesc;
			foreach(var err in VerifyMemberBackingField (src, linkedType))
				yield return err;

			bool compilerGenerated = IsCompilerGeneratedMember (src);
			bool expectedKept = ShouldBeKept (src) | compilerGenerated;

			if (!expectedKept) {
				if (linked is not null)
					yield return $"Event `{src}' should have been removed";

				yield break;
			}

			if (linked is null) {
				yield return $"Event `{src}' should have been kept";
				yield break;
			}

			if (src.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (KeptEventAddMethodAttribute))) {
				// TODO: This is wrong - we can't validate that the method is present by looking at linked (as that is not actually linked)
				//   we need to look into linkedMembers to see if the method was actually preserved by the compiler (and has an entry point)
				foreach(var err in VerifyMethodInternal (src.AddMethod, new LinkedMethodEntity(linked.AddMethod, false), true, compilerGenerated))
					yield return err;
				verifiedEventMethods.Add (src.AddMethod.FullName);
				linkedMembers.Remove (new AssemblyQualifiedToken (src.AddMethod));
			}

			if (src.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (KeptEventRemoveMethodAttribute))) {
				// TODO: This is wrong - we can't validate that the method is present by looking at linked (as that is not actually linked)
				//   we need to look into linkedMembers to see if the method was actually preserved by the compiler (and has an entry point)
				foreach(var err in VerifyMethodInternal (src.RemoveMethod, new LinkedMethodEntity(linked.RemoveMethod, false), true, compilerGenerated))
					yield return err;
				verifiedEventMethods.Add (src.RemoveMethod.FullName);
				linkedMembers.Remove (new AssemblyQualifiedToken (src.RemoveMethod));
			}

#if false
			foreach(var err in VerifyPseudoAttributes (src, linked))
				yield return err;
			if (!compilerGenerated)
			{
				foreach(var err in VerifyCustomAttributes (src, linned))
					yield return err;
			}
#endif
		}

		private IEnumerable<string> VerifyMethod (MethodDefinition src, LinkedEntity? linkedEntity)
		{
			LinkedMethodEntity? linked = linkedEntity as LinkedMethodEntity;
			bool compilerGenerated = IsCompilerGeneratedMember (src);
			bool expectedKept = ShouldMethodBeKept (src);
			foreach(var err in VerifyMethodInternal (src, linked, expectedKept, compilerGenerated))
				yield return err;
		}

		private IEnumerable<string> VerifyMethodInternal (MethodDefinition src, LinkedMethodEntity? linked, bool expectedKept, bool compilerGenerated)
		{
			if (!expectedKept) {
				if (linked == null)
					yield break;

				// Similar to comment on types, compiler-generated methods can't be annotated with Kept attribute directly
				// so we're not going to validate kept/remove on them. Note that we're still going to go validate "into" them
				// to check for other properties (like parameter name presence/removal for example)
				if (!compilerGenerated)
					yield return $"Method `{NameUtils.GetExpectedOriginDisplayName (src)}' should have been removed";
			}

			foreach(var err in VerifyMethodKept (src, linked, compilerGenerated))
				yield return err;
		}

		private IEnumerable<string> VerifyMemberBackingField (IMemberDefinition src, TypeDesc? linkedType)
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

			foreach(var err in VerifyFieldKept (srcField, linkedType?.GetFields ()?.FirstOrDefault (l => srcField.Name == l.Name), compilerGenerated: true))
				yield return err;
			verifiedGeneratedFields.Add (srcField.FullName);
			linkedMembers.Remove (new AssemblyQualifiedToken (srcField));
		}

		IEnumerable<string> VerifyMethodKept (MethodDefinition src, LinkedMethodEntity? linked, bool compilerGenerated)
		{
			if (linked == null) {
				yield return $"Method `{NameUtils.GetExpectedOriginDisplayName (src)}' should have been kept";
				yield break;
			}

#if false
			foreach(var err in VerifyPseudoAttributes (src, linked))
				yield return err;
			foreach(var err in VerifyGenericParameters (src, linked))
				yield return err;
			if (!compilerGenerated) {
				foreach(var err in VerifyCustomAttributes (src, linked))
					yield return err;
				foreach(var err in VerifyCustomAttributes (src.MethodReturnType, linked.MethodReturnType))
					yield return err;
			}
#endif
			foreach(var err in VerifyParameters (src, linked))
					yield return err;
#if false
			foreach(var err in VerifySecurityAttributes (src, linked))
				yield return err;
			foreach(var err in VerifyArrayInitializers (src, linked))
				yield return err;

			// Method bodies are not very different in Native AOT
			foreach(var err in VerifyMethodBody (src, linked))
				yield return err;
			foreach(var err in VerifyKeptByAttributes (src, linked))
				yield return err;
#endif
		}

		protected virtual IEnumerable<string> VerifyMethodBody (MethodDefinition src, MethodDefinition linked)
		{
			if (!src.HasBody)
				yield break;

			foreach(var err in VerifyInstructions (src, linked))
				yield return err;
			foreach(var err in VerifyLocals (src, linked))
				yield return err;
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
				attr => GetStringArrayAttributeValue (attr)!.ToArray ()))
			{
				yield return err;
			}
		}

		public static string[] FormatMethodBody (MethodBody body)
		{
			List<(Instruction?, string)> result = new List<(Instruction?, string)> (body.Instructions.Count);
			for (int index = 0; index < body.Instructions.Count; index++) {
				var instruction = body.Instructions[index];
				result.Add ((instruction, FormatInstruction (instruction)));
			}

			HashSet<(Instruction, Instruction)> existingTryBlocks = new HashSet<(Instruction, Instruction)> ();
			foreach (var exHandler in body.ExceptionHandlers) {
				if (existingTryBlocks.Add ((exHandler.TryStart, exHandler.TryEnd!))) {
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

		private static string FormatInstruction (Instruction instr)
		{
			switch (instr.OpCode.FlowControl) {
			case FlowControl.Branch:
			case FlowControl.Cond_Branch:
				if (instr.Operand is Instruction target)
					return $"{instr.OpCode} il_{target.Offset:x}";

				break;
			}

			switch (instr.OpCode.Code) {
			case Code.Ldc_I4:
				if (instr.Operand is int ivalue)
					return $"{instr.OpCode} 0x{ivalue:x}";

				throw new NotImplementedException (instr.Operand.GetType ().ToString ());
			case Code.Ldc_I4_S:
				if (instr.Operand is sbyte bvalue)
					return $"{instr.OpCode} 0x{bvalue:x}";

				throw new NotImplementedException (instr.Operand.GetType ().ToString ());
			case Code.Ldc_I8:
				if (instr.Operand is long lvalue)
					return $"{instr.OpCode} 0x{lvalue:x}";

				throw new NotImplementedException (instr.Operand.GetType ().ToString ());

			case Code.Ldc_R4:
				if (instr.Operand is float fvalue)
					return $"{instr.OpCode} {fvalue}";

				throw new NotImplementedException (instr.Operand.GetType ().ToString ());

			case Code.Ldc_R8:
				if (instr.Operand is double dvalue)
					return $"{instr.OpCode} {dvalue}";

				throw new NotImplementedException (instr.Operand.GetType ().ToString ());

			case Code.Ldstr:
				if (instr.Operand is string svalue)
					return $"{instr.OpCode} '{svalue}'";

				throw new NotImplementedException (instr.Operand.GetType ().ToString ());

			default: {
					string? operandString = null;
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
						return $"{instr.OpCode} {operandString}";
					else
						return instr.OpCode.ToString ();
				}
			}
		}

		private static IEnumerable<string> VerifyLocals (MethodDefinition src, MethodDefinition linked)
		{
			foreach(var err in VerifyBodyProperties (
				src,
				linked,
				nameof (ExpectedLocalsSequenceAttribute),
				nameof (ExpectLocalsModifiedAttribute),
				"locals",
				m => m.Body.Variables.Select (v => v.VariableType.ToString ()).ToArray (),
				attr => GetStringOrTypeArrayAttributeValue (attr).ToArray ()))
			{
				yield return err;
			}
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
				if (linkedValues.SequenceEqual(srcValues))
				{
					yield return $"Expected method `{src} to have {propertyDescription} modified, however, the {propertyDescription} were the same as the original\n{FormattingUtils.FormatSequenceCompareFailureMessage (linkedValues, srcValues)}";
				}
			} else if (expectedSequenceAttribute != null) {
				var expected = getExpectFromSequenceAttribute (expectedSequenceAttribute).ToArray ();
				if (!linkedValues.SequenceEqual(expected))
				{
					yield return $"Expected method `{src} to have it's {propertyDescription} modified, however, the sequence of {propertyDescription} does not match the expected value\n{FormattingUtils.FormatSequenceCompareFailureMessage2 (linkedValues, expected, srcValues)}";
				}
			} else {
				if (!linkedValues.SequenceEqual(srcValues))
				{
					yield return $"Expected method `{src} to have it's {propertyDescription} unchanged, however, the {propertyDescription} differ from the original\n{FormattingUtils.FormatSequenceCompareFailureMessage (linkedValues, srcValues)}";
				}
			}
		}

		private IEnumerable<string> VerifyReferences (AssemblyDefinition original, AssemblyDefinition linked)
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
				yield break;

			var actual = linked.MainModule.AssemblyReferences
				.Select (name => name.Name)
				.ToArray ();

			if (!actual.SequenceEqual(expected))
				yield return $"Expected references do not match actual references:\n\tExpected: {string.Join(", ", expected)}\n\tActual: {string.Join(", ", actual)}";
		}

		private string? ReduceAssemblyFileNameOrNameToNameOnly (string? fileNameOrAssemblyName)
		{
			if (fileNameOrAssemblyName == null)
				return null;

			if (fileNameOrAssemblyName.EndsWith (".dll") || fileNameOrAssemblyName.EndsWith (".exe") || fileNameOrAssemblyName.EndsWith (".winmd"))
				return System.IO.Path.GetFileNameWithoutExtension (fileNameOrAssemblyName);

			// It must already be just the assembly name
			return fileNameOrAssemblyName;
		}

		private IEnumerable<string> VerifyResources (AssemblyDefinition original, AssemblyDefinition linked)
		{
			List<string?> expectedResourceNames = original.MainModule.AllDefinedTypes ()
				.SelectMany (t => GetCustomAttributeCtorValues<string> (t, nameof (KeptResourceAttribute)))
				.ToList ();

			foreach (var resource in linked.MainModule.Resources) {
				if (!expectedResourceNames.Remove (resource.Name))
					yield return $"Resource '{resource.Name}' should be removed.";

				EmbeddedResource embeddedResource = (EmbeddedResource) resource;

				var expectedResource = (EmbeddedResource) original.MainModule.Resources.First (r => r.Name == resource.Name);

				if (!embeddedResource.GetResourceData().SequenceEqual(expectedResource.GetResourceData()))
					yield return $"Resource '{resource.Name}' data doesn't match.";
			}

			if (expectedResourceNames.Count > 0) {
				yield return $"Resource '{expectedResourceNames.First ()}' should be kept.";
			}
		}

		private IEnumerable<string> VerifyExportedTypes (AssemblyDefinition original, AssemblyDefinition linked)
		{
			var expectedTypes = original.MainModule.AllDefinedTypes ()
				.SelectMany (t => GetCustomAttributeCtorValues<TypeReference> (t, nameof (KeptExportedTypeAttribute)).Select (l => l?.FullName ?? "<null>")).ToArray ();

			if (!linked.MainModule.ExportedTypes.Select (l => l.FullName).SequenceEqual(expectedTypes))
				yield return $"Exported types do not match expected values";
		}

		protected virtual IEnumerable<string> VerifyPseudoAttributes (MethodDefinition src, MethodDefinition linked)
		{
			var expected = (MethodAttributes) GetExpectedPseudoAttributeValue (src, (uint) src.Attributes);
			if(!linked.Attributes.Equals(expected))
			{
				yield return $"Method `{src}' pseudo attributes did not match expected";
			}
		}

		protected virtual IEnumerable<string> VerifyPseudoAttributes (TypeDefinition src, TypeDefinition linked)
		{
			var expected = (TypeAttributes) GetExpectedPseudoAttributeValue (src, (uint) src.Attributes);

			if(!linked.Attributes.Equals(expected))
			{
				yield return $"Type `{src}' pseudo attributes did not match expected";
			}
		}

		protected virtual IEnumerable<string> VerifyPseudoAttributes (FieldDefinition src, FieldDefinition linked)
		{
			var expected = (FieldAttributes) GetExpectedPseudoAttributeValue (src, (uint) src.Attributes);
			if(!linked.Attributes.Equals(expected))
			{
				yield return $"Field `{src}' pseudo attributes did not match expected";
			}
		}

		protected virtual IEnumerable<string> VerifyPseudoAttributes (PropertyDefinition src, PropertyDefinition linked)
		{
			var expected = (PropertyAttributes) GetExpectedPseudoAttributeValue (src, (uint) src.Attributes);
			if(!linked.Attributes.Equals(expected))
			{
				yield return $"Property `{src}' pseudo attributes did not match expected";
			}

		}

		protected virtual IEnumerable<string> VerifyPseudoAttributes (EventDefinition src, EventDefinition linked)
		{
			var expected = (EventAttributes) GetExpectedPseudoAttributeValue (src, (uint) src.Attributes);
			if(!linked.Attributes.Equals(expected))
			{
				yield return $"Event `{src}' pseudo attributes did not match expected";
			}
		}

		protected virtual IEnumerable<string> VerifyCustomAttributes (ICustomAttributeProvider src, ICustomAttributeProvider linked)
		{
			var expectedAttrs = GetExpectedAttributes (src).ToList ();
			var linkedAttrs = FilterLinkedAttributes (linked).ToList ();

			if(!linkedAttrs.SequenceEqual(expectedAttrs))
			{
				yield return $"Custom attributes on `{src}' are not matching";
			}
		}

		protected virtual IEnumerable<string> VerifySecurityAttributes (ICustomAttributeProvider src, ISecurityDeclarationProvider linked)
		{
			var expectedAttrs = GetCustomAttributeCtorValues<object> (src, nameof (KeptSecurityAttribute))
				.Select (attr => attr?.ToString () ?? "<null>")
				.ToList ();

			var linkedAttrs = FilterLinkedSecurityAttributes (linked).ToList ();

			if(!linkedAttrs.SequenceEqual(expectedAttrs))
			{
				yield return $"Security attributes on `{src}' are not matching";
			}
		}

#if false
		protected virtual IEnumerable<string> VerifyArrayInitializers (MethodDefinition src, MethodDefinition linked)
		{
			var expectedIndices = GetCustomAttributeCtorValues<object> (src, nameof (KeptInitializerData))
				.Cast<int> ()
				.ToArray ();

			var expectKeptAll = src.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (KeptInitializerData) && !attr.HasConstructorArguments);

			if (expectedIndices.Length == 0 && !expectKeptAll)
				return;

			if (!src.HasBody)
				yield return $"`{nameof (KeptInitializerData)}` cannot be used on methods that don't have bodies";

			var srcImplementationDetails = src.Module.Types.FirstOrDefault (t => string.IsNullOrEmpty (t.Namespace) && t.Name.StartsWith ("<PrivateImplementationDetails>"));

			if (srcImplementationDetails == null) {
				yield return "Could not locate <PrivateImplementationDetails> in the original assembly.  Does your test use initializers?";
				return;
			}

			var linkedImplementationDetails = linked.Module.Types.FirstOrDefault (t => string.IsNullOrEmpty (t.Namespace) && t.Name.StartsWith ("<PrivateImplementationDetails>"));

			if (linkedImplementationDetails == null) {
				yield return "Could not locate <PrivateImplementationDetails> in the linked assembly";
				return;
			}

			var possibleInitializerFields = src.Body.Instructions
				.Where (ins => IsLdtokenOnPrivateImplementationDetails (srcImplementationDetails, ins))
				.Select (ins => ((FieldReference) ins.Operand).Resolve ())
				.ToArray ();

			if (possibleInitializerFields.Length == 0)
				yield return $"`{src}` does not make use of any initializers";

			if (expectKeptAll) {
				foreach (var srcField in possibleInitializerFields) {
					var linkedField = linkedImplementationDetails.Fields.FirstOrDefault (f => f.InitialValue.SequenceEqual (srcField.InitialValue));
					foreach(var err in VerifyInitializerField (srcField, linkedField))
						yield return err;
				}
			} else {
				foreach (var index in expectedIndices) {
					if (index < 0 || index > possibleInitializerFields.Length)
						yield return $"Invalid expected index `{index}` in {src}.  Value must be between 0 and {expectedIndices.Length}";

					var srcField = possibleInitializerFields[index];
					var linkedField = linkedImplementationDetails.Fields.FirstOrDefault (f => f.InitialValue.SequenceEqual (srcField.InitialValue));

					foreach(var err in VerifyInitializerField (srcField, linkedField))
						yield return err;
				}
			}
		}

		private IEnumerable<string> VerifyInitializerField (FieldDefinition src, FieldDefinition? linked)
		{
			foreach(var err in VerifyFieldKept (src, linked))
					yield return err;
			verifiedGeneratedFields.Add (linked!.FullName);
			linkedMembers.Remove (new (linked));
			// foreach(var err in VerifyTypeDefinitionKept (src.FieldType.Resolve (), linked.FieldType.Resolve ()))
			//     yield return err;
			linkedMembers.Remove (new (linked.FieldType.Resolve ()));
			linkedMembers.Remove (new (linked.DeclaringType.Resolve ()));
			verifiedGeneratedTypes.Add (linked.DeclaringType.FullName);
		}
#endif

		private static bool IsLdtokenOnPrivateImplementationDetails (TypeDefinition privateImplementationDetails, Instruction instruction)
		{
			if (instruction.OpCode.Code == Code.Ldtoken && instruction.Operand is FieldReference field) {
				return field.DeclaringType.Resolve () == privateImplementationDetails;
			}

			return false;
		}

		protected static IEnumerable<string?> GetExpectedAttributes (ICustomAttributeProvider original)
		{
			foreach (var expectedAttrs in GetCustomAttributeCtorValues<object> (original, nameof (KeptAttributeAttribute)))
				yield return expectedAttrs?.ToString ();

			// The name of the generated fixed buffer type is a little tricky.
			// Some versions of csc name it `<fieldname>e__FixedBuffer0`
			// while mcs and other versions of csc name it `<fieldname>__FixedBuffer0`
			if (original is TypeDefinition srcDefinition && srcDefinition.Name.Contains ("__FixedBuffer")) {
				var name = srcDefinition.Name.Substring (1, srcDefinition.Name.IndexOf ('>') - 1);
				var fixedField = srcDefinition.DeclaringType.Fields.FirstOrDefault (f => f.Name == name);
				if (fixedField == null)
					Assert.Fail($"Could not locate original fixed field for {srcDefinition}");

				foreach (var additionalExpectedAttributesFromFixedField in GetCustomAttributeCtorValues<object> (fixedField!, nameof (KeptAttributeOnFixedBufferTypeAttribute)))
					yield return additionalExpectedAttributesFromFixedField?.ToString ();

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

#if false
		private IEnumerable<string> VerifyFixedBufferFields (TypeDefinition src, TypeDefinition linked)
		{
			var fields = src.Fields.Where (f => f.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (KeptFixedBufferAttribute)));

			foreach (var field in fields) {
				// The name of the generated fixed buffer type is a little tricky.
				// Some versions of csc name it `<fieldname>e__FixedBuffer0`
				// while mcs and other versions of csc name it `<fieldname>__FixedBuffer0`
				var originalCompilerGeneratedBufferType = src.NestedTypes.FirstOrDefault (t => t.FullName.Contains ($"<{field.Name}>") && t.FullName.Contains ("__FixedBuffer"));
				if (originalCompilerGeneratedBufferType == null) {
					yield return $"Could not locate original compiler generated fixed buffer type for field {field}";
					yield break;
				}

				var linkedCompilerGeneratedBufferType = linked.NestedTypes.FirstOrDefault (t => t.Name == originalCompilerGeneratedBufferType.Name);
				if (linkedCompilerGeneratedBufferType == null) {
					yield return $"Missing expected type {originalCompilerGeneratedBufferType}";
					yield break;
				}

				// Have to verify the field before the type
				var originalElementField = originalCompilerGeneratedBufferType.Fields.FirstOrDefault ();
				if (originalElementField == null) {
					yield return $"Could not locate original compiler generated FixedElementField on {originalCompilerGeneratedBufferType}";
					yield break;
				}

				var linkedField = linkedCompilerGeneratedBufferType?.Fields.FirstOrDefault ();
				foreach(var err in VerifyFieldKept (originalElementField, linkedField))
					yield return err;
				verifiedGeneratedFields.Add (originalElementField.FullName);
				linkedMembers.Remove (new (linkedField!));

				// foreach(var err in VerifyTypeDefinitionKept (originalCompilerGeneratedBufferType, linkedCompilerGeneratedBufferType))
				//     yield return err;
				verifiedGeneratedTypes.Add (originalCompilerGeneratedBufferType.FullName);
			}
		}

		private IEnumerable<string> VerifyDelegateBackingFields (TypeDefinition src, TypeDefinition linked)
		{
			var expectedFieldNames = GetCustomAttributeCtorValues<string> (src, nameof (KeptDelegateCacheFieldAttribute))
				.Select (unique => $"<>f__mg$cache{unique}")
				.ToList ();

			if (expectedFieldNames.Count == 0)
				yield break;

			foreach (var srcField in src.Fields) {
				if (!expectedFieldNames.Contains (srcField.Name))
					continue;

				var linkedField = linked?.Fields.FirstOrDefault (l => l.Name == srcField.Name);
				foreach(var err in VerifyFieldKept (srcField, linkedField))
					yield return err;
				verifiedGeneratedFields.Add (srcField.FullName);
				linkedMembers.Remove (new (srcField));
			}
		}
#endif

		private IEnumerable<string> VerifyGenericParameters (IGenericParameterProvider src, IGenericParameterProvider linked)
		{
			if (src.HasGenericParameters != linked.HasGenericParameters)
				yield return $"Mismatch in having generic paramters. Expected {src.HasGenericParameters}, actual {linked.HasGenericParameters}";

			if (src.HasGenericParameters) {
				for (int i = 0; i < src.GenericParameters.Count; ++i) {
					// TODO: Verify constraints
					var srcp = src.GenericParameters[i];
					var lnkp = linked.GenericParameters[i];
					foreach(var err in VerifyCustomAttributes (srcp, lnkp))
						yield return err;

					if (checkNames) {
						if (srcp.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (RemovedNameValueAttribute))) {
							string name = (src.GenericParameterType == GenericParameterType.Method ? "!!" : "!") + srcp.Position;
							if (lnkp.Name != name)
								yield return "Expected empty generic parameter name";
						} else {
							if (lnkp.Name != srcp.Name)
								yield return "Mismatch in generic parameter name";
						}
					}
				}
			}
		}

		private IEnumerable<string> VerifyParameters (IMethodSignature src, LinkedMethodEntity linked)
		{
			if (src.HasParameters != linked.Method.Signature.Length > 0)
				yield return $"Mismatch in having parameters in {src as MethodDefinition}: Expected {src.HasParameters}, actual {linked.Method.Signature.Length > 0}";
			if (src.HasParameters) {
				for (int i = 0; i < src.Parameters.Count; ++i) {
					var srcp = src.Parameters[i];
					//var lnkp = linked.Parameters[i];

#if false
					foreach(var err in VerifyCustomAttributes (srcp, lnkp))
						yield return err;
#endif

					if (checkNames) {
						if (srcp.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (RemovedNameValueAttribute)))
						{
							if (linked.IsReflected != false)
								yield return $"Expected no parameter name (non-reflectable). Parameter {i} of {src as MethodDefinition}";
						}
						else
						{
							if (linked.IsReflected != true)
								yield return $"Expected accessible parameter name (reflectable). Parameter {i} of {(src as MethodDefinition)}";
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

		protected virtual bool ShouldBeKept<T> (T member, string? signature = null) where T : MemberReference, ICustomAttributeProvider
		{
			if (HasActiveKeptAttribute (member))
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

		protected static uint GetExpectedPseudoAttributeValue (ICustomAttributeProvider provider, uint sourceValue)
		{
			var removals = provider.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (RemovedPseudoAttributeAttribute)).ToArray ();
			var adds = provider.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (AddedPseudoAttributeAttribute)).ToArray ();

			return removals.Aggregate (sourceValue, (accum, item) => accum & ~(uint) item.ConstructorArguments[0].Value) |
				adds.Aggregate ((uint) 0, (acum, item) => acum | (uint) item.ConstructorArguments[0].Value);
		}

		protected static IEnumerable<T?> GetCustomAttributeCtorValues<T> (ICustomAttributeProvider provider, string attributeName) where T : class
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

		protected static IEnumerable<string>? GetStringArrayAttributeValue (CustomAttribute attribute)
		{
			return ((CustomAttributeArgument[]) attribute.ConstructorArguments[0].Value)?.Select (arg => arg.Value.ToString ()!);
		}

		private static IEnumerable<CustomAttribute> GetActiveKeptAttributes (ICustomAttributeProvider provider, string attributeName)
		{
			return provider.CustomAttributes.Where (ca => {
				if (ca.AttributeType.Name != attributeName) {
					return false;
				}

				object? keptBy = ca.GetPropertyValue (nameof (KeptAttribute.By));
				return keptBy is null ? true : ((Tool) keptBy).HasFlag (Tool.NativeAot);
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

				object? keptBy = ca.GetPropertyValue (nameof (KeptAttribute.By));
				return keptBy is null ? true : ((Tool) keptBy).HasFlag (Tool.NativeAot);
			});
		}


		private static bool HasActiveKeptDerivedAttribute (ICustomAttributeProvider provider)
		{
			return GetActiveKeptDerivedAttributes (provider).Any ();
		}

		internal IEnumerable<string> VerifyLinkingOfOtherAssemblies (AssemblyDefinition original)
		{
			var checks = BuildOtherAssemblyCheckTable (original);
			List<string> errs = [];

			try {
				foreach (var assemblyName in checks.Keys) {
					var linkedMembersInAssembly = ResolveLinkedMembersForAssembly (assemblyName);
					var originalTargetAssembly = ResolveOriginalsAssembly(assemblyName);
					foreach (var checkAttrInAssembly in checks[assemblyName]) {
						var attributeTypeName = checkAttrInAssembly.AttributeType.Name;

						switch (attributeTypeName) {
						case nameof (KeptAllTypesAndMembersInAssemblyAttribute):
							errs.AddRange(VerifyKeptAllTypesAndMembersInAssembly (assemblyName, linkedMembersInAssembly));
							continue;
						case nameof (KeptAttributeInAssemblyAttribute):
							// errs.AddRange(VerifyKeptAttributeInAssembly (checkAttrInAssembly, linkedAssembly))
							continue;
						case nameof (RemovedAttributeInAssembly):
							// errs.AddRange(VerifyRemovedAttributeInAssembly (checkAttrInAssembly, linkedAssembly))
							continue;
						default:
							break;
						}

						var expectedTypeName = checkAttrInAssembly.ConstructorArguments[1].Value.ToString ()!;
						var expectedType = originalTargetAssembly.MainModule.GetType(expectedTypeName);
						linkedMembersInAssembly.TryGetValue(new AssemblyQualifiedToken(expectedType), out LinkedEntity? linkedTypeEntity);
						MetadataType? linkedType = linkedTypeEntity?.Entity as MetadataType;

#if false
						if (linkedType == null && linkedAssembly.MainModule.HasExportedTypes) {
							ExportedType? exportedType = linkedAssembly.MainModule.ExportedTypes
									.FirstOrDefault (exported => exported.FullName == expectedTypeName);

							// Note that copied assemblies could have dangling references.
							if (exportedType != null && original.EntryPoint.DeclaringType.CustomAttributes.FirstOrDefault (
								ca => ca.AttributeType.Name == nameof (RemovedAssemblyAttribute)
								&& ca.ConstructorArguments[0].Value.ToString () == exportedType.Scope.Name + ".dll") != null)
								continue;

							linkedType = exportedType?.Resolve ();
						}
#endif

						switch (attributeTypeName) {
						case nameof (RemovedTypeInAssemblyAttribute):
							if (linkedType != null)
								errs.Add($"Type `{expectedTypeName}' should have been removed from assembly {assemblyName}");
							GetOriginalTypeFromInAssemblyAttribute (checkAttrInAssembly);
							break;
						case nameof (KeptTypeInAssemblyAttribute):
							if (linkedType == null)
								errs.Add($"Type `{expectedTypeName}' should have been kept in assembly {assemblyName}");
							break;
#if false
						case nameof (RemovedInterfaceOnTypeInAssemblyAttribute):
							if (linkedType == null)
							{
								errs.Add($"Type `{expectedTypeName}' should have been kept in assembly {assemblyName}");
								break;
							}
							errs.AddRange(VerifyRemovedInterfaceOnTypeInAssembly (checkAttrInAssembly, linkedType));
							break;
						case nameof (KeptInterfaceOnTypeInAssemblyAttribute):
							if (linkedType == null)
							{
								errs.Add($"Type `{expectedTypeName}' should have been kept in assembly {assemblyName}");
								break;
							}
							errs.AddRange(VerifyKeptInterfaceOnTypeInAssembly (checkAttrInAssembly, linkedType));
							break;
						case nameof (RemovedMemberInAssemblyAttribute):
							if (linkedType == null)
								continue;

							errs.AddRange(VerifyRemovedMemberInAssembly (checkAttrInAssembly, linkedType));
							break;
						case nameof (KeptBaseOnTypeInAssemblyAttribute):
							if (linkedType == null)
							{
								errs.Add($"Type `{expectedTypeName}' should have been kept in assembly {assemblyName}");
								break;
							}
							errs.AddRange(VerifyKeptBaseOnTypeInAssembly (checkAttrInAssembly, linkedType));
							break;
						case nameof (KeptMemberInAssemblyAttribute):
							if (linkedType == null)
							{
								errs.Add($"Type `{expectedTypeName}' should have been kept in assembly {assemblyName}");
								break;
							}

							errs.AddRange(VerifyKeptMemberInAssembly (checkAttrInAssembly, linkedType));
							break;
						case nameof (RemovedForwarderAttribute):
							if (linkedAssembly.MainModule.ExportedTypes.Any (l => l.Name == expectedTypeName))
								errs.Add($"Forwarder `{expectedTypeName}' should have been removed from assembly {assemblyName}");

							break;

						case nameof (RemovedAssemblyReferenceAttribute):
							if (linkedAssembly.MainModule.AssemblyReferences.Any (l => l.Name == expectedTypeName) != false)
								errs.Add($"AssemblyRef '{expectedTypeName}' should have been removed from assembly {assemblyName}");
							break;

						case nameof (KeptResourceInAssemblyAttribute):
							errs.AddRange(VerifyKeptResourceInAssembly (checkAttrInAssembly));
							break;
						case nameof (RemovedResourceInAssemblyAttribute):
							errs.AddRange(VerifyRemovedResourceInAssembly (checkAttrInAssembly));
							break;
						case nameof (KeptReferencesInAssemblyAttribute):
							errs.AddRange(VerifyKeptReferencesInAssembly (checkAttrInAssembly))
							break;
						case nameof (ExpectedInstructionSequenceOnMemberInAssemblyAttribute):
							if (linkedType == null)
							{
								errs.Add($"Type `{expectedTypeName}` should have been kept in assembly {assemblyName}");
								break;
							}
							errs.AddRange(VerifyExpectedInstructionSequenceOnMemberInAssembly (checkAttrInAssembly, linkedType));
							break;
						default:
							UnhandledOtherAssemblyAssertion (expectedTypeName, checkAttrInAssembly, linkedType);
							break;
#else
						default:
							break;
#endif
						}
					}
				}
			} catch (AssemblyResolutionException e) {
				errs.Add($"Failed to resolve linked assembly `{e.AssemblyReference.Name}`.  It must not exist in the output.");
			}
			return errs;
		}

		private IEnumerable<string> VerifyKeptAttributeInAssembly (CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly)
		{
			return VerifyAttributeInAssembly (inAssemblyAttribute, linkedAssembly, VerifyCustomAttributeKept);
		}

		private IEnumerable<string> VerifyRemovedAttributeInAssembly (CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly)
		{
			return VerifyAttributeInAssembly (inAssemblyAttribute, linkedAssembly, VerifyCustomAttributeRemoved);
		}

		private IEnumerable<string> VerifyAttributeInAssembly (CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly, Func<ICustomAttributeProvider, string, IEnumerable<string>> assertExpectedAttribute)
		{
			var assemblyName = (string) inAssemblyAttribute.ConstructorArguments[0].Value!;
			string expectedAttributeTypeName;
			var attributeTypeOrTypeName = inAssemblyAttribute.ConstructorArguments[1].Value!;
			if (attributeTypeOrTypeName is TypeReference typeReference) {
				expectedAttributeTypeName = typeReference.FullName;
			} else {
				expectedAttributeTypeName = attributeTypeOrTypeName.ToString ()!;
			}

			if (inAssemblyAttribute.ConstructorArguments.Count == 2) {
				// Assembly
				foreach(var err in assertExpectedAttribute (linkedAssembly, expectedAttributeTypeName))
					yield return err;
				yield break;
			}

			// We are asserting on type or member
			var typeOrTypeName = inAssemblyAttribute.ConstructorArguments[2].Value;
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ()!, typeOrTypeName);
			if (originalType == null)
			{
				yield return $"Invalid test assertion. The original `{assemblyName}` does not contain a type `{typeOrTypeName}`";
				yield break;
			}

			var linkedType = linkedAssembly.MainModule.GetType (originalType.FullName);
			if (linkedType == null)
			{
				yield return $"Missing expected type `{typeOrTypeName}` in `{assemblyName}`";
				yield break;
			}

			if (inAssemblyAttribute.ConstructorArguments.Count == 3) {
				assertExpectedAttribute (linkedType, expectedAttributeTypeName);
				yield break;
			}

			// we are asserting on a member
			string memberName = (string) inAssemblyAttribute.ConstructorArguments[3].Value;

			// We will find the matching type from the original assembly first that way we can confirm
			// that the name defined in the attribute corresponds to a member that actually existed
			var originalFieldMember = originalType.Fields.FirstOrDefault (m => m.Name == memberName);
			if (originalFieldMember != null) {
				var linkedField = linkedType.Fields.FirstOrDefault (m => m.Name == memberName);
				if (linkedField == null)
				{
					yield return $"Field `{memberName}` on Type `{originalType}` should have been kept";
					yield break;
				}

				assertExpectedAttribute (linkedField, expectedAttributeTypeName);
				yield break;
			}

			var originalPropertyMember = originalType.Properties.FirstOrDefault (m => m.Name == memberName);
			if (originalPropertyMember != null) {
				var linkedProperty = linkedType.Properties.FirstOrDefault (m => m.Name == memberName);
				if (linkedProperty == null)
				{
					yield return $"Property `{memberName}` on Type `{originalType}` should have been kept";
					yield break;
				}

				foreach(var err in assertExpectedAttribute (linkedProperty, expectedAttributeTypeName))
					yield return err;
				yield break;
			}

			var originalMethodMember = originalType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
			if (originalMethodMember != null) {
				var linkedMethod = linkedType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
				if (linkedMethod == null)
				{
					yield return $"Method `{memberName}` on Type `{originalType}` should have been kept";
					yield break;
				}

				assertExpectedAttribute (linkedMethod, expectedAttributeTypeName);
				yield break;
			}

			yield return $"Invalid test assertion.  No member named `{memberName}` exists on the original type `{originalType}`";
		}

		private static IEnumerable<string> VerifyCopyAssemblyIsKeptUnmodified (NPath outputDirectory, string assemblyName)
		{
			string inputAssemblyPath = Path.Combine (Directory.GetParent (outputDirectory)!.ToString (), "input", assemblyName);
			string outputAssemblyPath = Path.Combine (outputDirectory, assemblyName);
			if (true != File.ReadAllBytes (inputAssemblyPath).SequenceEqual (File.ReadAllBytes (outputAssemblyPath)))
				yield return $"Expected assemblies\n" +
							 $"\t{inputAssemblyPath}\n" +
							 $"\t{outputAssemblyPath}\n" +
							 $"binaries to be equal, since the input assembly has copy action.";
		}

		private IEnumerable<string> VerifyCustomAttributeKept (ICustomAttributeProvider provider, string expectedAttributeTypeName)
		{
			var match = provider.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.FullName == expectedAttributeTypeName);
			if (match == null)
				yield return $"Expected `{provider}` to have an attribute of type `{expectedAttributeTypeName}`";
		}

		private IEnumerable<string> VerifyCustomAttributeRemoved (ICustomAttributeProvider provider, string expectedAttributeTypeName)
		{
			var match = provider.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.FullName == expectedAttributeTypeName);
			if (match != null)
				yield return $"Expected `{provider}` to no longer have an attribute of type `{expectedAttributeTypeName}`";
		}

		private IEnumerable<string> VerifyRemovedInterfaceOnTypeInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);

			var interfaceAssemblyName = inAssemblyAttribute.ConstructorArguments[2].Value.ToString ()!;
			var interfaceType = inAssemblyAttribute.ConstructorArguments[3].Value;

			var originalInterface = GetOriginalTypeFromInAssemblyAttribute (interfaceAssemblyName, interfaceType);
			if (!originalType.HasInterfaces)
				yield return "Invalid assertion.  Original type does not have any interfaces";

			var originalInterfaceImpl = GetMatchingInterfaceImplementationOnType (originalType, originalInterface.FullName);
			if (originalInterfaceImpl == null)
				yield return $"Invalid assertion.  Original type never had an interface of type `{originalInterface}`";

			var linkedInterfaceImpl = GetMatchingInterfaceImplementationOnType (linkedType, originalInterface.FullName);
			if (linkedInterfaceImpl != null)
				yield return $"Expected `{linkedType}` to no longer have an interface of type {originalInterface.FullName}";
		}

		private IEnumerable<string> VerifyKeptInterfaceOnTypeInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);

			var interfaceAssemblyName = inAssemblyAttribute.ConstructorArguments[2].Value.ToString ()!;
			var interfaceType = inAssemblyAttribute.ConstructorArguments[3].Value;

			var originalInterface = GetOriginalTypeFromInAssemblyAttribute (interfaceAssemblyName, interfaceType);
			if (!originalType.HasInterfaces)
				yield return "Invalid assertion.  Original type does not have any interfaces";

			var originalInterfaceImpl = GetMatchingInterfaceImplementationOnType (originalType, originalInterface.FullName);
			if (originalInterfaceImpl == null)
				yield return $"Invalid assertion.  Original type never had an interface of type `{originalInterface}`";

			var linkedInterfaceImpl = GetMatchingInterfaceImplementationOnType (linkedType, originalInterface.FullName);
			if (linkedInterfaceImpl == null)
				yield return $"Expected `{linkedType}` to have interface of type {originalInterface.FullName}";
		}

		private IEnumerable<string> VerifyKeptBaseOnTypeInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);

			var baseAssemblyName = inAssemblyAttribute.ConstructorArguments[2].Value.ToString ()!;
			var baseType = inAssemblyAttribute.ConstructorArguments[3].Value;

			var originalBase = GetOriginalTypeFromInAssemblyAttribute (baseAssemblyName, baseType);
			if (originalType.BaseType.Resolve () != originalBase)
				yield return "Invalid assertion.  Original type's base does not match the expected base";

			if (originalBase.FullName != linkedType.BaseType.FullName)
				yield return $"Incorrect base on `{linkedType.FullName}`.  Expected `{originalBase.FullName}` but was `{linkedType.BaseType.FullName}`";
		}

		private static InterfaceImplementation? GetMatchingInterfaceImplementationOnType (TypeDefinition type, string expectedInterfaceTypeName)
		{
			return type.Interfaces.FirstOrDefault (impl => {
				var resolvedImpl = impl.InterfaceType.Resolve ();

				if (resolvedImpl == null)
					Assert.Fail ($"Failed to resolve interface : `{impl.InterfaceType}` on `{type}`");

				return resolvedImpl.FullName == expectedInterfaceTypeName;
			});
		}

		private IEnumerable<string> VerifyRemovedMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);
			foreach (var memberNameAttr in (CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[2].Value) {
				string memberName = (string) memberNameAttr.Value;

				// We will find the matching type from the original assembly first that way we can confirm
				// that the name defined in the attribute corresponds to a member that actually existed
				var originalFieldMember = originalType.Fields.FirstOrDefault (m => m.Name == memberName);
				if (originalFieldMember != null) {
					var linkedField = linkedType.Fields.FirstOrDefault (m => m.Name == memberName);
					if (linkedField != null)
						yield return $"Field `{memberName}` on Type `{originalType}` should have been removed";

					continue;
				}

				var originalPropertyMember = originalType.Properties.FirstOrDefault (m => m.Name == memberName);
				if (originalPropertyMember != null) {
					var linkedProperty = linkedType.Properties.FirstOrDefault (m => m.Name == memberName);
					if (linkedProperty != null)
						yield return $"Property `{memberName}` on Type `{originalType}` should have been removed";

					continue;
				}

				var originalMethodMember = originalType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
				if (originalMethodMember != null) {
					var linkedMethod = linkedType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
					if (linkedMethod != null)
						yield return $"Method `{memberName}` on Type `{originalType}` should have been removed";

					continue;
				}

				yield return $"Invalid test assertion.  No member named `{memberName}` exists on the original type `{originalType}`";
			}
		}

		private IEnumerable<string> VerifyKeptMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);
			var memberNames = (CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[2].Value;
			if (!(memberNames.Length > 0))
				yield return "Invalid KeptMemberInAssemblyAttribute. Expected member names.";
			foreach (var memberNameAttr in memberNames) {
				string memberName = (string) memberNameAttr.Value;

				// We will find the matching type from the original assembly first that way we can confirm
				// that the name defined in the attribute corresponds to a member that actually existed

				if (TryVerifyKeptMemberInAssemblyAsField (memberName, originalType, linkedType))
					continue;

				if (TryVerifyKeptMemberInAssemblyAsProperty (memberName, originalType, linkedType))
					continue;

				if (TryVerifyKeptMemberInAssemblyAsMethod (memberName, originalType, linkedType))
					continue;

				yield return $"Invalid test assertion. No member named `{memberName}` exists on the original type `{originalType}`";
			}
		}

		protected virtual bool TryVerifyKeptMemberInAssemblyAsField (string memberName, TypeDefinition originalType, TypeDefinition linkedType)
		{
			var originalFieldMember = originalType.Fields.FirstOrDefault (m => m.Name == memberName);
			if (originalFieldMember != null) {
				var linkedField = linkedType.Fields.FirstOrDefault (m => m.Name == memberName);
				if (linkedField == null)
					Assert.Fail ($"Field `{memberName}` on Type `{originalType}` should have been kept");

				return true;
			}

			return false;
		}

		protected virtual bool TryVerifyKeptMemberInAssemblyAsProperty (string memberName, TypeDefinition originalType, TypeDefinition linkedType)
		{
			var originalPropertyMember = originalType.Properties.FirstOrDefault (m => m.Name == memberName);
			if (originalPropertyMember != null) {
				var linkedProperty = linkedType.Properties.FirstOrDefault (m => m.Name == memberName);
				if (linkedProperty == null)
					Assert.Fail ($"Property `{memberName}` on Type `{originalType}` should have been kept");

				return true;
			}

			return false;
		}

		protected virtual bool TryVerifyKeptMemberInAssemblyAsMethod (string memberName, TypeDefinition originalType, TypeDefinition linkedType)
		{
			return TryVerifyKeptMemberInAssemblyAsMethod (memberName, originalType, linkedType, out _, out _);
		}

		protected virtual bool TryVerifyKeptMemberInAssemblyAsMethod (string memberName, TypeDefinition originalType, TypeDefinition linkedType, out MethodDefinition? originalMethod, out MethodDefinition? linkedMethod)
		{
			originalMethod = originalType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
			if (originalMethod != null) {
				linkedMethod = linkedType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
				if (linkedMethod == null)
					Assert.Fail ($"Method `{memberName}` on Type `{originalType}` should have been kept");

				return true;
			}

			linkedMethod = null;
			return false;
		}

		private IEnumerable<string> VerifyKeptReferencesInAssembly (CustomAttribute inAssemblyAttribute)
		{
#if false
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ()!);
			var expectedReferenceNames = ((CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[1].Value).Select (attr => (string) attr.Value).ToList ();
			for (int i = 0; i < expectedReferenceNames.Count; i++)
				if (expectedReferenceNames[i].EndsWith (".dll"))
					expectedReferenceNames[i] = expectedReferenceNames[i].Substring (0, expectedReferenceNames[i].LastIndexOf ("."));

			Assert.Equal (assembly.MainModule.AssemblyReferences.Select (asm => asm.Name), expectedReferenceNames);
#endif
			yield break;
		}

		private IEnumerable<string> VerifyKeptResourceInAssembly (CustomAttribute inAssemblyAttribute)
		{
#if false
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ()!);
			var resourceName = inAssemblyAttribute.ConstructorArguments[1].Value.ToString ();

			Assert.Contains (resourceName, assembly.MainModule.Resources.Select (r => r.Name));
#endif
			yield break;
		}

		private IEnumerable<string> VerifyRemovedResourceInAssembly (CustomAttribute inAssemblyAttribute)
		{
#if false
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ()!);
			var resourceName = inAssemblyAttribute.ConstructorArguments[1].Value.ToString ();

			Assert.DoesNotContain (resourceName, assembly.MainModule.Resources.Select (r => r.Name));
#endif
			yield break;
		}

		private IEnumerable<string> VerifyKeptAllTypesAndMembersInAssembly (string assemblyName, Dictionary<AssemblyQualifiedToken, LinkedEntity> linkedMembers)
		{
			var original = ResolveOriginalsAssembly (assemblyName);

			if (original == null)
			{
				yield return $"Failed to resolve original assembly {assemblyName}";
				yield break;
			}

			var originalTypes = original.AllDefinedTypes ().ToDictionary (t => new AssemblyQualifiedToken(t));
			var linkedTypes = linkedMembers.Where(t => t.Value.Entity is TypeDesc).ToDictionary();

			var missingInLinked = originalTypes.Keys.Except (linkedTypes.Keys);

			if (missingInLinked.Any ())
				yield return $"Expected all types to exist in the linked assembly {assemblyName}, but one or more were missing";

			foreach (var originalKvp in originalTypes) {
				var linkedType = linkedTypes[originalKvp.Key];
				TypeDesc linkedTypeDesc = (TypeDesc)linkedType.Entity;

				// NativeAOT field trimming is very different (it basically doesn't trim fields, not in the same way trimmer does)
				var originalMembers = originalKvp.Value.AllMembers ().Where(m => m is not FieldDefinition).Select (m => new AssemblyQualifiedToken(m));
				var linkedMembersOnType = linkedMembers.Where(t => GetOwningType(t.Value.Entity) == linkedTypeDesc).Select(t => t.Key);

				var missingMembersInLinked = originalMembers.Except (linkedMembersOnType);

				if (missingMembersInLinked.Any ())
					yield return $"Expected all members of `{linkedTypeDesc.GetDisplayName()}`to exist in the linked assembly, but one or more were missing";
			}
		}

		private TypeDefinition GetOriginalTypeFromInAssemblyAttribute (CustomAttribute inAssemblyAttribute)
		{
			string assemblyName;
			if (inAssemblyAttribute.HasProperties && inAssemblyAttribute.Properties[0].Name == "ExpectationAssemblyName")
				assemblyName = inAssemblyAttribute.Properties[0].Argument.Value.ToString ()!;
			else
				assemblyName = inAssemblyAttribute.ConstructorArguments[0].Value.ToString ()!;

			return GetOriginalTypeFromInAssemblyAttribute (assemblyName, inAssemblyAttribute.ConstructorArguments[1].Value);
		}

		private TypeDefinition GetOriginalTypeFromInAssemblyAttribute (string assemblyName, object typeOrTypeName)
		{
			if (typeOrTypeName is TypeReference attributeValueAsTypeReference)
				return attributeValueAsTypeReference.Resolve ();

			var assembly = ResolveOriginalsAssembly (assemblyName);

			var expectedTypeName = typeOrTypeName.ToString ();
			var originalType = assembly.MainModule.GetType (expectedTypeName);
			if (originalType == null)
				Assert.Fail ($"Invalid test assertion.  Unable to locate the original type `{expectedTypeName}.`");
			return originalType;
		}

		private static Dictionary<string, List<CustomAttribute>> BuildOtherAssemblyCheckTable (AssemblyDefinition original)
		{
			var checks = new Dictionary<string, List<CustomAttribute>> ();

			foreach (var typeWithRemoveInAssembly in original.AllDefinedTypes ()) {
				foreach (var attr in typeWithRemoveInAssembly.CustomAttributes.Where (IsTypeInOtherAssemblyAssertion)) {
					var assemblyName = (string) attr.ConstructorArguments[0].Value;

					Tool? toolTarget = (Tool?)(int?)attr.GetPropertyValue("Tool");
					if (toolTarget is not null && !toolTarget.Value.HasFlag(Tool.NativeAot))
						continue;

					if (!checks.TryGetValue (assemblyName, out List<CustomAttribute>? checksForAssembly))
						checks[assemblyName] = checksForAssembly = new List<CustomAttribute> ();

					checksForAssembly.Add (attr);
				}
			}

			return checks;
		}

		private Dictionary<AssemblyQualifiedToken, LinkedEntity> ResolveLinkedMembersForAssembly (string assemblyName)
		{
			var cleanAssemblyName = assemblyName;
			if (assemblyName.EndsWith(".exe") || assemblyName.EndsWith(".dll"))
				cleanAssemblyName = System.IO.Path.GetFileNameWithoutExtension(assemblyName);

			return this.linkedMembers.Where(e => GetModuleName(e.Value.Entity) == cleanAssemblyName).ToDictionary();
		}

		protected AssemblyDefinition ResolveOriginalsAssembly (string assemblyName)
		{
			var cleanAssemblyName = assemblyName;
			if (assemblyName.EndsWith (".exe") || assemblyName.EndsWith (".dll"))
				cleanAssemblyName = Path.GetFileNameWithoutExtension (assemblyName);
			return originalsResolver.Resolve (new AssemblyNameReference (cleanAssemblyName, null), originalReaderParameters);
		}

		private static bool IsTypeInOtherAssemblyAssertion (CustomAttribute attr)
		{
			return attr.AttributeType.Resolve ()?.DerivesFrom (nameof (BaseInAssemblyAttribute)) ?? false;
		}

		private IEnumerable<string> VerifyExpectedInstructionSequenceOnMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);
			var memberName = (string) inAssemblyAttribute.ConstructorArguments[2].Value;

			if (TryVerifyKeptMemberInAssemblyAsMethod (memberName, originalType, linkedType, out MethodDefinition? originalMethod, out MethodDefinition? linkedMethod)) {
				static string[] valueCollector (MethodDefinition m) => AssemblyChecker.FormatMethodBody (m.Body);
				var linkedValues = valueCollector (linkedMethod!);
				var srcValues = valueCollector (originalMethod!);

				var expected = ((CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[3].Value)?.Select (arg => arg.Value.ToString ()).ToArray ();
				if (!linkedValues.Equals(expected))
					yield return "Expected instruction sequence does not match";

				yield break;
			}

			yield return $"Invalid test assertion.  No method named `{memberName}` exists on the original type `{originalType}`";
		}

		protected virtual void UnhandledOtherAssemblyAssertion (string expectedTypeName, CustomAttribute checkAttrInAssembly, TypeDefinition? linkedType)
		{
			throw new NotImplementedException ($"Type {expectedTypeName}, has an unknown other assembly attribute of type {checkAttrInAssembly.AttributeType}");
		}
	}
}
