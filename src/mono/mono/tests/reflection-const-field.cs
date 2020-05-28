using System;
using System.Reflection;

class T {

	public const bool a = true;
	public const byte b = 1;
	public const sbyte c = 2;
	public const sbyte cc = -3;
	public const sbyte ccc = 0;
	public const char d = 'a';
	public const short e = 4;
	public const short ee = -5;
	public const short eee = 0;
	public const ushort f = 6;
	public const int g = 7;
	public const int gg = -8;
	public const int ggg = 0;
	public const uint h = 9;
	public const long i = 10;
	public const long ii = -11;
	public const long iii = 0;
	public const ulong j = 12;
	public const double k = 13.0;
	public const double kk = -14.0;
	public const double kkk = 0;
	public const float l = 15;
	public const float ll = -16;
	public const float lll = 0;
	public const string m = "la la la";
	public const string n = null;
	
	static void Main ()
	{
		X ("a", a);
		X ("b", b);
		X ("c", c);
		X ("cc", cc);
		X ("ccc", ccc);
		X ("d", d);
		X ("e", e);
		X ("ee", ee);
		X ("eee", eee);
		X ("f", f);
		X ("g", g);
		X ("gg", gg);
		X ("ggg", ggg);
		X ("h", h);
		X ("i", i);
		X ("ii", ii);
		X ("iii", iii);
		X ("j", j);
		X ("k", k);
		X ("kk", kk);
		X ("kkk", kkk);
		X ("l", l);
		X ("ll", ll);
		X ("lll", lll);
		X ("m", m);
		X ("n", n);
	}
	
	static void X (string n, object o)
	{
		if (! Object.Equals (typeof (T).GetField (n).GetValue (null), o))
			throw new Exception (n);
	}
}