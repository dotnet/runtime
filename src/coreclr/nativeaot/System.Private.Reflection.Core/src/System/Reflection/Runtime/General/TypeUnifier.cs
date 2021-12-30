// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.MethodInfos;

using Internal.Reflection.Core.Execution;

//
// It is common practice for app code to compare Type objects using reference equality with the expectation that reference equality
// is equivalent to semantic equality. To support this, all RuntimeTypeObject objects are interned using weak references.
//
// This assumption is baked into the codebase in these places:
//
//   - RuntimeTypeInfo.Equals(object) implements itself as Object.ReferenceEquals(this, obj)
//
//   - RuntimeTypeInfo.GetHashCode() is implemented in a flavor-specific manner (We can't use Object.GetHashCode()
//     because we don't want the hash value to change if a type is collected and resurrected later.)
//
// This assumption is actualized as follows:
//
//   - RuntimeTypeInfo classes hide their constructor. The only way to instantiate a RuntimeTypeInfo
//     is through its public static factory method which ensures the interning and are collected in this one
//     file for easy auditing and to help ensure that they all operate in a consistent manner.
//
//   - The TypeUnifier extension class provides a more friendly interface to the rest of the codebase.
//

namespace System.Reflection.Runtime.General
{
    internal static partial class TypeUnifier
    {
        // This can be replaced at native compile time using a feature switch.
        internal static bool IsTypeConstructionEagerlyValidated => true;

        public static RuntimeTypeInfo GetArrayType(this RuntimeTypeInfo elementType)
        {
            return RuntimeArrayTypeInfo.GetArrayTypeInfo(elementType, multiDim: false, rank: 1);
        }

        public static RuntimeTypeInfo GetArrayTypeWithTypeHandle(this RuntimeTypeInfo elementType)
        {
            return RuntimeArrayTypeInfo.GetArrayTypeInfo(elementType, multiDim: false, rank: 1).WithVerifiedTypeHandle(elementType);
        }

        public static RuntimeTypeInfo GetMultiDimArrayType(this RuntimeTypeInfo elementType, int rank)
        {
            return RuntimeArrayTypeInfo.GetArrayTypeInfo(elementType, multiDim: true, rank: rank);
        }

        public static RuntimeTypeInfo GetMultiDimArrayTypeWithTypeHandle(this RuntimeTypeInfo elementType, int rank)
        {
            return RuntimeArrayTypeInfo.GetArrayTypeInfo(elementType, multiDim: true, rank: rank).WithVerifiedTypeHandle(elementType);
        }

        private static RuntimeArrayTypeInfo WithVerifiedTypeHandle(this RuntimeArrayTypeInfo arrayType, RuntimeTypeInfo elementType)
        {
            // We only permit creating parameterized types if the pay-for-play policy specifically allows them *or* if the result
            // type would be an open type.
            RuntimeTypeHandle typeHandle = arrayType.InternalTypeHandleIfAvailable;
            if (IsTypeConstructionEagerlyValidated
                && typeHandle.IsNull() && !elementType.ContainsGenericParameters
#if FEATURE_COMINTEROP
                && !(elementType is RuntimeCLSIDTypeInfo)
#endif
                )
                throw ReflectionCoreExecution.ExecutionDomain.CreateMissingArrayTypeException(elementType, isMultiDim: false, 1);

            return arrayType;
        }

        public static RuntimeTypeInfo GetByRefType(this RuntimeTypeInfo targetType)
        {
            return RuntimeByRefTypeInfo.GetByRefTypeInfo(targetType);
        }

        public static RuntimeTypeInfo GetPointerType(this RuntimeTypeInfo targetType)
        {
            return RuntimePointerTypeInfo.GetPointerTypeInfo(targetType);
        }

        public static RuntimeTypeInfo GetConstructedGenericType(this RuntimeTypeInfo genericTypeDefinition, RuntimeTypeInfo[] genericTypeArguments)
        {
            return RuntimeConstructedGenericTypeInfo.GetRuntimeConstructedGenericTypeInfo(genericTypeDefinition, genericTypeArguments);
        }

