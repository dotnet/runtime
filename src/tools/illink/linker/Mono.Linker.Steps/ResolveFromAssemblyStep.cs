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

		protected override void Process ()
		{
			if (_assembly != null) {
				Context.SafeLoadSymbols (_assembly);
				Context.Resolver.CacheAssembly (_assembly);
			}

			AssemblyDefinition assembly = _assembly ?? Context.Resolve (_file);

			switch (assembly.MainModule.Kind) {
			case ModuleKind.Dll:
				ProcessLibrary (Context, assembly);
				return;
			default:
				ProcessExecutable (assembly);
				return;
			}
		}

		public static void ProcessLibrary (LinkContext context, AssemblyDefinition assembly)
		{
			context.Annotations.SetAction (assembly, AssemblyAction.Copy);

			foreach (TypeDefinition type in assembly.MainModule.Types)
				MarkType (context, type);
		}

		static void MarkType (LinkContext context, TypeDefinition type)
		{
			context.Annotations.Mark (type);

			if (type.HasFields)
				MarkFields (context, type.Fields);
			if (type.HasMethods)
				MarkMethods (context, type.Methods);
			if (type.HasNestedTypes)
				foreach (var nested in type.NestedTypes)
					MarkType (context, nested);
		}

		void ProcessExecutable (AssemblyDefinition assembly)
		{
			Annotations.SetAction (assembly, AssemblyAction.Link);

			Annotations.Mark (assembly.EntryPoint.DeclaringType);
			MarkMethod (Context, assembly.EntryPoint, MethodAction.Parse);
		}

		static void MarkFields (LinkContext context, ICollection fields)
		{
			foreach (FieldDefinition field in fields)
				context.Annotations.Mark (field);
		}

		static void MarkMethods (LinkContext context, ICollection methods)
		{
			foreach (MethodDefinition method in methods)
				MarkMethod (context, method, MethodAction.ForceParse);
		}

		static void MarkMethod (LinkContext context, MethodDefinition method, MethodAction action)
		{
			context.Annotations.Mark (method);
			context.Annotations.SetAction (method, action);
		}
	}
}
