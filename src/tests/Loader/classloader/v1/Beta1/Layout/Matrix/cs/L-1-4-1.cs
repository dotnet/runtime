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

public class Test_L_1_4_1
{
	[Fact]
	public static int TestEntryPoint()
	{
		int mi_RetCode;
		A a = new A();
		mi_RetCode = a.Test_L_1_4_1();
		
		if(mi_RetCode == 100)
			Console.WriteLine("Pass");
		else
			Console.WriteLine("FAIL");
		
		return mi_RetCode;
	}
}

class A
{
	public int Test_L_1_4_1()
	{
		int mi_RetCode = 100;
		
		/////////////////////////////////
		// Test nested class access
		if(Test_Nested(ClsPubInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(ClsPrivInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(ClsFamInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(ClsAsmInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(ClsFoaInst) != 100)
			mi_RetCode = 0;

		if(Test_Nested(ClsPubStat) != 100)
		mi_RetCode = 0;

		if(Test_Nested(ClsPrivStat) != 100)
			mi_RetCode = 0;
	
		return mi_RetCode;
	}
	
	public int Test_Nested(Cls Nested_Cls)
	{
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
		
		//@csharp - Note, CSharp won't allow access of family or private members of a nested class...
		//from it's enclosing class.
		
		Nested_Cls.NestFldAsmInst = 100;
		if(Nested_Cls.NestFldAsmInst != 100)
			mi_RetCode = 0;
		
		Nested_Cls.NestFldFoaInst = 100;
		if(Nested_Cls.NestFldFoaInst != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test static field access
		Cls.NestFldPubStat = 100;
		if(Cls.NestFldPubStat != 100)
			mi_RetCode = 0;
		
		//@csharp - See last @csharp
		
		Cls.NestFldAsmStat = 100;
		if(Cls.NestFldAsmStat != 100)
			mi_RetCode = 0;
		
		Cls.NestFldFoaStat = 100;
		if(Cls.NestFldFoaStat != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test instance method access  
		if(Nested_Cls.NestMethPubInst() != 100)
			mi_RetCode = 0;
		
		//@csharp - See last @csharp
		
		if(Nested_Cls.NestMethAsmInst() != 100)
			mi_RetCode = 0;
		
		if(Nested_Cls.NestMethFoaInst() != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test static method access
		if(Cls.NestMethPubStat() != 100)
			mi_RetCode = 0;
		
		//@csharp - See last @csharp
		
		if(Cls.NestMethAsmStat() != 100)
			mi_RetCode = 0;
		
		if(Cls.NestMethFoaStat() != 100)
			mi_RetCode = 0;  
		
		/////////////////////////////////
		// Test virtual method access
		if(Nested_Cls.NestMethPubVirt() != 100)
			mi_RetCode = 0;
		
		//@csharp - See last @csharp
		
		if(Nested_Cls.NestMethAsmVirt() != 100)
			mi_RetCode = 0;
		
		if(Nested_Cls.NestMethFoaVirt() != 100)
			mi_RetCode = 0;  
		
		////////////////////////////////////////////
		// Test access from within the nested class
		if(Nested_Cls.Test_L_1_4_1() != 100)
			mi_RetCode = 0;
		
		////////////////////////////////////////////
		// Test accessing the second nested class
		if(Test_Nested2(Nested_Cls.Cls2PubInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested2(Nested_Cls.Cls2AsmInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested2(Nested_Cls.Cls2FoaInst) != 100)
			mi_RetCode = 0;

		return mi_RetCode;
	}
	

	public int Test_Nested2(Cls.Cls2 Nested_Cls2)
	{
		int mi_RetCode = 100;
		
		/////////////////////////////////////////////////////////////////////////
		/////////////////////////////////////////////////////////////////////////
		// ACCESS NESTED FIELDS/MEMBERS
		/////////////////////////////////////////////////////////////////////////
		/////////////////////////////////////////////////////////////////////////
		
		/////////////////////////////////
		// Test instance field access
		Nested_Cls2.Nest2FldPubInst = 100;
		if(Nested_Cls2.Nest2FldPubInst != 100)
			mi_RetCode = 0;
		
		//@csharp - Note, CSharp won't allow access of family or private members of a nested class...
		//from it's enclosing class.
		
		Nested_Cls2.Nest2FldAsmInst = 100;
		if(Nested_Cls2.Nest2FldAsmInst != 100)
			mi_RetCode = 0;
		
		Nested_Cls2.Nest2FldFoaInst = 100;
		if(Nested_Cls2.Nest2FldFoaInst != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test static field access
		Cls.Cls2.Nest2FldPubStat = 100;
		if(Cls.Cls2.Nest2FldPubStat != 100)
			mi_RetCode = 0;
		
		//@csharp - See last @csharp
		
		Cls.Cls2.Nest2FldAsmStat = 100;
		if(Cls.Cls2.Nest2FldAsmStat != 100)
			mi_RetCode = 0;
		
		Cls.Cls2.Nest2FldFoaStat = 100;
		if(Cls.Cls2.Nest2FldFoaStat != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test instance method access  
		if(Nested_Cls2.Nest2MethPubInst() != 100)
			mi_RetCode = 0;
		
		//@csharp - See last @csharp
		
		if(Nested_Cls2.Nest2MethAsmInst() != 100)
			mi_RetCode = 0;
		
		if(Nested_Cls2.Nest2MethFoaInst() != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test static method access
		if(Cls.Cls2.Nest2MethPubStat() != 100)
			mi_RetCode = 0;
		
		//@csharp - See last @csharp
		
		if(Cls.Cls2.Nest2MethAsmStat() != 100)
			mi_RetCode = 0;
		
		if(Cls.Cls2.Nest2MethFoaStat() != 100)
			mi_RetCode = 0;  
		
		/////////////////////////////////
		// Test virtual method access
		if(Nested_Cls2.Nest2MethPubVirt() != 100)
			mi_RetCode = 0;
		
		//@csharp - See last @csharp
		
		if(Nested_Cls2.Nest2MethAsmVirt() != 100)
			mi_RetCode = 0;
		
		if(Nested_Cls2.Nest2MethFoaVirt() != 100)
			mi_RetCode = 0;  
		
		////////////////////////////////////////////
		// Test access from within the nested class
		if(Nested_Cls2.Test_L_1_4_1() != 100)
			mi_RetCode = 0;

		return mi_RetCode;
	}



	// TODO: CHECK IF THIS IS TESTED
	//////////////////////////////
	// Instance Fields
	// public int FldPubInst;
	// private int FldPrivInst;
	// protected int FldFamInst;          //Translates to "family"
	// internal int FldAsmInst;           //Translates to "assembly"
	// protected internal int FldFoaInst; //Translates to "famorassem"
	
	//////////////////////////////
	// Static Fields
	public static int FldPubStat;
	private static int FldPrivStat;
	protected static int FldFamStat;   //family
	internal static int FldAsmStat;    //assembly
	protected internal static int FldFoaStat; //famorassem
	
	//////////////////////////////////////
	// Instance fields for nested classes
	public Cls ClsPubInst = new Cls();
	private Cls ClsPrivInst = new Cls();
	protected Cls ClsFamInst = new Cls();
	internal Cls ClsAsmInst = new Cls();
	protected internal Cls ClsFoaInst = new Cls();
	
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
	
	protected int MethFamInst(){
		Console.WriteLine("A::MethFamInst()");
		return 100;
	}
	
	internal int MethAsmInst(){
		Console.WriteLine("A::MethAsmInst()");
		return 100;
	}
	
	protected internal int MethFoaInst(){
		Console.WriteLine("A::MethFoaInst()");
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
	
	protected static int MethFamStat(){
		Console.WriteLine("A::MethFamStat()");
		return 100;
	}
	
	internal static int MethAsmStat(){
		Console.WriteLine("A::MethAsmStat()");
		return 100;
	}
	
	protected internal static int MethFoaStat(){
		Console.WriteLine("A::MethFoaStat()");
		return 100;
	}
	
	//////////////////////////////
	// Virtual Instance Methods
	public virtual int MethPubVirt(){
		Console.WriteLine("A::MethPubVirt()");
		return 100;
	}
	
	//@csharp - Note that C# won't compile an illegal private virtual function
	//So there is no negative testing MethPrivVirt() here.
	
	protected virtual int MethFamVirt(){
		Console.WriteLine("A::MethFamVirt()");
		return 100;
	}
	
	internal virtual int MethAsmVirt(){
		Console.WriteLine("A::MethAsmVirt()");
		return 100;
	}
	
	protected internal virtual int MethFoaVirt(){
		Console.WriteLine("A::MethFoaVirt()");
		return 100;
	}
	
	public class Cls{
		public int Test_L_1_4_1(){
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

			FldPrivStat = 100;
			if(FldPrivStat != 100)
				mi_RetCode = 0;
			
			FldFamStat = 100;
			if(FldFamStat != 100)
				mi_RetCode = 0;
			
			FldAsmStat = 100;
			if(FldAsmStat != 100)
				mi_RetCode = 0;
			
			FldFoaStat = 100;
			if(FldFoaStat != 100)
				mi_RetCode = 0;
			
			/////////////////////////////////
			// Test static method access
			if(MethPubStat() != 100)
				mi_RetCode = 0;
			
			if(MethFamStat() != 100)
				mi_RetCode = 0;
			
			if(MethAsmStat() != 100)
				mi_RetCode = 0;
			
			if(MethFoaStat() != 100)
				mi_RetCode = 0;  
			
			////////////////////////////////////////////
			// Test access from within the nested class
			//@todo - Look at testing accessing one nested class from another, @bugug - NEED TO ADD SUCH TESTING, access the public nested class fields from here, etc...
			
			/////////////////////////////////
			// Test nested class access
			if(Test_Nested(Cls2PubInst) != 100)
				mi_RetCode = 0;
			
			if(Test_Nested(Cls2PrivInst) != 100)
				mi_RetCode = 0;
			
			if(Test_Nested(Cls2FamInst) != 100)
				mi_RetCode = 0;
			
			if(Test_Nested(Cls2AsmInst) != 100)
				mi_RetCode = 0;
			
			if(Test_Nested(Cls2FoaInst) != 100)
				mi_RetCode = 0;


					
			if(Test_Nested(ClsPubStat) != 100)
				mi_RetCode = 0;
			
			if(Test_Nested(ClsPrivStat) != 100)
				mi_RetCode = 0;
			
			return mi_RetCode;
		}
		
		public int Test_Nested(Cls2 Nested_Cls2){
			int mi_RetCode = 100;
			
			/////////////////////////////////////////////////////////////////////////
			/////////////////////////////////////////////////////////////////////////
			// ACCESS NESTED FIELDS/MEMBERS
			/////////////////////////////////////////////////////////////////////////
			/////////////////////////////////////////////////////////////////////////
			
			/////////////////////////////////
			// Test instance field access
			Nested_Cls2.Nest2FldPubInst = 100;
			if(Nested_Cls2.Nest2FldPubInst != 100)
				mi_RetCode = 0;
			
			//@csharp - Note, CSharp won't allow access of family or private members of a nested class...
			//from it's enclosing class.
			
			Nested_Cls2.Nest2FldAsmInst = 100;
			if(Nested_Cls2.Nest2FldAsmInst != 100)
				mi_RetCode = 0;
			
			Nested_Cls2.Nest2FldFoaInst = 100;
			if(Nested_Cls2.Nest2FldFoaInst != 100)
				mi_RetCode = 0;
			
			/////////////////////////////////
			// Test static field access
			Cls2.Nest2FldPubStat = 100;
			if(Cls2.Nest2FldPubStat != 100)
				mi_RetCode = 0;
			
			//@csharp - See last @csharp
			
			Cls2.Nest2FldAsmStat = 100;
			if(Cls2.Nest2FldAsmStat != 100)
				mi_RetCode = 0;
			
			Cls2.Nest2FldFoaStat = 100;
			if(Cls2.Nest2FldFoaStat != 100)
				mi_RetCode = 0;
			
			/////////////////////////////////
			// Test instance method access  
			if(Nested_Cls2.Nest2MethPubInst() != 100)
				mi_RetCode = 0;
			
			//@csharp - See last @csharp
			
			if(Nested_Cls2.Nest2MethAsmInst() != 100)
				mi_RetCode = 0;
			
			if(Nested_Cls2.Nest2MethFoaInst() != 100)
				mi_RetCode = 0;
			
			/////////////////////////////////
			// Test static method access
			if(Cls2.Nest2MethPubStat() != 100)
				mi_RetCode = 0;
			
			//@csharp - See last @csharp
			
			if(Cls2.Nest2MethAsmStat() != 100)
				mi_RetCode = 0;
			
			if(Cls2.Nest2MethFoaStat() != 100)
				mi_RetCode = 0;  
			
			/////////////////////////////////
			// Test virtual method access
			if(Nested_Cls2.Nest2MethPubVirt() != 100)
				mi_RetCode = 0;
			
			//@csharp - See last @csharp
			
			if(Nested_Cls2.Nest2MethAsmVirt() != 100)
				mi_RetCode = 0;
			
			if(Nested_Cls2.Nest2MethFoaVirt() != 100)
				mi_RetCode = 0;  
			
			////////////////////////////////////////////
			// Test access from within the nested class
			if(Nested_Cls2.Test_L_1_4_1() != 100)
				mi_RetCode = 0;
			
			return mi_RetCode;
		}
		
		//////////////////////////////
		// Instance Fields
		public int NestFldPubInst;
			
		// TODO: Check if this is covered in IL
		// private int NestFldPrivInst;		

		// TODO: Check if this is covered in IL
		// protected int NestFldFamInst;          //Translates to "family" 
		
		internal int NestFldAsmInst;           //Translates to "assembly"
		protected internal int NestFldFoaInst; //Translates to "famorassem"
		
		//////////////////////////////
		// Static Fields
		public static int NestFldPubStat;
		private static int NestFldPrivStat;       
		protected static int NestFldFamStat;   //family		
		internal static int NestFldAsmStat;    //assembly
		protected internal static int NestFldFoaStat; //famorassem
		
		//////////////////////////////////////
		// Instance fields for nested classes
		public Cls2 Cls2PubInst = new Cls2();
		private Cls2 Cls2PrivInst = new Cls2();
		protected Cls2 Cls2FamInst = new Cls2();
		internal Cls2 Cls2AsmInst = new Cls2();
		protected internal Cls2 Cls2FoaInst = new Cls2();
		
		/////////////////////////////////////
		// Static fields of nested classes
		public static Cls2 ClsPubStat = new Cls2();
		private static Cls2 ClsPrivStat = new Cls2();
		
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
		
		protected int NestMethFamInst(){
			Console.WriteLine("A::NestMethFamInst()");
			return 100;
		}
		
		internal int NestMethAsmInst(){
			Console.WriteLine("A::NestMethAsmInst()");
			return 100;
		}
		
		protected internal int NestMethFoaInst(){
			Console.WriteLine("A::NestMethFoaInst()");
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
		
		protected static int NestMethFamStat(){
			Console.WriteLine("A::NestMethFamStat()");
			return 100;
		}
		
		internal static int NestMethAsmStat(){
			Console.WriteLine("A::NestMethAsmStat()");
			return 100;
		}
		
		protected internal static int NestMethFoaStat(){
			Console.WriteLine("A::NestMethFoaStat()");
			return 100;
		}
		
		//////////////////////////////
		// Virtual Instance NestMethods
		public virtual int NestMethPubVirt(){
			Console.WriteLine("A::NestMethPubVirt()");
			return 100;
		}
		
		//@csharp - Note that C# won't compile an illegal private virtual function
		//So there is no negative testing NestMethPrivVirt() here.
		
		protected virtual int NestMethFamVirt(){
			Console.WriteLine("A::NestMethFamVirt()");
			return 100;
		}
		
		internal virtual int NestMethAsmVirt(){
			Console.WriteLine("A::NestMethAsmVirt()");
			return 100;
		}
		
		protected internal virtual int NestMethFoaVirt(){
			Console.WriteLine("A::NestMethFoaVirt()");
			return 100;
		}
		
		
		public class Cls2{
			public int Test_L_1_4_1(){
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
				
				FldFamStat = 100;
				if(FldFamStat != 100)
					mi_RetCode = 0;
				
				FldAsmStat = 100;
				if(FldAsmStat != 100)
					mi_RetCode = 0;
				
				FldFoaStat = 100;
				if(FldFoaStat != 100)
					mi_RetCode = 0;
				
				/////////////////////////////////
				// Test static method access
				if(MethPubStat() != 100)
					mi_RetCode = 0;
				
				if(MethFamStat() != 100)
					mi_RetCode = 0;
				
				if(MethAsmStat() != 100)
					mi_RetCode = 0;
				
				if(MethFoaStat() != 100)
					mi_RetCode = 0;  
				
				////////////////////////////////////////////
				// Test access from within the nested class
				//@todo - Look at testing accessing one nested class from another, @bugug - NEED TO ADD SUCH TESTING, access the public nested class fields from here, etc...
				
				/////////////////////////////////
				// Test static field access
				NestFldPubStat = 100;
				if(NestFldPubStat != 100)
					mi_RetCode = 0;

				NestFldPrivStat = 100;
				if(NestFldPrivStat != 100)
					mi_RetCode = 0;

				
				NestFldFamStat = 100;
				if(NestFldFamStat != 100)
					mi_RetCode = 0;
				
				NestFldAsmStat = 100;
				if(NestFldAsmStat != 100)
					mi_RetCode = 0;
				
				NestFldFoaStat = 100;
				if(NestFldFoaStat != 100)
					mi_RetCode = 0;
				
				/////////////////////////////////
				// Test static method access
				if(NestMethPubStat() != 100)
					mi_RetCode = 0;
				
				if(NestMethFamStat() != 100)
					mi_RetCode = 0;
				
				if(NestMethAsmStat() != 100)
					mi_RetCode = 0;
				
				if(NestMethFoaStat() != 100)
					mi_RetCode = 0;  

				return mi_RetCode;
			}
			
			//////////////////////////////
			// Instance Fields
			public int Nest2FldPubInst;

			// TODO: Check if this is covered in IL
			// private int Nest2FldPrivInst;

			// TODO: Check if this is covered in IL
			// protected int Nest2FldFamInst;          //Translates to "family"
			internal int Nest2FldAsmInst;           //Translates to "assembly"
			protected internal int Nest2FldFoaInst; //Translates to "famorassem"
			
			//////////////////////////////
			// Static Fields
			public static int Nest2FldPubStat;

			// TODO: Check if this is covered in IL
			// private static int Nest2FldPrivStat;
			// TODO: Check if this is covered in IL
			// protected static int Nest2FldFamStat;   //family
			internal static int Nest2FldAsmStat;    //assembly
			protected internal static int Nest2FldFoaStat; //famorassem
			
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
			
			protected int Nest2MethFamInst(){
				Console.WriteLine("A::Nest2MethFamInst()");
				return 100;
			}
			
			internal int Nest2MethAsmInst(){
				Console.WriteLine("A::Nest2MethAsmInst()");
				return 100;
			}
			
			protected internal int Nest2MethFoaInst(){
				Console.WriteLine("A::Nest2MethFoaInst()");
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
			
			protected static int Nest2MethFamStat(){
				Console.WriteLine("A::Nest2MethFamStat()");
				return 100;
			}
			
			internal static int Nest2MethAsmStat(){
				Console.WriteLine("A::Nest2MethAsmStat()");
				return 100;
			}
			
			protected internal static int Nest2MethFoaStat(){
				Console.WriteLine("A::Nest2MethFoaStat()");
				return 100;
			}
			
			//////////////////////////////
			// Virtual Instance Nest2Methods
			public virtual int Nest2MethPubVirt(){
				Console.WriteLine("A::Nest2MethPubVirt()");
				return 100;
			}
			
			//@csharp - Note that C# won't compile an illegal private virtual function
			//So there is no negative testing Nest2MethPrivVirt() here.
			
			protected virtual int Nest2MethFamVirt(){
				Console.WriteLine("A::Nest2MethFamVirt()");
				return 100;
			}
			
			internal virtual int Nest2MethAsmVirt(){
				Console.WriteLine("A::Nest2MethAsmVirt()");
				return 100;
			}
			
			protected internal virtual int Nest2MethFoaVirt(){
				Console.WriteLine("A::Nest2MethFoaVirt()");
				return 100;
			}
		}
	}
}

