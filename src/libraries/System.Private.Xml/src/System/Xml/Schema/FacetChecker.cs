// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;
    using System.Xml.Schema;
    using System.Xml.XPath;
    using System.Diagnostics;
    using System.Collections;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Globalization;

    internal abstract class FacetsChecker
    {
        private struct FacetsCompiler
        {
            private DatatypeImplementation _datatype;
            private RestrictionFacets _derivedRestriction;

            private RestrictionFlags _baseFlags;
            private RestrictionFlags _baseFixedFlags;
            private RestrictionFlags _validRestrictionFlags;

            //Helpers
            private XmlSchemaDatatype _nonNegativeInt;
            private XmlSchemaDatatype _builtInType;
            private XmlTypeCode _builtInEnum;

            private bool _firstPattern;
            private StringBuilder _regStr;
            private XmlSchemaPatternFacet _pattern_facet;

            public FacetsCompiler(DatatypeImplementation baseDatatype, RestrictionFacets restriction)
            {
                _firstPattern = true;
                _regStr = null;
                _pattern_facet = null;
                _datatype = baseDatatype;
                _derivedRestriction = restriction;
                _baseFlags = _datatype.Restriction != null ? _datatype.Restriction.Flags : 0;
                _baseFixedFlags = _datatype.Restriction != null ? _datatype.Restriction.FixedFlags : 0;
                _validRestrictionFlags = _datatype.ValidRestrictionFlags;
                _nonNegativeInt = DatatypeImplementation.GetSimpleTypeFromTypeCode(XmlTypeCode.NonNegativeInteger).Datatype;
                _builtInEnum = !(_datatype is Datatype_union || _datatype is Datatype_List) ? _datatype.TypeCode : 0;
                _builtInType = (int)_builtInEnum > 0 ? DatatypeImplementation.GetSimpleTypeFromTypeCode(_builtInEnum).Datatype : _datatype;
            }

            internal void CompileLengthFacet(XmlSchemaFacet facet)
            {
                CheckProhibitedFlag(facet, RestrictionFlags.Length, SR.Sch_LengthFacetProhibited);
                CheckDupFlag(facet, RestrictionFlags.Length, SR.Sch_DupLengthFacet);
                _derivedRestriction.Length = XmlBaseConverter.DecimalToInt32((decimal)ParseFacetValue(_nonNegativeInt, facet, SR.Sch_LengthFacetInvalid, null, null));

                if ((_baseFixedFlags & RestrictionFlags.Length) != 0)
                {
                    if (!_datatype.IsEqual(_datatype.Restriction.Length, _derivedRestriction.Length))
                    {
                        throw new XmlSchemaException(SR.Sch_FacetBaseFixed, facet);
                    }
                }
                if ((_baseFlags & RestrictionFlags.Length) != 0)
                {
                    if (_datatype.Restriction.Length < _derivedRestriction.Length)
                    {
                        throw new XmlSchemaException(SR.Sch_LengthGtBaseLength, facet);
                    }
                }
                // If the base has the MinLength facet, check that our derived length is not violating it
                if ((_baseFlags & RestrictionFlags.MinLength) != 0)
                {
                    if (_datatype.Restriction.MinLength > _derivedRestriction.Length)
                    {
                        throw new XmlSchemaException(SR.Sch_MaxMinLengthBaseLength, facet);
                    }
                }
                // If the base has the MaxLength facet, check that our derived length is not violating it
                if ((_baseFlags & RestrictionFlags.MaxLength) != 0)
                {
                    if (_datatype.Restriction.MaxLength < _derivedRestriction.Length)
                    {
                        throw new XmlSchemaException(SR.Sch_MaxMinLengthBaseLength, facet);
                    }
                }
                SetFlag(facet, RestrictionFlags.Length);
            }

            internal void CompileMinLengthFacet(XmlSchemaFacet facet)
            {
                CheckProhibitedFlag(facet, RestrictionFlags.MinLength, SR.Sch_MinLengthFacetProhibited);
                CheckDupFlag(facet, RestrictionFlags.MinLength, SR.Sch_DupMinLengthFacet);
                _derivedRestriction.MinLength = XmlBaseConverter.DecimalToInt32((decimal)ParseFacetValue(_nonNegativeInt, facet, SR.Sch_MinLengthFacetInvalid, null, null));

                if ((_baseFixedFlags & RestrictionFlags.MinLength) != 0)
                {
                    if (!_datatype.IsEqual(_datatype.Restriction.MinLength, _derivedRestriction.MinLength))
                    {
                        throw new XmlSchemaException(SR.Sch_FacetBaseFixed, facet);
                    }
                }
                if ((_baseFlags & RestrictionFlags.MinLength) != 0)
                {
                    if (_datatype.Restriction.MinLength > _derivedRestriction.MinLength)
                    {
                        throw new XmlSchemaException(SR.Sch_MinLengthGtBaseMinLength, facet);
                    }
                }
                if ((_baseFlags & RestrictionFlags.Length) != 0)
                {
                    if (_datatype.Restriction.Length < _derivedRestriction.MinLength)
                    {
                        throw new XmlSchemaException(SR.Sch_MaxMinLengthBaseLength, facet);
                    }
                }
                SetFlag(facet, RestrictionFlags.MinLength);
            }

            internal void CompileMaxLengthFacet(XmlSchemaFacet facet)
            {
                CheckProhibitedFlag(facet, RestrictionFlags.MaxLength, SR.Sch_MaxLengthFacetProhibited);
                CheckDupFlag(facet, RestrictionFlags.MaxLength, SR.Sch_DupMaxLengthFacet);
                _derivedRestriction.MaxLength = XmlBaseConverter.DecimalToInt32((decimal)ParseFacetValue(_nonNegativeInt, facet, SR.Sch_MaxLengthFacetInvalid, null, null));

                if ((_baseFixedFlags & RestrictionFlags.MaxLength) != 0)
                {
                    if (!_datatype.IsEqual(_datatype.Restriction.MaxLength, _derivedRestriction.MaxLength))
                    {
                        throw new XmlSchemaException(SR.Sch_FacetBaseFixed, facet);
                    }
                }
                if ((_baseFlags & RestrictionFlags.MaxLength) != 0)
                {
                    if (_datatype.Restriction.MaxLength < _derivedRestriction.MaxLength)
                    {
                        throw new XmlSchemaException(SR.Sch_MaxLengthGtBaseMaxLength, facet);
                    }
                }
                if ((_baseFlags & RestrictionFlags.Length) != 0)
                {
                    if (_datatype.Restriction.Length > _derivedRestriction.MaxLength)
                    {
                        throw new XmlSchemaException(SR.Sch_MaxMinLengthBaseLength, facet);
                    }
                }
                SetFlag(facet, RestrictionFlags.MaxLength);
            }

            internal void CompilePatternFacet(XmlSchemaPatternFacet facet)
            {
                CheckProhibitedFlag(facet, RestrictionFlags.Pattern, SR.Sch_PatternFacetProhibited);
                if (_firstPattern == true)
                {
                    _regStr = new StringBuilder();
                    _regStr.Append("(");
                    _regStr.Append(facet.Value);
                    _pattern_facet = facet;
                    _firstPattern = false;
                }
                else
                {
                    _regStr.Append(")|(");
                    _regStr.Append(facet.Value);
                }
                SetFlag(facet, RestrictionFlags.Pattern);
            }

            internal void CompileEnumerationFacet(XmlSchemaFacet facet, IXmlNamespaceResolver nsmgr, XmlNameTable nameTable)
            {
                CheckProhibitedFlag(facet, RestrictionFlags.Enumeration, SR.Sch_EnumerationFacetProhibited);
                if (_derivedRestriction.Enumeration == null)
                {
                    _derivedRestriction.Enumeration = new ArrayList();
                }
                _derivedRestriction.Enumeration.Add(ParseFacetValue(_datatype, facet, SR.Sch_EnumerationFacetInvalid, nsmgr, nameTable));
                SetFlag(facet, RestrictionFlags.Enumeration);
            }

            internal void CompileWhitespaceFacet(XmlSchemaFacet facet)
            {
                CheckProhibitedFlag(facet, RestrictionFlags.WhiteSpace, SR.Sch_WhiteSpaceFacetProhibited);
                CheckDupFlag(facet, RestrictionFlags.WhiteSpace, SR.Sch_DupWhiteSpaceFacet);
                if (facet.Value == "preserve")
                {
                    _derivedRestriction.WhiteSpace = XmlSchemaWhiteSpace.Preserve;
                }
                else if (facet.Value == "replace")
                {
                    _derivedRestriction.WhiteSpace = XmlSchemaWhiteSpace.Replace;
                }
                else if (facet.Value == "collapse")
                {
                    _derivedRestriction.WhiteSpace = XmlSchemaWhiteSpace.Collapse;
                }
                else
                {
                    throw new XmlSchemaException(SR.Sch_InvalidWhiteSpace, facet.Value, facet);
                }
                if ((_baseFixedFlags & RestrictionFlags.WhiteSpace) != 0)
                {
                    if (!_datatype.IsEqual(_datatype.Restriction.WhiteSpace, _derivedRestriction.WhiteSpace))
                    {
                        throw new XmlSchemaException(SR.Sch_FacetBaseFixed, facet);
                    }
                }
                //Check base and derived whitespace facets
                XmlSchemaWhiteSpace baseWhitespace;
                if ((_baseFlags & RestrictionFlags.WhiteSpace) != 0)
                {
                    baseWhitespace = _datatype.Restriction.WhiteSpace;
                }
                else
                {
                    baseWhitespace = _datatype.BuiltInWhitespaceFacet;
                }
                if (baseWhitespace == XmlSchemaWhiteSpace.Collapse &&
                    (_derivedRestriction.WhiteSpace == XmlSchemaWhiteSpace.Replace || _derivedRestriction.WhiteSpace == XmlSchemaWhiteSpace.Preserve)
                )
                {
                    throw new XmlSchemaException(SR.Sch_WhiteSpaceRestriction1, facet);
                }
                if (baseWhitespace == XmlSchemaWhiteSpace.Replace &&
                    _derivedRestriction.WhiteSpace == XmlSchemaWhiteSpace.Preserve
                )
                {
                    throw new XmlSchemaException(SR.Sch_WhiteSpaceRestriction2, facet);
                }
                SetFlag(facet, RestrictionFlags.WhiteSpace);
            }

            internal void CompileMaxInclusiveFacet(XmlSchemaFacet facet)
            {
                CheckProhibitedFlag(facet, RestrictionFlags.MaxInclusive, SR.Sch_MaxInclusiveFacetProhibited);
                CheckDupFlag(facet, RestrictionFlags.MaxInclusive, SR.Sch_DupMaxInclusiveFacet);
                _derivedRestriction.MaxInclusive = ParseFacetValue(_builtInType, facet, SR.Sch_MaxInclusiveFacetInvalid, null, null);

                if ((_baseFixedFlags & RestrictionFlags.MaxInclusive) != 0)
                {
                    if (!_datatype.IsEqual(_datatype.Restriction.MaxInclusive, _derivedRestriction.MaxInclusive))
                    {
                        throw new XmlSchemaException(SR.Sch_FacetBaseFixed, facet);
                    }
                }
                CheckValue(_derivedRestriction.MaxInclusive, facet);
                SetFlag(facet, RestrictionFlags.MaxInclusive);
            }

            internal void CompileMaxExclusiveFacet(XmlSchemaFacet facet)
            {
                CheckProhibitedFlag(facet, RestrictionFlags.MaxExclusive, SR.Sch_MaxExclusiveFacetProhibited);
                CheckDupFlag(facet, RestrictionFlags.MaxExclusive, SR.Sch_DupMaxExclusiveFacet);
                _derivedRestriction.MaxExclusive = ParseFacetValue(_builtInType, facet, SR.Sch_MaxExclusiveFacetInvalid, null, null);

                if ((_baseFixedFlags & RestrictionFlags.MaxExclusive) != 0)
                {
                    if (!_datatype.IsEqual(_datatype.Restriction.MaxExclusive, _derivedRestriction.MaxExclusive))
                    {
                        throw new XmlSchemaException(SR.Sch_FacetBaseFixed, facet);
                    }
                }
                CheckValue(_derivedRestriction.MaxExclusive, facet);
                SetFlag(facet, RestrictionFlags.MaxExclusive);
            }

            internal void CompileMinInclusiveFacet(XmlSchemaFacet facet)
            {
                CheckProhibitedFlag(facet, RestrictionFlags.MinInclusive, SR.Sch_MinInclusiveFacetProhibited);
                CheckDupFlag(facet, RestrictionFlags.MinInclusive, SR.Sch_DupMinInclusiveFacet);
                _derivedRestriction.MinInclusive = ParseFacetValue(_builtInType, facet, SR.Sch_MinInclusiveFacetInvalid, null, null);

                if ((_baseFixedFlags & RestrictionFlags.MinInclusive) != 0)
                {
                    if (!_datatype.IsEqual(_datatype.Restriction.MinInclusive, _derivedRestriction.MinInclusive))
                    {
                        throw new XmlSchemaException(SR.Sch_FacetBaseFixed, facet);
                    }
                }
                CheckValue(_derivedRestriction.MinInclusive, facet);
                SetFlag(facet, RestrictionFlags.MinInclusive);
            }

            internal void CompileMinExclusiveFacet(XmlSchemaFacet facet)
            {
                CheckProhibitedFlag(facet, RestrictionFlags.MinExclusive, SR.Sch_MinExclusiveFacetProhibited);
                CheckDupFlag(facet, RestrictionFlags.MinExclusive, SR.Sch_DupMinExclusiveFacet);
                _derivedRestriction.MinExclusive = ParseFacetValue(_builtInType, facet, SR.Sch_MinExclusiveFacetInvalid, null, null);

                if ((_baseFixedFlags & RestrictionFlags.MinExclusive) != 0)
                {
                    if (!_datatype.IsEqual(_datatype.Restriction.MinExclusive, _derivedRestriction.MinExclusive))
                    {
                        throw new XmlSchemaException(SR.Sch_FacetBaseFixed, facet);
                    }
                }
                CheckValue(_derivedRestriction.MinExclusive, facet);
                SetFlag(facet, RestrictionFlags.MinExclusive);
            }

            internal void CompileTotalDigitsFacet(XmlSchemaFacet facet)
            {
                CheckProhibitedFlag(facet, RestrictionFlags.TotalDigits, SR.Sch_TotalDigitsFacetProhibited);
                CheckDupFlag(facet, RestrictionFlags.TotalDigits, SR.Sch_DupTotalDigitsFacet);
                XmlSchemaDatatype positiveInt = DatatypeImplementation.GetSimpleTypeFromTypeCode(XmlTypeCode.PositiveInteger).Datatype;
                _derivedRestriction.TotalDigits = XmlBaseConverter.DecimalToInt32((decimal)ParseFacetValue(positiveInt, facet, SR.Sch_TotalDigitsFacetInvalid, null, null));

                if ((_baseFixedFlags & RestrictionFlags.TotalDigits) != 0)
                {
                    if (!_datatype.IsEqual(_datatype.Restriction.TotalDigits, _derivedRestriction.TotalDigits))
                    {
                        throw new XmlSchemaException(SR.Sch_FacetBaseFixed, facet);
                    }
                }
                if ((_baseFlags & RestrictionFlags.TotalDigits) != 0)
                {
                    if (_derivedRestriction.TotalDigits > _datatype.Restriction.TotalDigits)
                    {
                        throw new XmlSchemaException(SR.Sch_TotalDigitsMismatch, string.Empty);
                    }
                }
                SetFlag(facet, RestrictionFlags.TotalDigits);
            }

            internal void CompileFractionDigitsFacet(XmlSchemaFacet facet)
            {
                CheckProhibitedFlag(facet, RestrictionFlags.FractionDigits, SR.Sch_FractionDigitsFacetProhibited);
                CheckDupFlag(facet, RestrictionFlags.FractionDigits, SR.Sch_DupFractionDigitsFacet);
                _derivedRestriction.FractionDigits = XmlBaseConverter.DecimalToInt32((decimal)ParseFacetValue(_nonNegativeInt, facet, SR.Sch_FractionDigitsFacetInvalid, null, null));

                if ((_derivedRestriction.FractionDigits != 0) && (_datatype.TypeCode != XmlTypeCode.Decimal))
                {
                    throw new XmlSchemaException(SR.Sch_FractionDigitsFacetInvalid, SR.Sch_FractionDigitsNotOnDecimal, facet);
                }
                if ((_baseFlags & RestrictionFlags.FractionDigits) != 0)
                {
                    if (_derivedRestriction.FractionDigits > _datatype.Restriction.FractionDigits)
                    {
                        throw new XmlSchemaException(SR.Sch_TotalDigitsMismatch, string.Empty);
                    }
                }
                SetFlag(facet, RestrictionFlags.FractionDigits);
            }

            internal void FinishFacetCompile()
            {
                //Additional check for pattern facet
                //If facet is XMLSchemaPattern, then the String built inside the loop
                //needs to be converted to a RegEx
                if (_firstPattern == false)
                {
                    if (_derivedRestriction.Patterns == null)
                    {
                        _derivedRestriction.Patterns = new ArrayList();
                    }
                    try
                    {
                        _regStr.Append(")");
                        string tempStr = _regStr.ToString();
                        if (tempStr.Contains('|'))
                        { // ordinal compare
                            _regStr.Insert(0, "(");
                            _regStr.Append(")");
                        }
                        _derivedRestriction.Patterns.Add(new Regex(Preprocess(_regStr.ToString()), RegexOptions.None));
                    }
                    catch (Exception e)
                    {
                        throw new XmlSchemaException(SR.Sch_PatternFacetInvalid, new string[] { e.Message }, e, _pattern_facet.SourceUri, _pattern_facet.LineNumber, _pattern_facet.LinePosition, _pattern_facet);
                    }
                }
            }

            private void CheckValue(object value, XmlSchemaFacet facet)
            {
                RestrictionFacets restriction = _datatype.Restriction;
                switch (facet.FacetType)
                {
                    case FacetType.MaxInclusive:
                        if ((_baseFlags & RestrictionFlags.MaxInclusive) != 0)
                        { //Base facet has maxInclusive
                            if (_datatype.Compare(value, restriction.MaxInclusive) > 0)
                            {
                                throw new XmlSchemaException(SR.Sch_MaxInclusiveMismatch, string.Empty);
                            }
                        }
                        if ((_baseFlags & RestrictionFlags.MaxExclusive) != 0)
                        { //Base facet has maxExclusive
                            if (_datatype.Compare(value, restriction.MaxExclusive) >= 0)
                            {
                                throw new XmlSchemaException(SR.Sch_MaxIncExlMismatch, string.Empty);
                            }
                        }
                        break;

                    case FacetType.MaxExclusive:
                        if ((_baseFlags & RestrictionFlags.MaxExclusive) != 0)
                        { //Base facet has maxExclusive
                            if (_datatype.Compare(value, restriction.MaxExclusive) > 0)
                            {
                                throw new XmlSchemaException(SR.Sch_MaxExclusiveMismatch, string.Empty);
                            }
                        }
                        if ((_baseFlags & RestrictionFlags.MaxInclusive) != 0)
                        { //Base facet has maxInclusive
                            if (_datatype.Compare(value, restriction.MaxInclusive) > 0)
                            {
                                throw new XmlSchemaException(SR.Sch_MaxExlIncMismatch, string.Empty);
                            }
                        }
                        break;

                    case FacetType.MinInclusive:
                        if ((_baseFlags & RestrictionFlags.MinInclusive) != 0)
                        { //Base facet has minInclusive
                            if (_datatype.Compare(value, restriction.MinInclusive) < 0)
                            {
                                throw new XmlSchemaException(SR.Sch_MinInclusiveMismatch, string.Empty);
                            }
                        }
                        if ((_baseFlags & RestrictionFlags.MinExclusive) != 0)
                        { //Base facet has minExclusive
                            if (_datatype.Compare(value, restriction.MinExclusive) < 0)
                            {
                                throw new XmlSchemaException(SR.Sch_MinIncExlMismatch, string.Empty);
                            }
                        }
                        if ((_baseFlags & RestrictionFlags.MaxExclusive) != 0)
                        { //Base facet has maxExclusive
                            if (_datatype.Compare(value, restriction.MaxExclusive) >= 0)
                            {
                                throw new XmlSchemaException(SR.Sch_MinIncMaxExlMismatch, string.Empty);
                            }
                        }
                        break;

                    case FacetType.MinExclusive:
                        if ((_baseFlags & RestrictionFlags.MinExclusive) != 0)
                        { //Base facet has minExclusive
                            if (_datatype.Compare(value, restriction.MinExclusive) < 0)
                            {
                                throw new XmlSchemaException(SR.Sch_MinExclusiveMismatch, string.Empty);
                            }
                        }
                        if ((_baseFlags & RestrictionFlags.MinInclusive) != 0)
                        { //Base facet has minInclusive
                            if (_datatype.Compare(value, restriction.MinInclusive) < 0)
                            {
                                throw new XmlSchemaException(SR.Sch_MinExlIncMismatch, string.Empty);
                            }
                        }
                        if ((_baseFlags & RestrictionFlags.MaxExclusive) != 0)
                        { //Base facet has maxExclusive
                            if (_datatype.Compare(value, restriction.MaxExclusive) >= 0)
                            {
                                throw new XmlSchemaException(SR.Sch_MinExlMaxExlMismatch, string.Empty);
                            }
                        }
                        break;

                    default:
                        Debug.Fail($"Unexpected facet type {facet.FacetType}");
                        break;
                }
            }

            internal void CompileFacetCombinations()
            {
                RestrictionFacets baseRestriction = _datatype.Restriction;
                //They are not allowed on the same type but allowed on derived types.
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.MaxInclusive) != 0 &&
                    (_derivedRestriction.Flags & RestrictionFlags.MaxExclusive) != 0
                )
                {
                    throw new XmlSchemaException(SR.Sch_MaxInclusiveExclusive, string.Empty);
                }
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.MinInclusive) != 0 &&
                    (_derivedRestriction.Flags & RestrictionFlags.MinExclusive) != 0
                )
                {
                    throw new XmlSchemaException(SR.Sch_MinInclusiveExclusive, string.Empty);
                }
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.Length) != 0 &&
                    (_derivedRestriction.Flags & (RestrictionFlags.MinLength | RestrictionFlags.MaxLength)) != 0
                )
                {
                    throw new XmlSchemaException(SR.Sch_LengthAndMinMax, string.Empty);
                }

                CopyFacetsFromBaseType();

                // Check combinations
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.MinLength) != 0 &&
                    (_derivedRestriction.Flags & RestrictionFlags.MaxLength) != 0
                )
                {
                    if (_derivedRestriction.MinLength > _derivedRestriction.MaxLength)
                    {
                        throw new XmlSchemaException(SR.Sch_MinLengthGtMaxLength, string.Empty);
                    }
                }
                
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.MinInclusive) != 0 &&
                    (_derivedRestriction.Flags & RestrictionFlags.MaxInclusive) != 0
                )
                {
                    if (_datatype.Compare(_derivedRestriction.MinInclusive, _derivedRestriction.MaxInclusive) > 0)
                    {
                        throw new XmlSchemaException(SR.Sch_MinInclusiveGtMaxInclusive, string.Empty);
                    }
                }
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.MinInclusive) != 0 &&
                    (_derivedRestriction.Flags & RestrictionFlags.MaxExclusive) != 0
                )
                {
                    if (_datatype.Compare(_derivedRestriction.MinInclusive, _derivedRestriction.MaxExclusive) > 0)
                    {
                        throw new XmlSchemaException(SR.Sch_MinInclusiveGtMaxExclusive, string.Empty);
                    }
                }
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.MinExclusive) != 0 &&
                    (_derivedRestriction.Flags & RestrictionFlags.MaxExclusive) != 0
                )
                {
                    if (_datatype.Compare(_derivedRestriction.MinExclusive, _derivedRestriction.MaxExclusive) > 0)
                    {
                        throw new XmlSchemaException(SR.Sch_MinExclusiveGtMaxExclusive, string.Empty);
                    }
                }
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.MinExclusive) != 0 &&
                    (_derivedRestriction.Flags & RestrictionFlags.MaxInclusive) != 0
                )
                {
                    if (_datatype.Compare(_derivedRestriction.MinExclusive, _derivedRestriction.MaxInclusive) > 0)
                    {
                        throw new XmlSchemaException(SR.Sch_MinExclusiveGtMaxInclusive, string.Empty);
                    }
                }
                if ((_derivedRestriction.Flags & (RestrictionFlags.TotalDigits | RestrictionFlags.FractionDigits)) == (RestrictionFlags.TotalDigits | RestrictionFlags.FractionDigits))
                {
                    if (_derivedRestriction.FractionDigits > _derivedRestriction.TotalDigits)
                    {
                        throw new XmlSchemaException(SR.Sch_FractionDigitsGtTotalDigits, string.Empty);
                    }
                }
            }

            private void CopyFacetsFromBaseType()
            {
                RestrictionFacets baseRestriction = _datatype.Restriction;
                // Copy additional facets from the base type
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.Length) == 0 &&
                    (_baseFlags & RestrictionFlags.Length) != 0
                )
                {
                    _derivedRestriction.Length = baseRestriction.Length;
                    SetFlag(RestrictionFlags.Length);
                }
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.MinLength) == 0 &&
                    (_baseFlags & RestrictionFlags.MinLength) != 0
                )
                {
                    _derivedRestriction.MinLength = baseRestriction.MinLength;
                    SetFlag(RestrictionFlags.MinLength);
                }
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.MaxLength) == 0 &&
                    (_baseFlags & RestrictionFlags.MaxLength) != 0
                )
                {
                    _derivedRestriction.MaxLength = baseRestriction.MaxLength;
                    SetFlag(RestrictionFlags.MaxLength);
                }
                if ((_baseFlags & RestrictionFlags.Pattern) != 0)
                {
                    if (_derivedRestriction.Patterns == null)
                    {
                        _derivedRestriction.Patterns = baseRestriction.Patterns;
                    }
                    else
                    {
                        _derivedRestriction.Patterns.AddRange(baseRestriction.Patterns);
                    }
                    SetFlag(RestrictionFlags.Pattern);
                }

                if ((_baseFlags & RestrictionFlags.Enumeration) != 0)
                {
                    if (_derivedRestriction.Enumeration == null)
                    {
                        _derivedRestriction.Enumeration = baseRestriction.Enumeration;
                    }
                    SetFlag(RestrictionFlags.Enumeration);
                }

                if (
                    (_derivedRestriction.Flags & RestrictionFlags.WhiteSpace) == 0 &&
                    (_baseFlags & RestrictionFlags.WhiteSpace) != 0
                )
                {
                    _derivedRestriction.WhiteSpace = baseRestriction.WhiteSpace;
                    SetFlag(RestrictionFlags.WhiteSpace);
                }
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.MaxInclusive) == 0 &&
                    (_baseFlags & RestrictionFlags.MaxInclusive) != 0
                )
                {
                    _derivedRestriction.MaxInclusive = baseRestriction.MaxInclusive;
                    SetFlag(RestrictionFlags.MaxInclusive);
                }
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.MaxExclusive) == 0 &&
                    (_baseFlags & RestrictionFlags.MaxExclusive) != 0
                )
                {
                    _derivedRestriction.MaxExclusive = baseRestriction.MaxExclusive;
                    SetFlag(RestrictionFlags.MaxExclusive);
                }
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.MinInclusive) == 0 &&
                    (_baseFlags & RestrictionFlags.MinInclusive) != 0
                )
                {
                    _derivedRestriction.MinInclusive = baseRestriction.MinInclusive;
                    SetFlag(RestrictionFlags.MinInclusive);
                }
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.MinExclusive) == 0 &&
                    (_baseFlags & RestrictionFlags.MinExclusive) != 0
                )
                {
                    _derivedRestriction.MinExclusive = baseRestriction.MinExclusive;
                    SetFlag(RestrictionFlags.MinExclusive);
                }
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.TotalDigits) == 0 &&
                    (_baseFlags & RestrictionFlags.TotalDigits) != 0
                )
                {
                    _derivedRestriction.TotalDigits = baseRestriction.TotalDigits;
                    SetFlag(RestrictionFlags.TotalDigits);
                }
                if (
                    (_derivedRestriction.Flags & RestrictionFlags.FractionDigits) == 0 &&
                    (_baseFlags & RestrictionFlags.FractionDigits) != 0
                )
                {
                    _derivedRestriction.FractionDigits = baseRestriction.FractionDigits;
                    SetFlag(RestrictionFlags.FractionDigits);
                }
            }

            private object ParseFacetValue(XmlSchemaDatatype datatype, XmlSchemaFacet facet, string code, IXmlNamespaceResolver nsmgr, XmlNameTable nameTable)
            {
                object typedValue;
                Exception ex = datatype.TryParseValue(facet.Value, nameTable, nsmgr, out typedValue);
                if (ex == null)
                {
                    return typedValue;
                }
                else
                {
                    throw new XmlSchemaException(code, new string[] { ex.Message }, ex, facet.SourceUri, facet.LineNumber, facet.LinePosition, facet);
                }
            }

            private struct Map
            {
                internal Map(char m, string r)
                {
                    match = m;
                    replacement = r;
                }
                internal char match;
                internal string replacement;
            };

            private static readonly Map[] s_map = {
            new Map('c', "\\p{_xmlC}"),
            new Map('C', "\\P{_xmlC}"),
            new Map('d', "\\p{_xmlD}"),
            new Map('D', "\\P{_xmlD}"),
            new Map('i', "\\p{_xmlI}"),
            new Map('I', "\\P{_xmlI}"),
            new Map('w', "\\p{_xmlW}"),
            new Map('W', "\\P{_xmlW}"),
        };
            private static string Preprocess(string pattern)
            {
                StringBuilder bufBld = new StringBuilder();
                bufBld.Append("^");

                char[] source = pattern.ToCharArray();
                int length = pattern.Length;
                int copyPosition = 0;
                for (int position = 0; position < length - 2; position++)
                {
                    if (source[position] == '\\')
                    {
                        if (source[position + 1] == '\\')
                        {
                            position++; // skip it
                        }
                        else
                        {
                            char ch = source[position + 1];
                            for (int i = 0; i < s_map.Length; i++)
                            {
                                if (s_map[i].match == ch)
                                {
                                    if (copyPosition < position)
                                    {
                                        bufBld.Append(source, copyPosition, position - copyPosition);
                                    }
                                    bufBld.Append(s_map[i].replacement);
                                    position++;
                                    copyPosition = position + 1;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (copyPosition < length)
                {
                    bufBld.Append(source, copyPosition, length - copyPosition);
                }

                bufBld.Append("$");
                return bufBld.ToString();
            }

            private void CheckProhibitedFlag(XmlSchemaFacet facet, RestrictionFlags flag, string errorCode)
            {
                if ((_validRestrictionFlags & flag) == 0)
                {
                    throw new XmlSchemaException(errorCode, _datatype.TypeCodeString, facet);
                }
            }

            private void CheckDupFlag(XmlSchemaFacet facet, RestrictionFlags flag, string errorCode)
            {
                if ((_derivedRestriction.Flags & flag) != 0)
                {
                    throw new XmlSchemaException(errorCode, facet);
                }
            }

            private void SetFlag(XmlSchemaFacet facet, RestrictionFlags flag)
            {
                _derivedRestriction.Flags |= flag;
                if (facet.IsFixed)
                {
                    _derivedRestriction.FixedFlags |= flag;
                }
            }

            private void SetFlag(RestrictionFlags flag)
            {
                _derivedRestriction.Flags |= flag;
                if ((_baseFixedFlags & flag) != 0)
                {
                    _derivedRestriction.FixedFlags |= flag;
                }
            }
        }

        internal virtual Exception CheckLexicalFacets(ref string parseString, XmlSchemaDatatype datatype)
        {
            CheckWhitespaceFacets(ref parseString, datatype);
            return CheckPatternFacets(datatype.Restriction, parseString);
        }
        internal virtual Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
        {
            return null;
        }
        internal virtual Exception CheckValueFacets(decimal value, XmlSchemaDatatype datatype)
        {
            return null;
        }
        internal virtual Exception CheckValueFacets(long value, XmlSchemaDatatype datatype)
        {
            return null;
        }
        internal virtual Exception CheckValueFacets(int value, XmlSchemaDatatype datatype)
        {
            return null;
        }
        internal virtual Exception CheckValueFacets(short value, XmlSchemaDatatype datatype)
        {
            return null;
        }
        internal virtual Exception CheckValueFacets(DateTime value, XmlSchemaDatatype datatype)
        {
            return null;
        }
        internal virtual Exception CheckValueFacets(double value, XmlSchemaDatatype datatype)
        {
            return null;
        }
        internal virtual Exception CheckValueFacets(float value, XmlSchemaDatatype datatype)
        {
            return null;
        }
        internal virtual Exception CheckValueFacets(string value, XmlSchemaDatatype datatype)
        {
            return null;
        }
        internal virtual Exception CheckValueFacets(byte[] value, XmlSchemaDatatype datatype)
        {
            return null;
        }
        internal virtual Exception CheckValueFacets(TimeSpan value, XmlSchemaDatatype datatype)
        {
            return null;
        }
        internal virtual Exception CheckValueFacets(XmlQualifiedName value, XmlSchemaDatatype datatype)
        {
            return null;
        }

        internal void CheckWhitespaceFacets(ref string s, XmlSchemaDatatype datatype)
        {
            // before parsing, check whitespace facet
            RestrictionFacets restriction = datatype.Restriction;

            switch (datatype.Variety)
            {
                case XmlSchemaDatatypeVariety.List:
                    s = s.Trim();
                    break;

                case XmlSchemaDatatypeVariety.Atomic:
                    if (datatype.BuiltInWhitespaceFacet == XmlSchemaWhiteSpace.Collapse)
                    {
                        s = XmlComplianceUtil.NonCDataNormalize(s);
                    }
                    else if (datatype.BuiltInWhitespaceFacet == XmlSchemaWhiteSpace.Replace)
                    {
                        s = XmlComplianceUtil.CDataNormalize(s);
                    }
                    else if (restriction != null && (restriction.Flags & RestrictionFlags.WhiteSpace) != 0)
                    { //Restriction has whitespace facet specified
                        if (restriction.WhiteSpace == XmlSchemaWhiteSpace.Replace)
                        {
                            s = XmlComplianceUtil.CDataNormalize(s);
                        }
                        else if (restriction.WhiteSpace == XmlSchemaWhiteSpace.Collapse)
                        {
                            s = XmlComplianceUtil.NonCDataNormalize(s);
                        }
                    }
                    break;

                default:
                    break;
            }
        }
        internal Exception CheckPatternFacets(RestrictionFacets restriction, string value)
        {
            if (restriction != null && (restriction.Flags & RestrictionFlags.Pattern) != 0)
            {
                for (int i = 0; i < restriction.Patterns.Count; ++i)
                {
                    Regex regex = (Regex)restriction.Patterns[i];
                    if (!regex.IsMatch(value))
                    {
                        return new XmlSchemaException(SR.Sch_PatternConstraintFailed, string.Empty);
                    }
                }
            }
            return null;
        }

        internal virtual bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            return false;
        }

        //Compile-time Facet Checking
        internal virtual RestrictionFacets ConstructRestriction(DatatypeImplementation datatype, XmlSchemaObjectCollection facets, XmlNameTable nameTable)
        {
            //Datatype is the type on which this method is called
            RestrictionFacets derivedRestriction = new RestrictionFacets();
            FacetsCompiler facetCompiler = new FacetsCompiler(datatype, derivedRestriction);

            for (int i = 0; i < facets.Count; ++i)
            {
                XmlSchemaFacet facet = (XmlSchemaFacet)facets[i];
                if (facet.Value == null)
                {
                    throw new XmlSchemaException(SR.Sch_InvalidFacet, facet);
                }
                IXmlNamespaceResolver nsmgr = new SchemaNamespaceManager(facet);
                switch (facet.FacetType)
                {
                    case FacetType.Length:
                        facetCompiler.CompileLengthFacet(facet);
                        break;

                    case FacetType.MinLength:
                        facetCompiler.CompileMinLengthFacet(facet);
                        break;

                    case FacetType.MaxLength:
                        facetCompiler.CompileMaxLengthFacet(facet);
                        break;

                    case FacetType.Pattern:
                        facetCompiler.CompilePatternFacet(facet as XmlSchemaPatternFacet);
                        break;

                    case FacetType.Enumeration:
                        facetCompiler.CompileEnumerationFacet(facet, nsmgr, nameTable);
                        break;

                    case FacetType.Whitespace:
                        facetCompiler.CompileWhitespaceFacet(facet);
                        break;

                    case FacetType.MinInclusive:
                        facetCompiler.CompileMinInclusiveFacet(facet);
                        break;

                    case FacetType.MinExclusive:
                        facetCompiler.CompileMinExclusiveFacet(facet);
                        break;

                    case FacetType.MaxInclusive:
                        facetCompiler.CompileMaxInclusiveFacet(facet);
                        break;

                    case FacetType.MaxExclusive:
                        facetCompiler.CompileMaxExclusiveFacet(facet);
                        break;

                    case FacetType.TotalDigits:
                        facetCompiler.CompileTotalDigitsFacet(facet);
                        break;

                    case FacetType.FractionDigits:
                        facetCompiler.CompileFractionDigitsFacet(facet);
                        break;

                    default:
                        throw new XmlSchemaException(SR.Sch_UnknownFacet, facet);
                }
            }
            facetCompiler.FinishFacetCompile();
            facetCompiler.CompileFacetCombinations();
            return derivedRestriction;
        }





        internal static decimal Power(int x, int y)
        {
            //Returns X raised to the power Y
            decimal returnValue = 1m;
            decimal decimalValue = (decimal)x;
            if (y > 28)
            { //CLR decimal cannot handle more than 29 digits (10 power 28.)
                return decimal.MaxValue;
            }
            for (int i = 0; i < y; i++)
            {
                returnValue = returnValue * decimalValue;
            }
            return returnValue;
        }
    }


    internal class Numeric10FacetsChecker : FacetsChecker
    {
        private static readonly char[] s_signs = new char[] { '+', '-' };
        private decimal _maxValue;
        private decimal _minValue;

        internal Numeric10FacetsChecker(decimal minVal, decimal maxVal)
        {
            _minValue = minVal;
            _maxValue = maxVal;
        }

        internal override Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
        {
            decimal decimalValue = datatype.ValueConverter.ToDecimal(value);
            return CheckValueFacets(decimalValue, datatype);
        }

        internal override Exception CheckValueFacets(decimal value, XmlSchemaDatatype datatype)
        {
            RestrictionFacets restriction = datatype.Restriction;
            RestrictionFlags flags = restriction != null ? restriction.Flags : 0;
            XmlValueConverter valueConverter = datatype.ValueConverter;

            //Check built-in facets
            if (value > _maxValue || value < _minValue)
            {
                return new OverflowException(SR.Format(SR.XmlConvert_Overflow, value.ToString(CultureInfo.InvariantCulture), datatype.TypeCodeString));
            }
            //Check user-defined facets
            if (flags != 0)
            {
                if ((flags & RestrictionFlags.MaxInclusive) != 0)
                {
                    if (value > valueConverter.ToDecimal(restriction.MaxInclusive))
                    {
                        return new XmlSchemaException(SR.Sch_MaxInclusiveConstraintFailed, string.Empty);
                    }
                }

                if ((flags & RestrictionFlags.MaxExclusive) != 0)
                {
                    if (value >= valueConverter.ToDecimal(restriction.MaxExclusive))
                    {
                        return new XmlSchemaException(SR.Sch_MaxExclusiveConstraintFailed, string.Empty);
                    }
                }

                if ((flags & RestrictionFlags.MinInclusive) != 0)
                {
                    if (value < valueConverter.ToDecimal(restriction.MinInclusive))
                    {
                        return new XmlSchemaException(SR.Sch_MinInclusiveConstraintFailed, string.Empty);
                    }
                }

                if ((flags & RestrictionFlags.MinExclusive) != 0)
                {
                    if (value <= valueConverter.ToDecimal(restriction.MinExclusive))
                    {
                        return new XmlSchemaException(SR.Sch_MinExclusiveConstraintFailed, string.Empty);
                    }
                }
                if ((flags & RestrictionFlags.Enumeration) != 0)
                {
                    if (!MatchEnumeration(value, restriction.Enumeration, valueConverter))
                    {
                        return new XmlSchemaException(SR.Sch_EnumerationConstraintFailed, string.Empty);
                    }
                }
                return CheckTotalAndFractionDigits(value, restriction.TotalDigits, restriction.FractionDigits, ((flags & RestrictionFlags.TotalDigits) != 0), ((flags & RestrictionFlags.FractionDigits) != 0));
            }
            return null;
        }

        internal override Exception CheckValueFacets(long value, XmlSchemaDatatype datatype)
        {
            decimal decimalValue = (decimal)value;
            return CheckValueFacets(decimalValue, datatype);
        }

        internal override Exception CheckValueFacets(int value, XmlSchemaDatatype datatype)
        {
            decimal decimalValue = (decimal)value;
            return CheckValueFacets(decimalValue, datatype);
        }
        internal override Exception CheckValueFacets(short value, XmlSchemaDatatype datatype)
        {
            decimal decimalValue = (decimal)value;
            return CheckValueFacets(decimalValue, datatype);
        }
        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            return MatchEnumeration(datatype.ValueConverter.ToDecimal(value), enumeration, datatype.ValueConverter);
        }

        internal bool MatchEnumeration(decimal value, ArrayList enumeration, XmlValueConverter valueConverter)
        {
            for (int i = 0; i < enumeration.Count; ++i)
            {
                if (value == valueConverter.ToDecimal(enumeration[i]))
                {
                    return true;
                }
            }
            return false;
        }
        internal Exception CheckTotalAndFractionDigits(decimal value, int totalDigits, int fractionDigits, bool checkTotal, bool checkFraction)
        {
            decimal maxValue = FacetsChecker.Power(10, totalDigits) - 1; //(decimal)Math.Pow(10, totalDigits) - 1 ;
            int powerCnt = 0;
            if (value < 0)
            {
                value = decimal.Negate(value); //Need to compare maxValue allowed against the absolute value
            }
            while (decimal.Truncate(value) != value)
            { //Till it has a fraction
                value = value * 10;
                powerCnt++;
            }

            if (checkTotal && (value > maxValue || powerCnt > totalDigits))
            {
                return new XmlSchemaException(SR.Sch_TotalDigitsConstraintFailed, string.Empty);
            }
            if (checkFraction && powerCnt > fractionDigits)
            {
                return new XmlSchemaException(SR.Sch_FractionDigitsConstraintFailed, string.Empty);
            }
            return null;
        }
    }


    internal class Numeric2FacetsChecker : FacetsChecker
    {
        internal override Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
        {
            double doubleValue = datatype.ValueConverter.ToDouble(value);
            return CheckValueFacets(doubleValue, datatype);
        }

        internal override Exception CheckValueFacets(double value, XmlSchemaDatatype datatype)
        {
            RestrictionFacets restriction = datatype.Restriction;
            RestrictionFlags flags = restriction != null ? restriction.Flags : 0;
            XmlValueConverter valueConverter = datatype.ValueConverter;

            if ((flags & RestrictionFlags.MaxInclusive) != 0)
            {
                if (value > valueConverter.ToDouble(restriction.MaxInclusive))
                {
                    return new XmlSchemaException(SR.Sch_MaxInclusiveConstraintFailed, string.Empty);
                }
            }
            if ((flags & RestrictionFlags.MaxExclusive) != 0)
            {
                if (value >= valueConverter.ToDouble(restriction.MaxExclusive))
                {
                    return new XmlSchemaException(SR.Sch_MaxExclusiveConstraintFailed, string.Empty);
                }
            }

            if ((flags & RestrictionFlags.MinInclusive) != 0)
            {
                if (value < (valueConverter.ToDouble(restriction.MinInclusive)))
                {
                    return new XmlSchemaException(SR.Sch_MinInclusiveConstraintFailed, string.Empty);
                }
            }

            if ((flags & RestrictionFlags.MinExclusive) != 0)
            {
                if (value <= valueConverter.ToDouble(restriction.MinExclusive))
                {
                    return new XmlSchemaException(SR.Sch_MinExclusiveConstraintFailed, string.Empty);
                }
            }
            if ((flags & RestrictionFlags.Enumeration) != 0)
            {
                if (!MatchEnumeration(value, restriction.Enumeration, valueConverter))
                {
                    return new XmlSchemaException(SR.Sch_EnumerationConstraintFailed, string.Empty);
                }
            }
            return null;
        }

        internal override Exception CheckValueFacets(float value, XmlSchemaDatatype datatype)
        {
            double doubleValue = (double)value;
            return CheckValueFacets(doubleValue, datatype);
        }
        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            return MatchEnumeration(datatype.ValueConverter.ToDouble(value), enumeration, datatype.ValueConverter);
        }
        private bool MatchEnumeration(double value, ArrayList enumeration, XmlValueConverter valueConverter)
        {
            for (int i = 0; i < enumeration.Count; ++i)
            {
                if (value == valueConverter.ToDouble(enumeration[i]))
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal class DurationFacetsChecker : FacetsChecker
    {
        internal override Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
        {
            TimeSpan timeSpanValue = (TimeSpan)datatype.ValueConverter.ChangeType(value, typeof(TimeSpan));
            return CheckValueFacets(timeSpanValue, datatype);
        }

        internal override Exception CheckValueFacets(TimeSpan value, XmlSchemaDatatype datatype)
        {
            RestrictionFacets restriction = datatype.Restriction;
            RestrictionFlags flags = restriction != null ? restriction.Flags : 0;

            if ((flags & RestrictionFlags.MaxInclusive) != 0)
            {
                if (TimeSpan.Compare(value, (TimeSpan)restriction.MaxInclusive) > 0)
                {
                    return new XmlSchemaException(SR.Sch_MaxInclusiveConstraintFailed, string.Empty);
                }
            }

            if ((flags & RestrictionFlags.MaxExclusive) != 0)
            {
                if (TimeSpan.Compare(value, (TimeSpan)restriction.MaxExclusive) >= 0)
                {
                    return new XmlSchemaException(SR.Sch_MaxExclusiveConstraintFailed, string.Empty);
                }
            }

            if ((flags & RestrictionFlags.MinInclusive) != 0)
            {
                if (TimeSpan.Compare(value, (TimeSpan)restriction.MinInclusive) < 0)
                {
                    return new XmlSchemaException(SR.Sch_MinInclusiveConstraintFailed, string.Empty);
                }
            }

            if ((flags & RestrictionFlags.MinExclusive) != 0)
            {
                if (TimeSpan.Compare(value, (TimeSpan)restriction.MinExclusive) <= 0)
                {
                    return new XmlSchemaException(SR.Sch_MinExclusiveConstraintFailed, string.Empty);
                }
            }
            if ((flags & RestrictionFlags.Enumeration) != 0)
            {
                if (!MatchEnumeration(value, restriction.Enumeration))
                {
                    return new XmlSchemaException(SR.Sch_EnumerationConstraintFailed, string.Empty);
                }
            }
            return null;
        }
        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            return MatchEnumeration((TimeSpan)value, enumeration);
        }

        private bool MatchEnumeration(TimeSpan value, ArrayList enumeration)
        {
            for (int i = 0; i < enumeration.Count; ++i)
            {
                if (TimeSpan.Compare(value, (TimeSpan)enumeration[i]) == 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal class DateTimeFacetsChecker : FacetsChecker
    {
        internal override Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
        {
            DateTime dateTimeValue = datatype.ValueConverter.ToDateTime(value);
            return CheckValueFacets(dateTimeValue, datatype);
        }

        internal override Exception CheckValueFacets(DateTime value, XmlSchemaDatatype datatype)
        {
            RestrictionFacets restriction = datatype.Restriction;
            RestrictionFlags flags = restriction != null ? restriction.Flags : 0;

            if ((flags & RestrictionFlags.MaxInclusive) != 0)
            {
                if (datatype.Compare(value, (DateTime)restriction.MaxInclusive) > 0)
                {
                    return new XmlSchemaException(SR.Sch_MaxInclusiveConstraintFailed, string.Empty);
                }
            }

            if ((flags & RestrictionFlags.MaxExclusive) != 0)
            {
                if (datatype.Compare(value, (DateTime)restriction.MaxExclusive) >= 0)
                {
                    return new XmlSchemaException(SR.Sch_MaxExclusiveConstraintFailed, string.Empty);
                }
            }

            if ((flags & RestrictionFlags.MinInclusive) != 0)
            {
                if (datatype.Compare(value, (DateTime)restriction.MinInclusive) < 0)
                {
                    return new XmlSchemaException(SR.Sch_MinInclusiveConstraintFailed, string.Empty);
                }
            }

            if ((flags & RestrictionFlags.MinExclusive) != 0)
            {
                if (datatype.Compare(value, (DateTime)restriction.MinExclusive) <= 0)
                {
                    return new XmlSchemaException(SR.Sch_MinExclusiveConstraintFailed, string.Empty);
                }
            }
            if ((flags & RestrictionFlags.Enumeration) != 0)
            {
                if (!MatchEnumeration(value, restriction.Enumeration, datatype))
                {
                    return new XmlSchemaException(SR.Sch_EnumerationConstraintFailed, string.Empty);
                }
            }
            return null;
        }

        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            return MatchEnumeration(datatype.ValueConverter.ToDateTime(value), enumeration, datatype);
        }

        private bool MatchEnumeration(DateTime value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            for (int i = 0; i < enumeration.Count; ++i)
            {
                if (datatype.Compare(value, (DateTime)enumeration[i]) == 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal class StringFacetsChecker : FacetsChecker
    { //All types derived from string & anyURI
        private static Regex s_languagePattern;

        private static Regex LanguagePattern
        {
            get
            {
                if (s_languagePattern == null)
                {
                    Regex langRegex = new Regex("^([a-zA-Z]{1,8})(-[a-zA-Z0-9]{1,8})*$", RegexOptions.None);
                    Interlocked.CompareExchange(ref s_languagePattern, langRegex, null);
                }
                return s_languagePattern;
            }
        }

        internal override Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
        {
            string stringValue = datatype.ValueConverter.ToString(value);
            return CheckValueFacets(stringValue, datatype, true);
        }

        internal override Exception CheckValueFacets(string value, XmlSchemaDatatype datatype)
        {
            return CheckValueFacets(value, datatype, true);
        }

        internal Exception CheckValueFacets(string value, XmlSchemaDatatype datatype, bool verifyUri)
        {
            //Length, MinLength, MaxLength
            int length = value.Length;
            RestrictionFacets restriction = datatype.Restriction;
            RestrictionFlags flags = restriction != null ? restriction.Flags : 0;
            Exception exception;

            exception = CheckBuiltInFacets(value, datatype.TypeCode, verifyUri);
            if (exception != null) return exception;

            if (flags != 0)
            {
                if ((flags & RestrictionFlags.Length) != 0)
                {
                    if (restriction.Length != length)
                    {
                        return new XmlSchemaException(SR.Sch_LengthConstraintFailed, string.Empty);
                    }
                }
                if ((flags & RestrictionFlags.MinLength) != 0)
                {
                    if (length < restriction.MinLength)
                    {
                        return new XmlSchemaException(SR.Sch_MinLengthConstraintFailed, string.Empty);
                    }
                }
                if ((flags & RestrictionFlags.MaxLength) != 0)
                {
                    if (restriction.MaxLength < length)
                    {
                        return new XmlSchemaException(SR.Sch_MaxLengthConstraintFailed, string.Empty);
                    }
                }
                if ((flags & RestrictionFlags.Enumeration) != 0)
                {
                    if (!MatchEnumeration(value, restriction.Enumeration, datatype))
                    {
                        return new XmlSchemaException(SR.Sch_EnumerationConstraintFailed, string.Empty);
                    }
                }
            }
            return null;
        }

        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            return MatchEnumeration(datatype.ValueConverter.ToString(value), enumeration, datatype);
        }

        private bool MatchEnumeration(string value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            if (datatype.TypeCode == XmlTypeCode.AnyUri)
            {
                for (int i = 0; i < enumeration.Count; ++i)
                {
                    if (value.Equals(((Uri)enumeration[i]).OriginalString))
                    {
                        return true;
                    }
                }
            }
            else
            {
                for (int i = 0; i < enumeration.Count; ++i)
                {
                    if (value.Equals((string)enumeration[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private Exception CheckBuiltInFacets(string s, XmlTypeCode typeCode, bool verifyUri)
        {
            Exception exception = null;

            switch (typeCode)
            {
                case XmlTypeCode.AnyUri:
                    if (verifyUri)
                    {
                        Uri uri;
                        exception = XmlConvert.TryToUri(s, out uri);
                    }
                    break;

                case XmlTypeCode.NormalizedString:
                    exception = XmlConvert.TryVerifyNormalizedString(s);
                    break;

                case XmlTypeCode.Token:
                    exception = XmlConvert.TryVerifyTOKEN(s);
                    break;

                case XmlTypeCode.Language:
                    if (s == null || s.Length == 0)
                    {
                        return new XmlSchemaException(SR.Sch_EmptyAttributeValue, string.Empty);
                    }
                    if (!LanguagePattern.IsMatch(s))
                    {
                        return new XmlSchemaException(SR.Sch_InvalidLanguageId, string.Empty);
                    }
                    break;

                case XmlTypeCode.NmToken:
                    exception = XmlConvert.TryVerifyNMTOKEN(s);
                    break;

                case XmlTypeCode.Name:
                    exception = XmlConvert.TryVerifyName(s);
                    break;

                case XmlTypeCode.NCName:
                case XmlTypeCode.Id:
                case XmlTypeCode.Idref:
                case XmlTypeCode.Entity:
                    exception = XmlConvert.TryVerifyNCName(s);
                    break;
                default:
                    break;
            }
            return exception;
        }
    }

    internal class QNameFacetsChecker : FacetsChecker
    {
        internal override Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
        {
            XmlQualifiedName qualifiedNameValue = (XmlQualifiedName)datatype.ValueConverter.ChangeType(value, typeof(XmlQualifiedName));
            return CheckValueFacets(qualifiedNameValue, datatype);
        }

        internal override Exception CheckValueFacets(XmlQualifiedName value, XmlSchemaDatatype datatype)
        {
            RestrictionFacets restriction = datatype.Restriction;
            RestrictionFlags flags = restriction != null ? restriction.Flags : 0;
            if (flags != 0)
            { //If there are facets defined
                string strValue = value.ToString();
                int length = strValue.Length;
                if ((flags & RestrictionFlags.Length) != 0)
                {
                    if (restriction.Length != length)
                    {
                        return new XmlSchemaException(SR.Sch_LengthConstraintFailed, string.Empty);
                    }
                }
                if ((flags & RestrictionFlags.MinLength) != 0)
                {
                    if (length < restriction.MinLength)
                    {
                        return new XmlSchemaException(SR.Sch_MinLengthConstraintFailed, string.Empty);
                    }
                }
                if ((flags & RestrictionFlags.MaxLength) != 0)
                {
                    if (restriction.MaxLength < length)
                    {
                        return new XmlSchemaException(SR.Sch_MaxLengthConstraintFailed, string.Empty);
                    }
                }
                if ((flags & RestrictionFlags.Enumeration) != 0)
                {
                    if (!MatchEnumeration(value, restriction.Enumeration))
                    {
                        return new XmlSchemaException(SR.Sch_EnumerationConstraintFailed, string.Empty);
                    }
                }
            }
            return null;
        }
        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            return MatchEnumeration((XmlQualifiedName)datatype.ValueConverter.ChangeType(value, typeof(XmlQualifiedName)), enumeration);
        }

        private bool MatchEnumeration(XmlQualifiedName value, ArrayList enumeration)
        {
            for (int i = 0; i < enumeration.Count; ++i)
            {
                if (value.Equals((XmlQualifiedName)enumeration[i]))
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal class MiscFacetsChecker : FacetsChecker
    { //For bool, anySimpleType
    }

    internal class BinaryFacetsChecker : FacetsChecker
    { //hexBinary & Base64Binary
        internal override Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
        {
            byte[] byteArrayValue = (byte[])value;
            return CheckValueFacets(byteArrayValue, datatype);
        }

        internal override Exception CheckValueFacets(byte[] value, XmlSchemaDatatype datatype)
        {
            //Length, MinLength, MaxLength
            RestrictionFacets restriction = datatype.Restriction;
            int length = value.Length;
            RestrictionFlags flags = restriction != null ? restriction.Flags : 0;
            if (flags != 0)
            { //if it has facets defined
                if ((flags & RestrictionFlags.Length) != 0)
                {
                    if (restriction.Length != length)
                    {
                        return new XmlSchemaException(SR.Sch_LengthConstraintFailed, string.Empty);
                    }
                }
                if ((flags & RestrictionFlags.MinLength) != 0)
                {
                    if (length < restriction.MinLength)
                    {
                        return new XmlSchemaException(SR.Sch_MinLengthConstraintFailed, string.Empty);
                    }
                }
                if ((flags & RestrictionFlags.MaxLength) != 0)
                {
                    if (restriction.MaxLength < length)
                    {
                        return new XmlSchemaException(SR.Sch_MaxLengthConstraintFailed, string.Empty);
                    }
                }
                if ((flags & RestrictionFlags.Enumeration) != 0)
                {
                    if (!MatchEnumeration(value, restriction.Enumeration, datatype))
                    {
                        return new XmlSchemaException(SR.Sch_EnumerationConstraintFailed, string.Empty);
                    }
                }
            }
            return null;
        }
        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            return MatchEnumeration((byte[])value, enumeration, datatype);
        }

        private bool MatchEnumeration(byte[] value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            for (int i = 0; i < enumeration.Count; ++i)
            {
                if (datatype.Compare(value, (byte[])enumeration[i]) == 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal class ListFacetsChecker : FacetsChecker
    {
        internal override Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
        {
            //Check for facets allowed on lists - Length, MinLength, MaxLength
            Array values = value as Array;
            Debug.Assert(values != null);

            RestrictionFacets restriction = datatype.Restriction;
            RestrictionFlags flags = restriction != null ? restriction.Flags : 0;

            if ((flags & (RestrictionFlags.Length | RestrictionFlags.MinLength | RestrictionFlags.MaxLength)) != 0)
            {
                int length = values.Length;
                if ((flags & RestrictionFlags.Length) != 0)
                {
                    if (restriction.Length != length)
                    {
                        return new XmlSchemaException(SR.Sch_LengthConstraintFailed, string.Empty);
                    }
                }

                if ((flags & RestrictionFlags.MinLength) != 0)
                {
                    if (length < restriction.MinLength)
                    {
                        return new XmlSchemaException(SR.Sch_MinLengthConstraintFailed, string.Empty);
                    }
                }

                if ((flags & RestrictionFlags.MaxLength) != 0)
                {
                    if (restriction.MaxLength < length)
                    {
                        return new XmlSchemaException(SR.Sch_MaxLengthConstraintFailed, string.Empty);
                    }
                }
            }
            if ((flags & RestrictionFlags.Enumeration) != 0)
            {
                if (!MatchEnumeration(value, restriction.Enumeration, datatype))
                {
                    return new XmlSchemaException(SR.Sch_EnumerationConstraintFailed, string.Empty);
                }
            }
            return null;
        }

        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            for (int i = 0; i < enumeration.Count; ++i)
            {
                if (datatype.Compare(value, enumeration[i]) == 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal class UnionFacetsChecker : FacetsChecker
    {
        internal override Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
        {
            RestrictionFacets restriction = datatype.Restriction;
            RestrictionFlags flags = restriction != null ? restriction.Flags : 0;

            if ((flags & RestrictionFlags.Enumeration) != 0)
            {
                if (!MatchEnumeration(value, restriction.Enumeration, datatype))
                {
                    return new XmlSchemaException(SR.Sch_EnumerationConstraintFailed, string.Empty);
                }
            }
            return null;
        }

        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            for (int i = 0; i < enumeration.Count; ++i)
            {
                if (datatype.Compare(value, enumeration[i]) == 0)
                { //Compare on Datatype_union will compare two XsdSimpleValue
                    return true;
                }
            }
            return false;
        }
    }
}
