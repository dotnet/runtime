// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Speech.Internal;
using System.Speech.Internal.SrgsParser;
using System.Xml;


namespace System.Speech.Recognition.SrgsGrammar
{
    /// TODOC <_include file='doc\SrgsText.uex' path='docs/doc[@for="SrgsText"]/*' />
    [Serializable]
    [DebuggerDisplay("{DebuggerDisplayString ()}")]
    public class SrgsText : SrgsElement, IElementText
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        /// TODOC <_include file='doc\SrgsText.uex' path='docs/doc[@for="SrgsText.SrgsText1"]/*' />
        public SrgsText()
        {
        }

#pragma warning disable 56507

        /// TODOC <_include file='doc\SrgsText.uex' path='docs/doc[@for="SrgsText.SrgsText2"]/*' />
        public SrgsText(string text)
        {
            Helpers.ThrowIfNull(text, nameof(text));

            Text = text;
        }

        #endregion

        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************

        #region public Properties

        /// TODOC <_include file='doc\SrgsText.uex' path='docs/doc[@for="SrgsText.Text"]/*' />
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                Helpers.ThrowIfNull(value, nameof(value));

                // Parse the text to check for errors
                XmlParser.ParseText(null, value, null, null, -1f, null);
                _text = value;
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

        internal override void WriteSrgs(XmlWriter writer)
        {
            // Write _text if any
            if (_text != null && _text.Length > 0)
            {
                writer.WriteString(_text);
            }
        }

        internal override string DebuggerDisplayString()
        {
            return "'" + _text + "'";
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private string _text = string.Empty;

        #endregion
    }
}

