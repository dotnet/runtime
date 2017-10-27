// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if FEATURE_SERIALIZATION
using System.Runtime.Serialization;
#endif

namespace System.IO
{
    /// <summary>
    /// The FileFormatException class is thrown when an input file or a data stream that is supposed to conform
    /// to a certain file format specification is malformed.
    /// </summary>
#if FEATURE_SERIALIZATION
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
#endif
    public class FileFormatException : FormatException
    {
        /// <summary>
        /// Creates a new instance of FileFormatException class.
        /// This constructor initializes the Message property of the new instance to a system-supplied message that describes the error,
        /// such as "An input file or a data stream does not conform to the expected file format specification."
        /// This message takes into account the current system culture.
        /// </summary>
        public FileFormatException()
            : base(SR.FileFormatException)
        { }

        /// <summary>
        /// Creates a new instance of FileFormatException class.
        /// This constructor initializes the Message property of the new instance with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public FileFormatException(string message)
            : base(message)
        { }

        /// <summary>
        /// Creates a new instance of FileFormatException class.
        /// This constructor initializes the Message property of the new instance with a specified error message.
        /// The InnerException property is initialized using the innerException parameter.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public FileFormatException(string message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// Creates a new instance of FileFormatException class.
        /// This constructor initializes the Message property of the new instance to a system-supplied message that describes the error and includes the file name,
        /// such as "The file 'sourceUri' does not conform to the expected file format specification."
        /// This message takes into account the current system culture.
        /// The SourceUri property is initialized using the sourceUri parameter.
        /// </summary>
        /// <param name="sourceUri">The Uri of a file that caused this error.</param>
        public FileFormatException(Uri sourceUri)
            : base(
                sourceUri == null
                ? SR.FileFormatException
                : SR.Format(SR.FileFormatExceptionWithFileName, sourceUri))
        {
            _sourceUri = sourceUri;
        }

        /// <summary>
        /// Creates a new instance of FileFormatException class.
        /// This constructor initializes the Message property of the new instance using the message parameter.
        /// The content of message is intended to be understood by humans.
        /// The caller of this constructor is required to ensure that this string has been localized for the current system culture.
        /// The SourceUri property is initialized using the sourceUri parameter.
        /// </summary>
        /// <param name="sourceUri">The Uri of a file that caused this error.</param>
        /// <param name="message">The message that describes the error.</param>
        public FileFormatException(Uri sourceUri, string message)
            : base(message)
        {
            _sourceUri = sourceUri;
        }

        /// <summary>
        /// Creates a new instance of FileFormatException class.
        /// This constructor initializes the Message property of the new instance to a system-supplied message that describes the error and includes the file name,
        /// such as "The file 'sourceUri' does not conform to the expected file format specification."
        /// This message takes into account the current system culture.
        /// The SourceUri property is initialized using the sourceUri parameter.
        /// The InnerException property is initialized using the innerException parameter.
        /// </summary>
        /// <param name="sourceUri">The Uri of a file that caused this error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public FileFormatException(Uri sourceUri, Exception innerException)
            : base(
                sourceUri == null
                ? SR.FileFormatException
                : SR.Format(SR.FileFormatExceptionWithFileName, sourceUri),
                innerException)
        {
            _sourceUri = sourceUri;
        }

        /// <summary>
        /// Creates a new instance of FileFormatException class.
        /// This constructor initializes the Message property of the new instance using the message parameter.
        /// The content of message is intended to be understood by humans.
        /// The caller of this constructor is required to ensure that this string has been localized for the current system culture.
        /// The SourceUri property is initialized using the sourceUri parameter.
        /// The InnerException property is initialized using the innerException parameter.
        /// </summary>
        /// <param name="sourceUri">The Uri of a file that caused this error.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public FileFormatException(Uri sourceUri, string message, Exception innerException)
            : base(message, innerException)
        {
            _sourceUri = sourceUri;
        }

#if FEATURE_SERIALIZATION
        protected FileFormatException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            string sourceUriString = info.GetString("SourceUri");
            if (sourceUriString != null)
                _sourceUri = new Uri(sourceUriString, UriKind.RelativeOrAbsolute);
        }

        /// <summary>
        /// Sets the SerializationInfo object with the file name and additional exception information. 
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("SourceUri", SourceUri?.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped), typeof(string));
        }
#endif

        /// <summary>
        /// Returns the name of a file that caused this exception. This property may be equal to an empty string
        /// if obtaining the file path that caused the error was not possible.
        /// </summary>
        /// <value>The file name.</value>
        /// <SecurityNote>
        ///     Critical : Calls critical Demand for path discovery
        ///     Safe     : Path which could be leaked by an exception is already known to caller since it is supplied by the caller
        /// </SecurityNote>
        public Uri SourceUri
        {
            get
            {
                return _sourceUri;
            }
        }

        private readonly Uri _sourceUri;
    }
}
