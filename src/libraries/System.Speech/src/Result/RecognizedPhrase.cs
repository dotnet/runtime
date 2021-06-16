// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Speech.Internal.SapiInterop;
using System.Speech.Internal.SrgsCompiler;
using System.Speech.Internal.SrgsParser;
using System.Text;
using System.Xml;
using System.Xml.XPath;

#pragma warning disable 56500 // Remove all the catch all statements warnings used by the interop layer

namespace System.Speech.Recognition
{
    [Serializable]
    [DebuggerDisplay("{Text}")]
    public class RecognizedPhrase
    {
        #region Constructors

        internal RecognizedPhrase()
        {
        }

        #endregion

        #region Public Methods
        public IXPathNavigable ConstructSmlFromSemantics()
        {
            if (!string.IsNullOrEmpty(_smlContent))
            {
                XmlDocument smlDocument = new();
                smlDocument.LoadXml(_smlContent);
                return smlDocument;
            }

            if (_serializedPhrase.SemanticErrorInfoOffset != 0)
            {
                ThrowInvalidSemanticInterpretationError();
            }

            XmlDocument document = new();
            XmlElement root = document.CreateElement("SML");
            NumberFormatInfo nfo = new();
            nfo.NumberDecimalDigits = 3;

            document.AppendChild(root);

            root.SetAttribute("text", Text);
            root.SetAttribute("utteranceConfidence", Confidence.ToString("f", nfo));
            root.SetAttribute("confidence", Confidence.ToString("f", nfo));

            if (Semantics.Count > 0)
            {
                AppendPropertiesSML(document, root, Semantics, nfo);
            }
            else if (Semantics.Value != null)
            {
                XmlText valueText = document.CreateTextNode(Semantics.Value.ToString());
                root.AppendChild(valueText);
            }

            // now append the alternates
            for (int i = 0; i < _recoResult.Alternates.Count; i++)
            {
                RecognizedPhrase alternate = _recoResult.Alternates[i];
                alternate.AppendSml(document, i + 1, nfo);
            }

            _smlContent = document.OuterXml;
            return document;
        }

        #endregion

        #region Public Properties
        public string Text
        {
            get
            {
                if (_text == null)
                {
                    Collection<ReplacementText> replacements = ReplacementWordUnits;
                    ReplacementText replacement;

                    int iCurReplacementIndex = 0;
                    int iWordReplacement = NextReplacementWord(replacements, out replacement, ref iCurReplacementIndex);
                    StringBuilder sb = new();
                    for (int i = 0; i < Words.Count; i++)
                    {
                        DisplayAttributes displayAttribute;
                        string text;
                        if (i == iWordReplacement)
                        {
                            displayAttribute = replacement.DisplayAttributes;
                            text = replacement.Text;
                            i += replacement.CountOfWords - 1;
                            iWordReplacement = NextReplacementWord(replacements, out replacement, ref iCurReplacementIndex);
                        }
                        else
                        {
                            displayAttribute = Words[i].DisplayAttributes;
                            text = Words[i].Text;
                        }

                        // Remove leading spaces
                        if ((displayAttribute & DisplayAttributes.ConsumeLeadingSpaces) != 0)
                        {
                            while (sb.Length > 0 && sb[sb.Length - 1] == ' ')
                            {
                                sb.Remove(sb.Length - 1, 1);
                            }
                        }

                        // Append text
                        sb.Append(text);

                        // Add trailing spaces
                        if ((displayAttribute & DisplayAttributes.ZeroTrailingSpaces) != 0)
                        {
                            // no action
                        }
                        else if ((displayAttribute & DisplayAttributes.OneTrailingSpace) != 0)
                        {
                            sb.Append(' ');
                        }
                        else if ((displayAttribute & DisplayAttributes.TwoTrailingSpaces) != 0)
                        {
                            sb.Append("  ");
                        }
                    }

                    _text = sb.ToString().Trim(' ');
                }
                return _text;
            }
        }
        public float Confidence
        {
            get
            {
                return _confidence;
            }
        }
        public ReadOnlyCollection<RecognizedWordUnit> Words
        {
            get
            {
                if (_words == null)
                {
                    int countOfElements = (int)_serializedPhrase.Rule.ulCountOfElements;
                    int elementsOffset = (int)_serializedPhrase.ElementsOffset;
                    List<RecognizedWordUnit> wordList = new(countOfElements);

                    int sizeofPhraseElement = Marshal.SizeOf(typeof(SPSERIALIZEDPHRASEELEMENT));

                    GCHandle gc = GCHandle.Alloc(_phraseBuffer, GCHandleType.Pinned);
                    try
                    {
                        IntPtr buffer = gc.AddrOfPinnedObject();
                        for (int i = 0; i < countOfElements; i++)
                        {
                            IntPtr elementBuffer = new((long)buffer + elementsOffset + i * sizeofPhraseElement);
                            SPSERIALIZEDPHRASEELEMENT element = (SPSERIALIZEDPHRASEELEMENT)Marshal.PtrToStructure(elementBuffer, typeof(SPSERIALIZEDPHRASEELEMENT));

                            string displayForm = null, lexicalForm = null, pronunciation = null;
                            if (element.pszDisplayTextOffset != 0)
                            {
                                IntPtr displayFormBuffer = new((long)buffer + (int)element.pszDisplayTextOffset);
                                displayForm = Marshal.PtrToStringUni(displayFormBuffer);
                            }
                            if (element.pszLexicalFormOffset != 0)
                            {
                                IntPtr lexicalFormBuffer = new((long)buffer + (int)element.pszLexicalFormOffset);
                                lexicalForm = Marshal.PtrToStringUni(lexicalFormBuffer);
                            }

                            if (element.pszPronunciationOffset != 0)
                            {
                                IntPtr pronunciationBuffer = new((long)buffer + (int)element.pszPronunciationOffset);
                                pronunciation = Marshal.PtrToStringUni(pronunciationBuffer);
                                if (!_hasIPAPronunciation)
                                {
                                    pronunciation = _recoResult.ConvertPronunciation(pronunciation, _serializedPhrase.LangID);
                                }
                            }

                            DisplayAttributes displayAttributes = RecognizedWordUnit.SapiAttributesToDisplayAttributes(element.bDisplayAttributes);

                            // On SAPI 5.1, the engine confidence is not set. Force a value in this case
                            if (!_isSapi53Header)
                            {
                                element.SREngineConfidence = 1.0f;
                            }
                            wordList.Add(new RecognizedWordUnit(displayForm, element.SREngineConfidence, pronunciation, lexicalForm, displayAttributes, new TimeSpan(element.ulAudioTimeOffset * TimeSpan.TicksPerMillisecond / 10000), new TimeSpan(element.ulAudioSizeTime * TimeSpan.TicksPerMillisecond / 10000)));
                        }
                        _words = new ReadOnlyCollection<RecognizedWordUnit>(wordList);
                    }
                    finally
                    {
                        gc.Free();
                    }
                }
                return _words;
            }
        }

