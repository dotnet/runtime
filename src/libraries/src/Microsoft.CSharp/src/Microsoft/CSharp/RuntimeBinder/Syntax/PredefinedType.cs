// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Syntax
{
    internal enum PredefinedType : uint
    {
        PT_BYTE,
        PT_SHORT,
        PT_INT,
        PT_LONG,
        PT_FLOAT,
        PT_DOUBLE,
        PT_DECIMAL,
        PT_CHAR,
        PT_BOOL,

        // "simple" types are certain types that the compiler knows about for conversion and operator purposes.ses.
        // Keep these first so that we can build conversion tables on their ordinals... Don't change the orderer
        // of the simple types because it will mess up conversion tables.
        // The following Quasi-Simple types are considered simple, except they are non-CLS compliant
        PT_SBYTE,
        PT_USHORT,
        PT_UINT,
        PT_ULONG,

        // The special "pointer-sized int" types. Note that this are not considered numeric types from the compiler's point of view --
        // they are special only in that they have special signature encodings.
        FirstNonSimpleType,
        PT_INTPTR = FirstNonSimpleType,
        PT_UINTPTR,

        PT_OBJECT,

        // THE ORDER ABOVE HERE IS IMPORTANT!!!  It is used in tables in both fncbind and ilgen
        PT_STRING,
        PT_DELEGATE,
        PT_MULTIDEL,
        PT_ARRAY,
        PT_TYPE,
        PT_VALUE,
        PT_ENUM,
        PT_DATETIME,

        // predefined types for the BCL
        PT_IENUMERABLE,

        // Generic variants of enumerator interfaces
        PT_G_IENUMERABLE,

        // Nullable<T>
        PT_G_OPTIONAL,

        // LINQ
        PT_G_IQUERYABLE,
        PT_G_ICOLLECTION,
        PT_G_ILIST,
        PT_G_EXPRESSION,
        PT_EXPRESSION,
        PT_BINARYEXPRESSION,
        PT_UNARYEXPRESSION,
        PT_CONSTANTEXPRESSION,
        PT_PARAMETEREXPRESSION,
        PT_MEMBEREXPRESSION,
        PT_METHODCALLEXPRESSION,
        PT_NEWEXPRESSION,
        PT_NEWARRAYEXPRESSION,
        PT_INVOCATIONEXPRESSION,
        PT_FIELDINFO,
        PT_METHODINFO,
        PT_CONSTRUCTORINFO,
        PT_PROPERTYINFO,
        PT_MISSING,

        PT_G_IREADONLYLIST,
        PT_G_IREADONLYCOLLECTION,
        PT_FUNC,
        PT_COUNT,
        PT_VOID,             // (special case)

        PT_UNDEFINEDINDEX = 0xffffffff,
    }
}
