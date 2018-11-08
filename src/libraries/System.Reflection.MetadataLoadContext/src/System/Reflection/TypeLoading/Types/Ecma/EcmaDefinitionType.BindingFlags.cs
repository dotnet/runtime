// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;
using System.Collections.Generic;

namespace System.Reflection.TypeLoading.Ecma
{
    // Low level support for the BindingFlag-driven enumerator apis. These return members declared (not inherited) on the current
    // type, possibly doing case-sensitive/case-insensitive filtering on a supplied name. 
    internal sealed partial class EcmaDefinitionType
    {
        //
        // - It may sound odd to get a non-null name filter for a constructor search, but Type.GetMember() is an api that does this.
        //
        // - All GetConstructor() apis act as if BindingFlags.DeclaredOnly were specified. So the ReflectedType will always be the declaring type and so is not passed to this method.
        //
        internal sealed override IEnumerable<ConstructorInfo> SpecializeConstructors(NameFilter filter, RoInstantiationProviderType declaringType)
        {
            MetadataReader reader = Reader;
            foreach (MethodDefinitionHandle handle in TypeDefinition.GetMethods())
            {
                MethodDefinition methodDefinition = handle.GetMethodDefinition(reader);
                if (filter == null || filter.Matches(methodDefinition.Name, reader))
                {
                    if (methodDefinition.IsConstructor(reader))
                        yield return new RoDefinitionConstructor<EcmaMethodDecoder>(declaringType, new EcmaMethodDecoder(handle, GetEcmaModule()));
                }
            }
        }

        internal sealed override IEnumerable<MethodInfo> SpecializeMethods(NameFilter filter, Type reflectedType, RoInstantiationProviderType declaringType)
        {
            MetadataReader reader = Reader;
            foreach (MethodDefinitionHandle handle in TypeDefinition.GetMethods())
            {
                MethodDefinition methodDefinition = handle.GetMethodDefinition(reader);
                if (filter == null || filter.Matches(methodDefinition.Name, reader))
                {
                    if (!methodDefinition.IsConstructor(reader))
                        yield return new RoDefinitionMethod<EcmaMethodDecoder>(declaringType, reflectedType, new EcmaMethodDecoder(handle, GetEcmaModule()));
                }
            }
        }

        internal sealed override IEnumerable<EventInfo> SpecializeEvents(NameFilter filter, Type reflectedType, RoInstantiationProviderType declaringType)
        {
            MetadataReader reader = Reader;
            foreach (EventDefinitionHandle handle in TypeDefinition.GetEvents())
            {
                if (filter == null || filter.Matches(handle.GetEventDefinition(reader).Name, reader))
                    yield return new EcmaEvent(declaringType, handle, reflectedType);
            }
        }

        internal sealed override IEnumerable<FieldInfo> SpecializeFields(NameFilter filter, Type reflectedType, RoInstantiationProviderType declaringType)
        {
            MetadataReader reader = Reader;
            foreach (FieldDefinitionHandle handle in TypeDefinition.GetFields())
            {
                if (filter == null || filter.Matches(handle.GetFieldDefinition(reader).Name, reader))
                    yield return new EcmaField(declaringType, handle, reflectedType);
            }
        }

        internal sealed override IEnumerable<PropertyInfo> SpecializeProperties(NameFilter filter, Type reflectedType, RoInstantiationProviderType declaringType)
        {
            MetadataReader reader = Reader;
            foreach (PropertyDefinitionHandle handle in TypeDefinition.GetProperties())
            {
                if (filter == null || filter.Matches(handle.GetPropertyDefinition(reader).Name, reader))
                    yield return new EcmaProperty(declaringType, handle, reflectedType);
            }
        }

        internal sealed override IEnumerable<RoType> GetNestedTypesCore(NameFilter filter)
        {
            MetadataReader reader = Reader;
            foreach (TypeDefinitionHandle handle in TypeDefinition.GetNestedTypes())
            {
                TypeDefinition nestedTypeDefinition = handle.GetTypeDefinition(reader);
                if (filter == null || filter.Matches(nestedTypeDefinition.Name, reader))
                    yield return handle.ResolveTypeDef(GetEcmaModule());
            }
        }

        internal sealed override RoDefinitionType GetNestedTypeCore(ReadOnlySpan<byte> utf8Name)
        {
            RoDefinitionType match = null;
            MetadataReader reader = Reader;
            foreach (TypeDefinitionHandle handle in TypeDefinition.GetNestedTypes())
            {
                TypeDefinition nestedTypeDefinition = handle.GetTypeDefinition(reader);
                if (nestedTypeDefinition.Name.Equals(utf8Name, reader))
                {
                    if (match != null)
                        throw new AmbiguousMatchException();
                    match = handle.ResolveTypeDef(GetEcmaModule());
                }
            }
            return match;
        }
    }
}
