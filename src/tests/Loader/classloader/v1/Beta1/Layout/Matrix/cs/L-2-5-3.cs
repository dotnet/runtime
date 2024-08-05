// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//////////////////////////////////////////////////////////
// L-1-5-1.cs - Beta1 Layout Test - RDawson
//
// Tests layout of classes using 1-deep nesting in
// the same assembly and module (checking access from an
// unrelated class).
//
// See ReadMe.txt in the same project as this source for
// further details about these tests.
//

using System;
using Xunit;

public class Test_L_2_5_3{
	[Fact]
	public static int TestEntryPoint(){
		int mi_RetCode;
		mi_RetCode = B.Test();
		
		if(mi_RetCode == 100)
			Console.WriteLine("Pass");
		else
			Console.WriteLine("FAIL");
		
		return mi_RetCode;
	}
}

struct B{
	public static int Test(){
		int mi_RetCode = 100;
		
		A.Cls ac = new A.Cls();
		A a = new A();
		
		if(Test_Nested(ac) != 100)
			mi_RetCode = 0;
		
		//@csharp - C# simply won't compile non-related private/family/protected access
		
		if(Test_Nested(a.ClsPubInst) != 100)
			mi_RetCode = 0;
		
		return mi_RetCode;
	}
	
	public static int Test_Nested(A.Cls ac){
		int mi_RetCode = 100;
		
		/////////////////////////////////
		// Test instance field access
		ac.NestFldPubInst = 100;
		if(ac.NestFldPubInst != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test static field access
		A.Cls.NestFldPubStat = 100;
		if(A.Cls.NestFldPubStat != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test instance method access  
		if(ac.NestMethPubInst() != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test static method access
		if(A.Cls.NestMethPubStat() != 100)
			mi_RetCode = 0;
		
		////////////////////////////////////////////
		// Test access from within the nested class
		if(ac.Test() != 100)
			mi_RetCode = 0;
		
		return mi_RetCode;
	}
}
