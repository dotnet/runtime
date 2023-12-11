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

public class Test_L_2_6_1{
	[Fact]
	public static int TestEntryPoint(){
		int mi_RetCode;
		mi_RetCode = B.Test_L_2_6_1();
		
		if(mi_RetCode == 100)
			Console.WriteLine("Pass");
		else
			Console.WriteLine("FAIL");
		
		return mi_RetCode;
	}
}

struct B{
	public static int Test_L_2_6_1(){
		A a = new A();

		int mi_RetCode = 100;
		
		/////////////////////////////////
		// Test nested class access
		if(Test_Nested(a.ClsPubInst.Cls2PubInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(a.ClsPubInst.Cls2AsmInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(a.ClsAsmInst.Cls2PubInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(a.ClsAsmInst.Cls2AsmInst) != 100)
			mi_RetCode = 0;

		// to get rid of compiler warning 
		// warning CS0414: The private field 'A.ClsPrivStat' is assigned but its value is never used
		A.getClsPrivStat().ToString();

		// to get rid of compiler warning 
		// warning CS0414: The private field 'A.ClsPrivStat' is assigned but its value is never used
		A.Cls.getClsPrivStat().ToString();
		
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
		
		Nested_Cls.Nest2FldAsmInst = 100;
		if(Nested_Cls.Nest2FldAsmInst != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test static field access
		A.Cls.Cls2.Nest2FldPubStat = 100;
		if(A.Cls.Cls2.Nest2FldPubStat != 100)
			mi_RetCode = 0;
		
		A.Cls.Cls2.Nest2FldAsmStat = 100;
		if(A.Cls.Cls2.Nest2FldAsmStat != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test instance method access  
		if(Nested_Cls.Nest2MethPubInst() != 100)
			mi_RetCode = 0;
		
		if(Nested_Cls.Nest2MethAsmInst() != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test static method access
		if(A.Cls.Cls2.Nest2MethPubStat() != 100)
			mi_RetCode = 0;
		
		if(A.Cls.Cls2.Nest2MethAsmStat() != 100)
			mi_RetCode = 0;
		
		////////////////////////////////////////////
		// Test access from within the nested class
		if(Nested_Cls.Test_L_2_6_1() != 100)
			mi_RetCode = 0;

		return mi_RetCode;
	}
}


struct A{
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

#pragma warning disable 0414
	public static Cls ClsPubStat = new Cls();
	private static Cls ClsPrivStat = new Cls();
#pragma warning restore 0414

	public static Cls getClsPrivStat()
	{
		return ClsPrivStat;
	}
	
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
		
		//////////////////////////////////////
		// Instance fields for nested classes
		public Cls2 Cls2PubInst;
		private Cls2 Cls2PrivInst;
		internal Cls2 Cls2AsmInst;
		
		/////////////////////////////////////
		// Static fields of nested classes
#pragma warning disable 0414
		public static Cls ClsPubStat = new Cls();
		private static Cls ClsPrivStat = new Cls();
#pragma warning restore 0414
		public static Cls getClsPrivStat()
		{
			return ClsPrivStat;
		}
		
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
		
		
		
		public struct Cls2{
			public int Test_L_2_6_1(){
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
				
				/////////////////////////////////
				// Test static field access
				NestFldPubStat = 100;
				if(NestFldPubStat != 100)
					mi_RetCode = 0;
				
				NestFldAsmStat = 100;
				if(NestFldAsmStat != 100)
					mi_RetCode = 0;
				
				/////////////////////////////////
				// Test static method access
				if(NestMethPubStat() != 100)
					mi_RetCode = 0;
				
				if(NestMethAsmStat() != 100)
					mi_RetCode = 0;
				
				return mi_RetCode;
			}
			
			//////////////////////////////
			// Instance Fields
			public int Nest2FldPubInst;
			private int Nest2FldPrivInst;
			internal int Nest2FldAsmInst;           //Translates to "assembly"
			
			//////////////////////////////
			// Static Fields
			public static int Nest2FldPubStat;
			private static int Nest2FldPrivStat;
			internal static int Nest2FldAsmStat;    //assembly
			
			//////////////////////////////
			// Instance Nest2Methods
			public int Nest2MethPubInst(){
				Console.WriteLine("A::Nest2MethPubInst()");
				return 100;
			}
			
			private int Nest2MethPrivInst(){
				Console.WriteLine("A::Nest2MethPrivInst()");
				return 100;
			}
			
			internal int Nest2MethAsmInst(){
				Console.WriteLine("A::Nest2MethAsmInst()");
				return 100;
			}
			
			//////////////////////////////
			// Static Nest2Methods
			public static int Nest2MethPubStat(){
				Console.WriteLine("A::Nest2MethPubStat()");
				return 100;
			}
			
			private static int Nest2MethPrivStat(){
				Console.WriteLine("A::Nest2MethPrivStat()");
				return 100;
			}
			
			internal static int Nest2MethAsmStat(){
				Console.WriteLine("A::Nest2MethAsmStat()");
				return 100;
			}
			
			
		}
	}
}

