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
	public class AssemblyChecker
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
		private readonly ILCompilerTestCaseResult testResult;

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

				// Ignore compiler generated code which can't be reasonably matched to the source method
				"<PrivateImplementationDetails>",
			};

		public AssemblyChecker (
			BaseAssemblyResolver originalsResolver,
			ReaderParameters originalReaderParameters,
			AssemblyDefinition original,
			ILCompilerTestCaseResult testResult)
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

					VerifyTypeDefinition (td, linkedMember);
					linkedMembers.Remove (token);

					continue;
				}

				throw new NotImplementedException ($"Don't know how to check member of type {originalMember.GetType ()}");
			}

			// Filter out all members which are not from the main assembly
			// The Kept attributes are "optional" for non-main assemblies
			string mainModuleName = originalAssembly.Name.Name;
			List<AssemblyQualifiedToken> externalMembers = linkedMembers.Where (m => GetModuleName (m.Value.Entity) != mainModuleName).Select (m => m.Key).ToList ();
			foreach (var externalMember in externalMembers) {
				linkedMembers.Remove (externalMember);
			}

			if (linkedMembers.Count != 0)
				Assert.True (
					false,
					"Linked output includes unexpected member:\n  " +
					string.Join ("\n  ", linkedMembers.Values.Select (e => e.Entity.GetDisplayName ())));
		}

		static bool IsCompilerGeneratedMemberName (string memberName)
		{
			return memberName.Length > 0 && memberName[0] == '<';
		}

		static bool IsCompilerGeneratedMember (IMemberDefinition member)
		{
			if (IsCompilerGeneratedMemberName (member.Name))
				return true;

			if (member.DeclaringType != null)
				return IsCompilerGeneratedMember (member.DeclaringType);

			return false;
		}

		static bool IsDelegateBackingFieldsType (TypeDefinition type) => type.Name == "<>O";

		static bool IsPrivateImplementationDetailsType (TypeDefinition type) => string.IsNullOrEmpty (type.Namespace) && type.Name.StartsWith ("<PrivateImplementationDetails>");

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

		private static string? GetModuleName (TypeSystemEntity entity)
		{
			return entity switch {
				MetadataType type => type.Module.ToString (),
				MethodDesc { OwningType: MetadataType owningType } => owningType.Module.ToString (),
				_ => null
			};
		}

		protected virtual void VerifyModule (ModuleDefinition original, ModuleDefinition? linked)
		{
			// We never link away a module today so let's make sure the linked one isn't null
			if (linked == null) {
				Assert.True (false, $"Linked assembly `{original.Assembly.Name.Name}` is missing module `{original.Name}`");
				return;
			}

			var expected = original.Assembly.MainModule.AllDefinedTypes ()
				.SelectMany (t => GetCustomAttributeCtorValues<string> (t, nameof (KeptModuleReferenceAttribute)))
				.ToArray ();

			var actual = linked.ModuleReferences
				.Select (name => name.Name)
				.ToArray ();

			Assert.Equal (expected, actual);

			VerifyCustomAttributes (original, linked);
		}

		void VerifyTypeDefinition (TypeDefinition original, LinkedEntity? linkedEntity)
		{
			TypeDesc? linked = linkedEntity?.Entity as TypeDesc;
			if (linked != null && NameUtils.GetActualOriginDisplayName (linked) is string linkedDisplayName && verifiedGeneratedTypes.Contains (linkedDisplayName))
				return;

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
					return;

				// Compiler generated members can't be annotated with `Kept` attributes directly
				// For some of them we have special attributes (backing fields for example), but it's impractical to define
				// special attributes for all types of compiler generated members (there are quite a few of them and they're
				// going to change/increase over time).
				// So we're effectively disabling Kept validation on compiler generated members
				// Note that we still want to go "inside" each such member, as it might have additional attributes
				// we do want to validate. There's no specific use case right now, but I can easily imagine one
				// for more detailed testing of for example custom attributes on local functions, or similar.
				if (!IsCompilerGeneratedMember (original))
					Assert.True (false, $"Type `{original}' should have been removed");
			}

			bool prev = checkNames;
			checkNames |= original.HasAttribute (nameof (VerifyMetadataNamesAttribute));

			VerifyTypeDefinitionKept (original, linked);

			checkNames = prev;

			if (original.HasAttribute (nameof (CreatedMemberAttribute))) {
				// For now always fail on this attribute since we don't know how to validate it
				throw new NotSupportedException ("CreatedMemberAttribute is not yet supported by the test infra");
#if false
				foreach (var attr in original.CustomAttributes.Where (l => l.AttributeType.Name == nameof (CreatedMemberAttribute))) {
					var newName = original.FullName + "::" + attr.ConstructorArguments[0].Value.ToString ();

					var linkedMemberName = linkedMembers.Keys.FirstOrDefault (l => l.Contains (newName));
					if (linkedMemberName == null)
						Assert.True (false, $"Newly created member '{newName}' was not found");

					linkedMembers.Remove (linkedMemberName);
				}
#endif
			}
		}

		protected virtual void VerifyTypeDefinitionKept (TypeDefinition original, TypeDesc? linked)
		{
			// NativeAOT will not keep delegate backing field type information, it's compiled down to a set of static fields
			// this infra currently doesn't track fields in any way.
			// Same goes for private implementation detail type.
			if (IsDelegateBackingFieldsType (original) || IsPrivateImplementationDetailsType(original))
				return;

			if (linked == null) {
				Assert.True (false, $"Type `{original}' should have been kept");
				return;
			}

#if false
			// Skip verification of type metadata for compiler generated types (we don't currently need it yet)
			if (!IsCompilerGeneratedMember (original)) {
				VerifyKeptByAttributes (original, linked);
				if (!original.IsInterface)
					VerifyBaseType (original, linked);

				VerifyInterfaces (original, linked);
				VerifyPseudoAttributes (original, linked);
				VerifyGenericParameters (original, linked);
				VerifyCustomAttributes (original, linked);
				VerifySecurityAttributes (original, linked);

				VerifyFixedBufferFields (original, linked);
			}
#endif

			foreach (var td in original.NestedTypes) {
				AssemblyQualifiedToken token = new (td);
				linkedMembers.TryGetValue (
					token,
					out LinkedEntity? linkedMember);

				VerifyTypeDefinition (td, linkedMember);
				linkedMembers.Remove (token);
			}

			//// Need to check properties before fields so that the KeptBackingFieldAttribute is handled correctly
			foreach (var p in original.Properties) {
				AssemblyQualifiedToken token = new AssemblyQualifiedToken (p);

				linkedMembers.TryGetValue (
					token,
					out LinkedEntity? linkedMember);
				VerifyProperty (p, linkedMember, linked);
				linkedMembers.Remove (token);
			}
			// Need to check events before fields so that the KeptBackingFieldAttribute is handled correctly
			foreach (var e in original.Events) {
				AssemblyQualifiedToken token = new AssemblyQualifiedToken (e);

				linkedMembers.TryGetValue (
					token,
					out LinkedEntity? linkedMember);
				VerifyEvent (e, linkedMember, linked);
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

				VerifyMethod (m, linkedMember);
				linkedMembers.Remove (token);
			}
		}

		private void VerifyBaseType (TypeDefinition src, TypeDefinition linked)
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
				Assert.True (false, $"Incorrect base type on : {linked.Name}. Expected {expectedBaseName}, actual {linked.BaseType?.FullName}");
			}
		}

		private void VerifyInterfaces (TypeDefinition src, TypeDefinition linked)
		{
			var expectedInterfaces = new HashSet<string> (src.CustomAttributes
				.Where (w => w.AttributeType.Name == nameof (KeptInterfaceAttribute))
				.Select (FormatBaseOrInterfaceAttributeValue));
			if (expectedInterfaces.Count == 0) {
				Assert.False (linked.HasInterfaces, $"Type `{src}' has unexpected interfaces");
			} else {
				foreach (var iface in linked.Interfaces) {
					if (!expectedInterfaces.Remove (iface.InterfaceType.FullName)) {
						Assert.True (expectedInterfaces.Remove (iface.InterfaceType.Resolve ().FullName), $"Type `{src}' interface `{iface.InterfaceType.Resolve ().FullName}' should have been removed");
					}
				}

				if (expectedInterfaces.Count != 0)
					Assert.True (false, $"Expected interfaces were not found on {src}");
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

		private void VerifyField (FieldDefinition src, FieldDesc? linked)
		{
			bool compilerGenerated = IsCompilerGeneratedMember (src);
			bool expectedKept = ShouldBeKept (src) | compilerGenerated;

			if (!expectedKept) {
				if (linked != null)
					Assert.True (false, $"Field `{src}' should have been removed");

				return;
			}

			VerifyFieldKept (src, linked, compilerGenerated);
		}

		private static void VerifyFieldKept (FieldDefinition src, FieldDesc? linked, bool compilerGenerated)
		{
			if (linked == null) {
				Assert.True (false, $"Field `{src}' should have been kept");
				return;
			}


			if (src.HasConstant)
				throw new NotImplementedException ("Constant value for a field is not yet supported by the test infra.");
#if false
			if (!Equals (src.Constant, linked.Constant)) {
				Assert.True (false, $"Field '{src}' value doesn's match. Expected {src.Constant}, actual {linked.Constant}");
			}
#endif

#if false
			VerifyPseudoAttributes (src, linked);
			if (!compilerGenerated)
				VerifyCustomAttributes (src, linked);
#endif
		}

		private void VerifyProperty (PropertyDefinition src, LinkedEntity? linkedEntity, TypeDesc linkedType)
		{
			PropertyPseudoDesc? linked = linkedEntity?.Entity as PropertyPseudoDesc;
			VerifyMemberBackingField (src, linkedType);

			bool compilerGenerated = IsCompilerGeneratedMember (src);
			bool expectedKept = ShouldBeKept (src) || compilerGenerated;

			if (!expectedKept) {
				if (linked is not null)
					Assert.True (false, $"Property `{src}' should have been removed");

				return;
			}

			if (linked is null) {
				Assert.True (false, $"Property `{src}' should have been kept");
				return;
			}

			if (src.HasConstant)
				throw new NotSupportedException ("Constant value for a property is not yet supported by the test infra.");
#if false
			if (src.Constant != linked.Constant) {
				Assert.True (false, $"Property '{src}' value doesn's match. Expected {src.Constant}, actual {linked.Constant}");
			}
#endif

#if false
			VerifyPseudoAttributes (src, linked);
			if (!compilerGenerated)
				VerifyCustomAttributes (src, linked);
#endif
		}

		private void VerifyEvent (EventDefinition src, LinkedEntity? linkedEntity, TypeDesc linkedType)
		{
			EventPseudoDesc? linked = linkedEntity?.Entity as EventPseudoDesc;
			VerifyMemberBackingField (src, linkedType);

			bool compilerGenerated = IsCompilerGeneratedMember (src);
			bool expectedKept = ShouldBeKept (src) | compilerGenerated;

			if (!expectedKept) {
				if (linked is not null)
					Assert.True (false, $"Event `{src}' should have been removed");

				return;
			}

			if (linked is null) {
				Assert.True (false, $"Event `{src}' should have been kept");
				return;
			}

			if (src.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (KeptEventAddMethodAttribute))) {
				// TODO: This is wrong - we can't validate that the method is present by looking at linked (as that is not actually linked)
				//   we need to look into linkedMembers to see if the method was actually preserved by the compiler (and has an entry point)
				VerifyMethodInternal (src.AddMethod, new LinkedMethodEntity(linked.AddMethod, false), true, compilerGenerated);
				verifiedEventMethods.Add (src.AddMethod.FullName);
				linkedMembers.Remove (new AssemblyQualifiedToken (src.AddMethod));
			}

			if (src.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (KeptEventRemoveMethodAttribute))) {
				// TODO: This is wrong - we can't validate that the method is present by looking at linked (as that is not actually linked)
				//   we need to look into linkedMembers to see if the method was actually preserved by the compiler (and has an entry point)
				VerifyMethodInternal (src.RemoveMethod, new LinkedMethodEntity(linked.RemoveMethod, false), true, compilerGenerated);
				verifiedEventMethods.Add (src.RemoveMethod.FullName);
				linkedMembers.Remove (new AssemblyQualifiedToken (src.RemoveMethod));
			}

#if false
			VerifyPseudoAttributes (src, linked);
			if (!compilerGenerated)
				VerifyCustomAttributes (src, linked);
#endif
		}

		private void VerifyMethod (MethodDefinition src, LinkedEntity? linkedEntity)
		{
			LinkedMethodEntity? linked = linkedEntity as LinkedMethodEntity;
			bool compilerGenerated = IsCompilerGeneratedMember (src);
			bool expectedKept = ShouldMethodBeKept (src);
			VerifyMethodInternal (src, linked, expectedKept, compilerGenerated);
		}

		private void VerifyMethodInternal (MethodDefinition src, LinkedMethodEntity? linked, bool expectedKept, bool compilerGenerated)
		{
			if (!expectedKept) {
				if (linked == null)
					return;

				// Similar to comment on types, compiler-generated methods can't be annotated with Kept attribute directly
				// so we're not going to validate kept/remove on them. Note that we're still going to go validate "into" them
				// to check for other properties (like parameter name presence/removal for example)
				if (!compilerGenerated)
					Assert.True (false, $"Method `{NameUtils.GetExpectedOriginDisplayName (src)}' should have been removed");
			}

			VerifyMethodKept (src, linked, compilerGenerated);
		}

		private void VerifyMemberBackingField (IMemberDefinition src, TypeDesc? linkedType)
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

			if (srcField == null) {
				Assert.True (false, $"{src.MetadataToken.TokenType} `{src}', could not locate the expected backing field {backingFieldName}");
				return;
			}

			VerifyFieldKept (srcField, linkedType?.GetFields ()?.FirstOrDefault (l => srcField.Name == l.Name), compilerGenerated: true);
			verifiedGeneratedFields.Add (srcField.FullName);
			linkedMembers.Remove (new AssemblyQualifiedToken (srcField));
		}

		void VerifyMethodKept (MethodDefinition src, LinkedMethodEntity? linked, bool compilerGenerated)
		{
			if (linked == null) {
				Assert.True (false, $"Method `{NameUtils.GetExpectedOriginDisplayName (src)}' should have been kept");
				return;
			}

#if false
			VerifyPseudoAttributes (src, linked);
			VerifyGenericParameters (src, linked);
			if (!compilerGenerated) {
				VerifyCustomAttributes (src, linked);
				VerifyCustomAttributes (src.MethodReturnType, linked.MethodReturnType);
			}
#endif
			VerifyParameters (src, linked);
#if false
			VerifySecurityAttributes (src, linked);
			VerifyArrayInitializers (src, linked);

			// Method bodies are not very different in Native AOT
			VerifyMethodBody (src, linked);
			VerifyKeptByAttributes (src, linked);
#endif
		}

		protected virtual void VerifyMethodBody (MethodDefinition src, MethodDefinition linked)
		{
			if (!src.HasBody)
				return;

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
				m => FormatMethodBody (m.Body),
				attr => GetStringArrayAttributeValue (attr)!.ToArray ());
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

		private static void VerifyLocals (MethodDefinition src, MethodDefinition linked)
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

		public static void VerifyBodyProperties (MethodDefinition src, MethodDefinition linked, string sequenceAttributeName, string expectModifiedAttributeName,
			string propertyDescription,
			Func<MethodDefinition, string[]> valueCollector,
			Func<CustomAttribute, string[]> getExpectFromSequenceAttribute)
		{
			var expectedSequenceAttribute = src.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.Name == sequenceAttributeName);
			var linkedValues = valueCollector (linked);
			var srcValues = valueCollector (src);

			if (src.CustomAttributes.Any (attr => attr.AttributeType.Name == expectModifiedAttributeName)) {
				linkedValues.Should ().BeEquivalentTo (srcValues, $"Expected method `{src} to have {propertyDescription} modified, however, the {propertyDescription} were the same as the original\n{FormattingUtils.FormatSequenceCompareFailureMessage (linkedValues, srcValues)}");
			} else if (expectedSequenceAttribute != null) {
				var expected = getExpectFromSequenceAttribute (expectedSequenceAttribute).ToArray ();
				linkedValues.Should ().BeEquivalentTo (expected, $"Expected method `{src} to have it's {propertyDescription} modified, however, the sequence of {propertyDescription} does not match the expected value\n{FormattingUtils.FormatSequenceCompareFailureMessage2 (linkedValues, expected, srcValues)}");
			} else {
				linkedValues.Should ().BeEquivalentTo (srcValues, $"Expected method `{src} to have it's {propertyDescription} unchanged, however, the {propertyDescription} differ from the original\n{FormattingUtils.FormatSequenceCompareFailureMessage (linkedValues, srcValues)}");
			}
		}

		private void VerifyReferences (AssemblyDefinition original, AssemblyDefinition linked)
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

			actual.Should ().BeEquivalentTo (expected);
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

		private void VerifyResources (AssemblyDefinition original, AssemblyDefinition linked)
		{
			var expectedResourceNames = original.MainModule.AllDefinedTypes ()
				.SelectMany (t => GetCustomAttributeCtorValues<string> (t, nameof (KeptResourceAttribute)))
				.ToList ();

			foreach (var resource in linked.MainModule.Resources) {
				if (!expectedResourceNames.Remove (resource.Name))
					Assert.True (false, $"Resource '{resource.Name}' should be removed.");

				EmbeddedResource embeddedResource = (EmbeddedResource) resource;

				var expectedResource = (EmbeddedResource) original.MainModule.Resources.First (r => r.Name == resource.Name);

				embeddedResource.GetResourceData ().Should ().BeEquivalentTo (expectedResource.GetResourceData (), $"Resource '{resource.Name}' data doesn't match.");
			}

			if (expectedResourceNames.Count > 0) {
				Assert.True (false, $"Resource '{expectedResourceNames.First ()}' should be kept.");
			}
		}

		private void VerifyExportedTypes (AssemblyDefinition original, AssemblyDefinition linked)
		{
			var expectedTypes = original.MainModule.AllDefinedTypes ()
				.SelectMany (t => GetCustomAttributeCtorValues<TypeReference> (t, nameof (KeptExportedTypeAttribute)).Select (l => l?.FullName ?? "<null>")).ToArray ();

			linked.MainModule.ExportedTypes.Select (l => l.FullName).Should ().BeEquivalentTo (expectedTypes);
		}

		protected virtual void VerifyPseudoAttributes (MethodDefinition src, MethodDefinition linked)
		{
			var expected = (MethodAttributes) GetExpectedPseudoAttributeValue (src, (uint) src.Attributes);
			linked.Attributes.Should ().Be (expected, $"Method `{src}' pseudo attributes did not match expected");
		}

		protected virtual void VerifyPseudoAttributes (TypeDefinition src, TypeDefinition linked)
		{
			var expected = (TypeAttributes) GetExpectedPseudoAttributeValue (src, (uint) src.Attributes);
			linked.Attributes.Should ().Be (expected, $"Type `{src}' pseudo attributes did not match expected");
		}

		protected virtual void VerifyPseudoAttributes (FieldDefinition src, FieldDefinition linked)
		{
			var expected = (FieldAttributes) GetExpectedPseudoAttributeValue (src, (uint) src.Attributes);
			linked.Attributes.Should ().Be (expected, $"Field `{src}' pseudo attributes did not match expected");
		}

		protected virtual void VerifyPseudoAttributes (PropertyDefinition src, PropertyDefinition linked)
		{
			var expected = (PropertyAttributes) GetExpectedPseudoAttributeValue (src, (uint) src.Attributes);
			linked.Attributes.Should ().Be (expected, $"Property `{src}' pseudo attributes did not match expected");
		}

		protected virtual void VerifyPseudoAttributes (EventDefinition src, EventDefinition linked)
		{
			var expected = (EventAttributes) GetExpectedPseudoAttributeValue (src, (uint) src.Attributes);
			linked.Attributes.Should ().Be (expected, $"Event `{src}' pseudo attributes did not match expected");
		}

		protected virtual void VerifyCustomAttributes (ICustomAttributeProvider src, ICustomAttributeProvider linked)
		{
			var expectedAttrs = GetExpectedAttributes (src).ToList ();
			var linkedAttrs = FilterLinkedAttributes (linked).ToList ();

			linkedAttrs.Should ().BeEquivalentTo (expectedAttrs, $"Custom attributes on `{src}' are not matching");
		}

		protected virtual void VerifySecurityAttributes (ICustomAttributeProvider src, ISecurityDeclarationProvider linked)
		{
			var expectedAttrs = GetCustomAttributeCtorValues<object> (src, nameof (KeptSecurityAttribute))
				.Select (attr => attr?.ToString () ?? "<null>")
				.ToList ();

			var linkedAttrs = FilterLinkedSecurityAttributes (linked).ToList ();

			linkedAttrs.Should ().BeEquivalentTo (expectedAttrs, $"Security attributes on `{src}' are not matching");
		}

