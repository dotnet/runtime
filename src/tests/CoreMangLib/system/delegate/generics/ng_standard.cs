// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

public class Test_ng_standard{
	[Fact]
	public static void TestEntryPoint(){
		Console.WriteLine("Test creation/invocation of non-generic closed instance or open static delegates over various generic methods");

		GenericClass<EQStruct<long>> refr = new GenericClass<EQStruct<long>>();
		GenericStruct<EQStruct<long>> val = new GenericStruct<EQStruct<long>>();
		refr.value = 50;
		val.value = 50;

		//Closed instance methods
		dc1 d1 = new dc1(refr.M1);
		d1(50);
		d1 = new dc1(val.M1);
		d1(50);

		dc4 d2 = new dc4(refr.M2);
		d2(new EQStruct<long>(50),50);
		d2 = new dc4(val.M2);
		d2(new EQStruct<long>(50),50);

		dc5 d3 = new dc5(refr.M3);
		d3(50, new EQStruct<long>(50));
		d3 = new dc5(val.M3);
		d3(50, new EQStruct<long>(50));

		dc4 d4 = new dc4(refr.M4);
		d4(new EQStruct<long>(50),50);
		d4 = new dc4(val.M4);
		d4(new EQStruct<long>(50),50);

		dc5 d5 = new dc5(refr.M5);
		d5(50, new EQStruct<long>(50));
		d5 = new dc5(val.M5);
		d5(50, new EQStruct<long>(50));

		dc6 d6 = new dc6(refr.M6);
		d6(new EQClass<long>(50),50);
		d6 = new dc6(val.M6);
		d6(new EQClass<long>(50),50);

		dc7 d7 = new dc7(refr.M7);
		d7(50,new EQClass<long>(50));
		d7 = new dc7(val.M7);
		d7(50,new EQClass<long>(50));

		dc6 d8 = new dc6(refr.M8<EQClass<long>>);
		d8(new EQClass<long>(50),50);
		d8 = new dc6(val.M8<EQClass<long>>);
		d8(new EQClass<long>(50),50);

		dc7 d9 = new dc7(refr.M9<EQClass<long>>);
		d9(50,new EQClass<long>(50));
		d9 = new dc7(val.M9<EQClass<long>>);
		d9(50,new EQClass<long>(50));

		//Open static methods
		d1 = new dc1(GenericClass<EQStruct<long>>.SM1);
		d1(50);
		d1 = new dc1(GenericStruct<EQStruct<long>>.SM1);
		d1(50);

		d2 = new dc4(GenericClass<EQStruct<long>>.SM2);
		d2(new EQStruct<long>(50),50);
		d2 = new dc4(GenericStruct<EQStruct<long>>.SM2);
		d2(new EQStruct<long>(50),50);

		d3 = new dc5(GenericClass<EQStruct<long>>.SM3);
		d3(50, new EQStruct<long>(50));
		d3 = new dc5(GenericStruct<EQStruct<long>>.SM3);
		d3(50, new EQStruct<long>(50));

		d4 = new dc4(GenericClass<EQStruct<long>>.SM4);
		d4(new EQStruct<long>(50),50);
		d4 = new dc4(GenericStruct<EQStruct<long>>.SM4);
		d4(new EQStruct<long>(50),50);

		d5 = new dc5(GenericClass<EQStruct<long>>.SM5);
		d5(50, new EQStruct<long>(50));
		d5 = new dc5(GenericStruct<EQStruct<long>>.SM5);
		d5(50, new EQStruct<long>(50));

		d6 = new dc6(GenericClass<EQStruct<long>>.SM6);
		d6(new EQClass<long>(50),50);
		d6 = new dc6(GenericStruct<EQStruct<long>>.SM6);
		d6(new EQClass<long>(50),50);

		d7 = new dc7(GenericClass<EQStruct<long>>.SM7);
		d7(50,new EQClass<long>(50));
		d7 = new dc7(GenericStruct<EQStruct<long>>.SM7);
		d7(50,new EQClass<long>(50));

		d8 = new dc6(GenericClass<EQStruct<long>>.SM8<EQClass<long>>);
		d8(new EQClass<long>(50),50);
		d8 = new dc6(GenericStruct<EQStruct<long>>.SM8<EQClass<long>>);
		d8(new EQClass<long>(50),50);

		d9 = new dc7(GenericClass<EQStruct<long>>.SM9<EQClass<long>>);
		d9(50,new EQClass<long>(50));
		d9 = new dc7(GenericStruct<EQStruct<long>>.SM9<EQClass<long>>);
		d9(50,new EQClass<long>(50));		
	}
}
