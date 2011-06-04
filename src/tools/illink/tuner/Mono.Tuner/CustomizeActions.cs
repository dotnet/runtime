using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Tuner {

	public class CustomizeActions : BaseStep {

		readonly bool link_sdk_only;
		readonly HashSet<string> skipped_assemblies;

		public CustomizeActions (bool link_sdk_only, IEnumerable<string> skipped_assemblies)
		{
			this.link_sdk_only = link_sdk_only;
			this.skipped_assemblies = new HashSet<string> (skipped_assemblies);
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (IsSkipped (assembly)) {
				ProcessUserAssembly (assembly);
				return;
			}

			if (!link_sdk_only) {
				if (!Annotations.HasAction (assembly)) // stray assembly not picked up when resolving references
					Annotations.SetAction (assembly, AssemblyAction.Link);

				return;
			}

			if (Profile.IsSdkAssembly (assembly) || Profile.IsProductAssembly (assembly)) {
				Annotations.SetAction (assembly, AssemblyAction.Link);
				return;
			}

			ProcessUserAssembly (assembly);
		}

		bool IsSkipped (AssemblyDefinition assembly)
		{
			return skipped_assemblies.Contains (assembly.Name.Name);
		}

		void ProcessUserAssembly (AssemblyDefinition assembly)
		{
			ResolveFromAssemblyStep.ProcessLibrary (Context, assembly);
		}
	}
}
