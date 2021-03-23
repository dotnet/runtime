// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Speech.Internal;
using System.Speech.Internal.SrgsParser;
using System.Text;
using System.Xml;

namespace System.Speech.Recognition.SrgsGrammar
{
    [Serializable]
    [DebuggerDisplay("{DebuggerDisplayString ()}")]
    public class SrgsNameValueTag : SrgsElement, IPropertyTag
    {
        #region Constructors
        public SrgsNameValueTag()
        {
        }
        public SrgsNameValueTag(object value)
        {
            Helpers.ThrowIfNull(value, nameof(value));

            Value = value;
        }
        public SrgsNameValueTag(string name, object value)
            : this(value)
        {
            _name = GetTrimmedName(name, "name");
        }

        #endregion

        #region public Properties
        // Name of semantic property contained inside the <tag> element.
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = GetTrimmedName(value, "value");
            }
        }

        // Prefast cannot figure out that parameter checking is done
#pragma warning disable 56526
        // Value of semantic property contained inside the <tag> element.
        public object Value
        {
            get { return _value; }
            set
            {
                Helpers.ThrowIfNull(value, nameof(value));

                if ((value is string) ||
                (value is bool) ||
                (value is int) ||
                (value is double))
                {
                    _value = value;
                }
                else
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidValueType), nameof(value));
                }
            }
        }

#pragma warning restore 56526

        #endregion

        #region Internal methods

        internal override void WriteSrgs(XmlWriter writer)
        {
            // Figure out if the tag contains a value.
            bool hasValue = Value != null;

            // Do not write the tag if it is empty
            bool hasName = !string.IsNullOrEmpty(_name);
            // Write <tag>text</tag>
            writer.WriteStartElement("tag");

            StringBuilder sb = new();

            if (hasName)
            {
                sb.Append(_name);
                sb.Append('=');
            }

            if (hasValue)
            {
                if (Value is string)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "\"{0}\"", Value.ToString());
                }
                else
                {
                    sb.Append(Value.ToString());
                }
            }

            writer.WriteString(sb.ToString());
            writer.WriteEndElement();
        }

        internal override void Validate(SrgsGrammar grammar)
        {
            SrgsTagFormat tagFormat = grammar.TagFormat;
            if (tagFormat == SrgsTagFormat.Default)
            {
                grammar.TagFormat |= SrgsTagFormat.KeyValuePairs;
            }
            else if (tagFormat != SrgsTagFormat.KeyValuePairs)
            {
                XmlParser.ThrowSrgsException(SRID.SapiPropertiesAndSemantics);
            }
        }

        void IPropertyTag.NameValue(IElement parent, string name, object value)
        {
            _name = name;
            _value = value;
        }

        internal override string DebuggerDisplayString()
        {
            StringBuilder sb = new("SrgsNameValue ");

            if (_name != null)
            {
                sb.Append(_name);
                sb.Append(" (");
            }

            if (_value != null)
            {
                if (_value is string)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "\"{0}\"", _value.ToString());
                }
                else
                {
                    sb.Append(_value.ToString());
                }
            }
            else
            {
                sb.Append("null");
            }

            if (_name != null)
            {
                sb.Append(')');
            }

            return sb.ToString();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks if the name is not null and just made of blanks
        /// Returned the trimmed name
        /// </summary>
        private static string GetTrimmedName(string name, string parameterName)
        {
            Helpers.ThrowIfEmptyOrNull(name, parameterName);

            // Remove the leading and trailing spaces
            name = name.Trim(Helpers._achTrimChars);

            // Run again the validation code
            Helpers.ThrowIfEmptyOrNull(name, parameterName);

            return name;
        }

        #endregion

        #region Private Fields

        private string _name;

        private object _value;

        #endregion
    }
}
