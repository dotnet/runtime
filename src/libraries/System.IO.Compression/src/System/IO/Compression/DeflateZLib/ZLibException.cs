// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;

using ZErrorCode = System.IO.Compression.ZLibNative.ErrorCode;

namespace System.IO.Compression
{
    /// <summary>
    /// This is the exception that is thrown when a ZLib returns an error code indicating an unrecoverable error.
    /// </summary>
    internal partial class ZLibException : IOException
    {
        private string _zlibErrorContext = null;
        private string _zlibErrorMessage = null;
        private ZErrorCode _zlibErrorCode = ZErrorCode.Ok;



        /// <summary>
        /// This is the preferred constructor to use.
        /// The other constructors are provided for compliance to Fx design guidelines.
        /// </summary>
        /// <param name="message">A (localised) human readable error description.</param>
        /// <param name="zlibErrorContext">A description of the context within zlib where the error occurred (e.g. the function name).</param>
        /// <param name="zlibErrorCode">The error code returned by a ZLib function that caused this exception.</param>
        /// <param name="zlibErrorMessage">The string provided by ZLib as error information (unlocalised).</param>
        public ZLibException(string message, string zlibErrorContext, int zlibErrorCode, string zlibErrorMessage) :
            base(message)
        {
            Init(zlibErrorContext, (ZErrorCode)zlibErrorCode, zlibErrorMessage);
        }


        /// <summary>
        /// This constructor is provided in compliance with common NetFx design patterns;
        /// developers should prefer using the constructor
        /// <code>public ZLibException(string message, string zlibErrorContext, ZLibNative.ErrorCode zlibErrorCode, string zlibErrorMessage)</code>.
        /// </summary>    
        public ZLibException()
            : base()
        {
            Init();
        }


        /// <summary>
        /// This constructor is provided in compliance with common NetFx design patterns;
        /// developers should prefer using the constructor
        /// <code>public ZLibException(string message, string zlibErrorContext, ZLibNative.ErrorCode zlibErrorCode, string zlibErrorMessage)</code>.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ZLibException(string message)
            : base(message)
        {
            Init();
        }


        /// <summary>
        /// This constructor is provided in compliance with common NetFx design patterns;
        /// developers should prefer using the constructor
        /// <code>public ZLibException(string message, string zlibErrorContext, ZLibNative.ErrorCode zlibErrorCode, string zlibErrorMessage)</code>.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or a <code>null</code>.</param>
        public ZLibException(string message, Exception inner)
            : base(message, inner)
        {
            Init();
        }

        private void Init()
        {
            Init("", ZErrorCode.Ok, "");
        }

        private void Init(string zlibErrorContext, ZErrorCode zlibErrorCode, string zlibErrorMessage)
        {
            _zlibErrorContext = zlibErrorContext;
            _zlibErrorCode = zlibErrorCode;
            _zlibErrorMessage = zlibErrorMessage;
        }


        public string ZLibContext
        {
            [SecurityCritical]
            get { return _zlibErrorContext; }
        }

        public int ZLibErrorCode
        {
            [SecurityCritical]
            get { return (int)_zlibErrorCode; }
        }

        public string ZLibErrorMessage
        {
            [SecurityCritical]
            get { return _zlibErrorMessage; }
        }
    }
}
