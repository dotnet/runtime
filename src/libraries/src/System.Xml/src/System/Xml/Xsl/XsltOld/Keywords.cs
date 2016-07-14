// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;

    internal class Keywords
    {
        private XmlNameTable _NameTable;
        internal Keywords(XmlNameTable nameTable)
        {
            Debug.Assert(nameTable != null);
            _NameTable = nameTable;
        }

        internal void LookupKeywords()
        {
            _AtomEmpty = _NameTable.Add(string.Empty);
            _AtomXsltNamespace = _NameTable.Add(s_XsltNamespace);
            _AtomApplyTemplates = _NameTable.Add(s_ApplyTemplates);
            _AtomChoose = _NameTable.Add(s_Choose);
            _AtomForEach = _NameTable.Add(s_ForEach);
            _AtomIf = _NameTable.Add(s_If);
            _AtomOtherwise = _NameTable.Add(s_Otherwise);
            _AtomStylesheet = _NameTable.Add(s_Stylesheet);
            _AtomTemplate = _NameTable.Add(s_Template);
            _AtomTransform = _NameTable.Add(s_Transform);
            _AtomValueOf = _NameTable.Add(s_ValueOf);
            _AtomWhen = _NameTable.Add(s_When);
            _AtomMatch = _NameTable.Add(s_Match);
            _AtomName = _NameTable.Add(s_Name);
            _AtomSelect = _NameTable.Add(s_Select);
            _AtomTest = _NameTable.Add(s_Test);

            _AtomMsXsltNamespace = _NameTable.Add(s_MsXsltNamespace);
            _AtomScript = _NameTable.Add(s_Script);
            CheckKeyword(_AtomEmpty);
            CheckKeyword(_AtomXsltNamespace);
            CheckKeyword(_AtomApplyTemplates);
            CheckKeyword(_AtomChoose);
            CheckKeyword(_AtomForEach);
            CheckKeyword(_AtomIf);
            CheckKeyword(_AtomOtherwise);
            CheckKeyword(_AtomStylesheet);
            CheckKeyword(_AtomTemplate);
            CheckKeyword(_AtomTransform);
            CheckKeyword(_AtomValueOf);
            CheckKeyword(_AtomWhen);
            CheckKeyword(_AtomMatch);
            CheckKeyword(_AtomName);
            CheckKeyword(_AtomSelect);
            CheckKeyword(_AtomTest);
            CheckKeyword(_AtomMsXsltNamespace);
            CheckKeyword(_AtomScript);
        }

        private string _AtomEmpty;
        private string _AtomXsltNamespace;
        private string _AtomApplyImports;
        private string _AtomApplyTemplates;
        private string _AtomAttribute;
        private string _AtomAttributeSet;
        private string _AtomCallTemplate;
        private string _AtomChoose;
        private string _AtomComment;
        private string _AtomCopy;
        private string _AtomCopyOf;
        private string _AtomDecimalFormat;
        private string _AtomElement;
        private string _AtomFallback;
        private string _AtomForEach;
        private string _AtomIf;
        private string _AtomImport;
        private string _AtomInclude;
        private string _AtomKey;
        private string _AtomMessage;
        private string _AtomNamespaceAlias;
        private string _AtomNumber;
        private string _AtomOtherwise;
        private string _AtomOutput;
        private string _AtomParam;
        private string _AtomPreserveSpace;
        private string _AtomProcessingInstruction;
        private string _AtomSort;
        private string _AtomStripSpace;
        private string _AtomStylesheet;
        private string _AtomTemplate;
        private string _AtomText;
        private string _AtomTransform;
        private string _AtomValueOf;
        private string _AtomVariable;
        private string _AtomWhen;
        private string _AtomWithParam;
        private string _AtomCaseOrder;
        private string _AtomCdataSectionElements;
        private string _AtomCount;
        private string _AtomDataType;
        private string _AtomDecimalSeparator;
        private string _AtomDigit;
        private string _AtomDisableOutputEscaping;
        private string _AtomDoctypePublic;
        private string _AtomDoctypeSystem;
        private string _AtomElements;
        private string _AtomEncoding;
        private string _AtomExcludeResultPrefixes;
        private string _AtomExtensionElementPrefixes;
        private string _AtomFormat;
        private string _AtomFrom;
        private string _AtomGroupingSeparator;
        private string _AtomGroupingSize;
        private string _AtomHref;
        private string _AtomId;
        private string _AtomIndent;
        private string _AtomInfinity;
        private string _AtomLang;
        private string _AtomLetterValue;
        private string _AtomLevel;
        private string _AtomMatch;
        private string _AtomMediaType;
        private string _AtomMethod;
        private string _AtomMinusSign;
        private string _AtomMode;
        private string _AtomName;
        private string _AtomNamespace;
        private string _AtomNaN;
        private string _AtomOmitXmlDeclaration;
        private string _AtomOrder;
        private string _AtomPatternSeparator;
        private string _AtomPercent;
        private string _AtomPerMille;
        private string _AtomPriority;
        private string _AtomResultPrefix;
        private string _AtomSelect;
        private string _AtomStandalone;
        private string _AtomStylesheetPrefix;
        private string _AtomTerminate;
        private string _AtomTest;
        private string _AtomUse;
        private string _AtomUseAttributeSets;
        private string _AtomValue;
        private string _AtomVersion;
        private string _AtomZeroDigit;
        private string _AtomHashDefault;
        private string _AtomNo;
        private string _AtomYes;

        private string _AtomMsXsltNamespace;
        private string _AtomScript;
        private string _AtomLanguage;
        private string _AtomImplementsPrefix;

        internal const string s_Xmlns = "xmlns";
        internal const string s_XsltNamespace = XmlReservedNs.NsXslt;
        internal const string s_XmlNamespace = XmlReservedNs.NsXml;
        internal const string s_XmlnsNamespace = XmlReservedNs.NsXmlNs;
        internal const string s_WdXslNamespace = XmlReservedNs.NsWdXsl;
        internal const string s_Version10 = "1.0";
        internal const string s_ApplyImports = "apply-imports";
        internal const string s_ApplyTemplates = "apply-templates";
        internal const string s_Attribute = "attribute";
        internal const string s_AttributeSet = "attribute-set";
        internal const string s_CallTemplate = "call-template";
        internal const string s_Choose = "choose";
        internal const string s_Comment = "comment";
        internal const string s_Copy = "copy";
        internal const string s_CopyOf = "copy-of";
        internal const string s_DecimalFormat = "decimal-format";
        internal const string s_Element = "element";
        internal const string s_Fallback = "fallback";
        internal const string s_ForEach = "for-each";
        internal const string s_If = "if";
        internal const string s_Import = "import";
        internal const string s_Include = "include";
        internal const string s_Key = "key";
        internal const string s_Message = "message";
        internal const string s_NamespaceAlias = "namespace-alias";
        internal const string s_Number = "number";
        internal const string s_Otherwise = "otherwise";
        internal const string s_Output = "output";
        internal const string s_Param = "param";
        internal const string s_PreserveSpace = "preserve-space";
        internal const string s_ProcessingInstruction = "processing-instruction";
        internal const string s_Sort = "sort";
        internal const string s_StripSpace = "strip-space";
        internal const string s_Stylesheet = "stylesheet";
        internal const string s_Template = "template";
        internal const string s_Text = "text";
        internal const string s_Transform = "transform";
        internal const string s_ValueOf = "value-of";
        internal const string s_Variable = "variable";
        internal const string s_When = "when";
        internal const string s_WithParam = "with-param";
        internal const string s_CaseOrder = "case-order";
        internal const string s_CdataSectionElements = "cdata-section-elements";
        internal const string s_Count = "count";
        internal const string s_DataType = "data-type";
        internal const string s_DecimalSeparator = "decimal-separator";
        internal const string s_Digit = "digit";
        internal const string s_DisableOutputEscaping = "disable-output-escaping";
        internal const string s_DoctypePublic = "doctype-public";
        internal const string s_DoctypeSystem = "doctype-system";
        internal const string s_Elements = "elements";
        internal const string s_Encoding = "encoding";
        internal const string s_ExcludeResultPrefixes = "exclude-result-prefixes";
        internal const string s_ExtensionElementPrefixes = "extension-element-prefixes";
        internal const string s_Format = "format";
        internal const string s_From = "from";
        internal const string s_GroupingSeparator = "grouping-separator";
        internal const string s_GroupingSize = "grouping-size";
        internal const string s_Href = "href";
        internal const string s_Id = "id";
        internal const string s_Indent = "indent";
        internal const string s_Infinity = "infinity";
        internal const string s_Lang = "lang";
        internal const string s_LetterValue = "letter-value";
        internal const string s_Level = "level";
        internal const string s_Match = "match";
        internal const string s_MediaType = "media-type";
        internal const string s_Method = "method";
        internal const string s_MinusSign = "minus-sign";
        internal const string s_Mode = "mode";
        internal const string s_Name = "name";
        internal const string s_Namespace = "namespace";
        internal const string s_NaN = "NaN";
        internal const string s_OmitXmlDeclaration = "omit-xml-declaration";
        internal const string s_Order = "order";
        internal const string s_PatternSeparator = "pattern-separator";
        internal const string s_Percent = "percent";
        internal const string s_PerMille = "per-mille";
        internal const string s_Priority = "priority";
        internal const string s_ResultPrefix = "result-prefix";
        internal const string s_Select = "select";
        internal const string s_Space = "space";
        internal const string s_Standalone = "standalone";
        internal const string s_StylesheetPrefix = "stylesheet-prefix";
        internal const string s_Terminate = "terminate";
        internal const string s_Test = "test";
        internal const string s_Use = "use";
        internal const string s_UseAttributeSets = "use-attribute-sets";
        internal const string s_Value = "value";
        internal const string s_Version = "version";
        internal const string s_ZeroDigit = "zero-digit";
        internal const string s_Alphabetic = "alphabetic";
        internal const string s_Any = "any";
        internal const string s_Ascending = "ascending";
        internal const string s_Descending = "descending";
        internal const string s_HashDefault = "#default";
        internal const string s_Html = "html";
        internal const string s_LowerFirst = "lower-first";
        internal const string s_Multiple = "multiple";
        internal const string s_No = "no";
        internal const string s_Single = "single";
        internal const string s_Traditional = "traditional";
        internal const string s_UpperFirst = "upper-first";
        internal const string s_Xml = "xml";
        internal const string s_Yes = "yes";
        internal const string s_Vendor = "vendor";
        internal const string s_VendorUrl = "vendor-url";

        // extension
        internal const string s_MsXsltNamespace = "urn:schemas-microsoft-com:xslt";
        internal const string s_Script = "script";
        internal const string s_Language = "language";
        internal const string s_ImplementsPrefix = "implements-prefix";


        // string.Empty
        internal string Empty
        {
            get
            {
                CheckKeyword(_AtomEmpty);
                return _AtomEmpty;
            }
        }

        // http://www.w3.org/1999/XSL/Transform
        internal string XsltNamespace
        {
            get
            {
                CheckKeyword(_AtomXsltNamespace);
                return _AtomXsltNamespace;
            }
        }

        // apply-imports
        internal string ApplyImports
        {
            get
            {
                if (_AtomApplyImports == null)
                    _AtomApplyImports = _NameTable.Add(s_ApplyImports);
                CheckKeyword(_AtomApplyImports);
                return _AtomApplyImports;
            }
        }

        // apply-templates
        internal string ApplyTemplates
        {
            get
            {
                CheckKeyword(_AtomApplyTemplates);
                return _AtomApplyTemplates;
            }
        }

        // attribute
        internal string Attribute
        {
            get
            {
                if (_AtomAttribute == null)
                    _AtomAttribute = _NameTable.Add(s_Attribute);
                CheckKeyword(_AtomAttribute);
                return _AtomAttribute;
            }
        }

        // attribute-set
        internal string AttributeSet
        {
            get
            {
                if (_AtomAttributeSet == null)
                    _AtomAttributeSet = _NameTable.Add(s_AttributeSet);
                CheckKeyword(_AtomAttributeSet);
                return _AtomAttributeSet;
            }
        }

        // call-template
        internal string CallTemplate
        {
            get
            {
                if (_AtomCallTemplate == null)
                    _AtomCallTemplate = _NameTable.Add(s_CallTemplate);
                CheckKeyword(_AtomCallTemplate);
                return _AtomCallTemplate;
            }
        }

        // choose
        internal string Choose
        {
            get
            {
                CheckKeyword(_AtomChoose);
                return _AtomChoose;
            }
        }

        // comment
        internal string Comment
        {
            get
            {
                if (_AtomComment == null)
                    _AtomComment = _NameTable.Add(s_Comment);
                CheckKeyword(_AtomComment);
                return _AtomComment;
            }
        }

        // copy
        internal string Copy
        {
            get
            {
                if (_AtomCopy == null)
                    _AtomCopy = _NameTable.Add(s_Copy);
                CheckKeyword(_AtomCopy);
                return _AtomCopy;
            }
        }

        // copy-of
        internal string CopyOf
        {
            get
            {
                if (_AtomCopyOf == null)
                    _AtomCopyOf = _NameTable.Add(s_CopyOf);
                CheckKeyword(_AtomCopyOf);
                return _AtomCopyOf;
            }
        }

        // decimal-format
        internal string DecimalFormat
        {
            get
            {
                if (_AtomDecimalFormat == null)
                    _AtomDecimalFormat = _NameTable.Add(s_DecimalFormat);
                CheckKeyword(_AtomDecimalFormat);
                return _AtomDecimalFormat;
            }
        }

        // element
        internal string Element
        {
            get
            {
                if (_AtomElement == null)
                    _AtomElement = _NameTable.Add(s_Element);
                CheckKeyword(_AtomElement);
                return _AtomElement;
            }
        }

        // fallback
        internal string Fallback
        {
            get
            {
                if (_AtomFallback == null)
                    _AtomFallback = _NameTable.Add(s_Fallback);
                CheckKeyword(_AtomFallback);
                return _AtomFallback;
            }
        }

        // for-each
        internal string ForEach
        {
            get
            {
                CheckKeyword(_AtomForEach);
                return _AtomForEach;
            }
        }

        // if
        internal string If
        {
            get
            {
                CheckKeyword(_AtomIf);
                return _AtomIf;
            }
        }

        // import
        internal string Import
        {
            get
            {
                if (_AtomImport == null)
                    _AtomImport = _NameTable.Add(s_Import);
                CheckKeyword(_AtomImport);
                return _AtomImport;
            }
        }

        // include
        internal string Include
        {
            get
            {
                if (_AtomInclude == null)
                    _AtomInclude = _NameTable.Add(s_Include);
                CheckKeyword(_AtomInclude);
                return _AtomInclude;
            }
        }

        // key
        internal string Key
        {
            get
            {
                if (_AtomKey == null)
                    _AtomKey = _NameTable.Add(s_Key);
                CheckKeyword(_AtomKey);
                return _AtomKey;
            }
        }

        // message
        internal string Message
        {
            get
            {
                if (_AtomMessage == null)
                    _AtomMessage = _NameTable.Add(s_Message);
                CheckKeyword(_AtomMessage);
                return _AtomMessage;
            }
        }

        // namespace-alias
        internal string NamespaceAlias
        {
            get
            {
                if (_AtomNamespaceAlias == null)
                    _AtomNamespaceAlias = _NameTable.Add(s_NamespaceAlias);
                CheckKeyword(_AtomNamespaceAlias);
                return _AtomNamespaceAlias;
            }
        }

        // number
        internal string Number
        {
            get
            {
                if (_AtomNumber == null)
                    _AtomNumber = _NameTable.Add(s_Number);
                CheckKeyword(_AtomNumber);
                return _AtomNumber;
            }
        }

        // otherwise
        internal string Otherwise
        {
            get
            {
                CheckKeyword(_AtomOtherwise);
                return _AtomOtherwise;
            }
        }

        // output
        internal string Output
        {
            get
            {
                if (_AtomOutput == null)
                    _AtomOutput = _NameTable.Add(s_Output);
                CheckKeyword(_AtomOutput);
                return _AtomOutput;
            }
        }

        // param
        internal string Param
        {
            get
            {
                if (_AtomParam == null)
                    _AtomParam = _NameTable.Add(s_Param);
                CheckKeyword(_AtomParam);
                return _AtomParam;
            }
        }

        // preserve-space
        internal string PreserveSpace
        {
            get
            {
                if (_AtomPreserveSpace == null)
                    _AtomPreserveSpace = _NameTable.Add(s_PreserveSpace);
                CheckKeyword(_AtomPreserveSpace);
                return _AtomPreserveSpace;
            }
        }

        // processing-instruction
        internal string ProcessingInstruction
        {
            get
            {
                if (_AtomProcessingInstruction == null)
                    _AtomProcessingInstruction = _NameTable.Add(s_ProcessingInstruction);
                CheckKeyword(_AtomProcessingInstruction);
                return _AtomProcessingInstruction;
            }
        }

        // sort
        internal string Sort
        {
            get
            {
                if (_AtomSort == null)
                    _AtomSort = _NameTable.Add(s_Sort);
                CheckKeyword(_AtomSort);
                return _AtomSort;
            }
        }

        // strip-space
        internal string StripSpace
        {
            get
            {
                if (_AtomStripSpace == null)
                    _AtomStripSpace = _NameTable.Add(s_StripSpace);
                CheckKeyword(_AtomStripSpace);
                return _AtomStripSpace;
            }
        }

        // stylesheet
        internal string Stylesheet
        {
            get
            {
                CheckKeyword(_AtomStylesheet);
                return _AtomStylesheet;
            }
        }

        // template
        internal string Template
        {
            get
            {
                CheckKeyword(_AtomTemplate);
                return _AtomTemplate;
            }
        }

        // text
        internal string Text
        {
            get
            {
                if (_AtomText == null)
                    _AtomText = _NameTable.Add(s_Text);
                CheckKeyword(_AtomText);
                return _AtomText;
            }
        }

        // transform
        internal string Transform
        {
            get
            {
                CheckKeyword(_AtomTransform);
                return _AtomTransform;
            }
        }

        // value-of
        internal string ValueOf
        {
            get
            {
                CheckKeyword(_AtomValueOf);
                return _AtomValueOf;
            }
        }

        // variable
        internal string Variable
        {
            get
            {
                if (_AtomVariable == null)
                    _AtomVariable = _NameTable.Add(s_Variable);
                CheckKeyword(_AtomVariable);
                return _AtomVariable;
            }
        }

        // when
        internal string When
        {
            get
            {
                CheckKeyword(_AtomWhen);
                return _AtomWhen;
            }
        }

        // with-param
        internal string WithParam
        {
            get
            {
                if (_AtomWithParam == null)
                    _AtomWithParam = _NameTable.Add(s_WithParam);
                CheckKeyword(_AtomWithParam);
                return _AtomWithParam;
            }
        }

        // case-order
        internal string CaseOrder
        {
            get
            {
                if (_AtomCaseOrder == null)
                    _AtomCaseOrder = _NameTable.Add(s_CaseOrder);
                CheckKeyword(_AtomCaseOrder);
                return _AtomCaseOrder;
            }
        }

        // cdata-section-elements
        internal string CdataSectionElements
        {
            get
            {
                if (_AtomCdataSectionElements == null)
                    _AtomCdataSectionElements = _NameTable.Add(s_CdataSectionElements);
                CheckKeyword(_AtomCdataSectionElements);
                return _AtomCdataSectionElements;
            }
        }

        // count
        internal string Count
        {
            get
            {
                if (_AtomCount == null)
                    _AtomCount = _NameTable.Add(s_Count);
                CheckKeyword(_AtomCount);
                return _AtomCount;
            }
        }

        // data-type
        internal string DataType
        {
            get
            {
                if (_AtomDataType == null)
                    _AtomDataType = _NameTable.Add(s_DataType);
                CheckKeyword(_AtomDataType);
                return _AtomDataType;
            }
        }

        // decimal-separator
        internal string DecimalSeparator
        {
            get
            {
                if (_AtomDecimalSeparator == null)
                    _AtomDecimalSeparator = _NameTable.Add(s_DecimalSeparator);
                CheckKeyword(_AtomDecimalSeparator);
                return _AtomDecimalSeparator;
            }
        }

        // digit
        internal string Digit
        {
            get
            {
                if (_AtomDigit == null)
                    _AtomDigit = _NameTable.Add(s_Digit);
                CheckKeyword(_AtomDigit);
                return _AtomDigit;
            }
        }

        // disable-output-escaping
        internal string DisableOutputEscaping
        {
            get
            {
                if (_AtomDisableOutputEscaping == null)
                    _AtomDisableOutputEscaping = _NameTable.Add(s_DisableOutputEscaping);
                CheckKeyword(_AtomDisableOutputEscaping);
                return _AtomDisableOutputEscaping;
            }
        }

        // doctype-public
        internal string DoctypePublic
        {
            get
            {
                if (_AtomDoctypePublic == null)
                    _AtomDoctypePublic = _NameTable.Add(s_DoctypePublic);
                CheckKeyword(_AtomDoctypePublic);
                return _AtomDoctypePublic;
            }
        }

        // doctype-system
        internal string DoctypeSystem
        {
            get
            {
                if (_AtomDoctypeSystem == null)
                    _AtomDoctypeSystem = _NameTable.Add(s_DoctypeSystem);
                CheckKeyword(_AtomDoctypeSystem);
                return _AtomDoctypeSystem;
            }
        }

        // elements
        internal string Elements
        {
            get
            {
                if (_AtomElements == null)
                    _AtomElements = _NameTable.Add(s_Elements);
                CheckKeyword(_AtomElements);
                return _AtomElements;
            }
        }

        // encoding
        internal string Encoding
        {
            get
            {
                if (_AtomEncoding == null)
                    _AtomEncoding = _NameTable.Add(s_Encoding);
                CheckKeyword(_AtomEncoding);
                return _AtomEncoding;
            }
        }

        // exclude-result-prefixes
        internal string ExcludeResultPrefixes
        {
            get
            {
                if (_AtomExcludeResultPrefixes == null)
                    _AtomExcludeResultPrefixes = _NameTable.Add(s_ExcludeResultPrefixes);
                CheckKeyword(_AtomExcludeResultPrefixes);
                return _AtomExcludeResultPrefixes;
            }
        }

        // extension-element-prefixes
        internal string ExtensionElementPrefixes
        {
            get
            {
                if (_AtomExtensionElementPrefixes == null)
                    _AtomExtensionElementPrefixes = _NameTable.Add(s_ExtensionElementPrefixes);
                CheckKeyword(_AtomExtensionElementPrefixes);
                return _AtomExtensionElementPrefixes;
            }
        }

        // format
        internal string Format
        {
            get
            {
                if (_AtomFormat == null)
                    _AtomFormat = _NameTable.Add(s_Format);
                CheckKeyword(_AtomFormat);
                return _AtomFormat;
            }
        }

        // from
        internal string From
        {
            get
            {
                if (_AtomFrom == null)
                    _AtomFrom = _NameTable.Add(s_From);
                CheckKeyword(_AtomFrom);
                return _AtomFrom;
            }
        }

        // grouping-separator
        internal string GroupingSeparator
        {
            get
            {
                if (_AtomGroupingSeparator == null)
                    _AtomGroupingSeparator = _NameTable.Add(s_GroupingSeparator);
                CheckKeyword(_AtomGroupingSeparator);
                return _AtomGroupingSeparator;
            }
        }

        // grouping-size
        internal string GroupingSize
        {
            get
            {
                if (_AtomGroupingSize == null)
                    _AtomGroupingSize = _NameTable.Add(s_GroupingSize);
                CheckKeyword(_AtomGroupingSize);
                return _AtomGroupingSize;
            }
        }

        // href
        internal string Href
        {
            get
            {
                if (_AtomHref == null)
                    _AtomHref = _NameTable.Add(s_Href);
                CheckKeyword(_AtomHref);
                return _AtomHref;
            }
        }

        // id
        internal string Id
        {
            get
            {
                if (_AtomId == null)
                    _AtomId = _NameTable.Add(s_Id);
                CheckKeyword(_AtomId);
                return _AtomId;
            }
        }

        // indent
        internal string Indent
        {
            get
            {
                if (_AtomIndent == null)
                    _AtomIndent = _NameTable.Add(s_Indent);
                CheckKeyword(_AtomIndent);
                return _AtomIndent;
            }
        }

        // infinity
        internal string Infinity
        {
            get
            {
                if (_AtomInfinity == null)
                    _AtomInfinity = _NameTable.Add(s_Infinity);
                CheckKeyword(_AtomInfinity);
                return _AtomInfinity;
            }
        }

        // lang
        internal string Lang
        {
            get
            {
                if (_AtomLang == null)
                    _AtomLang = _NameTable.Add(s_Lang);
                CheckKeyword(_AtomLang);
                return _AtomLang;
            }
        }

        // letter-value
        internal string LetterValue
        {
            get
            {
                if (_AtomLetterValue == null)
                    _AtomLetterValue = _NameTable.Add(s_LetterValue);
                CheckKeyword(_AtomLetterValue);
                return _AtomLetterValue;
            }
        }

        // level
        internal string Level
        {
            get
            {
                if (_AtomLevel == null)
                    _AtomLevel = _NameTable.Add(s_Level);
                CheckKeyword(_AtomLevel);
                return _AtomLevel;
            }
        }

        // match
        internal string Match
        {
            get
            {
                CheckKeyword(_AtomMatch);
                return _AtomMatch;
            }
        }

        // media-type
        internal string MediaType
        {
            get
            {
                if (_AtomMediaType == null)
                    _AtomMediaType = _NameTable.Add(s_MediaType);
                CheckKeyword(_AtomMediaType);
                return _AtomMediaType;
            }
        }

        // method
        internal string Method
        {
            get
            {
                if (_AtomMethod == null)
                    _AtomMethod = _NameTable.Add(s_Method);
                CheckKeyword(_AtomMethod);
                return _AtomMethod;
            }
        }

        // minus-sign
        internal string MinusSign
        {
            get
            {
                if (_AtomMinusSign == null)
                    _AtomMinusSign = _NameTable.Add(s_MinusSign);
                CheckKeyword(_AtomMinusSign);
                return _AtomMinusSign;
            }
        }

        // mode
        internal string Mode
        {
            get
            {
                if (_AtomMode == null)
                    _AtomMode = _NameTable.Add(s_Mode);
                CheckKeyword(_AtomMode);
                return _AtomMode;
            }
        }

        // name
        internal string Name
        {
            get
            {
                CheckKeyword(_AtomName);
                return _AtomName;
            }
        }

        // namespace
        internal string Namespace
        {
            get
            {
                if (_AtomNamespace == null)
                    _AtomNamespace = _NameTable.Add(s_Namespace);
                CheckKeyword(_AtomNamespace);
                return _AtomNamespace;
            }
        }

        // NaN
        internal string NaN
        {
            get
            {
                if (_AtomNaN == null)
                    _AtomNaN = _NameTable.Add(s_NaN);
                CheckKeyword(_AtomNaN);
                return _AtomNaN;
            }
        }

        // omit-xml-declaration
        internal string OmitXmlDeclaration
        {
            get
            {
                if (_AtomOmitXmlDeclaration == null)
                    _AtomOmitXmlDeclaration = _NameTable.Add(s_OmitXmlDeclaration);
                CheckKeyword(_AtomOmitXmlDeclaration);
                return _AtomOmitXmlDeclaration;
            }
        }

        // order
        internal string Order
        {
            get
            {
                if (_AtomOrder == null)
                    _AtomOrder = _NameTable.Add(s_Order);
                CheckKeyword(_AtomOrder);
                return _AtomOrder;
            }
        }

        // pattern-separator
        internal string PatternSeparator
        {
            get
            {
                if (_AtomPatternSeparator == null)
                    _AtomPatternSeparator = _NameTable.Add(s_PatternSeparator);
                CheckKeyword(_AtomPatternSeparator);
                return _AtomPatternSeparator;
            }
        }

        // percent
        internal string Percent
        {
            get
            {
                if (_AtomPercent == null)
                    _AtomPercent = _NameTable.Add(s_Percent);
                CheckKeyword(_AtomPercent);
                return _AtomPercent;
            }
        }

        // per-mille
        internal string PerMille
        {
            get
            {
                if (_AtomPerMille == null)
                    _AtomPerMille = _NameTable.Add(s_PerMille);
                CheckKeyword(_AtomPerMille);
                return _AtomPerMille;
            }
        }

        // priority
        internal string Priority
        {
            get
            {
                if (_AtomPriority == null)
                    _AtomPriority = _NameTable.Add(s_Priority);
                CheckKeyword(_AtomPriority);
                return _AtomPriority;
            }
        }

        // result-prefix
        internal string ResultPrefix
        {
            get
            {
                if (_AtomResultPrefix == null)
                    _AtomResultPrefix = _NameTable.Add(s_ResultPrefix);
                CheckKeyword(_AtomResultPrefix);
                return _AtomResultPrefix;
            }
        }

        // select
        internal string Select
        {
            get
            {
                CheckKeyword(_AtomSelect);
                return _AtomSelect;
            }
        }

        // standalone
        internal string Standalone
        {
            get
            {
                if (_AtomStandalone == null)
                    _AtomStandalone = _NameTable.Add(s_Standalone);
                CheckKeyword(_AtomStandalone);
                return _AtomStandalone;
            }
        }

        // stylesheet-prefix
        internal string StylesheetPrefix
        {
            get
            {
                if (_AtomStylesheetPrefix == null)
                    _AtomStylesheetPrefix = _NameTable.Add(s_StylesheetPrefix);
                CheckKeyword(_AtomStylesheetPrefix);
                return _AtomStylesheetPrefix;
            }
        }

        // terminate
        internal string Terminate
        {
            get
            {
                if (_AtomTerminate == null)
                    _AtomTerminate = _NameTable.Add(s_Terminate);
                CheckKeyword(_AtomTerminate);
                return _AtomTerminate;
            }
        }

        // test
        internal string Test
        {
            get
            {
                CheckKeyword(_AtomTest);
                return _AtomTest;
            }
        }

        // use
        internal string Use
        {
            get
            {
                if (_AtomUse == null)
                    _AtomUse = _NameTable.Add(s_Use);
                CheckKeyword(_AtomUse);
                return _AtomUse;
            }
        }

        // use-attribute-sets
        internal string UseAttributeSets
        {
            get
            {
                if (_AtomUseAttributeSets == null)
                    _AtomUseAttributeSets = _NameTable.Add(s_UseAttributeSets);
                CheckKeyword(_AtomUseAttributeSets);
                return _AtomUseAttributeSets;
            }
        }

        // value
        internal string Value
        {
            get
            {
                if (_AtomValue == null)
                    _AtomValue = _NameTable.Add(s_Value);
                CheckKeyword(_AtomValue);
                return _AtomValue;
            }
        }

        // version
        internal string Version
        {
            get
            {
                if (_AtomVersion == null)
                    _AtomVersion = _NameTable.Add(s_Version);
                CheckKeyword(_AtomVersion);
                return _AtomVersion;
            }
        }

        // zero-digit
        internal string ZeroDigit
        {
            get
            {
                if (_AtomZeroDigit == null)
                    _AtomZeroDigit = _NameTable.Add(s_ZeroDigit);
                CheckKeyword(_AtomZeroDigit);
                return _AtomZeroDigit;
            }
        }

        // #default
        internal string HashDefault
        {
            get
            {
                if (_AtomHashDefault == null)
                    _AtomHashDefault = _NameTable.Add(s_HashDefault);
                CheckKeyword(_AtomHashDefault);
                return _AtomHashDefault;
            }
        }

        // no
        internal string No
        {
            get
            {
                if (_AtomNo == null)
                    _AtomNo = _NameTable.Add(s_No);
                CheckKeyword(_AtomNo);
                return _AtomNo;
            }
        }

        // yes
        internal string Yes
        {
            get
            {
                if (_AtomYes == null)
                    _AtomYes = _NameTable.Add(s_Yes);
                CheckKeyword(_AtomYes);
                return _AtomYes;
            }
        }

        //urn:schemas-microsoft-com:xslt
        internal string MsXsltNamespace
        {
            get
            {
                CheckKeyword(_AtomMsXsltNamespace);
                return _AtomMsXsltNamespace;
            }
        }

        // script
        internal string Script
        {
            get
            {
                CheckKeyword(_AtomScript);
                return _AtomScript;
            }
        }

        // language
        internal string Language
        {
            get
            {
                if (_AtomLanguage == null)
                    _AtomLanguage = _NameTable.Add(s_Language);
                CheckKeyword(_AtomLanguage);
                return _AtomLanguage;
            }
        }

        // implements-prefix
        internal string ImplementsPrefix
        {
            get
            {
                if (_AtomImplementsPrefix == null)
                    _AtomImplementsPrefix = _NameTable.Add(s_ImplementsPrefix);
                CheckKeyword(_AtomImplementsPrefix);
                return _AtomImplementsPrefix;
            }
        }

        // Keyword comparison methods
        internal static bool Equals(string strA, string strB)
        {
            Debug.Assert((object)strA == (object)strB || !String.Equals(strA, strB), "String atomization failure");
            return (object)strA == (object)strB;
        }

        internal static bool Compare(string strA, string strB)
        {
            return String.Equals(strA, strB);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void CheckKeyword(string keyword)
        {
            Debug.Assert(keyword != null);
            Debug.Assert((object)keyword == (object)_NameTable.Get(keyword));
        }
    }
}
