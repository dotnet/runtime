// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Takes a string and applies a formatting to it to transform
        /// parameters to input values (such as replacing %s in the string with a variable)
        /// </summary>
        /// <param name="str">The output buffer to put the transformed data into</param>
        /// <param name="size">The size of the output buffer</param>
        /// <param name="format">The input string with wildcards to be replaced</param>
        /// <param name="arg1">The argument to replace a wildcard with</param>
        /// <remarks>
        /// Since snprintf has a variadic parameter, which cannot be described by C#, we have different
        /// PInvokes declared for each one we need. The PInvoke layer will take care of putting the
        /// arguments in correctly.
        /// </remarks>
        /// <returns>
        /// Returns the number of characters (excluding null terminator) written to the buffer on
        /// success; if the return value is equal to the size then the result may have been truncated.
        /// On failure, returns a negative value.
        /// </returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SNPrintF_1S", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static unsafe partial int SNPrintF(byte* str, int size, string format, string arg1);

        /// <summary>
        /// Takes a string and applies a formatting to it to transform
        /// parameters to input values (such as replacing %s in the string with a variable)
        /// </summary>
        /// <param name="str">The output buffer to put the transformed data into</param>
        /// <param name="size">The size of the output buffer</param>
        /// <param name="format">The input string with wildcards to be replaced</param>
        /// <param name="arg1">The argument to replace a wildcard with</param>
        /// <remarks>
        /// Since snprintf has a variadic parameter, which cannot be described by C#, we have different
        /// PInvokes declared for each one we need. The PInvoke layer will take care of putting the
        /// arguments in correctly.
        /// </remarks>
        /// <returns>
        /// Returns the number of characters (excluding null terminator) written to the buffer on
        /// success; if the return value is equal to the size then the result may have been truncated.
        /// On failure, returns a negative value.
        /// </returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SNPrintF_1I", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static unsafe partial int SNPrintF(byte* str, int size, string format, int arg1);
    }
}
