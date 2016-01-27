// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

//
#ifndef _WELLKNOWNTYPES_H
#define _WELLKNOWNTYPES_H

enum WellKnownType
{
    WKT_FIRST,

    WKT_OBJECT = WKT_FIRST,
    WKT_STRING,
    WKT_VALUETYPE,
    WKT_ENUM,
    WKT_ARRAY,

    WKT_FIRST_PRIMITIVE,

    WKT_BOOLEAN = WKT_FIRST_PRIMITIVE,
#ifndef REDHAWK
    WKT_VOID,
#endif
    WKT_CHAR,
    WKT_I1,
    WKT_U1,
    WKT_I2,
    WKT_U2,
    WKT_I4,
    WKT_U4,
    WKT_I8,
    WKT_U8,
    WKT_R4,
    WKT_R8,
    WKT_I,
    WKT_U,

    WKT_LAST_PRIMITIVE = WKT_U,

#ifndef REDHAWK
    WKT_MARSHALBYREFOBJECT,
    WKT_MULTICASTDELEGATE,
    WKT_NULLABLE,
    WKT_CANON,
    WKT_TRANSPARENTPROXY,
    WKT_COMOBJECT,
    WKT_CONTEXTBOUNDOBJECT,
    WKT_DECIMAL,
    WKT_TYPEDREFERENCE,
    WKT_WINDOWS_RUNTIME_OBJECT,
#endif

    WKT_COUNT,
};
#endif
