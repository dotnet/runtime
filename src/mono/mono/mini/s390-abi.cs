using System;
using System.Reflection;

/*
 * Regression tests for the mono JIT.
 *
 * Each test needs to be of the form:
 *
 * static int test_<result>_<name> ();
 *
 * where <result> is an integer (the value that needs to be returned by
 * the method to make it pass.
 * <name> is a user-displayed name used to identify the test.
 *
 * The tests can be driven in two ways:
 * *) running the program directly: Main() uses reflection to find and invoke
 * 	the test methods (this is useful mostly to check that the tests are correct)
 * *) with the --regression switch of the jit (this is the preferred way since
 * 	all the tests will be run with optimizations on and off)
 *
 * The reflection logic could be moved to a .dll since we need at least another
 * regression test file written in IL code to have better control on how
 * the IL code looks.
 */

class Tests {

	public struct TestStruct1 
	{
		public int a;
	}
	
	public struct TestStruct2
	{	
		public int a;
		public int b;
	}

	public struct TestStruct3
	{
		public int a;
		public int b;
		public int c;
	}

	static int Main () {
		return TestDriver.RunTests (typeof (Tests));
	}

	static void reg_struct(TestStruct1 regStruct) 
	{
		regStruct.a = 1;
	}

	static int test_0_regstruct () 
	{
		TestStruct1 myStruct;
 		myStruct.a = 1;
		reg_struct(myStruct);
		if (myStruct.a == 1)
			return 0;
		else
			return 1;
	}

	static int reg_struct_ret(TestStruct2 regStruct) 
	{
		return regStruct.b;
	}

	static int test_0_reg_return () 
	{
		TestStruct2 myStruct;
		myStruct.a = 0;
		myStruct.b = 42;
		if (reg_struct_ret(myStruct) == 42)
			return 0;
		return 2;
	}

	static int spill_regs (int a, int b, int c, int d, int e, int f)
	{
		return f;
	}

	static int test_0_spill_regs ()
	{
		if (spill_regs (1, 2, 3, 4, 5, 6) == 6)
			return 0;
		else
			return 3;
	}

	static TestStruct3 spill_struct (TestStruct3 regStruct, int value)
	{
		regStruct.c = value;
		return(regStruct);
	}

	static TestStruct3 ret_big_struct (int value_a, int value_c)
	{
		TestStruct3 regStruct = new TestStruct3();
		regStruct.a = value_a;
		regStruct.c = value_c;
		return(regStruct);
	}

	static int spill_struct_void (TestStruct3 regStruct)
	{
		if (regStruct.c == 255)
			return 0;
		else
			return 7;
	}

	static int receive_spill_struct (TestStruct2 regStruct)
	{
		if (regStruct.b == 181)
			return 0;
		else
			return 8;
	}

	static int pass_spill_struct_big (int a, int b, int c, int d, int e, TestStruct3 regStruct)
	{
		int retVal;
		retVal = receive_spill_struct_big(regStruct);
		return retVal;
	}

	static int receive_spill_struct_big (TestStruct3 regStruct)
	{
		if (regStruct.c == 999)
			return 0;
		else
			return 9;
	}

	static int receive_struct_spill (int a, int b, int c, int d, int e, TestStruct2 regStruct)
	{
		if (regStruct.b == 181)
			return 0;
		else
			return 10;
	}

	static int receive_struct_spill_big (int a, int b, int c, int d, int e, TestStruct3 regStruct)
	{
		if (regStruct.c == 999)
			return 0;
		else
			return 11;
	}

	static int pass_spill_struct (int a, int b, int c, int d, int e, TestStruct2 regStruct)
	{
		int retVal;
		retVal = receive_spill_struct(regStruct);
		return retVal;
	}

	static int pass_struct_spill (TestStruct2 regStruct)
	{
		int retVal;
		retVal = receive_struct_spill(1,2,3,4,5,regStruct);
		return retVal;
	}

	static int pass_struct_spill_big(TestStruct3 regStruct)
	{
		int retVal;
		retVal = receive_struct_spill_big(1,2,3,4,5,regStruct);
		return retVal;
	}

	static int pass_spill_struct_spill (int a, int b, int c, int d, int e, TestStruct2 regStruct)
	{
		int retVal;
		retVal = receive_struct_spill(a,b,c,d,e,regStruct);
		return retVal;
	}

