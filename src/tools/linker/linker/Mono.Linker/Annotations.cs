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

using Mono.Cecil;

namespace Mono.Linker {

	public class Annotations {

		private static readonly object _actionKey = new object ();
		private static readonly object _markedKey = new object ();
		private static readonly object _processedKey = new object ();
		private static readonly object _preservedKey = new object ();
		private static readonly object _preservedMethodsKey = new object ();
		private static readonly object _publicKey = new object ();
		private static readonly object _symbolsKey = new object ();
		private static readonly object _overrideKey = new object ();
		private static readonly object _baseKey = new object ();

		public static AssemblyAction GetAction (AssemblyDefinition assembly)
		{
			return (AssemblyAction) GetAction (AsProvider (assembly));
		}

		public static MethodAction GetAction (MethodDefinition method)
		{
			var action = GetAction (AsProvider (method));
			return action == null ? MethodAction.Nothing : (MethodAction) action;
		}

		static object GetAction (IAnnotationProvider provider)
		{
			return provider.Annotations [_actionKey];
		}

		public static bool HasAction (IAnnotationProvider provider)
		{
			return provider.Annotations.Contains (_actionKey);
		}

		public static void SetAction (AssemblyDefinition assembly, AssemblyAction action)
		{
			SetAction (AsProvider (assembly), action);
		}

		public static void SetAction (MethodDefinition method, MethodAction action)
		{
			SetAction (AsProvider (method), action);
		}

		static void SetAction (IAnnotationProvider provider, object action)
		{
			provider.Annotations [_actionKey] = action;
		}

		public static void Mark (IAnnotationProvider provider)
		{
			provider.Annotations [_markedKey] = _markedKey;
		}

		public static bool IsMarked (IAnnotationProvider provider)
		{
			return provider.Annotations.Contains (_markedKey);
		}

		public static void Processed (IAnnotationProvider provider)
		{
			provider.Annotations [_processedKey] = _processedKey;
		}

		public static bool IsProcessed (IAnnotationProvider provider)
		{
			return provider.Annotations.Contains (_processedKey);
		}

		public static bool IsPreserved (TypeDefinition type)
		{
			return AsProvider (type).Annotations.Contains (_preservedKey);
		}

		public static void SetPreserve (TypeDefinition type, TypePreserve preserve)
		{
			AsProvider (type).Annotations [_preservedKey] = preserve;
		}

		public static TypePreserve GetPreserve (TypeDefinition type)
		{
			return (TypePreserve) AsProvider (type).Annotations [_preservedKey];
		}

		public static void SetPublic (IAnnotationProvider provider)
		{
			provider.Annotations [_publicKey] = _publicKey;
		}

		public static bool IsPublic (IAnnotationProvider provider)
		{
			return provider.Annotations.Contains (_publicKey);
		}

		static IAnnotationProvider AsProvider (object obj)
		{
			return (IAnnotationProvider) obj;
		}

		public static bool HasSymbols (AssemblyDefinition assembly)
		{
			return AsProvider (assembly).Annotations.Contains (_symbolsKey);
		}

		public static void SetHasSymbols (AssemblyDefinition assembly)
		{
			AsProvider (assembly).Annotations [_symbolsKey] = _symbolsKey;
		}

		public static void AddOverride (MethodDefinition @base, MethodDefinition @override)
		{
			ArrayList methods = (ArrayList) GetOverrides (@base);
			if (methods == null) {
				methods = new ArrayList ();
				AsProvider (@base).Annotations.Add (_overrideKey, methods);
			}

			methods.Add (@override);
		}

		public static IList GetOverrides (MethodDefinition method)
		{
			return (IList) AsProvider (method).Annotations [_overrideKey];
		}

		public static void AddBaseMethod (MethodDefinition method, MethodDefinition @base)
		{
			ArrayList methods = (ArrayList) GetBaseMethods (method);
			if (methods == null) {
				methods = new ArrayList ();
				AsProvider (method).Annotations.Add (_baseKey, methods);
			}

			methods.Add (@base);
		}

		public static IList GetBaseMethods (MethodDefinition method)
		{
			return (IList) AsProvider (method).Annotations [_baseKey];
		}

		public static IList GetPreservedMethods (TypeDefinition type)
		{
			return (IList) AsProvider (type).Annotations [_preservedMethodsKey];
		}

		public static void AddPreservedMethod (TypeDefinition type, MethodDefinition method)
		{
			ArrayList methods = (ArrayList) GetPreservedMethods (type);
			if (methods == null) {
				methods = new ArrayList ();
				AsProvider (type).Annotations.Add (_preservedMethodsKey, methods);
			}

			methods.Add (method);
		}

		private Annotations ()
		{
		}
	}
}
