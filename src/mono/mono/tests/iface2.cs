interface IA
{
	int Add(int i);

	int Add2(int i);
}

interface IB
{
	int Add(int i);	
}

interface IC
{
	int Add(int i);	
}

interface ID : IA, IB {}

class D : ID
{
	int IA.Add (int i) {
		return 5;
	}
	
	int IA.Add2 (int i) {
		return 6;
	}
	
	int IB.Add (int i) {
		return 7;
	}
}


class E : IC, ID
{
	int IC.Add (int i) {
		return 8;
	}

	int IA.Add (int i) {
		return 9;
	}
	
	int IA.Add2 (int i) {
		return 10;
	}

	int IB.Add (int i) {
		return 11;
	}
}


class C
{
	static int Test(ID n) {

		if (((IA)n).Add2(0) != 6)
			return 1;

		if (((IB)n).Add(0) != 7)
			return 1;


		return 0;
	}

	static int Main()
	{
		D d = new D();
		E e = new E();

		if (Test (d) != 0)
			return 1;
		
		return 0;
	}
}

