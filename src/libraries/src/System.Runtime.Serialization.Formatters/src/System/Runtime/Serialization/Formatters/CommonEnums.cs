// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization.Formatters
{
    public enum FormatterTypeStyle
    {
        TypesWhenNeeded = 0, // Types are outputted only for Arrays of Objects, Object Members of type Object, and ISerializable non-primitive value types
        TypesAlways = 0x1,   // Types are outputted for all Object members and ISerialiable object members.
        XsdString = 0x2      // Strings are outputed as xsd rather then SOAP-ENC strings. No string ID's are transmitted
    }

    public enum FormatterAssemblyStyle
    {
        Simple = 0,
        Full = 1,
    }

    public enum TypeFilterLevel
    {
        Low = 0x2,
        Full = 0x3
    }
}
