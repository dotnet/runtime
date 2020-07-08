// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//////////////////////////////////////////////////////////
// L-1-3-1.cs - Beta1 Layout Test - RDawson
//
// Tests layout of classes using 1-deep nesting in
// the same assembly and module
//
// See ReadMe.txt in the same project as this source for
// further details about these tests.
//

using System;

class Test{
	public static int Main(){
		int mi_RetCode;
		A a = new A();
		mi_RetCode = a.Test();

			
		if(mi_RetCode == 100)
			Console.WriteLine("Pass");
		else
			Console.WriteLine("FAIL");
		
		return mi_RetCode;
	}
}

struct A{
//@csharp - C# will not allow family or famorassem accessibility on value class members

	public int Test(){
		int mi_RetCode = 100;
		
		/////////////////////////////////
		// Test nested class access
		if(Test_Nested(ClsPubInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(ClsPrivInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(ClsAsmInst) != 100)
			mi_RetCode = 0;

		// to get rid of compiler warning 
		// warning CS0414: The private field 'A.ClsPrivStat' is assigned but its value is never used
		A.ClsPubStat.ToString();
		A.ClsPrivStat.ToString();
			
		return mi_RetCode;
	}
	
	public int Test_Nested(Cls Nested_Cls){
		int mi_RetCode = 100;
		
		/////////////////////////////////////////////////////////////////////////
		/////////////////////////////////////////////////////////////////////////
		// ACCESS NESTED FIELDS/MEMBERS
		/////////////////////////////////////////////////////////////////////////
		/////////////////////////////////////////////////////////////////////////
		
		/////////////////////////////////
		// Test instance field access
		Nested_Cls.NestFldPubInst = 100;
		if(Nested_Cls.NestFldPubInst != 100)
			mi_RetCode = 0;
		
		Nested_Cls.NestFldAsmInst = 100;
		if(Nested_Cls.NestFldAsmInst != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test static field access
		Cls.NestFldPubStat = 100;
		if(Cls.NestFldPubStat != 100)
			mi_RetCode = 0;
	
		Cls.NestFldAsmStat = 100;
		if(Cls.NestFldAsmStat != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test instance method access  
		if(Nested_Cls.NestMethPubInst() != 100)
			mi_RetCode = 0;
				
		if(Nested_Cls.NestMethAsmInst() != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test static method access
		if(Cls.NestMethPubStat() != 100)
			mi_RetCode = 0;
		
		if(Cls.NestMethAsmStat() != 100)
			mi_RetCode = 0;
		
		////////////////////////////////////////////
		// Test access from within the nested class
		if(Nested_Cls.Test() != 100)
			mi_RetCode = 0;
		
		return mi_RetCode;
	}
	
	
	//////////////////////////////
	// Instance Fields
	public int FldPubInst;
	private int FldPrivInst;
	internal int FldAsmInst;           //Translates to "assembly"
	
	//////////////////////////////
	// Static Fields
	public static int FldPubStat;
	private static int FldPrivStat;
	internal static int FldAsmStat;    //assembly
	
	//////////////////////////////////////
	// Instance fields for nested classes
	public Cls ClsPubInst;
	private Cls ClsPrivInst;
	internal Cls ClsAsmInst;
	
	/////////////////////////////////////
	// Static fields of nested classes
	public static Cls ClsPubStat = new Cls();
	private static Cls ClsPrivStat = new Cls();
	
	//////////////////////////////
	// Instance Methods
	public int MethPubInst(){
		Console.WriteLine("A::MethPubInst()");
		return 100;
	}
	
	private int MethPrivInst(){
		Console.WriteLine("A::MethPrivInst()");
		return 100;
	}
	
	internal int MethAsmInst(){
		Console.WriteLine("A::MethAsmInst()");
		return 100;
	}
	
	//////////////////////////////
	// Static Methods
	public static int MethPubStat(){
		Console.WriteLine("A::MethPubStat()");
		return 100;
	}
	
	private static int MethPrivStat(){
		Console.WriteLine("A::MethPrivStat()");
		return 100;
	}
	
	internal static int MethAsmStat(){
		Console.WriteLine("A::MethAsmStat()");
		return 100;
	}
	
	
	public struct Cls{
		public int Test(){
			int mi_RetCode = 100;
			
			/////////////////////////////////////////////////////////////////////////
			/////////////////////////////////////////////////////////////////////////
			// ACCESS ENCLOSING FIELDS/MEMBERS
			/////////////////////////////////////////////////////////////////////////
			/////////////////////////////////////////////////////////////////////////
			
			//@csharp - C# will not allow nested classes to access non-static members of their enclosing classes
			
			/////////////////////////////////
			// Test static field access
			FldPubStat = 100;
			if(FldPubStat != 100)
				mi_RetCode = 0;
			
			FldAsmStat = 100;
			if(FldAsmStat != 100)
				mi_RetCode = 0;
			
			/////////////////////////////////
			// Test static method access
			if(MethPubStat() != 100)
				mi_RetCode = 0;
			
			if(MethAsmStat() != 100)
				mi_RetCode = 0;
			
			////////////////////////////////////////////
			// Test access from within the nested class
			//@todo - Look at testing accessing one nested class from another, @bugug - NEED TO ADD SUCH TESTING, access the public nested class fields from here, etc...
			
			return mi_RetCode;
		}
		
		//////////////////////////////
		// Instance Fields
		public int NestFldPubInst;
		private int NestFldPrivInst;
		internal int NestFldAsmInst;           //Translates to "assembly"
		
		//////////////////////////////
		// Static Fields
		public static int NestFldPubStat;
		private static int NestFldPrivStat;
		internal static int NestFldAsmStat;    //assembly
		
		//////////////////////////////
		// Instance NestMethods
		public int NestMethPubInst(){
			Console.WriteLine("A::NestMethPubInst()");
			return 100;
		}
		
		private int NestMethPrivInst(){
			Console.WriteLine("A::NestMethPrivInst()");
			return 100;
		}
		
		internal int NestMethAsmInst(){
			Console.WriteLine("A::NestMethAsmInst()");
			return 100;
		}
		
		//////////////////////////////
		// Static NestMethods
		public static int NestMethPubStat(){
			Console.WriteLine("A::NestMethPubStat()");
			return 100;
		}
		
		private static int NestMethPrivStat(){
			Console.WriteLine("A::NestMethPrivStat()");
			return 100;
		}
		
		internal static int NestMethAsmStat(){
			Console.WriteLine("A::NestMethAsmStat()");
			return 100;
		}
	}
}








