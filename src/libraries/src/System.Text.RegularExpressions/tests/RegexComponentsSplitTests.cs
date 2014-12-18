// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class RegexComponentsSplitTests
{
    [Fact]
    public static void RegexComponentsSplit()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            for (int i = 0; i < s_regexTests.Length; i++)
            {
                iCountTestcases++;
                if (!s_regexTests[i].Run())
                {
                    Console.WriteLine("Err_79872asnko! Test {0} FAILED Pattern={1}, Input={2}\n", i, s_regexTests[i].Pattern, s_regexTests[i].Input);
                    iCountErrors++;
                }
            }
            ///////////////////////////////////////////////////////////////////
            /////////////////////////// END TESTS /////////////////////////////
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine("Error Err_8888yyy!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
        }

        ////  Finish Diagnostics
        Assert.Equal(0, iCountErrors);
    }

    private static RegexComponentsSplitTestCase[] s_regexTests = new RegexComponentsSplitTestCase[]
    {
        /*********************************************************
            ValidCases
            *********************************************************/
    new RegexComponentsSplitTestCase(@"(\s)?(-)", "once -upon-a time", new String[]
    {
    "once", " ", "-", "upon", "-", "a time"
    }

    ), new RegexComponentsSplitTestCase(@"(\s)?(-)", "once upon a time", new String[]
    {
    "once upon a time"
    }

    ), new RegexComponentsSplitTestCase(@"(\s)?(-)", "once - -upon- a- time", new String[]
    {
    "once", " ", "-", "", " ", "-", "upon", "-", " a", "-", " time"
    }

    ), new RegexComponentsSplitTestCase(@"a(.)c(.)e", "123abcde456aBCDe789", new String[]
    {
    "123", "b", "d", "456aBCDe789"
    }

    ), new RegexComponentsSplitTestCase(@"a(.)c(.)e", RegexOptions.IgnoreCase, "123abcde456aBCDe789", new String[]
    {
    "123", "b", "d", "456", "B", "D", "789"
    }

    ), new RegexComponentsSplitTestCase(@"a(?<dot1>.)c(.)e", "123abcde456aBCDe789", new String[]
    {
    "123", "d", "b", "456aBCDe789"
    }

    ), new RegexComponentsSplitTestCase(@"a(?<dot1>.)c(.)e", RegexOptions.IgnoreCase, "123abcde456aBCDe789", new String[]
    {
    "123", "d", "b", "456", "D", "B", "789"
    }

    ), /*********************************************************
        RightToLeft
        *********************************************************/
new RegexComponentsSplitTestCase(@"a(.)c(.)e", RegexOptions.RightToLeft, "123abcde456aBCDe789", new String[]
    {
    "123", "d", "b", "456aBCDe789"
    }

    ), new RegexComponentsSplitTestCase(@"a(.)c(.)e", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, "123abcde456aBCDe789", new String[]
    {
    "123", "d", "b", "456", "D", "B", "789"
    }

    ), new RegexComponentsSplitTestCase(@"a(?<dot1>.)c(.)e", RegexOptions.RightToLeft, "123abcde456aBCDe789", new String[]
    {
    "123", "b", "d", "456aBCDe789"
    }

    ), new RegexComponentsSplitTestCase(@"a(?<dot1>.)c(.)e", RegexOptions.RightToLeft | RegexOptions.IgnoreCase, "123abcde456aBCDe789", new String[]
    {
    "123", "b", "d", "456", "B", "D", "789"
    }

    ), }

    ;
    public class RegexComponentsSplitTestCase
    {
        private String _pattern;
        private String _input;
        private RegexOptions _options;
        private String[] _expectedResult;
        public RegexComponentsSplitTestCase(String pattern, String input, String[] expectedResult)
            : this(pattern, RegexOptions.None, input, expectedResult)
        {
        }

        public RegexComponentsSplitTestCase(String pattern, RegexOptions options, String input, String[] expectedResult)
        {
            _pattern = pattern;
            _options = options;
            _input = input;
            _expectedResult = expectedResult;
        }

        public string Pattern
        {
            get
            {
                return _pattern;
            }
        }

        public string Input
        {
            get
            {
                return _input;
            }
        }

        public RegexOptions Options
        {
            get
            {
                return _options;
            }
        }

        public String[] ExpectedResult
        {
            get
            {
                return _expectedResult;
            }
        }

        public bool ExpectSuccess
        {
            get
            {
                return null != _expectedResult && _expectedResult.Length != 0;
            }
        }

        public bool Run()
        {
            Regex r;
            String[] result = null;
            r = new Regex(_pattern, _options);
            try
            {
                result = r.Split(_input);
            }
            catch (Exception e)
            {
                Console.WriteLine("Err_78394ayuua! Expected no exception to be thrown and the following was thrown: \n{0}", e);
                return false;
            }

            if (result.Length != _expectedResult.Length)
            {
                Console.WriteLine("Err_13484pua! Expected string[].Length and actual differ expected={0} actual={1}", _expectedResult.Length, result.Length);
                return false;
            }

            for (int i = 0; i < _expectedResult.Length; i++)
            {
                if (result[i] != _expectedResult[i])
                {
                    Console.WriteLine("Err_6897nzxn! Expected result and actual result differ expected='{0}' actual='{1}' at {2}", _expectedResult[i], result[i], i);
                    return false;
                }
            }

            return true;
        }
    }
}