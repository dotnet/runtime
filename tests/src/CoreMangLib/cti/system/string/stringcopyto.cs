// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using TestLibrary;


public class StringCopyTo
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;
    public static int Main()
    {
        StringCopyTo sct = new StringCopyTo();
        TestLibrary.TestFramework.BeginTestCase("StringCopyTo");

        if (sct.RunTests())
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("FAIL");
            return 0;
        }
    }

    public bool RunTests()
    {
        bool retVal = true;
        TestLibrary.TestFramework.LogInformation("[Postive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;

        return retVal;
    }
    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        string strA;
        int srcIndex;
        char[] dst;
        int dstIndex;
        int count;

        TestLibrary.TestFramework.BeginScenario("PosTest1:empty string excute CopyTo empty char array dst");

        try
        {
            strA = "";
            srcIndex = 0;
            dst = new char[] {};
            dstIndex  = 0;
            count = 0;
            strA.CopyTo(srcIndex, dst, dstIndex, count);
            if(dst.Length != 0)
            {
                TestLibrary.TestFramework.LogError("001", "empty string excute CopyTo empty char array dst ExpectResult the dst is empty char array,ActualResult is " + dst + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string strA;
        int srcIndex;
        char[] dst;
        int dstIndex;
        int count;

        TestLibrary.TestFramework.BeginScenario("PosTest2:In CoptTo method this string length equels to the char array length and copy all string to char array");

        try
        {
            string strBasic1 = TestLibrary.Generator.GetString(-55, false, c_MAX_STRING_LENGTH, c_MAX_STRING_LENGTH);
            string strBasic2 = TestLibrary.Generator.GetString(-55, false, c_MAX_STRING_LENGTH, c_MAX_STRING_LENGTH);
            strA = strBasic1;//"abc";
            srcIndex = 0;
            dst = strBasic2.ToCharArray();
            dstIndex = 0;
            count = strA.Length;
            strA.CopyTo(srcIndex, dst, dstIndex, count);
            string dst2 = new string(dst);
            if (dst2.ToString () != strA.ToString())
            {
                TestLibrary.TestFramework.LogError("003", "In CoptTo method this string length equels to the char array length and copy all string to char array ExpectResult dst's element is replaced by strA's symbols ,ActualResult is " + dst2 + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string strA;
        int srcIndex;
        char[] dst;
        int dstIndex;
        int count;
        Random rand = new Random(-55);

        TestLibrary.TestFramework.BeginScenario("PosTest3:In CoptTo method this string length equels to the char array length but copy count is 0");

        try
        {
            string strBasic1 = TestLibrary.Generator.GetString(-55, false, c_MAX_STRING_LENGTH, c_MAX_STRING_LENGTH);
            string strBasic2 = TestLibrary.Generator.GetString(-55, false, c_MAX_STRING_LENGTH, c_MAX_STRING_LENGTH);
            strA = strBasic1;
            srcIndex = 0;
            dst = strBasic2.ToCharArray();
            dstIndex = rand.Next(0, dst.Length + 1);
            count = 0;
            strA.CopyTo(srcIndex, dst, dstIndex, count);
            string dst2 = new string(dst);
            if (dst2.ToString() != strBasic2.ToString())
            {
                TestLibrary.TestFramework.LogError("005", "In CoptTo method this string length equels to the char array length but copy count is 0 ExpectResult dst's elements do not change ,ActualResult is " + dst2 + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        string strA;
        int srcIndex;
        char[] dst;
        int dstIndex;
        int count;
        Random rand = new Random(-55);

        TestLibrary.TestFramework.BeginScenario("PosTest4:In CoptTo method this string length equels to the char array length and copy part of string to char arry");

        try
        {
            string strBasic1 = TestLibrary.Generator.GetString(-55, false, c_MAX_STRING_LENGTH, c_MAX_STRING_LENGTH);
            string strBasic2 = TestLibrary.Generator.GetString(-55, false, c_MAX_STRING_LENGTH, c_MAX_STRING_LENGTH);
            strA = strBasic1;
            srcIndex = 0;
            dst = strBasic2.ToCharArray();
            count = rand.Next(1,strA.Length);
            dstIndex = rand.Next(0, dst.Length - count);
            strA.CopyTo(srcIndex, dst, dstIndex, count);
            string dst2 = new string(dst);
            string dst3 = strBasic2.Substring(0,dstIndex) + strA.Substring(0,count) + strBasic2.Substring(dstIndex + count,dst.Length -dstIndex-count);
            if (dst2.ToString() !=dst3.ToString())
            {
                TestLibrary.TestFramework.LogError("007", "In CoptTo method this string length equels to the char array length and copy part of string to char array ExpectResult is "+dst3+",ActualResult is " + dst2 + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        string strA;
        int srcIndex;
        char[] dst;
        int dstIndex;
        int count;
        Random rand = new Random(-55);

        TestLibrary.TestFramework.BeginScenario("PosTest5:In CoptTo method copy part of string to the char array");

        try
        {
            string strBasic1 = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            string strBasic2 = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            strA = strBasic1;
            srcIndex = rand.Next(0,strA.Length+1);
            dst = strBasic2.ToCharArray();
            count = rand.Next(0,strA.Length+1 - srcIndex);
            if (dst.Length >= count)
            {
                dstIndex = rand.Next(0, dst.Length - count);
                strA.CopyTo(srcIndex, dst, dstIndex, count);
                string dst2 = new string(dst);
                string dst3 = strBasic2.Substring(0, dstIndex) + strA.Substring(srcIndex, count) + strBasic2.Substring(dstIndex + count, dst.Length - dstIndex - count);
                if (dst2.ToString() != dst3.ToString())
                {
                    TestLibrary.TestFramework.LogError("009", "In CoptTo method copy part of string to the char array ExpectResult is " + dst3 + ",ActualResult is " + dst2 + ")");
                    retVal = false;
                }
            }          
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion

    #region NegativeTesting
    public bool NegTest1()
    {
        bool retVal = true;
        string strA;
        int srcIndex;
        char[] dst;
        int dstIndex;
        int count;
        Random rand = new Random(-55);

        TestLibrary.TestFramework.BeginScenario("NegTest1:In CopyTo method destination is null");
        
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false,c_MINI_STRING_LENGTH,c_MAX_STRING_LENGTH);
            srcIndex = TestLibrary.Generator.GetInt32(-55);
            dst = null;
            dstIndex = TestLibrary.Generator.GetInt32(-55);
            count = TestLibrary.Generator.GetInt32(-55);
            strA.CopyTo(srcIndex, dst, dstIndex, count);
            retVal = false;
        }
        catch(ArgumentNullException)
        {
        }
        catch(Exception e)
        {
            TestLibrary.TestFramework.LogError("N001","Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        string strA;
        int srcIndex;
        char[] dst;
        int dstIndex;
        int count;
        Random rand = new Random(-55);

        TestLibrary.TestFramework.BeginScenario("NegTest2:In CopyTo method copy count is less than 0");
        
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            srcIndex = rand.Next(0, strA.Length + 1);
            string strB = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            dst = strB.ToCharArray();
            dstIndex = rand.Next(0, dst.Length + 1);
            count = rand.Next(1, strA.Length + 1 - srcIndex) * (-1);
            strA.CopyTo(srcIndex, dst, dstIndex, count);
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;
        string strA;
        int srcIndex;
        char[] dst;
        int dstIndex;
        int count;
        Random rand = new Random(-55);

        TestLibrary.TestFramework.BeginScenario("NegTest3:In CopyTo method sourceIndex is less than 0");
        
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            srcIndex = rand.Next(1, strA.Length + 1) * (-1);
            string strB = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            dst = strB.ToCharArray();
            dstIndex = rand.Next(0, dst.Length + 1);
            count = rand.Next(0, strA.Length + 1 - srcIndex);
            strA.CopyTo(srcIndex, dst, dstIndex, count);
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N003", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;
        string strA;
        int srcIndex;
        char[] dst;
        int dstIndex;
        int count;
        Random rand = new Random(-55);

        TestLibrary.TestFramework.BeginScenario("NegTest4:In CopyTo method copy count is greater source leng substract sourceIndex");

        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            srcIndex = rand.Next(0, strA.Length + 1);
            string strB = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            dst = strB.ToCharArray();
            dstIndex = rand.Next(0, dst.Length + 1);
            count = rand.Next(strA.Length - srcIndex + 1, strA.Length);
            strA.CopyTo(srcIndex, dst, dstIndex, count);
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;
        string strA;
        int srcIndex;
        char[] dst;
        int dstIndex;
        int count;
        Random rand = new Random(-55);
        
        TestLibrary.TestFramework.BeginScenario("NegTest5: In CopyTo method source Index is greater source length");
        
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            srcIndex = rand.Next(strA.Length + 1, strA.Length + 10);
            string strB = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            dst = strB.ToCharArray();
            dstIndex = rand.Next(0, dst.Length + 1);
            count = rand.Next(0,strA.Length +1);
            strA.CopyTo(srcIndex, dst, dstIndex, count);
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N005", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest6()
    {
        bool retVal = true;
        string strA;
        int srcIndex;
        char[] dst;
        int dstIndex;
        int count;
        Random rand = new Random(-55);

        TestLibrary.TestFramework.BeginScenario("NegTest6: In CopyTo method destination Index is less than 0");

        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            srcIndex = rand.Next(0, strA.Length + 1);
            string strB = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            dst = strB.ToCharArray();
            dstIndex = rand.Next(1, dst.Length + 1) * (-1);
            count = rand.Next(0,strA.Length + 1 - srcIndex);
            strA.CopyTo(srcIndex, dst, dstIndex, count);
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N006", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest7()
    {
        bool retVal = true;
        string strA;
        int srcIndex;
        char[] dst;
        int dstIndex;
        int count;
        Random rand = new Random(-55);

        TestLibrary.TestFramework.BeginScenario("NegTest7: In CopyTo method destination Index is greater destination length substract count");

        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            srcIndex = rand.Next(0, strA.Length + 1);
            string strB = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            dst = strB.ToCharArray();
            count = rand.Next(0, strA.Length + 1 - srcIndex);
            dstIndex = rand.Next(dst.Length - count + 1, dst.Length + 1);
            strA.CopyTo(srcIndex, dst, dstIndex, count);
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N007", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion

}

