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
        /// <summary>
        /// Field should be formatted as an HRESULT code. Typically applied to
        /// 32-bit integer types.
        /// </summary>
        HResult = 15,
    }
}
