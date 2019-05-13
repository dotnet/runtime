// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Globalization
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class CultureNotFoundException : ArgumentException
    {
        private string? _invalidCultureName; // unrecognized culture name
        private int? _invalidCultureId;     // unrecognized culture Lcid

        public CultureNotFoundException()
            : base(DefaultMessage)
        {
        }

        public CultureNotFoundException(string? message)
            : base(message)
        {
        }

        public CultureNotFoundException(string? paramName, string? message)
            : base(message, paramName)
        {
        }

        public CultureNotFoundException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        public CultureNotFoundException(string? paramName, string? invalidCultureName, string? message)
            : base(message, paramName)
        {
            _invalidCultureName = invalidCultureName;
        }

        public CultureNotFoundException(string? message, string? invalidCultureName, Exception? innerException)
            : base(message, innerException)
        {
            _invalidCultureName = invalidCultureName;
        }

        public CultureNotFoundException(string? message, int invalidCultureId, Exception? innerException)
            : base(message, innerException)
        {
            _invalidCultureId = invalidCultureId;
        }

        public CultureNotFoundException(string? paramName, int invalidCultureId, string? message)
            : base(message, paramName)
        {
            _invalidCultureId = invalidCultureId;
        }

        protected CultureNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _invalidCultureId = (int?)info.GetValue("InvalidCultureId", typeof(int?));
            _invalidCultureName = (string?)info.GetValue("InvalidCultureName", typeof(string));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("InvalidCultureId", _invalidCultureId, typeof(int?));
            info.AddValue("InvalidCultureName", _invalidCultureName, typeof(string));
        }

        public virtual Nullable<int> InvalidCultureId
        {
            get { return _invalidCultureId; }
        }

        public virtual string? InvalidCultureName
        {
            get { return _invalidCultureName; }
        }

        private static string DefaultMessage
        {
            get
            {
                return SR.Argument_CultureNotSupported;
            }
        }

        private string? FormattedInvalidCultureId
        {
            get
            {
                return InvalidCultureId != null ?
                    string.Format(CultureInfo.InvariantCulture, "{0} (0x{0:x4})", (int)InvalidCultureId) :
                    InvalidCultureName;
            }
        }

        public override string Message
        {
            get
            {
                string s = base.Message;
                if (_invalidCultureId != null || _invalidCultureName != null)
                {
                    string valueMessage = SR.Format(SR.Argument_CultureInvalidIdentifier, FormattedInvalidCultureId);
                    if (s == null)
                    {
                        return valueMessage;
                    }

                    return s + Environment.NewLine + valueMessage;
                }
                return s;
            }
        }
    }
}
