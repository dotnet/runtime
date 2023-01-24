// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.FieldInfos;
using System.Reflection.Runtime.PropertyInfos;
using System.Reflection.Runtime.EventInfos;
using NameFilter = System.Reflection.Runtime.BindingFlagSupport.NameFilter;

using Internal.Reflection.Core.Execution;

//
// The CoreGet() methods on RuntimeTypeInfo provide the raw source material for the Type.Get*() family of apis.
//
// These retrieve directly introduced (not inherited) members whose names match the passed in NameFilter (if NameFilter is null,
// return all members.) To avoid allocating objects, prefer to pass the metadata constant string value handle to NameFilter rather
// than strings.
//
// The ReflectedType is the type that the Type.Get*() api was invoked on. Use it to establish the returned MemberInfo object's
// ReflectedType.
//
namespace System.Reflection.Runtime.TypeInfos
{
    internal abstract partial class RuntimeTypeInfo
    {
        internal IEnumerable<ConstructorInfo> CoreGetDeclaredConstructors(NameFilter optionalNameFilter)
        {
            //
            // - It may sound odd to get a non-null name filter for a constructor search, but Type.GetMember() is an api that does this.
            //
            // - All GetConstructor() apis act as if BindingFlags.DeclaredOnly were specified. So the ReflectedType will always be the declaring type and so is not passed to this method.
            //
            RuntimeNamedTypeInfo definingType = AnchoringTypeDefinitionForDeclaredMembers;

            if (definingType != null)
            {
                // If there is a definingType, we do not support Synthetic constructors
                Debug.Assert(object.ReferenceEquals(SyntheticConstructors, Array.Empty<RuntimeConstructorInfo>()));

                return definingType.CoreGetDeclaredConstructors(optionalNameFilter, this);
            }

            return CoreGetDeclaredSyntheticConstructors(optionalNameFilter);
        }

        private IEnumerable<ConstructorInfo> CoreGetDeclaredSyntheticConstructors(NameFilter optionalNameFilter)
        {
            foreach (RuntimeConstructorInfo syntheticConstructor in SyntheticConstructors)
            {
                if (optionalNameFilter == null || optionalNameFilter.Matches(syntheticConstructor.IsStatic ? ConstructorInfo.TypeConstructorName : ConstructorInfo.ConstructorName))
                    yield return syntheticConstructor;
            }
        }

        internal IEnumerable<MethodInfo> CoreGetDeclaredMethods(NameFilter optionalNameFilter, RuntimeTypeInfo reflectedType)
        {
            RuntimeNamedTypeInfo definingType = AnchoringTypeDefinitionForDeclaredMembers;
            if (definingType != null)
            {
                // If there is a definingType, we do not support Synthetic constructors
                Debug.Assert(object.ReferenceEquals(SyntheticMethods, Array.Empty<RuntimeMethodInfo>()));

                return definingType.CoreGetDeclaredMethods(optionalNameFilter, reflectedType, this);
            }

            return CoreGetDeclaredSyntheticMethods(optionalNameFilter);
        }

        private IEnumerable<MethodInfo> CoreGetDeclaredSyntheticMethods(NameFilter optionalNameFilter)
        {
            foreach (RuntimeMethodInfo syntheticMethod in SyntheticMethods)
            {
                if (optionalNameFilter == null || optionalNameFilter.Matches(syntheticMethod.Name))
                    yield return syntheticMethod;
            }
        }

        internal IEnumerable<EventInfo> CoreGetDeclaredEvents(NameFilter optionalNameFilter, RuntimeTypeInfo reflectedType)
        {
            RuntimeNamedTypeInfo definingType = AnchoringTypeDefinitionForDeclaredMembers;
            if (definingType != null)
            {
                return definingType.CoreGetDeclaredEvents(optionalNameFilter, reflectedType, this);
            }
            return Array.Empty<EventInfo>();
        }

        internal IEnumerable<FieldInfo> CoreGetDeclaredFields(NameFilter optionalNameFilter, RuntimeTypeInfo reflectedType)
        {
            RuntimeNamedTypeInfo definingType = AnchoringTypeDefinitionForDeclaredMembers;
            if (definingType != null)
            {
                return definingType.CoreGetDeclaredFields(optionalNameFilter, reflectedType, this);
            }
            return Array.Empty<FieldInfo>();
        }

        internal IEnumerable<PropertyInfo> CoreGetDeclaredProperties(NameFilter optionalNameFilter, RuntimeTypeInfo reflectedType)
        {
            RuntimeNamedTypeInfo definingType = AnchoringTypeDefinitionForDeclaredMembers;
            if (definingType != null)
            {
                return definingType.CoreGetDeclaredProperties(optionalNameFilter, reflectedType, this);
            }

            return Array.Empty<PropertyInfo>();
        }

        //
        // - All GetNestedType() apis act as if BindingFlags.DeclaredOnly were specified. So the ReflectedType will always be the declaring type and so is not passed to this method.
        //
        // This method is left unsealed as RuntimeNamedTypeInfo and others need to override with specific implementations.
        //
        internal virtual IEnumerable<Type> CoreGetDeclaredNestedTypes(NameFilter optionalNameFilter)
        {
            return Array.Empty<Type>();
        }
    }

    internal abstract partial class RuntimeNamedTypeInfo
    {
        // Metadata providing implementations of RuntimeNamedTypeInfo implement the following methods
        // to provide filtered access to the various reflection objects by reading metadata directly.
        // The loop of examining methods is done in a metadata specific manner for greater efficiency.
        internal abstract IEnumerable<ConstructorInfo> CoreGetDeclaredConstructors(NameFilter optionalNameFilter, RuntimeTypeInfo contextTypeInfo);
        internal abstract IEnumerable<MethodInfo> CoreGetDeclaredMethods(NameFilter optionalNameFilter, RuntimeTypeInfo reflectedType, RuntimeTypeInfo contextTypeInfo);
        internal abstract IEnumerable<EventInfo> CoreGetDeclaredEvents(NameFilter optionalNameFilter, RuntimeTypeInfo reflectedType, RuntimeTypeInfo contextTypeInfo);
        internal abstract IEnumerable<FieldInfo> CoreGetDeclaredFields(NameFilter optionalNameFilter, RuntimeTypeInfo reflectedType, RuntimeTypeInfo contextTypeInfo);
        internal abstract IEnumerable<PropertyInfo> CoreGetDeclaredProperties(NameFilter optionalNameFilter, RuntimeTypeInfo reflectedType, RuntimeTypeInfo contextTypeInfo);
    }

    internal sealed partial class RuntimeConstructedGenericTypeInfo
    {
        internal sealed override IEnumerable<Type> CoreGetDeclaredNestedTypes(NameFilter optionalNameFilter)
        {
            return GenericTypeDefinitionTypeInfo.CoreGetDeclaredNestedTypes(optionalNameFilter);
        }
    }

    internal sealed partial class RuntimeBlockedTypeInfo
    {
        internal sealed override IEnumerable<Type> CoreGetDeclaredNestedTypes(NameFilter optionalNameFilter)
        {
            return Array.Empty<Type>();
        }
    }
}
