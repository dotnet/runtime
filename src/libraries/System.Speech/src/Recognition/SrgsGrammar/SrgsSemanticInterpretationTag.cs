// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Speech.Internal;
using System.Speech.Internal.SrgsParser;
using System.Text;
using System.Xml;

#pragma warning disable 1634, 1691 // Allows suppression of certain PreSharp messages.

namespace System.Speech.Recognition.SrgsGrammar
{
    /// TODOC <_include file='doc\Tag.uex' path='docs/doc[@for="Tag"]/*' />
    [Serializable]
    [DebuggerDisplay("{DebuggerDisplayString ()}")]
    public class SrgsSemanticInterpretationTag : SrgsElement, ISemanticTag
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        /// TODOC <_include file='doc\Tag.uex' path='docs/doc[@for="Tag.Tag1"]/*' />
        public SrgsSemanticInterpretationTag()
        {
        }

#pragma warning disable 56507

        /// TODOC <_include file='doc\Tag.uex' path='docs/doc[@for="Tag.Tag2"]/*' />
        public SrgsSemanticInterpretationTag(string script)
        {
            Helpers.ThrowIfNull(script, "script");

            _script = script;
        }

        #endregion

        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************

        #region public Properties

        /// TODOC <_include file='doc\Tag.uex' path='docs/doc[@for="Tag.Script"]/*' />
        public string Script
        {
            get
            {
                return _script;
            }
            set
            {
                Helpers.ThrowIfNull(value, "value");

                _script = value;
            }
        }

#pragma warning restore 56507

        #endregion

        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        // Validate the SRGS element.
        /// <summary>
        /// Validate each element and recurse through all the children srgs
        /// elements if any.
        /// </summary>
        override internal void Validate(SrgsGrammar grammar)
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
            StringBuilder sb = new StringBuilder("SrgsSemanticInterpretationTag '");
            sb.Append(_script);
            sb.Append('\'');
            return sb.ToString();
        }

        void ISemanticTag.Content(IElement parent, string value, int line)
        {
            Script = value;
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private string _script = string.Empty;

        #endregion
    }
}

