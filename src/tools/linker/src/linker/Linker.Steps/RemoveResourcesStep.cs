﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
    public class RemoveResourcesStep : BaseStep
    {
        protected override void ProcessAssembly(AssemblyDefinition assembly)
        {
            if (!ShouldProcess(assembly)) return;

            RemoveFSharpCompilationResources(assembly);
        }

        private bool ShouldProcess(AssemblyDefinition assembly)
        {
            if (!assembly.MainModule.HasResources)
                return false;

            var action = Annotations.GetAction(assembly);
            return action == AssemblyAction.Link || action == AssemblyAction.Save;
        }

        private void RemoveFSharpCompilationResources(AssemblyDefinition assembly)
        {
            var resourcesInAssembly = assembly.MainModule.Resources.OfType<EmbeddedResource>();
            foreach (var resource in resourcesInAssembly.Where(IsFSharpCompilationResource))
            {
                Annotations.AddResourceToRemove(assembly, resource);
            }

            static bool IsFSharpCompilationResource(Resource resource)
                => resource.Name.StartsWith("FSharpSignatureData", StringComparison.Ordinal)
                || resource.Name.StartsWith("FSharpOptimizationData", StringComparison.Ordinal);
        }
    }
}
