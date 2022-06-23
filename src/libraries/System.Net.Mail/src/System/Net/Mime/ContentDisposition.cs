// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Mail;
using System.Text;

namespace System.Net.Mime
{
    public class ContentDisposition
    {
        private const string CreationDateKey = "creation-date";
        private const string ModificationDateKey = "modification-date";
        private const string ReadDateKey = "read-date";
        private const string FileNameKey = "filename";
        private const string SizeKey = "size";

        private TrackingValidationObjectDictionary? _parameters;
        private string _disposition;
        private string _dispositionType;
        private bool _isChanged;
        private bool _isPersisted;

        private static readonly TrackingValidationObjectDictionary.ValidateAndParseValue s_dateParser =
                new TrackingValidationObjectDictionary.ValidateAndParseValue(v => new SmtpDateTime(v.ToString()!));
        // this will throw a FormatException if the value supplied is not a valid SmtpDateTime

        private static readonly TrackingValidationObjectDictionary.ValidateAndParseValue s_longParser =
                new TrackingValidationObjectDictionary.ValidateAndParseValue((object value) =>
                {
                    long longValue;
                    if (!long.TryParse(value.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out longValue))
                    {
                        throw new FormatException(SR.ContentDispositionInvalid);
                    }
                    return longValue;
                });

        private static readonly Dictionary<string, TrackingValidationObjectDictionary.ValidateAndParseValue> s_validators =
            new Dictionary<string, TrackingValidationObjectDictionary.ValidateAndParseValue>() {
                { CreationDateKey, s_dateParser },
                { ModificationDateKey, s_dateParser },
                { ReadDateKey, s_dateParser },
                { SizeKey, s_longParser }
            };

        public ContentDisposition()
        {
            _isChanged = true;
            _disposition = _dispositionType = "attachment";
            // no need to parse disposition since there's nothing to parse
        }

        public ContentDisposition(string disposition)
        {
            ArgumentNullException.ThrowIfNull(disposition);

            _isChanged = true;
            _disposition = disposition;
            ParseValue();
        }

        internal DateTime GetDateParameter(string parameterName)
        {
            SmtpDateTime? dateValue = ((TrackingValidationObjectDictionary)Parameters).InternalGet(parameterName) as SmtpDateTime;
            return dateValue == null ? DateTime.MinValue : dateValue.Date;
        }

        /// <summary>
        /// Gets the disposition type of the content.
        /// </summary>
        public string DispositionType
        {
            get { return _dispositionType; }
            set
            {
                ArgumentException.ThrowIfNullOrEmpty(value);

                _isChanged = true;
                _dispositionType = value;
            }
        }

        public StringDictionary Parameters => _parameters ??= new TrackingValidationObjectDictionary(s_validators);

        /// <summary>
        /// Gets the value of the Filename parameter.
        /// </summary>
        public string? FileName
        {
            get { return Parameters[FileNameKey]; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Parameters.Remove(FileNameKey);
                }
                else
                {
                    Parameters[FileNameKey] = value;
                }
            }
        }

        /// <summary>
        /// Gets the value of the Creation-Date parameter.
        /// </summary>
        public DateTime CreationDate
        {
            get { return GetDateParameter(CreationDateKey); }
            set
            {
                SmtpDateTime date = new SmtpDateTime(value);
                ((TrackingValidationObjectDictionary)Parameters).InternalSet(CreationDateKey, date);
            }
        }

        /// <summary>
        /// Gets the value of the Modification-Date parameter.
        /// </summary>
        public DateTime ModificationDate
        {
            get { return GetDateParameter(ModificationDateKey); }
            set
            {
                SmtpDateTime date = new SmtpDateTime(value);
                ((TrackingValidationObjectDictionary)Parameters).InternalSet(ModificationDateKey, date);
            }
        }

        public bool Inline
        {
            get { return _dispositionType == DispositionTypeNames.Inline; }
            set
            {
                _isChanged = true;
                _dispositionType = value ? DispositionTypeNames.Inline : DispositionTypeNames.Attachment;
            }
        }

        /// <summary>
        /// Gets the value of the Read-Date parameter.
        /// </summary>
        public DateTime ReadDate
        {
            get { return GetDateParameter(ReadDateKey); }
            set
            {
                SmtpDateTime date = new SmtpDateTime(value);
                ((TrackingValidationObjectDictionary)Parameters).InternalSet(ReadDateKey, date);
            }
        }

        /// <summary>
        /// Gets the value of the Size parameter (-1 if unspecified).
        /// </summary>
        public long Size
        {
            get
            {
                object? sizeValue = ((TrackingValidationObjectDictionary)Parameters).InternalGet(SizeKey);
                return sizeValue == null ? -1 : (long)sizeValue;
            }
            set
            {
                ((TrackingValidationObjectDictionary)Parameters).InternalSet(SizeKey, value);
            }
        }

