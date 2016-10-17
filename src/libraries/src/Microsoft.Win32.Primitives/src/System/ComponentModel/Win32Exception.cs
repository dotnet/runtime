// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>The exception that is thrown for a Win32 error code.</para>
    /// </devdoc>
    [Serializable]
    public partial class Win32Exception : ExternalException, ISerializable
    {
        /// <devdoc>
        ///    <para>Represents the Win32 error code associated with this exception. This 
        ///       field is read-only.</para>
        /// </devdoc>
        private readonly int nativeErrorCode;

        private const int E_FAIL = unchecked((int)0x80004005);

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.Win32Exception'/> class with the last Win32 error 
        ///    that occurred.</para>
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
        }

        protected Win32Exception(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            nativeErrorCode = info.GetInt32(nameof(NativeErrorCode));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(nameof(NativeErrorCode), nativeErrorCode);
            base.GetObjectData(info, context);
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
