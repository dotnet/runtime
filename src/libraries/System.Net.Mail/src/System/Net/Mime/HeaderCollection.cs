// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
using System.Globalization;
using System.Net.Mail;
using System.Text;

namespace System.Net.Mime
{
    /// <summary>
    /// Summary description for HeaderCollection.
    /// </summary>
    internal class HeaderCollection : NameValueCollection
    {
        private MimeBasePart _part = null;

        // default constructor
        // intentionally override the default comparer in the derived base class 
        internal HeaderCollection() : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        public override void Remove(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name == string.Empty)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(name)), nameof(name));
            }

            MailHeaderID id = MailHeaderInfo.GetID(name);

            if (id == MailHeaderID.ContentType && _part != null)
            {
                _part.ContentType = null;
            }
            else if (id == MailHeaderID.ContentDisposition && _part is MimePart)
            {
                ((MimePart)_part).ContentDisposition = null;
            }

            base.Remove(name);
        }


        public override string Get(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name == string.Empty)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(name)), nameof(name));
            }

            MailHeaderID id = MailHeaderInfo.GetID(name);

            if (id == MailHeaderID.ContentType && _part != null)
            {
                _part.ContentType.PersistIfNeeded(this, false);
            }
            else if (id == MailHeaderID.ContentDisposition && _part is MimePart)
            {
                ((MimePart)_part).ContentDisposition.PersistIfNeeded(this, false);
            }
            return base.Get(name);
        }

        public override string[] GetValues(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name == string.Empty)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(name)), nameof(name));
            }

            MailHeaderID id = MailHeaderInfo.GetID(name);

            if (id == MailHeaderID.ContentType && _part != null)
            {
                _part.ContentType.PersistIfNeeded(this, false);
            }
            else if (id == MailHeaderID.ContentDisposition && _part is MimePart)
            {
                ((MimePart)_part).ContentDisposition.PersistIfNeeded(this, false);
            }
            return base.GetValues(name);
        }


        internal void InternalRemove(string name) => base.Remove(name);

        //set an existing header's value
        internal void InternalSet(string name, string value) => base.Set(name, value);

        //add a new header and set its value
        internal void InternalAdd(string name, string value)
        {
            if (MailHeaderInfo.IsSingleton(name))
            {
                base.Set(name, value);
            }
            else
            {
                base.Add(name, value);
            }
        }

        public override void Set(string name, string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (name == string.Empty)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(name)), nameof(name));
            }

            if (value == string.Empty)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(value)), nameof(value));
            }

            if (!MimeBasePart.IsAscii(name, false))
            {
                throw new FormatException(SR.Format(SR.InvalidHeaderName));
            }

            // normalize the case of well known headers
            name = MailHeaderInfo.NormalizeCase(name);

            MailHeaderID id = MailHeaderInfo.GetID(name);

            value = value.Normalize(NormalizationForm.FormC);

            if (id == MailHeaderID.ContentType && _part != null)
            {
                _part.ContentType.Set(value.ToLower(CultureInfo.InvariantCulture), this);
            }
            else if (id == MailHeaderID.ContentDisposition && _part is MimePart)
            {
                ((MimePart)_part).ContentDisposition.Set(value.ToLower(CultureInfo.InvariantCulture), this);
            }
            else
            {
                base.Set(name, value);
            }
        }


        public override void Add(string name, string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (name == string.Empty)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(name)), nameof(name));
            }
            if (value == string.Empty)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(value)), nameof(value));
            }

            MailBnfHelper.ValidateHeaderName(name);

            // normalize the case of well known headers
            name = MailHeaderInfo.NormalizeCase(name);

            MailHeaderID id = MailHeaderInfo.GetID(name);

            value = value.Normalize(NormalizationForm.FormC);

            if (id == MailHeaderID.ContentType && _part != null)
            {
                _part.ContentType.Set(value.ToLower(CultureInfo.InvariantCulture), this);
            }
            else if (id == MailHeaderID.ContentDisposition && _part is MimePart)
            {
                ((MimePart)_part).ContentDisposition.Set(value.ToLower(CultureInfo.InvariantCulture), this);
            }
            else
            {
                InternalAdd(name, value);
            }
        }
    }
}
