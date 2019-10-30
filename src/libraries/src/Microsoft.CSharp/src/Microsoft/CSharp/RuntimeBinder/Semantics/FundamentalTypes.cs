// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal enum FUNDTYPE
    {
        FT_NONE,         // No fundamental type
        FT_I1,
        FT_I2,
        FT_I4,
        FT_U1,
        FT_U2,
        FT_U4,
        FT_LASTNONLONG = FT_U4,   // Last one that fits in a int.
        FT_I8,
        FT_U8,           // integral types
        FT_LASTINTEGRAL = FT_U8,
        FT_R4,
        FT_R8,           // floating types
        FT_LASTNUMERIC = FT_R8,
        FT_REF,          // reference type
        FT_STRUCT,       // structure type
        FT_PTR,          // pointer to unmanaged memory
        FT_VAR,          // polymorphic, unbounded, not yet committed
        FT_COUNT        // number of enumerators.
    }
}
