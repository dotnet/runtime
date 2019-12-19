// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Runtime.CompilerServices
{
    internal static unsafe class CastHelpers
    {
#pragma warning disable CA1823, 169 // this field is not used by managed code, yet
        private static int[]? s_table;
#pragma warning restore CA1823, 169

        private enum CastResult
        {
            CanCast = 1,
            CannotCast = 2,
            MaybeCast = 3
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object JITutil_IsInstanceOfAny_NoCacheLookup(void* toTypeHnd, object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object JITutil_ChkCastAny_NoCacheLookup(void* toTypeHnd, object obj);

        private static CastResult ObjIsInstanceOfCached(object obj, void* toTypeHnd)
        {
            // TODO: WIP cache lookup here.
            return CastResult.MaybeCast;
        }

        // IsInstanceOf test used for unusual cases (naked type parameters, variant generic types)
        // Unlike the IsInstanceOfInterface, IsInstanceOfClass, and IsIsntanceofArray functions,
        // this test must deal with all kinds of type tests
        private static object? JIT_IsInstanceOfAny(void* toTypeHnd, object? obj)
        {
            if (obj == null)
            {
                return obj;
            }

            switch (ObjIsInstanceOfCached(obj, toTypeHnd))
            {
                case CastResult.CanCast:
                    return obj;
                case CastResult.CannotCast:
                    return null;
                default:
                    // fall through to the slow helper
                    break;
            }

            return JITutil_IsInstanceOfAny_NoCacheLookup(toTypeHnd, obj);
        }

        // ChkCast test used for unusual cases (naked type parameters, variant generic types)
        // Unlike the ChkCastInterface, ChkCastClass, and ChkCastArray functions,
        // this test must deal with all kinds of type tests
        private static object? JIT_ChkCastAny(void* toTypeHnd, object? obj)
        {
            if (obj == null)
            {
                return obj;
            }

            CastResult result = ObjIsInstanceOfCached(obj, toTypeHnd);
            if (result == CastResult.CanCast)
            {
                return obj;
            }

            object objRet = JITutil_ChkCastAny_NoCacheLookup(toTypeHnd, obj);
            // Make sure that the fast helper have not lied
            Debug.Assert(result != CastResult.CannotCast);
            return objRet;
        }
    }
}
