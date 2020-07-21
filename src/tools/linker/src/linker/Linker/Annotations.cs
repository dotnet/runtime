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
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Dataflow;

namespace Mono.Linker
{

	public partial class AnnotationStore
	{

		protected readonly LinkContext context;

		protected readonly Dictionary<AssemblyDefinition, AssemblyAction> assembly_actions = new Dictionary<AssemblyDefinition, AssemblyAction> ();
		protected readonly Dictionary<MethodDefinition, MethodAction> method_actions = new Dictionary<MethodDefinition, MethodAction> ();
		protected readonly Dictionary<MethodDefinition, object> method_stub_values = new Dictionary<MethodDefinition, object> ();
		protected readonly Dictionary<FieldDefinition, object> field_values = new Dictionary<FieldDefinition, object> ();
		protected readonly HashSet<FieldDefinition> field_init = new HashSet<FieldDefinition> ();
		protected readonly HashSet<TypeDefinition> fieldType_init = new HashSet<TypeDefinition> ();
		protected readonly HashSet<IMetadataTokenProvider> marked = new HashSet<IMetadataTokenProvider> ();
		protected readonly HashSet<IMetadataTokenProvider> processed = new HashSet<IMetadataTokenProvider> ();
		protected readonly Dictionary<TypeDefinition, TypePreserve> preserved_types = new Dictionary<TypeDefinition, TypePreserve> ();
		protected readonly Dictionary<IMemberDefinition, List<MethodDefinition>> preserved_methods = new Dictionary<IMemberDefinition, List<MethodDefinition>> ();
		protected readonly HashSet<IMetadataTokenProvider> public_api = new HashSet<IMetadataTokenProvider> ();
		protected readonly Dictionary<MethodDefinition, List<OverrideInformation>> override_methods = new Dictionary<MethodDefinition, List<OverrideInformation>> ();
		protected readonly Dictionary<MethodDefinition, List<MethodDefinition>> base_methods = new Dictionary<MethodDefinition, List<MethodDefinition>> ();
		protected readonly Dictionary<AssemblyDefinition, ISymbolReader> symbol_readers = new Dictionary<AssemblyDefinition, ISymbolReader> ();
		readonly Dictionary<IMemberDefinition, LinkerAttributesInformation> linker_attributes = new Dictionary<IMemberDefinition, LinkerAttributesInformation> ();
		protected readonly Dictionary<MethodDefinition, List<(TypeDefinition InstanceType, InterfaceImplementation ImplementationProvider)>> default_interface_implementations = new Dictionary<MethodDefinition, List<(TypeDefinition, InterfaceImplementation)>> ();

		readonly Dictionary<object, Dictionary<IMetadataTokenProvider, object>> custom_annotations = new Dictionary<object, Dictionary<IMetadataTokenProvider, object>> ();
		protected readonly Dictionary<AssemblyDefinition, HashSet<EmbeddedResource>> resources_to_remove = new Dictionary<AssemblyDefinition, HashSet<EmbeddedResource>> ();
		protected readonly HashSet<CustomAttribute> marked_attributes = new HashSet<CustomAttribute> ();
		readonly HashSet<TypeDefinition> marked_types_with_cctor = new HashSet<TypeDefinition> ();
		protected readonly HashSet<TypeDefinition> marked_instantiated = new HashSet<TypeDefinition> ();
		protected readonly HashSet<MethodDefinition> indirectly_called = new HashSet<MethodDefinition> ();
		protected readonly HashSet<TypeDefinition> types_relevant_to_variant_casting = new HashSet<TypeDefinition> ();

		public AnnotationStore (LinkContext context)
		{
			this.context = context;
			FlowAnnotations = new FlowAnnotations (context);
			VirtualMethodsWithAnnotationsToValidate = new HashSet<MethodDefinition> ();
		}

		public bool ProcessSatelliteAssemblies { get; set; }

		protected Tracer Tracer {
			get {
				return context.Tracer;
			}
		}

		internal FlowAnnotations FlowAnnotations { get; }