#if false
		protected virtual void VerifyArrayInitializers (MethodDefinition src, MethodDefinition linked)
		{
			var expectedIndices = GetCustomAttributeCtorValues<object> (src, nameof (KeptInitializerData))
				.Cast<int> ()
				.ToArray ();

			var expectKeptAll = src.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (KeptInitializerData) && !attr.HasConstructorArguments);

			if (expectedIndices.Length == 0 && !expectKeptAll)
				return;

			if (!src.HasBody)
				Assert.True (false, $"`{nameof (KeptInitializerData)}` cannot be used on methods that don't have bodies");

			var srcImplementationDetails = src.Module.Types.FirstOrDefault (t => string.IsNullOrEmpty (t.Namespace) && t.Name.StartsWith ("<PrivateImplementationDetails>"));

			if (srcImplementationDetails == null) {
				Assert.True (false, "Could not locate <PrivateImplementationDetails> in the original assembly.  Does your test use initializers?");
				return;
			}

			var linkedImplementationDetails = linked.Module.Types.FirstOrDefault (t => string.IsNullOrEmpty (t.Namespace) && t.Name.StartsWith ("<PrivateImplementationDetails>"));

			if (linkedImplementationDetails == null) {
				Assert.True (false, "Could not locate <PrivateImplementationDetails> in the linked assembly");
				return;
			}

			var possibleInitializerFields = src.Body.Instructions
				.Where (ins => IsLdtokenOnPrivateImplementationDetails (srcImplementationDetails, ins))
				.Select (ins => ((FieldReference) ins.Operand).Resolve ())
				.ToArray ();

			if (possibleInitializerFields.Length == 0)
				Assert.True (false, $"`{src}` does not make use of any initializers");

			if (expectKeptAll) {
				foreach (var srcField in possibleInitializerFields) {
					var linkedField = linkedImplementationDetails.Fields.FirstOrDefault (f => f.InitialValue.SequenceEqual (srcField.InitialValue));
					VerifyInitializerField (srcField, linkedField);
				}
			} else {
				foreach (var index in expectedIndices) {
					if (index < 0 || index > possibleInitializerFields.Length)
						Assert.True (false, $"Invalid expected index `{index}` in {src}.  Value must be between 0 and {expectedIndices.Length}");

					var srcField = possibleInitializerFields[index];
					var linkedField = linkedImplementationDetails.Fields.FirstOrDefault (f => f.InitialValue.SequenceEqual (srcField.InitialValue));

					VerifyInitializerField (srcField, linkedField);
				}
			}
		}

		private void VerifyInitializerField (FieldDefinition src, FieldDefinition? linked)
		{
			VerifyFieldKept (src, linked);
			verifiedGeneratedFields.Add (linked!.FullName);
			linkedMembers.Remove (new (linked));
			//VerifyTypeDefinitionKept (src.FieldType.Resolve (), linked.FieldType.Resolve ());
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
					Assert.True (false, $"Could not locate original fixed field for {srcDefinition}");

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
		private void VerifyFixedBufferFields (TypeDefinition src, TypeDefinition linked)
		{
			var fields = src.Fields.Where (f => f.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (KeptFixedBufferAttribute)));

			foreach (var field in fields) {
				// The name of the generated fixed buffer type is a little tricky.
				// Some versions of csc name it `<fieldname>e__FixedBuffer0`
				// while mcs and other versions of csc name it `<fieldname>__FixedBuffer0`
				var originalCompilerGeneratedBufferType = src.NestedTypes.FirstOrDefault (t => t.FullName.Contains ($"<{field.Name}>") && t.FullName.Contains ("__FixedBuffer"));
				if (originalCompilerGeneratedBufferType == null) {
					Assert.True (false, $"Could not locate original compiler generated fixed buffer type for field {field}");
					return;
				}

				var linkedCompilerGeneratedBufferType = linked.NestedTypes.FirstOrDefault (t => t.Name == originalCompilerGeneratedBufferType.Name);
				if (linkedCompilerGeneratedBufferType == null) {
					Assert.True (false, $"Missing expected type {originalCompilerGeneratedBufferType}");
					return;
				}

				// Have to verify the field before the type
				var originalElementField = originalCompilerGeneratedBufferType.Fields.FirstOrDefault ();
				if (originalElementField == null) {
					Assert.True (false, $"Could not locate original compiler generated FixedElementField on {originalCompilerGeneratedBufferType}");
					return;
				}

				var linkedField = linkedCompilerGeneratedBufferType?.Fields.FirstOrDefault ();
				VerifyFieldKept (originalElementField, linkedField);
				verifiedGeneratedFields.Add (originalElementField.FullName);
				linkedMembers.Remove (new (linkedField!));

				//VerifyTypeDefinitionKept (originalCompilerGeneratedBufferType, linkedCompilerGeneratedBufferType);
				verifiedGeneratedTypes.Add (originalCompilerGeneratedBufferType.FullName);
			}
		}

		private void VerifyDelegateBackingFields (TypeDefinition src, TypeDefinition linked)
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
				linkedMembers.Remove (new (srcField));
			}
		}
