// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.MethodInfos.NativeFormat;
using System.Reflection.Runtime.FieldInfos.NativeFormat;
using System.Reflection.Runtime.PropertyInfos.NativeFormat;
using System.Reflection.Runtime.EventInfos.NativeFormat;
using NameFilter = System.Reflection.Runtime.BindingFlagSupport.NameFilter;

using Internal.Metadata.NativeFormat;

namespace System.Reflection.Runtime.TypeInfos.NativeFormat
{
    internal sealed partial class NativeFormatRuntimeNamedTypeInfo
    {
        internal sealed override IEnumerable<ConstructorInfo> CoreGetDeclaredConstructors(NameFilter optionalNameFilter, RuntimeTypeInfo contextTypeInfo)
        {
            //
            // - It may sound odd to get a non-null name filter for a constructor search, but Type.GetMember() is an api that does this.
            //
            // - All GetConstructor() apis act as if BindingFlags.DeclaredOnly were specified. So the ReflectedType will always be the declaring type and so is not passed to this method.
            //
            MetadataReader reader = Reader;
            foreach (MethodHandle methodHandle in DeclaredMethodAndConstructorHandles)
            {
                Method method = methodHandle.GetMethod(reader);

                if (!NativeFormatMetadataReaderExtensions.IsConstructor(ref method, reader))
                    continue;

                if (optionalNameFilter == null || optionalNameFilter.Matches(method.Name, reader))
                    yield return RuntimePlainConstructorInfo<NativeFormatMethodCommon>.GetRuntimePlainConstructorInfo(new NativeFormatMethodCommon(methodHandle, this, contextTypeInfo));
            }
        }

        internal sealed override IEnumerable<MethodInfo> CoreGetDeclaredMethods(NameFilter optionalNameFilter, RuntimeTypeInfo reflectedType, RuntimeTypeInfo contextTypeInfo)
        {
            MetadataReader reader = Reader;
            foreach (MethodHandle methodHandle in DeclaredMethodAndConstructorHandles)
            {
                Method method = methodHandle.GetMethod(reader);

                if (NativeFormatMetadataReaderExtensions.IsConstructor(ref method, reader))
                    continue;

                if (optionalNameFilter == null || optionalNameFilter.Matches(method.Name, reader))
                    yield return RuntimeNamedMethodInfo<NativeFormatMethodCommon>.GetRuntimeNamedMethodInfo(new NativeFormatMethodCommon(methodHandle, this, contextTypeInfo), reflectedType);
            }
        }

        internal sealed override IEnumerable<EventInfo> CoreGetDeclaredEvents(NameFilter optionalNameFilter, RuntimeTypeInfo reflectedType, RuntimeTypeInfo contextTypeInfo)
        {
            MetadataReader reader = Reader;
            foreach (EventHandle eventHandle in DeclaredEventHandles)
            {
                if (optionalNameFilter == null || optionalNameFilter.Matches(eventHandle.GetEvent(reader).Name, reader))
                    yield return NativeFormatRuntimeEventInfo.GetRuntimeEventInfo(eventHandle, this, contextTypeInfo, reflectedType);
            }
        }

        internal sealed override IEnumerable<FieldInfo> CoreGetDeclaredFields(NameFilter optionalNameFilter, RuntimeTypeInfo reflectedType, RuntimeTypeInfo contextTypeInfo)
        {
            MetadataReader reader = Reader;
            foreach (FieldHandle fieldHandle in DeclaredFieldHandles)
            {
                if (optionalNameFilter == null || optionalNameFilter.Matches(fieldHandle.GetField(reader).Name, reader))
                    yield return NativeFormatRuntimeFieldInfo.GetRuntimeFieldInfo(fieldHandle, this, contextTypeInfo, reflectedType);
            }
        }

        internal sealed override IEnumerable<PropertyInfo> CoreGetDeclaredProperties(NameFilter optionalNameFilter, RuntimeTypeInfo reflectedType, RuntimeTypeInfo contextTypeInfo)
        {
            MetadataReader reader = Reader;
            foreach (PropertyHandle propertyHandle in DeclaredPropertyHandles)
            {
                if (optionalNameFilter == null || optionalNameFilter.Matches(propertyHandle.GetProperty(reader).Name, reader))
                    yield return NativeFormatRuntimePropertyInfo.GetRuntimePropertyInfo(propertyHandle, this, contextTypeInfo, reflectedType);
            }
        }

        internal sealed override IEnumerable<Type> CoreGetDeclaredNestedTypes(NameFilter optionalNameFilter)
        {
            foreach (TypeDefinitionHandle nestedTypeHandle in _typeDefinition.NestedTypes)
            {
                if (optionalNameFilter == null || optionalNameFilter.Matches(nestedTypeHandle.GetTypeDefinition(_reader).Name, _reader))
                    yield return nestedTypeHandle.GetNamedType(_reader);
            }
        }
    }
}
