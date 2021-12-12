// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Ecma = System.Reflection.Metadata;

using Debug = System.Diagnostics.Debug;
using AssemblyName = System.Reflection.AssemblyName;
using AssemblyContentType = System.Reflection.AssemblyContentType;
using AssemblyNameFlags = System.Reflection.AssemblyNameFlags;
using AssemblyFlags = System.Reflection.AssemblyFlags;

namespace ILCompiler.Metadata
{
    partial class Transform<TPolicy>
    {
        private void HandleTypeForwarders(Cts.Ecma.EcmaModule module)
        {
            foreach (var exportedTypeHandle in module.MetadataReader.ExportedTypes)
            {
                if (!_policy.GeneratesMetadata(module, exportedTypeHandle))
                {
                    continue;
                }

                Ecma.ExportedType exportedType = module.MetadataReader.GetExportedType(exportedTypeHandle);
                if (exportedType.IsForwarder || exportedType.Implementation.Kind == Ecma.HandleKind.ExportedType)
                {
                    HandleTypeForwarder(module, exportedType);
                }
                else
                {
                    Debug.Assert(false, "Multi-module assemblies");
                }
            }
        }

        private TypeForwarder HandleTypeForwarder(Cts.Ecma.EcmaModule module, Ecma.ExportedType exportedType)
        {
            Ecma.MetadataReader reader = module.MetadataReader;
            string name = reader.GetString(exportedType.Name);
            TypeForwarder result;

            switch (exportedType.Implementation.Kind)
            {
                case Ecma.HandleKind.AssemblyReference:
                    {
                        string ns = reader.GetString(exportedType.Namespace);
                        NamespaceDefinition namespaceDefinition = HandleNamespaceDefinition(module, ns);

                        Ecma.AssemblyReference assemblyRef = reader.GetAssemblyReference((Ecma.AssemblyReferenceHandle)exportedType.Implementation);
                        AssemblyName refName = new AssemblyName
                        {
                            ContentType = (AssemblyContentType)((int)(assemblyRef.Flags & AssemblyFlags.ContentTypeMask) >> 9),
                            Flags = (AssemblyNameFlags)(assemblyRef.Flags & ~AssemblyFlags.ContentTypeMask),
                            CultureName = reader.GetString(assemblyRef.Culture),
                            Name = reader.GetString(assemblyRef.Name),
                            Version = assemblyRef.Version,
                        };

                        if ((assemblyRef.Flags & AssemblyFlags.PublicKey) != 0)
                            refName.SetPublicKey(reader.GetBlobBytes(assemblyRef.PublicKeyOrToken));
                        else
                            refName.SetPublicKeyToken(reader.GetBlobBytes(assemblyRef.PublicKeyOrToken));

                        result = new TypeForwarder
                        {
                            Name = HandleString(name),
                            Scope = HandleScopeReference(refName),
                        };

                        namespaceDefinition.TypeForwarders.Add(result);
                    }
                    break;

                case Ecma.HandleKind.ExportedType:
                    {
                        TypeForwarder scope = HandleTypeForwarder(module, reader.GetExportedType((Ecma.ExportedTypeHandle)exportedType.Implementation));

                        result = new TypeForwarder
                        {
                            Name = HandleString(name),
                            Scope = scope.Scope,
                        };

                        scope.NestedTypes.Add(result);
                    }
                    break;

                default:
                    throw new BadImageFormatException();
            }

            return result;
        }
    }
}
