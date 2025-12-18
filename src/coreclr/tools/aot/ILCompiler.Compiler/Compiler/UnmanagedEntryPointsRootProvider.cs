// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

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
                MetadataReader reader = _module.MetadataReader;
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
                        EcmaMethod method = _module.GetMethod(parent);
                        if (method.GetRuntimeExportName() != null)
                            yield return method;
                    }

                    if (comparer.Equals(nameHandle, "UnmanagedCallersOnlyAttribute")
                        && comparer.Equals(nsHandle, "System.Runtime.InteropServices"))
                    {
                        EcmaMethod method = _module.GetMethod(parent);
                        if (method.GetUnmanagedCallersOnlyExportName() != null)
                            yield return method;
                    }
                }
            }
        }

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            foreach (var ecmaMethod in ExportedMethods)
            {
                if (ecmaMethod.IsUnmanagedCallersOnly)
                {
                    string unmanagedCallersOnlyExportName = ecmaMethod.GetUnmanagedCallersOnlyExportName();
                    rootProvider.AddCompilationRoot((MethodDesc)ecmaMethod, "Native callable", unmanagedCallersOnlyExportName, Hidden);
                }
                else
                {
                    string runtimeExportName = ecmaMethod.GetRuntimeExportName();
                    rootProvider.AddCompilationRoot((MethodDesc)ecmaMethod, "Runtime export", runtimeExportName, Hidden);
                }
            }
        }
    }
}
