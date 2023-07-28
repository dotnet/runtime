// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

/// <summary>
/// InvalidCastException is thrown when 'ngen /profile' image doesn't restore System.Enum TypeRef
/// This is a bug in JIT-EE interface. The fix was to add call to 
/// m_pOverride->classMustBeLoadedBeforeCodelsRun in CEEInfo::getUnBoxHelper
/// </summary>
public class Test_Dev11_457559
{
    [Fact]
    public static int TestEntryPoint()
    {
        if (RunTest(1))
        {
            Console.WriteLine("Pass");
            return 100;
        }
        Console.WriteLine("Fail");
        return 101;
    }
    public static bool RunTest(object o)
    {
        try
        {
            bool b = ((MyEnum)o) == MyEnum.Value0;
            Console.WriteLine("{0}", b);
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected exception thrown: " + e);
            return false;
        }
        return true;
    }
}
public enum MyEnum { Value0 = 0 }
