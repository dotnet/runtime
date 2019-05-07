// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Text.Json
{
    /// <summary>
    /// Defines a custom exception object that is thrown when invalid JSON text is encountered, when the defined maximum depth is passed,
    /// or the JSON text is not compatible with the type of a property on an object.
    /// </summary>
    [Serializable]
    public class JsonException : Exception
    {
        /// <summary>
        /// Creates a new exception object to relay error information to the user.
        /// </summary>
        /// <param name="message">The context specific error message.</param>
        /// <param name="lineNumber">The line number at which the invalid JSON was encountered (starting at 0) when deserializing.</param>
        /// <param name="bytePositionInLine">The byte count within the current line where the invalid JSON was encountered (starting at 0).</param>
        /// <param name="path">The object's property path where the invalid JSON was encountered.</param>
        /// <param name="innerException">The exception that caused the current exception.</param>
        /// <remarks>
        /// Note that the <paramref name="bytePositionInLine"/> counts the number of bytes (i.e. UTF-8 code units) and not characters or scalars.
        /// </remarks>
        public JsonException(string message, string path, long? lineNumber, long? bytePositionInLine, Exception innerException) : base(message, innerException)
        {
            LineNumber = lineNumber;
            BytePositionInLine = bytePositionInLine;
            Path = path;
        }

        /// <summary>
        /// Creates a new exception object to relay error information to the user.
        /// </summary>
        /// <param name="message">The context specific error message.</param>
        /// <param name="path">The object's property path where the invalid JSON was encountered.</param>
        /// <param name="lineNumber">The line number at which the invalid JSON was encountered (starting at 0) when deserializing.</param>
        /// <param name="bytePositionInLine">The byte count within the current line where the invalid JSON was encountered (starting at 0).</param>
        /// <remarks>
        /// Note that the <paramref name="bytePositionInLine"/> counts the number of bytes (i.e. UTF-8 code units) and not characters or scalars.
        /// </remarks>
        public JsonException(string message, string path, long? lineNumber, long? bytePositionInLine) : base(message)
        {
            LineNumber = lineNumber;
            BytePositionInLine = bytePositionInLine;
            Path = path;
        }

        protected JsonException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            LineNumber = (long?)info.GetValue("LineNumber", typeof(long?));
            BytePositionInLine = (long?)info.GetValue("BytePositionInLine", typeof(long?));
            Path = info.GetString("Path");
        }

        /// <summary>
        ///  Sets the <see cref="SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("LineNumber", LineNumber, typeof(long?));
            info.AddValue("BytePositionInLine", BytePositionInLine, typeof(long?));
            info.AddValue("Path", Path, typeof(string));
        }

        /// <summary>
        /// The number of lines read so far before the exception (starting at 0).
        /// </summary>
        public long? LineNumber { get; private set; }

        /// <summary>
        /// The number of bytes read within the current line before the exception (starting at 0).
        /// </summary>
        public long? BytePositionInLine { get; private set; }

        /// <summary>
        /// The property path within the JSON where the exception was encountered.
        /// </summary>
        public string Path { get; private set; }
    }
}
