// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Speech.Internal;
using System.Speech.Internal.SrgsParser;
using System.Text;
using System.Xml;

namespace System.Speech.Recognition.SrgsGrammar
{
    [Serializable]
    [DebuggerDisplay("{DebuggerDisplayString ()}")]
    public class SrgsSemanticInterpretationTag : SrgsElement, ISemanticTag
    {
        #region Constructors
        public SrgsSemanticInterpretationTag()
        {
        }
        public SrgsSemanticInterpretationTag(string script)
        {
            Helpers.ThrowIfNull(script, nameof(script));

            _script = script;
        }

        #endregion

        #region public Properties
        public string Script
        {
            get
            {
                return _script;
            }
            set
            {
                Helpers.ThrowIfNull(value, nameof(value));

                _script = value;
            }
        }

        #endregion

        #region Internal Methods

        // Validate the SRGS element.
        /// <summary>
        /// Validate each element and recurse through all the children srgs
        /// elements if any.
        /// </summary>
        internal override void Validate(SrgsGrammar grammar)
        {
            if (grammar.TagFormat == SrgsTagFormat.Default)
            {
                grammar.TagFormat |= SrgsTagFormat.W3cV1;
            }
            else if (grammar.TagFormat == SrgsTagFormat.KeyValuePairs)
            {
                XmlParser.ThrowSrgsException(SRID.SapiPropertiesAndSemantics);
            }
        }

        internal override void WriteSrgs(XmlWriter writer)
        {
            // Skip writing the tag if empty
            string script = Script.Trim(Helpers._achTrimChars);

            // Write <tag>script</tag>
            writer.WriteStartElement("tag");

            // Write the script if any
            if (!string.IsNullOrEmpty(script))
            {
                writer.WriteString(script);
            }
            writer.WriteEndElement();
        }

        internal override string DebuggerDisplayString()
        {
            StringBuilder sb = new("SrgsSemanticInterpretationTag '");
            sb.Append(_script);
            sb.Append('\'');
            return sb.ToString();
        }

        void ISemanticTag.Content(IElement parent, string value, int line)
        {
            Script = value;
        }

        #endregion

        #region Private Fields

        private string _script = string.Empty;

        #endregion
    }
}
