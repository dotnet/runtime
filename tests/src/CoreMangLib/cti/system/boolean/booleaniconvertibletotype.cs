// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

class MyTestClass
{
    public MyTestClass()
    {
        this.m_value = false;
    }

    public MyTestClass(bool value)
    {
        this.m_value = value;
    }

    bool m_value;
};

public class BooleanIConvertibleToType
{

    public static int Main()
    {
        BooleanIConvertibleToType testCase = new BooleanIConvertibleToType();

        TestLibrary.TestFramework.BeginTestCase("Boolean.IConvertible.ToType");
        if (testCase.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;
        retVal = PosTest10() && retVal;
        retVal = PosTest11() && retVal;
        retVal = PosTest12() && retVal;
        retVal = PosTest13() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;


        return retVal;
    }

    #region Positive test cases
    /// <summary>
    /// convert to int
    /// </summary>
    /// <returns></returns>
    public bool PosTest1()
    {
        bool retVal = true;
        try
        {
            if ( (int)(true as IConvertible).ToType(typeof(int), null) != 1 )
            {
                TestLibrary.TestFramework.LogError("001", "expect (int)(true as IConvertible).ToType(typeof(int), null) == 1");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    /// <summary>
    /// convert to uint
    /// </summary>
    /// <returns></returns>
    public bool PosTest2()
    {
        bool retVal = true;
        try
        {
            if ((uint)(true as IConvertible).ToType(typeof(uint), null) != 1)
            {
                TestLibrary.TestFramework.LogError("002", "expect (uint)(true as IConvertible).ToType(typeof(uint), null) == 1");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    
    /// <summary>
    /// convert to long
    /// </summary>
    /// <returns></returns>
    public bool PosTest3()
    {
        bool retVal = true;
        try
        {
            if ((long)(false as IConvertible).ToType(typeof(long), null) != (long)0)
            {
                TestLibrary.TestFramework.LogError("003", "expect (long)(false as IConvertible).ToType(typeof(long), null) == (long)0");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    /// <summary>
    /// convert to ulong
    /// </summary>
    /// <returns></returns>
    public bool PosTest4()
    {
        bool retVal = true;
        try
        {
            if ((ulong)(false as IConvertible).ToType(typeof(ulong), null) != (ulong)0)
            {
                TestLibrary.TestFramework.LogError("004", "expect (ulong)(false as IConvertible).ToType(typeof(ulong), null) == (ulong)0");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    /// <summary>
    /// convert to short
    /// </summary>
    /// <returns></returns>
    public bool PosTest5()
    {
        bool retVal = true;
        try
        {
            if ((short)(true as IConvertible).ToType(typeof(short), null) != (short)1)
            {
                TestLibrary.TestFramework.LogError("005", "expect (short)(true as IConvertible).ToType(typeof(short), null) == (short)1");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    /// <summary>
    /// convert to ushort
    /// </summary>
    /// <returns></returns>
    public bool PosTest6()
    {
        bool retVal = true;
        try
        {
            if ((ushort)(false as IConvertible).ToType(typeof(ushort), null) != (ushort)0)
            {
                TestLibrary.TestFramework.LogError("006", "expect (ushort)(false as IConvertible).ToType(typeof(ushort), null) == (ushort)0");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    /// <summary>
    /// convert to byte
    /// </summary>
    /// <returns></returns>
    public bool PosTest7()
    {
        bool retVal = true;
        try
        {
            if ((byte)(true as IConvertible).ToType(typeof(byte), null) != (byte)1)
            {
                TestLibrary.TestFramework.LogError("007", "expect (byte)(true as IConvertible).ToType(typeof(byte), null) == (byte)1");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    /// <summary>
    /// convert to sbyte
    /// </summary>
    /// <returns></returns>
    public bool PosTest8()
    {
        bool retVal = true;
        try
        {
            if ((sbyte)(true as IConvertible).ToType(typeof(sbyte), null) != (sbyte)1)
            {
                TestLibrary.TestFramework.LogError("008", "expect (sbyte)(true as IConvertible).ToType(typeof(sbyte), null) == (sbyte)1");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    /// <summary>
    /// convert to bool
    /// </summary>
    /// <returns></returns>
    public bool PosTest9()
    {
        bool retVal = true;
        try
        {
            if ((bool)(true as IConvertible).ToType(typeof(Boolean), null) != true)
            {
                TestLibrary.TestFramework.LogError("009", "expect (bool)(true as IConvertible).ToType(typeof(Boolean), null) == true");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    
    /// <summary>
    /// convert to Object
    /// </summary>
    /// <returns></returns>
    public bool PosTest10()
    {
        bool retVal = true;
        try
        {
            object v = (true as IConvertible).ToType(typeof(Object), null);
            if ( v.GetType() != typeof(Boolean) )
            {
                TestLibrary.TestFramework.LogError("010.1", "expect (true as IConvertible).ToType(typeof(Object), null).GetType() == typeof(Boolean)");
                retVal = false;
            }

            if ((bool)v != true)
            {
                TestLibrary.TestFramework.LogError("0010.2", "expect (bool)(true as IConvertible).ToType(typeof(Object), null) == true");
                retVal = false;
            }
            
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// convert to float
    /// </summary>
    /// <returns></returns>
    public bool PosTest11()
    {
        bool retVal = true;
        try
        {
            if ((float)(true as IConvertible).ToType(typeof(float), null) != 1.0f)
            {
                TestLibrary.TestFramework.LogError("011.1", "expect (float)(true as IConvertible).ToType(typeof(float), null) == 1.0f");
                retVal = false;
            }
            if ((float)(false as IConvertible).ToType(typeof(float), null) != 0.0f)
            {
                TestLibrary.TestFramework.LogError("011.1", "expect (float)(false as IConvertible).ToType(typeof(float), null) == 0.0f");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("011", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    
    /// <summary>
    /// convert to double
    /// </summary>
    /// <returns></returns>
    public bool PosTest12()
    {
        bool retVal = true;
        try
        {
            if ((double)(true as IConvertible).ToType(typeof(double), null) != 1.0)
            {
                TestLibrary.TestFramework.LogError("012.1", "expect (double)(true as IConvertible).ToType(typeof(double), null) == 1.0");
                retVal = false;
            }
            if ((double)(false as IConvertible).ToType(typeof(double), null) != 0.0)
            {
                TestLibrary.TestFramework.LogError("012.1", "expect (double)(false as IConvertible).ToType(typeof(double), null) == 0.0");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// convert to decimal
    /// </summary>
    /// <returns></returns>
    public bool PosTest13()
    {
        bool retVal = true;
        try
        {
            if ((decimal)(true as IConvertible).ToType(typeof(decimal), null) != (decimal)1.0)
            {
                TestLibrary.TestFramework.LogError("013.1", "expect (decimal)(true as IConvertible).ToType(typeof(decimal), null) == (decimal)1.0");
                retVal = false;
            }
            if ((decimal)(false as IConvertible).ToType(typeof(decimal), null) != (decimal)0.0)
            {
                TestLibrary.TestFramework.LogError("013.1", "expect (decimal)(false as IConvertible).ToType(typeof(decimal), null) == (decimal)0.0");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("013", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion


    #region Negative test cases

    /// <summary>
    /// convert to char and expcet InvalidCastException
    /// </summary>
    /// <returns></returns>
    public bool NegTest1()
    {
        bool retVal = true;
        try
        {
            char v = (char)(true as IConvertible).ToType(typeof(Char), null);
            TestLibrary.TestFramework.LogError("001",
                String.Format("expected a InvalidCastException on (true as IConvertible).ToType(typeof(Char), null) but got {0}", v));
            retVal = false;
        }
        catch (InvalidCastException)
        {
            retVal = true;
            return retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    /// <summary>
    /// convert to DateTime and expcet InvalidCastException
    /// </summary>
    /// <returns></returns>
    public bool NegTest2()
    {
        bool retVal = true;
        try
        {
            DateTime v = (DateTime)(true as IConvertible).ToType(typeof(DateTime), null);
            TestLibrary.TestFramework.LogError("002",
                String.Format("expected a InvalidCastException on (true as IConvertible).ToType(typeof(DateTime), null) but got {0}", v));
            retVal = false;
        }
        catch (InvalidCastException)
        {
            retVal = true;
            return retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    /// <summary>
    /// convert to MyTestClass and expcet InvalidCastException
    /// </summary>
    /// <returns></returns>
    public bool NegTest3()
    {
        bool retVal = true;
        try
        {
            MyTestClass v = (MyTestClass)(true as IConvertible).ToType(typeof(MyTestClass), null);
            TestLibrary.TestFramework.LogError("003",
                String.Format("expected a InvalidCastException on (true as IConvertible).ToType(typeof(MyTestClass), null) but got {0}", v));
            retVal = false;
        }
        catch (InvalidCastException)
        {
            retVal = true;
            return retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
