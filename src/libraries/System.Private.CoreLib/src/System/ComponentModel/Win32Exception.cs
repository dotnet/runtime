// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
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
    public class Win32Exception : ExternalException
    {
        private const int E_FAIL = unchecked((int)0x80004005);

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.Win32Exception'/> class with the last Win32 error
        /// that occurred.
        /// </summary>
        public Win32Exception() : this(Marshal.GetLastPInvokeError())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.Win32Exception'/> class with the specified error.
        /// </summary>
        public Win32Exception(int error) : this(error, Marshal.GetPInvokeErrorMessage(error))
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
        public Win32Exception(string? message) : this(Marshal.GetLastPInvokeError(), message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Exception class with a specified error message and a
        /// reference to the inner exception that is the cause of this exception.
        /// </summary>
        public Win32Exception(string? message, Exception? innerException) : base(message, innerException)
        {
            NativeErrorCode = Marshal.GetLastPInvokeError();
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected Win32Exception(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            NativeErrorCode = info.GetInt32(nameof(NativeErrorCode));
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
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
                s.Append($" ({nativeErrorString})");
            }
            else
            {
                s.Append($" ({HResult:X8}, {nativeErrorString})");
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
