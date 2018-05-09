// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Marker for types in .resources files
**
** 
===========================================================*/

namespace System.Resources
{
    /* An internal implementation detail for .resources files, describing
       what type an object is.  
       Ranges:
       0 - 0x1F     Primitives and reserved values
       0x20 - 0x3F  Specially recognized types, like byte[] and Streams

       Note this data must be included in any documentation describing the
       internals of .resources files.
    */
    internal enum ResourceTypeCode
    {
        // Primitives
        Null = 0,
        String = 1,
        Boolean = 2,
        Char = 3,
        Byte = 4,
        SByte = 5,
        Int16 = 6,
        UInt16 = 7,
        Int32 = 8,
        UInt32 = 9,
        Int64 = 0xa,
        UInt64 = 0xb,
        Single = 0xc,
        Double = 0xd,
        Decimal = 0xe,
        DateTime = 0xf,
        TimeSpan = 0x10,

        // A meta-value - change this if you add new primitives
        LastPrimitive = TimeSpan,

        // Types with a special representation, like byte[] and Stream
        ByteArray = 0x20,
        Stream = 0x21,

        // User types - serialized using the binary formatter.
        StartOfUserTypes = 0x40
    }
}
