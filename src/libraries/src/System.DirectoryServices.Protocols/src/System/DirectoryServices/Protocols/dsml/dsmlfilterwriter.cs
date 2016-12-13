// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Collections;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.Xml;
    using System.Diagnostics;

    internal class DSMLFilterWriter
    {
        protected void WriteValue(string valueElt, ADValue value, XmlWriter mXmlWriter, string strNamespace)
        {
            if (strNamespace != null)
            {
                mXmlWriter.WriteStartElement(valueElt, strNamespace);
            }
            else
            {
                mXmlWriter.WriteStartElement(valueElt);
            }

            if (value.IsBinary && value.BinaryVal != null)
            {
                mXmlWriter.WriteAttributeString("xsi", "type", DsmlConstants.XsiUri,
                    DsmlConstants.AttrBinaryTypePrefixedValue);
                mXmlWriter.WriteBase64(value.BinaryVal, 0, value.BinaryVal.Length);
            }
            else
            {
                //note that the WriteString method handles a null argument correctly
                mXmlWriter.WriteString(value.StringVal);
            }

            mXmlWriter.WriteEndElement();
        }

        protected void WriteAttrib(string attrName, ADAttribute attrib, XmlWriter mXmlWriter, string strNamespace)
        {
            if (strNamespace != null)
            {
                mXmlWriter.WriteStartElement(attrName, strNamespace);
            }
            else
            {
                mXmlWriter.WriteStartElement(attrName);
            }

            mXmlWriter.WriteAttributeString(DsmlConstants.AttrDsmlAttrName, attrib.Name);

            foreach (ADValue val in attrib.Values)
            {
                WriteValue(DsmlConstants.ElementDsmlAttrValue, val, mXmlWriter, strNamespace);
            }

            mXmlWriter.WriteEndElement();
        }

        public void WriteFilter(ADFilter filter, bool filterTags, XmlWriter mXmlWriter, string strNamespace)
        {
            if (filterTags)
            {
                if (strNamespace != null)
                {
                    mXmlWriter.WriteStartElement(DsmlConstants.ElementSearchReqFilter, strNamespace);
                }
                else
                {
                    mXmlWriter.WriteStartElement(DsmlConstants.ElementSearchReqFilter);
                }
            }

            switch (filter.Type)
            {
                case ADFilter.FilterType.And:
                    if (strNamespace != null)
                    {
                        mXmlWriter.WriteStartElement(DsmlConstants.ElementSearchReqFilterAnd, strNamespace);
                    }
                    else
                    {
                        mXmlWriter.WriteStartElement(DsmlConstants.ElementSearchReqFilterAnd);
                    }

                    foreach (object andClause in filter.Filter.And)
                    {
                        WriteFilter((ADFilter)andClause, false, mXmlWriter, strNamespace);
                    }
                    mXmlWriter.WriteEndElement();
                    break;
                case ADFilter.FilterType.Or:
                    if (strNamespace != null)
                    {
                        mXmlWriter.WriteStartElement(DsmlConstants.ElementSearchReqFilterOr, strNamespace);
                    }
                    else
                    {
                        mXmlWriter.WriteStartElement(DsmlConstants.ElementSearchReqFilterOr);
                    }

                    foreach (object orClause in filter.Filter.Or)
                    {
                        WriteFilter((ADFilter)orClause, false, mXmlWriter, strNamespace);
                    }
                    mXmlWriter.WriteEndElement();
                    break;
                case ADFilter.FilterType.Not:
                    if (strNamespace != null)
                    {
                        mXmlWriter.WriteStartElement(DsmlConstants.ElementSearchReqFilterNot, strNamespace);
                    }
                    else
                    {
                        mXmlWriter.WriteStartElement(DsmlConstants.ElementSearchReqFilterNot);
                    }

                    WriteFilter(filter.Filter.Not, false, mXmlWriter, strNamespace);
                    mXmlWriter.WriteEndElement();
                    break;
                case ADFilter.FilterType.EqualityMatch:
                    WriteAttrib(DsmlConstants.ElementSearchReqFilterEqual,
                        filter.Filter.EqualityMatch, mXmlWriter, strNamespace);
                    break;
                case ADFilter.FilterType.Present:
                    if (strNamespace != null)
                    {
                        mXmlWriter.WriteStartElement(DsmlConstants.ElementSearchReqFilterPresent, strNamespace);
                    }
                    else
                    {
                        mXmlWriter.WriteStartElement(DsmlConstants.ElementSearchReqFilterPresent);
                    }
                    mXmlWriter.WriteAttributeString(DsmlConstants.AttrSearchReqFilterPresentName,
                                                    filter.Filter.Present);
                    mXmlWriter.WriteEndElement();
                    break;
                case ADFilter.FilterType.GreaterOrEqual:
                    WriteAttrib(DsmlConstants.ElementSearchReqFilterGrteq,
                        filter.Filter.GreaterOrEqual, mXmlWriter, strNamespace);
                    break;
                case ADFilter.FilterType.LessOrEqual:
                    WriteAttrib(DsmlConstants.ElementSearchReqFilterLesseq,
                        filter.Filter.LessOrEqual, mXmlWriter, strNamespace);
                    break;
                case ADFilter.FilterType.ApproxMatch:
                    WriteAttrib(DsmlConstants.ElementSearchReqFilterApprox,
                        filter.Filter.ApproxMatch, mXmlWriter, strNamespace);
                    break;
                case ADFilter.FilterType.ExtensibleMatch:
                    ADExtenMatchFilter exten = filter.Filter.ExtensibleMatch;

                    if (strNamespace != null)
                    {
                        mXmlWriter.WriteStartElement(DsmlConstants.ElementSearchReqFilterExtenmatch, strNamespace);
                    }
                    else
                    {
                        mXmlWriter.WriteStartElement(DsmlConstants.ElementSearchReqFilterExtenmatch);
                    }

                    if ((exten.Name != null) && (exten.Name.Length != 0))
                    {
                        mXmlWriter.WriteAttributeString(
                            DsmlConstants.AttrSearchReqFilterExtenmatchName, exten.Name);
                    }

                    if ((exten.MatchingRule != null) && (exten.MatchingRule.Length != 0))
                    {
                        mXmlWriter.WriteAttributeString(
                            DsmlConstants.AttrSearchReqFilterExtenmatchMatchrule, exten.MatchingRule);
                    }

                    mXmlWriter.WriteAttributeString(
                        DsmlConstants.AttrSearchReqFilterExtenmatchDnattr,
                        XmlConvert.ToString(exten.DNAttributes));

                    WriteValue(DsmlConstants.ElementSearchReqFilterExtenmatchValue, exten.Value, mXmlWriter, strNamespace);
                    mXmlWriter.WriteEndElement();
                    break;
                case ADFilter.FilterType.Substrings:
                    //handle <substrings>
                    ADSubstringFilter substr = filter.Filter.Substrings;

                    if (strNamespace != null)
                    {
                        mXmlWriter.WriteStartElement(DsmlConstants.ElementSearchReqFilterSubstr, strNamespace);
                    }
                    else
                    {
                        mXmlWriter.WriteStartElement(DsmlConstants.ElementSearchReqFilterSubstr);
                    }

                    mXmlWriter.WriteAttributeString(DsmlConstants.AttrSearchReqFilterSubstrName,
                        substr.Name);

                    if (substr.Initial != null)
                    {
                        WriteValue(DsmlConstants.ElementSearchReqFilterSubstrInit,
                            substr.Initial, mXmlWriter, strNamespace);
                    }

                    if (substr.Any != null)
                    {
                        foreach (object sub in substr.Any)
                        {
                            WriteValue(DsmlConstants.ElementSearchReqFilterSubstrAny,
                                (ADValue)sub, mXmlWriter, strNamespace);
                        }
                    }

                    if (substr.Final != null)
                    {
                        WriteValue(DsmlConstants.ElementSearchReqFilterSubstrFinal,
                            substr.Final, mXmlWriter, strNamespace);
                    }

                    mXmlWriter.WriteEndElement();
                    break;

                default:
                    Debug.Fail("Invalid substring filter");

                    //just in case ... this will be caught by the caller 
                    throw new ArgumentException(Res.GetString(Res.InvalidFilterType, filter.Type));
            }

            if (filterTags)
            {
                mXmlWriter.WriteEndElement();     //</filter>
            }
        }
    }
}
