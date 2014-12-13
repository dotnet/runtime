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
    public partial class Win32Exception : Exception
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
            HResult = Interop.E_FAIL;
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
            HResult = Interop.E_FAIL;
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
    }
}
