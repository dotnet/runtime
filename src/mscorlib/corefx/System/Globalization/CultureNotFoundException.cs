// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

// 

using System;
using System.Threading;
using System.Runtime.Serialization;

namespace System.Globalization
{
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public partial class CultureNotFoundException : ArgumentException, ISerializable
    {
        private string m_invalidCultureName; // unrecognized culture name

        public CultureNotFoundException()
            : base(DefaultMessage)
        {
        }

        public CultureNotFoundException(String message)
            : base(message)
        {
        }

        public CultureNotFoundException(String paramName, String message)
            : base(message, paramName)
        {
        }

        public CultureNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        public CultureNotFoundException(String paramName, string invalidCultureName, String message)
            : base(message, paramName)
        {
            m_invalidCultureName = invalidCultureName;
        }

        public CultureNotFoundException(String message, string invalidCultureName, Exception innerException)
            : base(message, innerException)
        {
            m_invalidCultureName = invalidCultureName;
        }

        protected CultureNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            m_invalidCultureName = (string)info.GetValue("InvalidCultureName", typeof(string));
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            base.GetObjectData(info, context);
            info.AddValue("InvalidCultureName", m_invalidCultureName, typeof(string));
        }

        public virtual string InvalidCultureName
        {
            get { return m_invalidCultureName; }
        }

        private static String DefaultMessage
        {
            get
            {
                return SR.Argument_CultureNotSupported;
            }
        }

        private String FormatedInvalidCultureId
        {
            get
            {
                return InvalidCultureName;
            }
        }

        public override String Message
        {
            get
            {
                String s = base.Message;
                if (
                    m_invalidCultureName != null)
                {
                    String valueMessage = SR.Format(SR.Argument_CultureInvalidIdentifier, FormatedInvalidCultureId);
                    if (s == null)
                        return valueMessage;
                    return s + Environment.NewLine + valueMessage;
                }
                return s;
            }
        }
    }
}
