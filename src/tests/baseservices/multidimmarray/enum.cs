using System;
using Xunit;

public class Test
{
    enum State : sbyte { OK = 0, BUG = -1 }
    [Fact]
    public static int TestEntryPoint()
    {
        TestLibrary.TestFramework.BeginTestCase("Enum MultidimmArray");
        var s = new State[1, 1];
        s[0, 0] = State.BUG;
        State a = s[0, 0];
        if(a == s[0, 0])
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

}
