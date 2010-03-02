//
// ResolveFromAssemblyStep.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
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

using System.Collections;
using Mono.Cecil;

namespace Mono.Linker.Steps {

	public class ResolveFromAssemblyStep : ResolveStep {

		AssemblyDefinition _assembly;
		string _file;

		public ResolveFromAssemblyStep (string assembly)
		{
			_file = assembly;
		}

		public ResolveFromAssemblyStep (AssemblyDefinition assembly)
		{
			_assembly = assembly;
		}

		public override void Process (LinkContext context)
		{
			if (_assembly != null) {
				context.SafeLoadSymbols (_assembly);
				context.Resolver.CacheAssembly (_assembly);
			}

			AssemblyDefinition assembly = _assembly ?? context.Resolve (_file);

			switch (assembly.Kind) {
			case AssemblyKind.Dll:
				ProcessLibrary (assembly);
				return;
			default:
				ProcessExecutable (assembly);
				return;
			}
		}

		public static void ProcessLibrary (AssemblyDefinition assembly)
		{
			Annotations.SetAction (assembly, AssemblyAction.Copy);

			foreach (TypeDefinition type in assembly.MainModule.Types) {
				Annotations.Mark (type);

				if (type.HasFields)
					MarkFields (type.Fields);
				if (type.HasMethods)
					MarkMethods (type.Methods);
				if (type.HasConstructors)
					MarkMethods (type.Constructors);
			}
		}

		static void ProcessExecutable (AssemblyDefinition assembly)
		{
			Annotations.SetAction (assembly, AssemblyAction.Link);

			Annotations.Mark (assembly.EntryPoint.DeclaringType);
			MarkMethod (assembly.EntryPoint, MethodAction.Parse);
		}

		static void MarkFields (ICollection fields)
		{
			foreach (FieldDefinition field in fields)
				Annotations.Mark (field);
		}

		static void MarkMethods (ICollection methods)
		{
			foreach (MethodDefinition method in methods)
				MarkMethod (method, MethodAction.ForceParse);
		}

		static void MarkMethod (MethodDefinition method, MethodAction action)
		{
			Annotations.Mark (method);
			Annotations.SetAction (method, action);
		}
	}
}
