// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//@TODO - API level testing for generics, particulary with multicast invocation lists and co/contra variant generics
//@TODO - Vary the return type

//Disable warnings about unused fields, they exist only to pad the size of the structs/objects to stress the stub types, we don't care that they aren't used.
#pragma warning disable 649
#pragma warning disable 169
using System;

//////////////////////////////////////////////////////////////////////
//Non generic delegates over various generic targets
//////////////////////////////////////////////////////////////////////

//Closed Instance / Open static Non-Generic delegates for all M1-M9s
delegate long dc1(long l);
delegate long dc4(EQStruct<long> t, long l);
delegate long dc5(long l, EQStruct<long> t);
delegate long dc6(EQClass<long> t, long l);
delegate long dc7(long l, EQClass<long> t);

//Open Instance Non-Generic delegates for M1-M9 over a reference generic
delegate long do1_ref(GenericClass<EQStruct<long>> o,long l);
delegate long do4_ref(GenericClass<EQStruct<long>> o, EQStruct<long> t, long l);
delegate long do5_ref(GenericClass<EQStruct<long>> o, long l, EQStruct<long> t);
delegate long do6_ref(GenericClass<EQStruct<long>> o, EQClass<long> t, long l);
delegate long do7_ref(GenericClass<EQStruct<long>> o, long l, EQClass<long> t);

//Open Instance Non-Generic delegates for M1-M9 over a value generic
delegate long do1_val(ref GenericStruct<EQStruct<long>> o,long l);
delegate long do4_val(ref GenericStruct<EQStruct<long>> o, EQStruct<long> t, long l);
delegate long do5_val(ref GenericStruct<EQStruct<long>> o, long l, EQStruct<long> t);
delegate long do6_val(ref GenericStruct<EQStruct<long>> o, EQClass<long> t, long l);
delegate long do7_val(ref GenericStruct<EQStruct<long>> o, long l, EQClass<long> t);

//Closed Static Non-Generic delegates for SM1-SM9
delegate long dcs1();
delegate long dcs2(long l);
delegate long dcs5(EQStruct<long> t);
delegate long dcs7(EQClass<long> t);

//////////////////////////////////////////////////////////////////////
//Generic delegate types over varous generic targets
//////////////////////////////////////////////////////////////////////

//Minimal set of generic delegates to cover all the above cases
delegate T g0<T>();
delegate T g1<T,U>(U u);
delegate T g2<T, U, V>(U u, V v);
delegate T g3<T, U, V, W>(U u, V v, W w);

//////////////////////////////////////////////////////////////////////

class GenericClass<T> where T:Equality{
	public long value,field2,field3,field4,field5,field6;

	public long M1(long l){
		if(value!=l)
			throw new Exception();
		return 100;
	}

	public long M2(T t, long l){
		if(!t.Equals(l) || value!=l)
			throw new Exception();
		return 100;
	}

	public long M3(long l, T t){
		if(!t.Equals(l) || value!=l)
			throw new Exception();
		return 100;
	}

	public long M4(EQStruct<long> t, long l){
		if(!t.Equals(l) || value!=l)
			throw new Exception();
		return 100;
	}

	public long M5(long l, EQStruct<long> t){
		if(!t.Equals(l) || value!=l)
			throw new Exception();
		return 100;
	}

	public long M6(EQClass<long> t, long l){
		if(!t.Equals(l) || value!=l)
			throw new Exception();
		return 100;
	}

	public long M7(long l, EQClass<long> t){
		if(!t.Equals(l) || value!=l)
			throw new Exception();
		return 100;
	}

	public long M8<U>(U u, long l) where U:Equality{
		if(!u.Equals(l) || value!=l)
			throw new Exception();
		return 100;
	}

	public long M9<U>(long l, U u) where U:Equality{
		if(!u.Equals(l) || value!=l)
			throw new Exception();
		return 100;
	}

	public static long SM1(long l){
		return 100;
	}

	public static long SM2(T t, long l){
		if(!t.Equals(l))
			throw new Exception();
		return 100;
	}

	public static long SM3(long l, T t){
		if(!t.Equals(l))
			throw new Exception();
		return 100;
	}

	public static long SM4(EQStruct<long> t, long l){
		if(!t.Equals(l))
			throw new Exception();
		return 100;
	}