        public static RuntimeTypeInfo GetConstructedGenericTypeWithTypeHandle(this RuntimeTypeInfo genericTypeDefinition, RuntimeTypeInfo[] genericTypeArguments)
        {
            return RuntimeConstructedGenericTypeInfo.GetRuntimeConstructedGenericTypeInfo(genericTypeDefinition, genericTypeArguments).WithVerifiedTypeHandle(genericTypeArguments);
        }

        private static RuntimeConstructedGenericTypeInfo WithVerifiedTypeHandle(this RuntimeConstructedGenericTypeInfo genericType, RuntimeTypeInfo[] genericTypeArguments)
        {
            // We only permit creating parameterized types if the pay-for-play policy specifically allows them *or* if the result
            // type would be an open type.
            RuntimeTypeHandle typeHandle = genericType.InternalTypeHandleIfAvailable;
            if (IsTypeConstructionEagerlyValidated && typeHandle.IsNull())
            {
                bool atLeastOneOpenType = false;
                foreach (RuntimeTypeInfo genericTypeArgument in genericTypeArguments)
                {
                    if (genericTypeArgument.ContainsGenericParameters)
                        atLeastOneOpenType = true;
                }
                if (!atLeastOneOpenType)
                    throw ReflectionCoreExecution.ExecutionDomain.CreateMissingConstructedGenericTypeException(genericType.GetGenericTypeDefinition(), genericTypeArguments.CloneTypeArray());
            }

            return genericType;
        }

        public static RuntimeTypeInfo GetTypeForRuntimeTypeHandle(this RuntimeTypeHandle typeHandle)
        {
            Type type = Type.GetTypeFromHandle(typeHandle);
            return type.CastToRuntimeTypeInfo();
        }

        //======================================================================================================
        // This next group services the Type.GetTypeFromHandle() path. Since we already have a RuntimeTypeHandle
        // in that case, we pass it in as an extra argument as an optimization (otherwise, the unifier will
        // waste cycles looking up the handle again from the mapping tables.)
        //======================================================================================================

        public static RuntimeTypeInfo GetArrayType(this RuntimeTypeInfo elementType, RuntimeTypeHandle precomputedTypeHandle)
        {
            return RuntimeArrayTypeInfo.GetArrayTypeInfo(elementType, multiDim: false, rank: 1, precomputedTypeHandle: precomputedTypeHandle);
        }

        public static RuntimeTypeInfo GetMultiDimArrayType(this RuntimeTypeInfo elementType, int rank, RuntimeTypeHandle precomputedTypeHandle)
        {
            return RuntimeArrayTypeInfo.GetArrayTypeInfo(elementType, multiDim: true, rank: rank, precomputedTypeHandle: precomputedTypeHandle);
        }

        public static RuntimeTypeInfo GetPointerType(this RuntimeTypeInfo targetType, RuntimeTypeHandle precomputedTypeHandle)
        {
            return RuntimePointerTypeInfo.GetPointerTypeInfo(targetType, precomputedTypeHandle);
        }

        public static RuntimeTypeInfo GetByRefType(this RuntimeTypeInfo targetType, RuntimeTypeHandle precomputedTypeHandle)
        {
            return RuntimeByRefTypeInfo.GetByRefTypeInfo(targetType, precomputedTypeHandle);
        }

        public static RuntimeTypeInfo GetConstructedGenericType(this RuntimeTypeInfo genericTypeDefinition, RuntimeTypeInfo[] genericTypeArguments, RuntimeTypeHandle precomputedTypeHandle)
        {
            return RuntimeConstructedGenericTypeInfo.GetRuntimeConstructedGenericTypeInfo(genericTypeDefinition, genericTypeArguments, precomputedTypeHandle);
        }
    }
}

