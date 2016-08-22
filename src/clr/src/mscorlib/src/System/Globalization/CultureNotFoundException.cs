// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Globalization {

    using System;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Diagnostics.Contracts;
    
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public partial class CultureNotFoundException : ArgumentException, ISerializable
    {
        private string          m_invalidCultureName; // unrecognized culture name
#if !FEATURE_CORECLR
        private Nullable<int>   m_invalidCultureId;   // unrecognized culture Lcid
#endif //!FEATURE_CORECLR

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
#if !FEATURE_CORECLR
        public CultureNotFoundException(String paramName, int invalidCultureId, String message)
            : base(message, paramName)
        {
            m_invalidCultureId = invalidCultureId;
        }

        public CultureNotFoundException(String message, int invalidCultureId, Exception innerException)
            : base(message, innerException)
        {
            m_invalidCultureId = invalidCultureId;
        }
#endif //!FEATURE_CORECLR

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

        protected CultureNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) {
#if !FEATURE_CORECLR
            m_invalidCultureId      = (Nullable<int>) info.GetValue("InvalidCultureId", typeof(Nullable<int>));
#endif //!FEATURE_CORECLR
            m_invalidCultureName    = (string) info.GetValue("InvalidCultureName", typeof(string));
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            if (info==null) {
                throw new ArgumentNullException("info");
            }
            Contract.EndContractBlock();
            base.GetObjectData(info, context);
            Nullable<int> invalidCultureId = null;
#if !FEATURE_CORECLR
            invalidCultureId = m_invalidCultureId;
#endif //!FEATURE_CORECLR
            info.AddValue("InvalidCultureId", invalidCultureId, typeof(Nullable<int>));
            info.AddValue("InvalidCultureName", m_invalidCultureName, typeof(string));
        }
#if !FEATURE_CORECLR
        public virtual Nullable<int> InvalidCultureId
        {
            get { return m_invalidCultureId; }
        }
#endif //!FEATURE_CORECLR

        public virtual string InvalidCultureName
        {
            get { return m_invalidCultureName; }
        }

        private static String DefaultMessage
        {
            get 
            {
                return Environment.GetResourceString("Argument_CultureNotSupported");
            }
        }
        
        private String FormatedInvalidCultureId 
        {
            get
            {
#if !FEATURE_CORECLR
                if (InvalidCultureId != null)
                {
                    return String.Format(CultureInfo.InvariantCulture,
                                        "{0} (0x{0:x4})", (int)InvalidCultureId);
                }
#endif //!FEATURE_CORECLR
                return InvalidCultureName;
            }
        }

        public override String Message 
        {
            get 
            {
                String s = base.Message;
                if (
#if !FEATURE_CORECLR
                    m_invalidCultureId != null || 
#endif //!FEATURE_CORECLR
                    m_invalidCultureName != null) 
                {
                    String valueMessage = Environment.GetResourceString("Argument_CultureInvalidIdentifier", FormatedInvalidCultureId);
                    if (s == null)
                        return valueMessage;
                    return s + Environment.NewLine + valueMessage; 
                }
                return s;
            }
        }

    }
}
