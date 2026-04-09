namespace Test
{
        using System;
        using System.Reflection;
        
        class Foo
        {
                public static int Sum (params int[] args)
                {
                        int ret = 0;
                        foreach (int a in args)
                                ret += a;
                        return ret;
                }
        }
        
        class TestInvokeArray
        {
                public static int Main (string[] args)
                {
                        Foo f = new Foo ();
                        MethodInfo m = (MethodInfo) (f.GetType ().FindMembers (MemberTypes.All, BindingFlags.Public | BindingFlags.Static, Type.FilterName, "Sum"))[0];
                        int[] numbers = new int[3]{4, 5, 6};
                        object[] parms = new object[1]{numbers};
			int sum = (int)m.Invoke (f, parms);
                        Console.WriteLine ("sum is " + sum);

			if (sum != 15)
				return 1;

			return 0;
                }
        }
}
