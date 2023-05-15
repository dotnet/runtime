// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.IO
{
    /// <devdoc>
    ///    The exception that is thrown when the internal buffer overflows.
    /// </devdoc>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class InternalBufferOverflowException : SystemException
    {
        /// <devdoc>
        ///    Initializes a new default instance of the <see cref='System.IO.InternalBufferOverflowException'/> class.
        /// </devdoc>
        public InternalBufferOverflowException() : base()
        {
            HResult = HResults.InternalBufferOverflow;
        }

        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.IO.InternalBufferOverflowException'/> class with the error message to be displayed specified.
        /// </devdoc>
        public InternalBufferOverflowException(string? message) : base(message)
        {
            HResult = HResults.InternalBufferOverflow;
        }

        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.IO.InternalBufferOverflowException'/>
        ///    class with the message to be displayed and the generated inner exception specified.
        /// </devdoc>
        public InternalBufferOverflowException(string? message, Exception? inner) : base(message, inner)
        {
            HResult = HResults.InternalBufferOverflow;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected InternalBufferOverflowException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
