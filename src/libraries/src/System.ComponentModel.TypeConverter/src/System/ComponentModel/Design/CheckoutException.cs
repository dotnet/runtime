// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>
    ///       The exception thrown when an attempt is made to edit a file that is checked into
    ///       a source control program.
    ///    </para>
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class CheckoutException : ExternalException
    {
        private const int E_ABORT = unchecked((int)0x80004004);
        /// <summary>
        ///    <para>
        ///       Initializes a <see cref='System.ComponentModel.Design.CheckoutException'/> that specifies that the checkout
        ///       was
        ///       canceled. This field is read-only.
        ///    </para>
        /// </summary>
        public static readonly CheckoutException Canceled = new CheckoutException(SR.CHECKOUTCanceled, E_ABORT);

        /// <summary>
        ///    <para>
        ///       Initializes
        ///       a new instance of the <see cref='System.ComponentModel.Design.CheckoutException'/> class with no
        ///       associated message or
        ///       error code.
        ///    </para>
        /// </summary>
        public CheckoutException()
        {
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.CheckoutException'/>
        ///       class with the specified message.
        ///    </para>
        /// </summary>
        public CheckoutException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.CheckoutException'/>
        ///       class with the specified message and error code.
        ///    </para>
        /// </summary>
        public CheckoutException(string message, int errorCode)
            : base(message, errorCode)
        {
        }

        /// <summary>
        ///     Need this constructor since Exception implements ISerializable. We don't have any fields,
        ///     so just forward this to base.
        /// </summary>
        protected CheckoutException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the Exception class with a specified error message and a 
        ///     reference to the inner exception that is the cause of this exception.
        ///     FxCop CA1032: Multiple constructors are required to correctly implement a custom exception.
        /// </summary>
        public CheckoutException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
