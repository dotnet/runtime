// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_throwinfinallyrecursive_20_cs
{
// levels of nesting = 20
public class Class1
{
    private static TestUtil.TestLog testLog;

    static Class1()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("in main try");
        expectedOut.WriteLine("in foo i = 0");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("in foo i = 1");
        expectedOut.WriteLine("-in foo try");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("-in foo catch");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("in main catch");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    static public void foo(int i)
    {
        if (i > 1) return;
        Console.WriteLine("in foo i = {0}", i);
        int j = 0;
        int k = 0;
        try
        {
            Console.WriteLine("-in foo try");
            try
            {
                Console.WriteLine("--in foo try");
                try
                {
                    Console.WriteLine("---in foo try");
                    try
                    {
                        Console.WriteLine("----in foo try");
                        try
                        {
                            Console.WriteLine("-----in foo try");
                            try
                            {
                                Console.WriteLine("------in foo try");
                                try
                                {
                                    Console.WriteLine("-------in foo try");
                                    try
                                    {
                                        Console.WriteLine("--------in foo try");
                                        try
                                        {
                                            Console.WriteLine("---------in foo try");
                                            try
                                            {
                                                Console.WriteLine("----------in foo try");
                                                try
                                                {
                                                    Console.WriteLine("-----------in foo try");
                                                    try
                                                    {
                                                        Console.WriteLine("------------in foo try");
                                                        try
                                                        {
                                                            Console.WriteLine("-------------in foo try");
                                                            try
                                                            {
                                                                Console.WriteLine("--------------in foo try");
                                                                try
                                                                {
                                                                    Console.WriteLine("---------------in foo try");
                                                                    try
                                                                    {
                                                                        Console.WriteLine("----------------in foo try");
                                                                        try
                                                                        {
                                                                            Console.WriteLine("-----------------in foo try");
                                                                            try
                                                                            {
                                                                                Console.WriteLine("------------------in foo try");
                                                                                try
                                                                                {
                                                                                    Console.WriteLine("-------------------in foo try");
                                                                                    try
                                                                                    {
                                                                                        Console.WriteLine("--------------------in foo try");
                                                                                        goto L20;
                                                                                    }
                                                                                    catch
                                                                                    {
                                                                                        Console.WriteLine("--------------------in foo catch");
                                                                                    }
                                                                                    finally
                                                                                    {
                                                                                        Console.WriteLine("--------------------in foo finally");
                                                                                        foo(i + 1);
                                                                                        j = 1 / i;
                                                                                        k = 1 / (j - 1);
                                                                                    }
                                                                                    L20:
                                                                                    goto L19;
                                                                                }
                                                                                catch
                                                                                {
                                                                                    Console.WriteLine("-------------------in foo catch");
                                                                                }
                                                                                finally
                                                                                {
                                                                                    Console.WriteLine("-------------------in foo finally");
                                                                                    foo(i + 1);
                                                                                    j = 1 / i;
                                                                                    k = 1 / (j - 1);
                                                                                }
                                                                                L19:
                                                                                goto L18;
                                                                            }
                                                                            catch
                                                                            {
                                                                                Console.WriteLine("------------------in foo catch");
                                                                            }
                                                                            finally
                                                                            {
                                                                                Console.WriteLine("------------------in foo finally");
                                                                                foo(i + 1);
                                                                                j = 1 / i;
                                                                                k = 1 / (j - 1);
                                                                            }
                                                                            L18:
                                                                            goto L17;
                                                                        }
                                                                        catch
                                                                        {
                                                                            Console.WriteLine("-----------------in foo catch");
                                                                        }
                                                                        finally
                                                                        {
                                                                            Console.WriteLine("-----------------in foo finally");
                                                                            foo(i + 1);
                                                                            j = 1 / i;
                                                                            k = 1 / (j - 1);
                                                                        }
                                                                        L17:
                                                                        goto L16;
                                                                    }
                                                                    catch
                                                                    {
                                                                        Console.WriteLine("----------------in foo catch");
                                                                    }
                                                                    finally
                                                                    {
                                                                        Console.WriteLine("----------------in foo finally");
                                                                        foo(i + 1);
                                                                        j = 1 / i;
                                                                        k = 1 / (j - 1);
                                                                    }
                                                                    L16:
                                                                    goto L15;
                                                                }
                                                                catch
                                                                {
                                                                    Console.WriteLine("---------------in foo catch");
                                                                }
                                                                finally
                                                                {
                                                                    Console.WriteLine("---------------in foo finally");
                                                                    foo(i + 1);
                                                                    j = 1 / i;
                                                                    k = 1 / (j - 1);
                                                                }
                                                                L15:
                                                                goto L14;
                                                            }
                                                            catch
                                                            {
                                                                Console.WriteLine("--------------in foo catch");
                                                            }
                                                            finally
                                                            {
                                                                Console.WriteLine("--------------in foo finally");
                                                                foo(i + 1);
                                                                j = 1 / i;
                                                                k = 1 / (j - 1);
                                                            }
                                                            L14:
                                                            goto L13;
                                                        }
                                                        catch
                                                        {
                                                            Console.WriteLine("-------------in foo catch");
                                                        }
                                                        finally
                                                        {
                                                            Console.WriteLine("-------------in foo finally");
                                                            foo(i + 1);
                                                            j = 1 / i;
                                                            k = 1 / (j - 1);
                                                        }
                                                        L13:
                                                        goto L12;
                                                    }
                                                    catch
                                                    {
                                                        Console.WriteLine("------------in foo catch");
                                                    }
                                                    finally
                                                    {
                                                        Console.WriteLine("------------in foo finally");
                                                        foo(i + 1);
                                                        j = 1 / i;
                                                        k = 1 / (j - 1);
                                                    }
                                                    L12:
                                                    goto L11;
                                                }
                                                catch
                                                {
                                                    Console.WriteLine("-----------in foo catch");
                                                }
                                                finally
                                                {
                                                    Console.WriteLine("-----------in foo finally");
                                                    foo(i + 1);
                                                    j = 1 / i;
                                                    k = 1 / (j - 1);
                                                }
                                                L11:
                                                goto L10;
                                            }
                                            catch
                                            {
                                                Console.WriteLine("----------in foo catch");
                                            }
                                            finally
                                            {
                                                Console.WriteLine("----------in foo finally");
                                                foo(i + 1);
                                                j = 1 / i;
                                                k = 1 / (j - 1);
                                            }
                                            L10:
                                            goto L9;
                                        }
                                        catch
                                        {
                                            Console.WriteLine("---------in foo catch");
                                        }
                                        finally
                                        {
                                            Console.WriteLine("---------in foo finally");
                                            foo(i + 1);
                                            j = 1 / i;
                                            k = 1 / (j - 1);
                                        }
                                        L9:
                                        goto L8;
                                    }
                                    catch
                                    {
                                        Console.WriteLine("--------in foo catch");
                                    }
                                    finally
                                    {
                                        Console.WriteLine("--------in foo finally");
                                        foo(i + 1);
                                        j = 1 / i;
                                        k = 1 / (j - 1);
                                    }
                                    L8:
                                    goto L7;
                                }
                                catch
                                {
                                    Console.WriteLine("-------in foo catch");
                                }
                                finally
                                {
                                    Console.WriteLine("-------in foo finally");
                                    foo(i + 1);
                                    j = 1 / i;
                                    k = 1 / (j - 1);
                                }
                                L7:
                                goto L6;
                            }
                            catch
                            {
                                Console.WriteLine("------in foo catch");
                            }
                            finally
                            {
                                Console.WriteLine("------in foo finally");
                                foo(i + 1);
                                j = 1 / i;
                                k = 1 / (j - 1);
                            }
                            L6:
                            goto L5;
                        }
                        catch
                        {
                            Console.WriteLine("-----in foo catch");
                        }
                        finally
                        {
                            Console.WriteLine("-----in foo finally");
                            foo(i + 1);
                            j = 1 / i;
                            k = 1 / (j - 1);
                        }
                        L5:
                        goto L4;
                    }
                    catch
                    {
                        Console.WriteLine("----in foo catch");
                    }
                    finally
                    {
                        Console.WriteLine("----in foo finally");
                        foo(i + 1);
                        j = 1 / i;
                        k = 1 / (j - 1);
                    }
                    L4:
                    goto L3;
                }
                catch
                {
                    Console.WriteLine("---in foo catch");
                }
                finally
                {
                    Console.WriteLine("---in foo finally");
                    foo(i + 1);
                    j = 1 / i;
                    k = 1 / (j - 1);
                }
                L3:
                goto L2;
            }
            catch
            {
                Console.WriteLine("--in foo catch");
            }
            finally
            {
                Console.WriteLine("--in foo finally");
                foo(i + 1);
                j = 1 / i;
                k = 1 / (j - 1);
            }
            L2:
            goto L1;
        }
        catch
        {
            Console.WriteLine("-in foo catch");
        }
        finally
        {
            Console.WriteLine("-in foo finally");
            foo(i + 1);
            j = 1 / i;
            k = 1 / (j - 1);
        }
        L1:
        foo(i + 1);
    }


    [Fact]
    static public int TestEntryPoint()
    {
        //Start recording
        testLog.StartRecording();

        int i = Environment.TickCount != 0 ? 0 : 1;
        try
        {
            Console.WriteLine("in main try");
            foo(i);
        }
        catch
        {
            Console.WriteLine("in main catch");
        }

        //Stop recording
        testLog.StopRecording();

        return testLog.VerifyOutput();
    }
}
}