namespace System.Reflection.Runtime.TypeInfos
{
    //-----------------------------------------------------------------------------------------------------------
    // TypeInfos for type definitions (i.e. "Foo" and "Foo<>" but not "Foo<int>") that aren't opted into metadata.
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class RuntimeNoMetadataNamedTypeInfo
    {
        internal static RuntimeNoMetadataNamedTypeInfo GetRuntimeNoMetadataNamedTypeInfo(RuntimeTypeHandle typeHandle, bool isGenericTypeDefinition)
        {
            RuntimeNoMetadataNamedTypeInfo type;
            if (isGenericTypeDefinition)
                type = GenericNoMetadataNamedTypeTable.Table.GetOrAdd(new RuntimeTypeHandleKey(typeHandle));
            else
                type = NoMetadataNamedTypeTable.Table.GetOrAdd(new RuntimeTypeHandleKey(typeHandle));
            type.EstablishDebugName();
            return type;
        }

        private sealed class NoMetadataNamedTypeTable : ConcurrentUnifierW<RuntimeTypeHandleKey, RuntimeNoMetadataNamedTypeInfo>
        {
            protected sealed override RuntimeNoMetadataNamedTypeInfo Factory(RuntimeTypeHandleKey key)
            {
                return new RuntimeNoMetadataNamedTypeInfo(key.TypeHandle, isGenericTypeDefinition: false);
            }

            public static readonly NoMetadataNamedTypeTable Table = new NoMetadataNamedTypeTable();
        }

        private sealed class GenericNoMetadataNamedTypeTable : ConcurrentUnifierW<RuntimeTypeHandleKey, RuntimeNoMetadataNamedTypeInfo>
        {
            protected sealed override RuntimeNoMetadataNamedTypeInfo Factory(RuntimeTypeHandleKey key)
            {
                return new RuntimeNoMetadataNamedTypeInfo(key.TypeHandle, isGenericTypeDefinition: true);
            }

            public static readonly GenericNoMetadataNamedTypeTable Table = new GenericNoMetadataNamedTypeTable();
        }
    }

    //-----------------------------------------------------------------------------------------------------------
    // TypeInfos that represent type definitions (i.e. Foo or Foo<>) or constructed generic types (Foo<int>)
    // that can never be reflection-enabled due to the framework Reflection block.
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class RuntimeBlockedTypeInfo
    {
        internal static RuntimeBlockedTypeInfo GetRuntimeBlockedTypeInfo(RuntimeTypeHandle typeHandle, bool isGenericTypeDefinition)
        {
            RuntimeBlockedTypeInfo type;
            if (isGenericTypeDefinition)
                type = GenericBlockedTypeTable.Table.GetOrAdd(new RuntimeTypeHandleKey(typeHandle));
            else
                type = BlockedTypeTable.Table.GetOrAdd(new RuntimeTypeHandleKey(typeHandle));
            type.EstablishDebugName();
            return type;
        }

        private sealed class BlockedTypeTable : ConcurrentUnifierW<RuntimeTypeHandleKey, RuntimeBlockedTypeInfo>
        {
            protected sealed override RuntimeBlockedTypeInfo Factory(RuntimeTypeHandleKey key)
            {
                return new RuntimeBlockedTypeInfo(key.TypeHandle, isGenericTypeDefinition: false);
            }

            public static readonly BlockedTypeTable Table = new BlockedTypeTable();
        }

        private sealed class GenericBlockedTypeTable : ConcurrentUnifierW<RuntimeTypeHandleKey, RuntimeBlockedTypeInfo>
        {
            protected sealed override RuntimeBlockedTypeInfo Factory(RuntimeTypeHandleKey key)
            {
                return new RuntimeBlockedTypeInfo(key.TypeHandle, isGenericTypeDefinition: true);
            }

            public static readonly GenericBlockedTypeTable Table = new GenericBlockedTypeTable();
        }
    }

