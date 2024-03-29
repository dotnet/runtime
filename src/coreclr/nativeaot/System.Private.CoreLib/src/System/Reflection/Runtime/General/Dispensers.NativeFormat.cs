// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Runtime.Assemblies;
using System.Reflection.Runtime.Assemblies.NativeFormat;
using System.Reflection.Runtime.CustomAttributes.NativeFormat;
using System.Reflection.Runtime.Dispensers;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.PropertyInfos;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.TypeInfos.NativeFormat;

using Internal.Metadata.NativeFormat;
using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;

//=================================================================================================================
// This file collects the various chokepoints that create the various Runtime*Info objects. This allows
// easy reviewing of the overall caching and unification policy.
//
// The dispenser functions are defined as static members of the associated Info class. This permits us
// to keep the constructors private to ensure that these really are the only ways to obtain these objects.
//=================================================================================================================

namespace System.Reflection.Runtime.Assemblies
{
    //-----------------------------------------------------------------------------------------------------------
    // Assemblies (maps 1-1 with a MetadataReader/ScopeDefinitionHandle.
    //-----------------------------------------------------------------------------------------------------------
    internal partial class RuntimeAssemblyInfo
    {
        static partial void GetNativeFormatRuntimeAssembly(AssemblyBindResult bindResult, ref RuntimeAssembly? runtimeAssembly)
        {
            if (bindResult.Reader != null)
                runtimeAssembly = NativeFormatRuntimeAssembly.GetRuntimeAssembly(bindResult.Reader, bindResult.ScopeDefinitionHandle, bindResult.OverflowScopes);
        }
    }
}

namespace System.Reflection.Runtime.Assemblies.NativeFormat
{
    internal sealed partial class NativeFormatRuntimeAssembly
    {
        internal static RuntimeAssembly GetRuntimeAssembly(MetadataReader reader, ScopeDefinitionHandle scope, IEnumerable<QScopeDefinition> overflowScopes)
        {
            return s_scopeToAssemblyDispenser.GetOrAdd(new RuntimeAssemblyKey(reader, scope, overflowScopes));
        }

        private static readonly Dispenser<RuntimeAssemblyKey, RuntimeAssembly> s_scopeToAssemblyDispenser =
            DispenserFactory.CreateDispenserV<RuntimeAssemblyKey, RuntimeAssembly>(
                DispenserScenario.Scope_Assembly,
                delegate (RuntimeAssemblyKey qScopeDefinition)
                {
                    return (RuntimeAssembly)new NativeFormat.NativeFormatRuntimeAssembly(qScopeDefinition.Reader, qScopeDefinition.Handle, qScopeDefinition.Overflows);
                }
        );

        //-----------------------------------------------------------------------------------------------------------
        // Captures a qualified scope (a reader plus a handle) representing the canonical definition of an assembly,
        // plus a set of "overflow" scopes representing additional pieces of the assembly.
        //-----------------------------------------------------------------------------------------------------------
        private struct RuntimeAssemblyKey : IEquatable<RuntimeAssemblyKey>
        {
            public RuntimeAssemblyKey(MetadataReader reader, ScopeDefinitionHandle handle, IEnumerable<QScopeDefinition> overflows)
            {
                _reader = reader;
                _handle = handle;
                _overflows = overflows;
            }

            public MetadataReader Reader { get { return _reader; } }
            public ScopeDefinitionHandle Handle { get { return _handle; } }
            public IEnumerable<QScopeDefinition> Overflows { get { return _overflows; } }

            public override bool Equals(object obj)
            {
                if (!(obj is RuntimeAssemblyKey other))
                    return false;
                return Equals(other);
            }


            public bool Equals(RuntimeAssemblyKey other)
            {
                // Equality depends only on the canonical definition of an assembly, not
                // the overflows.
                if (!(_reader == other._reader))
                    return false;
                if (!(_handle.Equals(other._handle)))
                    return false;
                return true;
            }

            public override int GetHashCode()
            {
                return _handle.GetHashCode();
            }

            private readonly MetadataReader _reader;
            private readonly ScopeDefinitionHandle _handle;
            private readonly IEnumerable<QScopeDefinition> _overflows;
        }
    }
}

