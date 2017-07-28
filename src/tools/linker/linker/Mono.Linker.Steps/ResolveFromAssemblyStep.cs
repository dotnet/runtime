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

		AssemblyDefinition _assembly;
		string _file;
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

			AssemblyDefinition assembly = _assembly ?? Context.Resolve (_file);

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

		protected static void SetAction (LinkContext context, AssemblyDefinition assembly, AssemblyAction action)
		{
			TryReadSymbols (context, assembly);

			context.Annotations.SetAction (assembly, action);
		}

		static void TryReadSymbols (LinkContext context, AssemblyDefinition assembly)
		{
			context.SafeReadSymbols (assembly);
		}

		protected virtual void ProcessLibrary (AssemblyDefinition assembly)
		{
			ProcessLibrary (Context, assembly, _rootVisibility);
		}

		public static void ProcessLibrary (LinkContext context, AssemblyDefinition assembly, RootVisibility rootVisibility = RootVisibility.Any)
		{
			var action = rootVisibility == RootVisibility.Any ? AssemblyAction.Copy : AssemblyAction.Link;
			SetAction (context, assembly, action);

			context.Annotations.Push (assembly);

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
					TypeDefinition resolvedExportedType = null;
					try {
						resolvedExportedType = exported.Resolve ();
					} catch (AssemblyResolutionException) {
						continue;
					}
					if (resolvedExportedType == null) {
						// we ignore the nested forwarders here as a workaround for older csc bug,
						// where it was adding nested forwarders to exported types, even when the nested type was not public
						// see https://bugzilla.xamarin.com/show_bug.cgi?id=57645#c13
						if (exported.DeclaringType != null) {
							if (context.LogInternalExceptions)
								System.Console.WriteLine ($"warning: unable to resolve exported nested type: {exported} (declaring type: {exported.DeclaringType}) from the assembly: {assembly}");

							continue;
						}
						throw new LoadException ($"unable to resolve exported forwarded type: {exported} from the assembly: {assembly}");
					}

					context.Resolve (resolvedExportedType.Scope);
					MarkType (context, resolvedExportedType, rootVisibility);
					context.Annotations.Mark (exported);
					if (context.KeepTypeForwarderOnlyAssemblies) {
						context.Annotations.Mark (assembly.MainModule);
					}
				}
			}

			context.Annotations.Pop ();
		}

		static void MarkType (LinkContext context, TypeDefinition type, RootVisibility rootVisibility)
		{
			bool markType;
			switch (rootVisibility) {
			default:
				markType = true;
				break;

			case RootVisibility.PublicAndFamilyAndAssembly:
				markType = !type.IsNestedPrivate;
				break;

			case RootVisibility.PublicAndFamily:
				markType = type.IsPublic || type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamilyOrAssembly;
				break;
			}

			if (!markType) {
				return;
			}

			context.Annotations.Mark (type);

			context.Annotations.Push (type);

			if (type.HasFields)
				MarkFields (context, type.Fields, rootVisibility);
			if (type.HasMethods)
				MarkMethods (context, type.Methods, rootVisibility);
			if (type.HasNestedTypes)
				foreach (var nested in type.NestedTypes)
					MarkType (context, nested, rootVisibility);

			context.Annotations.Pop ();
		}

		void ProcessExecutable (AssemblyDefinition assembly)
		{
			SetAction (Context, assembly, AssemblyAction.Link);

			Annotations.Push (assembly);

			Annotations.Mark (assembly.EntryPoint.DeclaringType);

			MarkMethod (Context, assembly.EntryPoint, MethodAction.Parse, RootVisibility.Any);

			Annotations.Pop ();
		}

		static void MarkFields (LinkContext context, Collection<FieldDefinition> fields, RootVisibility rootVisibility)
		{
			foreach (FieldDefinition field in fields) {
				bool markField;
				switch (rootVisibility) {
				default:
					markField = true;
					break;

				case RootVisibility.PublicAndFamily:
					markField = field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;
					break;

				case RootVisibility.PublicAndFamilyAndAssembly:
					markField = field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly || field.IsAssembly || field.IsFamilyAndAssembly;
					break;
				}
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
			bool markMethod;
			switch (rootVisibility) {
			default:
				markMethod = true;
				break;

			case RootVisibility.PublicAndFamily:
				markMethod = method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;
				break;

			case RootVisibility.PublicAndFamilyAndAssembly:
				markMethod = method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly || method.IsAssembly || method.IsFamilyAndAssembly;
				break;
			}

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
