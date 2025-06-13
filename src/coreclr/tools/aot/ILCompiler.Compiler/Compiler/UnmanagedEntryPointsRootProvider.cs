// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    /// <summary>
    /// Computes a set of roots based on managed and unmanaged methods exported from a module.
    /// </summary>
    public class UnmanagedEntryPointsRootProvider : ICompilationRootProvider
    {
        private EcmaModule _module;

        public UnmanagedEntryPointsRootProvider(EcmaModule module, bool hidden = false)
        {
            _module = module;
            Hidden = hidden;
        }

        public bool Hidden { get; }

        public struct ExportedMethodInfo
        {
            public EcmaMethod Method { get; init; }
            public SymbolFlags Flags { get; init; }

            public void Deconstruct(out EcmaMethod method, out SymbolFlags flags)
            {
                method = Method;
                flags = Flags;
            }
        }

        public IEnumerable<ExportedMethodInfo> ExportedMethods
        {
            get
            {
                MetadataReader reader = _module.MetadataReader;
                MetadataStringComparer comparer = reader.StringComparer;
                foreach (CustomAttributeHandle caHandle in reader.CustomAttributes)
                {
                    CustomAttribute ca = reader.GetCustomAttribute(caHandle);
                    if (ca.Parent.Kind != HandleKind.MethodDefinition)
                        continue;

                    if (!reader.GetAttributeNamespaceAndName(caHandle, out StringHandle nsHandle, out StringHandle nameHandle))
                        continue;

                    if (comparer.Equals(nameHandle, "RuntimeExportAttribute")
                        && comparer.Equals(nsHandle, "System.Runtime"))
                    {
                        var method = (EcmaMethod)_module.GetMethod(ca.Parent);

                        RuntimeExportInfo exportInfo = method.GetRuntimeExportInfo();

                        if (exportInfo.Name != null)
                        {
                            SymbolFlags flags = Hidden ? SymbolFlags.Hidden : SymbolFlags.None;
                            if (exportInfo.Weak)
                                flags |= SymbolFlags.Weak;
                            yield return new ExportedMethodInfo { Method = method, Flags = flags };
                        }
                    }

                    if (comparer.Equals(nameHandle, "UnmanagedCallersOnlyAttribute")
                        && comparer.Equals(nsHandle, "System.Runtime.InteropServices"))
                    {
                        var method = (EcmaMethod)_module.GetMethod(ca.Parent);
                        if (method.GetUnmanagedCallersOnlyExportName() != null)
                            yield return new ExportedMethodInfo { Method = method, Flags = Hidden ? SymbolFlags.Hidden : SymbolFlags.None };
                    }
                }
            }
        }

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            foreach (var (ecmaMethod, exportedFlags) in ExportedMethods)
            {
                if (ecmaMethod.GetUnmanagedCallersOnlyExportName() != null)
                {
                    string unmanagedCallersOnlyExportName = ecmaMethod.GetUnmanagedCallersOnlyExportName();
                    rootProvider.AddCompilationRoot(ecmaMethod, "Native callable", unmanagedCallersOnlyExportName, exportedFlags);
                }
                else
                {
                    RuntimeExportInfo runtimeExportInfo = ecmaMethod.GetRuntimeExportInfo();

                    if (runtimeExportInfo.ConditionalConstructedDependency is null)
                    {
                        rootProvider.AddCompilationRoot(ecmaMethod, "Runtime export", runtimeExportInfo.Name, exportedFlags);
                    }
                    else
                    {
                        rootProvider.AddCompilationRoot(new ConditionalRuntimeExportNode(ecmaMethod, runtimeExportInfo.Name, exportedFlags, runtimeExportInfo.ConditionalConstructedDependency), "Runtime export");
                    }
                }
            }
        }

        private sealed class ConditionalRuntimeExportNode(EcmaMethod method, string exportName, SymbolFlags flags, TypeDesc dependency) : DependencyNodeCore<NodeFactory>
        {
            public override bool StaticDependenciesAreComputed => true;
            public override bool HasConditionalStaticDependencies => true;

            public override bool InterestingForDynamicDependencyAnalysis => false;

            public override bool HasDynamicDependencies => true;

            public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context)
            {
                List<CombinedDependencyListEntry> dependencies = [];
                MethodDesc canonMethod = method.GetCanonMethodTarget(CanonicalFormKind.Specific);
                IMethodNode methodEntryPoint = context.MethodEntrypoint(canonMethod);

                dependencies.Add(new CombinedDependencyListEntry(methodEntryPoint, context.MaximallyConstructableType(dependency), "Conditional type constructed"));

                if (exportName != null)
                {
                    exportName = context.NameMangler.NodeMangler.ExternMethod(exportName, method);
                    context.NodeAliases.Add(methodEntryPoint, (exportName, flags));
                }

                if (canonMethod != method && method.HasInstantiation)
                {
                    dependencies.Add(new CombinedDependencyListEntry(context.MethodGenericDictionary(method), context.MaximallyConstructableType(dependency), "Conditional type constructed"));
                }

                return dependencies;
            }

            public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => [];
            public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => [];
            protected override string GetName(NodeFactory context) => $"Conditional runtime export on usage of {dependency}";
        }
    }
}