        internal void Set(string contentDisposition, HeaderCollection headers)
        {
            // we don't set ischanged because persistence was already handled
            // via the headers.
            _disposition = contentDisposition;
            ParseValue();
            headers.InternalSet(MailHeaderInfo.GetString(MailHeaderID.ContentDisposition)!, ToString());
            _isPersisted = true;
        }

        internal void PersistIfNeeded(HeaderCollection headers, bool forcePersist)
        {
            if (IsChanged || !_isPersisted || forcePersist)
            {
                headers.InternalSet(MailHeaderInfo.GetString(MailHeaderID.ContentDisposition)!, ToString());
                _isPersisted = true;
            }
        }

        internal bool IsChanged => _isChanged || _parameters != null && _parameters.IsChanged;

        public override string ToString()
        {
            if (_disposition == null || _isChanged || _parameters != null && _parameters.IsChanged)
            {
                _disposition = Encode(false); // Legacy wire-safe format
                _isChanged = false;
                _parameters!.IsChanged = false;
                _isPersisted = false;
            }
            return _disposition;
        }

        internal string Encode(bool allowUnicode)
        {
            var builder = new StringBuilder();
            builder.Append(_dispositionType); // Must not have unicode, already validated

            // Validate and encode unicode where required
            foreach (string key in Parameters.Keys)
            {
                builder.Append("; ");
                EncodeToBuffer(key, builder, allowUnicode);

                builder.Append('=');
                EncodeToBuffer(_parameters![key]!, builder, allowUnicode);
            }

            return builder.ToString();
        }

        private static void EncodeToBuffer(string value, StringBuilder builder, bool allowUnicode)
        {
            Encoding? encoding = MimeBasePart.DecodeEncoding(value);
            if (encoding != null) // Manually encoded elsewhere, pass through
            {
                builder.Append('"').Append(value).Append('"');
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

        public override bool Equals([NotNullWhen(true)] object? rparam)
        {
            return rparam == null ?
                false :
                string.Equals(ToString(), rparam.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode() => ToString().ToLowerInvariant().GetHashCode();

        [MemberNotNull(nameof(_dispositionType))]
        private void ParseValue()
        {
            int offset = 0;
            try
            {
                // the disposition MUST be the first parameter in the string
                _dispositionType = MailBnfHelper.ReadToken(_disposition, ref offset, null);

                // disposition MUST not be empty
                if (string.IsNullOrEmpty(_dispositionType))
                {
                    throw new FormatException(SR.MailHeaderFieldMalformedHeader);
                }

                // now we know that there are parameters so we must initialize or clear
                // and parse
                if (_parameters == null)
                {
                    _parameters = new TrackingValidationObjectDictionary(s_validators);
                }
                else
                {
                    _parameters.Clear();
                }

                while (MailBnfHelper.SkipCFWS(_disposition, ref offset))
                {
                    // ensure that the separator charactor is present
                    if (_disposition[offset++] != ';')
                    {
                        throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, _disposition[offset - 1]));
                    }

                    // skip whitespace and see if there's anything left to parse or if we're done
                    if (!MailBnfHelper.SkipCFWS(_disposition, ref offset))
                    {
                        break;
                    }

                    string? paramAttribute = MailBnfHelper.ReadParameterAttribute(_disposition, ref offset, null);
                    string? paramValue;

                    // verify the next character after the parameter is correct
                    if (_disposition[offset++] != '=')
                    {
                        throw new FormatException(SR.MailHeaderFieldMalformedHeader);
                    }

                    if (!MailBnfHelper.SkipCFWS(_disposition, ref offset))
                    {
                        // parameter was at end of string and has no value
                        // this is not valid
                        throw new FormatException(SR.ContentDispositionInvalid);
                    }

                    paramValue = _disposition[offset] == '"' ?
                        MailBnfHelper.ReadQuotedString(_disposition, ref offset, null) :
                        MailBnfHelper.ReadToken(_disposition, ref offset, null);

                    // paramValue could potentially still be empty if it was a valid quoted string that
                    // contained no inner value.  this is invalid
                    if (string.IsNullOrEmpty(paramAttribute) || string.IsNullOrEmpty(paramValue))
                    {
                        throw new FormatException(SR.ContentDispositionInvalid);
                    }

                    // if validation is needed, the parameters dictionary will have a validator registered
                    // for the parameter that is being set so no additional formatting checks are needed here
                    Parameters.Add(paramAttribute, paramValue);
                }
            }
            catch (FormatException exception)
            {
                // it's possible that something in MailBNFHelper could throw so ensure that we catch it and wrap it
                // so that the exception has the correct text
                throw new FormatException(SR.ContentDispositionInvalid, exception);
            }

            _parameters.IsChanged = false;
        }
    }
}
