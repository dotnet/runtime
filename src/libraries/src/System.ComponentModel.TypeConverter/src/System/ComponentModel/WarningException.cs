// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies an exception that is handled as a warning instead of an error.</para>
    /// </summary>
    [Serializable]
    public class WarningException : SystemException
    {
        private readonly string _helpUrl;
        private readonly string _helpTopic;

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.Win32Exception'/> class with the last Win32 error 
        ///    that occured.</para>
        /// </summary>
        public WarningException() : this(null, null, null)
        {
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.WarningException'/> class with 
        ///    the specified message and no Help file.</para>
        /// </summary>
        public WarningException(string message) : this(message, null, null)
        {
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.WarningException'/> class with 
        ///    the specified message, and with access to the specified Help file.</para>
        /// </summary>
        public WarningException(string message, string helpUrl) : this(message, helpUrl, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the Exception class with a specified error message and a 
        ///     reference to the inner exception that is the cause of this exception.
        ///     FxCop CA1032: Multiple constructors are required to correctly implement a custom exception.
        /// </summary>
        public WarningException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.WarningException'/> class with the 
        ///    specified message, and with access to the specified Help file and topic.</para>
        /// </summary>
        public WarningException(string message, string helpUrl, string helpTopic)
            : base(message)
        {
            _helpUrl = helpUrl;
            _helpTopic = helpTopic;
        }

        /// <summary>
        ///     Need this constructor since Exception implements ISerializable. 
        /// </summary>
        protected WarningException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _helpUrl = (string)info.GetValue("helpUrl", typeof(string));
            _helpTopic = (string)info.GetValue("helpTopic", typeof(string));
        }

        /// <summary>
        ///    <para> Specifies the Help file associated with the 
        ///       warning. This field is read-only.</para>
        /// </summary>
        public string HelpUrl
        {
            get
            {
                return _helpUrl;
            }
        }

        /// <summary>
        ///    <para> Specifies the 
        ///       Help topic associated with the warning. This field is read-only. </para>
        /// </summary>
        public string HelpTopic
        {
            get
            {
                return _helpTopic;
            }
        }

        /// <summary>
        ///     Need this since Exception implements ISerializable and we have fields to save out.
        /// </summary>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("helpUrl", _helpUrl);
            info.AddValue("helpTopic", _helpTopic);

            base.GetObjectData(info, context);
        }
    }
}
