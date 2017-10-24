// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// Provides a hint that may be used by an event listener when formatting
    /// an event field for display. Note that the event listener may ignore the
    /// hint if it does not recognize a particular combination of type and format.
    /// Similar to TDH_OUTTYPE.
    /// </summary>
    public enum EventFieldFormat
    {
        /// <summary>
        /// Field receives default formatting based on the field's underlying type.
        /// </summary>
        Default = 0,
#if false 
        /// <summary>
        /// Field should not be displayed.
        /// </summary>
        NoPrint = 1,
#endif
        /// <summary>
        /// Field should be formatted as character or string data.
        /// Typically applied to 8-bit or 16-bit integers.
        /// This is the default format for String and Char types.
        /// </summary>
        String = 2,

        /// <summary>
        /// Field should be formatted as boolean data. Typically applied to 8-bit
        /// or 32-bit integers. This is the default format for the Boolean type.
        /// </summary>
        Boolean = 3,

        /// <summary>
        /// Field should be formatted as hexadecimal data. Typically applied to
        /// integer types.
        /// </summary>
        Hexadecimal = 4,

#if false 
        /// <summary>
        /// Field should be formatted as a process identifier. Typically applied to
        /// 32-bit integer types.
        /// </summary>
        ProcessId = 5,

        /// <summary>
        /// Field should be formatted as a thread identifier. Typically applied to
        /// 32-bit integer types.
        /// </summary>
        ThreadId = 6,

        /// <summary>
        /// Field should be formatted as an Internet port. Typically applied to 16-bit integer
        /// types.
        /// </summary>
        Port = 7,
        /// <summary>
        /// Field should be formatted as an Internet Protocol v4 address. Typically applied to
        /// 32-bit integer types.
        /// </summary>
        Ipv4Address = 8,

        /// <summary>
        /// Field should be formatted as an Internet Protocol v6 address. Typically applied to
        /// byte[] types.
        /// </summary>
        Ipv6Address = 9,
        /// <summary>
        /// Field should be formatted as a SOCKADDR. Typically applied to byte[] types.
        /// </summary>
        SocketAddress = 10,
#endif
        /// <summary>
        /// Field should be formatted as XML string data. Typically applied to
        /// strings or arrays of 8-bit or 16-bit integers.
        /// </summary>
        Xml = 11,

        /// <summary>
        /// Field should be formatted as JSON string data. Typically applied to
        /// strings or arrays of 8-bit or 16-bit integers.
        /// </summary>
        Json = 12,
#if false 
        /// <summary>
        /// Field should be formatted as a Win32 error code. Typically applied to
        /// 32-bit integer types.
        /// </summary>
        Win32Error = 13,

        /// <summary>
        /// Field should be formatted as an NTSTATUS code. Typically applied to
        /// 32-bit integer types.
        /// </summary>
        NTStatus = 14,
#endif
        /// <summary>
        /// Field should be formatted as an HRESULT code. Typically applied to
        /// 32-bit integer types.
        /// </summary>
        HResult = 15,
#if false 
        /// <summary>
        /// Field should be formatted as a FILETIME. Typically applied to 64-bit
        /// integer types. This is the default format for DateTime types.
        /// </summary>
        FileTime = 16,
        /// <summary>
        /// When applied to a numeric type, indicates that the type should be formatted
        /// as a signed integer. This is the default format for signed integer types.
        /// </summary>
        Signed = 17,

        /// <summary>
        /// When applied to a numeric type, indicates that the type should be formatted
        /// as an unsigned integer. This is the default format for unsigned integer types.
        /// </summary>
        Unsigned = 18,
#endif
    }
}
