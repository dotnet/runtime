/*


Packed interface table for class Repro.Derived

*/
using System;

namespace Repro {
	interface Interface<T0>
	{
	    void Problem();
	}
	class Base<T1> : Interface<T1>
	{
	    void Interface<T1>.Problem() {
			Console.WriteLine("Base.Method()");
			throw new Exception ();
		}
	}
	class Derived<U> : Base<int>, Interface<U>
	{
	    void Interface<U>.Problem() { Console.WriteLine("Derived`2.Method()"); }
		~Derived() {
			
		}
	}
	class FinalClass : Derived<int>, Interface<string>, Interface<int>
	{
	    void Interface<string>.Problem() {
			Console.WriteLine("Derived.Method()");
			throw new Exception ();
		}
	}
	class Program
	{
	    public static void Main()
	    {
	        Interface<int> j = new FinalClass();
	        j.Problem();
	    }
	}
}