    //-----------------------------------------------------------------------------------------------------------
    // TypeInfos for Sz and multi-dim Array types.
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class RuntimeArrayTypeInfo : RuntimeHasElementTypeInfo
    {
        internal static RuntimeArrayTypeInfo GetArrayTypeInfo(RuntimeTypeInfo elementType, bool multiDim, int rank)
        {
            return GetArrayTypeInfo(elementType, multiDim, rank, GetRuntimeTypeHandleIfAny(elementType, multiDim, rank));
        }

        internal static RuntimeArrayTypeInfo GetArrayTypeInfo(RuntimeTypeInfo elementType, bool multiDim, int rank, RuntimeTypeHandle precomputedTypeHandle)
        {
            Debug.Assert(multiDim || rank == 1);

            UnificationKey key = new UnificationKey(elementType, precomputedTypeHandle);
            RuntimeArrayTypeInfo type;
            if (!multiDim)
                type = ArrayTypeTable.Table.GetOrAdd(key);
            else
                type = TypeTableForMultiDimArrayTypeTables.Table.GetOrAdd(rank).GetOrAdd(key);
            type.EstablishDebugName();
            return type;
        }

        private static RuntimeTypeHandle GetRuntimeTypeHandleIfAny(RuntimeTypeInfo elementType, bool multiDim, int rank)
        {
            Debug.Assert(multiDim || rank == 1);

            RuntimeTypeHandle elementTypeHandle = elementType.InternalTypeHandleIfAvailable;
            if (elementTypeHandle.IsNull())
                return default(RuntimeTypeHandle);

            // The check is here on purpose - one of the implementations of IsByRefLike contains a custom attribute
            // search and those are very expensive from size on disk footprint perspective. We purposefully
            // place this call in a path that won't be part of the executable image unless more advanced reflection services
            // are also needed ("pay for play"). We really don't want a typeof() to push the app into requiring the full reflection
            // stack to be compiled into the final executable.
            if (elementType.IsByRefLike)
                throw new TypeLoadException(SR.Format(SR.ArgumentException_InvalidArrayElementType, elementType));

            RuntimeTypeHandle typeHandle;
            if (!multiDim)
            {
                if (!ReflectionCoreExecution.ExecutionEnvironment.TryGetArrayTypeForElementType(elementTypeHandle, out typeHandle))
                    return default(RuntimeTypeHandle);
            }
            else
            {
                if (!ReflectionCoreExecution.ExecutionEnvironment.TryGetMultiDimArrayTypeForElementType(elementTypeHandle, rank, out typeHandle))
                    return default(RuntimeTypeHandle);
            }

            return typeHandle;
        }

        private sealed class ArrayTypeTable : ConcurrentUnifierWKeyed<UnificationKey, RuntimeArrayTypeInfo>
        {
            protected sealed override RuntimeArrayTypeInfo Factory(UnificationKey key)
            {
                ValidateElementType(key.ElementType, key.TypeHandle, multiDim: false, rank: 1);

                return new RuntimeArrayTypeInfo(key, multiDim: false, rank: 1);
            }

            public static readonly ArrayTypeTable Table = new ArrayTypeTable();
        }

        private sealed class MultiDimArrayTypeTable : ConcurrentUnifierWKeyed<UnificationKey, RuntimeArrayTypeInfo>
        {
            public MultiDimArrayTypeTable(int rank)
            {
                _rank = rank;
            }

            protected sealed override RuntimeArrayTypeInfo Factory(UnificationKey key)
            {
                ValidateElementType(key.ElementType, key.TypeHandle, multiDim: true, rank: _rank);

                return new RuntimeArrayTypeInfo(key, multiDim: true, rank: _rank);
            }

            private readonly int _rank;
        }

        //
        // For the hopefully rare case of multidim arrays, we have a dictionary of dictionaries.
        //
        private sealed class TypeTableForMultiDimArrayTypeTables : ConcurrentUnifier<int, MultiDimArrayTypeTable>
        {
            protected sealed override MultiDimArrayTypeTable Factory(int rank)
            {
                Debug.Assert(rank > 0);
                return new MultiDimArrayTypeTable(rank);
            }

            public static readonly TypeTableForMultiDimArrayTypeTables Table = new TypeTableForMultiDimArrayTypeTables();
        }

        private static void ValidateElementType(RuntimeTypeInfo elementType, RuntimeTypeHandle typeHandle, bool multiDim, int rank)
        {
            Debug.Assert(multiDim || rank == 1);

            if (elementType.IsByRef)
                throw new TypeLoadException(SR.Format(SR.ArgumentException_InvalidArrayElementType, elementType));
        }
    }

