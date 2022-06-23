// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The algoritm to choose the default equality comparer is duplicated in the IL compiler. The compiler will replace the code within
// EqualityComparer<T>.Create method with more specific implementation based on what sort of type is being compared where possible.
//
// In addition, there are a set of generic functions which are used by Array.IndexOf<T> to perform equality checking
// in a similar manner. Array.IndexOf<T> uses these functions instead of the EqualityComparer<T> infrastructure because constructing
// a full EqualityComparer<T> has substantial size costs due to Array.IndexOf<T> use within all arrays.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Internal.Runtime.Augments;

namespace Internal.IntrinsicSupport
{
    internal static class EqualityComparerHelpers
    {
        private static bool ImplementsIEquatable(RuntimeTypeHandle t)
        {
            EETypePtr objectType = t.ToEETypePtr();
            EETypePtr iequatableType = typeof(IEquatable<>).TypeHandle.ToEETypePtr();
            int interfaceCount = objectType.Interfaces.Count;
            for (int i = 0; i < interfaceCount; i++)
            {
                EETypePtr interfaceType = objectType.Interfaces[i];

                if (!interfaceType.IsGeneric)
                    continue;

                if (interfaceType.GenericDefinition == iequatableType)
                {
                    var instantiation = interfaceType.Instantiation;

                    if (instantiation.Length != 1)
                        continue;

                    if (instantiation[0] == objectType)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool IsEnum(RuntimeTypeHandle t)
        {
            return t.ToEETypePtr().IsEnum;
        }

        // this function utilizes the template type loader to generate new
        // EqualityComparer types on the fly
        internal static object GetComparer(RuntimeTypeHandle t)
        {
            RuntimeTypeHandle comparerType;
            RuntimeTypeHandle openComparerType = default(RuntimeTypeHandle);
            RuntimeTypeHandle comparerTypeArgument = default(RuntimeTypeHandle);

            if (RuntimeAugments.IsNullable(t))
            {
                RuntimeTypeHandle nullableType = RuntimeAugments.GetNullableType(t);
                openComparerType = typeof(NullableEqualityComparer<>).TypeHandle;
                comparerTypeArgument = nullableType;
            }
            if (IsEnum(t))
            {
                openComparerType = typeof(EnumEqualityComparer<>).TypeHandle;
                comparerTypeArgument = t;
            }

            if (openComparerType.Equals(default(RuntimeTypeHandle)))
            {
                if (ImplementsIEquatable(t))
                {
                    openComparerType = typeof(GenericEqualityComparer<>).TypeHandle;
                    comparerTypeArgument = t;
                }
                else
                {
                    openComparerType = typeof(ObjectEqualityComparer<>).TypeHandle;
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

        //-----------------------------------------------------------------------
        // Redirection target functions for redirecting behavior of Array.IndexOf
        //-----------------------------------------------------------------------

        // This one is an intrinsic that is used to make enum comparisions more efficient.
        [Intrinsic]
        internal static bool EnumOnlyEquals<T>(T x, T y) where T : struct
        {
            return x.Equals(y);
        }

        private static bool StructOnlyEqualsIEquatable<T>(T x, T y) where T : IEquatable<T>
        {
            return x.Equals(y);
        }

        private static bool StructOnlyEqualsNullable<T>(Nullable<T> x, Nullable<T> y) where T : struct, IEquatable<T>
        {
            if (x.HasValue)
            {
                if (y.HasValue)
                    return x.Value.Equals(y.Value);
                return false;
            }

            if (y.HasValue)
                return false;

            return true;
        }

        // These functions look odd, as they are part of a complex series of compiler intrinsics
        // designed to produce very high quality code for equality comparison cases without utilizing
        // reflection like other platforms. The major complication is that the specification of
        // IndexOf is that it is supposed to use IEquatable<T> if possible, but that requirement
        // cannot be expressed in IL directly due to the lack of constraints.
        // Instead, specialization at call time is used within the compiler.
        //
        // General Approach
        // - Perform fancy redirection for EqualityComparerHelpers.GetComparerForReferenceTypesOnly<T>(). If T is a reference
        //   type or UniversalCanon, have this redirect to EqualityComparer<T>.get_Default, Otherwise, use
        //   the function as is. (will return null in that case)
        // - Change the contents of the IndexOf functions to have a pair of loops. One for if
        //   GetComparerForReferenceTypesOnly returns null, and one for when it does not.
        //   - If it does not return null, call the EqualityComparer<T> code.
        //   - If it does return null, use a special function StructOnlyEquals<T>().
        //     - Calls to that function result in calls to a pair of helper function in
        //       EqualityComparerHelpers (StructOnlyEqualsIEquatable, or StructOnlyEqualsNullable)
        //       depending on whether or not they are the right function to call.
        // - The end result is that in optimized builds, we have the same single function compiled size
        //   characteristics that the old EqualsOnlyComparer<T>.Equals function had, but we maintain
        //   correctness as well.
        [Intrinsic]
        internal static EqualityComparer<T> GetComparerForReferenceTypesOnly<T>()
        {
            return EqualityComparer<T>.Default;
        }

        private static bool StructOnlyNormalEquals<T>(T left, T right)
            where T : notnull
        {
            return left.Equals(right);
        }

        [Intrinsic]
        internal static bool StructOnlyEquals<T>(T left, T right)
        {
            return EqualityComparer<T>.Default.Equals(left, right);
        }
    }
}
