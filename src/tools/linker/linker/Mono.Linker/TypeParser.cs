//
// TypeParser.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2010 Jb Evain
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

using Mono.Cecil;

namespace Mono.Linker {

	class TypeParser {

		class Type {
			public const int Ptr = -1;
			public const int ByRef = -2;
			public const int SzArray = -3;

			public string type_fullname;
			public string [] nested_names;
			public int arity;
			public int [] specs;
			public Type [] generic_arguments;
			public string assembly;
		}

		readonly string fullname;
		readonly int length;

		int position;

		TypeParser (string fullname)
		{
			this.fullname = fullname;
			this.length = fullname.Length;
		}

		Type ParseType (bool fq_name)
		{
			var type = new Type ();
			type.type_fullname = ParsePart ();

			type.nested_names = ParseNestedNames ();

			if (TryGetArity (type))
				type.generic_arguments = ParseGenericArguments (type.arity);

			type.specs = ParseSpecs ();

			if (fq_name)
				type.assembly = ParseAssemblyName ();

			return type;
		}

		static bool TryGetArity (Type type)
		{
			int arity = 0;

			TryAddArity (type.type_fullname, ref arity);

			var nested_names = type.nested_names;
			if (!IsNullOrEmpty (nested_names)) {
				for (int i = 0; i < nested_names.Length; i++)
					TryAddArity (nested_names [i], ref arity);
			}

			type.arity = arity;
			return arity > 0;
		}

		static bool TryGetArity (string name, out int arity)
		{
			arity = 0;
			var index = name.LastIndexOf ('`');
			if (index == -1)
				return false;

			return int.TryParse (name.Substring (index + 1), out arity);
		}

		static void TryAddArity (string name, ref int arity)
		{
			int type_arity;
			if (!TryGetArity (name, out type_arity))
				return;

			arity += type_arity;
		}

		string ParsePart ()
		{
			int start = position;
			while (position < length && !IsDelimiter (fullname [position]))
				position++;

			return fullname.Substring (start, position - start);
		}

		static bool IsDelimiter (char chr)
		{
			return "+,[]*&".IndexOf (chr) != -1;
		}

		void TryParseWhiteSpace ()
		{
			while (position < length && Char.IsWhiteSpace (fullname [position]))
				position++;
		}

		string [] ParseNestedNames ()
		{
			string [] nested_names = null;
			while (TryParse ('+'))
				Add (ref nested_names, ParsePart ());

			return nested_names;
		}

		bool TryParse (char chr)
		{
			if (position < length && fullname [position] == chr) {
				position++;
				return true;
			}

			return false;
		}

		static void Add<T> (ref T [] array, T item)
		{
			if (array == null) {
				array = new [] { item };
				return;
			}

			Array.Resize (ref array, array.Length + 1);
			array [array.Length - 1] = item;
		}

		int [] ParseSpecs ()
		{
			int [] specs = null;

			while (position < length) {
				switch (fullname [position]) {
				case '*':
					position++;
					Add (ref specs, Type.Ptr);
					break;
				case '&':
					position++;
					Add (ref specs, Type.ByRef);
					break;
				case '[':
					position++;
					switch (fullname [position]) {
					case ']':
						position++;
						Add (ref specs, Type.SzArray);
						break;
					case '*':
						position++;
						Add (ref specs, 1);
						break;
					default:
						var rank = 1;
						while (TryParse (','))
							rank++;

						Add (ref specs, rank);

						TryParse (']');
						break;
					}
					break;
				default:
					return specs;
				}
			}

			return specs;
		}

		Type [] ParseGenericArguments (int arity)
		{
			Type [] generic_arguments = null;

			if (position == length || fullname [position] != '[')
				return generic_arguments;

			TryParse ('[');

			for (int i = 0; i < arity; i++) {
				var fq_argument = TryParse ('[');
				Add (ref generic_arguments, ParseType (fq_argument));
				if (fq_argument)
					TryParse (']');

				TryParse (',');
				TryParseWhiteSpace ();
			}

			TryParse (']');

			return generic_arguments;
		}

		string ParseAssemblyName ()
		{
			if (!TryParse (','))
				return string.Empty;

			TryParseWhiteSpace ();

			var start = position;
			while (position < length) {
				var chr = fullname [position];
				if (chr == '[' || chr == ']')
					break;

				position++;
			}

			return fullname.Substring (start, position - start);
		}

		public static TypeReference ParseType (ModuleDefinition module, string fullname)
		{
			if (fullname == null)
				return null;

			var parser = new TypeParser (fullname);
			return GetTypeReference (module, parser.ParseType (true));
		}

		static TypeReference GetTypeReference (ModuleDefinition module, Type type_info)
		{
			TypeReference type;
			if (!TryGetDefinition (module, type_info, out type))
				type = CreateReference (type_info, module, GetMetadataScope (module, type_info));

			return CreateSpecs (type, type_info);
		}

