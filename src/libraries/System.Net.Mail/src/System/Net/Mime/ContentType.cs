// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using System.Text;

namespace System.Net.Mime
{
    // Typed Content-Type header
    //
    // We parse the type during construction and set.
    // null and string.empty will throw for construction,set and mediatype/subtype
    // constructors set isPersisted to false.  isPersisted needs to be tracked separately
    // than isChanged because isChanged only determines if the cached value should be used.
    // isPersisted tracks if the object has been persisted. However, obviously if isChanged is true
    // the object isn't  persisted.
    // If any subcomponents are changed, isChanged is set to true and isPersisted is false
    // ToString caches the value until a isChanged is true, then it recomputes the full value.

    public class ContentType
    {
        private readonly TrackingStringDictionary _parameters = new TrackingStringDictionary();

        private string _mediaType;
        private string _subType;
        private bool _isChanged;
        private string _type;
        private bool _isPersisted;

        /// <summary>
        /// Default content type - can be used if the Content-Type header
        /// is not defined in the message headers.
        /// </summary>
        internal const string Default = "application/octet-stream";

        public ContentType() : this(Default)
        {
        }

        /// <summary>
        /// ctor.
        /// </summary>
        /// <param name="contentType">Unparsed value of the Content-Type header.</param>
        public ContentType(string contentType)
        {
            ArgumentException.ThrowIfNullOrEmpty(contentType);

            _isChanged = true;
            _type = contentType;
            ParseValue();
        }

        public string? Boundary
        {
            get { return Parameters["boundary"]; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Parameters.Remove("boundary");
                }
                else
                {
                    Parameters["boundary"] = value;
                }
            }
        }

