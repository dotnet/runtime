using System;

public class Foo<T>
{
    public void DoSomething()
    {
        try
        {
            throw new Exception("Error");
        }
        catch
        {
            throw;
        }
    }
}

public class Bar: Foo<string>
{
}


public class MainClass
{
    public static int Main()
    {
	    try {
		    new Bar().DoSomething();
	    } catch {
		    return 0;
	    }
	    return 1;
    }
}
