// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Ecma = System.Reflection.Metadata;

using Debug = System.Diagnostics.Debug;
using AssemblyFlags = Internal.Metadata.NativeFormat.AssemblyFlags;
using AssemblyNameFlags = System.Reflection.AssemblyNameFlags;
using AssemblyContentType = System.Reflection.AssemblyContentType;
using AssemblyName = System.Reflection.AssemblyName;

namespace ILCompiler.Metadata
{
    internal partial class Transform<TPolicy>
    {
        internal EntityMap<Cts.ModuleDesc, ScopeDefinition> _scopeDefs
            = new EntityMap<Cts.ModuleDesc, ScopeDefinition>(EqualityComparer<Cts.ModuleDesc>.Default);
        private Action<Cts.ModuleDesc, ScopeDefinition> _initScopeDef;

        private ScopeDefinition HandleScopeDefinition(Cts.ModuleDesc module)
        {
            return _scopeDefs.GetOrCreate(module, _initScopeDef ??= InitializeScopeDefinition);
        }

        private void InitializeScopeDefinition(Cts.ModuleDesc module, ScopeDefinition scopeDefinition)
        {
            var assemblyDesc = module as Cts.IAssemblyDesc;
            if (assemblyDesc != null)
            {
                var assemblyName = assemblyDesc.GetName();

                scopeDefinition.Name = HandleString(assemblyName.Name);
#if NETFX_45
                // With NET 4.5 contract System.Reflection 4.0.0.0 EcmaModule has no way
                // to set Culture in its AssemblyName.
                scopeDefinition.Culture = HandleString(assemblyName.CultureName ?? "");
#else
                scopeDefinition.Culture = HandleString(assemblyName.CultureName);
#endif
                scopeDefinition.MajorVersion = checked((ushort)assemblyName.Version.Major);
                scopeDefinition.MinorVersion = checked((ushort)assemblyName.Version.Minor);
                scopeDefinition.BuildNumber = checked((ushort)assemblyName.Version.Build);
                scopeDefinition.RevisionNumber = checked((ushort)assemblyName.Version.Revision);

                Debug.Assert((int)AssemblyFlags.PublicKey == (int)AssemblyNameFlags.PublicKey);
                Debug.Assert((int)AssemblyFlags.Retargetable == (int)AssemblyNameFlags.Retargetable);
                scopeDefinition.Flags = (AssemblyFlags)assemblyName.Flags;

                if (assemblyName.ContentType == AssemblyContentType.WindowsRuntime)
                {
                    scopeDefinition.Flags |= (AssemblyFlags)((int)AssemblyContentType.WindowsRuntime << 9);
                }

                if ((scopeDefinition.Flags & AssemblyFlags.PublicKey) != 0)
                {
                    scopeDefinition.PublicKey = assemblyName.GetPublicKey();
                }
                else
                {
                    scopeDefinition.PublicKey = assemblyName.GetPublicKeyToken();
                }

                Cts.MetadataType moduleType = module.GetGlobalModuleType();
                if (moduleType != null && _policy.GeneratesMetadata(moduleType))
                {
                    scopeDefinition.GlobalModuleType = (TypeDefinition)HandleType(moduleType);
                }

                Cts.Ecma.EcmaAssembly ecmaAssembly = module as Cts.Ecma.EcmaAssembly;
                if (ecmaAssembly != null)
                {
                    Ecma.CustomAttributeHandleCollection customAttributes = ecmaAssembly.AssemblyDefinition.GetCustomAttributes();
                    if (customAttributes.Count > 0)
                    {
                        scopeDefinition.CustomAttributes = HandleCustomAttributes(ecmaAssembly, customAttributes);
                    }

                    Cts.MethodDesc entryPoint = ecmaAssembly.EntryPoint;
                    if (entryPoint != null && _policy.GeneratesMetadata(entryPoint))
                    {
                        scopeDefinition.EntryPoint = (QualifiedMethod)HandleQualifiedMethod(entryPoint);
                    }

                    Ecma.MetadataReader reader = ecmaAssembly.MetadataReader;
                    Ecma.ModuleDefinition moduleDefinition = reader.GetModuleDefinition();
                    scopeDefinition.ModuleName = HandleString(reader.GetString(moduleDefinition.Name));
                    scopeDefinition.Mvid = reader.GetGuid(moduleDefinition.Mvid).ToByteArray();

                    // This is rather awkward because ModuleDefinition doesn't offer means to get to the custom attributes
                    Ecma.CustomAttributeHandleCollection moduleAttributes = reader.GetCustomAttributes(Ecma.Ecma335.MetadataTokens.EntityHandle(0x1));
                    if (moduleAttributes.Count > 0)
                    {
                        scopeDefinition.ModuleCustomAttributes = HandleCustomAttributes(ecmaAssembly, moduleAttributes);
                    }

                    HandleTypeForwarders(ecmaAssembly);
                }
            }
            else
            {
                throw new NotSupportedException("Multi-module assemblies");
            }
        }

        private EntityMap<AssemblyName, ScopeReference> _scopeRefs
            = new EntityMap<AssemblyName, ScopeReference>(new SimpleAssemblyNameComparer());
        private Action<AssemblyName, ScopeReference> _initScopeRef;

        private ScopeReference HandleScopeReference(Cts.ModuleDesc module)
        {
            var assembly = module as Cts.IAssemblyDesc;
            if (assembly != null)
                return HandleScopeReference(assembly.GetName());
            else
                throw new NotSupportedException("Multi-module assemblies");
        }

        private ScopeReference HandleScopeReference(AssemblyName assemblyName)
        {
            return _scopeRefs.GetOrCreate(assemblyName, _initScopeRef ??= InitializeScopeReference);
        }

        private void InitializeScopeReference(AssemblyName assemblyName, ScopeReference scopeReference)
        {
            scopeReference.Name = HandleString(assemblyName.Name);
            scopeReference.Culture = HandleString(assemblyName.CultureName);
            scopeReference.MajorVersion = checked((ushort)assemblyName.Version.Major);
            scopeReference.MinorVersion = checked((ushort)assemblyName.Version.Minor);
            scopeReference.BuildNumber = checked((ushort)assemblyName.Version.Build);
            scopeReference.RevisionNumber = checked((ushort)assemblyName.Version.Revision);

            Debug.Assert((int)AssemblyFlags.PublicKey == (int)AssemblyNameFlags.PublicKey);
            Debug.Assert((int)AssemblyFlags.Retargetable == (int)AssemblyNameFlags.Retargetable);

            // References use a public key token instead of full public key.
            scopeReference.Flags = (AssemblyFlags)(assemblyName.Flags & ~AssemblyNameFlags.PublicKey);

            if (assemblyName.ContentType == AssemblyContentType.WindowsRuntime)
            {
                scopeReference.Flags |= (AssemblyFlags)((int)AssemblyContentType.WindowsRuntime << 9);
            }

            scopeReference.PublicKeyOrToken = assemblyName.GetPublicKeyToken();
        }

        private sealed class SimpleAssemblyNameComparer : IEqualityComparer<AssemblyName>
        {
            public bool Equals(AssemblyName x, AssemblyName y)
            {
                return Equals(x.Name, y.Name);
            }

            public int GetHashCode(AssemblyName obj)
            {
                return obj.Name?.GetHashCode() ?? 0;
            }
        }
    }
}
