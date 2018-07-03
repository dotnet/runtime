// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Collections.Generic;

namespace OLEDB.Test.ModuleCore
{
    public class MyDict<Type1, Type2> : Dictionary<Type1, Type2>
    {
        public new Type2 this[Type1 key]
        {
            get
            {
                if (ContainsKey(key))
                {
                    return base[key];
                }
                return default(Type2);
            }
            set
            {
                base[key] = value;
            }
        }
    }
    ////////////////////////////////////////////////////////////////
    // CModInfo
    //
    ////////////////////////////////////////////////////////////////
    public class CModInfo
    {
        //Data
        private static string s_strCommandLine;
        private static MyDict<string, string> s_hashOptions;
        private static object _includenotimplemented;

        //Constructor
        public CModInfo()
        {
        }

        //Helpers
        internal static void Dispose()
        {
            //Reset the info.  
            //Since this is a static class, (to make it simpler to access from anywhere in your code)
            //we need to reset this info every time a test is run - so if you don't select an alias
            //the next time it doesn't use the previous alias setting - i.e.: ProviderInfo doesn't 
            //get called when no alias is selected...
            s_strCommandLine = null;
            s_hashOptions = null;
        }
        public static string CommandLine
        {
            // This Assert allows callers without the EnvironementPermission to use this property
            get
            {
                if (s_strCommandLine == null)
                    s_strCommandLine = "";
                return s_strCommandLine;
            }
            set
            {
                s_strCommandLine = value;
            }
        }
        public static MyDict<string, string> Options
        {
            get
            {
                //Deferred Parsing
                if (s_hashOptions == null)
                {
                    CKeywordParser.Tokens tokens = new CKeywordParser.Tokens();
                    tokens.Equal = " ";
                    tokens.Seperator = "/";
                    s_hashOptions = CKeywordParser.ParseKeywords(CommandLine, tokens);
                }
                return s_hashOptions;
            }
        }

        public static string Filter
        {
            //Typed options
            get { return (string)CModInfo.Options["Filter"]; }
        }

        public static string MaxPriority
        {
            //Typed options
            get { return (string)CModInfo.Options["MaxPriority"]; }
        }

        public static bool IncludeNotImplemented
        {
            //Typed options
            get
            {
                if (_includenotimplemented == null)
                {
                    _includenotimplemented = false;
                    if (CModInfo.Options["IncludeNotImplemented"] != null)
                        _includenotimplemented = true;
                }

                return (bool)_includenotimplemented;
            }
            set { _includenotimplemented = value; }
        }

        public static bool IsTestCaseSelected(string testcasename)
        {
            bool ret = true;
            string testcasefilter = CModInfo.Options["testcase"];
            if (testcasefilter != null
                && testcasefilter != "*"
                && testcasefilter != testcasename)
            {
                ret = false;
            }

            return ret;
        }

        public static bool IsVariationSelected(string variationname)
        {
            bool ret = true;
            string variationfilter = CModInfo.Options["variation"];
            if (variationfilter != null
                && variationfilter != "*"
                && variationfilter != variationname)
            {
                ret = false;
            }

            return ret;
        }
    }
}


