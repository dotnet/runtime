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

delegate int IntNoArgs();

public class Base {

	public virtual int method1 () {
		return 1;
	}

	public virtual int method2 () {
		return 1;
	}
	
	public virtual int method3 () {
		return 1;
	}

	public virtual int method4 () {
		return 1;
	}

	public virtual int method5 () {
		return 1;
	}
	
}

public class Middle : Base {
	public override int method2 () {
		return 2;
	}
	
	public override int method4 () {
		return 2;
	}
	
	public override sealed int method5 () {
		return 2;
	}
}


public class OpenFinal : Middle {
	public override sealed int method4 () {
		return 3;
	}
	
	static public int staticMethod() {
		return 3;
	}

}

sealed public class SealedFinal : Middle {
	public override int method1 () {
		return 4;
	}
	
	static public int staticMethod() {
		return 4;
	}
}


class DevirtualizationTests {

#if !__MOBILE__
	static int Main  (string[] args) {
		return TestDriver.RunTests (typeof (DevirtualizationTests), args);
	}
#endif
	
	static public int test_0_sealed_class_devirt_right_method () {
		SealedFinal x = new SealedFinal ();
		if (x.method1 () != 4)
			return 1;
		if (x.method2 () != 2)
			return 2;
		if (x.method3 () != 1)
			return 1;
		return 0;	
	}
	
	static public int test_0_sealed_method_devirt_right_method () {
		OpenFinal x = new OpenFinal ();
		if (x.method4 () != 3)
			return 1;
		if (x.method5 () != 2)
			return 2;
		return 0;	
	}
	
	static public int test_0_sealed_class_devirt_right_method_using_delegates () {
		SealedFinal x = new SealedFinal ();
		IntNoArgs d1 = new IntNoArgs(x.method1);
		IntNoArgs d2 = new IntNoArgs(x.method2);
		IntNoArgs d3 = new IntNoArgs(x.method3);
		
		if (d1 () != 4)
			return 1;
		if (d2 () != 2)
			return 2;
		if (d3 () != 1)
			return 1;
		return 0;	
	}
	
	static public int test_0_sealed_method_devirt_right_method_using_delegates () {
		OpenFinal x = new OpenFinal ();
		IntNoArgs d1 = new IntNoArgs(x.method4);
		IntNoArgs d2 = new IntNoArgs(x.method5);
		
		if (d1 () != 3)
			return 1;
		if (d2 () != 2)
			return 2;
		return 0;	
	}
	
	
	static public int test_0_delegate_over_static_method_devirtualize_ok () {
		IntNoArgs d1 = new IntNoArgs(OpenFinal.staticMethod);
		IntNoArgs d2 = new IntNoArgs(SealedFinal.staticMethod);
		
		if (d1 () != 3)
			return 1;
		if (d2 () != 4)
			return 2;
			
		return 0;
	}

	static public int test_0_npe_still_happens() {
		OpenFinal x = null;
		SealedFinal y = null;
		
		try {
			y.method1();
			return 1;
		} catch(NullReferenceException e) {
			;//ok
		}

		try {
			y.method2();
			return 2;
		} catch(NullReferenceException e) {
			;//ok
		}

		try {
			y.method3();
			return 3;
		} catch(NullReferenceException e) {
			;//ok
		}
		
		try {
			x.method4();
			return 4;
		} catch(NullReferenceException e) {
			;//ok
		}

		try {
			x.method5();
			return 5;
		} catch(NullReferenceException e) {
			;//ok
		}
		
		return 0;
	}
}
