// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_throwinfinallynestedintry_30_cs
{
// levels of nesting = 30
public class Class1
{
    private static TestUtil.TestLog testLog;

    static Class1()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("in main try");
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
        expectedOut.WriteLine("---------------------in foo try");
        expectedOut.WriteLine("----------------------in foo try");
        expectedOut.WriteLine("-----------------------in foo try");
        expectedOut.WriteLine("------------------------in foo try");
        expectedOut.WriteLine("-------------------------in foo try");
        expectedOut.WriteLine("--------------------------in foo try");
        expectedOut.WriteLine("---------------------------in foo try");
        expectedOut.WriteLine("----------------------------in foo try");
        expectedOut.WriteLine("-----------------------------in foo try");
        expectedOut.WriteLine("------------------------------in foo try");
        expectedOut.WriteLine("------------------------------in foo finally");
        expectedOut.WriteLine("------------------------------foo L30");
        expectedOut.WriteLine("-----------------------------in foo finally");
        expectedOut.WriteLine("------------------------------in foo try");
        expectedOut.WriteLine("------------------------------in foo finally");
        expectedOut.WriteLine("------------------------------throwing an exception [i = 0]");
        expectedOut.WriteLine("----------------------------in foo finally");
        expectedOut.WriteLine("-----------------------------in foo try");
        expectedOut.WriteLine("-----------------------------throwing an exception [i = 0]");
        expectedOut.WriteLine("-----------------------------in foo catch");
        expectedOut.WriteLine("-----------------------------in foo finally");
        expectedOut.WriteLine("-----------------------------throwing an exception [i = 1]");
        expectedOut.WriteLine("---------------------------in foo finally");
        expectedOut.WriteLine("----------------------------in foo try");
        expectedOut.WriteLine("----------------------------throwing an exception [i = 1]");
        expectedOut.WriteLine("----------------------------in foo catch");
        expectedOut.WriteLine("----------------------------in foo finally");
        expectedOut.WriteLine("----------------------------throwing an exception [i = 2]");
        expectedOut.WriteLine("--------------------------in foo finally");
        expectedOut.WriteLine("---------------------------in foo try");
        expectedOut.WriteLine("---------------------------throwing an exception [i = 2]");
        expectedOut.WriteLine("---------------------------in foo catch");
        expectedOut.WriteLine("---------------------------in foo finally");
        expectedOut.WriteLine("---------------------------throwing an exception [i = 3]");
        expectedOut.WriteLine("-------------------------in foo finally");
        expectedOut.WriteLine("--------------------------in foo try");
        expectedOut.WriteLine("--------------------------throwing an exception [i = 3]");
        expectedOut.WriteLine("--------------------------in foo catch");
        expectedOut.WriteLine("--------------------------in foo finally");
        expectedOut.WriteLine("--------------------------throwing an exception [i = 4]");
        expectedOut.WriteLine("------------------------in foo finally");
        expectedOut.WriteLine("-------------------------in foo try");
        expectedOut.WriteLine("-------------------------throwing an exception [i = 4]");
        expectedOut.WriteLine("-------------------------in foo catch");
        expectedOut.WriteLine("-------------------------in foo finally");
        expectedOut.WriteLine("-------------------------throwing an exception [i = 5]");
        expectedOut.WriteLine("-----------------------in foo finally");
        expectedOut.WriteLine("------------------------in foo try");
        expectedOut.WriteLine("------------------------throwing an exception [i = 5]");
        expectedOut.WriteLine("------------------------in foo catch");
        expectedOut.WriteLine("------------------------in foo finally");
        expectedOut.WriteLine("------------------------throwing an exception [i = 6]");
        expectedOut.WriteLine("----------------------in foo finally");
        expectedOut.WriteLine("-----------------------in foo try");
        expectedOut.WriteLine("-----------------------throwing an exception [i = 6]");
        expectedOut.WriteLine("-----------------------in foo catch");
        expectedOut.WriteLine("-----------------------in foo finally");
        expectedOut.WriteLine("-----------------------throwing an exception [i = 7]");
        expectedOut.WriteLine("---------------------in foo finally");
        expectedOut.WriteLine("----------------------in foo try");
        expectedOut.WriteLine("----------------------throwing an exception [i = 7]");
        expectedOut.WriteLine("----------------------in foo catch");
        expectedOut.WriteLine("----------------------in foo finally");
        expectedOut.WriteLine("----------------------throwing an exception [i = 8]");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("---------------------in foo try");
        expectedOut.WriteLine("---------------------throwing an exception [i = 8]");
        expectedOut.WriteLine("---------------------in foo catch");
        expectedOut.WriteLine("---------------------in foo finally");
        expectedOut.WriteLine("---------------------throwing an exception [i = 9]");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("--------------------in foo try");
        expectedOut.WriteLine("--------------------throwing an exception [i = 9]");
        expectedOut.WriteLine("--------------------in foo catch");
        expectedOut.WriteLine("--------------------in foo finally");
        expectedOut.WriteLine("--------------------throwing an exception [i = 10]");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("-------------------in foo try");
        expectedOut.WriteLine("-------------------throwing an exception [i = 10]");
        expectedOut.WriteLine("-------------------in foo catch");
        expectedOut.WriteLine("-------------------in foo finally");
        expectedOut.WriteLine("-------------------throwing an exception [i = 11]");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("------------------in foo try");
        expectedOut.WriteLine("------------------throwing an exception [i = 11]");
        expectedOut.WriteLine("------------------in foo catch");
        expectedOut.WriteLine("------------------in foo finally");
        expectedOut.WriteLine("------------------throwing an exception [i = 12]");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("-----------------in foo try");
        expectedOut.WriteLine("-----------------throwing an exception [i = 12]");
        expectedOut.WriteLine("-----------------in foo catch");
        expectedOut.WriteLine("-----------------in foo finally");
        expectedOut.WriteLine("-----------------throwing an exception [i = 13]");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("----------------in foo try");
        expectedOut.WriteLine("----------------throwing an exception [i = 13]");
        expectedOut.WriteLine("----------------in foo catch");
        expectedOut.WriteLine("----------------in foo finally");
        expectedOut.WriteLine("----------------throwing an exception [i = 14]");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("---------------in foo try");
        expectedOut.WriteLine("---------------throwing an exception [i = 14]");
        expectedOut.WriteLine("---------------in foo catch");
        expectedOut.WriteLine("---------------in foo finally");
        expectedOut.WriteLine("---------------throwing an exception [i = 15]");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("--------------in foo try");
        expectedOut.WriteLine("--------------throwing an exception [i = 15]");
        expectedOut.WriteLine("--------------in foo catch");
        expectedOut.WriteLine("--------------in foo finally");
        expectedOut.WriteLine("--------------throwing an exception [i = 16]");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("-------------in foo try");
        expectedOut.WriteLine("-------------throwing an exception [i = 16]");
        expectedOut.WriteLine("-------------in foo catch");
        expectedOut.WriteLine("-------------in foo finally");
        expectedOut.WriteLine("-------------throwing an exception [i = 17]");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("------------in foo try");
        expectedOut.WriteLine("------------throwing an exception [i = 17]");
        expectedOut.WriteLine("------------in foo catch");
        expectedOut.WriteLine("------------in foo finally");
        expectedOut.WriteLine("------------throwing an exception [i = 18]");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("-----------in foo try");
        expectedOut.WriteLine("-----------throwing an exception [i = 18]");
        expectedOut.WriteLine("-----------in foo catch");
        expectedOut.WriteLine("-----------in foo finally");
        expectedOut.WriteLine("-----------throwing an exception [i = 19]");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("----------in foo try");
        expectedOut.WriteLine("----------throwing an exception [i = 19]");
        expectedOut.WriteLine("----------in foo catch");
        expectedOut.WriteLine("----------in foo finally");
        expectedOut.WriteLine("----------throwing an exception [i = 20]");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("---------in foo try");
        expectedOut.WriteLine("---------throwing an exception [i = 20]");
        expectedOut.WriteLine("---------in foo catch");
        expectedOut.WriteLine("---------in foo finally");
        expectedOut.WriteLine("---------throwing an exception [i = 21]");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("--------in foo try");
        expectedOut.WriteLine("--------throwing an exception [i = 21]");
        expectedOut.WriteLine("--------in foo catch");
        expectedOut.WriteLine("--------in foo finally");
        expectedOut.WriteLine("--------throwing an exception [i = 22]");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("-------in foo try");
        expectedOut.WriteLine("-------throwing an exception [i = 22]");
        expectedOut.WriteLine("-------in foo catch");
        expectedOut.WriteLine("-------in foo finally");
        expectedOut.WriteLine("-------throwing an exception [i = 23]");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("------in foo try");
        expectedOut.WriteLine("------throwing an exception [i = 23]");
        expectedOut.WriteLine("------in foo catch");
        expectedOut.WriteLine("------in foo finally");
        expectedOut.WriteLine("------throwing an exception [i = 24]");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("-----in foo try");
        expectedOut.WriteLine("-----throwing an exception [i = 24]");
        expectedOut.WriteLine("-----in foo catch");
        expectedOut.WriteLine("-----in foo finally");
        expectedOut.WriteLine("-----throwing an exception [i = 25]");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("----in foo try");
        expectedOut.WriteLine("----throwing an exception [i = 25]");
        expectedOut.WriteLine("----in foo catch");
        expectedOut.WriteLine("----in foo finally");
        expectedOut.WriteLine("----throwing an exception [i = 26]");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("---in foo try");
        expectedOut.WriteLine("---throwing an exception [i = 26]");
        expectedOut.WriteLine("---in foo catch");
        expectedOut.WriteLine("---in foo finally");
        expectedOut.WriteLine("---throwing an exception [i = 27]");
        expectedOut.WriteLine("-in foo finally");
        expectedOut.WriteLine("--in foo try");
        expectedOut.WriteLine("--throwing an exception [i = 27]");
        expectedOut.WriteLine("--in foo catch");
        expectedOut.WriteLine("--in foo finally");
        expectedOut.WriteLine("--throwing an exception [i = 28]");
        expectedOut.WriteLine("in main catch");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    static public void foo(int i)
    {
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
                                                                                        try
                                                                                        {
                                                                                            Console.WriteLine("---------------------in foo try");
                                                                                            try
                                                                                            {
                                                                                                Console.WriteLine("----------------------in foo try");
                                                                                                try
                                                                                                {
                                                                                                    Console.WriteLine("-----------------------in foo try");
                                                                                                    try
                                                                                                    {
                                                                                                        Console.WriteLine("------------------------in foo try");
                                                                                                        try
                                                                                                        {
                                                                                                            Console.WriteLine("-------------------------in foo try");
                                                                                                            try
                                                                                                            {
                                                                                                                Console.WriteLine("--------------------------in foo try");
                                                                                                                try
                                                                                                                {
                                                                                                                    Console.WriteLine("---------------------------in foo try");
                                                                                                                    try
                                                                                                                    {
                                                                                                                        Console.WriteLine("----------------------------in foo try");
                                                                                                                        try
                                                                                                                        {
                                                                                                                            Console.WriteLine("-----------------------------in foo try");
                                                                                                                            try
                                                                                                                            {
                                                                                                                                Console.WriteLine("------------------------------in foo try");
                                                                                                                                goto L30;
                                                                                                                            }
                                                                                                                            finally
                                                                                                                            {
                                                                                                                                Console.WriteLine("------------------------------in foo finally");
                                                                                                                            }
                                                                                                                            L30:
                                                                                                                            Console.WriteLine("------------------------------foo L30");
                                                                                                                            goto L29;
                                                                                                                        }
                                                                                                                        finally
                                                                                                                        {
                                                                                                                            Console.WriteLine("-----------------------------in foo finally");
                                                                                                                            try
                                                                                                                            {
                                                                                                                                Console.WriteLine("------------------------------in foo try");
                                                                                                                                if (i % 2 == 1)
                                                                                                                                {
                                                                                                                                    Console.WriteLine("------------------------------throwing an exception [i = {0}]", i);
                                                                                                                                    throw new Exception();
                                                                                                                                }
                                                                                                                                else
                                                                                                                                {
                                                                                                                                    goto L29A;
                                                                                                                                }
                                                                                                                            }
                                                                                                                            catch
                                                                                                                            {
                                                                                                                                Console.WriteLine("------------------------------in foo catch");
                                                                                                                                i++;
                                                                                                                            }
                                                                                                                            finally
                                                                                                                            {
                                                                                                                                Console.WriteLine("------------------------------in foo finally");
                                                                                                                                if (i % 2 == 0)
                                                                                                                                {
                                                                                                                                    Console.WriteLine("------------------------------throwing an exception [i = {0}]", i);
                                                                                                                                    throw new Exception();
                                                                                                                                }
                                                                                                                            }
                                                                                                                            L29A:
                                                                                                                            Console.WriteLine("------------------------------foo L29A");
                                                                                                                        }
                                                                                                                        L29:
                                                                                                                        Console.WriteLine("-----------------------------foo L29");
                                                                                                                        goto L28;
                                                                                                                    }
                                                                                                                    finally
                                                                                                                    {
                                                                                                                        Console.WriteLine("----------------------------in foo finally");
                                                                                                                        try
                                                                                                                        {
                                                                                                                            Console.WriteLine("-----------------------------in foo try");
                                                                                                                            if (i % 2 == 0)
                                                                                                                            {
                                                                                                                                Console.WriteLine("-----------------------------throwing an exception [i = {0}]", i);
                                                                                                                                throw new Exception();
                                                                                                                            }
                                                                                                                            else
                                                                                                                            {
                                                                                                                                goto L28A;
                                                                                                                            }
                                                                                                                        }
                                                                                                                        catch
                                                                                                                        {
                                                                                                                            Console.WriteLine("-----------------------------in foo catch");
                                                                                                                            i++;
                                                                                                                        }
                                                                                                                        finally
                                                                                                                        {
                                                                                                                            Console.WriteLine("-----------------------------in foo finally");
                                                                                                                            if (i % 2 == 1)
                                                                                                                            {
                                                                                                                                Console.WriteLine("-----------------------------throwing an exception [i = {0}]", i);
                                                                                                                                throw new Exception();
                                                                                                                            }
                                                                                                                        }
                                                                                                                        L28A:
                                                                                                                        Console.WriteLine("-----------------------------foo L28A");
                                                                                                                    }
                                                                                                                    L28:
                                                                                                                    Console.WriteLine("----------------------------foo L28");
                                                                                                                    goto L27;
                                                                                                                }
                                                                                                                finally
                                                                                                                {
                                                                                                                    Console.WriteLine("---------------------------in foo finally");
                                                                                                                    try
                                                                                                                    {
                                                                                                                        Console.WriteLine("----------------------------in foo try");
                                                                                                                        if (i % 2 == 1)
                                                                                                                        {
                                                                                                                            Console.WriteLine("----------------------------throwing an exception [i = {0}]", i);
                                                                                                                            throw new Exception();
                                                                                                                        }
                                                                                                                        else
                                                                                                                        {
                                                                                                                            goto L27A;
                                                                                                                        }
                                                                                                                    }
                                                                                                                    catch
                                                                                                                    {
                                                                                                                        Console.WriteLine("----------------------------in foo catch");
                                                                                                                        i++;
                                                                                                                    }
                                                                                                                    finally
                                                                                                                    {
                                                                                                                        Console.WriteLine("----------------------------in foo finally");
                                                                                                                        if (i % 2 == 0)
                                                                                                                        {
                                                                                                                            Console.WriteLine("----------------------------throwing an exception [i = {0}]", i);
                                                                                                                            throw new Exception();
                                                                                                                        }
                                                                                                                    }
                                                                                                                    L27A:
                                                                                                                    Console.WriteLine("----------------------------foo L27A");
                                                                                                                }
                                                                                                                L27:
                                                                                                                Console.WriteLine("---------------------------foo L27");
                                                                                                                goto L26;
                                                                                                            }
                                                                                                            finally
                                                                                                            {
                                                                                                                Console.WriteLine("--------------------------in foo finally");
                                                                                                                try
                                                                                                                {
                                                                                                                    Console.WriteLine("---------------------------in foo try");
                                                                                                                    if (i % 2 == 0)
                                                                                                                    {
                                                                                                                        Console.WriteLine("---------------------------throwing an exception [i = {0}]", i);
                                                                                                                        throw new Exception();
                                                                                                                    }
                                                                                                                    else
                                                                                                                    {
                                                                                                                        goto L26A;
                                                                                                                    }
                                                                                                                }
                                                                                                                catch
                                                                                                                {
                                                                                                                    Console.WriteLine("---------------------------in foo catch");
                                                                                                                    i++;
                                                                                                                }
                                                                                                                finally
                                                                                                                {
                                                                                                                    Console.WriteLine("---------------------------in foo finally");
                                                                                                                    if (i % 2 == 1)
                                                                                                                    {
                                                                                                                        Console.WriteLine("---------------------------throwing an exception [i = {0}]", i);
                                                                                                                        throw new Exception();
                                                                                                                    }
                                                                                                                }
                                                                                                                L26A:
                                                                                                                Console.WriteLine("---------------------------foo L26A");
                                                                                                            }
                                                                                                            L26:
                                                                                                            Console.WriteLine("--------------------------foo L26");
                                                                                                            goto L25;
                                                                                                        }
                                                                                                        finally
                                                                                                        {
                                                                                                            Console.WriteLine("-------------------------in foo finally");
                                                                                                            try
                                                                                                            {
                                                                                                                Console.WriteLine("--------------------------in foo try");
                                                                                                                if (i % 2 == 1)
                                                                                                                {
                                                                                                                    Console.WriteLine("--------------------------throwing an exception [i = {0}]", i);
                                                                                                                    throw new Exception();
                                                                                                                }
                                                                                                                else
                                                                                                                {
                                                                                                                    goto L25A;
                                                                                                                }
                                                                                                            }
                                                                                                            catch
                                                                                                            {
                                                                                                                Console.WriteLine("--------------------------in foo catch");
                                                                                                                i++;
                                                                                                            }
                                                                                                            finally
                                                                                                            {
                                                                                                                Console.WriteLine("--------------------------in foo finally");
                                                                                                                if (i % 2 == 0)
                                                                                                                {
                                                                                                                    Console.WriteLine("--------------------------throwing an exception [i = {0}]", i);
                                                                                                                    throw new Exception();
                                                                                                                }
                                                                                                            }
                                                                                                            L25A:
                                                                                                            Console.WriteLine("--------------------------foo L25A");
                                                                                                        }
                                                                                                        L25:
                                                                                                        Console.WriteLine("-------------------------foo L25");
                                                                                                        goto L24;
                                                                                                    }
                                                                                                    finally
                                                                                                    {
                                                                                                        Console.WriteLine("------------------------in foo finally");
                                                                                                        try
                                                                                                        {
                                                                                                            Console.WriteLine("-------------------------in foo try");
                                                                                                            if (i % 2 == 0)
                                                                                                            {
                                                                                                                Console.WriteLine("-------------------------throwing an exception [i = {0}]", i);
                                                                                                                throw new Exception();
                                                                                                            }
                                                                                                            else
                                                                                                            {
                                                                                                                goto L24A;
                                                                                                            }
                                                                                                        }
                                                                                                        catch
                                                                                                        {
                                                                                                            Console.WriteLine("-------------------------in foo catch");
                                                                                                            i++;
                                                                                                        }
                                                                                                        finally
                                                                                                        {
                                                                                                            Console.WriteLine("-------------------------in foo finally");
                                                                                                            if (i % 2 == 1)
                                                                                                            {
                                                                                                                Console.WriteLine("-------------------------throwing an exception [i = {0}]", i);
                                                                                                                throw new Exception();
                                                                                                            }
                                                                                                        }
                                                                                                        L24A:
                                                                                                        Console.WriteLine("-------------------------foo L24A");
                                                                                                    }
                                                                                                    L24:
                                                                                                    Console.WriteLine("------------------------foo L24");
                                                                                                    goto L23;
                                                                                                }
                                                                                                finally
                                                                                                {
                                                                                                    Console.WriteLine("-----------------------in foo finally");
                                                                                                    try
                                                                                                    {
                                                                                                        Console.WriteLine("------------------------in foo try");
                                                                                                        if (i % 2 == 1)
                                                                                                        {
                                                                                                            Console.WriteLine("------------------------throwing an exception [i = {0}]", i);
                                                                                                            throw new Exception();
                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            goto L23A;
                                                                                                        }
                                                                                                    }
                                                                                                    catch
                                                                                                    {
                                                                                                        Console.WriteLine("------------------------in foo catch");
                                                                                                        i++;
                                                                                                    }
                                                                                                    finally
                                                                                                    {
                                                                                                        Console.WriteLine("------------------------in foo finally");
                                                                                                        if (i % 2 == 0)
                                                                                                        {
                                                                                                            Console.WriteLine("------------------------throwing an exception [i = {0}]", i);
                                                                                                            throw new Exception();
                                                                                                        }
                                                                                                    }
                                                                                                    L23A:
                                                                                                    Console.WriteLine("------------------------foo L23A");
                                                                                                }
                                                                                                L23:
                                                                                                Console.WriteLine("-----------------------foo L23");
                                                                                                goto L22;
                                                                                            }
                                                                                            finally
                                                                                            {
                                                                                                Console.WriteLine("----------------------in foo finally");
                                                                                                try
                                                                                                {
                                                                                                    Console.WriteLine("-----------------------in foo try");
                                                                                                    if (i % 2 == 0)
                                                                                                    {
                                                                                                        Console.WriteLine("-----------------------throwing an exception [i = {0}]", i);
                                                                                                        throw new Exception();
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        goto L22A;
                                                                                                    }
                                                                                                }
                                                                                                catch
                                                                                                {
                                                                                                    Console.WriteLine("-----------------------in foo catch");
                                                                                                    i++;
                                                                                                }
                                                                                                finally
                                                                                                {
                                                                                                    Console.WriteLine("-----------------------in foo finally");
                                                                                                    if (i % 2 == 1)
                                                                                                    {
                                                                                                        Console.WriteLine("-----------------------throwing an exception [i = {0}]", i);
                                                                                                        throw new Exception();
                                                                                                    }
                                                                                                }
                                                                                                L22A:
                                                                                                Console.WriteLine("-----------------------foo L22A");
                                                                                            }
                                                                                            L22:
                                                                                            Console.WriteLine("----------------------foo L22");
                                                                                            goto L21;
                                                                                        }
                                                                                        finally
                                                                                        {
                                                                                            Console.WriteLine("---------------------in foo finally");
                                                                                            try
                                                                                            {
                                                                                                Console.WriteLine("----------------------in foo try");
                                                                                                if (i % 2 == 1)
                                                                                                {
                                                                                                    Console.WriteLine("----------------------throwing an exception [i = {0}]", i);
                                                                                                    throw new Exception();
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    goto L21A;
                                                                                                }
                                                                                            }
                                                                                            catch
                                                                                            {
                                                                                                Console.WriteLine("----------------------in foo catch");
                                                                                                i++;
                                                                                            }
                                                                                            finally
                                                                                            {
                                                                                                Console.WriteLine("----------------------in foo finally");
                                                                                                if (i % 2 == 0)
                                                                                                {
                                                                                                    Console.WriteLine("----------------------throwing an exception [i = {0}]", i);
                                                                                                    throw new Exception();
                                                                                                }
                                                                                            }
                                                                                            L21A:
                                                                                            Console.WriteLine("----------------------foo L21A");
                                                                                        }
                                                                                        L21:
                                                                                        Console.WriteLine("---------------------foo L21");
                                                                                        goto L20;
                                                                                    }
                                                                                    finally
                                                                                    {
                                                                                        Console.WriteLine("--------------------in foo finally");
                                                                                        try
                                                                                        {
                                                                                            Console.WriteLine("---------------------in foo try");
                                                                                            if (i % 2 == 0)
                                                                                            {
                                                                                                Console.WriteLine("---------------------throwing an exception [i = {0}]", i);
                                                                                                throw new Exception();
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                goto L20A;
                                                                                            }
                                                                                        }
                                                                                        catch
                                                                                        {
                                                                                            Console.WriteLine("---------------------in foo catch");
                                                                                            i++;
                                                                                        }
                                                                                        finally
                                                                                        {
                                                                                            Console.WriteLine("---------------------in foo finally");
                                                                                            if (i % 2 == 1)
                                                                                            {
                                                                                                Console.WriteLine("---------------------throwing an exception [i = {0}]", i);
                                                                                                throw new Exception();
                                                                                            }
                                                                                        }
                                                                                        L20A:
                                                                                        Console.WriteLine("---------------------foo L20A");
                                                                                    }
                                                                                    L20:
                                                                                    Console.WriteLine("--------------------foo L20");
                                                                                    goto L19;
                                                                                }
                                                                                finally
                                                                                {
                                                                                    Console.WriteLine("-------------------in foo finally");
                                                                                    try
                                                                                    {
                                                                                        Console.WriteLine("--------------------in foo try");
                                                                                        if (i % 2 == 1)
                                                                                        {
                                                                                            Console.WriteLine("--------------------throwing an exception [i = {0}]", i);
                                                                                            throw new Exception();
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            goto L19A;
                                                                                        }
                                                                                    }
                                                                                    catch
                                                                                    {
                                                                                        Console.WriteLine("--------------------in foo catch");
                                                                                        i++;
                                                                                    }
                                                                                    finally
                                                                                    {
                                                                                        Console.WriteLine("--------------------in foo finally");
                                                                                        if (i % 2 == 0)
                                                                                        {
                                                                                            Console.WriteLine("--------------------throwing an exception [i = {0}]", i);
                                                                                            throw new Exception();
                                                                                        }
                                                                                    }
                                                                                    L19A:
                                                                                    Console.WriteLine("--------------------foo L19A");
                                                                                }
                                                                                L19:
                                                                                Console.WriteLine("-------------------foo L19");
                                                                                goto L18;
                                                                            }
                                                                            finally
                                                                            {
                                                                                Console.WriteLine("------------------in foo finally");
                                                                                try
                                                                                {
                                                                                    Console.WriteLine("-------------------in foo try");
                                                                                    if (i % 2 == 0)
                                                                                    {
                                                                                        Console.WriteLine("-------------------throwing an exception [i = {0}]", i);
                                                                                        throw new Exception();
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        goto L18A;
                                                                                    }
                                                                                }
                                                                                catch
                                                                                {
                                                                                    Console.WriteLine("-------------------in foo catch");
                                                                                    i++;
                                                                                }
                                                                                finally
                                                                                {
                                                                                    Console.WriteLine("-------------------in foo finally");
                                                                                    if (i % 2 == 1)
                                                                                    {
                                                                                        Console.WriteLine("-------------------throwing an exception [i = {0}]", i);
                                                                                        throw new Exception();
                                                                                    }
                                                                                }
                                                                                L18A:
                                                                                Console.WriteLine("-------------------foo L18A");
                                                                            }
                                                                            L18:
                                                                            Console.WriteLine("------------------foo L18");
                                                                            goto L17;
                                                                        }
                                                                        finally
                                                                        {
                                                                            Console.WriteLine("-----------------in foo finally");
                                                                            try
                                                                            {
                                                                                Console.WriteLine("------------------in foo try");
                                                                                if (i % 2 == 1)
                                                                                {
                                                                                    Console.WriteLine("------------------throwing an exception [i = {0}]", i);
                                                                                    throw new Exception();
                                                                                }
                                                                                else
                                                                                {
                                                                                    goto L17A;
                                                                                }
                                                                            }
                                                                            catch
                                                                            {
                                                                                Console.WriteLine("------------------in foo catch");
                                                                                i++;
                                                                            }
                                                                            finally
                                                                            {
                                                                                Console.WriteLine("------------------in foo finally");
                                                                                if (i % 2 == 0)
                                                                                {
                                                                                    Console.WriteLine("------------------throwing an exception [i = {0}]", i);
                                                                                    throw new Exception();
                                                                                }
                                                                            }
                                                                            L17A:
                                                                            Console.WriteLine("------------------foo L17A");
                                                                        }
                                                                        L17:
                                                                        Console.WriteLine("-----------------foo L17");
                                                                        goto L16;
                                                                    }
                                                                    finally
                                                                    {
                                                                        Console.WriteLine("----------------in foo finally");
                                                                        try
                                                                        {
                                                                            Console.WriteLine("-----------------in foo try");
                                                                            if (i % 2 == 0)
                                                                            {
                                                                                Console.WriteLine("-----------------throwing an exception [i = {0}]", i);
                                                                                throw new Exception();
                                                                            }
                                                                            else
                                                                            {
                                                                                goto L16A;
                                                                            }
                                                                        }
                                                                        catch
                                                                        {
                                                                            Console.WriteLine("-----------------in foo catch");
                                                                            i++;
                                                                        }
                                                                        finally
                                                                        {
                                                                            Console.WriteLine("-----------------in foo finally");
                                                                            if (i % 2 == 1)
                                                                            {
                                                                                Console.WriteLine("-----------------throwing an exception [i = {0}]", i);
                                                                                throw new Exception();
                                                                            }
                                                                        }
                                                                        L16A:
                                                                        Console.WriteLine("-----------------foo L16A");
                                                                    }
                                                                    L16:
                                                                    Console.WriteLine("----------------foo L16");
                                                                    goto L15;
                                                                }
                                                                finally
                                                                {
                                                                    Console.WriteLine("---------------in foo finally");
                                                                    try
                                                                    {
                                                                        Console.WriteLine("----------------in foo try");
                                                                        if (i % 2 == 1)
                                                                        {
                                                                            Console.WriteLine("----------------throwing an exception [i = {0}]", i);
                                                                            throw new Exception();
                                                                        }
                                                                        else
                                                                        {
                                                                            goto L15A;
                                                                        }
                                                                    }
                                                                    catch
                                                                    {
                                                                        Console.WriteLine("----------------in foo catch");
                                                                        i++;
                                                                    }
                                                                    finally
                                                                    {
                                                                        Console.WriteLine("----------------in foo finally");
                                                                        if (i % 2 == 0)
                                                                        {
                                                                            Console.WriteLine("----------------throwing an exception [i = {0}]", i);
                                                                            throw new Exception();
                                                                        }
                                                                    }
                                                                    L15A:
                                                                    Console.WriteLine("----------------foo L15A");
                                                                }
                                                                L15:
                                                                Console.WriteLine("---------------foo L15");
                                                                goto L14;
                                                            }
                                                            finally
                                                            {
                                                                Console.WriteLine("--------------in foo finally");
                                                                try
                                                                {
                                                                    Console.WriteLine("---------------in foo try");
                                                                    if (i % 2 == 0)
                                                                    {
                                                                        Console.WriteLine("---------------throwing an exception [i = {0}]", i);
                                                                        throw new Exception();
                                                                    }
                                                                    else
                                                                    {
                                                                        goto L14A;
                                                                    }
                                                                }
                                                                catch
                                                                {
                                                                    Console.WriteLine("---------------in foo catch");
                                                                    i++;
                                                                }
                                                                finally
                                                                {
                                                                    Console.WriteLine("---------------in foo finally");
                                                                    if (i % 2 == 1)
                                                                    {
                                                                        Console.WriteLine("---------------throwing an exception [i = {0}]", i);
                                                                        throw new Exception();
                                                                    }
                                                                }
                                                                L14A:
                                                                Console.WriteLine("---------------foo L14A");
                                                            }
                                                            L14:
                                                            Console.WriteLine("--------------foo L14");
                                                            goto L13;
                                                        }
                                                        finally
                                                        {
                                                            Console.WriteLine("-------------in foo finally");
                                                            try
                                                            {
                                                                Console.WriteLine("--------------in foo try");
                                                                if (i % 2 == 1)
                                                                {
                                                                    Console.WriteLine("--------------throwing an exception [i = {0}]", i);
                                                                    throw new Exception();
                                                                }
                                                                else
                                                                {
                                                                    goto L13A;
                                                                }
                                                            }
                                                            catch
                                                            {
                                                                Console.WriteLine("--------------in foo catch");
                                                                i++;
                                                            }
                                                            finally
                                                            {
                                                                Console.WriteLine("--------------in foo finally");
                                                                if (i % 2 == 0)
                                                                {
                                                                    Console.WriteLine("--------------throwing an exception [i = {0}]", i);
                                                                    throw new Exception();
                                                                }
                                                            }
                                                            L13A:
                                                            Console.WriteLine("--------------foo L13A");
                                                        }
                                                        L13:
                                                        Console.WriteLine("-------------foo L13");
                                                        goto L12;
                                                    }
                                                    finally
                                                    {
                                                        Console.WriteLine("------------in foo finally");
                                                        try
                                                        {
                                                            Console.WriteLine("-------------in foo try");
                                                            if (i % 2 == 0)
                                                            {
                                                                Console.WriteLine("-------------throwing an exception [i = {0}]", i);
                                                                throw new Exception();
                                                            }
                                                            else
                                                            {
                                                                goto L12A;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.WriteLine("-------------in foo catch");
                                                            i++;
                                                        }
                                                        finally
                                                        {
                                                            Console.WriteLine("-------------in foo finally");
                                                            if (i % 2 == 1)
                                                            {
                                                                Console.WriteLine("-------------throwing an exception [i = {0}]", i);
                                                                throw new Exception();
                                                            }
                                                        }
                                                        L12A:
                                                        Console.WriteLine("-------------foo L12A");
                                                    }
                                                    L12:
                                                    Console.WriteLine("------------foo L12");
                                                    goto L11;
                                                }
                                                finally
                                                {
                                                    Console.WriteLine("-----------in foo finally");
                                                    try
                                                    {
                                                        Console.WriteLine("------------in foo try");
                                                        if (i % 2 == 1)
                                                        {
                                                            Console.WriteLine("------------throwing an exception [i = {0}]", i);
                                                            throw new Exception();
                                                        }
                                                        else
                                                        {
                                                            goto L11A;
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        Console.WriteLine("------------in foo catch");
                                                        i++;
                                                    }
                                                    finally
                                                    {
                                                        Console.WriteLine("------------in foo finally");
                                                        if (i % 2 == 0)
                                                        {
                                                            Console.WriteLine("------------throwing an exception [i = {0}]", i);
                                                            throw new Exception();
                                                        }
                                                    }
                                                    L11A:
                                                    Console.WriteLine("------------foo L11A");
                                                }
                                                L11:
                                                Console.WriteLine("-----------foo L11");
                                                goto L10;
                                            }
                                            finally
                                            {
                                                Console.WriteLine("----------in foo finally");
                                                try
                                                {
                                                    Console.WriteLine("-----------in foo try");
                                                    if (i % 2 == 0)
                                                    {
                                                        Console.WriteLine("-----------throwing an exception [i = {0}]", i);
                                                        throw new Exception();
                                                    }
                                                    else
                                                    {
                                                        goto L10A;
                                                    }
                                                }
                                                catch
                                                {
                                                    Console.WriteLine("-----------in foo catch");
                                                    i++;
                                                }
                                                finally
                                                {
                                                    Console.WriteLine("-----------in foo finally");
                                                    if (i % 2 == 1)
                                                    {
                                                        Console.WriteLine("-----------throwing an exception [i = {0}]", i);
                                                        throw new Exception();
                                                    }
                                                }
                                                L10A:
                                                Console.WriteLine("-----------foo L10A");
                                            }
                                            L10:
                                            Console.WriteLine("----------foo L10");
                                            goto L9;
                                        }
                                        finally
                                        {
                                            Console.WriteLine("---------in foo finally");
                                            try
                                            {
                                                Console.WriteLine("----------in foo try");
                                                if (i % 2 == 1)
                                                {
                                                    Console.WriteLine("----------throwing an exception [i = {0}]", i);
                                                    throw new Exception();
                                                }
                                                else
                                                {
                                                    goto L9A;
                                                }
                                            }
                                            catch
                                            {
                                                Console.WriteLine("----------in foo catch");
                                                i++;
                                            }
                                            finally
                                            {
                                                Console.WriteLine("----------in foo finally");
                                                if (i % 2 == 0)
                                                {
                                                    Console.WriteLine("----------throwing an exception [i = {0}]", i);
                                                    throw new Exception();
                                                }
                                            }
                                            L9A:
                                            Console.WriteLine("----------foo L9A");
                                        }
                                        L9:
                                        Console.WriteLine("---------foo L9");
                                        goto L8;
                                    }
                                    finally
                                    {
                                        Console.WriteLine("--------in foo finally");
                                        try
                                        {
                                            Console.WriteLine("---------in foo try");
                                            if (i % 2 == 0)
                                            {
                                                Console.WriteLine("---------throwing an exception [i = {0}]", i);
                                                throw new Exception();
                                            }
                                            else
                                            {
                                                goto L8A;
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("---------in foo catch");
                                            i++;
                                        }
                                        finally
                                        {
                                            Console.WriteLine("---------in foo finally");
                                            if (i % 2 == 1)
                                            {
                                                Console.WriteLine("---------throwing an exception [i = {0}]", i);
                                                throw new Exception();
                                            }
                                        }
                                        L8A:
                                        Console.WriteLine("---------foo L8A");
                                    }
                                    L8:
                                    Console.WriteLine("--------foo L8");
                                    goto L7;
                                }
                                finally
                                {
                                    Console.WriteLine("-------in foo finally");
                                    try
                                    {
                                        Console.WriteLine("--------in foo try");
                                        if (i % 2 == 1)
                                        {
                                            Console.WriteLine("--------throwing an exception [i = {0}]", i);
                                            throw new Exception();
                                        }
                                        else
                                        {
                                            goto L7A;
                                        }
                                    }
                                    catch
                                    {
                                        Console.WriteLine("--------in foo catch");
                                        i++;
                                    }
                                    finally
                                    {
                                        Console.WriteLine("--------in foo finally");
                                        if (i % 2 == 0)
                                        {
                                            Console.WriteLine("--------throwing an exception [i = {0}]", i);
                                            throw new Exception();
                                        }
                                    }
                                    L7A:
                                    Console.WriteLine("--------foo L7A");
                                }
                                L7:
                                Console.WriteLine("-------foo L7");
                                goto L6;
                            }
                            finally
                            {
                                Console.WriteLine("------in foo finally");
                                try
                                {
                                    Console.WriteLine("-------in foo try");
                                    if (i % 2 == 0)
                                    {
                                        Console.WriteLine("-------throwing an exception [i = {0}]", i);
                                        throw new Exception();
                                    }
                                    else
                                    {
                                        goto L6A;
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("-------in foo catch");
                                    i++;
                                }
                                finally
                                {
                                    Console.WriteLine("-------in foo finally");
                                    if (i % 2 == 1)
                                    {
                                        Console.WriteLine("-------throwing an exception [i = {0}]", i);
                                        throw new Exception();
                                    }
                                }
                                L6A:
                                Console.WriteLine("-------foo L6A");
                            }
                            L6:
                            Console.WriteLine("------foo L6");
                            goto L5;
                        }
                        finally
                        {
                            Console.WriteLine("-----in foo finally");
                            try
                            {
                                Console.WriteLine("------in foo try");
                                if (i % 2 == 1)
                                {
                                    Console.WriteLine("------throwing an exception [i = {0}]", i);
                                    throw new Exception();
                                }
                                else
                                {
                                    goto L5A;
                                }
                            }
                            catch
                            {
                                Console.WriteLine("------in foo catch");
                                i++;
                            }
                            finally
                            {
                                Console.WriteLine("------in foo finally");
                                if (i % 2 == 0)
                                {
                                    Console.WriteLine("------throwing an exception [i = {0}]", i);
                                    throw new Exception();
                                }
                            }
                            L5A:
                            Console.WriteLine("------foo L5A");
                        }
                        L5:
                        Console.WriteLine("-----foo L5");
                        goto L4;
                    }
                    finally
                    {
                        Console.WriteLine("----in foo finally");
                        try
                        {
                            Console.WriteLine("-----in foo try");
                            if (i % 2 == 0)
                            {
                                Console.WriteLine("-----throwing an exception [i = {0}]", i);
                                throw new Exception();
                            }
                            else
                            {
                                goto L4A;
                            }
                        }
                        catch
                        {
                            Console.WriteLine("-----in foo catch");
                            i++;
                        }
                        finally
                        {
                            Console.WriteLine("-----in foo finally");
                            if (i % 2 == 1)
                            {
                                Console.WriteLine("-----throwing an exception [i = {0}]", i);
                                throw new Exception();
                            }
                        }
                        L4A:
                        Console.WriteLine("-----foo L4A");
                    }
                    L4:
                    Console.WriteLine("----foo L4");
                    goto L3;
                }
                finally
                {
                    Console.WriteLine("---in foo finally");
                    try
                    {
                        Console.WriteLine("----in foo try");
                        if (i % 2 == 1)
                        {
                            Console.WriteLine("----throwing an exception [i = {0}]", i);
                            throw new Exception();
                        }
                        else
                        {
                            goto L3A;
                        }
                    }
                    catch
                    {
                        Console.WriteLine("----in foo catch");
                        i++;
                    }
                    finally
                    {
                        Console.WriteLine("----in foo finally");
                        if (i % 2 == 0)
                        {
                            Console.WriteLine("----throwing an exception [i = {0}]", i);
                            throw new Exception();
                        }
                    }
                    L3A:
                    Console.WriteLine("----foo L3A");
                }
                L3:
                Console.WriteLine("---foo L3");
                goto L2;
            }
            finally
            {
                Console.WriteLine("--in foo finally");
                try
                {
                    Console.WriteLine("---in foo try");
                    if (i % 2 == 0)
                    {
                        Console.WriteLine("---throwing an exception [i = {0}]", i);
                        throw new Exception();
                    }
                    else
                    {
                        goto L2A;
                    }
                }
                catch
                {
                    Console.WriteLine("---in foo catch");
                    i++;
                }
                finally
                {
                    Console.WriteLine("---in foo finally");
                    if (i % 2 == 1)
                    {
                        Console.WriteLine("---throwing an exception [i = {0}]", i);
                        throw new Exception();
                    }
                }
                L2A:
                Console.WriteLine("---foo L2A");
            }
            L2:
            Console.WriteLine("--foo L2");
            goto L1;
        }
        finally
        {
            Console.WriteLine("-in foo finally");
            try
            {
                Console.WriteLine("--in foo try");
                if (i % 2 == 1)
                {
                    Console.WriteLine("--throwing an exception [i = {0}]", i);
                    throw new Exception();
                }
                else
                {
                    goto L1A;
                }
            }
            catch
            {
                Console.WriteLine("--in foo catch");
                i++;
            }
            finally
            {
                Console.WriteLine("--in foo finally");
                if (i % 2 == 0)
                {
                    Console.WriteLine("--throwing an exception [i = {0}]", i);
                    throw new Exception();
                }
            }
            L1A:
            Console.WriteLine("--foo L1A");
        }
        L1:
        Console.WriteLine("-foo L1");
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
