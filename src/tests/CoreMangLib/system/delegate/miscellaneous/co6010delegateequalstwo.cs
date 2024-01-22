//Auto-edited to add globalization coverage by Globalizer, 6/7/2004 1:48:02 PM, written by RDawson
//Auto-edited to add globalization coverage by Globalizer, 6/3/2004 11:54:47 AM, written by RDawson
//Updated: 06/18/03, by Alicial, add more test cases for code coverage.

using System;
using System.Diagnostics;
using Xunit;

delegate void Void_VoidDelegate();
public class TestClass
{
	//coreclr doesn't have Delegate.Combine(Delegate[])
	static Delegate DelegateCombine(params Delegate[] delegates) {
		if ((delegates == null) || (delegates.Length == 0)) {
			return null;
		}
		Delegate a = delegates[0];
		for (int i = 1; i < delegates.Length; i++) {
			a = Delegate.Combine(a, delegates[i]);
		}
		return a;
	}

	[Fact]
	public static int TestEntryPoint() {
		int iErrorCount = 0;
		int iTestCount = 0;
		{
			iTestCount++;
			Console.WriteLine( "test1: delegates point to different static method with same signature" );
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( staticMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( staticMethVoid_Void2 );
			if( sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_101: delegates point to different static method with same signature should not equals" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test2: delegates point to static method with same name but in different classes " );
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( staticMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( Foo_Globİı.staticMethVoid_Void1 );
			if( sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_202: delegates point to static method with same name but in different classes should not equals" );
				iErrorCount++;
			}
		}
	

		{
			iTestCount++;
			Console.WriteLine( "test3: delegates point to static method with same name but in different nested classes " );
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( Foo_Globİı.NestFoo_Globİı.staticMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( Foo_Globİı.staticMethVoid_Void1 );
			if( sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_301: delegates point to static method with same name but in different nested classes should not equals" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test3.1: delegates point to same static method in same nested classes " );
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( Foo_Globİı.NestFoo_Globİı.staticMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( Foo_Globİı.NestFoo_Globİı.staticMethVoid_Void1 );
			if( !sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_302: delegates point to same static method in same nested classes should equals" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test4: delegates point to static method with same name but one is newslot function in derived class " );
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( Foo_Globİı.staticMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( DFoo1.staticMethVoid_Void1 );
			if( sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_401: delegates point to static method with same name but one is newslot function in derived class should not equals" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test5: delegates point to same static method, DFoo2 drives from Foo" );
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( Foo_Globİı.staticMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( DFoo2.staticMethVoid_Void1 );
			if( !sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_501: delegates point to same static method, DFoo2 drives from Foo_Globİı. should equals" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test6: delegates point to static method with same name in Foo_Globİı.NestFoo_Globİı and DFoo1.NestFoo_Globİı " );
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( Foo_Globİı.NestFoo_Globİı.staticMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( DFoo1.NestFoo_Globİı.staticMethVoid_Void1 );
			if( sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_601: delegates point to static method with same name in Foo_Globİı.NestFoo_Globİı and DFoo1.NestFoo_Globİı should not equals" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test6.1: delegates point to same static method in Foo_Globİı.NestFoo_Globİı and DFoo2.NestFoo_Globİı " );
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( Foo_Globİı.NestFoo_Globİı.staticMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( DFoo2.NestFoo_Globİı.staticMethVoid_Void1 );
			if( !sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_602: delegates point to same static method in Foo_Globİı.NestFoo_Globİı and DFoo2.NestFoo_Globİı should equals" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test7: delegates point to different instance method with same signature" );
			TestClass obj1 = new TestClass();
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( obj1.instanceMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( obj1.instanceMethVoid_Void2 );
			if( sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_701: delegates point to different instance method with same signature should not equals" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test8: delegates point to same instance method on different objects" );
			TestClass obj1 = new TestClass();
			TestClass obj2 = new TestClass();
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( obj1.instanceMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( obj1.instanceMethVoid_Void2 );
			if( sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_101: delegates point to same instance method on different objects should not equals" );
				iErrorCount++;
			}
		}
		{
			iTestCount++;
			Console.WriteLine( "test9: delegates point to instance method with same name but in different classes " );
			TestClass obj1 = new TestClass();
			Foo_Globİı obj2 = new Foo_Globİı();
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( obj1.instanceMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( obj2.instanceMethVoid_Void1 );
			if( sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_902: delegates point to instance method with same name but in different classes should not equals" );
				iErrorCount++;
			}
		}
	

		{
			iTestCount++;
			Console.WriteLine( "test10: delegates point to instance method with same name but in different nested classes " );
			Foo_Globİı obj1 = new Foo_Globİı();
			Foo_Globİı.NestFoo_Globİı obj2 = new Foo_Globİı.NestFoo_Globİı();
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( obj1.instanceMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( obj2.instanceMethVoid_Void1 );
			if( sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_1001: delegates point to instance method with same name but in different nested classes should not equals" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test11: delegates point to instance method with same name but in different nested classes " );
			Foo_Globİı.NestFoo_Globİı obj1 = new Foo_Globİı.NestFoo_Globİı();

			Void_VoidDelegate sdg1 = new Void_VoidDelegate( obj1.instanceMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( obj1.instanceMethVoid_Void1 );
			if( !sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_1101: delegates point to instance method with same name but in same nested classes should equals" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test12: delegates point to instance method with same name but one is newslot function in derived class " );
			Foo_Globİı obj1 = new Foo_Globİı();
			DFoo1 obj2 = new DFoo1();
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( obj1.instanceMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( obj2.instanceMethVoid_Void1 );
			if( sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_1201: delegates point to instance method with same name but one is newslot function in derived class should not equals" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test13: delegates point to same instance method of DFoo2, DFoo2 drives from Foo" );
			DFoo2 obj2 = new DFoo2();
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( obj2.instanceMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( obj2.instanceMethVoid_Void1 );
			if( !sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_1301: delegates point to same instance method, DFoo2 drives from Foo_Globİı. should equals" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test14: delegates point to same instance method on instance of DFoo2 and Foo_Globİı. DFoo2 drives from Foo" );
			Foo_Globİı obj1 = new Foo_Globİı();
			DFoo2 obj2 = new DFoo2();
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( obj1.instanceMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( obj2.instanceMethVoid_Void1 );
			if( sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_1401: delegates point to same instance method on instance of DFoo2 and Foo_Globİı. DFoo2 drives from Foo_Globİı. should not equals" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test15: delegates point to instance method with same name in Foo_Globİı.NestFoo_Globİı and DFoo1.NestFoo_Globİı " );
			Foo_Globİı.NestFoo_Globİı obj1 = new Foo_Globİı.NestFoo_Globİı();
			DFoo1.NestFoo_Globİı obj2 = new DFoo1.NestFoo_Globİı();
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( obj1.instanceMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( obj2.instanceMethVoid_Void1 );
			if( sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_1501: delegates point to instance method with same name in Foo_Globİı.NestFoo_Globİı and DFoo1.NestFoo_Globİı should not equals" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test16: delegates point to instance method should not equals to the one point to static method " );
			Foo_Globİı obj1 = new Foo_Globİı();
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( obj1.instanceMethVoid_Void1 );
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( Foo_Globİı.staticMethVoid_Void1 );
			if( sdg1.Equals( sdg2 ) )
			{
				Console.WriteLine( "Err_1601: delegates point to instance method should not equals to the one point to static method" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test17: Test InvocationListEquals() " );
			Foo_Globİı obj1 = new Foo_Globİı();
			DFoo1.NestFoo_Globİı obj2 = new DFoo1.NestFoo_Globİı();
			Void_VoidDelegate [] dgs = new Void_VoidDelegate[3];
			dgs[0] = new Void_VoidDelegate( obj1.instanceMethVoid_Void1 );
			dgs[1] = new Void_VoidDelegate( Foo_Globİı.staticMethVoid_Void1 );
			dgs[2] = new Void_VoidDelegate( obj2.instanceMethVoid_Void1 );
			Void_VoidDelegate dcombined1 = (Void_VoidDelegate)DelegateCombine( dgs );
			Void_VoidDelegate dcombined2 = (Void_VoidDelegate)DelegateCombine( dgs );
			if( !dcombined2.Equals( dcombined1 ) )
			{
				Console.WriteLine( "Err_1701: should be the same" );
				iErrorCount++;
			}

			iTestCount++;
			Void_VoidDelegate [] dgs2 = new Void_VoidDelegate[2];
			//delegate number is different in the invocation list.
			dgs2[0] = new Void_VoidDelegate( obj1.instanceMethVoid_Void1 );
			dgs2[1] = new Void_VoidDelegate( Foo_Globİı.staticMethVoid_Void1 );
			Void_VoidDelegate dcombined3 = (Void_VoidDelegate)DelegateCombine( dgs2 );
			if( dcombined3.Equals( dcombined1 ) )
			{
				Console.WriteLine( "Err_1801: should not be the same" );
				iErrorCount++;
			}

			iTestCount++;
			Void_VoidDelegate [] dgs3 = new Void_VoidDelegate[3];
			//checked the order of the delegate in the invocation list.
			dgs3[0] = new Void_VoidDelegate( Foo_Globİı.staticMethVoid_Void1 );
			dgs3[1] = new Void_VoidDelegate( obj1.instanceMethVoid_Void1 );
			dgs3[2] = new Void_VoidDelegate( obj2.instanceMethVoid_Void1 );
			Void_VoidDelegate dcombined4 = (Void_VoidDelegate)DelegateCombine( dgs3 );
			if( dcombined4.Equals( dcombined1 ) )
			{
				Console.WriteLine( "Err_1901: should not be the same" );
				iErrorCount++;
			}

			iTestCount++;
			Void_VoidDelegate [] dgs4 = new Void_VoidDelegate[4];
			//checked the order of the delegate in the invocation list.
			dgs4[0] = new Void_VoidDelegate( obj1.instanceMethVoid_Void1 );
			dgs4[1] = new Void_VoidDelegate( Foo_Globİı.staticMethVoid_Void1 );
			dgs4[2] = new Void_VoidDelegate( obj2.instanceMethVoid_Void1 );
			dgs4[3] = new Void_VoidDelegate( obj1.instanceMethVoid_Void1 );
			Void_VoidDelegate dcombined5 = (Void_VoidDelegate)DelegateCombine( dgs4 );
			if( dcombined5.Equals( dcombined1 ) )
			{
				Console.WriteLine( "Err_1921: should not be the same" );
				iErrorCount++;
			}
			iTestCount++;
			if( dcombined1.Equals( dcombined5 ) )
			{
				Console.WriteLine( "Err_1931: should not be the same" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test20: != operator, first delegate is null" );
			Foo_Globİı obj1 = new Foo_Globİı();
			Void_VoidDelegate sdg1 = null;
			Void_VoidDelegate sdg2 = new Void_VoidDelegate( Foo_Globİı.staticMethVoid_Void1 );
			//this delegate is null
			if( !(sdg1 != sdg2) )
			{
				Console.WriteLine( "Err_2001: should not be equal" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test21: != operator, both delegate is null " );
			Foo_Globİı obj1 = new Foo_Globİı();
			Void_VoidDelegate sdg1 = null;
			Void_VoidDelegate sdg2 = null;
			if( sdg1 != sdg2 )
			{
				Console.WriteLine( "Err_2101: should be equal" );
				iErrorCount++;
			}
		}

		{
			iTestCount++;
			Console.WriteLine( "test22: != operator, second delegate is null " );
			Foo_Globİı obj1 = new Foo_Globİı();
			Void_VoidDelegate sdg1 = new Void_VoidDelegate( Foo_Globİı.staticMethVoid_Void1 );
			Void_VoidDelegate sdg2 = null;
			if( !(sdg1 != sdg2) )
			{
				Console.WriteLine( "Err_2201: should be not equal" );
				iErrorCount++;
			}
		}
		if( iErrorCount >0 )
		{
			Console.WriteLine( "Total tests: " + iTestCount + "   failed tests: " + iErrorCount );
			return 101;
		}
		else{
			Console.WriteLine( "Total tests: " + iTestCount + " all passed" );
			return 100;
		}

    }

	public static void staticMethVoid_Void1()
	{
	}

	public static void staticMethVoid_Void2()
	{
	}

	public void instanceMethVoid_Void1()
	{
	}

	public void instanceMethVoid_Void2()
	{
	}

}

public class Foo_Globİı
{
	public class NestFoo_Globİı
	{
		public static void staticMethVoid_Void1()
		{
		}

		public static void staticMethVoid_Void2()
		{
		}

		public virtual void instanceMethVoid_Void1()
		{
		}
		public virtual void instanceMethVoid_Void2()
		{
		}

	}
	public static void staticMethVoid_Void1()
	{
	}

	public static void staticMethVoid_Void2()
	{
	}

	public virtual void instanceMethVoid_Void1()
	{
	}

	public virtual void instanceMethVoid_Void2()
	{
	}
}

//hide the base class's functions.
public class DFoo1 : Foo_Globİı
{
	new public class NestFoo_Globİı
	{
		public static void staticMethVoid_Void1()
		{
		}

		public static void staticMethVoid_Void2()
		{
		}

		public virtual void instanceMethVoid_Void1()
		{
		}
		public virtual void instanceMethVoid_Void2()
		{
		}

	}
	public new static void staticMethVoid_Void1()
	{
	}

	public new static void staticMethVoid_Void2()
	{
	}

	public new virtual void instanceMethVoid_Void1()
	{
	}

	public new virtual void instanceMethVoid_Void2()
	{
	}
}



public class DFoo2 : Foo_Globİı
{

}