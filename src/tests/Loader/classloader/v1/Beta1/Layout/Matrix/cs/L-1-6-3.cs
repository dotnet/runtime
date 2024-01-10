// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//////////////////////////////////////////////////////////
// L-1-4-1.cs - Beta1 Layout Test - RDawson
//
// Tests layout of classes using 2-deep nesting in
// the same assembly and module
//
// See ReadMe.txt in the same project as this source for
// further details about these tests.
//

using System;
using Xunit;

public class Test_L_1_6_3{
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

class B{
	public static int Test(){
		A a = new A();

		int mi_RetCode = 100;
		
		/////////////////////////////////
		// Test nested class access
		if(Test_Nested(a.ClsPubInst.Cls2PubInst) != 100)
			mi_RetCode = 0;
		
		return mi_RetCode;
	}
	
	public static int Test_Nested(A.Cls.Cls2 Nested_Cls){
		int mi_RetCode = 100;
		
		/////////////////////////////////////////////////////////////////////////
		/////////////////////////////////////////////////////////////////////////
		// ACCESS NESTED FIELDS/MEMBERS
		/////////////////////////////////////////////////////////////////////////
		/////////////////////////////////////////////////////////////////////////
		
		/////////////////////////////////
		// Test instance field access
		Nested_Cls.Nest2FldPubInst = 100;
		if(Nested_Cls.Nest2FldPubInst != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test static field access
		A.Cls.Cls2.Nest2FldPubStat = 100;
		if(A.Cls.Cls2.Nest2FldPubStat != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test instance method access  
		if(Nested_Cls.Nest2MethPubInst() != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test static method access
		if(A.Cls.Cls2.Nest2MethPubStat() != 100)
			mi_RetCode = 0;
				
		/////////////////////////////////
		// Test virtual method access
		if(Nested_Cls.Nest2MethPubVirt() != 100)
			mi_RetCode = 0;
		
		////////////////////////////////////////////
		// Test access from within the nested class
		if(Nested_Cls.Test() != 100)
			mi_RetCode = 0;

		return mi_RetCode;
	}
}
