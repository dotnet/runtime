using System;

public interface IInterface
{
    int getRet() { return -10; }
}

public interface IInterface2 : IInterface
{
    int IInterface.getRet() { return -1; }
}


public class AbstractClass : IInterface2
{
    int IInterface.getRet() { return 0; }
}


public class FinalClass : AbstractClass
{
}

public class Test
{
    public static int test_0_dim_18917()
    {
        var var0 = new FinalClass();
        var var4 = (IInterface2)var0;
        var var5 = var4.getRet();
        return var5;

    }

    public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Test), args);
	}
}