        // Semantic data about result:
        public SemanticValue Semantics
        {
            get
            {
                if (_serializedPhrase.SemanticErrorInfoOffset != 0)
                {
                    ThrowInvalidSemanticInterpretationError();
                }

                if (_phraseBuffer == null)
                {
                    throw new NotSupportedException();
                }
                if (_semantics == null)
                {
                    CalcSemantics(Grammar);
                }
                return _semantics;
            }
        }

        // Homophonic alternates for this phrase
        public ReadOnlyCollection<RecognizedPhrase> Homophones
        {
            get
            {
                if (_phraseBuffer == null)
                {
                    throw new NotSupportedException();
                }
                if (_homophones == null)
                {
                    // Walks the list of alternates and collects all phrases that have the same
                    // homophoneGroupId at the phrase
                    List<RecognizedPhrase> homophones = new(_recoResult.Alternates.Count);
                    for (int i = 0; i < _recoResult.Alternates.Count; i++)
                    {
                        if ((_recoResult.Alternates[i]._homophoneGroupId == _homophoneGroupId) && (_recoResult.Alternates[i].Text != this.Text))
                        {
                            homophones.Add(_recoResult.Alternates[i]);
                        }
                    }
                    _homophones = new ReadOnlyCollection<RecognizedPhrase>(homophones);
                }
                return _homophones;
            }
        }
        public Grammar Grammar
        {
            get
            {
                // If this phrase comes from a deserialize, then throw
                if (_grammarId == unchecked((ulong)(-1)))
                {
                    throw new NotSupportedException(SR.Get(SRID.CantGetPropertyFromSerializedInfo, "Grammar"));
                }

                if (_grammar == null && _recoResult.Recognizer != null)
                {
                    _grammar = _recoResult.Recognizer.GetGrammarFromId(_grammarId);
                }
                return _grammar;
            }
        }
        public Collection<ReplacementText> ReplacementWordUnits
        {
            get
            {
                if (_replacementText == null)
                {
                    _replacementText = new Collection<ReplacementText>();

                    GCHandle gc = GCHandle.Alloc(_phraseBuffer, GCHandleType.Pinned);
                    try
                    {
                        IntPtr buffer = gc.AddrOfPinnedObject();

                        // Get the ITN and Look for replacement phrase/
                        IntPtr itnBuffer = new((long)buffer + _serializedPhrase.ReplacementsOffset);
                        for (int i = 0; i < _serializedPhrase.cReplacements; i++, itnBuffer = (IntPtr)((long)itnBuffer + Marshal.SizeOf(typeof(SPPHRASEREPLACEMENT))))
                        {
                            SPPHRASEREPLACEMENT replacement = (SPPHRASEREPLACEMENT)Marshal.PtrToStructure(itnBuffer, typeof(SPPHRASEREPLACEMENT));
                            string text = Marshal.PtrToStringUni(new IntPtr((long)buffer + replacement.pszReplacementText));
                            DisplayAttributes displayAttributes = RecognizedWordUnit.SapiAttributesToDisplayAttributes(replacement.bDisplayAttributes);
                            _replacementText.Add(new ReplacementText(displayAttributes, text, (int)replacement.ulFirstElement, (int)replacement.ulCountOfElements));
                        }
                    }
                    finally
                    {
                        gc.Free();
                    }
                }
                return _replacementText;
            }
        }
        public int HomophoneGroupId
        {
            get
            {
                return _homophoneGroupId;
            }
        }
        #endregion

        #region Internal Methods

