// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Ecma = System.Reflection.Metadata;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.Metadata
{
    internal partial class Transform<TPolicy>
    {
        private EntityMap<ForwarderKey, TypeForwarder> _forwarders = new EntityMap<ForwarderKey, TypeForwarder>(EqualityComparer<ForwarderKey>.Default);

        private Action<ForwarderKey, TypeForwarder> _initForwarder;

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
                    HandleTypeForwarder(module, exportedTypeHandle);
                }
                else
                {
                    Debug.Fail("Multi-module assemblies");
                }
            }
        }

        private TypeForwarder HandleTypeForwarder(Cts.Ecma.EcmaModule module, Ecma.ExportedTypeHandle handle)
        {
            return _forwarders.GetOrCreate(new ForwarderKey(module, handle), _initForwarder ??= InitializeTypeForwarder);
        }

        private void InitializeTypeForwarder(ForwarderKey key, TypeForwarder record)
        {
            Cts.Ecma.EcmaModule module = key.Module;
            Ecma.MetadataReader reader = module.MetadataReader;
            Ecma.ExportedType exportedType = reader.GetExportedType(key.ExportedType);

            record.Name = HandleString(reader.GetString(exportedType.Name));

            switch (exportedType.Implementation.Kind)
            {
                case Ecma.HandleKind.AssemblyReference:
                    {
                        string ns = reader.GetString(exportedType.Namespace);
                        Cts.ModuleDesc targetModule = (Cts.ModuleDesc)module.GetObject(exportedType.Implementation);
                        record.Namespace = HandleNamespaceReference(targetModule, ns);

                        HandleScopeDefinition(module).TypeForwarders.Add(record);
                    }
                    break;

                case Ecma.HandleKind.ExportedType:
                    {
                        TypeForwarder scope = HandleTypeForwarder(module, (Ecma.ExportedTypeHandle)exportedType.Implementation);

                        record.Namespace = scope.Namespace;

                        scope.NestedTypes.Add(record);
                    }
                    break;

                default:
                    throw new BadImageFormatException();
            }
        }

#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T>
        private readonly struct ForwarderKey : IEquatable<ForwarderKey>
#pragma warning restore CA1067 // Override Object.Equals(object) when implementing IEquatable<T>
        {
            public readonly Cts.Ecma.EcmaModule Module;
            public readonly Ecma.ExportedTypeHandle ExportedType;
            public ForwarderKey(Cts.Ecma.EcmaModule module, Ecma.ExportedTypeHandle exportedType)
                => (Module, ExportedType) = (module, exportedType);

            public bool Equals(ForwarderKey other) => Module == other.Module && ExportedType == other.ExportedType;
            public override int GetHashCode() => HashCode.Combine(Module.GetHashCode(), ExportedType.GetHashCode());
        }
    }
}
