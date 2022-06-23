// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_throwinfinally_50_cs
{
// levels of nesting = 50
public class Class1
{
    private static TestUtil.TestLog testLog;

    static Class1()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("in main try");
        expectedOut.WriteLine("-in foo try [1]");
        expectedOut.WriteLine("--in foo try [2]");
        expectedOut.WriteLine("---in foo try [3]");
        expectedOut.WriteLine("----in foo try [4]");
        expectedOut.WriteLine("-----in foo try [5]");
        expectedOut.WriteLine("------in foo try [6]");
        expectedOut.WriteLine("-------in foo try [7]");
        expectedOut.WriteLine("--------in foo try [8]");
        expectedOut.WriteLine("---------in foo try [9]");
        expectedOut.WriteLine("----------in foo try [10]");
        expectedOut.WriteLine("-----------in foo try [11]");
        expectedOut.WriteLine("------------in foo try [12]");
        expectedOut.WriteLine("-------------in foo try [13]");
        expectedOut.WriteLine("--------------in foo try [14]");
        expectedOut.WriteLine("---------------in foo try [15]");
        expectedOut.WriteLine("----------------in foo try [16]");
        expectedOut.WriteLine("-----------------in foo try [17]");
        expectedOut.WriteLine("------------------in foo try [18]");
        expectedOut.WriteLine("-------------------in foo try [19]");
        expectedOut.WriteLine("--------------------in foo try [20]");
        expectedOut.WriteLine("---------------------in foo try [21]");
        expectedOut.WriteLine("----------------------in foo try [22]");
        expectedOut.WriteLine("-----------------------in foo try [23]");
        expectedOut.WriteLine("------------------------in foo try [24]");
        expectedOut.WriteLine("-------------------------in foo try [25]");
        expectedOut.WriteLine("--------------------------in foo try [26]");
        expectedOut.WriteLine("---------------------------in foo try [27]");
        expectedOut.WriteLine("----------------------------in foo try [28]");
        expectedOut.WriteLine("-----------------------------in foo try [29]");
        expectedOut.WriteLine("------------------------------in foo try [30]");
        expectedOut.WriteLine("-------------------------------in foo try [31]");
        expectedOut.WriteLine("--------------------------------in foo try [32]");
        expectedOut.WriteLine("---------------------------------in foo try [33]");
        expectedOut.WriteLine("----------------------------------in foo try [34]");
        expectedOut.WriteLine("-----------------------------------in foo try [35]");
        expectedOut.WriteLine("------------------------------------in foo try [36]");
        expectedOut.WriteLine("-------------------------------------in foo try [37]");
        expectedOut.WriteLine("--------------------------------------in foo try [38]");
        expectedOut.WriteLine("---------------------------------------in foo try [39]");
        expectedOut.WriteLine("----------------------------------------in foo try [40]");
        expectedOut.WriteLine("-----------------------------------------in foo try [41]");
        expectedOut.WriteLine("------------------------------------------in foo try [42]");
        expectedOut.WriteLine("-------------------------------------------in foo try [43]");
        expectedOut.WriteLine("--------------------------------------------in foo try [44]");
        expectedOut.WriteLine("---------------------------------------------in foo try [45]");
        expectedOut.WriteLine("----------------------------------------------in foo try [46]");
        expectedOut.WriteLine("-----------------------------------------------in foo try [47]");
        expectedOut.WriteLine("------------------------------------------------in foo try [48]");
        expectedOut.WriteLine("-------------------------------------------------in foo try [49]");
        expectedOut.WriteLine("--------------------------------------------------in foo try [50]");
        expectedOut.WriteLine("--------------------------------------------------in foo finally [50]");
        expectedOut.WriteLine("-------------------------------------------------in foo catch [49]");
        expectedOut.WriteLine("-------------------------------------------------in foo finally [49]");
        expectedOut.WriteLine("------------------------------------------------in foo catch [48]");
        expectedOut.WriteLine("------------------------------------------------in foo finally [48]");
        expectedOut.WriteLine("-----------------------------------------------in foo catch [47]");
        expectedOut.WriteLine("-----------------------------------------------in foo finally [47]");
        expectedOut.WriteLine("----------------------------------------------in foo catch [46]");
        expectedOut.WriteLine("----------------------------------------------in foo finally [46]");
        expectedOut.WriteLine("---------------------------------------------in foo catch [45]");
        expectedOut.WriteLine("---------------------------------------------in foo finally [45]");
        expectedOut.WriteLine("--------------------------------------------in foo catch [44]");
        expectedOut.WriteLine("--------------------------------------------in foo finally [44]");
        expectedOut.WriteLine("-------------------------------------------in foo catch [43]");
        expectedOut.WriteLine("-------------------------------------------in foo finally [43]");
        expectedOut.WriteLine("------------------------------------------in foo catch [42]");
        expectedOut.WriteLine("------------------------------------------in foo finally [42]");
        expectedOut.WriteLine("-----------------------------------------in foo catch [41]");
        expectedOut.WriteLine("-----------------------------------------in foo finally [41]");
        expectedOut.WriteLine("----------------------------------------in foo catch [40]");
        expectedOut.WriteLine("----------------------------------------in foo finally [40]");
        expectedOut.WriteLine("---------------------------------------in foo catch [39]");
        expectedOut.WriteLine("---------------------------------------in foo finally [39]");
        expectedOut.WriteLine("--------------------------------------in foo catch [38]");
        expectedOut.WriteLine("--------------------------------------in foo finally [38]");
        expectedOut.WriteLine("-------------------------------------in foo catch [37]");
        expectedOut.WriteLine("-------------------------------------in foo finally [37]");
        expectedOut.WriteLine("------------------------------------in foo catch [36]");
        expectedOut.WriteLine("------------------------------------in foo finally [36]");
        expectedOut.WriteLine("-----------------------------------in foo catch [35]");
        expectedOut.WriteLine("-----------------------------------in foo finally [35]");
        expectedOut.WriteLine("----------------------------------in foo catch [34]");
        expectedOut.WriteLine("----------------------------------in foo finally [34]");
        expectedOut.WriteLine("---------------------------------in foo catch [33]");
        expectedOut.WriteLine("---------------------------------in foo finally [33]");
        expectedOut.WriteLine("--------------------------------in foo catch [32]");
        expectedOut.WriteLine("--------------------------------in foo finally [32]");
        expectedOut.WriteLine("-------------------------------in foo catch [31]");
        expectedOut.WriteLine("-------------------------------in foo finally [31]");
        expectedOut.WriteLine("------------------------------in foo catch [30]");
        expectedOut.WriteLine("------------------------------in foo finally [30]");
        expectedOut.WriteLine("-----------------------------in foo catch [29]");
        expectedOut.WriteLine("-----------------------------in foo finally [29]");
        expectedOut.WriteLine("----------------------------in foo catch [28]");
        expectedOut.WriteLine("----------------------------in foo finally [28]");
        expectedOut.WriteLine("---------------------------in foo catch [27]");
        expectedOut.WriteLine("---------------------------in foo finally [27]");
        expectedOut.WriteLine("--------------------------in foo catch [26]");
        expectedOut.WriteLine("--------------------------in foo finally [26]");
        expectedOut.WriteLine("-------------------------in foo catch [25]");
        expectedOut.WriteLine("-------------------------in foo finally [25]");
        expectedOut.WriteLine("------------------------in foo catch [24]");
        expectedOut.WriteLine("------------------------in foo finally [24]");
        expectedOut.WriteLine("-----------------------in foo catch [23]");
        expectedOut.WriteLine("-----------------------in foo finally [23]");
        expectedOut.WriteLine("----------------------in foo catch [22]");
        expectedOut.WriteLine("----------------------in foo finally [22]");
        expectedOut.WriteLine("---------------------in foo catch [21]");
        expectedOut.WriteLine("---------------------in foo finally [21]");
        expectedOut.WriteLine("--------------------in foo catch [20]");
        expectedOut.WriteLine("--------------------in foo finally [20]");
        expectedOut.WriteLine("-------------------in foo catch [19]");
        expectedOut.WriteLine("-------------------in foo finally [19]");
        expectedOut.WriteLine("------------------in foo catch [18]");
        expectedOut.WriteLine("------------------in foo finally [18]");
        expectedOut.WriteLine("-----------------in foo catch [17]");
        expectedOut.WriteLine("-----------------in foo finally [17]");
        expectedOut.WriteLine("----------------in foo catch [16]");
        expectedOut.WriteLine("----------------in foo finally [16]");
        expectedOut.WriteLine("---------------in foo catch [15]");
        expectedOut.WriteLine("---------------in foo finally [15]");
        expectedOut.WriteLine("--------------in foo catch [14]");
        expectedOut.WriteLine("--------------in foo finally [14]");
        expectedOut.WriteLine("-------------in foo catch [13]");
        expectedOut.WriteLine("-------------in foo finally [13]");
        expectedOut.WriteLine("------------in foo catch [12]");
        expectedOut.WriteLine("------------in foo finally [12]");
        expectedOut.WriteLine("-----------in foo catch [11]");
        expectedOut.WriteLine("-----------in foo finally [11]");
        expectedOut.WriteLine("----------in foo catch [10]");
        expectedOut.WriteLine("----------in foo finally [10]");
        expectedOut.WriteLine("---------in foo catch [9]");
        expectedOut.WriteLine("---------in foo finally [9]");
        expectedOut.WriteLine("--------in foo catch [8]");
        expectedOut.WriteLine("--------in foo finally [8]");
        expectedOut.WriteLine("-------in foo catch [7]");
        expectedOut.WriteLine("-------in foo finally [7]");
        expectedOut.WriteLine("------in foo catch [6]");
        expectedOut.WriteLine("------in foo finally [6]");
        expectedOut.WriteLine("-----in foo catch [5]");
        expectedOut.WriteLine("-----in foo finally [5]");
        expectedOut.WriteLine("----in foo catch [4]");
        expectedOut.WriteLine("----in foo finally [4]");
        expectedOut.WriteLine("---in foo catch [3]");
        expectedOut.WriteLine("---in foo finally [3]");
        expectedOut.WriteLine("--in foo catch [2]");
        expectedOut.WriteLine("--in foo finally [2]");
        expectedOut.WriteLine("-in foo catch [1]");
        expectedOut.WriteLine("-in foo finally [1]");
        expectedOut.WriteLine("in main catch");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }

    static public void foo(int i)
    {
        try
        {
            Console.WriteLine("-in foo try [1]");
            try
            {
                Console.WriteLine("--in foo try [2]");
                try
                {
                    Console.WriteLine("---in foo try [3]");
                    try
                    {
                        Console.WriteLine("----in foo try [4]");
                        try
                        {
                            Console.WriteLine("-----in foo try [5]");
                            try
                            {
                                Console.WriteLine("------in foo try [6]");
                                try
                                {
                                    Console.WriteLine("-------in foo try [7]");
                                    try
                                    {
                                        Console.WriteLine("--------in foo try [8]");
                                        try
                                        {
                                            Console.WriteLine("---------in foo try [9]");
                                            try
                                            {
                                                Console.WriteLine("----------in foo try [10]");
                                                try
                                                {
                                                    Console.WriteLine("-----------in foo try [11]");
                                                    try
                                                    {
                                                        Console.WriteLine("------------in foo try [12]");
                                                        try
                                                        {
                                                            Console.WriteLine("-------------in foo try [13]");
                                                            try
                                                            {
                                                                Console.WriteLine("--------------in foo try [14]");
                                                                try
                                                                {
                                                                    Console.WriteLine("---------------in foo try [15]");
                                                                    try
                                                                    {
                                                                        Console.WriteLine("----------------in foo try [16]");
                                                                        try
                                                                        {
                                                                            Console.WriteLine("-----------------in foo try [17]");
                                                                            try
                                                                            {
                                                                                Console.WriteLine("------------------in foo try [18]");
                                                                                try
                                                                                {
                                                                                    Console.WriteLine("-------------------in foo try [19]");
                                                                                    try
                                                                                    {
                                                                                        Console.WriteLine("--------------------in foo try [20]");
                                                                                        try
                                                                                        {
                                                                                            Console.WriteLine("---------------------in foo try [21]");
                                                                                            try
                                                                                            {
                                                                                                Console.WriteLine("----------------------in foo try [22]");
                                                                                                try
                                                                                                {
                                                                                                    Console.WriteLine("-----------------------in foo try [23]");
                                                                                                    try
                                                                                                    {
                                                                                                        Console.WriteLine("------------------------in foo try [24]");
                                                                                                        try
                                                                                                        {
                                                                                                            Console.WriteLine("-------------------------in foo try [25]");
                                                                                                            try
                                                                                                            {
                                                                                                                Console.WriteLine("--------------------------in foo try [26]");
                                                                                                                try
                                                                                                                {
                                                                                                                    Console.WriteLine("---------------------------in foo try [27]");
                                                                                                                    try
                                                                                                                    {
                                                                                                                        Console.WriteLine("----------------------------in foo try [28]");
                                                                                                                        try
                                                                                                                        {
                                                                                                                            Console.WriteLine("-----------------------------in foo try [29]");
                                                                                                                            try
                                                                                                                            {
                                                                                                                                Console.WriteLine("------------------------------in foo try [30]");
                                                                                                                                try
                                                                                                                                {
                                                                                                                                    Console.WriteLine("-------------------------------in foo try [31]");
                                                                                                                                    try
                                                                                                                                    {
                                                                                                                                        Console.WriteLine("--------------------------------in foo try [32]");
                                                                                                                                        try
                                                                                                                                        {
                                                                                                                                            Console.WriteLine("---------------------------------in foo try [33]");
                                                                                                                                            try
                                                                                                                                            {
                                                                                                                                                Console.WriteLine("----------------------------------in foo try [34]");
                                                                                                                                                try
                                                                                                                                                {
                                                                                                                                                    Console.WriteLine("-----------------------------------in foo try [35]");
                                                                                                                                                    try
                                                                                                                                                    {
                                                                                                                                                        Console.WriteLine("------------------------------------in foo try [36]");
                                                                                                                                                        try
                                                                                                                                                        {
                                                                                                                                                            Console.WriteLine("-------------------------------------in foo try [37]");
                                                                                                                                                            try
                                                                                                                                                            {
                                                                                                                                                                Console.WriteLine("--------------------------------------in foo try [38]");
                                                                                                                                                                try
                                                                                                                                                                {
                                                                                                                                                                    Console.WriteLine("---------------------------------------in foo try [39]");
                                                                                                                                                                    try
                                                                                                                                                                    {
                                                                                                                                                                        Console.WriteLine("----------------------------------------in foo try [40]");
                                                                                                                                                                        try
                                                                                                                                                                        {
                                                                                                                                                                            Console.WriteLine("-----------------------------------------in foo try [41]");
                                                                                                                                                                            try
                                                                                                                                                                            {
                                                                                                                                                                                Console.WriteLine("------------------------------------------in foo try [42]");
                                                                                                                                                                                try
                                                                                                                                                                                {
                                                                                                                                                                                    Console.WriteLine("-------------------------------------------in foo try [43]");
                                                                                                                                                                                    try
                                                                                                                                                                                    {
                                                                                                                                                                                        Console.WriteLine("--------------------------------------------in foo try [44]");
                                                                                                                                                                                        try
                                                                                                                                                                                        {
                                                                                                                                                                                            Console.WriteLine("---------------------------------------------in foo try [45]");
                                                                                                                                                                                            try
                                                                                                                                                                                            {
                                                                                                                                                                                                Console.WriteLine("----------------------------------------------in foo try [46]");
                                                                                                                                                                                                try
                                                                                                                                                                                                {
                                                                                                                                                                                                    Console.WriteLine("-----------------------------------------------in foo try [47]");
                                                                                                                                                                                                    try
                                                                                                                                                                                                    {
                                                                                                                                                                                                        Console.WriteLine("------------------------------------------------in foo try [48]");
                                                                                                                                                                                                        try
                                                                                                                                                                                                        {
                                                                                                                                                                                                            Console.WriteLine("-------------------------------------------------in foo try [49]");
                                                                                                                                                                                                            try
                                                                                                                                                                                                            {
                                                                                                                                                                                                                Console.WriteLine("--------------------------------------------------in foo try [50]");
                                                                                                                                                                                                                if (i == 0) goto L1;
                                                                                                                                                                                                            }
                                                                                                                                                                                                            catch
                                                                                                                                                                                                            {
                                                                                                                                                                                                                Console.WriteLine("--------------------------------------------------in foo catch [50]");
                                                                                                                                                                                                            }
                                                                                                                                                                                                            finally
                                                                                                                                                                                                            {
                                                                                                                                                                                                                Console.WriteLine("--------------------------------------------------in foo finally [50]");
                                                                                                                                                                                                                if (i == 0) throw new Exception();
                                                                                                                                                                                                            }
                                                                                                                                                                                                        }
                                                                                                                                                                                                        catch
                                                                                                                                                                                                        {
                                                                                                                                                                                                            Console.WriteLine("-------------------------------------------------in foo catch [49]");
                                                                                                                                                                                                        }
                                                                                                                                                                                                        finally
                                                                                                                                                                                                        {
                                                                                                                                                                                                            Console.WriteLine("-------------------------------------------------in foo finally [49]");
                                                                                                                                                                                                            if (i == 0) throw new Exception();
                                                                                                                                                                                                        }
                                                                                                                                                                                                    }
                                                                                                                                                                                                    catch
                                                                                                                                                                                                    {
                                                                                                                                                                                                        Console.WriteLine("------------------------------------------------in foo catch [48]");
                                                                                                                                                                                                    }
                                                                                                                                                                                                    finally
                                                                                                                                                                                                    {
                                                                                                                                                                                                        Console.WriteLine("------------------------------------------------in foo finally [48]");
                                                                                                                                                                                                        if (i == 0) throw new Exception();
                                                                                                                                                                                                    }
                                                                                                                                                                                                }
                                                                                                                                                                                                catch
                                                                                                                                                                                                {
                                                                                                                                                                                                    Console.WriteLine("-----------------------------------------------in foo catch [47]");
                                                                                                                                                                                                }
                                                                                                                                                                                                finally
                                                                                                                                                                                                {
                                                                                                                                                                                                    Console.WriteLine("-----------------------------------------------in foo finally [47]");
                                                                                                                                                                                                    if (i == 0) throw new Exception();
                                                                                                                                                                                                }
                                                                                                                                                                                            }
                                                                                                                                                                                            catch
                                                                                                                                                                                            {
                                                                                                                                                                                                Console.WriteLine("----------------------------------------------in foo catch [46]");
                                                                                                                                                                                            }
                                                                                                                                                                                            finally
                                                                                                                                                                                            {
                                                                                                                                                                                                Console.WriteLine("----------------------------------------------in foo finally [46]");
                                                                                                                                                                                                if (i == 0) throw new Exception();
                                                                                                                                                                                            }
                                                                                                                                                                                        }
                                                                                                                                                                                        catch
                                                                                                                                                                                        {
                                                                                                                                                                                            Console.WriteLine("---------------------------------------------in foo catch [45]");
                                                                                                                                                                                        }
                                                                                                                                                                                        finally
                                                                                                                                                                                        {
                                                                                                                                                                                            Console.WriteLine("---------------------------------------------in foo finally [45]");
                                                                                                                                                                                            if (i == 0) throw new Exception();
                                                                                                                                                                                        }
                                                                                                                                                                                    }
                                                                                                                                                                                    catch
                                                                                                                                                                                    {
                                                                                                                                                                                        Console.WriteLine("--------------------------------------------in foo catch [44]");
                                                                                                                                                                                    }
                                                                                                                                                                                    finally
                                                                                                                                                                                    {
                                                                                                                                                                                        Console.WriteLine("--------------------------------------------in foo finally [44]");
                                                                                                                                                                                        if (i == 0) throw new Exception();
                                                                                                                                                                                    }
                                                                                                                                                                                }
                                                                                                                                                                                catch
                                                                                                                                                                                {
                                                                                                                                                                                    Console.WriteLine("-------------------------------------------in foo catch [43]");
                                                                                                                                                                                }
                                                                                                                                                                                finally
                                                                                                                                                                                {
                                                                                                                                                                                    Console.WriteLine("-------------------------------------------in foo finally [43]");
                                                                                                                                                                                    if (i == 0) throw new Exception();
                                                                                                                                                                                }
                                                                                                                                                                            }
                                                                                                                                                                            catch
                                                                                                                                                                            {
                                                                                                                                                                                Console.WriteLine("------------------------------------------in foo catch [42]");
                                                                                                                                                                            }
                                                                                                                                                                            finally
                                                                                                                                                                            {
                                                                                                                                                                                Console.WriteLine("------------------------------------------in foo finally [42]");
                                                                                                                                                                                if (i == 0) throw new Exception();
                                                                                                                                                                            }
                                                                                                                                                                        }
                                                                                                                                                                        catch
                                                                                                                                                                        {
                                                                                                                                                                            Console.WriteLine("-----------------------------------------in foo catch [41]");
                                                                                                                                                                        }
                                                                                                                                                                        finally
                                                                                                                                                                        {
                                                                                                                                                                            Console.WriteLine("-----------------------------------------in foo finally [41]");
                                                                                                                                                                            if (i == 0) throw new Exception();
                                                                                                                                                                        }
                                                                                                                                                                    }
                                                                                                                                                                    catch
                                                                                                                                                                    {
                                                                                                                                                                        Console.WriteLine("----------------------------------------in foo catch [40]");
                                                                                                                                                                    }
                                                                                                                                                                    finally
                                                                                                                                                                    {
                                                                                                                                                                        Console.WriteLine("----------------------------------------in foo finally [40]");
                                                                                                                                                                        if (i == 0) throw new Exception();
                                                                                                                                                                    }
                                                                                                                                                                }
                                                                                                                                                                catch
                                                                                                                                                                {
                                                                                                                                                                    Console.WriteLine("---------------------------------------in foo catch [39]");
                                                                                                                                                                }
                                                                                                                                                                finally
                                                                                                                                                                {
                                                                                                                                                                    Console.WriteLine("---------------------------------------in foo finally [39]");
                                                                                                                                                                    if (i == 0) throw new Exception();
                                                                                                                                                                }
                                                                                                                                                            }
                                                                                                                                                            catch
                                                                                                                                                            {
                                                                                                                                                                Console.WriteLine("--------------------------------------in foo catch [38]");
                                                                                                                                                            }
                                                                                                                                                            finally
                                                                                                                                                            {
                                                                                                                                                                Console.WriteLine("--------------------------------------in foo finally [38]");
                                                                                                                                                                if (i == 0) throw new Exception();
                                                                                                                                                            }
                                                                                                                                                        }
                                                                                                                                                        catch
                                                                                                                                                        {
                                                                                                                                                            Console.WriteLine("-------------------------------------in foo catch [37]");
                                                                                                                                                        }
                                                                                                                                                        finally
                                                                                                                                                        {
                                                                                                                                                            Console.WriteLine("-------------------------------------in foo finally [37]");
                                                                                                                                                            if (i == 0) throw new Exception();
                                                                                                                                                        }
                                                                                                                                                    }
                                                                                                                                                    catch
                                                                                                                                                    {
                                                                                                                                                        Console.WriteLine("------------------------------------in foo catch [36]");
                                                                                                                                                    }
                                                                                                                                                    finally
                                                                                                                                                    {
                                                                                                                                                        Console.WriteLine("------------------------------------in foo finally [36]");
                                                                                                                                                        if (i == 0) throw new Exception();
                                                                                                                                                    }
                                                                                                                                                }
                                                                                                                                                catch
                                                                                                                                                {
                                                                                                                                                    Console.WriteLine("-----------------------------------in foo catch [35]");
                                                                                                                                                }
                                                                                                                                                finally
                                                                                                                                                {
                                                                                                                                                    Console.WriteLine("-----------------------------------in foo finally [35]");
                                                                                                                                                    if (i == 0) throw new Exception();
                                                                                                                                                }
                                                                                                                                            }
                                                                                                                                            catch
                                                                                                                                            {
                                                                                                                                                Console.WriteLine("----------------------------------in foo catch [34]");
                                                                                                                                            }
                                                                                                                                            finally
                                                                                                                                            {
                                                                                                                                                Console.WriteLine("----------------------------------in foo finally [34]");
                                                                                                                                                if (i == 0) throw new Exception();
                                                                                                                                            }
                                                                                                                                        }
                                                                                                                                        catch
                                                                                                                                        {
                                                                                                                                            Console.WriteLine("---------------------------------in foo catch [33]");
                                                                                                                                        }
                                                                                                                                        finally
                                                                                                                                        {
                                                                                                                                            Console.WriteLine("---------------------------------in foo finally [33]");
                                                                                                                                            if (i == 0) throw new Exception();
                                                                                                                                        }
                                                                                                                                    }
                                                                                                                                    catch
                                                                                                                                    {
                                                                                                                                        Console.WriteLine("--------------------------------in foo catch [32]");
                                                                                                                                    }
                                                                                                                                    finally
                                                                                                                                    {
                                                                                                                                        Console.WriteLine("--------------------------------in foo finally [32]");
                                                                                                                                        if (i == 0) throw new Exception();
                                                                                                                                    }
                                                                                                                                }
                                                                                                                                catch
                                                                                                                                {
                                                                                                                                    Console.WriteLine("-------------------------------in foo catch [31]");
                                                                                                                                }
                                                                                                                                finally
                                                                                                                                {
                                                                                                                                    Console.WriteLine("-------------------------------in foo finally [31]");
                                                                                                                                    if (i == 0) throw new Exception();
                                                                                                                                }
                                                                                                                            }
                                                                                                                            catch
                                                                                                                            {
                                                                                                                                Console.WriteLine("------------------------------in foo catch [30]");
                                                                                                                            }
                                                                                                                            finally
                                                                                                                            {
                                                                                                                                Console.WriteLine("------------------------------in foo finally [30]");
                                                                                                                                if (i == 0) throw new Exception();
                                                                                                                            }
                                                                                                                        }
                                                                                                                        catch
                                                                                                                        {
                                                                                                                            Console.WriteLine("-----------------------------in foo catch [29]");
                                                                                                                        }
                                                                                                                        finally
                                                                                                                        {
                                                                                                                            Console.WriteLine("-----------------------------in foo finally [29]");
                                                                                                                            if (i == 0) throw new Exception();
                                                                                                                        }
                                                                                                                    }
                                                                                                                    catch
                                                                                                                    {
                                                                                                                        Console.WriteLine("----------------------------in foo catch [28]");
                                                                                                                    }
                                                                                                                    finally
                                                                                                                    {
                                                                                                                        Console.WriteLine("----------------------------in foo finally [28]");
                                                                                                                        if (i == 0) throw new Exception();
                                                                                                                    }
                                                                                                                }
                                                                                                                catch
                                                                                                                {
                                                                                                                    Console.WriteLine("---------------------------in foo catch [27]");
                                                                                                                }
                                                                                                                finally
                                                                                                                {
                                                                                                                    Console.WriteLine("---------------------------in foo finally [27]");
                                                                                                                    if (i == 0) throw new Exception();
                                                                                                                }
                                                                                                            }
                                                                                                            catch
                                                                                                            {
                                                                                                                Console.WriteLine("--------------------------in foo catch [26]");
                                                                                                            }
                                                                                                            finally
                                                                                                            {
                                                                                                                Console.WriteLine("--------------------------in foo finally [26]");
                                                                                                                if (i == 0) throw new Exception();
                                                                                                            }
                                                                                                        }
                                                                                                        catch
                                                                                                        {
                                                                                                            Console.WriteLine("-------------------------in foo catch [25]");
                                                                                                        }
                                                                                                        finally
                                                                                                        {
                                                                                                            Console.WriteLine("-------------------------in foo finally [25]");
                                                                                                            if (i == 0) throw new Exception();
                                                                                                        }
                                                                                                    }
                                                                                                    catch
                                                                                                    {
                                                                                                        Console.WriteLine("------------------------in foo catch [24]");
                                                                                                    }
                                                                                                    finally
                                                                                                    {
                                                                                                        Console.WriteLine("------------------------in foo finally [24]");
                                                                                                        if (i == 0) throw new Exception();
                                                                                                    }
                                                                                                }
                                                                                                catch
                                                                                                {
                                                                                                    Console.WriteLine("-----------------------in foo catch [23]");
                                                                                                }
                                                                                                finally
                                                                                                {
                                                                                                    Console.WriteLine("-----------------------in foo finally [23]");
                                                                                                    if (i == 0) throw new Exception();
                                                                                                }
                                                                                            }
                                                                                            catch
                                                                                            {
                                                                                                Console.WriteLine("----------------------in foo catch [22]");
                                                                                            }
                                                                                            finally
                                                                                            {
                                                                                                Console.WriteLine("----------------------in foo finally [22]");
                                                                                                if (i == 0) throw new Exception();
                                                                                            }
                                                                                        }
                                                                                        catch
                                                                                        {
                                                                                            Console.WriteLine("---------------------in foo catch [21]");
                                                                                        }
                                                                                        finally
                                                                                        {
                                                                                            Console.WriteLine("---------------------in foo finally [21]");
                                                                                            if (i == 0) throw new Exception();
                                                                                        }
                                                                                    }
                                                                                    catch
                                                                                    {
                                                                                        Console.WriteLine("--------------------in foo catch [20]");
                                                                                    }
                                                                                    finally
                                                                                    {
                                                                                        Console.WriteLine("--------------------in foo finally [20]");
                                                                                        if (i == 0) throw new Exception();
                                                                                    }
                                                                                }
                                                                                catch
                                                                                {
                                                                                    Console.WriteLine("-------------------in foo catch [19]");
                                                                                }
                                                                                finally
                                                                                {
                                                                                    Console.WriteLine("-------------------in foo finally [19]");
                                                                                    if (i == 0) throw new Exception();
                                                                                }
                                                                            }
                                                                            catch
                                                                            {
                                                                                Console.WriteLine("------------------in foo catch [18]");
                                                                            }
                                                                            finally
                                                                            {
                                                                                Console.WriteLine("------------------in foo finally [18]");
                                                                                if (i == 0) throw new Exception();
                                                                            }
                                                                        }
                                                                        catch
                                                                        {
                                                                            Console.WriteLine("-----------------in foo catch [17]");
                                                                        }
                                                                        finally
                                                                        {
                                                                            Console.WriteLine("-----------------in foo finally [17]");
                                                                            if (i == 0) throw new Exception();
                                                                        }
                                                                    }
                                                                    catch
                                                                    {
                                                                        Console.WriteLine("----------------in foo catch [16]");
                                                                    }
                                                                    finally
                                                                    {
                                                                        Console.WriteLine("----------------in foo finally [16]");
                                                                        if (i == 0) throw new Exception();
                                                                    }
                                                                }
                                                                catch
                                                                {
                                                                    Console.WriteLine("---------------in foo catch [15]");
                                                                }
                                                                finally
                                                                {
                                                                    Console.WriteLine("---------------in foo finally [15]");
                                                                    if (i == 0) throw new Exception();
                                                                }
                                                            }
                                                            catch
                                                            {
                                                                Console.WriteLine("--------------in foo catch [14]");
                                                            }
                                                            finally
                                                            {
                                                                Console.WriteLine("--------------in foo finally [14]");
                                                                if (i == 0) throw new Exception();
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.WriteLine("-------------in foo catch [13]");
                                                        }
                                                        finally
                                                        {
                                                            Console.WriteLine("-------------in foo finally [13]");
                                                            if (i == 0) throw new Exception();
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        Console.WriteLine("------------in foo catch [12]");
                                                    }
                                                    finally
                                                    {
                                                        Console.WriteLine("------------in foo finally [12]");
                                                        if (i == 0) throw new Exception();
                                                    }
                                                }
                                                catch
                                                {
                                                    Console.WriteLine("-----------in foo catch [11]");
                                                }
                                                finally
                                                {
                                                    Console.WriteLine("-----------in foo finally [11]");
                                                    if (i == 0) throw new Exception();
                                                }
                                            }
                                            catch
                                            {
                                                Console.WriteLine("----------in foo catch [10]");
                                            }
                                            finally
                                            {
                                                Console.WriteLine("----------in foo finally [10]");
                                                if (i == 0) throw new Exception();
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("---------in foo catch [9]");
                                        }
                                        finally
                                        {
                                            Console.WriteLine("---------in foo finally [9]");
                                            if (i == 0) throw new Exception();
                                        }
                                    }
                                    catch
                                    {
                                        Console.WriteLine("--------in foo catch [8]");
                                    }
                                    finally
                                    {
                                        Console.WriteLine("--------in foo finally [8]");
                                        if (i == 0) throw new Exception();
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("-------in foo catch [7]");
                                }
                                finally
                                {
                                    Console.WriteLine("-------in foo finally [7]");
                                    if (i == 0) throw new Exception();
                                }
                            }
                            catch
                            {
                                Console.WriteLine("------in foo catch [6]");
                            }
                            finally
                            {
                                Console.WriteLine("------in foo finally [6]");
                                if (i == 0) throw new Exception();
                            }
                        }
                        catch
                        {
                            Console.WriteLine("-----in foo catch [5]");
                        }
                        finally
                        {
                            Console.WriteLine("-----in foo finally [5]");
                            if (i == 0) throw new Exception();
                        }
                    }
                    catch
                    {
                        Console.WriteLine("----in foo catch [4]");
                    }
                    finally
                    {
                        Console.WriteLine("----in foo finally [4]");
                        if (i == 0) throw new Exception();
                    }
                }
                catch
                {
                    Console.WriteLine("---in foo catch [3]");
                }
                finally
                {
                    Console.WriteLine("---in foo finally [3]");
                    if (i == 0) throw new Exception();
                }
            }
            catch
            {
                Console.WriteLine("--in foo catch [2]");
            }
            finally
            {
                Console.WriteLine("--in foo finally [2]");
                if (i == 0) throw new Exception();
            }
        }
        catch
        {
            Console.WriteLine("-in foo catch [1]");
        }
        finally
        {
            Console.WriteLine("-in foo finally [1]");
            if (i == 0) throw new Exception();
        }
        Console.WriteLine("after finally");
        L1:
        Console.WriteLine("foo L1");
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
