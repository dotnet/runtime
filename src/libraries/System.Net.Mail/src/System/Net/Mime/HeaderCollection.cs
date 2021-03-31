// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.Net.Mail;
using System.Text;

namespace System.Net.Mime
{
    /// <summary>
    /// Summary description for HeaderCollection.
    /// </summary>
    internal sealed class HeaderCollection : NameValueCollection
    {
        // default constructor
        // intentionally override the default comparer in the derived base class
        internal HeaderCollection() : base(StringComparer.OrdinalIgnoreCase)
        {
        }

#pragma warning disable CS8765 // Nullability of parameter 'name' doesn't match overridden member
        public override void Remove(string name)
#pragma warning restore CS8765
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(name)), nameof(name));
            }

            base.Remove(name);
        }


#pragma warning disable CS8765 // Nullability of parameter 'name' doesn't match overridden member
        public override string? Get(string name)
#pragma warning restore CS8765
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(name)), nameof(name));
            }

            return base.Get(name);
        }

#pragma warning disable CS8765 // Nullability of parameter 'name' doesn't match overridden member
        public override string[]? GetValues(string name)
#pragma warning restore CS8765
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(name)), nameof(name));
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

#pragma warning disable CS8765 // Nullability of parameters 'name' and 'value' don't match overridden member
        public override void Set(string name, string value)
#pragma warning restore CS8765
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(name)), nameof(name));
            }

            if (value.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(value)), nameof(value));
            }

            if (!MimeBasePart.IsAscii(name, false))
            {
                throw new FormatException(SR.InvalidHeaderName);
            }

            // normalize the case of well known headers
            name = MailHeaderInfo.NormalizeCase(name);

            value = value.Normalize(NormalizationForm.FormC);

            base.Set(name, value);
        }


#pragma warning disable CS8765 // Nullability of parameters 'name' and 'value' don't match overridden member
        public override void Add(string name, string value)
#pragma warning restore CS8765
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(name)), nameof(name));
            }
            if (value.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(value)), nameof(value));
            }

            MailBnfHelper.ValidateHeaderName(name);

            // normalize the case of well known headers
            name = MailHeaderInfo.NormalizeCase(name);

            value = value.Normalize(NormalizationForm.FormC);

            InternalAdd(name, value);
        }
    }
}