#endif

		private void VerifyGenericParameters (IGenericParameterProvider src, IGenericParameterProvider linked)
		{
			Assert.Equal (src.HasGenericParameters, linked.HasGenericParameters);
			if (src.HasGenericParameters) {
				for (int i = 0; i < src.GenericParameters.Count; ++i) {
					// TODO: Verify constraints
					var srcp = src.GenericParameters[i];
					var lnkp = linked.GenericParameters[i];
					VerifyCustomAttributes (srcp, lnkp);

					if (checkNames) {
						if (srcp.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (RemovedNameValueAttribute))) {
							string name = (src.GenericParameterType == GenericParameterType.Method ? "!!" : "!") + srcp.Position;
							lnkp.Name.Should ().Be (name, "Expected empty generic parameter name");
						} else {
							lnkp.Name.Should ().Be (srcp.Name, "Mismatch in generic parameter name");
						}
					}
				}
			}
		}

		private void VerifyParameters (IMethodSignature src, LinkedMethodEntity linked)
		{
			Assert.Equal (src.HasParameters, linked.Method.Signature.Length > 0);
			if (src.HasParameters) {
				for (int i = 0; i < src.Parameters.Count; ++i) {
					var srcp = src.Parameters[i];
					//var lnkp = linked.Parameters[i];

#if false
					VerifyCustomAttributes (srcp, lnkp);
#endif

					if (checkNames) {
						if (srcp.CustomAttributes.Any (attr => attr.AttributeType.Name == nameof (RemovedNameValueAttribute)))
							linked.IsReflected.Should ().BeFalse ($"Expected no parameter name (non-reflectable). Parameter {i} of {(src as MethodDefinition)}");
						else
							linked.IsReflected.Should ().BeTrue ($"Expected accessible parameter name (reflectable). Parameter {i} of {(src as MethodDefinition)}");
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

		private void VerifyLinkingOfOtherAssemblies (AssemblyDefinition original)
		{
			var checks = BuildOtherAssemblyCheckTable (original);

			// TODO
			// For now disable the code below by simply removing all checks
			checks.Clear ();

			try {
				foreach (var assemblyName in checks.Keys) {
					var linkedAssembly = ResolveLinkedAssembly (assemblyName);
					foreach (var checkAttrInAssembly in checks[assemblyName]) {
						var attributeTypeName = checkAttrInAssembly.AttributeType.Name;

						switch (attributeTypeName) {
						case nameof (KeptAllTypesAndMembersInAssemblyAttribute):
							VerifyKeptAllTypesAndMembersInAssembly (linkedAssembly);
							continue;
						case nameof (KeptAttributeInAssemblyAttribute):
							VerifyKeptAttributeInAssembly (checkAttrInAssembly, linkedAssembly);
							continue;
						case nameof (RemovedAttributeInAssembly):
							VerifyRemovedAttributeInAssembly (checkAttrInAssembly, linkedAssembly);
							continue;
						default:
							break;
						}

						var expectedTypeName = checkAttrInAssembly.ConstructorArguments[1].Value.ToString ()!;
						TypeDefinition? linkedType = linkedAssembly.MainModule.GetType (expectedTypeName);

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

						switch (attributeTypeName) {
						case nameof (RemovedTypeInAssemblyAttribute):
							if (linkedType != null)
								Assert.Fail ($"Type `{expectedTypeName}' should have been removed from assembly {assemblyName}");
							GetOriginalTypeFromInAssemblyAttribute (checkAttrInAssembly);
							break;
						case nameof (KeptTypeInAssemblyAttribute):
							if (linkedType == null)
								Assert.Fail ($"Type `{expectedTypeName}' should have been kept in assembly {assemblyName}");
							break;
						case nameof (RemovedInterfaceOnTypeInAssemblyAttribute):
							if (linkedType == null)
								Assert.Fail ($"Type `{expectedTypeName}' should have been kept in assembly {assemblyName}");
							VerifyRemovedInterfaceOnTypeInAssembly (checkAttrInAssembly, linkedType);
							break;
						case nameof (KeptInterfaceOnTypeInAssemblyAttribute):
							if (linkedType == null)
								Assert.Fail ($"Type `{expectedTypeName}' should have been kept in assembly {assemblyName}");
							VerifyKeptInterfaceOnTypeInAssembly (checkAttrInAssembly, linkedType);
							break;
						case nameof (RemovedMemberInAssemblyAttribute):
							if (linkedType == null)
								continue;

							VerifyRemovedMemberInAssembly (checkAttrInAssembly, linkedType);
							break;
						case nameof (KeptBaseOnTypeInAssemblyAttribute):
							if (linkedType == null)
								Assert.Fail ($"Type `{expectedTypeName}' should have been kept in assembly {assemblyName}");
							VerifyKeptBaseOnTypeInAssembly (checkAttrInAssembly, linkedType);
							break;
						case nameof (KeptMemberInAssemblyAttribute):
							if (linkedType == null)
								Assert.Fail ($"Type `{expectedTypeName}' should have been kept in assembly {assemblyName}");

							VerifyKeptMemberInAssembly (checkAttrInAssembly, linkedType);
							break;
						case nameof (RemovedForwarderAttribute):
							if (linkedAssembly.MainModule.ExportedTypes.Any (l => l.Name == expectedTypeName))
								Assert.Fail ($"Forwarder `{expectedTypeName}' should have been removed from assembly {assemblyName}");

							break;

						case nameof (RemovedAssemblyReferenceAttribute):
							Assert.False (linkedAssembly.MainModule.AssemblyReferences.Any (l => l.Name == expectedTypeName),
								$"AssemblyRef '{expectedTypeName}' should have been removed from assembly {assemblyName}");
							break;

						case nameof (KeptResourceInAssemblyAttribute):
							VerifyKeptResourceInAssembly (checkAttrInAssembly);
							break;
						case nameof (RemovedResourceInAssemblyAttribute):
							VerifyRemovedResourceInAssembly (checkAttrInAssembly);
							break;
						case nameof (KeptReferencesInAssemblyAttribute):
							VerifyKeptReferencesInAssembly (checkAttrInAssembly);
							break;
						case nameof (ExpectedInstructionSequenceOnMemberInAssemblyAttribute):
							if (linkedType == null)
								Assert.Fail ($"Type `{expectedTypeName}` should have been kept in assembly {assemblyName}");
							VerifyExpectedInstructionSequenceOnMemberInAssembly (checkAttrInAssembly, linkedType);
							break;
						default:
							UnhandledOtherAssemblyAssertion (expectedTypeName, checkAttrInAssembly, linkedType);
							break;
						}
					}
				}
			} catch (AssemblyResolutionException e) {
				Assert.Fail ($"Failed to resolve linked assembly `{e.AssemblyReference.Name}`.  It must not exist in the output.");
			}
		}

		private void VerifyKeptAttributeInAssembly (CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly)
		{
			VerifyAttributeInAssembly (inAssemblyAttribute, linkedAssembly, VerifyCustomAttributeKept);
		}

		private void VerifyRemovedAttributeInAssembly (CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly)
		{
			VerifyAttributeInAssembly (inAssemblyAttribute, linkedAssembly, VerifyCustomAttributeRemoved);
		}

		private void VerifyAttributeInAssembly (CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly, Action<ICustomAttributeProvider, string> assertExpectedAttribute)
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
				assertExpectedAttribute (linkedAssembly, expectedAttributeTypeName);
				return;
			}

			// We are asserting on type or member
			var typeOrTypeName = inAssemblyAttribute.ConstructorArguments[2].Value;
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ()!, typeOrTypeName);
			if (originalType == null)
				Assert.Fail ($"Invalid test assertion.  The original `{assemblyName}` does not contain a type `{typeOrTypeName}`");

			var linkedType = linkedAssembly.MainModule.GetType (originalType.FullName);
			if (linkedType == null)
				Assert.Fail ($"Missing expected type `{typeOrTypeName}` in `{assemblyName}`");

			if (inAssemblyAttribute.ConstructorArguments.Count == 3) {
				assertExpectedAttribute (linkedType, expectedAttributeTypeName);
				return;
			}

			// we are asserting on a member
			string memberName = (string) inAssemblyAttribute.ConstructorArguments[3].Value;

			// We will find the matching type from the original assembly first that way we can confirm
			// that the name defined in the attribute corresponds to a member that actually existed
			var originalFieldMember = originalType.Fields.FirstOrDefault (m => m.Name == memberName);
			if (originalFieldMember != null) {
				var linkedField = linkedType.Fields.FirstOrDefault (m => m.Name == memberName);
				if (linkedField == null)
					Assert.Fail ($"Field `{memberName}` on Type `{originalType}` should have been kept");

				assertExpectedAttribute (linkedField, expectedAttributeTypeName);
				return;
			}

			var originalPropertyMember = originalType.Properties.FirstOrDefault (m => m.Name == memberName);
			if (originalPropertyMember != null) {
				var linkedProperty = linkedType.Properties.FirstOrDefault (m => m.Name == memberName);
				if (linkedProperty == null)
					Assert.Fail ($"Property `{memberName}` on Type `{originalType}` should have been kept");

				assertExpectedAttribute (linkedProperty, expectedAttributeTypeName);
				return;
			}

			var originalMethodMember = originalType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
			if (originalMethodMember != null) {
				var linkedMethod = linkedType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
				if (linkedMethod == null)
					Assert.Fail ($"Method `{memberName}` on Type `{originalType}` should have been kept");

				assertExpectedAttribute (linkedMethod, expectedAttributeTypeName);
				return;
			}

			Assert.Fail ($"Invalid test assertion.  No member named `{memberName}` exists on the original type `{originalType}`");
		}

		private static void VerifyCopyAssemblyIsKeptUnmodified (NPath outputDirectory, string assemblyName)
		{
			string inputAssemblyPath = Path.Combine (Directory.GetParent (outputDirectory)!.ToString (), "input", assemblyName);
			string outputAssemblyPath = Path.Combine (outputDirectory, assemblyName);
			Assert.True (File.ReadAllBytes (inputAssemblyPath).SequenceEqual (File.ReadAllBytes (outputAssemblyPath)),
				$"Expected assemblies\n" +
				$"\t{inputAssemblyPath}\n" +
				$"\t{outputAssemblyPath}\n" +
				$"binaries to be equal, since the input assembly has copy action.");
		}

		private void VerifyCustomAttributeKept (ICustomAttributeProvider provider, string expectedAttributeTypeName)
		{
			var match = provider.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.FullName == expectedAttributeTypeName);
			if (match == null)
				Assert.Fail ($"Expected `{provider}` to have an attribute of type `{expectedAttributeTypeName}`");
		}

		private void VerifyCustomAttributeRemoved (ICustomAttributeProvider provider, string expectedAttributeTypeName)
		{
			var match = provider.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.FullName == expectedAttributeTypeName);
			if (match != null)
				Assert.Fail ($"Expected `{provider}` to no longer have an attribute of type `{expectedAttributeTypeName}`");
		}

		private void VerifyRemovedInterfaceOnTypeInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);

			var interfaceAssemblyName = inAssemblyAttribute.ConstructorArguments[2].Value.ToString ()!;
			var interfaceType = inAssemblyAttribute.ConstructorArguments[3].Value;

			var originalInterface = GetOriginalTypeFromInAssemblyAttribute (interfaceAssemblyName, interfaceType);
			if (!originalType.HasInterfaces)
				Assert.Fail ("Invalid assertion.  Original type does not have any interfaces");

			var originalInterfaceImpl = GetMatchingInterfaceImplementationOnType (originalType, originalInterface.FullName);
			if (originalInterfaceImpl == null)
				Assert.Fail ($"Invalid assertion.  Original type never had an interface of type `{originalInterface}`");

			var linkedInterfaceImpl = GetMatchingInterfaceImplementationOnType (linkedType, originalInterface.FullName);
			if (linkedInterfaceImpl != null)
				Assert.Fail ($"Expected `{linkedType}` to no longer have an interface of type {originalInterface.FullName}");
		}

		private void VerifyKeptInterfaceOnTypeInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);

			var interfaceAssemblyName = inAssemblyAttribute.ConstructorArguments[2].Value.ToString ()!;
			var interfaceType = inAssemblyAttribute.ConstructorArguments[3].Value;

			var originalInterface = GetOriginalTypeFromInAssemblyAttribute (interfaceAssemblyName, interfaceType);
			if (!originalType.HasInterfaces)
				Assert.Fail ("Invalid assertion.  Original type does not have any interfaces");

			var originalInterfaceImpl = GetMatchingInterfaceImplementationOnType (originalType, originalInterface.FullName);
			if (originalInterfaceImpl == null)
				Assert.Fail ($"Invalid assertion.  Original type never had an interface of type `{originalInterface}`");

			var linkedInterfaceImpl = GetMatchingInterfaceImplementationOnType (linkedType, originalInterface.FullName);
			if (linkedInterfaceImpl == null)
				Assert.Fail ($"Expected `{linkedType}` to have interface of type {originalInterface.FullName}");
		}

		private void VerifyKeptBaseOnTypeInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);

			var baseAssemblyName = inAssemblyAttribute.ConstructorArguments[2].Value.ToString ()!;
			var baseType = inAssemblyAttribute.ConstructorArguments[3].Value;

			var originalBase = GetOriginalTypeFromInAssemblyAttribute (baseAssemblyName, baseType);
			if (originalType.BaseType.Resolve () != originalBase)
				Assert.Fail ("Invalid assertion.  Original type's base does not match the expected base");

			Assert.True (originalBase.FullName == linkedType.BaseType.FullName,
				$"Incorrect base on `{linkedType.FullName}`.  Expected `{originalBase.FullName}` but was `{linkedType.BaseType.FullName}`");
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

		private void VerifyRemovedMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
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
						Assert.Fail ($"Field `{memberName}` on Type `{originalType}` should have been removed");

					continue;
				}

				var originalPropertyMember = originalType.Properties.FirstOrDefault (m => m.Name == memberName);
				if (originalPropertyMember != null) {
					var linkedProperty = linkedType.Properties.FirstOrDefault (m => m.Name == memberName);
					if (linkedProperty != null)
						Assert.Fail ($"Property `{memberName}` on Type `{originalType}` should have been removed");

					continue;
				}

				var originalMethodMember = originalType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
				if (originalMethodMember != null) {
					var linkedMethod = linkedType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
					if (linkedMethod != null)
						Assert.Fail ($"Method `{memberName}` on Type `{originalType}` should have been removed");

					continue;
				}

				Assert.Fail ($"Invalid test assertion.  No member named `{memberName}` exists on the original type `{originalType}`");
			}
		}

		private void VerifyKeptMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);
			var memberNames = (CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[2].Value;
			Assert.True (memberNames.Length > 0, "Invalid KeptMemberInAssemblyAttribute. Expected member names.");
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

				Assert.Fail ($"Invalid test assertion.  No member named `{memberName}` exists on the original type `{originalType}`");
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

		private void VerifyKeptReferencesInAssembly (CustomAttribute inAssemblyAttribute)
		{
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ()!);
			var expectedReferenceNames = ((CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[1].Value).Select (attr => (string) attr.Value).ToList ();
			for (int i = 0; i < expectedReferenceNames.Count; i++)
				if (expectedReferenceNames[i].EndsWith (".dll"))
					expectedReferenceNames[i] = expectedReferenceNames[i].Substring (0, expectedReferenceNames[i].LastIndexOf ("."));

			Assert.Equal (assembly.MainModule.AssemblyReferences.Select (asm => asm.Name), expectedReferenceNames);
		}

		private void VerifyKeptResourceInAssembly (CustomAttribute inAssemblyAttribute)
		{
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ()!);
			var resourceName = inAssemblyAttribute.ConstructorArguments[1].Value.ToString ();

			Assert.Contains (resourceName, assembly.MainModule.Resources.Select (r => r.Name));
		}

		private void VerifyRemovedResourceInAssembly (CustomAttribute inAssemblyAttribute)
		{
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ()!);
			var resourceName = inAssemblyAttribute.ConstructorArguments[1].Value.ToString ();

			Assert.DoesNotContain (resourceName, assembly.MainModule.Resources.Select (r => r.Name));
		}

		private void VerifyKeptAllTypesAndMembersInAssembly (AssemblyDefinition linked)
		{
			var original = ResolveOriginalsAssembly (linked.MainModule.Assembly.Name.Name);

			if (original == null)
				Assert.Fail ($"Failed to resolve original assembly {linked.MainModule.Assembly.Name.Name}");

			var originalTypes = original.AllDefinedTypes ().ToDictionary (t => t.FullName);
			var linkedTypes = linked.AllDefinedTypes ().ToDictionary (t => t.FullName);

			var missingInLinked = originalTypes.Keys.Except (linkedTypes.Keys);

			Assert.True (missingInLinked.Any (), $"Expected all types to exist in the linked assembly, but one or more were missing");

			foreach (var originalKvp in originalTypes) {
				var linkedType = linkedTypes[originalKvp.Key];

				var originalMembers = originalKvp.Value.AllMembers ().Select (m => m.FullName);
				var linkedMembers = linkedType.AllMembers ().Select (m => m.FullName);

				var missingMembersInLinked = originalMembers.Except (linkedMembers);

				Assert.True (missingMembersInLinked.Any (), $"Expected all members of `{originalKvp.Key}`to exist in the linked assembly, but one or more were missing");
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
					if (!checks.TryGetValue (assemblyName, out List<CustomAttribute>? checksForAssembly))
						checks[assemblyName] = checksForAssembly = new List<CustomAttribute> ();

					checksForAssembly.Add (attr);
				}
			}

			return checks;
		}

		protected AssemblyDefinition ResolveLinkedAssembly (string assemblyName)
		{
			//var cleanAssemblyName = assemblyName;
			//if (assemblyName.EndsWith (".exe") || assemblyName.EndsWith (".dll"))
			//cleanAssemblyName = System.IO.Path.GetFileNameWithoutExtension (assemblyName);
			//return _linkedResolver.Resolve (new AssemblyNameReference (cleanAssemblyName, null), _linkedReaderParameters);
			// TODO - adapt to Native AOT
			return ResolveOriginalsAssembly (assemblyName);
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

		private void VerifyExpectedInstructionSequenceOnMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);
			var memberName = (string) inAssemblyAttribute.ConstructorArguments[2].Value;

			if (TryVerifyKeptMemberInAssemblyAsMethod (memberName, originalType, linkedType, out MethodDefinition? originalMethod, out MethodDefinition? linkedMethod)) {
				static string[] valueCollector (MethodDefinition m) => AssemblyChecker.FormatMethodBody (m.Body);
				var linkedValues = valueCollector (linkedMethod!);
				var srcValues = valueCollector (originalMethod!);

				var expected = ((CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[3].Value)?.Select (arg => arg.Value.ToString ()).ToArray ();
				Assert.Equal (
					linkedValues,
					expected);

				return;
			}

			Assert.Fail ($"Invalid test assertion.  No method named `{memberName}` exists on the original type `{originalType}`");
		}

		protected virtual void UnhandledOtherAssemblyAssertion (string expectedTypeName, CustomAttribute checkAttrInAssembly, TypeDefinition? linkedType)
		{
			throw new NotImplementedException ($"Type {expectedTypeName}, has an unknown other assembly attribute of type {checkAttrInAssembly.AttributeType}");
		}
	}
}
