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
using System.Collections;
using System.Collections.Generic;

using Mono.Cecil;

namespace Mono.Linker {

	public class AnnotationStore {

		readonly Dictionary<AssemblyDefinition, AssemblyAction> assembly_actions = new Dictionary<AssemblyDefinition, AssemblyAction> ();
		readonly Dictionary<MethodDefinition, MethodAction> method_actions = new Dictionary<MethodDefinition, MethodAction> ();
		readonly HashSet<IMetadataTokenProvider> marked = new HashSet<IMetadataTokenProvider> ();
		readonly HashSet<IMetadataTokenProvider> processed = new HashSet<IMetadataTokenProvider> ();
		readonly Dictionary<TypeDefinition, TypePreserve> preserved_types = new Dictionary<TypeDefinition, TypePreserve> ();
		readonly Dictionary<IMemberDefinition, List<MethodDefinition>> preserved_methods = new Dictionary<IMemberDefinition, List<MethodDefinition>> ();
		readonly HashSet<IMetadataTokenProvider> public_api = new HashSet<IMetadataTokenProvider> ();
		readonly Dictionary<MethodDefinition, List<MethodDefinition>> override_methods = new Dictionary<MethodDefinition, List<MethodDefinition>> ();
		readonly Dictionary<MethodDefinition, List<MethodDefinition>> base_methods = new Dictionary<MethodDefinition, List<MethodDefinition>> ();

		readonly Dictionary<object, Dictionary<IMetadataTokenProvider, object>> custom_annotations = new Dictionary<object, Dictionary<IMetadataTokenProvider, object>> ();

		public AssemblyAction GetAction (AssemblyDefinition assembly)
		{
			AssemblyAction action;
			if (assembly_actions.TryGetValue (assembly, out action))
				return action;

			throw new NotSupportedException ();
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
		}

		public bool IsMarked (IMetadataTokenProvider provider)
		{
			return marked.Contains (provider);
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
			preserved_types [type] = preserve;
		}

		public TypePreserve GetPreserve (TypeDefinition type)
		{
			TypePreserve preserve;
			if (preserved_types.TryGetValue (type, out preserve))
				return preserve;

			throw new NotSupportedException ();
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

		public Dictionary<IMetadataTokenProvider, object> GetCustomAnnotations (object key)
		{
			Dictionary<IMetadataTokenProvider, object> slots;
			if (custom_annotations.TryGetValue (key, out slots))
				return slots;

			slots = new Dictionary<IMetadataTokenProvider, object> ();
			custom_annotations.Add (key, slots);
			return slots;
		}
	}
}
