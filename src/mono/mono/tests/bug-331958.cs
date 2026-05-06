class Program
{
	static int Main ()
	{
		X18 x18 = new X18 ();
		x18.x1 = new X17 ();
		x18.x2 = new X17 ();
		if (x18.GetType () != typeof (X18))
			return 1;

		return 0;
	}
}


struct X0 { public byte b; }
struct X1 { public X0 x1; public X0 x2; }
struct X2 { public X1 x1; public X1 x2; }
struct X3 { public X2 x1; public X2 x2; }
struct X4 { public X3 x1; public X3 x2; }
struct X5 { public X4 x1; public X4 x2; }
struct X6 { public X5 x1; public X5 x2; }
struct X7 { public X6 x1; public X6 x2; }
struct X8 { public X7 x1; public X7 x2; }
struct X9 { public X8 x1; public X8 x2; }
struct X10 { public X9 x1; public X9 x2; }
struct X11 { public X10 x1; public X10 x2; }
struct X12 { public X11 x1; public X11 x2; }
struct X13 { public X12 x1; public X12 x2; }
struct X14 { public X13 x1; public X13 x2; }
struct X15 { public X14 x1; public X14 x2; }
struct X16 { public X15 x1; public X15 x2; }
struct X17 { public X16 x1; public X16 x2; }
struct X18 { public X17 x1; public X17 x2; }