        public string? CharSet
        {
            get { return Parameters["charset"]; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Parameters.Remove("charset");
                }
                else
                {
                    Parameters["charset"] = value;
                }
            }
        }

        /// <summary>
        /// Gets the media type.
        /// </summary>
        public string MediaType
        {
            get { return _mediaType + "/" + _subType; }
            set
            {
                ArgumentException.ThrowIfNullOrEmpty(value);

                int offset = 0;
                _mediaType = MailBnfHelper.ReadToken(value, ref offset);
                if (_mediaType.Length == 0 || offset >= value.Length || value[offset++] != '/')
                    throw new FormatException(SR.MediaTypeInvalid);

                _subType = MailBnfHelper.ReadToken(value, ref offset);
                if (_subType.Length == 0 || offset < value.Length)
                {
                    throw new FormatException(SR.MediaTypeInvalid);
                }

                _isChanged = true;
                _isPersisted = false;
            }
        }

        [AllowNull]
        public string Name
        {
            get
            {
                string? value = Parameters["name"];
                Encoding? nameEncoding = MimeBasePart.DecodeEncoding(value);
                if (nameEncoding != null)
                {
                    value = MimeBasePart.DecodeHeaderValue(value);
                }
                return value!;
            }
            set
            {
                if (value == null || value == string.Empty)
                {
                    Parameters.Remove("name");
                }
                else
                {
                    Parameters["name"] = value;
                }
            }
        }

        public StringDictionary Parameters => _parameters;

        internal void Set(string contentType, HeaderCollection headers)
        {
            _type = contentType;
            ParseValue();
            headers.InternalSet(MailHeaderInfo.GetString(MailHeaderID.ContentType)!, ToString());
            _isPersisted = true;
        }

        internal void PersistIfNeeded(HeaderCollection headers, bool forcePersist)
        {
            if (IsChanged || !_isPersisted || forcePersist)
            {
                headers.InternalSet(MailHeaderInfo.GetString(MailHeaderID.ContentType)!, ToString());
                _isPersisted = true;
            }
        }

        internal bool IsChanged => _isChanged || _parameters != null && _parameters.IsChanged;

        public override string ToString()
        {
            if (_type == null || IsChanged)
            {
                _type = Encode(false); // Legacy wire-safe format
                _isChanged = false;
                _parameters.IsChanged = false;
                _isPersisted = false;
            }
            return _type;
        }

        internal string Encode(bool allowUnicode)
        {
            var builder = new StringBuilder();

            builder.Append(_mediaType); // Must not have unicode, already validated
            builder.Append('/');
            builder.Append(_subType);  // Must not have unicode, already validated

            // Validate and encode unicode where required
            foreach (string key in Parameters.Keys)
            {
                builder.Append("; ");
                EncodeToBuffer(key, builder, allowUnicode);
                builder.Append('=');
                EncodeToBuffer(_parameters[key]!, builder, allowUnicode);
            }

            return builder.ToString();
        }

        private static void EncodeToBuffer(string value, StringBuilder builder, bool allowUnicode)
        {
            Encoding? encoding = MimeBasePart.DecodeEncoding(value);
            if (encoding != null) // Manually encoded elsewhere, pass through
            {
                builder.Append('\"').Append(value).Append('"');
            }
            else if ((allowUnicode && !MailBnfHelper.HasCROrLF(value)) // Unicode without CL or LF's
                || MimeBasePart.IsAscii(value, false)) // Ascii
            {
                MailBnfHelper.GetTokenOrQuotedString(value, builder, allowUnicode);
            }
            else
            {
                // MIME Encoding required
                encoding = Encoding.GetEncoding(MimeBasePart.DefaultCharSet);
                builder.Append('"').Append(MimeBasePart.EncodeHeaderValue(value, encoding, MimeBasePart.ShouldUseBase64Encoding(encoding))).Append('"');
            }
        }

        public override bool Equals([NotNullWhen(true)] object? rparam) =>
            rparam == null ? false : string.Equals(ToString(), rparam.ToString(), StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(ToString());

        // Helper methods.
        [MemberNotNull(nameof(_mediaType))]
        [MemberNotNull(nameof(_subType))]
        private void ParseValue()
        {
            try
            {
                int offset = 0;

                _mediaType = MailBnfHelper.ReadToken(_type, ref offset);
                if (string.IsNullOrEmpty(_mediaType) || offset >= _type.Length || _type[offset++] != '/')
                {
                    throw new FormatException(SR.ContentTypeInvalid);
                }

                _subType = MailBnfHelper.ReadToken(_type, ref offset);
                if (string.IsNullOrEmpty(_subType))
                {
                    throw new FormatException(SR.ContentTypeInvalid);
                }

                while (MailBnfHelper.SkipCFWS(_type, ref offset))
                {
                    if (_type[offset++] != ';')
                    {
                        throw new FormatException(SR.ContentTypeInvalid);
                    }

                    if (!MailBnfHelper.SkipCFWS(_type, ref offset))
                    {
                        break;
                    }

                    string? paramAttribute = MailBnfHelper.ReadParameterAttribute(_type, ref offset);

                    if (string.IsNullOrEmpty(paramAttribute))
                    {
                        throw new FormatException(SR.ContentTypeInvalid);
                    }

                    string? paramValue;
                    if (offset >= _type.Length || _type[offset++] != '=')
                    {
                        throw new FormatException(SR.ContentTypeInvalid);
                    }

                    if (!MailBnfHelper.SkipCFWS(_type, ref offset))
                    {
                        throw new FormatException(SR.ContentTypeInvalid);
                    }

                    paramValue = _type[offset] == '"' ?
                        MailBnfHelper.ReadQuotedString(_type, ref offset, null) :
                        MailBnfHelper.ReadToken(_type, ref offset);

                    if (paramValue == null)
                    {
                        throw new FormatException(SR.ContentTypeInvalid);
                    }

                    _parameters.Add(paramAttribute, paramValue);
                }

                _parameters.IsChanged = false;
            }
            catch (FormatException fe) when (fe.Message != SR.ContentTypeInvalid)
            {
                throw new FormatException(SR.ContentTypeInvalid);
            }
        }
    }
}
