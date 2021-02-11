using System;

public interface I1
{
    int M1()
    {
        return 100;
    }
}

public interface I2 : I1 { int I1.M1() { return 200; } }

public interface I3 : I1 { int I1.M1() { return 300; } }

public interface I4 : I1 { int I1.M1() { return 400; } }

class Test10 : I1, I2, I3, I4 {
    //void I1.M1() { System.Console.WriteLine("I1.I1.M1"); }
    public int M1() { return 0; }
}

public interface I1t
{
    int M1t()
    {
        return 100;
    }
}

public interface I2t : I1t { int I1t.M1t() { return 200; } }

public interface I3t : I1t { int I1t.M1t() { return 300; } }

public interface I4t : I1t { int I1t.M1t() { return 400; } }

class Test10t : I1t, I2t, I3t, I4t
{
    int I1t.M1t() { return 0; }
    public int M1t() { return 10; }
}


public interface IName
{
	string Name { get; }
}
public interface IOther<T>
{
	T other { get; }
	string Name { get; }
}
public class Name1 : IName
{
	public string Name { get { return "ClassName"; } }
	string IName.Name { get { return "InterfaceName"; } }
}
public class Name2 : IName, IOther<int>
{
	public string Name { get { return "ClassName"; } }
	string IName.Name { get { return "InterfaceName"; } }
	public int other { get { return 43; } }
}

public class Test
{
	public static int test_0_override()
	{
		var name1 = new Name1();
		var name2 = new Name2();
		IName iName1 = name1;
		IName iName2 = name2;
		if (!iName1.Name.Equals("InterfaceName"))
			return 10;
		if (!iName2.Name.Equals("InterfaceName"))
			return 20;
 		if (!name1.Name.Equals("ClassName"))
 			return 30;
		if (!name2.Name.Equals("ClassName"))
			return 40;
        return 0;
	}

	public static int test_0_dim_override()
	{
		I1 var = new Test10();
		if (var.M1() != 0)
			return var.M1();

		I1t var2 = new Test10t();
		return var2.M1t();
    }

    public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Test), args);
	}

}
