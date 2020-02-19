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

using Mono.Cecil;
using Mono.Collections.Generic;

namespace Mono.Linker.Steps
{

	public class ResolveFromAssemblyStep : ResolveStep
	{

		readonly AssemblyDefinition _assembly;
		readonly string _file;
		RootVisibility _rootVisibility;

		public enum RootVisibility
		{
			Any = 0,
			PublicAndFamily = 1,
			PublicAndFamilyAndAssembly = 2
		}


		public ResolveFromAssemblyStep (string assembly, RootVisibility rootVisibility = RootVisibility.Any)
		{
			_file = assembly;
			_rootVisibility = rootVisibility;
		}

		public ResolveFromAssemblyStep (AssemblyDefinition assembly)
		{
			_assembly = assembly;
		}

		protected override void Process ()
		{
			if (_assembly != null)
				Context.Resolver.CacheAssembly (_assembly);

			var ignoreUnresolved = Context.Resolver.IgnoreUnresolved;
			if (_rootVisibility == RootVisibility.PublicAndFamily) {
				Context.Resolver.IgnoreUnresolved = false;
			}

			AssemblyDefinition assembly = _assembly ?? Context.Resolve (_file);
			Context.Resolver.IgnoreUnresolved = ignoreUnresolved;
			if (_rootVisibility != RootVisibility.Any && HasInternalsVisibleTo (assembly)) {
				_rootVisibility = RootVisibility.PublicAndFamilyAndAssembly;
			}

			switch (assembly.MainModule.Kind) {
			case ModuleKind.Dll:
				ProcessLibrary (assembly);
				break;
			default:
				ProcessExecutable (assembly);
				break;
			}
		}

		protected virtual void ProcessLibrary (AssemblyDefinition assembly)
		{
			ProcessLibrary (Context, assembly, _rootVisibility);
		}

		public static void ProcessLibrary (LinkContext context, AssemblyDefinition assembly, RootVisibility rootVisibility = RootVisibility.Any)
		{
			var action = rootVisibility == RootVisibility.Any ? AssemblyAction.Copy : AssemblyAction.Link;
			context.SetAction (assembly, action);

			context.Tracer.Push (assembly);

			foreach (TypeDefinition type in assembly.MainModule.Types)
				MarkType (context, type, rootVisibility);

			if (assembly.MainModule.HasExportedTypes) {
				foreach (var exported in assembly.MainModule.ExportedTypes) {
					bool isForwarder = exported.IsForwarder;
					var declaringType = exported.DeclaringType;
					while (!isForwarder && (declaringType != null)) {
						isForwarder = declaringType.IsForwarder;
						declaringType = declaringType.DeclaringType;
					}

					if (!isForwarder)
						continue;
					TypeDefinition resolvedExportedType = exported.Resolve ();

					if (resolvedExportedType == null) {
						//
						// It's quite common for assemblies to have broken exported types
						//
						// One source of them is from native csc which added all nested types of
						// type-forwarded types automatically including private ones. 
						//
						// Next source of broken type-forwarders is from custom metadata writers which
						// simply write bogus information.
						//
						// Both cases are bugs not on our end but we still want to link all assemblies
						// especially when such types cannot be used anyway
						//
						context.LogMessage ($"Cannot find declaration of exported type '{exported}' from the assembly '{assembly}'");

						continue;
					}

					context.Resolve (resolvedExportedType.Scope);
					MarkType (context, resolvedExportedType, rootVisibility);
					context.MarkingHelpers.MarkExportedType (exported, assembly.MainModule);
				}
			}

			context.Tracer.Pop ();
		}

		static void MarkType (LinkContext context, TypeDefinition type, RootVisibility rootVisibility)
		{
			bool markType = rootVisibility switch {
				RootVisibility.PublicAndFamilyAndAssembly => !type.IsNestedPrivate,
				RootVisibility.PublicAndFamily => type.IsPublic || type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamilyOrAssembly,
				_ => true
			};

			if (!markType) {
				return;
			}

			context.Annotations.MarkAndPush (type);

			if (type.HasFields)
				MarkFields (context, type.Fields, rootVisibility);
			if (type.HasMethods)
				MarkMethods (context, type.Methods, rootVisibility);
			if (type.HasNestedTypes)
				foreach (var nested in type.NestedTypes)
					MarkType (context, nested, rootVisibility);

			context.Tracer.Pop ();
		}

		void ProcessExecutable (AssemblyDefinition assembly)
		{
			Context.SetAction (assembly, AssemblyAction.Link);

			Tracer.Push (assembly);

			Annotations.Mark (assembly.EntryPoint.DeclaringType);

			MarkMethod (Context, assembly.EntryPoint, MethodAction.Parse, RootVisibility.Any);

			Tracer.Pop ();
		}

		static void MarkFields (LinkContext context, Collection<FieldDefinition> fields, RootVisibility rootVisibility)
		{
			foreach (FieldDefinition field in fields) {
				bool markField = rootVisibility switch {
					RootVisibility.PublicAndFamily => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly,
					RootVisibility.PublicAndFamilyAndAssembly => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly || field.IsAssembly || field.IsFamilyAndAssembly,
					_ => true
				};
				if (markField) {
					context.Annotations.Mark (field);
				}
			}
		}

		static void MarkMethods (LinkContext context, Collection<MethodDefinition> methods, RootVisibility rootVisibility)
		{
			foreach (MethodDefinition method in methods)
				MarkMethod (context, method, MethodAction.ForceParse, rootVisibility);
		}

		static void MarkMethod (LinkContext context, MethodDefinition method, MethodAction action, RootVisibility rootVisibility)
		{
			bool markMethod = rootVisibility switch {
				RootVisibility.PublicAndFamily => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly,
				RootVisibility.PublicAndFamilyAndAssembly => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly || method.IsAssembly || method.IsFamilyAndAssembly,
				_ => true
			};

			if (markMethod) {
				context.Annotations.Mark (method);
				context.Annotations.SetAction (method, action);
			}
		}

		static bool HasInternalsVisibleTo (AssemblyDefinition assembly)
		{
			foreach (CustomAttribute attribute in assembly.CustomAttributes) {
				if (attribute.Constructor.DeclaringType.FullName ==
					"System.Runtime.CompilerServices.InternalsVisibleToAttribute")
					return true;
			}

			return false;
		}
	}
}
