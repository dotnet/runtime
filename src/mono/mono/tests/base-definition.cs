using System;
using System.Reflection;

abstract class test
{
	public static int Main ()
	{
		MethodInfo m = typeof (SubTestClass).GetMethod ("get_name");
		MethodInfo bm = m.GetBaseDefinition ();
		if (bm == null || bm.DeclaringType != typeof (TestClass) || bm.Name != "get_name") return 1;

		m = typeof (SubTestClass).GetMethod ("get_name2");
		bm = m.GetBaseDefinition ();
		if (bm == null || bm.DeclaringType != typeof (TestClass) || bm.Name != "get_name2") return 2;

		m = typeof (SubTestClass).GetMethod ("get_name3");
		bm = m.GetBaseDefinition ();
		if (bm == null || bm.DeclaringType != typeof (BaseTestClass) || bm.Name != "get_name3") return 3;
		
		return 0;
	}
}

abstract class BaseTestClass
{
	public abstract string name3
	{
		get;
	}

}

abstract class TestClass : BaseTestClass
{
	public abstract string name
	{
		get;
	}

	public virtual string name2
	{
		get { return null; }
	}
}

class SubTestClass : TestClass
{
	public override string name
	{
		get { return ""; }
	}

	public override string name2
	{
		get { return ""; }
	}

	public override string name3
	{
		get { return ""; }
	}
}


