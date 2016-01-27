// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
 **
 **
 **
 ** Purpose: Soap XML Formatter Enums
 **
 **
 ===========================================================*/

namespace System.Runtime.Serialization.Formatters {
    using System.Threading;
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;
    using System;
    // Enums which specify options to the XML and Binary formatters
    // These will be public so that applications can use them
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum FormatterTypeStyle
    {
        TypesWhenNeeded = 0, // Types are outputted only for Arrays of Objects, Object Members of type Object, and ISerializable non-primitive value types
        TypesAlways = 0x1, // Types are outputted for all Object members and ISerialiable object members.
        XsdString = 0x2     // Strings are outputed as xsd rather then SOAP-ENC strings. No string ID's are transmitted
    }

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum FormatterAssemblyStyle
    {
        Simple = 0,
        Full = 1,
    }

[System.Runtime.InteropServices.ComVisible(true)]
    public enum TypeFilterLevel {
        Low = 0x2,
        Full = 0x3
    }    
}
