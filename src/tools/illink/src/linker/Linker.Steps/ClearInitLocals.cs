using System;
using System.Collections.Generic;

using Mono.Linker;
using Mono.Linker.Steps;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class ClearInitLocalsStep : BaseStep
	{
		HashSet<string> _assemblies;

		protected override void Process ()
		{
			string parameterName = "ClearInitLocalsAssemblies";

			if (Context.HasParameter (parameterName)) {
				string parameter = Context.GetParameter (parameterName);
				_assemblies = new HashSet<string> (parameter.Split(','), StringComparer.OrdinalIgnoreCase);
			}
		}

		private static IEnumerable<TypeDefinition> EnumerateTypesAndNestedTypes (IEnumerable<TypeDefinition> types)
		{
			// Recursively (depth-first) yield each element in 'types' and all nested types under each element.

			foreach (TypeDefinition type in types) {
				yield return type;

				foreach (TypeDefinition nestedType in EnumerateTypesAndNestedTypes (type.NestedTypes)) {
					yield return nestedType;
				}
			}
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if ((_assemblies != null) && (!_assemblies.Contains (assembly.Name.Name))) {
				return;
			}

			bool changed = false;

			foreach (ModuleDefinition module in assembly.Modules) {
				foreach (TypeDefinition type in EnumerateTypesAndNestedTypes (module.Types)) {
					foreach (MethodDefinition method in type.Methods) {
						if (method.Body != null) {
							if (method.Body.InitLocals) {
								method.Body.InitLocals = false;
								changed = true;
							}
						}
					}
				}
			}

			if (changed && (Annotations.GetAction (assembly) == AssemblyAction.Copy))
					Annotations.SetAction (assembly, AssemblyAction.Save);
		}
	}
}