        internal static SPSERIALIZEDPHRASE GetPhraseHeader(IntPtr phraseBuffer, uint expectedPhraseSize, bool isSapi53Header)
        {
            SPSERIALIZEDPHRASE serializedPhrase;

            if (isSapi53Header)
            {
                serializedPhrase = (SPSERIALIZEDPHRASE)Marshal.PtrToStructure(phraseBuffer, typeof(SPSERIALIZEDPHRASE));
            }
            else
            {
                SPSERIALIZEDPHRASE_Sapi51 legacyPhrase = (SPSERIALIZEDPHRASE_Sapi51)Marshal.PtrToStructure(phraseBuffer, typeof(SPSERIALIZEDPHRASE_Sapi51));
                serializedPhrase = new SPSERIALIZEDPHRASE(legacyPhrase);
            }

            if (serializedPhrase.ulSerializedSize > expectedPhraseSize)
            {
                throw new FormatException(SR.Get(SRID.ResultInvalidFormat));
            }
            return serializedPhrase;
        }

        internal void InitializeFromSerializedBuffer(RecognitionResult recoResult, SPSERIALIZEDPHRASE serializedPhrase, IntPtr phraseBuffer, int phraseLength, bool isSapi53Header, bool hasIPAPronunciation)
        {
            _recoResult = recoResult;
            _isSapi53Header = isSapi53Header;
            _serializedPhrase = serializedPhrase;

            _confidence = _serializedPhrase.Rule.SREngineConfidence;
            _grammarId = _serializedPhrase.ullGrammarID;
            _homophoneGroupId = _serializedPhrase.wHomophoneGroupId;
            _hasIPAPronunciation = hasIPAPronunciation;

            // Save the phrase blob
            _phraseBuffer = new byte[phraseLength];
            Marshal.Copy(phraseBuffer, _phraseBuffer, 0, phraseLength);

            // Get the grammar options
            _grammarOptions = recoResult.Grammar != null ? recoResult.Grammar._semanticTag : GrammarOptions.KeyValuePairSrgs;

            // This triggers the semantic processing if any
            CalcSemantics(recoResult.Grammar);
        }

        #endregion

        #region Internal Properties

        internal ulong GrammarId
        {
            get
            {
                return _grammarId;
            }
        }

        internal string SmlContent
        {
            get
            {
                if (_smlContent == null)
                {
                    // this method already sets _smlContent
                    ConstructSmlFromSemantics();
                }
                return _smlContent;
            }
        }

        #endregion

        #region Internal fields

        internal SPSERIALIZEDPHRASE _serializedPhrase;
        internal byte[] _phraseBuffer;
        internal bool _isSapi53Header;
        internal bool _hasIPAPronunciation;

        #endregion

        #region Private Methods

        // Semantic data about result:
        private void CalcSemantics(Grammar grammar)
        {
            if (_semantics == null && _serializedPhrase.SemanticErrorInfoOffset == 0)
            {
                GCHandle gc = GCHandle.Alloc(_phraseBuffer, GCHandleType.Pinned);
                try
                {
                    IntPtr buffer = gc.AddrOfPinnedObject();

                    if (!CalcILSemantics(buffer))
                    {
                        // List of recognized words
                        IList<RecognizedWordUnit> words = Words;

                        // Build the list of rules and property values
                        RuleNode ruleTree = ExtractRules(grammar, _serializedPhrase.Rule, buffer);
                        List<ResultPropertiesRef> propertyList = BuildRecoPropertyTree(_serializedPhrase, buffer, ruleTree, words, _isSapi53Header);

                        // Recursively build the dictionary of properties
                        _semantics = RecursiveBuildSemanticProperties(words, propertyList, ruleTree, _grammarOptions & GrammarOptions.TagFormat, ref _dupItems);
                        // Delay the call to TryExecuteOnRecognition until the _semantics has been set
                        _semantics.Value = TryExecuteOnRecognition(grammar, _recoResult, ruleTree._rule);
                    }
                }
                finally
                {
                    gc.Free();
                }
            }
        }

        private bool CalcILSemantics(IntPtr phraseBuffer)
        {
            if ((_grammarOptions & (GrammarOptions.MssV1 | GrammarOptions.W3cV1)) != 0 || _grammarOptions == GrammarOptions.KeyValuePairs)
            {
                IList<RecognizedWordUnit> words = Words;
                _semantics = new SemanticValue("<ROOT>", null, _confidence);
                if (_serializedPhrase.PropertiesOffset != 0)
                {
                    RecursivelyExtractSemanticValue(phraseBuffer, (int)_serializedPhrase.PropertiesOffset, _semantics, words, _isSapi53Header, _grammarOptions & GrammarOptions.TagFormat);
                }
                return true;
            }
            return false;
        }

        private static List<ResultPropertiesRef> BuildRecoPropertyTree(SPSERIALIZEDPHRASE serializedPhrase, IntPtr phraseBuffer, RuleNode ruleTree, IList<RecognizedWordUnit> words, bool isSapi53Header)
        {
            List<ResultPropertiesRef> propertyList = new();

            // Array of string containing the rule names.
            if ((int)serializedPhrase.PropertiesOffset > 0)
            {
                RecursivelyExtractSemanticProperties(propertyList, (int)serializedPhrase.PropertiesOffset, phraseBuffer, ruleTree, words, isSapi53Header);
            }
            return propertyList;
        }

