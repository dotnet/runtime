// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;
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

        public IEnumerable<EcmaMethod> ExportedMethods
        {
            get
            {
                return UnmanagedEntryPointsNode.GetExportedMethods(_module);
            }
        }

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            rootProvider.AddCompilationRoot(new UnmanagedEntryPointsNode(_module, Hidden), "Unmanaged entry points");
        }

        private sealed class UnmanagedEntryPointsNode : DependencyNodeCore<NodeFactory>
        {
            private readonly EcmaModule _module;
            private readonly bool _isHidden;

            public UnmanagedEntryPointsNode(EcmaModule module, bool isHidden)
            {
                _module = module;
                _isHidden = isHidden;
            }

            public override bool InterestingForDynamicDependencyAnalysis => false;

            public override bool HasDynamicDependencies => false;

            public override bool HasConditionalStaticDependencies => true;

            public override bool StaticDependenciesAreComputed => true;

            public static IEnumerable<EcmaMethod> GetExportedMethods(EcmaModule module)
            {
                MetadataReader reader = module.MetadataReader;
                MetadataStringComparer comparer = reader.StringComparer;
                foreach (CustomAttributeHandle caHandle in reader.CustomAttributes)
                {
                    CustomAttribute ca = reader.GetCustomAttribute(caHandle);
                    if (ca.Parent.Kind != HandleKind.MethodDefinition)
                        continue;

                    var parent = (MethodDefinitionHandle)ca.Parent;

                    if (!reader.GetAttributeNamespaceAndName(caHandle, out StringHandle nsHandle, out StringHandle nameHandle))
                        continue;

                    if (comparer.Equals(nameHandle, "RuntimeExportAttribute")
                        && comparer.Equals(nsHandle, "System.Runtime"))
                    {
                        EcmaMethod method = module.GetMethod(parent);
                        if (method.GetRuntimeExportName() != null)
                            yield return method;
                    }

                    if (comparer.Equals(nameHandle, "UnmanagedCallersOnlyAttribute")
                        && comparer.Equals(nsHandle, "System.Runtime.InteropServices"))
                    {
                        EcmaMethod method = module.GetMethod(parent);
                        bool hasExportName = false;
                        try
                        {
                            hasExportName = method.GetUnmanagedCallersOnlyExportName() != null;
                        }
                        catch (TypeSystemException)
                        {
                            // Keep export discovery consistent with the later associated-source handling:
                            // if decoding UnmanagedCallersOnlyAttribute fails because a type-valued
                            // named argument cannot be resolved, skip the export instead of failing
                            // the compilation during discovery.
                        }

                        if (hasExportName)
                            yield return method;
                    }
                }
            }

            public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
            {
                foreach (EcmaMethod method in GetExportedMethods(_module))
                {
                    if (!method.IsUnmanagedCallersOnly)
                    {
                        foreach (DependencyListEntry dependency in GetMethodStaticDependencies(context, method, "Runtime export", new Utf8String(method.GetRuntimeExportName())))
                            yield return dependency;

                        continue;
                    }

                    if (!TryGetAssociatedSourceType(method, out TypeDesc associatedSourceType) || associatedSourceType is not null)
                        continue;

                    foreach (DependencyListEntry dependency in GetMethodStaticDependencies(context, method, "Native callable", new Utf8String(method.GetUnmanagedCallersOnlyExportName())))
                        yield return dependency;
                }
            }

            public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context)
            {
                List<CombinedDependencyListEntry> dependencies = [];

                foreach (EcmaMethod method in GetExportedMethods(_module))
                {
                    if (!TryGetAssociatedSourceType(method, out TypeDesc associatedSourceType) || associatedSourceType is null)
                        continue;

                    IEETypeNode effectiveTrimTargetType = RuntimeConstructableTypeDependencies.GetEffectiveTrimTargetType(context, associatedSourceType, conditionConstructed: true);

                    IMethodNode methodEntryPoint = GetMethodEntrypointAndAddAlias(context, method, new Utf8String(method.GetUnmanagedCallersOnlyExportName()));

                    dependencies.Add(new CombinedDependencyListEntry(
                        methodEntryPoint,
                        effectiveTrimTargetType,
                        "Native callable with associated source type"));

                    RuntimeConstructableTypeDependencies.AddTypeLoaderDependencies(dependencies, context, effectiveTrimTargetType, "Associated source type that could be loaded at runtime");
                }

                return dependencies;
            }

            public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();

            private IEnumerable<DependencyListEntry> GetMethodStaticDependencies(NodeFactory context, EcmaMethod method, string reason, Utf8String exportName)
            {
                IMethodNode methodEntryPoint = GetMethodEntrypointAndAddAlias(context, method, exportName);

                yield return new DependencyListEntry(methodEntryPoint, reason);
            }

            private IMethodNode GetMethodEntrypointAndAddAlias(NodeFactory context, EcmaMethod method, Utf8String exportName)
            {
                IMethodNode methodEntryPoint = context.MethodEntrypoint(method);

                if (!exportName.IsNull)
                {
                    exportName = context.NameMangler.NodeMangler.ExternMethod(exportName, method);
                    context.NodeAliases[methodEntryPoint] = (exportName, _isHidden);
                }

                return methodEntryPoint;
            }

            private static bool TryGetAssociatedSourceType(EcmaMethod method, out TypeDesc associatedSourceType)
            {
                try
                {
                    associatedSourceType = method.GetUnmanagedCallersOnlyAssociatedSourceType();
                    return true;
                }
                catch (TypeSystemException)
                {
                    associatedSourceType = null;
                    return false;
                }
            }

            protected override string GetName(NodeFactory context) => $"Unmanaged entry points: {_module}";
        }
    }
}
