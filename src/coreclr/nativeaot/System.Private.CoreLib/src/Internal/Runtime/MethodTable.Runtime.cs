// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Internal.Runtime
{
    // Extensions to MethodTable that are specific to the use in the CoreLib.
    internal unsafe partial struct MethodTable
    {
#pragma warning disable CA1822
        internal MethodTable* GetArrayEEType()
        {
            return MethodTable.Of<Array>();
        }

        internal Exception GetClasslibException(ExceptionIDs id)
        {
            return RuntimeExceptionHelpers.GetRuntimeException(id);
        }
#pragma warning restore CA1822

        internal static bool AreSameType(MethodTable* mt1, MethodTable* mt2)
        {
            return mt1 == mt2;
        }

        internal bool IsEnum
        {
            get
            {
                // Q: When is an enum type a constructed generic type?
                // A: When it's nested inside a generic type.

                // Generic type definitions that return true for IsPrimitive are type definitions of generic enums.
                // Otherwise check the base type.
                return IsPrimitive && (IsGenericTypeDefinition || NonArrayBaseType == MethodTable.Of<Enum>());
            }
        }

        // Returns true for actual primitives only, returns false for enums and void
        internal bool IsActualPrimitive
        {
            get
            {
                return (ElementType is > EETypeElementType.Void and < EETypeElementType.ValueType)
                    && NonArrayBaseType == MethodTable.Of<ValueType>();
            }
        }
    }
}
