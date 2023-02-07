// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using TaskItem = Microsoft.Build.Utilities.TaskItem;

namespace Microsoft.WebAssembly.Build.Tasks;

/// <summary>
/// Starting from the entrypoint assembly, walks the graph of referenced assemblies using candidates from the
/// runtime pack (first priority) or application assembly list (second priority). This is a way of reducing the
/// number of bundled assemblies to the minimal set, instead of including every possible assembly from the runtime
/// pack and all framework references.
/// </summary>
public class WasmResolveAssemblyDependencies : Microsoft.Build.Utilities.Task
{
    [Required]
    public string EntryPoint { get; set; } = default!;

    [Required]
    public ITaskItem[] ApplicationAssemblies { get; set; } = default!;

    [Required]
    public ITaskItem[] WasiRuntimePackAssemblies { get; set; } = default!;

    [Output]
    public ITaskItem[]? Dependencies { get; set; }

    public override bool Execute()
    {
        var paths = ResolveRuntimeDependenciesCore(EntryPoint, ApplicationAssemblies, WasiRuntimePackAssemblies);
        Dependencies = paths.Select(p => new TaskItem(p.Path)).ToArray();

        return true;
    }

    private static List<AssemblyEntry> ResolveRuntimeDependenciesCore(
        string entryPointPath,
        IEnumerable<ITaskItem> applicationAssemblies,
        IEnumerable<ITaskItem> runtimePackAssemblies)
    {
        var entryAssembly = new AssemblyEntry(entryPointPath, GetAssemblyName(entryPointPath), originalTaskItem: null);
        var applicationAssemblyEntries = CreateAssemblyLookup(applicationAssemblies);
        var runtimePackAssemblyEntries = CreateAssemblyLookup(runtimePackAssemblies);

        var assemblyResolutionContext = new AssemblyResolutionContext(
            entryAssembly,
            applicationAssemblyEntries,
            runtimePackAssemblyEntries);
        assemblyResolutionContext.ResolveAssemblies();

        return assemblyResolutionContext.Results;
    }

    private static Dictionary<string, AssemblyEntry> CreateAssemblyLookup(IEnumerable<ITaskItem> assemblies)
    {
        var dictionary = new Dictionary<string, AssemblyEntry>(StringComparer.Ordinal);
        foreach (var assembly in assemblies)
        {
            var assemblyName = GetAssemblyName(assembly.ItemSpec);
            if (dictionary.TryGetValue(assemblyName, out var previous))
            {
                throw new InvalidOperationException($"Multiple assemblies found with the same assembly name '{assemblyName}':" +
                    Environment.NewLine + string.Join(Environment.NewLine, previous, assembly.ItemSpec));
            }
            dictionary[assemblyName] = new AssemblyEntry(assembly.ItemSpec, assemblyName, assembly);
        }

        return dictionary;
    }

    private static string GetAssemblyName(string assemblyPath)
    {
        // It would be more correct to return AssemblyName.GetAssemblyName(assemblyPath).Name, but that involves
        // actually loading the assembly file and maybe hitting a BadImageFormatException if it's not actually
        // something that can be loaded by the active .NET version (e.g., .NET Framework if this task is running
        // inside VS).
        // Instead we'll rely on the filename matching the assembly name.
        return Path.GetFileNameWithoutExtension(assemblyPath);
    }

    private sealed class AssemblyResolutionContext
    {
        public AssemblyResolutionContext(
            AssemblyEntry entryAssembly,
            Dictionary<string, AssemblyEntry> applicationAssemblies,
            Dictionary<string, AssemblyEntry> runtimePackAssemblies)
        {
            EntryAssembly = entryAssembly;
            ApplicationAssemblies = applicationAssemblies;
            RuntimePackAssemblies = runtimePackAssemblies;
        }

        public AssemblyEntry EntryAssembly { get; }
        public Dictionary<string, AssemblyEntry> ApplicationAssemblies { get; }
        public Dictionary<string, AssemblyEntry> RuntimePackAssemblies { get; }

        public List<AssemblyEntry> Results { get; } = new();

        public void ResolveAssemblies()
        {
            var visitedAssemblies = new HashSet<string>();
            var pendingAssemblies = new Stack<string>();
            pendingAssemblies.Push(EntryAssembly.Name);
            ResolveAssembliesCore();

            void ResolveAssembliesCore()
            {
                while (pendingAssemblies.Count > 0)
                {
                    var current = pendingAssemblies.Pop();
                    if (visitedAssemblies.Add(current))
                    {
                        // Not all references will be resolvable within the runtime pack.
                        // Skipping unresolved assemblies here is equivalent to passing "--skip-unresolved true" to the .NET linker.
                        if (Resolve(current) is AssemblyEntry resolved)
                        {
                            Results.Add(resolved);
                            var references = GetAssemblyReferences(resolved.Path);
                            foreach (var reference in references)
                            {
                                pendingAssemblies.Push(reference);
                            }
                        }
                    }
                }
            }

            AssemblyEntry? Resolve(string assemblyName)
            {
                if (string.Equals(assemblyName, EntryAssembly.Name, StringComparison.Ordinal))
                {
                    return EntryAssembly;
                }

                // Resolution logic. For right now, we will prefer the runtime pack version of a given
                // assembly if there is a candidate assembly and an equivalent runtime pack assembly.
                if (RuntimePackAssemblies.TryGetValue(assemblyName, out var assembly)
                    || ApplicationAssemblies.TryGetValue(assemblyName, out assembly))
                {
                    return assembly;
                }

                return null;
            }

            static IReadOnlyList<string> GetAssemblyReferences(string assemblyPath)
            {
                try
                {
                    using var peReader = new PEReader(File.OpenRead(assemblyPath));
                    if (!peReader.HasMetadata)
                    {
                        return Array.Empty<string>(); // not a managed assembly
                    }

                    var metadataReader = peReader.GetMetadataReader();

                    var references = new List<string>();
                    foreach (var handle in metadataReader.AssemblyReferences)
                    {
                        var reference = metadataReader.GetAssemblyReference(handle);
                        var referenceName = metadataReader.GetString(reference.Name);

                        references.Add(referenceName);
                    }

                    return references;
                }
                catch (BadImageFormatException)
                {
                    // not a PE file, or invalid metadata
                }

                return Array.Empty<string>(); // not a managed assembly
            }
        }
    }

    internal readonly struct AssemblyEntry
    {
        public AssemblyEntry(string path, string name, ITaskItem? originalTaskItem)
        {
            Path = path;
            Name = name;
            _originalTaskItem = originalTaskItem;
        }

        private readonly ITaskItem? _originalTaskItem;
        public string Path { get; }
        public string Name { get; }
    }
}