        private static SemanticValue RecursiveBuildSemanticProperties(IList<RecognizedWordUnit> words, List<ResultPropertiesRef> properties, RuleNode ruleTree, GrammarOptions semanticTag, ref Collection<SemanticValue> dupItems)
        {
            SemanticValue semanticValue = new(ruleTree._name, null, ruleTree._confidence);

            // Add the semantic values from the child rules
            for (RuleNode children = ruleTree._child; children != null; children = children._next)
            {
                // Propagate up the semantic values calculated at the children level
                SemanticValue childrenSemantics = RecursiveBuildSemanticProperties(words, properties, children, semanticTag, ref dupItems);
                if (!children._hasName)
                {
                    foreach (KeyValuePair<string, SemanticValue> kv in childrenSemantics._dictionary)
                    {
                        InsertSemanticValueToDictionary(semanticValue, kv.Key, kv.Value, semanticTag, ref dupItems);
                    }
                    if (childrenSemantics.Value != null)
                    {
                        if ((semanticTag & (GrammarOptions.MssV1 | GrammarOptions.W3cV1)) == 0 && semanticValue._valueFieldSet && !semanticValue.Value.Equals(childrenSemantics.Value))
                        {
                            throw new InvalidOperationException(SR.Get(SRID.DupSemanticValue, ruleTree._name));
                        }
                        semanticValue.Value = childrenSemantics.Value;
                        semanticValue._valueFieldSet = true;
                    }
                }
                else
                {
                    // If no value has been set then the recognized text is returned as the value
                    if (!childrenSemantics._valueFieldSet && childrenSemantics.Count == 0)
                    {
                        StringBuilder sb = new();
                        for (int i = 0; i < children._count; i++)
                        {
                            if (sb.Length > 0)
                            {
                                sb.Append(' ');
                            }
                            sb.Append(words[(int)children._firstElement + i].Text);
                        }
                        childrenSemantics._valueFieldSet = true;
                        childrenSemantics.Value = sb.ToString();
                    }
                    semanticValue._dictionary.Add(children._name, childrenSemantics);
                }
            }

            // Add the semantic value from the properties
            foreach (ResultPropertiesRef property in properties)
            {
                if (property._ruleNode == ruleTree)
                {
                    InsertSemanticValueToDictionary(semanticValue, property._name, property._value, semanticTag, ref dupItems);
                }
            }

            Exception exceptionThrown = null;

            // Try to execute the semantic value if OnParse is defined
            object newValue;
            bool doneOnParse = TryExecuteOnParse(ruleTree, semanticValue, words, out newValue, ref exceptionThrown);

            if (exceptionThrown != null)
            {
                ExceptionDispatchInfo.Throw(exceptionThrown);
            }

            //
            if (doneOnParse)
            {
                semanticValue._dictionary.Clear();
                semanticValue.Value = newValue;
                semanticValue._valueFieldSet = true;
            }

            return semanticValue;
        }

        private static void RecursivelyExtractSemanticProperties(List<ResultPropertiesRef> propertyList, int semanticsOffset, IntPtr phraseBuffer, RuleNode ruleTree, IList<RecognizedWordUnit> words, bool isSapi53Header)
        {
            IntPtr propertyBuffer = new((long)phraseBuffer + semanticsOffset);
            SPSERIALIZEDPHRASEPROPERTY property = (SPSERIALIZEDPHRASEPROPERTY)Marshal.PtrToStructure(propertyBuffer, typeof(SPSERIALIZEDPHRASEPROPERTY));

            string propertyName;
            SemanticValue thisSemanticValue = ExtractSemanticValueInformation(semanticsOffset, property, phraseBuffer, isSapi53Header, out propertyName);

            RuleNode node = ruleTree.Find(property.ulFirstElement, property.ulCountOfElements);
            if (propertyName == "SemanticKey")
            {
                node._name = (string)thisSemanticValue.Value;
                node._hasName = true;
            }
            else
            {
                propertyList.Add(new ResultPropertiesRef(propertyName, thisSemanticValue, node));
            }

            if (property.pFirstChildOffset > 0)
            {
                // add children to the new node
                RecursivelyExtractSemanticProperties(propertyList, (int)property.pFirstChildOffset, phraseBuffer, ruleTree, words, isSapi53Header);
            }

            if (property.pNextSiblingOffset > 0)
            {
                // add siblings to parent node
                RecursivelyExtractSemanticProperties(propertyList, (int)property.pNextSiblingOffset, phraseBuffer, ruleTree, words, isSapi53Header);
            }
        }

        private void RecursivelyExtractSemanticValue(IntPtr phraseBuffer, int semanticsOffset, SemanticValue semanticValue, IList<RecognizedWordUnit> words, bool isSapi53Header, GrammarOptions semanticTag)
        {
            IntPtr propertyBuffer = new((long)phraseBuffer + semanticsOffset);
            SPSERIALIZEDPHRASEPROPERTY property =
                (SPSERIALIZEDPHRASEPROPERTY)Marshal.PtrToStructure(propertyBuffer, typeof(SPSERIALIZEDPHRASEPROPERTY));

            string propertyName;
            SemanticValue thisSemanticValue = ExtractSemanticValueInformation(semanticsOffset, property, phraseBuffer, isSapi53Header, out propertyName);

            if (propertyName == "_value" && semanticValue != null)
            {
                // 'remove' the _value node from the tree by setting its value to the parent's value
                // and use the parent as the node to add children (if present)
                semanticValue.Value = thisSemanticValue.Value;
                if (property.pFirstChildOffset > 0)
                {
                    thisSemanticValue = semanticValue;
                }
            }
            else
            {
                InsertSemanticValueToDictionary(semanticValue, propertyName, thisSemanticValue, semanticTag, ref _dupItems);
            }

            if (property.pFirstChildOffset > 0)
            {
                // add children to the new node
                RecursivelyExtractSemanticValue(phraseBuffer, (int)property.pFirstChildOffset, thisSemanticValue, words, isSapi53Header, semanticTag);
            }

            if (property.pNextSiblingOffset > 0)
            {
                // add siblings to parent node
                RecursivelyExtractSemanticValue(phraseBuffer, (int)property.pNextSiblingOffset, semanticValue, words, isSapi53Header, semanticTag);
            }
        }

