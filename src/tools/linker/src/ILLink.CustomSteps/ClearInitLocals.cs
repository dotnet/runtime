using System;
using System.Collections.Generic;

using Mono.Linker;
using Mono.Linker.Steps;
using Mono.Cecil;

namespace ILLink.CustomSteps
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

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if ((_assemblies != null) && (!_assemblies.Contains (assembly.Name.Name))) {
				return;
			}

			bool changed = false;

			foreach (ModuleDefinition module in assembly.Modules) {
				foreach (TypeDefinition type in module.Types) {
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
