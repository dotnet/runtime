// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        internal MailAddress(string displayName, string userName, string domain, Encoding? displayNameEncoding)
        {
            _host = domain;
            _userName = userName;
            _displayName = displayName;
            _displayNameEncoding = displayNameEncoding ?? Encoding.GetEncoding(MimeBasePart.DefaultCharSet);

            Debug.Assert(_host != null,
                "host was null in internal constructor");

            Debug.Assert(userName != null,
                "userName was null in internal constructor");

            Debug.Assert(displayName != null,
                "displayName was null in internal constructor");
        }

        public MailAddress(string address) : this(address, null, (Encoding?)null)
        {
        }

        public MailAddress(string address, string? displayName) : this(address, displayName, (Encoding?)null)
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
        public MailAddress(string address, string? displayName, Encoding? displayNameEncoding)
        {
            bool parseSuccess = TryParse(address, displayName, displayNameEncoding,
                                        out (string displayName, string user, string host, Encoding displayNameEncoding) parsedData,
                                        throwExceptionIfFail: true);

            _displayName = parsedData.displayName;
            _userName = parsedData.user;
            _host = parsedData.host;
            _displayNameEncoding = parsedData.displayNameEncoding;

            Debug.Assert(parseSuccess);
        }

        /// <summary>
        /// Create a new <see cref="MailAddress"/>. Does not throw an exception if the MailAddress cannot be created.
        /// </summary>
        /// <param name="address">A <see cref="string"/> that contains an email address.</param>
        /// <param name="result">When this method returns, contains the <see cref="MailAddress"/> instance if address parsing succeed</param>
        /// <returns>A <see cref="bool"/> value that is true if the <see cref="MailAddress"/> was successfully created; otherwise, false.</returns>
        public static bool TryCreate(string address, [NotNullWhen(true)] out MailAddress? result) => TryCreate(address, displayName: null, out result);

        /// <summary>
        /// Create a new <see cref="MailAddress"/>. Does not throw an exception if the MailAddress cannot be created.
        /// </summary>
        /// <param name="address">A <see cref="string"/> that contains an email address.</param>
        /// <param name="displayName">A <see cref="string"/> that contains the display name associated with address. This parameter can be null.</param>
        /// <param name="result">When this method returns, contains the <see cref="MailAddress"/> instance if address parsing succeed</param>
        /// <returns>A <see cref="bool"/> value that is true if the <see cref="MailAddress"/> was successfully created; otherwise, false.</returns>
        public static bool TryCreate(string address, string? displayName, [NotNullWhen(true)] out MailAddress? result) => TryCreate(address, displayName, displayNameEncoding: null, out result);

        /// <summary>
        /// Create a new <see cref="MailAddress"/>. Does not throw an exception if the MailAddress cannot be created.
        /// </summary>
        /// <param name="address">A <see cref="string"/> that contains an email address.</param>
        /// <param name="displayName">A <see cref="string"/> that contains the display name associated with address. This parameter can be null.</param>
        /// <param name="displayNameEncoding">The <see cref="Encoding"/> that defines the character set used for displayName</param>
        /// <param name="result">When this method returns, contains the <see cref="MailAddress"/> instance if address parsing succeed</param>
        /// <returns>A <see cref="bool"/> value that is true if the <see cref="MailAddress"/> was successfully created; otherwise, false.</returns>
        public static bool TryCreate(string address, string? displayName, Encoding? displayNameEncoding, [NotNullWhen(true)] out MailAddress? result)
        {
            if (TryParse(address, displayName, displayNameEncoding,
                        out (string displayName, string user, string host, Encoding displayNameEncoding) parsed,
                        throwExceptionIfFail: false))
            {
                result = new MailAddress(parsed.displayName, parsed.user, parsed.host, parsed.displayNameEncoding);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        private static bool TryParse(string address, string? displayName, Encoding? displayNameEncoding, out (string displayName, string user, string host, Encoding displayNameEncoding) parsedData, bool throwExceptionIfFail)
        {
            if (throwExceptionIfFail)
            {
                ArgumentException.ThrowIfNullOrEmpty(address);
            }
            else if (string.IsNullOrEmpty(address))
            {
                parsedData = default;
                return false;
            }

            displayNameEncoding ??= Encoding.GetEncoding(MimeBasePart.DefaultCharSet);
            displayName ??= string.Empty;

            // Check for bounding quotes
            if (!string.IsNullOrEmpty(displayName))
            {
                if (!MailAddressParser.TryNormalizeOrThrow(displayName, out displayName, throwExceptionIfFail))
                {
                    parsedData = default;
                    return false;
                }

                if (displayName.Length >= 2 && displayName[0] == '\"' && displayName[^1] == '\"')
                {
                    // Peal bounding quotes, they'll get re-added later.
                    displayName = displayName.Substring(1, displayName.Length - 2);
                }
            }

            if (!MailAddressParser.TryParseAddress(address, out ParseAddressInfo info, throwExceptionIfFail))
            {
                parsedData = default;
                return false;
            }

            // If we were not given a display name, use the one parsed from 'address'.
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = info.DisplayName;
            }

            parsedData = (displayName, info.User, info.Host, displayNameEncoding);

            return true;
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
        /// i.e. "some email address display name" &lt;user@host&gt;
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
                return "\"" + DisplayName.Replace("\"", "\\\"") + "\" " + SmtpAddress;
            }
        }

        public override bool Equals([NotNullWhen(true)] object? value)
        {
            if (value == null)
            {
                return false;
            }
            return ToString().Equals(value.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return StringComparer.InvariantCultureIgnoreCase.GetHashCode(ToString());
        }

        // Encodes the full email address, folding as needed
        internal string Encode(int charsConsumed, bool allowUnicode)
        {
            string encodedAddress;
            IEncodableStream encoder;

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
                    encoder = EncodedStreamFactory.GetEncoderForHeader(_displayNameEncoding, false, charsConsumed);
                    encoder.EncodeString(_displayName, _displayNameEncoding);
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
