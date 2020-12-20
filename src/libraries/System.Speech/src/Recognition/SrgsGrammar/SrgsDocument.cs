// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Collections.Generic;
using System.Speech.Internal;
using System.Speech.Internal.SrgsCompiler;
using System.Speech.Internal.SrgsParser;

#pragma warning disable 1634, 1691 // Allows suppression of certain PreSharp messages.

namespace System.Speech.Recognition.SrgsGrammar
{
    /// <summary>
    /// This class allows a _grammar to be specified in SRGS form.   
    /// APITODO: needs programmatic access to SRGS dom; PACOG
    /// APITODO: needs rule activation/deactivation methods
    /// </summary>
    [Serializable]
    public class SrgsDocument
    {
        //*******************************************************************
        //
        // Constructors / Destructors
        //
        //*******************************************************************

        #region Constructors / Destructors

        /// <summary>
        /// The default constructor - creates an empty SrgsGrammar object
        /// </summary>
        public SrgsDocument()
        {
            _grammar = new SrgsGrammar();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public SrgsDocument(string path)
        {
            Helpers.ThrowIfEmptyOrNull(path, "path");

            using (XmlTextReader reader = new XmlTextReader(path))
            {
                Load(reader);
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="srgsGrammar"></param>
        public SrgsDocument(XmlReader srgsGrammar)
        {
            Helpers.ThrowIfNull(srgsGrammar, "srgsGrammar");

            Load(srgsGrammar);
        }


        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="builder"></param>
        public SrgsDocument(GrammarBuilder builder)
        {
            Helpers.ThrowIfNull(builder, "builder");

            // New grammar
            _grammar = new SrgsGrammar();
#pragma warning disable 56504 // The Culture property is the Grammar builder is already checked.
            _grammar.Culture = builder.Culture;
#pragma warning restore 56504

            // Creates SrgsDocument elements
            IElementFactory elementFactory = new SrgsElementFactory(_grammar);

            // Do it
            builder.CreateGrammar(elementFactory);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="grammarRootRule"></param>
        public SrgsDocument(SrgsRule grammarRootRule) : this()
        {
            Helpers.ThrowIfNull(grammarRootRule, "grammarRootRule");

            Root = grammarRootRule;
            Rules.Add(grammarRootRule);
        }

        #endregion

        //*******************************************************************
        //
        // Public methods
        //
        //*******************************************************************

        #region public methods

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="srgsGrammar"></param>
        public void WriteSrgs(XmlWriter srgsGrammar)
        {
            Helpers.ThrowIfNull(srgsGrammar, "srgsGrammar");

            // Make sure the grammar is ok
            _grammar.Validate();

            // Write the data.
            _grammar.WriteSrgs(srgsGrammar);
        }

        #endregion

        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************

        #region Public Properties

        /// <summary>
        /// Base URI of _grammar (xml:base).
        /// </summary>
        public Uri XmlBase
        {
            set
            {
                // base value can be null
#pragma warning disable 56526
                _grammar.XmlBase = value;
#pragma warning restore 56526
            }
            get
            {
                return _grammar.XmlBase;
            }
        }

        /// <summary>
        /// Grammar language (xml:lang)
        /// </summary>
        public CultureInfo Culture
        {
            set
            {
                Helpers.ThrowIfNull(value, "value");
                if (value.Equals(CultureInfo.InvariantCulture))
                {
                    throw new ArgumentException(SR.Get(SRID.InvariantCultureInfo), "value");
                }
                _grammar.Culture = value;
            }
            get
            {
                return _grammar.Culture;
            }
        }

        /// <summary>
        /// Root rule (srgs:root)
        /// </summary>
        public SrgsRule Root
        {
            set
            {
                // base value can be null
#pragma warning disable 56526
                _grammar.Root = value;
#pragma warning restore 56526
            }
            get
            {
                return _grammar.Root;
            }
        }

        /// <summary>
        /// Grammar mode (srgs:mode) - voice, dtmf
        /// </summary>
        public SrgsGrammarMode Mode
        {
            set
            {
                _grammar.Mode = value == SrgsGrammarMode.Voice ? GrammarType.VoiceGrammar : GrammarType.DtmfGrammar;
            }
            get
            {
                return _grammar.Mode == GrammarType.VoiceGrammar ? SrgsGrammarMode.Voice : SrgsGrammarMode.Dtmf;
            }
        }

        /// <summary>
        /// Grammar mode (srgs:mode) - voice, dtmf
        /// </summary>
        public SrgsPhoneticAlphabet PhoneticAlphabet
        {
            set
            {
                _grammar.PhoneticAlphabet = (AlphabetType)value;
                _grammar.HasPhoneticAlphabetBeenSet = true;
            }
            get
            {
                return (SrgsPhoneticAlphabet)_grammar.PhoneticAlphabet;
            }
        }

        /// <summary>
        /// A collection of rules that this _grammar houses.
        /// </summary>
        // APITODO: Implementations of Rules and all other SRGS objects not here for now
        public SrgsRulesCollection Rules
        {
            get
            {
                return _grammar.Rules;
            }
        }


        /// <summary>
        /// Programming Language used for the inline code; C#, VB or JScript
        /// </summary>
        public string Language
        {
            set
            {
                // Language can be set to null
#pragma warning disable 56526
                _grammar.Language = value;
#pragma warning restore 56526
            }
            get
            {
                return _grammar.Language;
            }
        }

        /// |summary|
        /// namespace
        /// |/summary|
        public string Namespace
        {
            set
            {
                // namespace can be set to null
#pragma warning disable 56526
                _grammar.Namespace = value;
#pragma warning restore 56526
            }
            get
            {
                return _grammar.Namespace;
            }
        }

        /// |summary|
        /// CodeBehind
        /// |/summary|
        public Collection<string> CodeBehind
        {
            get
            {
                return _grammar.CodeBehind;
            }
        }

        /// |summary|
        /// Add #line statements to the inline scripts if set
        /// |/summary|
        public bool Debug
        {
            get
            {
                return _grammar.Debug;
            }
            set
            {
                _grammar.Debug = value;
            }
        }

        /// |summary|
        /// language
        /// |/summary|
        public string Script
        {
            set
            {
                Helpers.ThrowIfEmptyOrNull(value, "value");
                _grammar.Script = value;
            }
            get
            {
                return _grammar.Script;
            }
        }


        /// |summary|
        /// ImportNameSpaces
        /// |/summary|
        public Collection<string> ImportNamespaces
        {
            get
            {
                return _grammar.ImportNamespaces;
            }
        }

        /// |summary|
        /// ImportNameSpaces
        /// |/summary|
        public Collection<string> AssemblyReferences
        {
            get
            {
                return _grammar.AssemblyReferences;
            }
        }

        #endregion

        //*******************************************************************
        //
        // Internal methods
        //
        //*******************************************************************

        #region Internal methods

        // Initialize an SrgsDocument from an Srgs text source.
        internal void Load(XmlReader srgsGrammar)
        {
            // New grammar
            _grammar = new SrgsGrammar();

            // For SrgsGrammar, the default is IPA, for xml grammars, it is sapi.
            _grammar.PhoneticAlphabet = AlphabetType.Sapi;

            // create an XMl Parser
            XmlParser srgsParser = new XmlParser(srgsGrammar, null);

            // Creates SrgsDocument elements
            srgsParser.ElementFactory = new SrgsElementFactory(_grammar);

            // Do it
            srgsParser.Parse();

            // This provides the path the XML was loaded from.
            // {Note potentially this may also be overridden by an xml:base attribute in the XML itself.
            // But for this scenario that doesn't matter since this is used to calculate the correct base path.}
            if (!string.IsNullOrEmpty(srgsGrammar.BaseURI))
            {
                _baseUri = new Uri(srgsGrammar.BaseURI);
            }
        }

        static internal GrammarOptions TagFormat2GrammarOptions(SrgsTagFormat value)
        {
            GrammarOptions newValue = 0;

            switch (value)
            {
                case SrgsTagFormat.KeyValuePairs:
                    newValue = GrammarOptions.KeyValuePairSrgs;
                    break;

                case SrgsTagFormat.MssV1:
                    newValue = GrammarOptions.MssV1;
                    break;

                case SrgsTagFormat.W3cV1:
                    newValue = GrammarOptions.W3cV1;
                    break;
            }
            return newValue;
        }

        static internal SrgsTagFormat GrammarOptions2TagFormat(GrammarOptions value)
        {
            SrgsTagFormat tagFormat = SrgsTagFormat.Default;

            switch (value & GrammarOptions.TagFormat)
            {
                case GrammarOptions.MssV1:
                    tagFormat = SrgsTagFormat.MssV1;
                    break;

                case GrammarOptions.W3cV1:
                    tagFormat = SrgsTagFormat.W3cV1;
                    break;

                case GrammarOptions.KeyValuePairSrgs:
                case GrammarOptions.KeyValuePairs:
                    tagFormat = SrgsTagFormat.KeyValuePairs;
                    break;
            }
            return tagFormat;
        }

        #endregion

        //*******************************************************************
        //
        // Internal Properties
        //
        //*******************************************************************

        #region Internal Properties

        /// <summary>
        /// Tag format (srgs:tag-format)
        /// </summary>summary>
        internal SrgsTagFormat TagFormat
        {
            set
            {
                _grammar.TagFormat = value;
            }
        }

        internal Uri BaseUri
        {
            get
            {
                return _baseUri;
            }
        }

        internal SrgsGrammar Grammar
        {
            get
            {
                return _grammar;
            }
        }

        #endregion


        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private SrgsGrammar _grammar;

        // Path the grammar was actually loaded from, if this exists.
        // Note this is different to SrgsGrammar.XmlBase which is the value of the xml:base attribute in the document itself.
        private Uri _baseUri;

        #endregion Fields
    }

    #region Enumerations

    /// TODOC <_include file='doc\SrgsGrammar.uex' path='docs/doc[@for="SrgsGrammarMode"]/*' />
    // Grammar mode.  Voice, Dtmf
    public enum SrgsGrammarMode
    {
        /// TODOC <_include file='doc\SrgsGrammar.uex' path='docs/doc[@for="SrgsGrammarMode.Voice"]/*' />
        Voice,
        /// TODOC <_include file='doc\SrgsGrammar.uex' path='docs/doc[@for="SrgsGrammarMode.Dtmf"]/*' />
        Dtmf
    }

    /// TODOC <_include file='doc\SrgsGrammar.uex' path='docs/doc[@for="SrgsGrammarMode"]/*' />
    // Grammar mode.  Voice, Dtmf
    public enum SrgsPhoneticAlphabet
    {
        /// <summary>
        /// TODOC
        /// </summary>
        Sapi,
        /// <summary>
        /// TODOC
        /// </summary>
        Ipa,
        /// <summary>
        /// TODOC
        /// </summary>
        Ups
    }

    #endregion Enumerations
}
