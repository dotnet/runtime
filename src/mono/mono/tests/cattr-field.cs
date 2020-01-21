using System;

[My(val=2,sval="str",bb=0,S="Buh",P=4)]
class T {
	static int Main() {
		object[] a = Attribute.GetCustomAttributes (typeof (T), true);
		My attr = (My)a [0];
		if (attr.val != 2)
			return 1;
		if (attr.P != 4)
			return 2;
		if (attr.S != "Buh")
			return 3;
		return 0;
	}
}

class My : Attribute {
	public int val;
	public uint prop;
	public string s;
	public string sval;
	public byte bb;
	public uint P {
		set {prop = value;}
		get {return prop;}
	}
	public string S {
		set {s = value;}
		get {return s;}
	}
}
