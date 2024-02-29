// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// Annotations.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILLink.Shared.TrimAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker
{

	public partial class AnnotationStore
	{

		protected readonly LinkContext context;

		protected readonly Dictionary<AssemblyDefinition, AssemblyAction> assembly_actions = new Dictionary<AssemblyDefinition, AssemblyAction> ();
		protected readonly HashSet<TypeDefinition> fieldType_init = new HashSet<TypeDefinition> ();

		// Annotations.Mark will add unmarked items to marked_pending, to be fully marked later ("processed") by MarkStep.
		// Items go through state changes from "unmarked" -> "pending" -> "processed". "pending" items are only tracked
		// once, and once "processed", an item never becomes "pending" again.
		protected readonly Dictionary<IMetadataTokenProvider, MessageOrigin> marked_pending = new Dictionary<IMetadataTokenProvider, MessageOrigin> ();
		protected readonly HashSet<IMetadataTokenProvider> processed = new HashSet<IMetadataTokenProvider> ();
		protected readonly Dictionary<TypeDefinition, (TypePreserve preserve, bool applied)> preserved_types = new Dictionary<TypeDefinition, (TypePreserve, bool)> ();
		protected readonly HashSet<TypeDefinition> pending_preserve = new HashSet<TypeDefinition> ();
		protected readonly Dictionary<TypeDefinition, TypePreserveMembers> preserved_type_members = new ();
		protected readonly Dictionary<ExportedType, TypePreserveMembers> preserved_exportedtype_members = new ();
		protected readonly Dictionary<IMemberDefinition, List<MethodDefinition>> preserved_methods = new Dictionary<IMemberDefinition, List<MethodDefinition>> ();
		readonly HashSet<AssemblyDefinition> assemblies_with_root_all_members = new ();
		protected readonly HashSet<IMetadataTokenProvider> public_api = new HashSet<IMetadataTokenProvider> ();
		protected readonly Dictionary<AssemblyDefinition, ISymbolReader> symbol_readers = new Dictionary<AssemblyDefinition, ISymbolReader> ();
		readonly Dictionary<IMemberDefinition, LinkerAttributesInformation> linker_attributes = new Dictionary<IMemberDefinition, LinkerAttributesInformation> ();
		readonly Dictionary<object, Dictionary<IMetadataTokenProvider, object>> custom_annotations = new Dictionary<object, Dictionary<IMetadataTokenProvider, object>> ();
		protected readonly Dictionary<AssemblyDefinition, HashSet<EmbeddedResource>> resources_to_remove = new Dictionary<AssemblyDefinition, HashSet<EmbeddedResource>> ();
		protected readonly HashSet<CustomAttribute> marked_attributes = new HashSet<CustomAttribute> ();
		readonly HashSet<TypeDefinition> marked_types_with_cctor = new HashSet<TypeDefinition> ();
		protected readonly HashSet<TypeDefinition> marked_instantiated = new HashSet<TypeDefinition> ();
		protected readonly HashSet<MethodDefinition> indirectly_called = new HashSet<MethodDefinition> ();
		protected readonly HashSet<TypeDefinition> types_relevant_to_variant_casting = new HashSet<TypeDefinition> ();
		readonly HashSet<IMemberDefinition> reflection_used = new ();

		public AnnotationStore (LinkContext context)
		{
			this.context = context;
			FlowAnnotations = new FlowAnnotations (context);
			VirtualMethodsWithAnnotationsToValidate = new HashSet<MethodDefinition> ();
			TypeMapInfo = new TypeMapInfo (context);
			MemberActions = new MemberActionStore (context);
		}

		public bool ProcessSatelliteAssemblies { get; set; }

		protected Tracer Tracer {
			get {
				return context.Tracer;
			}
		}

		internal FlowAnnotations FlowAnnotations { get; }

		internal HashSet<MethodDefinition> VirtualMethodsWithAnnotationsToValidate { get; }

		public TypeMapInfo TypeMapInfo { get; }

		public MemberActionStore MemberActions { get; }

		[Obsolete ("Use Tracer in LinkContext directly")]
		public void PrepareDependenciesDump ()
		{
			Tracer.AddRecorder (new XmlDependencyRecorder (context));
		}

		[Obsolete ("Use Tracer in LinkContext directly")]
		public void PrepareDependenciesDump (string filename)
		{
			Tracer.AddRecorder (new XmlDependencyRecorder (context, filename));
		}

		public ICollection<AssemblyDefinition> GetAssemblies ()
		{
			return assembly_actions.Keys;
		}

		public AssemblyAction GetAction (AssemblyDefinition assembly)
		{
			if (assembly_actions.TryGetValue (assembly, out AssemblyAction action))
				return action;

			throw new InvalidOperationException ($"No action for the assembly {assembly.Name} defined");
		}

		public MethodAction GetAction (MethodDefinition method)
		{
			return MemberActions.GetAction (method);
		}

		public void SetAction (AssemblyDefinition assembly, AssemblyAction action)
		{
			assembly_actions[assembly] = action;
		}

		public bool HasAction (AssemblyDefinition assembly)
		{
			return assembly_actions.ContainsKey (assembly);
		}

		public void SetAction (MethodDefinition method, MethodAction action)
		{
			MemberActions.PrimarySubstitutionInfo.SetMethodAction (method, action);
		}

		public void SetStubValue (MethodDefinition method, object value)
		{
			MemberActions.PrimarySubstitutionInfo.SetMethodStubValue (method, value);
		}

		public bool HasSubstitutedInit (FieldDefinition field)
		{
			return MemberActions.HasSubstitutedInit (field);
		}

		public void SetSubstitutedInit (TypeDefinition type)
		{
			fieldType_init.Add (type);
		}

		public bool HasSubstitutedInit (TypeDefinition type)
		{
			return fieldType_init.Contains (type);
		}

		[Obsolete ("Mark token providers with a reason instead.")]
		public void Mark (IMetadataTokenProvider provider)
		{
			// No origin provided, so use the provider itself if possible
			if (!processed.Contains (provider))
				marked_pending.TryAdd (provider, new MessageOrigin (provider as ICustomAttributeProvider));
		}

		public void Mark (IMetadataTokenProvider provider, in DependencyInfo reason, in MessageOrigin origin)
		{
			Debug.Assert (!(reason.Kind == DependencyKind.AlreadyMarked));
			if (!processed.Contains (provider))
				marked_pending.TryAdd (provider, origin); // It's OK if it already exists, one origin is enough to remember
			Tracer.AddDirectDependency (provider, reason, marked: true);
		}

		[Obsolete ("Mark attributes with a reason instead.")]
		public void Mark (CustomAttribute attribute)
		{
			marked_attributes.Add (attribute);
		}

		public void Mark (CustomAttribute attribute, in DependencyInfo reason)
		{
			Debug.Assert (!(reason.Kind == DependencyKind.AlreadyMarked));
			marked_attributes.Add (attribute);
			Tracer.AddDirectDependency (attribute, reason, marked: true);
		}

		public KeyValuePair<IMetadataTokenProvider, MessageOrigin>[] GetMarkedPending ()
		{
			return marked_pending.ToArray ();
		}

		public bool IsMarked (IMetadataTokenProvider provider)
		{
			return processed.Contains (provider) || marked_pending.ContainsKey (provider);
		}

		public bool IsMarked (CustomAttribute attribute)
		{
			return marked_attributes.Contains (attribute);
		}

		public void MarkIndirectlyCalledMethod (MethodDefinition method)
		{
			if (!context.AddReflectionAnnotations)
				return;

			indirectly_called.Add (method);
		}

		public bool HasMarkedAnyIndirectlyCalledMethods ()
		{
			return indirectly_called.Count != 0;
		}

		public bool IsIndirectlyCalled (MethodDefinition method)
		{
			return indirectly_called.Contains (method);
		}

		public void MarkReflectionUsed (IMemberDefinition member)
		{
			reflection_used.Add (member);
		}

		public bool IsReflectionUsed (IMemberDefinition method)
		{
			return reflection_used.Contains (method);
		}

		public void MarkInstantiated (TypeDefinition type)
		{
			marked_instantiated.Add (type);
		}

		public bool IsInstantiated (TypeDefinition type)
		{
			return marked_instantiated.Contains (type);
		}

		public void MarkRelevantToVariantCasting (TypeDefinition type)
		{
			if (type != null)
				types_relevant_to_variant_casting.Add (type);
		}

		public bool IsRelevantToVariantCasting (TypeDefinition type)
		{
			return types_relevant_to_variant_casting.Contains (type);
		}

		public bool SetProcessed (IMetadataTokenProvider provider)
		{
			if (processed.Add (provider)) {
				if (!marked_pending.Remove (provider))
					throw new InternalErrorException ($"{provider} must be marked before it can be processed.");
				return true;
			}

			return false;
		}

		public bool IsProcessed (IMetadataTokenProvider provider)
		{
			return processed.Contains (provider);
		}

		public bool MarkProcessed (IMetadataTokenProvider provider, in DependencyInfo reason)
		{
			Tracer.AddDirectDependency (provider, reason, marked: true);
			// The item may or may not be pending.
			marked_pending.Remove (provider);
			return processed.Add (provider);
		}

		public TypeDefinition[] GetPendingPreserve ()
		{
			return pending_preserve.ToArray ();
		}

		public bool SetAppliedPreserve (TypeDefinition type, TypePreserve preserve)
		{
			if (!preserved_types.TryGetValue (type, out (TypePreserve preserve, bool applied) existing))
				throw new InternalErrorException ($"Type {type} must have a TypePreserve before it can be applied.");

			if (preserve != existing.preserve)
				throw new InternalErrorException ($"Type {type} does not have {preserve}. The TypePreserve may have changed before the call to {nameof (SetAppliedPreserve)}.");

			if (existing.applied) {
				Debug.Assert (!pending_preserve.Contains (type));
				return false;
			}

			preserved_types[type] = (existing.preserve, true);
			pending_preserve.Remove (type);
			return true;
		}

		public bool HasAppliedPreserve (TypeDefinition type, TypePreserve preserve)
		{
			if (!preserved_types.TryGetValue (type, out (TypePreserve preserve, bool applied) existing))
				throw new InternalErrorException ($"Type {type} must have a TypePreserve before it can be applied.");

			if (preserve != existing.preserve)
				throw new InternalErrorException ($"Type {type} does not have {preserve}. The TypePreserve may have changed before the call to {nameof (HasAppliedPreserve)}.");

			return existing.applied;
		}

		public void SetPreserve (TypeDefinition type, TypePreserve preserve)
		{
			Debug.Assert (preserve != TypePreserve.Nothing);
			if (!preserved_types.TryGetValue (type, out (TypePreserve preserve, bool applied) existing)) {
				preserved_types.Add (type, (preserve, false));
				if (IsProcessed (type)) {
					// Required to track preserve for marked types where the existing preserve
					// was Nothing (since these aren't explicitly tracked.)
					var addedPending = pending_preserve.Add (type);
					Debug.Assert (addedPending);
				}
				return;
			}
			Debug.Assert (existing.preserve != TypePreserve.Nothing);
			var newPreserve = ChoosePreserveActionWhichPreservesTheMost (existing.preserve, preserve);
			if (newPreserve != existing.preserve) {
				if (existing.applied) {
					var addedPending = pending_preserve.Add (type);
					Debug.Assert (addedPending);
				}
				preserved_types[type] = (newPreserve, false);
			}
		}

		public static TypePreserve ChoosePreserveActionWhichPreservesTheMost (TypePreserve leftPreserveAction, TypePreserve rightPreserveAction)
		{
			if (leftPreserveAction == rightPreserveAction)
				return leftPreserveAction;

			if (leftPreserveAction == TypePreserve.All || rightPreserveAction == TypePreserve.All)
				return TypePreserve.All;

			if (leftPreserveAction == TypePreserve.Nothing)
				return rightPreserveAction;

			if (rightPreserveAction == TypePreserve.Nothing)
				return leftPreserveAction;

			if ((leftPreserveAction == TypePreserve.Methods && rightPreserveAction == TypePreserve.Fields) ||
				(leftPreserveAction == TypePreserve.Fields && rightPreserveAction == TypePreserve.Methods))
				return TypePreserve.All;

			return rightPreserveAction;
		}

		public bool TryGetPreserve (TypeDefinition type, out TypePreserve preserve)
		{
			if (preserved_types.TryGetValue (type, out (TypePreserve preserve, bool _applied) existing)) {
				preserve = existing.preserve;
				return true;
			}

			preserve = default (TypePreserve);
			return false;
		}

		public void SetMembersPreserve (TypeDefinition type, TypePreserveMembers preserve)
		{
			if (preserved_type_members.TryGetValue (type, out TypePreserveMembers existing))
				preserved_type_members[type] = CombineMembers (existing, preserve);
			else
				preserved_type_members.Add (type, preserve);
		}

		static TypePreserveMembers CombineMembers (TypePreserveMembers left, TypePreserveMembers right)
		{
			return left | right;
		}

		public void SetMembersPreserve (ExportedType type, TypePreserveMembers preserve)
		{
			if (preserved_exportedtype_members.TryGetValue (type, out TypePreserveMembers existing))
				preserved_exportedtype_members[type] = CombineMembers (existing, preserve);
			else
				preserved_exportedtype_members.Add (type, preserve);
		}

		public bool TryGetPreservedMembers (TypeDefinition type, out TypePreserveMembers preserve)
		{
			return preserved_type_members.TryGetValue (type, out preserve);
		}

		public bool TryGetPreservedMembers (ExportedType type, out TypePreserveMembers preserve)
		{
			return preserved_exportedtype_members.TryGetValue (type, out preserve);
		}

		public void SetRootAssembly (AssemblyDefinition assembly)
		{
			assemblies_with_root_all_members.Add (assembly);
		}

		public bool IsRootAssembly (AssemblyDefinition assembly)
		{
			return assemblies_with_root_all_members.Contains (assembly);
		}

		public bool TryGetMethodStubValue (MethodDefinition method, out object? value)
		{
			return MemberActions.TryGetMethodStubValue (method, out value);
		}

		public bool TryGetFieldUserValue (FieldDefinition field, out object? value)
		{
			return MemberActions.TryGetFieldUserValue (field, out value);
		}

		public HashSet<EmbeddedResource>? GetResourcesToRemove (AssemblyDefinition assembly)
		{
			if (resources_to_remove.TryGetValue (assembly, out HashSet<EmbeddedResource>? resources))
				return resources;

			return null;
		}

		public void AddResourceToRemove (AssemblyDefinition assembly, EmbeddedResource resource)
		{
			if (!resources_to_remove.TryGetValue (assembly, out HashSet<EmbeddedResource>? resources))
				resources = resources_to_remove[assembly] = new HashSet<EmbeddedResource> ();

			resources.Add (resource);
		}

		public void SetPublic (IMetadataTokenProvider provider)
		{
			public_api.Add (provider);
		}

		public bool IsPublic (IMetadataTokenProvider provider)
		{
			return public_api.Contains (provider);
		}

		/// <summary>
		/// Returns a list of all known methods that override <paramref name="method"/>.
		/// The list may be incomplete if other overrides exist in assemblies that haven't been processed by TypeMapInfo yet
		/// </summary>
		public IEnumerable<OverrideInformation>? GetOverrides (MethodDefinition method)
		{
			return TypeMapInfo.GetOverrides (method);
		}

		/// <summary>
		/// Returns a list of all default interface methods that implement <paramref name="method"/> for a type.
		/// ImplementingType is the type that implements the interface,
		/// InterfaceImpl is the <see cref="InterfaceImplementation" /> for the interface <paramref name="method" /> is declared on, and
		/// DefaultInterfaceMethod is the method that implements <paramref name="method"/>.
		/// </summary>
		/// <param name="method">The interface method to find default implementations for</param>
		public IEnumerable<OverrideInformation>? GetDefaultInterfaceImplementations (MethodDefinition method)
		{
			return TypeMapInfo.GetDefaultInterfaceImplementations (method);
		}

		/// <summary>
		/// Returns all base methods that <paramref name="method"/> overrides.
		/// This includes methods on <paramref name="method"/>'s declaring type's base type (but not methods higher up in the type hierarchy),
		/// methods on an interface that <paramref name="method"/>'s declaring type implements,
		/// and methods an interface implemented by a derived type of <paramref name="method"/>'s declaring type if the derived type uses <paramref name="method"/> as the implementing method.
		/// The list may be incomplete if there are derived types in assemblies that havent been processed yet that use <paramref name="method"/> to implement an interface.
		/// </summary>
		public List<OverrideInformation>? GetBaseMethods (MethodDefinition method)
		{
			return TypeMapInfo.GetBaseMethods (method);
		}

		public List<MethodDefinition>? GetPreservedMethods (TypeDefinition type)
		{
			return GetPreservedMethods (type as IMemberDefinition);
		}

		public bool ClearPreservedMethods (TypeDefinition type)
		{
			return preserved_methods.Remove (type);
		}

		public void AddPreservedMethod (TypeDefinition type, MethodDefinition method)
		{
			AddPreservedMethod (type as IMemberDefinition, method);
		}

		public List<MethodDefinition>? GetPreservedMethods (MethodDefinition method)
		{
			return GetPreservedMethods (method as IMemberDefinition);
		}

		public bool ClearPreservedMethods (MethodDefinition key)
		{
			return preserved_methods.Remove (key);
		}

		public void AddPreservedMethod (MethodDefinition key, MethodDefinition method)
		{
			AddPreservedMethod (key as IMemberDefinition, method);
		}

		List<MethodDefinition>? GetPreservedMethods (IMemberDefinition definition)
		{
			if (preserved_methods.TryGetValue (definition, out List<MethodDefinition>? preserved))
				return preserved;

			return null;
		}

		void AddPreservedMethod (IMemberDefinition definition, MethodDefinition method)
		{
			if (IsMarked (definition)) {
				Mark (method, new DependencyInfo (DependencyKind.PreservedMethod, definition), new MessageOrigin (definition));
				Debug.Assert (GetPreservedMethods (definition) == null);
				return;
			}

			var methods = GetPreservedMethods (definition);
			if (methods == null) {
				methods = new List<MethodDefinition> ();
				preserved_methods[definition] = methods;
			}

			methods.Add (method);
		}

		public void AddSymbolReader (AssemblyDefinition assembly, ISymbolReader symbolReader)
		{
			symbol_readers[assembly] = symbolReader;
		}

		public void CloseSymbolReader (AssemblyDefinition assembly)
		{
			if (!symbol_readers.TryGetValue (assembly, out ISymbolReader? symbolReader))
				return;

			symbol_readers.Remove (assembly);
			symbolReader.Dispose ();
		}

		public object? GetCustomAnnotation (object key, IMetadataTokenProvider item)
		{
			if (!custom_annotations.TryGetValue (key, out Dictionary<IMetadataTokenProvider, object>? slots))
				return null;

			if (!slots.TryGetValue (item, out object? value))
				return null;

			return value;
		}

		public void SetCustomAnnotation (object key, IMetadataTokenProvider item, object value)
		{
			if (!custom_annotations.TryGetValue (key, out Dictionary<IMetadataTokenProvider, object>? slots)) {
				slots = new Dictionary<IMetadataTokenProvider, object> ();
				custom_annotations.Add (key, slots);
			}

			slots[item] = value;
		}

		public bool HasPreservedStaticCtor (TypeDefinition type)
		{
			return marked_types_with_cctor.Contains (type);
		}

		public bool SetPreservedStaticCtor (TypeDefinition type)
		{
			return marked_types_with_cctor.Add (type);
		}

		public bool HasLinkerAttribute<T> (IMemberDefinition member) where T : Attribute
		{
			// Avoid setting up and inserting LinkerAttributesInformation for members without attributes.
			if (!context.CustomAttributes.HasAny (member))
				return false;

			if (!linker_attributes.TryGetValue (member, out var linkerAttributeInformation)) {
				linkerAttributeInformation = LinkerAttributesInformation.Create (context, member);
				linker_attributes.Add (member, linkerAttributeInformation);
			}

			return linkerAttributeInformation.HasAttribute<T> ();
		}

		public IEnumerable<T> GetLinkerAttributes<T> (IMemberDefinition member) where T : Attribute
		{
			// Avoid setting up and inserting LinkerAttributesInformation for members without attributes.
			if (!context.CustomAttributes.HasAny (member))
				return Enumerable.Empty<T> ();

			if (!linker_attributes.TryGetValue (member, out var linkerAttributeInformation)) {
				linkerAttributeInformation = LinkerAttributesInformation.Create (context, member);
				linker_attributes.Add (member, linkerAttributeInformation);
			}

			return linkerAttributeInformation.GetAttributes<T> ();
		}

		public bool TryGetLinkerAttribute<T> (IMemberDefinition member, [NotNullWhen (returnValue: true)] out T? attribute) where T : Attribute
		{
			var attributes = GetLinkerAttributes<T> (member);
			// This should only be called for attribute types which don't allow multiple attributes.
			attribute = attributes.SingleOrDefault ();
			return attribute != null;
		}

		/// <summary>
		/// Determines if method is within a declared RUC scope - this typically means that trim analysis
		/// warnings should be suppressed in such a method.
		/// </summary>
		/// <remarks>Unlike <see cref="DoesMethodRequireUnreferencedCode(IMemberDefinition, out RequiresUnreferencedCodeAttribute?)"/>
		/// if a declaring type has RUC, all methods in that type are considered "in scope" of that RUC. So this includes also
		/// instance methods (not just statics and .ctors).</remarks>
		internal bool IsInRequiresUnreferencedCodeScope (MethodDefinition method, [NotNullWhen (true)] out RequiresUnreferencedCodeAttribute? attribute)
		{
			if (TryGetLinkerAttribute (method, out attribute) && !method.IsStaticConstructor ())
				return true;

			if (method.DeclaringType is not null && TryGetLinkerAttribute (method.DeclaringType, out attribute))
				return true;

			attribute = null;
			return false;
		}

		internal bool ShouldSuppressAnalysisWarningsForRequiresUnreferencedCode (ICustomAttributeProvider? originMember, [NotNullWhen (true)] out RequiresUnreferencedCodeAttribute? attribute)
		{
			attribute = null;
			// Check if the current scope method has RequiresUnreferencedCode on it
			// since that attribute automatically suppresses all trim analysis warnings.
			// Check both the immediate origin method as well as suppression context method
			// since that will be different for compiler generated code.
			if (originMember is MethodDefinition &&
				IsInRequiresUnreferencedCodeScope ((MethodDefinition) originMember, out attribute))
				return true;

			if (originMember is FieldDefinition field)
				return DoesFieldRequireUnreferencedCode (field, out attribute);

			if (originMember is not IMemberDefinition member)
				return false;

			MethodDefinition? owningMethod;
			while (context.CompilerGeneratedState.TryGetOwningMethodForCompilerGeneratedMember (member, out owningMethod)) {
				Debug.Assert (owningMethod != member);
				if (IsInRequiresUnreferencedCodeScope (owningMethod, out attribute))
					return true;
				member = owningMethod;
			}

			return false;
		}

		/// <summary>
		/// Determines if a method requires unreferenced code (and thus any usage of such method should be warned about).
		/// </summary>
		/// <remarks>Unlike <see cref="IsInRequiresUnreferencedCodeScope(MethodDefinition)"/> only static methods
		/// and .ctors are reported as requiring unreferenced code when the declaring type has RUC on it.</remarks>
		internal bool DoesMethodRequireUnreferencedCode (MethodDefinition originalMethod, [NotNullWhen (returnValue: true)] out RequiresUnreferencedCodeAttribute? attribute)
		{
			MethodDefinition? method = originalMethod;
			do {
				if (!method.IsStaticConstructor () && TryGetLinkerAttribute (method, out attribute))
					return true;

				if ((method.IsStatic || method.IsConstructor) && method.DeclaringType is not null &&
					TryGetLinkerAttribute (method.DeclaringType, out attribute))
					return true;
			} while (context.CompilerGeneratedState.TryGetOwningMethodForCompilerGeneratedMember (method, out method));

			attribute = null;
			return false;
		}

		internal bool DoesFieldRequireUnreferencedCode (FieldDefinition field, [NotNullWhen (returnValue: true)] out RequiresUnreferencedCodeAttribute? attribute)
		{
			if (!field.IsStatic || field.DeclaringType is null) {
				attribute = null;
				return false;
			}

			return TryGetLinkerAttribute (field.DeclaringType, out attribute);
		}

		/// <Summary>
		/// Adds a virtual method to the queue if it is annotated and must have matching annotations on its bases and overrides. It does not check if the method is marked before producing a warning about mismatched annotations.
		/// </summary>
		public void EnqueueVirtualMethod (MethodDefinition method)
		{
			if (!method.IsVirtual)
				return;

			// Implementations of static interface methods are not virtual and won't reach here
			// We'll search through the implementations of static interface methods to find if any need to be enqueued
			if (method.IsStatic) {
				Debug.Assert (method.DeclaringType.IsInterface);
				var overrides = GetOverrides (method);
				if (overrides is not null) {
					foreach (var @override in overrides) {
						if (FlowAnnotations.RequiresVirtualMethodDataFlowAnalysis (@override.Override) || HasLinkerAttribute<RequiresUnreferencedCodeAttribute> (@override.Override))
							VirtualMethodsWithAnnotationsToValidate.Add (@override.Override);
					}
				}
			}

			if (FlowAnnotations.RequiresVirtualMethodDataFlowAnalysis (method) || HasLinkerAttribute<RequiresUnreferencedCodeAttribute> (method))
				VirtualMethodsWithAnnotationsToValidate.Add (method);
		}

		internal List<InterfaceImplementor> GetRecusiveInterfaces (TypeDefinition type) => TypeMapInfo.GetRecursiveInterfaces (type);
	}
}
