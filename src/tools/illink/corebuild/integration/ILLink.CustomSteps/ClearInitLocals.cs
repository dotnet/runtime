using System;

using Mono.Linker;
using Mono.Linker.Steps;
using Mono.Cecil;

namespace ILLink.CustomSteps
{
	public class ClearInitLocalsStep : BaseStep
	{
		protected override void ProcessAssembly(AssemblyDefinition assembly)
		{
			foreach (ModuleDefinition module in assembly.Modules) {
				foreach (TypeDefinition type in module.Types) {
					foreach (MethodDefinition method in type.Methods) {
						if (method.Body != null) {
							method.Body.InitLocals = false;
						}
					}
				}
			}
		}
	}
}
