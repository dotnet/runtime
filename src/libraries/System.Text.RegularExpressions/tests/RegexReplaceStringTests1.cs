// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexReplaceStringTests
{
    [Fact]
    public static void RegexReplaceStringTestCase1()
    {
        for (int i = 0; i < s_regexTests.Length; i++)
        {
            Assert.True(s_regexTests[i].Run());
        }
    }

    private static RegexReplaceStringTest[] s_regexTests = new RegexReplaceStringTest[]
    {
        /*********************************************************
        ValidCases
        *********************************************************/
    new RegexReplaceStringTest(@"(?<cat>cat)\s*(?<dog>dog)", "cat dog", "${cat}est ${dog}est", "catest dogest"), new RegexReplaceStringTest(@"(?<cat>cat)\s*(?<dog>dog)", "slkfjsdcat dogkljeah", "START${cat}dogcat${dog}END", "slkfjsdSTARTcatdogcatdogENDkljeah"), new RegexReplaceStringTest(@"(?<512>cat)\s*(?<256>dog)", "slkfjsdcat dogkljeah", "START${512}dogcat${256}END", "slkfjsdSTARTcatdogcatdogENDkljeah"), new RegexReplaceStringTest(@"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "START${256}dogcat${512}END", "slkfjsdSTARTcatdogcatdogENDkljeah"), new RegexReplaceStringTest(@"(?<512>cat)\s*(?<256>dog)", "slkfjsdcat dogkljeah", "STARTcat$256$512dogEND", "slkfjsdSTARTcatdogcatdogENDkljeah"), new RegexReplaceStringTest(@"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "STARTcat$512$256dogEND", "slkfjsdSTARTcatdogcatdogENDkljeah"), new RegexReplaceStringTest(@"(hello)cat\s+dog(world)", "hellocat dogworld", "$1$$$2", "hello$world"), new RegexReplaceStringTest(@"(hello)\s+(world)", "What the hello world goodby", "$&, how are you?", "What the hello world, how are you? goodby"), new RegexReplaceStringTest(@"(hello)\s+(world)", "What the hello world goodby", "$`cookie are you doing", "What the What the cookie are you doing goodby"), new RegexReplaceStringTest(@"(cat)\s+(dog)", "before textcat dogafter text", ". This is the $' and ", "before text. This is the after text and after text"), new RegexReplaceStringTest(@"(cat)\s+(dog)", "before textcat dogafter text", ". The following should be dog and it is $+. ", "before text. The following should be dog and it is dog. after text"), new RegexReplaceStringTest(@"(cat)\s+(dog)", "before textcat dogafter text", ". The following should be the entire string '$_'. ", "before text. The following should be the entire string 'before textcat dogafter text'. after text"), new RegexReplaceStringTest(@"(hello)\s+(world)", "START hello    world END", "$2 $1 $1 $2 $3$4", "START world hello hello world $3$4 END"), new RegexReplaceStringTest(@"(hello)\s+(world)", "START hello    world END", "$2 $1 $1 $2 $123$234", "START world hello hello world $123$234 END"), new RegexReplaceStringTest(@"(d)(o)(g)(\s)(c)(a)(t)(\s)(h)(a)(s)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline, "My dog cat has fleas.", "$01$02$03$04$05$06$07$08$09$10$11", "My dog cat has fleas."), new RegexReplaceStringTest(@"(d)(o)(g)(\s)(c)(a)(t)(\s)(h)(a)(s)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline, "My dog cat has fleas.", "$05$06$07$04$01$02$03$08$09$10$11", "My cat dog has fleas."), /*********************************************************
        ValidCases with ECMAScript option
        *********************************************************/
    new RegexReplaceStringTest(@"(?<512>cat)\s*(?<256>dog)", RegexOptions.ECMAScript, "slkfjsdcat dogkljeah", "STARTcat${256}${512}dogEND", "slkfjsdSTARTcatdogcatdogENDkljeah"), new RegexReplaceStringTest(@"(?<256>cat)\s*(?<512>dog)", RegexOptions.ECMAScript, "slkfjsdcat dogkljeah", "STARTcat${512}${256}dogEND", "slkfjsdSTARTcatdogcatdogENDkljeah"), new RegexReplaceStringTest(@"(?<1>cat)\s*(?<2>dog)", RegexOptions.ECMAScript, "slkfjsdcat dogkljeah", "STARTcat$2$1dogEND", "slkfjsdSTARTcatdogcatdogENDkljeah"), new RegexReplaceStringTest(@"(?<2>cat)\s*(?<1>dog)", RegexOptions.ECMAScript, "slkfjsdcat dogkljeah", "STARTcat$1$2dogEND", "slkfjsdSTARTcatdogcatdogENDkljeah"), new RegexReplaceStringTest(@"(?<512>cat)\s*(?<256>dog)", RegexOptions.ECMAScript, "slkfjsdcat dogkljeah", "STARTcat$256$512dogEND", "slkfjsdSTARTcatdogcatdogENDkljeah"), new RegexReplaceStringTest(@"(?<256>cat)\s*(?<512>dog)", RegexOptions.ECMAScript, "slkfjsdcat dogkljeah", "STARTcat$512$256dogEND", "slkfjsdSTARTcatdogcatdogENDkljeah"), new RegexReplaceStringTest(@"(hello)\s+world", RegexOptions.ECMAScript, "START hello    world END", "$234 $1 $1 $234 $3$4", "START $234 hello hello $234 $3$4 END"), new RegexReplaceStringTest(@"(hello)\s+(world)", RegexOptions.ECMAScript, "START hello    world END", "$2 $1 $1 $2 $3$4", "START world hello hello world $3$4 END"), new RegexReplaceStringTest(@"(hello)\s+(world)", RegexOptions.ECMAScript, "START hello    world END", "$2 $1 $1 $2 $123$234", "START world hello hello world hello23world34 END"), new RegexReplaceStringTest(@"(?<12>hello)\s+(world)", RegexOptions.ECMAScript, "START hello    world END", "$1 $12 $12 $1 $123$134", "START world hello hello world hello3world34 END"), new RegexReplaceStringTest(@"(?<123>hello)\s+(?<23>world)", RegexOptions.ECMAScript, "START hello    world END", "$23 $123 $123 $23 $123$234", "START world hello hello world helloworld4 END"), new RegexReplaceStringTest(@"(?<123>hello)\s+(?<234>world)", RegexOptions.ECMAScript, "START hello    world END", "$234 $123 $123 $234 $123456$234567", "START world hello hello world hello456world567 END"), new RegexReplaceStringTest(@"(d)(o)(g)(\s)(c)(a)(t)(\s)(h)(a)(s)", RegexOptions.CultureInvariant | RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline, "My dog cat has fleas.", "$01$02$03$04$05$06$07$08$09$10$11", "My dog cat has fleas."), new RegexReplaceStringTest(@"(d)(o)(g)(\s)(c)(a)(t)(\s)(h)(a)(s)", RegexOptions.CultureInvariant | RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline, "My dog cat has fleas.", "$05$06$07$04$01$02$03$08$09$10$11", "My cat dog has fleas."), /*********************************************************
        Error cases
        *********************************************************/
    new RegexReplaceStringTest(@"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "STARTcat$512$", "slkfjsdSTARTcatdog$kljeah"), new RegexReplaceStringTest(@"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "STARTcat$2048$1024dogEND", "slkfjsdSTARTcat$2048$1024dogENDkljeah"), new RegexReplaceStringTest(@"(?<cat>cat)\s*(?<dog>dog)", "slkfjsdcat dogkljeah", "START${catTWO}dogcat${dogTWO}END", "slkfjsdSTART${catTWO}dogcat${dogTWO}ENDkljeah"), }

    ;
    public class RegexReplaceStringTest
    {
        private string _pattern;
        private string _input;
        private RegexOptions _options;
        private string _replaceString;
        private string _expectedResult;
        private Type _expectedExceptionType;
        public RegexReplaceStringTest(string pattern, string input, string replaceString, string expectedResult)
            : this(pattern, RegexOptions.None, input, replaceString, expectedResult)
        {
        }

        public RegexReplaceStringTest(string pattern, RegexOptions options, string input, string replaceString, string expectedResult)
        {
            _pattern = pattern;
            _options = options;
            _input = input;
            _replaceString = replaceString;
            _expectedResult = expectedResult;
        }

        public RegexReplaceStringTest(string pattern, string input, string replaceString, Type expectedExceptionType)
            : this(pattern, RegexOptions.None, input, replaceString, expectedExceptionType)
        {
        }

        public RegexReplaceStringTest(string pattern, RegexOptions options, string input, string replaceString, Type expectedExceptionType)
        {
            _pattern = pattern;
            _options = options;
            _input = input;
            _replaceString = replaceString;
            _expectedExceptionType = expectedExceptionType;
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

        public string ReplaceString
        {
            get
            {
                return _replaceString;
            }
        }

        public string ExpectedResult
        {
            get
            {
                return _expectedResult;
            }
        }

        public Type ExpectedExceptionType
        {
            get
            {
                return _expectedExceptionType;
            }
        }

        public bool ExpectException
        {
            get
            {
                return null != _expectedExceptionType;
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
            string result = null;
            r = new Regex(_pattern, _options);
            try
            {
                result = r.Replace(_input, _replaceString);
                if (ExpectException)
                {
                    Console.WriteLine("Err_09872anba! Expected Regex to throw {0} exception and none was thrown", _expectedExceptionType);
                    return false;
                }
            }
            catch (Exception e)
            {
                if (ExpectException && e.GetType() == _expectedExceptionType)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("Err_78394ayuua! Expected no exception to be thrown and the following was thrown: \n{0}", e);
                    return false;
                }
            }

            if (!ExpectSuccess)
            {
                if (result != _input)
                {
                    Console.WriteLine("Err_2270awanm! Did not expect anything to be replaced result='{0}'", result);
                    return false;
                }

                return true;
            }

            if (result != _expectedResult)
            {
                Console.WriteLine("Err_68997asnzxn! Expected result and actual result differ\n expected={0} actual={1}", _expectedResult, result);
                return false;
            }

            return true;
        }
    }
}