	public static long SM5(long l, EQStruct<long> t){
		if(!t.Equals(l))
			throw new Exception();
		return 100;
	}

	public static long SM6(EQClass<long> t, long l){
		if(!t.Equals(l))
			throw new Exception();
		return 100;
	}

	public static long SM7(long l, EQClass<long> t){
		if(!t.Equals(l))
			throw new Exception();
		return 100;
	}

	public static long SM8<U>(U u, long l) where U:Equality{
		if(!u.Equals(l))
			throw new Exception();
		return 100;
	}

	public static long SM9<U>(long l, U u) where U:Equality{
		if(!u.Equals(l))
			throw new Exception();
		return 100;
	}
}

struct GenericStruct<T> where T:Equality{
	public long value,field2,field3,field4,field5,field6;

	public long M1(long l){
		if(value!=l)
			throw new Exception();
		return 100;
	}

	public long M2(T t, long l){
        Console.WriteLine(l);
		if(!t.Equals(l) || value!=l)
			throw new Exception();
		return 100;
	}

	public long M3(long l, T t){
		if(!t.Equals(l) || value!=l)
			throw new Exception();
		return 100;
	}

	public long M4(EQStruct<long> t, long l){
		if(!t.Equals(l) || value!=l)
			throw new Exception();
		return 100;
	}

	public long M5(long l, EQStruct<long> t){
		if(!t.Equals(l) || value!=l)
			throw new Exception();
		return 100;
	}

	public long M6(EQClass<long> t, long l){
		if(!t.Equals(l) || value!=l)
			throw new Exception();
		return 100;
	}

	public long M7(long l, EQClass<long> t){
		if(!t.Equals(l) || value!=l)
			throw new Exception();
		return 100;
	}

	public long M8<U>(U u, long l) where U:Equality{
		if(!u.Equals(l) || value!=l)
			throw new Exception();
		return 100;
	}

	public long M9<U>(long l, U u) where U:Equality{
		if(!u.Equals(l) || value!=l)
			throw new Exception();
		return 100;
	}

	public static long SM1(long l){
		return 100;
	}

	public static long SM2(T t, long l){
		if(!t.Equals(l))
			throw new Exception();
		return 100;
	}

	public static long SM3(long l, T t){
		if(!t.Equals(l))
			throw new Exception();
		return 100;
	}

	public static long SM4(EQStruct<long> t, long l){
		if(!t.Equals(l))
			throw new Exception();
		return 100;
	}

	public static long SM5(long l, EQStruct<long> t){
		if(!t.Equals(l))
			throw new Exception();
		return 100;
	}

	public static long SM6(EQClass<long> t, long l){
		if(!t.Equals(l))
			throw new Exception();
		return 100;
	}

	public static long SM7(long l, EQClass<long> t){
		if(!t.Equals(l))
			throw new Exception();
		return 100;
	}

	public static long SM8<U>(U u, long l) where U:Equality{
		if(!u.Equals(l))
			throw new Exception();
		return 100;
	}

	public static long SM9<U>(long l, U u) where U:Equality{
		if(!u.Equals(l))
			throw new Exception();
		return 100;
	}
}

interface Equality{
	bool Equals(long l);
}
/*
class EQClass : Equality{
	double f1,f2,f3,f4,f5,f6,f7,f8,f9,f10;
	long value;

	public EQClass(long l){
		value = l;
	}

	public bool Equals(long l){
		return (value==l);
	}
}

class EQStruct : Equality{
	double f1,f2,f3,f4,f5,f6,f7,f8,f9,f10;
	long value;

	public EQStruct(long l){
		value = l;
	}

	public bool Equals(long l){
		return (value==l);
	}
}
*/
class EQClass<T> : Equality{
	double f1,f2,f3,f4,f5,f6,f7,f8,f9,f10;
	public T f11;
	long value;

	public EQClass(long l){
		value = l;
	}

	public bool Equals(long l){
		return (value==l);
	}
}

class EQStruct<T> : Equality{
	double f1,f2,f3,f4,f5,f6,f7,f8,f9,f10;
	public T f11;
	long value;

	public EQStruct(long l){
		value = l;
	}

	public bool Equals(long l){
		return (value==l);
	}
}
#pragma warning restore
