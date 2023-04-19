// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Diagnostics.CodeAnalysis;

using Debug = System.Diagnostics.Debug;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Array helpers for generated code.
    /// </summary>
    internal static class ArrayHelpers
    {
        /// <summary>
        /// Helper for array allocations via `newobj` IL instruction. Dimensions are passed in as block of integers.
        /// The content of the dimensions block may be modified by the helper.
        /// </summary>
        public static unsafe Array NewObjArray(IntPtr pEEType, int nDimensions, int* pDimensions)
        {
            EETypePtr eeType = new EETypePtr(pEEType);
            Debug.Assert(eeType.IsArray && !eeType.IsSzArray);
            Debug.Assert(nDimensions > 0);

            // Rank 1 arrays are handled below.
            Debug.Assert(eeType.ArrayRank > 1);

            // Multidimensional arrays have two ctors, one with and one without lower bounds
            int rank = eeType.ArrayRank;
            Debug.Assert(rank == nDimensions || 2 * rank == nDimensions);

            if (rank < nDimensions)
            {
                for (int i = 0; i < rank; i++)
                {
                    if (pDimensions[2 * i] != 0)
                        throw new PlatformNotSupportedException(SR.PlatformNotSupported_NonZeroLowerBound);

                    pDimensions[i] = pDimensions[2 * i + 1];
                }
            }

            return Array.NewMultiDimArray(eeType, pDimensions, rank);
        }

        /// <summary>
        /// Helper for array allocations via `newobj` IL instruction. Dimensions are passed in as block of integers.
        /// The content of the dimensions block may be modified by the helper.
        /// </summary>
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures that if we have a TypeHandle of a Rank-1 MdArray, we also generated the SzArray.")]
        public static unsafe Array NewObjArrayRare(IntPtr pEEType, int nDimensions, int* pDimensions)
        {
            EETypePtr eeType = new EETypePtr(pEEType);
            Debug.Assert(eeType.IsArray);
            Debug.Assert(nDimensions > 0);

            Debug.Assert(eeType.ArrayRank == 1);

            if (eeType.IsSzArray)
            {
                Array ret = RuntimeImports.RhNewArray(eeType, pDimensions[0]);

                if (nDimensions > 1)
                {
                    // Jagged arrays have constructor for each possible depth
                    EETypePtr elementType = eeType.ArrayElementType;
                    Debug.Assert(elementType.IsSzArray);

                    Array[] arrayOfArrays = (Array[])ret;
                    for (int i = 0; i < arrayOfArrays.Length; i++)
                        arrayOfArrays[i] = NewObjArrayRare(elementType.RawValue, nDimensions - 1, pDimensions + 1);
                }

                return ret;
            }
            else
            {
                // Multidimensional arrays have two ctors, one with and one without lower bounds
                const int rank = 1;
                Debug.Assert(rank == nDimensions || 2 * rank == nDimensions);

                if (rank < nDimensions)
                {
                    if (pDimensions[0] != 0)
                        throw new PlatformNotSupportedException(SR.PlatformNotSupported_NonZeroLowerBound);

                    pDimensions[0] = pDimensions[1];
                }

                // Multidimensional array of rank 1 with 0 lower bounds gets actually allocated
                // as an SzArray. SzArray is castable to MdArray rank 1.
                Type elementType = Type.GetTypeFromHandle(new RuntimeTypeHandle(eeType.ArrayElementType))!;
                return RuntimeImports.RhNewArray(elementType.MakeArrayType().TypeHandle.ToEETypePtr(), pDimensions[0]);
            }
        }
    }
}
