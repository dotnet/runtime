// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;
using System.Runtime.CompilerServices;
using TestLibrary;

#if WINCORESYS
[assembly: AllowPartiallyTrustedCallers]
#endif
// Class has two fields, x and y; we consider two instances to be equal (Equals returns true,
//  and GetHashCode returns the same for both instances) iff x==x and y==y
class ClassWithEquivalence
{
    int x;
    int y;

    public ClassWithEquivalence(int _x, int _y)
    {
        this.x = _x;
        this.y = _y;
    }

    public override bool Equals(object b)
    {
        ClassWithEquivalence bProper = b as ClassWithEquivalence;
        if (bProper != null)
            return (x == bProper.x) && (y == bProper.y);
        else
            return false;
    }

    public override int GetHashCode()
    {
        return x.GetHashCode() + y.GetHashCode();
    }
}
public class GetHashCode
{
    public bool PosTest1()
    {
        bool retVal = true;
        TestFramework.BeginScenario("PosTest1: RuntimeHelpers.GetHashCode() on two non-reference-equals objects returns non-equal codes");
        Object a, b;
        try
        {
            a = new ClassWithEquivalence(10, 20);
            b = new ClassWithEquivalence(10, 20);

            if (!(a.Equals(b) && (a.GetHashCode() == b.GetHashCode()))
                || (Object.ReferenceEquals(a, b)))
            {
                // Log: setup failed
                return false;
            }

            if (RuntimeHelpers.GetHashCode(a) == RuntimeHelpers.GetHashCode(b))
            {
                // Log: RTH.GHC should have returned different hash codes since the
                //  objects are not reference-equals.
                TestFramework.LogError("001", "a and b are not reference equal, and yet RuntimeHelpers.GetHashCode returned same value for each");
                retVal = false;

            }
        }
        catch (Exception e)
        {
            TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool RunTests()
    {
        bool retVal = true;
        retVal = PosTest1() && retVal;

        return retVal;
    }
    
    public static int Main(string[] args)
    {
        GetHashCode ghc = new GetHashCode();
        if (ghc.RunTests())
        {
            TestFramework.LogInformation("PASSED");
            return 100;
        }
        else
        {
            TestFramework.LogInformation("FAILED");
            return 99;
        }
    }

}