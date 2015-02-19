// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using System.Globalization;

public class RegexTestCase
{
    private String _pattern;
    private String _input;
    private RegexOptions _options;
    private String[] _expectedGroups;
    private Type _expectedExceptionType;
    private System.Globalization.CultureInfo _cultureInfo;

    public RegexTestCase(String pattern, String input, params String[] expectedGroups) : this(pattern, RegexOptions.None, input, expectedGroups) { }

    public RegexTestCase(String pattern, String input, String[] expectedGroups, CultureInfo culture) : this(pattern, RegexOptions.None, input, expectedGroups, culture) { }

    public RegexTestCase(String pattern, RegexOptions options, String input, params String[] expectedGroups)
    {
        _pattern = pattern;
        _options = options;
        _input = input;
        _expectedGroups = expectedGroups;
        _cultureInfo = null;
    }

    public RegexTestCase(String pattern, RegexOptions options, String input, String[] expectedGroups, CultureInfo culture)
    {
        _pattern = pattern;
        _options = options;
        _input = input;
        _expectedGroups = expectedGroups;
        _cultureInfo = culture;
    }

    public RegexTestCase(String pattern, Type expectedExceptionType) : this(pattern, RegexOptions.None, expectedExceptionType) { }

    public RegexTestCase(String pattern, RegexOptions options, Type expectedExceptionType)
    {
        _pattern = pattern;
        _options = options;
        _expectedExceptionType = expectedExceptionType;
    }

    public string Pattern { get { return _pattern; } }

    public string Input { get { return _input; } }

    public RegexOptions Options { get { return _options; } }

    public String[] ExpectedGroups { get { return _expectedGroups; } }

    public Type ExpectedExceptionType { get { return _expectedExceptionType; } }

    public bool ExpectException { get { return null != _expectedExceptionType; } }

    public bool ExpectSuccess { get { return null != _expectedGroups && _expectedGroups.Length != 0; } }

    public bool Run()
    {
        Regex r;
        Match m;
        System.Globalization.CultureInfo originalCulture = null;

        try
        {
            if (null != _cultureInfo)
            {
                originalCulture = CultureInfo.CurrentCulture;
                CultureInfo.CurrentCulture = _cultureInfo;
            }


            try
            {
                r = new Regex(_pattern, _options);

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
                else if (ExpectException)
                {
                    Console.WriteLine("Err_4980asu! Expected exception of type {0} and instead the following was thrown: \n{1}", _expectedExceptionType, e);
                    return false;
                }
                else
                {
                    Console.WriteLine("Err_78394ayuua! Expected no exception to be thrown and the following was thrown: \n{0}", e);
                    return false;
                }
            }

            m = r.Match(_input);
        }
        finally
        {
            if (null != _cultureInfo)
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        if (m.Success && !ExpectSuccess)
        {
            Console.WriteLine("Err_2270awanm! Did not expect the match to succeed");
            return false;
        }
        else if (!m.Success && ExpectSuccess)
        {
            Console.WriteLine("Err_68997asnzxn! Did not expect the match to fail");
            return false;
        }

        if (!ExpectSuccess)
        {
            //The match was not suppose to succeed and it failed. There is no more checking to do
            //so the test was a success
            return true;
        }

        if (m.Groups.Count != _expectedGroups.Length)
        {
            Console.WriteLine("Err_0234jah!Expected {0} groups and got {1} groups", _expectedGroups.Length, m.Groups.Count);
            return false;
        }

        if (m.Value != _expectedGroups[0])
        {
            Console.WriteLine("Err_611074ahhar Expected Value='{0}' Actual='{1}'", _expectedGroups[0], m.Value);
            return false;
        }

        int[] groupNumbers = r.GetGroupNumbers();
        string[] groupNames = r.GetGroupNames();

        for (int i = 0; i < _expectedGroups.Length; i++)
        {
            //Verify Group.Value
            if (m.Groups[groupNumbers[i]].Value != _expectedGroups[i])
            {
                Console.WriteLine("Err_07823nhhl Expected Group[{0}]='{1}' actual='{2}' Name={3}",
                    groupNumbers[i], _expectedGroups[i], m.Groups[groupNumbers[i]], r.GroupNameFromNumber(groupNumbers[i]));
                return false;
            }

            //Verify the same thing is returned from groups when using either an int or string index
            if (m.Groups[groupNumbers[i]] != m.Groups[groupNames[i]])
            {
                Console.WriteLine("Err_08712saklj Expected Groups[{0}]='{1}' Groups[{2}]='{3}'",
                    groupNumbers[i], m.Groups[groupNumbers[i]], groupNames[i], m.Groups[groupNames[i]]);
                return false;
            }

            //Verify GetGroupNumberFromName
            if (groupNumbers[i] != r.GroupNumberFromName(groupNames[i]))
            {
                Console.WriteLine("Err_68974aehui Expected GroupNumberFromName({0})={1} actual{2}",
                    groupNames[i], groupNumbers[i], r.GroupNumberFromName(groupNames[i]));
                return false;
            }

            //Verify GetGroupNameFromNumber
            if (groupNames[i] != r.GroupNameFromNumber(groupNumbers[i]))
            {
                Console.WriteLine("Err_3468plhmy Expected GroupNameFromNumber({0})={1} actual{2}",
                    groupNumbers[i], groupNames[i], r.GroupNameFromNumber(groupNumbers[i]));
                return false;
            }
        }

        return true;
    }
}
