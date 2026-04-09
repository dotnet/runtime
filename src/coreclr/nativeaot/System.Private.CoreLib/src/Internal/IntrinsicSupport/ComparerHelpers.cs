// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The algoritm to choose the default comparer is duplicated in the IL compiler. The compiler will replace the code within
// Comparer<T>.Create method with more specific implementation based on what sort of type is being compared where possible.

using System;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.CompilerServices;

using Internal.Runtime;
using Internal.Runtime.Augments;

namespace Internal.IntrinsicSupport
{
    internal static class ComparerHelpers
    {
        private static unsafe bool ImplementsIComparable(RuntimeTypeHandle t)
        {
            MethodTable* objectType = t.ToMethodTable();
            MethodTable* icomparableType = typeof(IComparable<>).TypeHandle.ToMethodTable();
            int interfaceCount = objectType->NumInterfaces;
            for (int i = 0; i < interfaceCount; i++)
            {
                MethodTable* interfaceType = objectType->InterfaceMap[i];

                if (!interfaceType->IsGeneric)
                    continue;

                if (interfaceType->GenericDefinition == icomparableType)
                {
                    if (interfaceType->GenericArity != 1)
                        continue;

                    if (objectType->IsValueType)
                    {
                        if (interfaceType->GenericArguments[0] == objectType)
                        {
                            return true;
                        }
                    }
                    else if (RuntimeImports.AreTypesAssignable(objectType, interfaceType->GenericArguments[0]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static object GetComparer(RuntimeTypeHandle t)
        {
            RuntimeTypeHandle comparerType;
            RuntimeTypeHandle openComparerType = default(RuntimeTypeHandle);
            RuntimeTypeHandle comparerTypeArgument = default(RuntimeTypeHandle);

            if (RuntimeAugments.IsNullable(t))
            {
                RuntimeTypeHandle nullableType = RuntimeAugments.GetNullableType(t);
                openComparerType = typeof(NullableComparer<>).TypeHandle;
                comparerTypeArgument = nullableType;
            }
            if (EqualityComparerHelpers.IsEnum(t))
            {
                openComparerType = typeof(EnumComparer<>).TypeHandle;
                comparerTypeArgument = t;
            }

            if (openComparerType.Equals(default(RuntimeTypeHandle)))
            {
                if (ImplementsIComparable(t))
                {
                    openComparerType = typeof(GenericComparer<>).TypeHandle;
                    comparerTypeArgument = t;
                }
                else
                {
                    openComparerType = typeof(ObjectComparer<>).TypeHandle;
                    comparerTypeArgument = t;
                }
            }

            bool success = RuntimeAugments.TypeLoaderCallbacks.TryGetConstructedGenericTypeForComponents(openComparerType, new RuntimeTypeHandle[] { comparerTypeArgument }, out comparerType);
            if (!success)
            {
                Environment.FailFast("Unable to create comparer");
            }

            return RuntimeAugments.RawNewObject(comparerType);
        }

        // This one is an intrinsic that is used to make enum comparisons more efficient.
        [Intrinsic]
        internal static int EnumOnlyCompare<T>(T x, T y) where T : struct, Enum
        {
            return x.CompareTo(y);
        }
    }
}
