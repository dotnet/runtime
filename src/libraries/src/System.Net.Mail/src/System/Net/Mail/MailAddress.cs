// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Net.Mime;
using System.Text;

namespace System.Net.Mail
{
    //
    // This class stores the basic components of an e-mail address as described in RFC 2822 Section 3.4.
    // Any parsing required is done with the MailAddressParser class.
    //
    public partial class MailAddress
    {
        // These components form an e-mail address when assembled as follows:
        // "EncodedDisplayname" <userName@host>
        private readonly Encoding _displayNameEncoding;
        private readonly string _displayName;
        private readonly string _userName;
        private readonly string _host;

        // For internal use only by MailAddressParser.
        // The components were already validated before this is called.
        internal MailAddress(string displayName, string userName, string domain)
        {
            _host = domain;
            _userName = userName;
            _displayName = displayName;
            _displayNameEncoding = Encoding.GetEncoding(MimeBasePart.DefaultCharSet);

            Debug.Assert(_host != null,
                "host was null in internal constructor");

            Debug.Assert(userName != null,
                "userName was null in internal constructor");

            Debug.Assert(displayName != null,
                "displayName was null in internal constructor");
        }

        public MailAddress(string address) : this(address, null, (Encoding)null)
        {
        }

        public MailAddress(string address, string displayName) : this(address, displayName, (Encoding)null)
        {
        }

        //
        // This constructor validates and stores the components of an e-mail address.   
        // 
        // Preconditions:
        // - 'address' must not be null or empty.
        // 
        // Postconditions:
        // - The e-mail address components from the given 'address' are parsed, which should be formatted as:
        // "EncodedDisplayname" <username@host>
        // - If a 'displayName' is provided separately, it overrides whatever display name is parsed from the 'address'
        // field.  The display name does not need to be pre-encoded if a 'displayNameEncoding' is provided.
        //
        // A FormatException will be thrown if any of the components in 'address' are invalid.
        public MailAddress(string address, string displayName, Encoding displayNameEncoding)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }
            if (address == string.Empty)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(address)), nameof(address));
            }

            _displayNameEncoding = displayNameEncoding ?? Encoding.GetEncoding(MimeBasePart.DefaultCharSet);
            _displayName = displayName ?? string.Empty;

            // Check for bounding quotes
            if (!string.IsNullOrEmpty(_displayName))
            {
                _displayName = MailAddressParser.NormalizeOrThrow(_displayName);

                if (_displayName.Length >= 2 && _displayName[0] == '\"'
                    && _displayName[_displayName.Length - 1] == '\"')
                {
                    // Peal bounding quotes, they'll get re-added later.
                    _displayName = _displayName.Substring(1, _displayName.Length - 2);
                }
            }

            MailAddress result = MailAddressParser.ParseAddress(address);

            _host = result._host;
            _userName = result._userName;

            // If we were not given a display name, use the one parsed from 'address'.
            if (string.IsNullOrEmpty(_displayName))
            {
                _displayName = result._displayName;
            }
        }

        public string DisplayName
        {
            get
            {
                return _displayName;
            }
        }

        public string User
        {
            get
            {
                return _userName;
            }
        }

        private string GetUser(bool allowUnicode)
        {
            // Unicode usernames cannot be downgraded
            if (!allowUnicode && !MimeBasePart.IsAscii(_userName, true))
            {
                throw new SmtpException(SR.Format(SR.SmtpNonAsciiUserNotSupported, Address));
            }
            return _userName;
        }

        public string Host
        {
            get
            {
                return _host;
            }
        }

        private string GetHost(bool allowUnicode)
        {
            string domain = _host;

            // Downgrade Unicode domain names
            if (!allowUnicode && !MimeBasePart.IsAscii(domain, true))
            {
                IdnMapping mapping = new IdnMapping();
                try
                {
                    domain = mapping.GetAscii(domain);
                }
                catch (ArgumentException argEx)
                {
                    throw new SmtpException(SR.Format(SR.SmtpInvalidHostName, Address), argEx);
                }
            }
            return domain;
        }

        public string Address
        {
            get
            {
                return _userName + "@" + _host;
            }
        }

        private string GetAddress(bool allowUnicode)
        {
            return GetUser(allowUnicode) + "@" + GetHost(allowUnicode);
        }

        private string SmtpAddress
        {
            get
            {
                return "<" + Address + ">";
            }
        }

        internal string GetSmtpAddress(bool allowUnicode)
        {
            return "<" + GetAddress(allowUnicode) + ">";
        }

        /// <summary>
        /// this returns the full address with quoted display name.
        /// i.e. "some email address display name" <user@host>
        /// if displayname is not provided then this returns only user@host (no angle brackets)
        /// </summary>
        public override string ToString()
        {
            if (string.IsNullOrEmpty(DisplayName))
            {
                return Address;
            }
            else
            {
                return "\"" + DisplayName + "\" " + SmtpAddress;
            }
        }

        public override bool Equals(object value)
        {
            if (value == null)
            {
                return false;
            }
            return ToString().Equals(value.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        private static readonly EncodedStreamFactory s_encoderFactory = new EncodedStreamFactory();

        // Encodes the full email address, folding as needed
        internal string Encode(int charsConsumed, bool allowUnicode)
        {
            string encodedAddress = string.Empty;
            IEncodableStream encoder;
            byte[] buffer;

            Debug.Assert(Address != null, "address was null");

            //do we need to take into account the Display name?  If so, encode it
            if (!string.IsNullOrEmpty(_displayName))
            {
                //figure out the encoding type.  If it's all ASCII and contains no CRLF then
                //it does not need to be encoded for parity with other email clients.  We will 
                //however fold at the end of the display name so that the email address itself can
                //be appended.
                if (MimeBasePart.IsAscii(_displayName, false) || allowUnicode)
                {
                    encodedAddress = "\"" + _displayName + "\"";
                }
                else
                {
                    //encode the displayname since it's non-ascii
                    encoder = s_encoderFactory.GetEncoderForHeader(_displayNameEncoding, false, charsConsumed);
                    buffer = _displayNameEncoding.GetBytes(_displayName);
                    encoder.EncodeBytes(buffer, 0, buffer.Length);
                    encodedAddress = encoder.GetEncodedString();
                }

                //address should be enclosed in <> when a display name is present
                encodedAddress += " " + GetSmtpAddress(allowUnicode);
            }
            else
            {
                //no display name, just return the address
                encodedAddress = GetAddress(allowUnicode);
            }

            return encodedAddress;
        }
    }
}
