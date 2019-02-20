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

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker {

	public partial class AnnotationStore {

		protected readonly LinkContext context;

		protected readonly Dictionary<AssemblyDefinition, AssemblyAction> assembly_actions = new Dictionary<AssemblyDefinition, AssemblyAction> ();
		protected readonly Dictionary<MethodDefinition, MethodAction> method_actions = new Dictionary<MethodDefinition, MethodAction> ();
		protected readonly HashSet<IMetadataTokenProvider> marked = new HashSet<IMetadataTokenProvider> ();
		protected readonly HashSet<IMetadataTokenProvider> processed = new HashSet<IMetadataTokenProvider> ();
		protected readonly Dictionary<TypeDefinition, TypePreserve> preserved_types = new Dictionary<TypeDefinition, TypePreserve> ();
		protected readonly Dictionary<IMemberDefinition, List<MethodDefinition>> preserved_methods = new Dictionary<IMemberDefinition, List<MethodDefinition>> ();
		protected readonly HashSet<IMetadataTokenProvider> public_api = new HashSet<IMetadataTokenProvider> ();
		protected readonly Dictionary<MethodDefinition, List<MethodDefinition>> override_methods = new Dictionary<MethodDefinition, List<MethodDefinition>> ();
		protected readonly Dictionary<MethodDefinition, List<MethodDefinition>> base_methods = new Dictionary<MethodDefinition, List<MethodDefinition>> ();
		protected readonly Dictionary<AssemblyDefinition, ISymbolReader> symbol_readers = new Dictionary<AssemblyDefinition, ISymbolReader> ();
		protected readonly Dictionary<TypeDefinition, List<TypeDefinition>> class_type_base_hierarchy = new Dictionary<TypeDefinition, List<TypeDefinition>> ();
		protected readonly Dictionary<TypeDefinition, List<TypeDefinition>> derived_interfaces = new Dictionary<TypeDefinition, List<TypeDefinition>>();

		protected readonly Dictionary<object, Dictionary<IMetadataTokenProvider, object>> custom_annotations = new Dictionary<object, Dictionary<IMetadataTokenProvider, object>> ();
		protected readonly Dictionary<AssemblyDefinition, HashSet<string>> resources_to_remove = new Dictionary<AssemblyDefinition, HashSet<string>> ();
		protected readonly HashSet<CustomAttribute> marked_attributes = new HashSet<CustomAttribute> ();
		readonly HashSet<TypeDefinition> marked_types_with_cctor = new HashSet<TypeDefinition> ();
		protected readonly HashSet<TypeDefinition> marked_instantiated = new HashSet<TypeDefinition> ();

		public AnnotationStore (LinkContext context) => this.context = context;

		protected Tracer Tracer {
			get {
				return context.Tracer;
			}
		}

		[Obsolete ("Use Tracer in LinkContext directly")]
		public void PrepareDependenciesDump ()
		{
			Tracer.Start ();
		}

		[Obsolete ("Use Tracer in LinkContext directly")]
		public void PrepareDependenciesDump (string filename)
		{
			Tracer.DependenciesFileName = filename;
			Tracer.Start ();
		}

		public ICollection<AssemblyDefinition> GetAssemblies ()
		{
			return assembly_actions.Keys;
		}

		public AssemblyAction GetAction (AssemblyDefinition assembly)
		{
			AssemblyAction action;
			if (assembly_actions.TryGetValue (assembly, out action))
				return action;

			throw new InvalidOperationException($"No action for the assembly {assembly.Name} defined");
		}

		public MethodAction GetAction (MethodDefinition method)
		{
			MethodAction action;
			if (method_actions.TryGetValue (method, out action))
				return action;

			return MethodAction.Nothing;
		}

		public void SetAction (AssemblyDefinition assembly, AssemblyAction action)
		{
			assembly_actions [assembly] = action;
		}

		public bool HasAction (AssemblyDefinition assembly)
		{
			return assembly_actions.ContainsKey (assembly);
		}

		public void SetAction (MethodDefinition method, MethodAction action)
		{
			method_actions [method] = action;
		}

		public void Mark (IMetadataTokenProvider provider)
		{
			marked.Add (provider);
			Tracer.AddDependency (provider, true);
		}

		public void Mark (CustomAttribute attribute)
		{
			marked_attributes.Add (attribute);
		}

		public void MarkAndPush (IMetadataTokenProvider provider)
		{
			Mark (provider);
			Tracer.Push (provider, false);
		}

		public bool IsMarked (IMetadataTokenProvider provider)
		{
			return marked.Contains (provider);
		}

		public bool IsMarked (CustomAttribute attribute)
		{
			return marked_attributes.Contains (attribute);
		}

		public void MarkInstantiated (TypeDefinition type)
		{
			marked_instantiated.Add (type);
		}

		public bool IsInstantiated (TypeDefinition type)
		{
			return marked_instantiated.Contains (type);
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
			TypePreserve existing;
			if (preserved_types.TryGetValue (type, out existing))
				preserved_types [type] = ChoosePreserveActionWhichPreservesTheMost (existing, preserve);
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
			TypePreserve preserve;
			if (preserved_types.TryGetValue (type, out preserve))
				return preserve;

			throw new NotSupportedException ($"No type preserve information for `{type}`");
		}

		public bool TryGetPreserve (TypeDefinition type, out TypePreserve preserve)
		{
			return preserved_types.TryGetValue (type, out preserve);
		}

		public HashSet<string> GetResourcesToRemove (AssemblyDefinition assembly)
		{
			HashSet<string> resources;
			if (resources_to_remove.TryGetValue (assembly, out resources))
				return resources;

			return null;
		}

		public void AddResourceToRemove (AssemblyDefinition assembly, string name)
		{
			HashSet<string> resources;
			if (!resources_to_remove.TryGetValue (assembly, out resources)) {
				resources = resources_to_remove [assembly] = new HashSet<string> ();
			}

			resources.Add (name);
		}

		public void SetPublic (IMetadataTokenProvider provider)
		{
			public_api.Add (provider);
		}

		public bool IsPublic (IMetadataTokenProvider provider)
		{
			return public_api.Contains (provider);
		}

		public void AddOverride (MethodDefinition @base, MethodDefinition @override)
		{
			var methods = GetOverrides (@base);
			if (methods == null) {
				methods = new List<MethodDefinition> ();
				override_methods [@base] = methods;
			}

			methods.Add (@override);
		}

		public List<MethodDefinition> GetOverrides (MethodDefinition method)
		{
			List<MethodDefinition> overrides;
			if (override_methods.TryGetValue (method, out overrides))
				return overrides;

			return null;
		}

		public void AddBaseMethod (MethodDefinition method, MethodDefinition @base)
		{
			var methods = GetBaseMethods (method);
			if (methods == null) {
				methods = new List<MethodDefinition> ();
				base_methods [method] = methods;
			}

			methods.Add (@base);
		}

		public List<MethodDefinition> GetBaseMethods (MethodDefinition method)
		{
			List<MethodDefinition> bases;
			if (base_methods.TryGetValue (method, out bases))
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
			List<MethodDefinition> preserved;
			if (preserved_methods.TryGetValue (definition, out preserved))
				return preserved;

			return null;
		}

		void AddPreservedMethod (IMemberDefinition definition, MethodDefinition method)
		{
			var methods = GetPreservedMethods (definition);
			if (methods == null) {
				methods = new List<MethodDefinition> ();
				preserved_methods [definition] = methods;
			}

			methods.Add (method);
		}

		public void AddSymbolReader (AssemblyDefinition assembly, ISymbolReader symbolReader)
		{
			symbol_readers [assembly] = symbolReader;
		}

		public void CloseSymbolReader (AssemblyDefinition assembly)
		{
			ISymbolReader symbolReader;
			if (!symbol_readers.TryGetValue (assembly, out symbolReader))
				return;

			symbol_readers.Remove (assembly);
			symbolReader.Dispose ();
		}

		public Dictionary<IMetadataTokenProvider, object> GetCustomAnnotations (object key)
		{
			Dictionary<IMetadataTokenProvider, object> slots;
			if (custom_annotations.TryGetValue (key, out slots))
				return slots;

			slots = new Dictionary<IMetadataTokenProvider, object> ();
			custom_annotations.Add (key, slots);
			return slots;
		}

		public bool HasPreservedStaticCtor (TypeDefinition type)
		{
			return marked_types_with_cctor.Contains (type);
		}

		public bool SetPreservedStaticCtor (TypeDefinition type)
		{
			return marked_types_with_cctor.Add (type);
		}

		public void SetClassHierarchy (TypeDefinition type, List<TypeDefinition> bases)
		{
			class_type_base_hierarchy [type] = bases;
		}

		public List<TypeDefinition> GetClassHierarchy (TypeDefinition type)
		{
			if (class_type_base_hierarchy.TryGetValue (type, out List<TypeDefinition> bases))
				return bases;

			return null;
		}

		public void AddDerivedInterfaceForInterface (TypeDefinition @base, TypeDefinition derived)
		{
			if (!@base.IsInterface)
				throw new ArgumentException ($"{nameof (@base)} must be an interface");

			if (!derived.IsInterface)
				throw new ArgumentException ($"{nameof (derived)} must be an interface");

			List<TypeDefinition> derivedInterfaces;
			if (!derived_interfaces.TryGetValue (@base, out derivedInterfaces))
				derived_interfaces [@base] = derivedInterfaces = new List<TypeDefinition> ();
			
			derivedInterfaces.Add(derived);
		}

		public List<TypeDefinition> GetDerivedInterfacesForInterface (TypeDefinition @interface)
		{
			if (!@interface.IsInterface)
				throw new ArgumentException ($"{nameof (@interface)} must be an interface");
			
			List<TypeDefinition> derivedInterfaces;
			if (derived_interfaces.TryGetValue (@interface, out derivedInterfaces))
				return derivedInterfaces;

			return null;
		}
	}
}
