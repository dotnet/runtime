// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma warning disable 414

using System;
using System.Globalization;

//Define all the standard delegates to be used
public delegate int iDi(int i, out string m);
public delegate int iDNi(int? i, out string m);
public delegate int iDI(I i, out string m);
public delegate int iDS(S s, out string m);
public delegate int iDNS(S? s, out string m);
public delegate int IDo(object o, out string m);
public delegate S SDi(int i, out string m);
public delegate S? NSDi(int i, out string m);
public delegate I IDi(int i, out string m);
public delegate int iDo(object o, out string m);
public delegate object oDi(int i, out string m);

//Define all the open instance delegates to be used
public delegate int iDi<T>(T t,int i, out string m);
public delegate int iDNi<T>(T t,int? i, out string m);
public delegate int iDI<T>(T t,I i, out string m);
public delegate int iDS<T>(T t,S s, out string m);
public delegate int iDNS<T>(T t,S? s, out string m);
public delegate int iDo<T>(T t,object o, out string m);
public delegate S SDi<T>(T t, int i, out string m);
public delegate S? NSDi<T>(T t, int i, out string m);
public delegate I IDi<T>(T t, int i, out string m);
public delegate int IDo<T>(T t,object o, out string m);
public delegate object oDi<T>(T t, int i, out string m);

//Define all the closed static delegates to be used
public delegate T tD<T>(out string m);
public delegate T tDi<T>(int i, out string m);

//@TODO - Are G<Foo> and G<Foo?> equivalent?  Can you even specify a G<Foo?>??
//@TODO - Can you close over an out or ref parameter???
//@TODO - another case, ex. close this method static M(int? i) over a null argument
//@TODO - A delegate declared as D(S?) used as an open instance to bind to a method on S?????  Probably just doesn't work as the type isn't really an S, might work to bind to methods on the Nullable<T> type, but that would be expected.  Should check it to be sure.

//Define the custom types to be used
public interface I{
	bool Equals(int i);
}

public struct S : I{
	//Dummy fields to extend this value type and stress
	//the stub.  We really don't care that they're not used.
	private double f1,f2,f3,f4,f5,f6,f7,f8,f9,f10;

	//An assignable field to be checked for correctness
	public int value;

	public S(int i){
		f1=0;f2=0;f3=0;f4=0;f5=0;f6=0;f7=0;f8=0;f9=0;f10=0;//@BUGBUG - It puzzles me to no end why there is a compiler error if I don't initialize these in the constructor
		value = i;
	}

	public bool Equals(int i){
		return (value==i);
	}

	//For later cleanliness
	public static bool operator ==(S s, int i){
		return s.Equals(i);
	}

	public static bool operator !=(S s, int i){
		return !s.Equals(i);
	}

	public override bool Equals(object o){
		throw new Exception("this just exists to stop a compiler warning, don't call it");
	}
	public override int GetHashCode(){
		throw new Exception("this just exists to stop a compiler warning, don't call it");
	}
}

//Define the various delegate target methods

public class RefInst{ //Instance methods on a reference class
	//The out parameters are a crude tag to verify which method
	//was actually called.  Necessary because the other functionality
	//of the methods is pretty much identical

#region Overloads for BindToMethodName ambiguity testing
	//These should appear in order from most general to most
	//specific or (@TODO) we should have additional tests that
	//vary the method order.  This is to confirm that any
	//ambiguous matching logic in BindToMethodName isn't just
	//settling for the first "match" it sees.  There should
	//be no ambiguity at all in matching.

	public int M(int? i, out string m){
		m = "one";
		if(i==null)
			throw new ArgumentNullException();
		else
			return (int)i;
	}

	public int M(S? s, out string m){
		m = "two";
		if(s==null)
			throw new ArgumentException();
		else
			return ((S)s).value;
	}

	public int M(I i, out string m){
		m = "three";
		if(i==null)
			throw new ArgumentNullException();
		if(!(i is S))
			throw new ArgumentException();
		return ((S)i).value;
	}

	public int M(object o, out string m){
		m = "four";
		if(o == null)
			throw new ArgumentNullException();
		if(!(o is S))
			throw new ArgumentException();
		return ((S)o).value;
	}

	public int M(S s, out string m){
		m = "five";
		return s.value;
	}

	public int M(int i, out string m){
		m = "six";
		return i;
	}
#endregion

#region Non-overloaded methods to allow for (easier) explicit method selection
	public int iMNi(int? i, out string m){
		m = "iMNi";
		if(i==null)
			throw new ArgumentNullException();
		else
			return (int)i;
	}

