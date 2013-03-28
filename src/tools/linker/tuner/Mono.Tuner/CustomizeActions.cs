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
			if (!IsSkipped (assembly) && IsLinked (assembly)) {
				if (!Annotations.HasAction (assembly)) // stray assembly not picked up when resolving references
					Annotations.SetAction (assembly, AssemblyAction.Link);
				return;
			}
			ProcessUserAssembly (assembly);
		}

		protected virtual bool IsPreservedAttribute (CustomAttribute attribute)
		{
			return (attribute.AttributeType.Name == "PreserveAttribute");
		}

		protected virtual bool IsLinkerSafeAttribute (CustomAttribute attribute)
		{
			return (attribute.AttributeType.Name == "LinkerSafeAttribute");
		}

		const ModuleAttributes Supported = ModuleAttributes.ILOnly | ModuleAttributes.Required32Bit | 
			ModuleAttributes.Preferred32Bit | ModuleAttributes.StrongNameSigned;

		protected virtual bool IsSkipped (AssemblyDefinition assembly)
		{
			// Cecil can't save back mixed-mode assemblies - so we can't link them
			if ((assembly.MainModule.Attributes & ~Supported) != 0)
				return true;

			if (assembly.HasCustomAttributes) {
				foreach (var ca in assembly.CustomAttributes) {
					if (IsPreservedAttribute (ca))
						return true;
				}
			}
			return skipped_assemblies.Contains (assembly.Name.Name);
		}

		protected virtual bool IsLinked (AssemblyDefinition assembly)
		{
			// LinkAll
			if (!link_sdk_only)
				return true;
			// Link SDK : applies to BCL/SDK and product assembly (e.g. monotouch.dll)
			if (Profile.IsSdkAssembly (assembly))
				return true;
			if (Profile.IsProductAssembly (assembly))
			    return true;
			// the assembly can be marked with [LinkAssembly]
			if (assembly.HasCustomAttributes) {
				foreach (var ca in assembly.CustomAttributes) {
					if (IsLinkerSafeAttribute (ca))
						return true;
				}
			}
			return false;
		}

		protected void ProcessUserAssembly (AssemblyDefinition assembly)
		{
			ResolveFromAssemblyStep.ProcessLibrary (Context, assembly);
		}
	}
}
