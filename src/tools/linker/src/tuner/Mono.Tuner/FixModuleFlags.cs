using System;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	public class FixModuleFlags : BaseStep {

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			assembly.MainModule.Attributes = ModuleAttributes.ILOnly;
		}
	}
}