namespace System.Reflection.Runtime.FieldInfos.NativeFormat
{
    //-----------------------------------------------------------------------------------------------------------
    // FieldInfos
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class NativeFormatRuntimeFieldInfo
    {
        internal static RuntimeFieldInfo GetRuntimeFieldInfo(FieldHandle fieldHandle, NativeFormatRuntimeNamedTypeInfo definingTypeInfo, RuntimeTypeInfo contextTypeInfo, RuntimeTypeInfo reflectedType)
        {
            return new NativeFormatRuntimeFieldInfo(fieldHandle, definingTypeInfo, contextTypeInfo, reflectedType).WithDebugName();
        }
    }
}

namespace System.Reflection.Runtime.PropertyInfos.NativeFormat
{
    //-----------------------------------------------------------------------------------------------------------
    // PropertyInfos
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class NativeFormatRuntimePropertyInfo
    {
        internal static RuntimePropertyInfo GetRuntimePropertyInfo(PropertyHandle propertyHandle, NativeFormatRuntimeNamedTypeInfo definingTypeInfo, RuntimeTypeInfo contextTypeInfo, RuntimeTypeInfo reflectedType)
        {
            return new NativeFormatRuntimePropertyInfo(propertyHandle, definingTypeInfo, contextTypeInfo, reflectedType).WithDebugName();
        }
    }
}

namespace System.Reflection.Runtime.EventInfos.NativeFormat
{
    //-----------------------------------------------------------------------------------------------------------
    // EventInfos
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class NativeFormatRuntimeEventInfo
    {
        internal static RuntimeEventInfo GetRuntimeEventInfo(EventHandle eventHandle, NativeFormatRuntimeNamedTypeInfo definingTypeInfo, RuntimeTypeInfo contextTypeInfo, RuntimeTypeInfo reflectedType)
        {
            return new NativeFormatRuntimeEventInfo(eventHandle, definingTypeInfo, contextTypeInfo, reflectedType).WithDebugName();
        }
    }
}

namespace System.Reflection.Runtime.Modules.NativeFormat
{
    //-----------------------------------------------------------------------------------------------------------
    // Modules (these exist only because Modules still exist in the Win8P surface area. There is a 1-1
    //          mapping between Assemblies and Modules.)
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class NativeFormatRuntimeModule
    {
        internal static RuntimeModule GetRuntimeModule(NativeFormatRuntimeAssembly assembly)
        {
            return new NativeFormatRuntimeModule(assembly);
        }
    }
}

namespace System.Reflection.Runtime.ParameterInfos.NativeFormat
{
    //-----------------------------------------------------------------------------------------------------------
    // ParameterInfos for MethodBase objects with Parameter metadata.
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class NativeFormatMethodParameterInfo
    {
        internal static NativeFormatMethodParameterInfo GetNativeFormatMethodParameterInfo(MethodBase member, int position, ParameterHandle parameterHandle, QSignatureTypeHandle qualifiedParameterType, TypeContext typeContext)
        {
            return new NativeFormatMethodParameterInfo(member, position, parameterHandle, qualifiedParameterType, typeContext);
        }
    }
}

namespace System.Reflection.Runtime.CustomAttributes
{
    //-----------------------------------------------------------------------------------------------------------
    // CustomAttributeData objects returned by various CustomAttributes properties.
    //-----------------------------------------------------------------------------------------------------------
    internal abstract partial class RuntimeCustomAttributeData
    {
        internal static IEnumerable<CustomAttributeData> GetCustomAttributes(MetadataReader reader, CustomAttributeHandleCollection customAttributeHandles)
        {
            foreach (CustomAttributeHandle customAttributeHandle in customAttributeHandles)
                yield return GetCustomAttributeData(reader, customAttributeHandle);
        }

        private static NativeFormatCustomAttributeData GetCustomAttributeData(MetadataReader reader, CustomAttributeHandle customAttributeHandle)
        {
            return new NativeFormatCustomAttributeData(reader, customAttributeHandle);
        }
    }
}