	public int iMNS(S? s, out string m){
		m = "iMNS";
		if(s==null)
			throw new ArgumentException();
		else
			return ((S)s).value;
	}

	public int iMI(I i, out string m){
		m = "iMI";
		if(i==null)
			throw new ArgumentNullException();
		if(!(i is S))
			throw new ArgumentException();
		return ((S)i).value;
	}

	public int iMo(object o, out string m){
		m = "iMo";
		if(o == null)
			throw new ArgumentNullException();
		if(!(o is S))
			throw new ArgumentException();
		return ((S)o).value;
	}

	public int iMS(S s, out string m){
		m = "iMS";
		return s.value;
	}

	public int iMi(int i, out string m){
		m = "iMi";
		return i;
	}
#endregion

	public S SMi(int i, out string m){
		m = "SMi";
		return new S(i);
	}

	public S? NSMi(int i, out string m){
		m = "NSMi";
		return new S(i);
	}

	public I IMi(int i, out string m){
		m = "IMi";
		return new S(i);
	}

	public object oMi(int i, out string m){
		m = "oMi";
		return new S(i);
	}
}

public class RefStat{ //Static methods on a reference class
	//The out parameters are a crude tag to verify which method
	//was actually called.  Necessary because the other functionality
	//of the methods is pretty much identical

#region Overloads for BindToMethodName ambiguity testing
	//These should appear in order from most general to most
	//specific or (@TODO) we should have additional tests that
	//vary the method order.  This is to confirm that any
	//ambiguous matching logic in BindToMethodName isn't just
	//settling for the first "match" it sees.  There should
	//be no ambiguity at all in matching.

	public static int M(int? i, out string m){
		m = "one";
		if(i==null)
			throw new ArgumentNullException();
		else
			return (int)i;
	}

	public static int M(S? s, out string m){
		m = "two";
		if(s==null)
			throw new ArgumentException();
		else
			return ((S)s).value;
	}

	public static int M(I i, out string m){
		m = "three";
		if(i==null)
			throw new ArgumentNullException();
		if(!(i is S))
			throw new ArgumentException();
		return ((S)i).value;
	}

	public static int M(object o, out string m){
		m = "four";
		if(o == null)
			throw new ArgumentNullException();
		if(!(o is S))
			throw new ArgumentException();
		return ((S)o).value;
	}

	public static int M(S s, out string m){
		m = "five";
		return s.value;
	}

	public static int M(int i, out string m){
		m = "six";
		return i;
	}
#endregion

#region Non-overloaded methods to allow for (easier) explicit method selection
	public static int iMNi(int? i, out string m){
		m = "iMNi";
		if(i==null)
			throw new ArgumentNullException();
		else
			return (int)i;
	}

	public static int iMNS(S? s, out string m){
		m = "iMNS";
		if(s==null)
			throw new ArgumentException();
		else
			return ((S)s).value;
	}

	public static int iMI(I i, out string m){
		m = "iMI";
		if(i==null)
			throw new ArgumentNullException();
		if(!(i is S))
			throw new ArgumentException();
		return ((S)i).value;
	}

	public static int iMo(object o, out string m){
		m = "iMo";
		if(o == null)
			throw new ArgumentNullException();
		if(!(o is S))
			throw new ArgumentException();
		return ((S)o).value;
	}

	public static int iMS(S s, out string m){
		m = "iMS";
		return s.value;
	}

	public static int iMi(int i, out string m){
		m = "iMi";
		return i;
	}
#endregion

	public static S SMi(int i, out string m){
		m = "SMi";
		return new S(i);
	}

	public static S? NSMi(int i, out string m){
		m = "NSMi";
		return new S(i);
	}

	public static I IMi(int i, out string m){
		m = "IMi";
		return new S(i);
	}

	public static object oMi(int i, out string m){
		m = "oMi";
		return new S(i);
	}
}

public struct ValInst{ //Instance methods on a value class
}

public struct ValStat{ //Static methods on a value class
}

//Some reusable helper methods
public class Util{
	//Method to do quick culture invariant string comparisons (quick in the sense that I don't have to type of cultureinfo.invariantlsjflakjdlfjsldkjf 7000 times).
	public static bool Equals(string s1, string s2){
		return String.Equals(s1, s2, StringComparison.Ordinal);
	}
}

#pragma warning restore
