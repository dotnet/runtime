// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Speech.Internal.SrgsParser;

namespace System.Speech.Internal.SrgsCompiler
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct CfgSemanticTag
    {
        #region Constructors

        internal CfgSemanticTag(CfgSemanticTag cfgTag)
        {
            _flag1 = cfgTag._flag1;
            _flag2 = cfgTag._flag2;
            _flag3 = cfgTag._flag3;
            _propId = cfgTag._propId;
            _nameOffset = cfgTag._nameOffset;
            _varInt = 0;
            _valueOffset = cfgTag._valueOffset;
            _varDouble = cfgTag._varDouble;

            // Initialize
            StartArcIndex = 0x3FFFFF;
        }

        internal CfgSemanticTag(StringBlob symbols, CfgGrammar.CfgProperty property)
        {
            int iWord;

            _flag1 = _flag2 = _flag3 = 0;
            _valueOffset = 0;
            _varInt = 0;
            _varDouble = 0;

            _propId = property._ulId;
            if (property._pszName != null)
            {
                _nameOffset = symbols.Add(property._pszName, out iWord);
            }
            else
            {
                _nameOffset = 0; // Offset must be zero if no string
            }
#pragma warning disable 0618 // VarEnum is obsolete
            switch (property._comType)
            {
                case 0:
                case VarEnum.VT_BSTR:
                    if (property._comValue != null)
                    {
                        _valueOffset = symbols.Add((string)property._comValue, out iWord);
                    }
                    else
                    {
                        _valueOffset = 0; // Offset must be zero if no string
                    }
                    break;

                case VarEnum.VT_I4:
                    _varInt = (int)property._comValue;
                    break;

                case VarEnum.VT_BOOL:
                    _varInt = (bool)property._comValue ? unchecked(0xffff) : 0;
                    break;

                case VarEnum.VT_R8:
                    _varDouble = (double)property._comValue;
                    break;

                default:
                    System.Diagnostics.Debug.Fail("Unknown Semantic Tag type");
                    break;
            }
#pragma warning restore 0618
            PropVariantType = property._comType;
            ArcIndex = 0;
        }

        #endregion

        #region Internal Properties

        internal uint StartArcIndex
        {
            get
            {
                return _flag1 & 0x3FFFFF;
            }
            set
            {
                if (value > 0x3FFFFF)
                {
                    XmlParser.ThrowSrgsException(SRID.TooManyArcs);
                }

                _flag1 &= ~(uint)0x3FFFFF;
                _flag1 |= value;
            }
        }

        internal uint EndArcIndex
        {
            get
            {
                return _flag2 & 0x3FFFFF;
            }
            set
            {
                if (value > 0x3FFFFF)
                {
                    XmlParser.ThrowSrgsException(SRID.TooManyArcs);
                }

                _flag2 &= ~(uint)0x3FFFFF;
                _flag2 |= value;
            }
        }

#pragma warning disable 0618 // VarEnum is obsolete
        internal VarEnum PropVariantType
        {
            get
            {
                return (VarEnum)(_flag3 & 0xFF);
            }
            set
            {
                uint varType = (uint)value;

                if (varType > 0xFF)
                {
                    XmlParser.ThrowSrgsException(SRID.TooManyArcs);
                }

                _flag3 &= ~(uint)0xFF;
                _flag3 |= varType;
            }
        }
#pragma warning restore 0618

        internal uint ArcIndex
        {
            get
            {
                return (_flag3 >> 8) & 0x3FFFFF;
            }
            set
            {
                if (value > 0x3FFFFF)
                {
                    XmlParser.ThrowSrgsException(SRID.TooManyArcs);
                }

                _flag3 &= ~((uint)0x3FFFFF << 8);
                _flag3 |= value << 8;
            }
        }

        #endregion

        #region Internal Fields

        // Should be in the private section but the order for parameters is key
        [FieldOffset(0)]
        private uint _flag1;

        [FieldOffset(4)]
        private uint _flag2;

        [FieldOffset(8)]
        private uint _flag3;

        [FieldOffset(12)]
        internal int _nameOffset;

        [FieldOffset(16)]
        internal uint _propId;

        [FieldOffset(20)]
        internal int _valueOffset;
        [FieldOffset(24)]
        internal int _varInt;

        [FieldOffset(24)]
        internal double _varDouble;

        #endregion
    }

    [Flags]
    internal enum GrammarOptions
    {
        KeyValuePairs = 0,
        MssV1 = 1,
        KeyValuePairSrgs = 2,
        IpaPhoneme = 4,
        W3cV1 = 8,
        STG = 0x10,

        TagFormat = KeyValuePairs | MssV1 | W3cV1 | KeyValuePairSrgs,
        SemanticInterpretation = MssV1 | W3cV1
    };
}
