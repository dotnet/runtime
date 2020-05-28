using System;

public class A {
	
	static readonly A a = new A ();
	
	static int Main ()
	{	
		for (int i = 0; i < 500000000; i++)
			a.Dummy ();
		
		return 0;
	}
	
	public virtual void Dummy () {
	}
}