        private static void InsertSemanticValueToDictionary(SemanticValue semanticValue, string propertyName, SemanticValue thisSemanticValue, GrammarOptions semanticTag, ref Collection<SemanticValue> dupItems)
        {
            string key = propertyName;
            if ((key == "$" && semanticTag == GrammarOptions.MssV1)
                || (key == "=" && (semanticTag == GrammarOptions.KeyValuePairSrgs || semanticTag == GrammarOptions.KeyValuePairs))
                || (thisSemanticValue.Count == -1 && semanticTag == GrammarOptions.W3cV1))
            {
                if ((semanticTag & (GrammarOptions.MssV1 | GrammarOptions.W3cV1)) == 0 && semanticValue._valueFieldSet && !semanticValue.Value.Equals(thisSemanticValue.Value))
                {
                    throw new InvalidOperationException(SR.Get(SRID.DupSemanticValue, semanticValue.KeyName));
                }
                semanticValue.Value = thisSemanticValue.Value;
                semanticValue._valueFieldSet = true;
            }
            else
            {
                bool containsKey = semanticValue._dictionary.ContainsKey(key);
                if (!containsKey)
                {
                    semanticValue._dictionary.Add(key, thisSemanticValue);
                }
                else
                {
                    if (!semanticValue._dictionary[key].Equals(thisSemanticValue))
                    {
                        // Error out for Srgs grammars
                        if (semanticTag == GrammarOptions.KeyValuePairSrgs)
                        {
                            throw new InvalidOperationException(SR.Get(SRID.DupSemanticKey, propertyName, semanticValue.KeyName));
                        }

                        // Append a _* on the key name for none SAPI grammars
                        int count = 0;
                        do
                        {
                            key = propertyName + string.Format(CultureInfo.InvariantCulture, "_{0}", count++);
                        }
                        while (semanticValue._dictionary.ContainsKey(key));
                        semanticValue._dictionary.Add(key, thisSemanticValue);
                        if (dupItems == null)
                        {
                            dupItems = new Collection<SemanticValue>();
                        }
                        SemanticValue s = semanticValue._dictionary[key];
                        dupItems.Add(s);
                    }
                }
            }
        }

        private static SemanticValue ExtractSemanticValueInformation(int semanticsOffset, SPSERIALIZEDPHRASEPROPERTY property, IntPtr phraseBuffer, bool isSapi53Header, out string propertyName)
        {
            object propertyValue;

            bool isIdName = false;
            if (property.pszNameOffset > 0)
            {
                IntPtr nameBuffer = new((long)phraseBuffer + (int)property.pszNameOffset);
                propertyName = Marshal.PtrToStringUni(nameBuffer);
            }
            else
            {
                propertyName = property.ulId.ToString(CultureInfo.InvariantCulture);
                isIdName = true;
            }

            if (property.pszValueOffset > 0)
            {
                IntPtr valueStringBuffer = new((long)phraseBuffer + (int)property.pszValueOffset);
                propertyValue = Marshal.PtrToStringUni(valueStringBuffer);
                if (!isSapi53Header && isIdName && ((string)propertyValue).Contains("$"))
                {
                    // SAPI 5.1 result that contains script fragments rather than output of executing script.
                    // Strip this information as script-based grammars aren't supported on 5.1.
                    throw new NotSupportedException(SR.Get(SRID.NotSupportedWithThisVersionOfSAPI));
                }
            }
            else
            {
                if (property.SpVariantSubset >= 0)
                {
                    IntPtr valueBuffer = new((long)phraseBuffer + +semanticsOffset + SpVariantSubsetOffset);
#pragma warning disable 0618 // VarEnum is obsolete
                    switch ((VarEnum)property.vValue)
                    {
                        case VarEnum.VT_I4:
                            propertyValue = Marshal.ReadInt32(valueBuffer);
                            break;

                        case VarEnum.VT_UI4:
                            propertyValue = Marshal.PtrToStructure(valueBuffer, typeof(uint));
                            break;

                        case VarEnum.VT_I8:
                            propertyValue = Marshal.ReadInt64(valueBuffer);
                            break;

                        case VarEnum.VT_UI8:
                            propertyValue = Marshal.PtrToStructure(valueBuffer, typeof(ulong));
                            break;

                        case VarEnum.VT_R4:
                            propertyValue = Marshal.PtrToStructure(valueBuffer, typeof(float));
                            break;

                        case VarEnum.VT_R8:
                            propertyValue = Marshal.PtrToStructure(valueBuffer, typeof(double));
                            break;

                        case VarEnum.VT_BOOL:
                            propertyValue = (Marshal.ReadByte(valueBuffer) != 0);
                            break;

                        case VarEnum.VT_EMPTY:
                            propertyValue = null;
                            break;
                        default:
                            throw new NotSupportedException(SR.Get(SRID.UnhandledVariant));
                    }
#pragma warning restore 0618
                }
                else
                {
                    propertyValue = string.Empty;
                }
            }
            return new SemanticValue(propertyName, propertyValue, property.SREngineConfidence);
        }

