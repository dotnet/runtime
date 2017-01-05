// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Utility class to perform conversion of an string representation of 
    /// na LDAP filter to ADFilter.
    /// </summary>
    internal class FilterParser
    {
        public static ADFilter ParseFilterString(string filter)
        {
            Debug.Assert(filter != null);

            //perfomr the matthing. On success all info we are interested in 
            //will be stored in the named captures.
            try
            {
                Match m = s_mFilter.Match(filter);
                if (!m.Success)
                {
                    return null;
                }

                ADFilter result = new ADFilter();
                if (m.Groups["item"].ToString().Length != 0)
                {
                    //we have am "item"filter which can be 
                    //of type "present", "simple", "substring", or "extensible" 

                    if (m.Groups["present"].ToString().Length != 0)
                    {
                        //present filter, e.g. (objectClass=*)
                        result.Type = ADFilter.FilterType.Present;

                        Debug.Assert(m.Groups["presentattr"].ToString().Length != 0);
                        result.Filter.Present = m.Groups["presentattr"].ToString();
                    }
                    else if (m.Groups["simple"].ToString().Length != 0)
                    {
                        //simple filter, e.g.  (simpleattr =|~=|>=|<= simplevalue)
                        ADAttribute simple = new ADAttribute();

                        if (m.Groups["simplevalue"].ToString().Length != 0)
                        {
                            ADValue val = StringFilterValueToADValue(m.Groups["simplevalue"].ToString());
                            simple.Values.Add(val);
                        }

                        simple.Name = m.Groups["simpleattr"].ToString();

                        //the variours types of relationships we might have 
                        switch (m.Groups["filtertype"].ToString())
                        {
                            case "=":
                                result.Type = ADFilter.FilterType.EqualityMatch;
                                result.Filter.EqualityMatch = simple;
                                break;
                            case "~=":
                                result.Type = ADFilter.FilterType.ApproxMatch;
                                result.Filter.ApproxMatch = simple;
                                break;
                            case "<=":
                                result.Type = ADFilter.FilterType.LessOrEqual;
                                result.Filter.LessOrEqual = simple;
                                break;
                            case ">=":
                                result.Type = ADFilter.FilterType.GreaterOrEqual;
                                result.Filter.GreaterOrEqual = simple;
                                break;
                            default:
                                //this should not occur 
                                Debug.Fail("FilterParser.ParseFilterString: Invalid simple filter");

                                //treat like a parse error 
                                return null;
                        }
                    }
                    else if (m.Groups["substr"].ToString().Length != 0)
                    {
                        //we have a substring filter. Get the various parts of it 
                        result.Type = ADFilter.FilterType.Substrings;

                        ADSubstringFilter substr = new ADSubstringFilter();

                        substr.Initial = StringFilterValueToADValue(m.Groups["initialvalue"].ToString());
                        substr.Final = StringFilterValueToADValue(m.Groups["finalvalue"].ToString());

                        if (m.Groups["anyvalue"].ToString().Length != 0)
                        {
                            foreach (Capture c in m.Groups["anyvalue"].Captures)
                            {
                                substr.Any.Add(StringFilterValueToADValue(c.ToString()));
                            }
                        }

                        substr.Name = m.Groups["substrattr"].ToString();
                        result.Filter.Substrings = substr;
                    }
                    else if (m.Groups["extensible"].ToString().Length != 0)
                    {
                        //extensible filter
                        result.Type = ADFilter.FilterType.ExtensibleMatch;

                        ADExtenMatchFilter exten = new ADExtenMatchFilter();

                        exten.Value = StringFilterValueToADValue(m.Groups["extenvalue"].ToString());
                        exten.DNAttributes = (m.Groups["dnattr"].ToString().Length != 0);
                        exten.Name = m.Groups["extenattr"].ToString();
                        exten.MatchingRule = m.Groups["matchrule"].ToString();

                        result.Filter.ExtensibleMatch = exten;
                    }
                    else
                    {
                        //this should not occur 
                        Debug.Fail("Invalid item filter");

                        //treat like a parse error 
                        return null;
                    }
                }
                else
                {
                    //compound recursive filter

                    //extract the filter lists 
                    ArrayList filters = new ArrayList();
                    string filterList = m.Groups["filterlist"].ToString().Trim();

                    while (filterList.Length > 0)
                    {
                        if (filterList[0] != '(')
                        {
                            //this is a parse error: invalid filter
                            return null;
                        }

                        int strIdx = 1;
                        int left = 1;       //count opening brackest
                        bool gotSubfilter = false;

                        while (strIdx < filterList.Length && !gotSubfilter)
                        {
                            if (filterList[strIdx] == '(')
                            {
                                left++;
                            }

                            if (filterList[strIdx] == ')')
                            {
                                if (left < 1)
                                {
                                    //unbalanced parenthesis 
                                    return null;
                                }
                                else if (left == 1)
                                {
                                    //the end of the subfilter
                                    gotSubfilter = true;
                                }
                                else
                                {
                                    left--;
                                }
                            }

                            strIdx++;
                        }

                        if (!gotSubfilter)
                        {
                            //the filter list did not consist entirely of subfilters
                            return null;
                        }

                        filters.Add(filterList.Substring(0, strIdx));
                        filterList = filterList.Substring(strIdx).TrimStart();
                    }

                    ADFilter eltFilter = null;
                    switch (m.Groups["filtercomp"].ToString())
                    {
                        case "|":
                            result.Type = ADFilter.FilterType.Or;
                            result.Filter.Or = new ArrayList();
                            foreach (String f in filters)
                            {
                                eltFilter = ParseFilterString(f);
                                if (eltFilter == null)
                                {
                                    return null;
                                }

                                result.Filter.Or.Add(eltFilter);
                            }

                            if (result.Filter.Or.Count < 1)
                            {
                                return null;
                            }

                            break;
                        case "&":
                            result.Type = ADFilter.FilterType.And;
                            result.Filter.And = new ArrayList();
                            foreach (String f in filters)
                            {
                                eltFilter = ParseFilterString(f);
                                if (eltFilter == null)
                                {
                                    return null;
                                }

                                result.Filter.And.Add(eltFilter);
                            }

                            if (result.Filter.And.Count < 1)
                            {
                                return null;
                            }

                            break;
                        case "!":
                            result.Type = ADFilter.FilterType.Not;
                            eltFilter = ParseFilterString((string)filters[0]);
                            //Note that for ease of defining the filter grammar we allow 
                            //more than one filter after '!'. We catch this here 
                            if (filters.Count > 1 || eltFilter == null)
                            {
                                return null;
                            }

                            result.Filter.Not = eltFilter;
                            break;

                        default:
                            //this should not occur 
                            Debug.Fail("Invalid filter composition");

                            //treat like a parse error 
                            return null;
                    }
                }

                return result;
            }//end of try
            catch (RegexMatchTimeoutException)
            {
                Debug.WriteLine("The input filter String: {0} exceeded the regex match timeout of {1} seconds.", filter, mFilterTimeOutInSeconds);
                return null;
            }
        }

        /// <summary>
        /// Converts the string representation of a filter value to the appropriate ADvalue
        /// </summary>
        /// <remarks>
        /// If at least 1 binary escaping (e.g. \20) exists in the string representation
        /// then the value is converted to binary. Any string portions are converted 
        /// to binary UTF8 encoding.
        /// </remarks>
        /// <param name="strVal"> String representation of the filter. </param>
        /// <returns>Returns a properly initialized ADValue </returns>
        protected static ADValue StringFilterValueToADValue(string strVal)
        {
            if (strVal == null || strVal.Length == 0)
            {
                return null;
            }

            ADValue val = new ADValue();

            //The idea is that if we have at least 1 escaped binary value 
            // like \20 we will convert the entire value to binary 
            // something like \30va\2c\lue will be converted as follows:
            // 30,UTF8 binary representation of "va", 2c, UTF8 bin representation of "lue"
            String[] parts = strVal.Split(new char[] { '\\' });

            if (parts.Length == 1)
            {
                //we have no binary data in the value
                val.IsBinary = false;
                val.StringVal = strVal;
                val.BinaryVal = null;
            }
            else
            {
                ArrayList binChunks = new ArrayList(parts.Length);
                UTF8Encoding utf8 = new UTF8Encoding();

                //there is at least 1 binary value
                //for something like "\30va\2c\lue" we will have 
                //parts = {"", "30va", "2c", "lue"}
                val.IsBinary = true;
                val.StringVal = null;

                if (parts[0].Length != 0)
                {
                    //parts[0] is either empty of doesn't have 2 char hex prefix
                    binChunks.Add(utf8.GetBytes(parts[0]));
                }

                for (int i = 1; i < parts.Length; i++)
                {
                    //we must have a 2 character hex prefix 
                    Debug.Assert(parts[i].Length >= 2,
                        "FilterParser.ProcessStringFilterValue: Unexpected value. " +
                        "The the value matching regular expression must be incorrect");

                    string hexPrefix = parts[i].Substring(0, 2);

                    //handle the prefix 
                    binChunks.Add(new Byte[] { Byte.Parse(hexPrefix, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture) });

                    if (parts[i].Length > 2)
                    {
                        //handle the string portion
                        binChunks.Add(utf8.GetBytes(parts[i].Substring(2)));
                    }
                }

                //now we have all the binary chunks. Put them together
                //figure out the size we need
                int lenghtNeeded = 0;
                foreach (Byte[] chunk in binChunks)
                {
                    lenghtNeeded += chunk.Length;
                }

                val.BinaryVal = new Byte[lenghtNeeded];

                //do the actual copying
                int currIdx = 0;
                foreach (Byte[] chunk in binChunks)
                {
                    chunk.CopyTo(val.BinaryVal, currIdx);
                    currIdx += chunk.Length;
                }
            }

            return val;
        }

        //The filter timeout in seconds. Necessary to avoid ReDoS attacks on the regexes used in this filter. 
        //If no match was found within the timeout value, the pattern-matching method times out and RegexMatchTimeoutException is thrown. 
        //Timeout value of 3 seconds should be sufficient for most filter strings.
        private const UInt32 mFilterTimeOutInSeconds = 3;

        //the filter regEx that does most of the work for us
        private static Regex s_mFilter = new Regex(mFilterRE, RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(mFilterTimeOutInSeconds));

        //The filter grammar
        //This is nothing more but a regular expression representation of the LDAP filter
        //grammar in RFC2254
        //
        //filter     = "(" filtercomp ")"
        //filtercomp = and / or / not / item
        //and        = "&" filterlist
        //or         = "|" filterlist
        //not        = "!" filter
        //filterlist = 1*filter
        //item       = simple / present / substring / extensible
        //simple     = attr filtertype value
        //filtertype = equal / approx / greater / less
        //equal      = "="
        //approx     = "~="
        //greater    = ">="
        //less       = "<="
        //extensible = attr [":dn"] [":" matchingrule] ":=" value
        //                / [":dn"] ":" matchingrule ":=" value
        //present    = attr "=*"
        //substring  = attr "=" [initial] any [final]
        //initial    = value
        //any        = "*" *(value "*")
        //final      = value
        //attr       = AttributeDescription from Section 4.1.5 of RFC2251
        //matchingrule = MatchingRuleId from Section 4.1.9 of RFC2251
        //value      = AttributeValue from Section 4.1.6 of RFC2251

        //In order to allow whitespacing between elements and to avoid more \s* elements 
        //then necessary we always put '\s*' on the RIGHT of any element. The only exception is 
        //the the filter element which allows for white space on both sides.

        private const string mAttrRE = @"(([0-2](\.[0-9]+)+)|([a-zA-Z]+([a-zA-Z0-9]|[-])*))(;([a-zA-Z0-9]|[-])+)*";
        private const string mValueRE = @"(([^\*\(\)\\])|(\\[a-fA-F0-9][a-fA-F0-9]))+?";

        //extensible filter grammar
        private const string mExtenAttrRE = @"(?<extenattr>" + mAttrRE + @")\s*";
        private const string mExtenValueRE = @"(?<extenvalue>" + mValueRE + @")\s*";
        private const string mDNAttrRE = @"(?<dnattr>\:dn){0,1}\s*";

        //matc rule grammar
        private const string mMatchRuleOptionalRE = @"(\:(?<matchrule>([a-zA-Z][a-zA-Z0-9]*)|([0-9]+(\.[0-9]+)+))){0,1}\s*";
        private const string mMatchRuleRE = @"(\:(?<matchrule>([a-zA-Z][a-zA-Z0-9]*)|([0-9]+(\.[0-9]+)+)))\s*";
        private const string mExtenRE = @"(?<extensible>((" + mExtenAttrRE + mDNAttrRE + mMatchRuleOptionalRE + @")|" +
                                                      @"(" + mDNAttrRE + mMatchRuleRE + @"))\:\=\s*" + mExtenValueRE + @")\s*";

        // substr filter grammar 
        private const string mSubstrAttrRE = @"(?<substrattr>" + mAttrRE + @")\s*";
        private const string mInitialRE = @"\s*(?<initialvalue>" + mValueRE + @"){0,1}\s*";
        private const string mFinalRE = @"\s*(?<finalvalue>" + mValueRE + @"){0,1}\s*";
        private const string mAnyRE = @"(\*\s*((?<anyvalue>" + mValueRE + @")\*\s*)*)";
        private const string mSubstrRE = @"(?<substr>" + mSubstrAttrRE + @"\=\s*" +
                             mInitialRE + mAnyRE + mFinalRE + @")\s*";

        // simple filter grammar
        private const string mSimpleValueRE = @"(?<simplevalue>" + mValueRE + @")\s*";
        private const string mSimpleAttrRE = @"(?<simpleattr>" + mAttrRE + @")\s*";
        private const string mFiltertypeRE = @"(?<filtertype>\=|\~\=|\>\=|\<\=)\s*";
        private const string mSimpleRE = @"(?<simple>" + mSimpleAttrRE + mFiltertypeRE + mSimpleValueRE + @")\s*";

        //present filter grammar
        private const string mPresentRE = @"(?<present>(?<presentattr>" + mAttrRE + @")\=\*)\s*";

        //highlevel filter grammar 
        private const string mItemRE = @"(?<item>" + mSimpleRE + "|" + mPresentRE + "|" + mSubstrRE + "|" + mExtenRE + @")\s*";
        private const string mFiltercompRE = @"(?<filtercomp>\!|\&|\|)\s*";
        private const string mFilterlistRE = @"(?<filterlist>.+)\s*";        //needs postprocessing
        private const string mFilterRE = @"^\s*\(\s*((" + mFiltercompRE + mFilterlistRE + ")|(" + mItemRE + @"))\)\s*$";
    }
}
