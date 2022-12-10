// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class RemoveResourcesStep : BaseStep
	{
		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (!ShouldProcess (assembly)) return;

			RemoveFSharpCompilationResources (assembly);
		}

		private bool ShouldProcess (AssemblyDefinition assembly)
		{
			if (!assembly.MainModule.HasResources)
				return false;

			var action = Annotations.GetAction (assembly);
			return action == AssemblyAction.Link || action == AssemblyAction.Save;
		}

		private void RemoveFSharpCompilationResources (AssemblyDefinition assembly)
		{
			var resourcesInAssembly = assembly.MainModule.Resources.OfType<EmbeddedResource> ();
			foreach (var resource in resourcesInAssembly.Where (IsFSharpCompilationResource)) {
				Annotations.AddResourceToRemove (assembly, resource);
			}

			static bool IsFSharpCompilationResource (Resource resource)
				=> resource.Name.StartsWith ("FSharpSignatureData", StringComparison.Ordinal)
				|| resource.Name.StartsWith ("FSharpOptimizationData", StringComparison.Ordinal);
		}
	}
}
