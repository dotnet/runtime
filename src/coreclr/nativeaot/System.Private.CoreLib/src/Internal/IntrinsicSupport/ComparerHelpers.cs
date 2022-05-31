// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The algoritm to choose the default comparer is duplicated in the IL compiler. The compiler will replace the code within
// Comparer<T>.Create method with more specific implementation based on what sort of type is being compared where possible.

using System;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.CompilerServices;

using Internal.Runtime.Augments;

namespace Internal.IntrinsicSupport
{
    internal static class ComparerHelpers
    {
        private static bool ImplementsIComparable(RuntimeTypeHandle t)
        {
            EETypePtr objectType = t.ToEETypePtr();
            EETypePtr icomparableType = typeof(IComparable<>).TypeHandle.ToEETypePtr();
            int interfaceCount = objectType.Interfaces.Count;
            for (int i = 0; i < interfaceCount; i++)
            {
                EETypePtr interfaceType = objectType.Interfaces[i];

                if (!interfaceType.IsGeneric)
                    continue;

                if (interfaceType.GenericDefinition == icomparableType)
                {
                    var instantiation = interfaceType.Instantiation;
                    if (instantiation.Length != 1)
                        continue;

                    if (objectType.IsValueType)
                    {
                        if (instantiation[0] == objectType)
                        {
                            return true;
                        }
                    }
                    else if (RuntimeImports.AreTypesAssignable(objectType, instantiation[0]))
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

            return RuntimeAugments.NewObject(comparerType);
        }

        // This one is an intrinsic that is used to make enum comparisions more efficient.
        [Intrinsic]
        internal static int EnumOnlyCompare<T>(T x, T y) where T : struct, Enum
        {
            return x.CompareTo(y);
        }
    }
}