		static TypeReference CreateSpecs (TypeReference type, Type type_info)
		{
			type = TryCreateGenericInstanceType (type, type_info);

			var specs = type_info.specs;
			if (IsNullOrEmpty (specs))
				return type;

			for (int i = 0; i < specs.Length; i++) {
				switch (specs [i]) {
				case Type.Ptr:
					type = new PointerType (type);
					break;
				case Type.ByRef:
					type = new ReferenceType (type);
					break;
				case Type.SzArray:
					type = new ArrayType (type);
					break;
				default:
					var array = new ArrayType (type);
					array.Dimensions.Clear ();

					for (int j = 0; j < specs [i]; j++)
						array.Dimensions.Add (new ArrayDimension (0, 0));

					type = array;
					break;
				}
			}

			return type;
		}

		static TypeReference TryCreateGenericInstanceType (TypeReference type, Type type_info)
		{
			var generic_arguments = type_info.generic_arguments;
			if (IsNullOrEmpty (generic_arguments))
				return type;

			var instance = new GenericInstanceType (type);
			for (int i = 0; i < generic_arguments.Length; i++)
				instance.GenericArguments.Add (GetTypeReference (type.Module, generic_arguments [i]));

			return instance;
		}

		public static void SplitFullName (string fullname, out string @namespace, out string name)
		{
			var last_dot = fullname.LastIndexOf ('.');

			if (last_dot == -1) {
				@namespace = string.Empty;
				name = fullname;
			} else {
				@namespace = fullname.Substring (0, last_dot);
				name = fullname.Substring (last_dot + 1);
			}
		}

		static TypeReference CreateReference (Type type_info, ModuleDefinition module, IMetadataScope scope)
		{
			string @namespace, name;
			SplitFullName (type_info.type_fullname, out @namespace, out name);

			var type = new TypeReference (name, @namespace, scope, false) {
				Module = module,
			};

			AdjustGenericParameters (type);

			var nested_names = type_info.nested_names;
			if (IsNullOrEmpty (nested_names))
				return type;

			for (int i = 0; i < nested_names.Length; i++) {
				type = new TypeReference (nested_names [i], string.Empty, null, false) {
					DeclaringType = type,
					Module = module,
				};

				AdjustGenericParameters (type);
			}

			return type;
		}

		static void AdjustGenericParameters (TypeReference type)
		{
			int arity;
			if (!TryGetArity (type.Name, out arity))
				return;

			for (int i = 0; i < arity; i++)
				type.GenericParameters.Add (new GenericParameter (null, type));
		}

		static IMetadataScope GetMetadataScope (ModuleDefinition module, Type type_info)
		{
			if (string.IsNullOrEmpty (type_info.assembly))
				return GetCorlib (module);

			return MatchReference (module, AssemblyNameReference.Parse (type_info.assembly));
		}

		static AssemblyNameReference GetCorlib (ModuleDefinition module)
		{
			foreach (AssemblyNameReference reference in module.AssemblyReferences)
				if (reference.Name == "mscorlib")
					return reference;

			return null;
		}

		static AssemblyNameReference MatchReference (ModuleDefinition module, AssemblyNameReference pattern)
		{
			var references = module.AssemblyReferences;

			for (int i = 0; i < references.Count; i++) {
				var reference = references [i];
				if (reference.FullName == pattern.FullName)
					return reference;
			}

			return pattern;
		}

		static bool TryGetDefinition (ModuleDefinition module, Type type_info, out TypeReference type)
		{
			type = null;
			if (!TryCurrentModule (module, type_info))
				return false;

			var typedef = module.Types [type_info.type_fullname];
			if (typedef == null)
				return false;

			var nested_names = type_info.nested_names;
			if (!IsNullOrEmpty (nested_names)) {
				for (int i = 0; i < nested_names.Length; i++)
					typedef = GetNestedType (typedef, nested_names [i]);
			}

			type = typedef;
			return true;
		}

		static bool TryCurrentModule (ModuleDefinition module, Type type_info)
		{
			if (string.IsNullOrEmpty (type_info.assembly))
				return true;

			if (module.Assembly != null && module.Assembly.Name.FullName == type_info.assembly)
				return true;

			return false;
		}

		static TypeDefinition GetNestedType (TypeDefinition type, string nestedTypeName)
		{
			if (!type.HasNestedTypes)
				return null;

			foreach (TypeDefinition nested_type in type.NestedTypes)
				if (nested_type.Name == nestedTypeName)
					return nested_type;

			return null;
		}

		static bool IsNullOrEmpty<T> (T [] array)
		{
			return array == null || array.Length == 0;
		}
	}
}
