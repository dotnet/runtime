// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies an exception that is handled as a warning instead of an error.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class WarningException : SystemException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.WarningException'/> class with the last Win32 error
        /// that occurred.
        /// </summary>
        public WarningException() : this(null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.WarningException'/> class with
        /// the specified message and no Help file.
        /// </summary>
        public WarningException(string? message) : this(message, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.WarningException'/> class with
        /// the specified message, and with access to the specified Help file.
        /// </summary>
        public WarningException(string? message, string? helpUrl) : this(message, helpUrl, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Exception class with a specified error message and a
        /// reference to the inner exception that is the cause of this exception.
        /// FxCop CA1032: Multiple constructors are required to correctly implement a custom exception.
        /// </summary>
        public WarningException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.WarningException'/> class with the
        /// specified message, and with access to the specified Help file and topic.
        /// </summary>
        public WarningException(string? message, string? helpUrl, string? helpTopic) : base(message)
        {
            HelpUrl = helpUrl;
            HelpTopic = helpTopic;
        }

        /// <summary>
        /// Need this constructor since Exception implements ISerializable.
        /// </summary>
        protected WarningException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            HelpUrl = (string?)info.GetValue("helpUrl", typeof(string));
            HelpTopic = (string?)info.GetValue("helpTopic", typeof(string));
        }

        /// <summary>
        /// Specifies the Help file associated with the warning. This field is read-only.
        /// </summary>
        public string? HelpUrl { get; }

        /// <summary>
        /// Specifies the Help topic associated with the warning. This field is read-only.
        /// </summary>
        public string? HelpTopic { get; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("helpUrl", HelpUrl);
            info.AddValue("helpTopic", HelpTopic);
        }
    }
}