        private static RuleNode ExtractRules(Grammar grammar, SPSERIALIZEDPHRASERULE rule, IntPtr phraseBuffer)
        {
            // Get the rule name
            IntPtr nameBuffer = new((long)phraseBuffer + (int)rule.pszNameOffset);

            // Add the rule name to the proper element index
            string name = Marshal.PtrToStringUni(nameBuffer);

            // find the grammar for this rule. If the grammar does not belong to any existing ruleref then
            // it must be local.
            Grammar ruleRef = grammar != null ? grammar.Find(name) : null;
            if (ruleRef != null)
            {
                grammar = ruleRef;
            }
            RuleNode node = new(grammar, name, rule.SREngineConfidence, rule.ulFirstElement, rule.ulCountOfElements);

            if (rule.NextSiblingOffset > 0)
            {
                IntPtr elementBuffer = new((long)phraseBuffer + rule.NextSiblingOffset);
                SPSERIALIZEDPHRASERULE ruleNext = (SPSERIALIZEDPHRASERULE)Marshal.PtrToStructure(elementBuffer, typeof(SPSERIALIZEDPHRASERULE));

                node._next = ExtractRules(grammar, ruleNext, phraseBuffer);
            }

            if (rule.FirstChildOffset > 0)
            {
                IntPtr elementBuffer = new((long)phraseBuffer + rule.FirstChildOffset);
                SPSERIALIZEDPHRASERULE ruleFirst = (SPSERIALIZEDPHRASERULE)Marshal.PtrToStructure(elementBuffer, typeof(SPSERIALIZEDPHRASERULE));

                node._child = ExtractRules(grammar, ruleFirst, phraseBuffer);
            }
            return node;
        }

        private void ThrowInvalidSemanticInterpretationError()
        {
            //string error;
            if (!_isSapi53Header)
            {
                throw new NotSupportedException(SR.Get(SRID.NotSupportedWithThisVersionOfSAPI));
            }
            GCHandle gc = GCHandle.Alloc(_phraseBuffer, GCHandleType.Pinned);
            try
            {
                IntPtr smlBuffer = gc.AddrOfPinnedObject();

                SPSEMANTICERRORINFO semanticError = (SPSEMANTICERRORINFO)Marshal.PtrToStructure((IntPtr)((long)smlBuffer + (int)_serializedPhrase.SemanticErrorInfoOffset), typeof(SPSEMANTICERRORINFO));

                string source = Marshal.PtrToStringUni(new IntPtr((long)smlBuffer + semanticError.pszSourceOffset));
                string description = Marshal.PtrToStringUni(new IntPtr((long)smlBuffer + semanticError.pszDescriptionOffset));
                string script = Marshal.PtrToStringUni(new IntPtr((long)smlBuffer + semanticError.pszScriptLineOffset));

                string error = string.Format(CultureInfo.InvariantCulture, "Error while evaluating semantic interpretation:\n" +
                                            "  HRESULT:     {0:x}\n" +
                                            "  Line:        {1}\n" +
                                            "  Source:      {2}\n" +
                                            "  Description: {3}\n" +
                                            "  Script:      {4}\n", semanticError.hrResultCode, semanticError.ulLineNumber, source, description, script);
                throw new InvalidOperationException(error);
            }
            finally
            {
                gc.Free();
            }
        }