    //-----------------------------------------------------------------------------------------------------------
    // TypeInfos for ByRef types.
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class RuntimeByRefTypeInfo : RuntimeHasElementTypeInfo
    {
        internal static RuntimeByRefTypeInfo GetByRefTypeInfo(RuntimeTypeInfo elementType)
        {
            return GetByRefTypeInfo(elementType, GetRuntimeTypeHandleIfAny(elementType));
        }

        internal static RuntimeByRefTypeInfo GetByRefTypeInfo(RuntimeTypeInfo elementType, RuntimeTypeHandle precomputedTypeHandle)
        {
            RuntimeByRefTypeInfo type = ByRefTypeTable.Table.GetOrAdd(new UnificationKey(elementType, precomputedTypeHandle));
            type.EstablishDebugName();
            return type;
        }

        private static RuntimeTypeHandle GetRuntimeTypeHandleIfAny(RuntimeTypeInfo elementType)
        {
            RuntimeTypeHandle elementTypeHandle = elementType.InternalTypeHandleIfAvailable;
            if (elementTypeHandle.IsNull())
                return default(RuntimeTypeHandle);

            RuntimeTypeHandle typeHandle;
            if (!ReflectionCoreExecution.ExecutionEnvironment.TryGetByRefTypeForTargetType(elementTypeHandle, out typeHandle))
                return default(RuntimeTypeHandle);

            return typeHandle;
        }

        private sealed class ByRefTypeTable : ConcurrentUnifierWKeyed<UnificationKey, RuntimeByRefTypeInfo>
        {
            protected sealed override RuntimeByRefTypeInfo Factory(UnificationKey key)
            {
                if (key.ElementType.IsByRef)
                    throw new TypeLoadException(SR.Format(SR.CannotCreateByRefOfByRef, key.ElementType));

                return new RuntimeByRefTypeInfo(key);
            }

            public static readonly ByRefTypeTable Table = new ByRefTypeTable();
        }
    }

    //-----------------------------------------------------------------------------------------------------------
    // TypeInfos for Pointer types.
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class RuntimePointerTypeInfo : RuntimeHasElementTypeInfo
    {
        internal static RuntimePointerTypeInfo GetPointerTypeInfo(RuntimeTypeInfo elementType)
        {
            return GetPointerTypeInfo(elementType, precomputedTypeHandle: GetRuntimeTypeHandleIfAny(elementType));
        }

        internal static RuntimePointerTypeInfo GetPointerTypeInfo(RuntimeTypeInfo elementType, RuntimeTypeHandle precomputedTypeHandle)
        {
            RuntimePointerTypeInfo type = PointerTypeTable.Table.GetOrAdd(new UnificationKey(elementType, precomputedTypeHandle));
            type.EstablishDebugName();
            return type;
        }

        private static RuntimeTypeHandle GetRuntimeTypeHandleIfAny(RuntimeTypeInfo elementType)
        {
            RuntimeTypeHandle elementTypeHandle = elementType.InternalTypeHandleIfAvailable;
            if (elementTypeHandle.IsNull())
                return default(RuntimeTypeHandle);

            RuntimeTypeHandle typeHandle;
            if (!ReflectionCoreExecution.ExecutionEnvironment.TryGetPointerTypeForTargetType(elementTypeHandle, out typeHandle))
                return default(RuntimeTypeHandle);

            return typeHandle;
        }

        private sealed class PointerTypeTable : ConcurrentUnifierWKeyed<UnificationKey, RuntimePointerTypeInfo>
        {
            protected sealed override RuntimePointerTypeInfo Factory(UnificationKey key)
            {
                if (key.ElementType.IsByRef)
                    throw new TypeLoadException(SR.Format(SR.CannotCreatePointerOfByRef, key.ElementType));

                return new RuntimePointerTypeInfo(key);
            }

            public static readonly PointerTypeTable Table = new PointerTypeTable();
        }
    }

