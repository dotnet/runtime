// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Runtime.InteropServices;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>The exception that is thrown for a Win32 error code.</para>
    /// </devdoc>
    public class Win32Exception : Exception
    {
        /// <devdoc>
        ///    <para>Represents the Win32 error code associated with this exception. This 
        ///       field is read-only.</para>
        /// </devdoc>
        private readonly int nativeErrorCode;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.Win32Exception'/> class with the last Win32 error 
        ///    that occured.</para>
        /// </devdoc>
        public Win32Exception() : this(Marshal.GetLastWin32Error())
        {
        }
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.Win32Exception'/> class with the specified error.</para>
        /// </devdoc>
        public Win32Exception(int error) : this(error, GetErrorMessage(error))
        {
        }
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.Win32Exception'/> class with the specified error and the 
        ///    specified detailed description.</para>
        /// </devdoc>
        public Win32Exception(int error, string message)
        : base(message)
        {
            nativeErrorCode = error;
            // Win32Exception is not inheriting from ExternalException anymore, 
            // so we set the HResult manually to have the exact behavior we used to have when inherited from ExternalException
            HResult = Interop.mincore.E_FAIL;
        }

        /// <devdoc>
        ///     Initializes a new instance of the Exception class with a specified error message.
        ///     FxCop CA1032: Multiple constructors are required to correctly implement a custom exception.
        /// </devdoc>
        public Win32Exception(string message) : this(Marshal.GetLastWin32Error(), message)
        {
        }

        /// <devdoc>
        ///     Initializes a new instance of the Exception class with a specified error message and a 
        ///     reference to the inner exception that is the cause of this exception.
        ///     FxCop CA1032: Multiple constructors are required to correctly implement a custom exception.
        /// </devdoc>
        public Win32Exception(string message, Exception innerException) : base(message, innerException)
        {
            nativeErrorCode = Marshal.GetLastWin32Error();
            // Win32Exception is not inheriting from ExternalException anymore, 
            // so we set the HResult manually to have the exact behavior we used to have when inherited from ExternalException
            HResult = Interop.mincore.E_FAIL;
        }

        /// <devdoc>
        ///    <para>Represents the Win32 error code associated with this exception. This 
        ///       field is read-only.</para>
        /// </devdoc>
        public int NativeErrorCode
        {
            get
            {
                return nativeErrorCode;
            }
        }

        private static bool TryGetErrorMessage(int error, StringBuilder sb, out string errorMsg) 
        {
            errorMsg = "";

            int result = Interop.mincore.FormatMessage(
                                        Interop.mincore.FORMAT_MESSAGE_IGNORE_INSERTS |
                                        Interop.mincore.FORMAT_MESSAGE_FROM_SYSTEM |
                                        Interop.mincore.FORMAT_MESSAGE_ARGUMENT_ARRAY,
                                        IntPtr.Zero, (uint)error, 0, sb, sb.Capacity + 1,
                                        null);
            if (result != 0)
            {
                int i = sb.Length;
                while (i > 0)
                {
                    char ch = sb[i - 1];
                    if (ch > 32 && ch != '.') break;
                    i--;
                }
                errorMsg = sb.ToString(0, i);
            }
            else if (Marshal.GetLastWin32Error() == Interop.mincore.ERROR_INSUFFICIENT_BUFFER) 
            {
                return false;
            }
            else {
                errorMsg = String.Format("Unknown error (0x{0:x})", error);
            }

            return true;
        }

        // Windows API FormatMessage lets you format a message string given an errocode.
        // Unlike other APIs this API does not support a way to query it for the total message size.
        //
        // So the API can only be used in one of these two ways.
        // a. You pass a buffer of appropriate size and get the resource.
        // b. Windows creates a buffer and passes the address back and the onus of releasing the bugffer lies on the caller.
        //
        // Since the error code is coming from the user, it is not possible to know the size in advance.
        // Unfortunately we can't use option b. since the buffer can only be freed using LocalFree and it is a private API on onecore.
        // Also, using option b is ugly for the manged code and could cause memory leak in situations where freeing is unsuccessful.
        // 
        // As a result we use the following approach.
        // We initially call the API with a buffer size of 256 and then gradually increase the size in case of failure until we reach the maxiumum allowed limit of 65K.
        private const int MaxAllowedBufferSize = 65 * 1024;

        private static string GetErrorMessage(int error) 
        {
            string errorMsg;

            StringBuilder sb = new StringBuilder(256);
            do 
            {
                if (TryGetErrorMessage(error, sb, out errorMsg))
                    return errorMsg;
                else 
                {
                    // increase the capacity of the StringBuilder by 4.
                    sb.Capacity *= 4;
                }
            }
            while (sb.Capacity < MaxAllowedBufferSize);

            // If you come here then a size as large as 65K is also not sufficient and so we give the generic errorMsg.
            return String.Format("Unknown error (0x{0:x})", error);
        }
    }
}
