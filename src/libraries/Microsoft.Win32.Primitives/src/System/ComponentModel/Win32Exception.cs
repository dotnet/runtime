// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.ComponentModel
{
    /// <summary>
    /// The exception that is thrown for a Win32 error code.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class Win32Exception : ExternalException, ISerializable
    {
        private const int E_FAIL = unchecked((int)0x80004005);

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.Win32Exception'/> class with the last Win32 error
        /// that occurred.
        /// </summary>
        public Win32Exception() : this(Marshal.GetLastWin32Error())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.Win32Exception'/> class with the specified error.
        /// </summary>
        public Win32Exception(int error) : this(error, GetErrorMessage(error))
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.Win32Exception'/> class with the specified error and the
        /// specified detailed description.
        /// </summary>
        public Win32Exception(int error, string? message) : base(message)
        {
            NativeErrorCode = error;
        }

        /// <summary>
        /// Initializes a new instance of the Exception class with a specified error message.
        /// </summary>
        public Win32Exception(string? message) : this(Marshal.GetLastWin32Error(), message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Exception class with a specified error message and a
        /// reference to the inner exception that is the cause of this exception.
        /// </summary>
        public Win32Exception(string? message, Exception? innerException) : base(message, innerException)
        {
            NativeErrorCode = Marshal.GetLastWin32Error();
        }

        protected Win32Exception(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            NativeErrorCode = info.GetInt32(nameof(NativeErrorCode));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(NativeErrorCode), NativeErrorCode);
        }

        /// <summary>
        /// Represents the Win32 error code associated with this exception. This field is read-only.
        /// </summary>
        public int NativeErrorCode { get; }

        /// <summary>
        /// Returns a string that contains the <see cref="NativeErrorCode"/>, or <see cref="Exception.HResult"/>, or both.
        /// </summary>
        /// <returns>A string that represents the <see cref="NativeErrorCode"/>, or <see cref="Exception.HResult"/>, or both.</returns>
        public override string ToString()
        {
            if (NativeErrorCode == 0 || NativeErrorCode == HResult)
            {
                return base.ToString();
            }

            string message = Message;
            string className = GetType().ToString();
            StringBuilder s = new StringBuilder(className);
            string nativeErrorString = NativeErrorCode < 0
                ? $"0x{NativeErrorCode:X8}"
                : NativeErrorCode.ToString(CultureInfo.InvariantCulture);
            if (HResult == E_FAIL)
            {
                s.AppendFormat(CultureInfo.InvariantCulture, " ({0})", nativeErrorString);
            }
            else
            {
                s.AppendFormat(CultureInfo.InvariantCulture, " ({0:X8}, {1})", HResult, nativeErrorString);
            }

            if (!(string.IsNullOrEmpty(message)))
            {
                s.Append(": ");
                s.Append(message);
            }

            Exception? innerException = InnerException;
            if (innerException != null)
            {
                s.Append(" ---> ");
                s.Append(innerException.ToString());
            }

            string? stackTrace = StackTrace;
            if (stackTrace != null)
            {
                s.AppendLine();
                s.Append(stackTrace);
            }

            return s.ToString();
        }
    }
}
