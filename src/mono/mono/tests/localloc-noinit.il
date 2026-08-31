/*
using System;

class A
{
	// In the il, remove "init" in the ".locals" of this function:
	// There might be a way to write this in C# directly, but it is not
	// straightforward.
	static unsafe byte* NoInit (int size, byte value)
	{
		byte* data = stackalloc byte [0x10000 + size];
		for (int i = 0; i < size; ++i)
			data [i] += value; // try to keep same form as Init function
		return data;
	}

	static unsafe byte* Init (int size, byte value)
	{
		byte* data = stackalloc byte [0x10000 + size];
		for (int i = 0; i < size; ++i)
			data [i] = value;
		return data;
	}

	static void assert (int a, int b, int c, bool f)
	{
		if (f)
			return;
		Console.WriteLine ("assert failed {0} {1} {2}", a, b, c);
		Environment.Exit (1);
	}

	unsafe public static void Main ()
	{
		int count = 123;
		byte value = 31;
		byte * data;

		data = Init (count, value);
		for (int i = 0; i < 100; ++i)
			assert (1, i, data [i], data [i] == value);

		data = NoInit (count, 0);
		// Previously this would work:
		for (int i = 0; i < 100; ++i)
			assert (2, i, data [i], data [i] == 0);

		// Now this works:
		for (int i = 0; i < 100; ++i)
			assert (3, i, data [i], data [i] == value);
	}
}
*/

.assembly extern mscorlib { }

.assembly '1' { }

.module '1.exe'

.class A
{
  .method static uint8* NoInit(int32 size, uint8 valu) noinlining
  {
    .maxstack  3
    .locals      (uint8* data, int32 i)
//  .locals init (uint8* data, int32 i)
	ldc.i4     0x10000
	ldarg size
	add
	localloc
//	ret		// changes stack layout do not match Init?
	stloc data
	ldc.i4 0
	stloc i
	br       IL_001d

IL_0010:
	ldloc data
	ldloc i
	add
	dup
	ldind.u1
	ldarg valu
	add
	stind.i1
	ldloc i
	ldc.i4 1
	add
	stloc i
IL_001d:
	ldloc 1
	ldarg size
	clt
	brtrue   IL_0010

	ldloc data
	ret
  }

  .method static uint8* Init (int32 size, uint8 valu) noinlining
  {
	.maxstack  2
	.locals init (uint8* data, int32 i)
	ldc.i4     0x10000
	ldarg size
	add
	localloc
	stloc data
	ldc.i4 0
	stloc i
	br.s       IL_0019

IL_0010:
	ldloc data
	ldloc i
	add
	ldarg valu
	stind.i1
	ldloc i
	ldc.i4 1
	add
	stloc i
IL_0019:
	ldloc i
	ldarg size
	clt
	brtrue   IL_0010

	ldloc data
	ret
  }

  .method static void  'assert' (int32 a, int32 b, int32 c, bool f)
  {
	.maxstack  4

	ldarg.3
	brfalse.s  IL_0008
	br.s       IL_002c

IL_0008:
	ldstr      "assert failed {0} {1} {2}"
	ldarg.0
	box        [mscorlib]System.Int32
	ldarg 1
	box        [mscorlib]System.Int32
	ldarg 2
	box        [mscorlib]System.Int32
	call       void [mscorlib]System.Console::WriteLine (string, object, object, object)
	ldc.i4 1
	call       void [mscorlib]System.Environment::Exit (int32)
IL_002c:
	ret
  }

  .method public static void Main ()
  {
    .entrypoint
    .maxstack  5
    .locals init (int32 count, uint8 valu, uint8* data, int32 i)

// --------------------------------------------------

	ldc.i4   123
	stloc count
	ldc.i4   31
	stloc valu

// --------------------------------------------------

	ldloc count
	ldloc valu
	call       uint8* A::Init (int32, uint8)
	stloc data


	ldc.i4 0
	stloc i
	br.s       IL_0031
IL_0013:
	ldc.i4 1
	ldloc i
	ldloc i
	ldloc data
	add
	ldind.u1
	dup
	ldloc valu
	ceq
	call       void A::'assert' (int32, int32, int32, bool)
	ldloc i
	ldc.i4 1
	add
	stloc i
IL_0031:
	ldloc i
	ldc.i4   100
	clt
	brtrue   IL_0013

 // --------------------------------------------------

	ldloc count
	ldc.i4 0
	call       uint8* A::NoInit (int32, uint8)
	stloc data

// --------------------------------------------------
/*
	ldc.i4 0
	stloc.s    i
	br.s       IL_0065

IL_0049:
	ldc.i4 2
	ldloc    i
	ldloc data
	ldloc    i
	add
	ldind.u1
	dup
	ldc.i4 0
	ceq
	call       void A::'assert' (int32, int32, int32, bool)
	ldloc    i
	ldc.i4 1
	add
	stloc    i
IL_0065:
	ldloc    i
	ldc.i4   100
	clt
	brtrue   IL_0049
*/
// --------------------------------------------------
	ldc.i4.0
	stloc.s    i
	br.s       IL_0092

IL_0076:
	ldc.i4 3
	ldloc    i
	ldloc data
	ldloc    i
	add
	ldind.u1
	dup
	ldloc valu
	ceq
	call       void A::'assert' (int32, int32, int32, bool)
	ldloc    i
	ldc.i4 1
	add
	stloc    i
IL_0092:
	ldloc    i
	ldc.i4   100
	clt
	brtrue   IL_0076
	ret
  }
}
