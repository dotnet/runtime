// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    // The CLR data marshaler has some behaviors that are incompatible with
    // C++. Specifically, C++ treats boolean variables as byte size, whereas 
    // the marshaller treats them as 4-byte size.  Similarly, C++ treats
    // wchar_t variables as 4-byte size, whereas the marshaller treats them
    // as single byte size under certain conditions.  In order to work around
    // such issues, the C++ compiler will emit a type that the marshaller will
    // marshal using the correct sizes.  In addition, the compiler will place
    // this modopt onto the variables to indicate that the specified type is
    // not the true type.  Any compiler that needed to deal with similar
    // marshalling incompatibilities could use this attribute as well.
    //
    // Indicates that the modified instance differs from its true type for
    // correct marshalling.
    public static class CompilerMarshalOverride
    {
    }
}