        private static bool TryExecuteOnParse(RuleNode ruleRef, SemanticValue value, IList<RecognizedWordUnit> words, out object newValue, ref Exception exceptionThrown)
        {
            newValue = null;
            bool doneOnParse = false;
            Grammar grammar = ruleRef._grammar;

            if (grammar != null && grammar._scripts != null)
            {
                // Check if the Inner
                try
                {
                    if (exceptionThrown == null)
                    {
                        doneOnParse = ExecuteOnParse(grammar, ruleRef, value, words, out newValue);
                    }
                    else
                    {
                        if (ExecuteOnError(grammar, ruleRef, exceptionThrown))
                        {
                            exceptionThrown = null;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (exceptionThrown == null)
                    {
                        exceptionThrown = e;
                        // Try to execute on Error on this thread
                        try
                        {
                            if (ExecuteOnError(grammar, ruleRef, exceptionThrown))
                            {
                                exceptionThrown = null;
                            }
                        }
                        catch (Exception e2)
                        {
                            exceptionThrown = e2;
                        }
                    }
                }
            }
            return doneOnParse;
        }

        private static bool ExecuteOnParse(Grammar grammar, RuleNode ruleRef, SemanticValue value, IList<RecognizedWordUnit> words, out object newValue)
        {
            // Get the rule list
            ScriptRef[] scripts = grammar._scripts;
            bool doneOnParse = false;
            newValue = null;

            // Look if an OnParse exist for this method
            for (int iScript = 0; iScript < scripts.Length; iScript++)
            {
                ScriptRef script = scripts[iScript];
                if (ruleRef._rule == script._rule)
                {
                    if (script._method == RuleMethodScript.onParse)
                    {
                        // Get the method to invoke
                        RecognizedWordUnit[] recoUnits = new RecognizedWordUnit[ruleRef._count];
                        for (int i = 0; i < ruleRef._count; i++)
                        {
                            recoUnits[i] = words[i];
                        }

                        object[] parameters = new object[2] { value, recoUnits };

                        if (grammar._proxy != null)
                        {
                            Exception appDomainException;
                            newValue = grammar._proxy.OnParse(script._rule, script._sMethod, parameters, out appDomainException);

                            if (appDomainException != null)
                            {
                                ExceptionDispatchInfo.Throw(appDomainException);
                            }
                        }
                        else
                        {
                            MethodInfo onParse;
                            System.Speech.Recognition.Grammar rule;
                            GetRuleInstance(grammar, script._rule, script._sMethod, out onParse, out rule);

                            // Execute the parse routine
                            newValue = onParse.Invoke(rule, parameters);
                        }
                        doneOnParse = true;
                    }
                }
            }
            return doneOnParse;
        }

        private static bool ExecuteOnError(Grammar grammar, RuleNode ruleRef, Exception e)
        {
            // Get the rule list
            ScriptRef[] scripts = grammar._scripts;
            bool invoked = false;

            // Look if an OnParse exist for this method
            for (int iScript = 0; iScript < scripts.Length; iScript++)
            {
                ScriptRef script = scripts[iScript];
                if (ruleRef._rule == script._rule)
                {
                    if (script._method == RuleMethodScript.onError)
                    {
                        // Get the method to invoke
                        object[] parameters = new object[] { e };

                        if (grammar._proxy != null)
                        {
                            Exception appDomainException;
                            grammar._proxy.OnError(script._rule, script._sMethod, parameters, out appDomainException);
                            if (appDomainException != null)
                            {
                                ExceptionDispatchInfo.Throw(appDomainException);
                            }
                        }
                        else
                        {
                            MethodInfo onError;
                            System.Speech.Recognition.Grammar rule;
                            GetRuleInstance(grammar, script._rule, script._sMethod, out onError, out rule);

                            // Execute the parse routine
                            onError.Invoke(rule, parameters);
                        }
                        invoked = true;
                    }
                }
            }
            return invoked;
        }

        private static object TryExecuteOnRecognition(Grammar grammar, RecognitionResult result, string rootRule)
        {
            object resultValue = result.Semantics.Value;
            if (grammar != null && grammar._scripts != null)
            {
                // Get the rule list
                ScriptRef[] scripts = grammar._scripts;

                // Look if an OnRecognition exist for this method
                for (int iScript = 0; iScript < scripts.Length; iScript++)
                {
                    ScriptRef script = scripts[iScript];
                    if (rootRule == script._rule)
                    {
                        if (script._method == RuleMethodScript.onRecognition)
                        {
                            // Get the method to invoke
                            object[] parameters = new object[1] { result };

                            if (grammar._proxy != null)
                            {
                                Exception appDomainException;
                                resultValue = grammar._proxy.OnRecognition(script._sMethod, parameters, out appDomainException);
                                if (appDomainException != null)
                                {
                                    ExceptionDispatchInfo.Throw(appDomainException);
                                }
                            }
                            else
                            {
                                Type grammarType = grammar.GetType();
                                MethodInfo onRecognition = grammarType.GetMethod(script._sMethod, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                                // Execute the parse routine
                                resultValue = onRecognition.Invoke(grammar, parameters);
                            }
                        }
                    }
                }
            }

            return resultValue;
        }

        private static void GetRuleInstance(Grammar grammar, string rule, string method, out MethodInfo onParse, out Grammar ruleInstance)
        {
            Type grammarType = grammar.GetType();
            Assembly assembly = grammarType.Assembly;
            Type ruleClass = rule == grammarType.Name ? grammarType : GetTypeForRule(assembly, rule);
            if (ruleClass == null || !ruleClass.IsSubclassOf(typeof(System.Speech.Recognition.Grammar)))
            {
                throw new FormatException(SR.Get(SRID.RecognizerInvalidBinaryGrammar));
            }
            ruleInstance = ruleClass == grammarType ? grammar : (System.Speech.Recognition.Grammar)assembly.CreateInstance(ruleClass.FullName);
            onParse = ruleInstance.MethodInfo(method);
        }

        private static Type GetTypeForRule(Assembly assembly, string rule)
        {
            Type[] types = assembly.GetTypes();
            for (int iType = 0; iType < types.Length; iType++)
            {
                Type type = types[iType];
                if (type.Name == rule && type.IsPublic && type.IsSubclassOf(typeof(System.Speech.Recognition.Grammar)))
                {
                    return type;
                }
            }
            return null;
        }

        private static int NextReplacementWord(Collection<ReplacementText> replacements, out ReplacementText replacement, ref int posInCollection)
        {
            if (posInCollection < replacements.Count)
            {
                replacement = replacements[posInCollection++];
                return replacement.FirstWordIndex;
            }
            else
            {
                replacement = null;
                return -1;
            }
        }

        private void AppendSml(XmlDocument document, int i, NumberFormatInfo nfo)
        {
            XmlNode root = document.DocumentElement;
            XmlElement alternateNode = document.CreateElement("alternate");
            root.AppendChild(alternateNode);

            alternateNode.SetAttribute("Rank", i.ToString(CultureInfo.CurrentCulture));
            alternateNode.SetAttribute("text", Text);
            alternateNode.SetAttribute("utteranceConfidence", Confidence.ToString("f", nfo));
            alternateNode.SetAttribute("confidence", Confidence.ToString("f", nfo));

            if (_semantics.Value != null)
            {
                XmlText valueText = document.CreateTextNode(_semantics.Value.ToString());
                alternateNode.AppendChild(valueText);
            }

            // recursively add the properties now
            AppendPropertiesSML(document, alternateNode, _semantics, nfo);
        }

        private void AppendPropertiesSML(XmlDocument document, XmlElement alternateNode, SemanticValue semanticsNode, NumberFormatInfo nfo)
        {
            if (semanticsNode != null)
            {
                foreach (KeyValuePair<string, SemanticValue> kv in semanticsNode)
                {
                    if (kv.Key == "_attributes")
                    {
                        // all the attributes are located under the attribute property.
                        AppendAttributes(alternateNode, kv.Value);
                        if (string.IsNullOrEmpty(alternateNode.InnerText) && semanticsNode.Value != null)
                        {
                            XmlText valueText = document.CreateTextNode(semanticsNode.Value.ToString());
                            alternateNode.AppendChild(valueText);
                        }
                    }
                    else
                    {
                        string keyName = kv.Key;
                        if (_dupItems != null && _dupItems.Contains(kv.Value))
                        {
                            keyName = RemoveTrailingNumber(kv.Key);
                        }

                        XmlElement propertyNode = document.CreateElement(keyName);
                        propertyNode.SetAttribute("confidence", semanticsNode[kv.Key].Confidence.ToString("f", nfo));
                        alternateNode.AppendChild(propertyNode);

                        if (kv.Value.Count > 0)
                        {
                            if (kv.Value.Value != null)
                            {
                                XmlText valueText = document.CreateTextNode(kv.Value.Value.ToString());
                                propertyNode.AppendChild(valueText);
                            }
                            AppendPropertiesSML(document, propertyNode, kv.Value, nfo);
                        }
                        else if (kv.Value.Value != null)
                        {
                            XmlText valueText = document.CreateTextNode(kv.Value.Value.ToString());
                            propertyNode.AppendChild(valueText);
                        }
                    }
                }
            }
        }

        private string RemoveTrailingNumber(string name)
        {
            return name.Substring(0, name.LastIndexOf('_'));
        }

        private void AppendAttributes(XmlElement propertyNode, SemanticValue semanticValue)
        {
            foreach (KeyValuePair<string, SemanticValue> kv in semanticValue)
            {
                if (propertyNode.Attributes[kv.Key] == null)
                {
                    propertyNode.SetAttribute(kv.Key, kv.Value.Value.ToString());
                }
            }
        }

        #endregion

        #region Private Types
        [DebuggerDisplay("{DisplayDebugInfo()}")]
        private sealed class RuleNode
        {
            internal RuleNode(Grammar grammar, string rule, float confidence, uint first, uint count)
            {
                _rule = _name = rule;
                _firstElement = first;
                _count = count;
                _confidence = confidence;
                _grammar = grammar;
                //_ruleId = id;
            }

            /// <summary>
            /// Find the rule enclosing a property.
            /// </summary>
            /// <param name="firstElement">First word matching the property</param>
            /// <param name="count">Count of words</param>
            internal RuleNode Find(uint firstElement, uint count)
            {
                // If the count of word is set to zero. It means that the tag is located just before a word.
                // The trick here is to use 1/2 position to locate tags in this case.
                float firstWord, lastWord;
                if (count == 0)
                {
                    firstWord = lastWord = firstElement - 0.5f;
                }
                else
                {
                    firstWord = firstElement;
                    lastWord = firstWord + (count - 1);
                }

                for (RuleNode child = _child; child != null; child = child._next)
                {
                    float ruleFirstWord, ruleLastWord;
                    if (child._count == 0)
                    {
                        ruleFirstWord = ruleLastWord = child._firstElement - 0.5f;
                    }
                    else
                    {
                        ruleFirstWord = child._firstElement;
                        ruleLastWord = ruleFirstWord + (child._count - 1);
                    }
                    if (ruleFirstWord <= firstElement && ruleLastWord >= lastWord)
                    {
                        return child.Find(firstElement, count);
                    }
                }
                return this;
            }

            private string DisplayDebugInfo()
            {
                return string.Format("'rule={0}", _rule);
            }
            internal Grammar _grammar;
            internal string _rule;
            internal string _name;
            internal uint _firstElement;
            internal uint _count;
            internal float _confidence;
            internal bool _hasName;

            internal RuleNode _next;
            internal RuleNode _child;
        }
        [DebuggerDisplay("Name={_name} node={_ruleNode._rule} value={_value != null && _value.Value != null ? _value.Value.ToString() : \"\"}")]
        private struct ResultPropertiesRef
        {
            internal string _name;
            internal SemanticValue _value;
            internal RuleNode _ruleNode;

            internal ResultPropertiesRef(string name, SemanticValue value, RuleNode ruleNode)
            {
                _name = name;
                _value = value;
                _ruleNode = ruleNode;
            }
        }

        #endregion

        #region Private Fields

        private RecognitionResult _recoResult;
        private GrammarOptions _grammarOptions;

        private string _text;
        private float _confidence;
        private SemanticValue _semantics;
        private ReadOnlyCollection<RecognizedWordUnit> _words;
        private Collection<ReplacementText> _replacementText;

        [NonSerializedAttribute]
        private ulong _grammarId = unchecked((ulong)(-1));
#pragma warning disable 6524
        [NonSerializedAttribute]
        private Grammar _grammar;
#pragma warning restore 6524
        private int _homophoneGroupId;
        private ReadOnlyCollection<RecognizedPhrase> _homophones;
        private Collection<SemanticValue> _dupItems;

        private string _smlContent;

        private const int SpVariantSubsetOffset = 16;

        #endregion
    }
}
