using System;

interface IA
{
	int Add(int i);
}

interface IB
{
	int Add(int i);	
}

interface IC : IA, IB {}

interface IE : ICloneable, IDisposable {
	void doom ();
}

class D : IC, IB
{
	int IA.Add (int i) {
		return 5;
	}
	
	int IB.Add (int i) {
		return 6;
	}
}

class E: IE, IC {
	public E() {
	}
	public void doom () {
		return;
	}
	public Object Clone () {
		return null;
	}
	public void Dispose () {}
	int IA.Add (int i) {
		return 7;
	}
	
	int IB.Add (int i) {
		return 8;
	}
}

class C
{
	static int Test(IC n) {

		if (((IA)n).Add(0) != 5)
			return 1;

		if (((IB)n).Add(0) != 6)
			return 1;


		return 0;
	}

	static void Test2(IE ie) {
		ie.doom ();
		object o = ie.Clone();
		ie.Dispose ();
	}

	static int Main()
	{
		D d = new D();
		E e = new E();
		int a = Test (e);
		Test2 (e);
		
		return Test (d);
	}
}