	static int pass_spill_struct_spill_big(int a, int b, int c, int d, int e, TestStruct3 regStruct)
	{
		int retVal;
		retVal = receive_struct_spill_big(a,b,c,d,e,regStruct);
		return retVal;
	}

	static int test_0_spill () 
	{
		TestStruct3 myStruct;
		myStruct.a = 64;	
		myStruct.b = 255;
		myStruct.c = 127;
		myStruct = spill_struct(myStruct, 99);
		if (myStruct.c == 99)
			return 0;
		return myStruct.c;
	}

	static int test_0_spill_void ()
	{
		TestStruct3 myStruct;
		myStruct.a = 0;
		myStruct.b = 127;
		myStruct.c = 255;
		return (spill_struct_void(myStruct));
	}

	static int spill_struct_ret (TestStruct3 regStruct)
	{
		return (regStruct.c);
		
	}

	static int test_0_spill_ret ()
	{
		TestStruct3 myStruct;
		myStruct.a = 0;	
		myStruct.b = 0;
		myStruct.c = 69;
		if (spill_struct_ret(myStruct) == 69)
			return 0;
		return 5;
	}

	static TestStruct2 struct_ret(TestStruct2 regStruct)
	{
		regStruct.a = -1;
		regStruct.b = 72;
		return(regStruct);
	}

	static int test_0_struct_ret ()
	{
		TestStruct2 myStruct;
		myStruct.a = 99;
		myStruct.b = 14;
		myStruct = struct_ret(myStruct);
		if (myStruct.b == 72)
			return 0;
		else
			return myStruct.b;
	}

	static float TestSingle (float a, float b, float c)
	{
		return b;
	}

	static int test_0_TestSingle ()
	{
		float a = 3F; float b = 4.5F; float c = 900F;
		if (TestSingle(a, b, c) == b)
			return 0;
		else
			return 6;
	}

	static int test_0_pass_spill ()
	{
		TestStruct2 myStruct;
		myStruct.a = 32;
		myStruct.b = 181;
		return (pass_spill_struct (1, 2, 3, 4, 5, myStruct));
	}
		
	static int test_0_pass_spill_big ()
	{
		TestStruct3 myStruct;
		myStruct.a = 32;
		myStruct.b = 181;
		myStruct.c = 999;
		return (pass_spill_struct_big (1, 2, 3, 4, 5, myStruct));
	}
		
	static int test_0_pass_struct_spill ()
	{
		TestStruct2 myStruct;
		myStruct.a = 32;
		myStruct.b = 181;
		return (pass_struct_spill (myStruct));
	}
		
	static int test_0_pass_struct_spill_big ()
	{
		TestStruct3 myStruct;
		myStruct.a = 32;
		myStruct.b = 181;
		myStruct.c = 999;
		return (pass_struct_spill_big (myStruct));
	}
		
	static int test_0_pass_ret_big_struct ()
	{
		TestStruct3 myStruct;
		myStruct = ret_big_struct(10,132);
		if (myStruct.c == 132)
			return 0;
		else
			return 1;
	}
		
	static int test_0_pass_spill_struct_spill ()
	{
		TestStruct2 myStruct;
		myStruct.a = 32;
		myStruct.b = 181;
		return (pass_spill_struct_spill (1,2,3,4,5,myStruct));
	}
		
	static int test_0_pass_spill_struct_spill_big ()
	{
		TestStruct3 myStruct;
		myStruct.a = 32;
		myStruct.b = 181;
		myStruct.c = 999;
		return (pass_spill_struct_spill_big (1,2,3,4,5,myStruct));
	}

	static long pass_long_odd (int a, long b)
	{
		return (b);
	}
		
	static int test_0_pass_long_odd ()
	{
		int a = 5;
		long b = 9000;
		if (pass_long_odd(a,b) == 9000)
			return 0;
		else
			return 9;
	}

	static float pass_double_ret_float(double a)
	{
		float b;
		b = (float) a;
		return b;
	}

	static int test_0_pass_double_ret_float ()
	{
		double a = 654.34;
		float b = 654.34f;
		if (pass_double_ret_float(a) == b)
			return 0;
		else
			return 10;
	}

	static double pass_float_ret_double(float a)
	{
		double b;
		b = (double) a;
		return b;
	}

	static int test_0_pass_float_ret_double ()
	{
		float a = 654.34f;
		double b = 654.34;
		if (pass_float_ret_double(a) == b)
			return 0;
		else
			return 11;
	}

}