    //-----------------------------------------------------------------------------------------------------------
    // TypeInfos for Constructed generic types ("Foo<int>")
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class RuntimeConstructedGenericTypeInfo : RuntimeTypeInfo, IKeyedItem<RuntimeConstructedGenericTypeInfo.UnificationKey>
    {
        internal static RuntimeConstructedGenericTypeInfo GetRuntimeConstructedGenericTypeInfo(RuntimeTypeInfo genericTypeDefinition, RuntimeTypeInfo[] genericTypeArguments)
        {
            return GetRuntimeConstructedGenericTypeInfo(genericTypeDefinition, genericTypeArguments, precomputedTypeHandle: GetRuntimeTypeHandleIfAny(genericTypeDefinition, genericTypeArguments));
        }

        internal static RuntimeConstructedGenericTypeInfo GetRuntimeConstructedGenericTypeInfo(RuntimeTypeInfo genericTypeDefinition, RuntimeTypeInfo[] genericTypeArguments, RuntimeTypeHandle precomputedTypeHandle)
        {
            UnificationKey key = new UnificationKey(genericTypeDefinition, genericTypeArguments, precomputedTypeHandle);
            RuntimeConstructedGenericTypeInfo typeInfo = ConstructedGenericTypeTable.Table.GetOrAdd(key);
            typeInfo.EstablishDebugName();
            return typeInfo;
        }

        private static RuntimeTypeHandle GetRuntimeTypeHandleIfAny(RuntimeTypeInfo genericTypeDefinition, RuntimeTypeInfo[] genericTypeArguments)
        {
            RuntimeTypeHandle genericTypeDefinitionHandle = genericTypeDefinition.InternalTypeHandleIfAvailable;
            if (genericTypeDefinitionHandle.IsNull())
                return default(RuntimeTypeHandle);

            if (ReflectionCoreExecution.ExecutionEnvironment.IsReflectionBlocked(genericTypeDefinitionHandle))
                return default(RuntimeTypeHandle);

            int count = genericTypeArguments.Length;
            RuntimeTypeHandle[] genericTypeArgumentHandles = new RuntimeTypeHandle[count];
            for (int i = 0; i < count; i++)
            {
                RuntimeTypeHandle genericTypeArgumentHandle = genericTypeArguments[i].InternalTypeHandleIfAvailable;
                if (genericTypeArgumentHandle.IsNull())
                    return default(RuntimeTypeHandle);
                genericTypeArgumentHandles[i] = genericTypeArgumentHandle;
            }

            RuntimeTypeHandle typeHandle;
            if (!ReflectionCoreExecution.ExecutionEnvironment.TryGetConstructedGenericTypeForComponents(genericTypeDefinitionHandle, genericTypeArgumentHandles, out typeHandle))
                return default(RuntimeTypeHandle);

            return typeHandle;
        }

        private sealed class ConstructedGenericTypeTable : ConcurrentUnifierWKeyed<UnificationKey, RuntimeConstructedGenericTypeInfo>
        {
            protected sealed override RuntimeConstructedGenericTypeInfo Factory(UnificationKey key)
            {
                foreach (RuntimeTypeInfo genericTypeArgument in key.GenericTypeArguments)
                {
                    if (genericTypeArgument.IsByRef || genericTypeArgument.IsGenericTypeDefinition)
                        throw new ArgumentException(SR.Format(SR.ArgumentException_InvalidTypeArgument, genericTypeArgument));
                }

                return new RuntimeConstructedGenericTypeInfo(key);
            }

            public static readonly ConstructedGenericTypeTable Table = new ConstructedGenericTypeTable();
        }
    }

#if FEATURE_COMINTEROP
    internal sealed partial class RuntimeCLSIDTypeInfo
    {
        public static RuntimeCLSIDTypeInfo GetRuntimeCLSIDTypeInfo(Guid clsid, string server)
        {
            UnificationKey key = new UnificationKey(clsid, server);
            return ClsIdTypeTable.Table.GetOrAdd(key);
        }

        private sealed class ClsIdTypeTable : ConcurrentUnifierWKeyed<UnificationKey, RuntimeCLSIDTypeInfo>
        {
            protected sealed override RuntimeCLSIDTypeInfo Factory(UnificationKey key)
            {
                return new RuntimeCLSIDTypeInfo(key.ClsId, key.Server);
            }

            public static readonly ClsIdTypeTable Table = new ClsIdTypeTable();
        }
    }
#endif
}
