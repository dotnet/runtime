interface IA
{
	int Add(int i);
}

interface IB
{
	int Add(int i);	
}

interface IC : IA, IB {}

class D : IC
{
	int IA.Add (int i) {
		return 5;
	}
	
	int IB.Add (int i) {
		return 6;
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

	static int Main()
	{
		D d = new D();
		
		return Test (d);
	}
}

