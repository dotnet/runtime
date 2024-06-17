// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Ecma = System.Reflection.Metadata;

using Debug = System.Diagnostics.Debug;
using AssemblyFlags = Internal.Metadata.NativeFormat.AssemblyFlags;
using AssemblyNameInfo = System.Reflection.Metadata.AssemblyNameInfo;

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
                scopeDefinition.Culture = HandleString(assemblyName.CultureName);
                scopeDefinition.MajorVersion = checked((ushort)assemblyName.Version.Major);
                scopeDefinition.MinorVersion = checked((ushort)assemblyName.Version.Minor);
                scopeDefinition.BuildNumber = checked((ushort)assemblyName.Version.Build);
                scopeDefinition.RevisionNumber = checked((ushort)assemblyName.Version.Revision);
                scopeDefinition.Flags = (AssemblyFlags)assemblyName.Flags;
                scopeDefinition.PublicKey = assemblyName.PublicKeyOrToken.ToArray();

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

        private EntityMap<AssemblyNameInfo, ScopeReference> _scopeRefs
            = new EntityMap<AssemblyNameInfo, ScopeReference>(new SimpleAssemblyNameComparer());
        private Action<AssemblyNameInfo, ScopeReference> _initScopeRef;

        private ScopeReference HandleScopeReference(Cts.ModuleDesc module)
        {
            var assembly = module as Cts.IAssemblyDesc;
            if (assembly != null)
                return HandleScopeReference(assembly.GetName());
            else
                throw new NotSupportedException("Multi-module assemblies");
        }

        private ScopeReference HandleScopeReference(AssemblyNameInfo assemblyName)
        {
            return _scopeRefs.GetOrCreate(assemblyName, _initScopeRef ??= InitializeScopeReference);
        }

        private void InitializeScopeReference(AssemblyNameInfo assemblyName, ScopeReference scopeReference)
        {
            scopeReference.Name = HandleString(assemblyName.Name);
            scopeReference.Culture = HandleString(assemblyName.CultureName);
            scopeReference.MajorVersion = checked((ushort)assemblyName.Version.Major);
            scopeReference.MinorVersion = checked((ushort)assemblyName.Version.Minor);
            scopeReference.BuildNumber = checked((ushort)assemblyName.Version.Build);
            scopeReference.RevisionNumber = checked((ushort)assemblyName.Version.Revision);
            scopeReference.Flags = (AssemblyFlags)assemblyName.Flags & (AssemblyFlags.Retargetable | AssemblyFlags.ContentTypeMask);

            // References use a public key token instead of full public key.
            ImmutableArray<byte> publicKeyOrToken = assemblyName.PublicKeyOrToken;
            if ((assemblyName.Flags & AssemblyNameFlags.PublicKey) != 0)
            {
                // Use AssemblyName to convert PublicKey to PublicKeyToken to avoid calling crypto APIs directly
                AssemblyName an = new();
                an.SetPublicKey(ImmutableCollectionsMarshal.AsArray<byte>(publicKeyOrToken));
                publicKeyOrToken = ImmutableCollectionsMarshal.AsImmutableArray<byte>(an.GetPublicKeyToken());
            }
            scopeReference.PublicKeyOrToken = publicKeyOrToken.ToArray();
        }

        private sealed class SimpleAssemblyNameComparer : IEqualityComparer<AssemblyNameInfo>
        {
            public bool Equals(AssemblyNameInfo x, AssemblyNameInfo y)
            {
                return Equals(x.Name, y.Name);
            }

            public int GetHashCode(AssemblyNameInfo obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}
