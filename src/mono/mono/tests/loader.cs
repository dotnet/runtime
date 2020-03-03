//
// loader.cs:
//
//  Tests for assembly loading
//

using System;
using System.Reflection;
using System.Reflection.Emit;

public class Tests {

	public static int Main (string[] args)
	{
		return TestDriver.RunTests (typeof (Tests), args);
	}

	public static int test_0_load_partial_name ()
	{
		if (Assembly.LoadWithPartialName ("mscorlib") == null)
			return 1;
		else
			return 0;
	}

	public static int test_0_load_dynamic ()
	{
		// Check that dynamic assemblies are not loaded by Assembly.Load
        AssemblyName an = new AssemblyName();
        an.Name = "NOT.EXISTS";

        AssemblyBuilder ab = 
            AppDomain.CurrentDomain.DefineDynamicAssembly(an,
                                                     AssemblyBuilderAccess.RunAndSave);

        ModuleBuilder mb = ab.DefineDynamicModule("NOT.EXISTS");

		Assembly b = Assembly.LoadWithPartialName ("NOT.EXISTS");
		if (b == null)
			return 0;
		else
			return 1;
	}
}

		