		internal HashSet<MethodDefinition> VirtualMethodsWithAnnotationsToValidate { get; }

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
			if (method_actions.TryGetValue (method, out MethodAction action))
				return action;

			return MethodAction.Nothing;
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
			method_actions[method] = action;
		}

		public void SetMethodStubValue (MethodDefinition method, object value)
		{
			method_stub_values[method] = value;
		}

		public void SetFieldValue (FieldDefinition field, object value)
		{
			field_values[field] = value;
		}

		public void SetSubstitutedInit (FieldDefinition field)
		{
			field_init.Add (field);
		}

		public bool HasSubstitutedInit (FieldDefinition field)
		{
			return field_init.Contains (field);
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
			marked.Add (provider);
		}

		public void Mark (IMetadataTokenProvider provider, in DependencyInfo reason)
		{
			Debug.Assert (!(reason.Kind == DependencyKind.AlreadyMarked));
			marked.Add (provider);
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

		public bool IsMarked (IMetadataTokenProvider provider)
		{
			return marked.Contains (provider);
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

		public void Processed (IMetadataTokenProvider provider)
		{
			processed.Add (provider);
		}

		public bool IsProcessed (IMetadataTokenProvider provider)
		{
			return processed.Contains (provider);
		}

		public bool IsPreserved (TypeDefinition type)
		{
			return preserved_types.ContainsKey (type);
		}

		public void SetPreserve (TypeDefinition type, TypePreserve preserve)
		{
			if (preserved_types.TryGetValue (type, out TypePreserve existing))
				preserved_types[type] = ChoosePreserveActionWhichPreservesTheMost (existing, preserve);
			else
				preserved_types.Add (type, preserve);
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

		public TypePreserve GetPreserve (TypeDefinition type)
		{
			if (preserved_types.TryGetValue (type, out TypePreserve preserve))
				return preserve;

			throw new NotSupportedException ($"No type preserve information for `{type}`");
		}

		public bool TryGetPreserve (TypeDefinition type, out TypePreserve preserve)
		{
			return preserved_types.TryGetValue (type, out preserve);
		}

		public bool TryGetMethodStubValue (MethodDefinition method, out object value)
		{
			return method_stub_values.TryGetValue (method, out value);
		}

		public bool TryGetFieldUserValue (FieldDefinition field, out object value)
		{
			return field_values.TryGetValue (field, out value);
		}

		public HashSet<EmbeddedResource> GetResourcesToRemove (AssemblyDefinition assembly)
		{
			if (resources_to_remove.TryGetValue (assembly, out HashSet<EmbeddedResource> resources))
				return resources;

			return null;
		}

		public void AddResourceToRemove (AssemblyDefinition assembly, EmbeddedResource resource)
		{
			if (!resources_to_remove.TryGetValue (assembly, out HashSet<EmbeddedResource> resources))
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

		public void AddOverride (MethodDefinition @base, MethodDefinition @override, InterfaceImplementation matchingInterfaceImplementation = null)
		{
			if (!override_methods.TryGetValue (@base, out List<OverrideInformation> methods)) {
				methods = new List<OverrideInformation> ();
				override_methods.Add (@base, methods);
			}

			methods.Add (new OverrideInformation (@base, @override, matchingInterfaceImplementation));
		}

		public IEnumerable<OverrideInformation> GetOverrides (MethodDefinition method)
		{
			override_methods.TryGetValue (method, out List<OverrideInformation> overrides);
			return overrides;
		}

		public void AddDefaultInterfaceImplementation (MethodDefinition @base, TypeDefinition implementingType, InterfaceImplementation matchingInterfaceImplementation)
		{
			if (!default_interface_implementations.TryGetValue (@base, out var implementations)) {
				implementations = new List<(TypeDefinition, InterfaceImplementation)> ();
				default_interface_implementations.Add (@base, implementations);
			}

			implementations.Add ((implementingType, matchingInterfaceImplementation));
		}

		public IEnumerable<(TypeDefinition InstanceType, InterfaceImplementation ProvidingInterface)> GetDefaultInterfaceImplementations (MethodDefinition method)
		{
			default_interface_implementations.TryGetValue (method, out var ret);
			return ret;
		}

		public void AddBaseMethod (MethodDefinition method, MethodDefinition @base)
		{
			var methods = GetBaseMethods (method);
			if (methods == null) {
				methods = new List<MethodDefinition> ();
				base_methods[method] = methods;
			}

			methods.Add (@base);
		}

		public List<MethodDefinition> GetBaseMethods (MethodDefinition method)
		{
			if (base_methods.TryGetValue (method, out List<MethodDefinition> bases))
				return bases;

			return null;
		}

		public List<MethodDefinition> GetPreservedMethods (TypeDefinition type)
		{
			return GetPreservedMethods (type as IMemberDefinition);
		}

		public void AddPreservedMethod (TypeDefinition type, MethodDefinition method)
		{
			AddPreservedMethod (type as IMemberDefinition, method);
		}

		public List<MethodDefinition> GetPreservedMethods (MethodDefinition method)
		{
			return GetPreservedMethods (method as IMemberDefinition);
		}

		public void AddPreservedMethod (MethodDefinition key, MethodDefinition method)
		{
			AddPreservedMethod (key as IMemberDefinition, method);
		}

		List<MethodDefinition> GetPreservedMethods (IMemberDefinition definition)
		{
			if (preserved_methods.TryGetValue (definition, out List<MethodDefinition> preserved))
				return preserved;

			return null;
		}

		void AddPreservedMethod (IMemberDefinition definition, MethodDefinition method)
		{
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
			if (!symbol_readers.TryGetValue (assembly, out ISymbolReader symbolReader))
				return;

			symbol_readers.Remove (assembly);
			symbolReader.Dispose ();
		}

		public object GetCustomAnnotation (object key, IMetadataTokenProvider item)
		{
			if (!custom_annotations.TryGetValue (key, out Dictionary<IMetadataTokenProvider, object> slots))
				return null;

			if (!slots.TryGetValue (item, out object value))
				return null;

			return value;
		}

		public void SetCustomAnnotation (object key, IMetadataTokenProvider item, object value)
		{
			if (!custom_annotations.TryGetValue (key, out Dictionary<IMetadataTokenProvider, object> slots)) {
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
			if (!context.CustomAttributes.HasAttributes (member))
				return false;

			if (!linker_attributes.TryGetValue (member, out var linkerAttributeInformation)) {
				linkerAttributeInformation = new LinkerAttributesInformation (context, member);
				linker_attributes.Add (member, linkerAttributeInformation);
			}

			return linkerAttributeInformation.HasAttribute<T> ();
		}

		public IEnumerable<T> GetLinkerAttributes<T> (IMemberDefinition member) where T : Attribute
		{
			// Avoid setting up and inserting LinkerAttributesInformation for members without attributes.
			if (!context.CustomAttributes.HasAttributes (member))
				return Enumerable.Empty<T> ();

			if (!linker_attributes.TryGetValue (member, out var linkerAttributeInformation)) {
				linkerAttributeInformation = new LinkerAttributesInformation (context, member);
				linker_attributes.Add (member, linkerAttributeInformation);
			}

			return linkerAttributeInformation.GetAttributes<T> ();
		}

		public bool TryGetLinkerAttribute<T> (IMemberDefinition member, out T attribute) where T : Attribute
		{
			var attributes = GetLinkerAttributes<T> (member);
			if (attributes.Count () > 1) {
				context.LogWarning ($"Attribute '{typeof (T).FullName}' should only be used once on '{member}'.", 2027, member);
			}

			Debug.Assert (attributes.Count () <= 1);
			attribute = attributes.FirstOrDefault ();
			return attribute != null;
		}

		public void EnqueueVirtualMethod (MethodDefinition method)
		{
			if (!method.IsVirtual)
				return;

			if (FlowAnnotations.RequiresDataFlowAnalysis (method) || HasLinkerAttribute<RequiresUnreferencedCodeAttribute> (method))
				VirtualMethodsWithAnnotationsToValidate.Add (method);
		}
	}
}
