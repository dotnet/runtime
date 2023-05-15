// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// The exception thrown when an attempt is made to edit a file that is checked into
    /// a source control program.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class CheckoutException : ExternalException
    {
        private const int E_ABORT = unchecked((int)0x80004004);
        /// <summary>
        /// Initializes a <see cref='System.ComponentModel.Design.CheckoutException'/> that specifies that the checkout
        /// was canceled. This field is read-only.
        /// </summary>
        public static readonly CheckoutException Canceled = new CheckoutException(SR.CHECKOUTCanceled, E_ABORT);

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.Design.CheckoutException'/> class with
        /// no associated message or error code.
        /// </summary>
        public CheckoutException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.Design.CheckoutException'/>
        /// class with the specified message.
        /// </summary>
        public CheckoutException(string? message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.Design.CheckoutException'/>
        /// class with the specified message and error code.
        /// </summary>
        public CheckoutException(string? message, int errorCode) : base(message, errorCode)
        {
        }

        /// <summary>
        /// Need this constructor since Exception implements ISerializable. We don't have any fields,
        /// so just forward this to base.
        /// </summary>
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected CheckoutException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Exception class with a specified error message and a
        /// reference to the inner exception that is the cause of this exception.
        /// FxCop CA1032: Multiple constructors are required to correctly implement a custom exception.
        /// </summary>
        public CheckoutException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
