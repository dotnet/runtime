using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class ClearInitLocalsStep : BaseStep
	{
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
			if (!Context.IsOptimizationEnabled (CodeOptimizations.ClearInitLocals, assembly))
				return;

			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			foreach (ModuleDefinition module in assembly.Modules) {
				foreach (TypeDefinition type in EnumerateTypesAndNestedTypes (module.Types)) {
					foreach (MethodDefinition method in type.Methods) {
						if (method.Body != null) {
							if (method.Body.InitLocals) {
								method.Body.InitLocals = false;
							}
						}
					}
				}
			}
		}
	}
}
