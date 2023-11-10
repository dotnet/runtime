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

public class Test_L_1_6_1{
	[Fact]
	public static int TestEntryPoint()
	{
		int mi_RetCode;
		mi_RetCode = B.Test_L_1_6_1();
		
		if(mi_RetCode == 100)
			Console.WriteLine("Pass");
		else
			Console.WriteLine("FAIL");
		
		return mi_RetCode;
	}
}

class B
{
	public static int Test_L_1_6_1()
	{
		A a = new A();

		int mi_RetCode = 100;
		
		/////////////////////////////////
		// Test nested class access
		if(Test_Nested(a.ClsPubInst.Cls2PubInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(a.ClsPubInst.Cls2AsmInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(a.ClsPubInst.Cls2FoaInst) != 100)
			mi_RetCode = 0;

			
		if(Test_Nested(a.ClsAsmInst.Cls2PubInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(a.ClsAsmInst.Cls2AsmInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(a.ClsAsmInst.Cls2FoaInst) != 100)
			mi_RetCode = 0;

		if(Test_Nested(a.ClsFoaInst.Cls2PubInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(a.ClsFoaInst.Cls2AsmInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(a.ClsFoaInst.Cls2FoaInst) != 100)
			mi_RetCode = 0;


		// static fields
		if(Test_Nested(A.ClsPubStat.Cls2PubInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(A.ClsPubStat.Cls2AsmInst) != 100)
			mi_RetCode = 0;
		
		if(Test_Nested(A.ClsPubStat.Cls2FoaInst) != 100)
			mi_RetCode = 0;

		if(Test_Nested(A.Cls.ClsPubStat) != 100)
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
		
		Nested_Cls.Nest2FldAsmInst = 100;
		if(Nested_Cls.Nest2FldAsmInst != 100)
			mi_RetCode = 0;
		
		Nested_Cls.Nest2FldFoaInst = 100;
		if(Nested_Cls.Nest2FldFoaInst != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test static field access
		A.Cls.Cls2.Nest2FldPubStat = 100;
		if(A.Cls.Cls2.Nest2FldPubStat != 100)
			mi_RetCode = 0;
		
		A.Cls.Cls2.Nest2FldAsmStat = 100;
		if(A.Cls.Cls2.Nest2FldAsmStat != 100)
			mi_RetCode = 0;
		
		A.Cls.Cls2.Nest2FldFoaStat = 100;
		if(A.Cls.Cls2.Nest2FldFoaStat != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test instance method access  
		if(Nested_Cls.Nest2MethPubInst() != 100)
			mi_RetCode = 0;
		
		if(Nested_Cls.Nest2MethAsmInst() != 100)
			mi_RetCode = 0;
		
		if(Nested_Cls.Nest2MethFoaInst() != 100)
			mi_RetCode = 0;
		
		/////////////////////////////////
		// Test static method access
		if(A.Cls.Cls2.Nest2MethPubStat() != 100)
			mi_RetCode = 0;
		
		if(A.Cls.Cls2.Nest2MethAsmStat() != 100)
			mi_RetCode = 0;
		
		if(A.Cls.Cls2.Nest2MethFoaStat() != 100)
			mi_RetCode = 0;  
		
		/////////////////////////////////
		// Test virtual method access
		if(Nested_Cls.Nest2MethPubVirt() != 100)
			mi_RetCode = 0;
		
		if(Nested_Cls.Nest2MethAsmVirt() != 100)
			mi_RetCode = 0;
		
		if(Nested_Cls.Nest2MethFoaVirt() != 100)
			mi_RetCode = 0;  
		
		////////////////////////////////////////////
		// Test access from within the nested class
		if(Nested_Cls.Test_L_1_6_1() != 100)
			mi_RetCode = 0;

		return mi_RetCode;
	}
}


class A
{
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

	// not used
	//private Cls ClsPrivInst = new Cls();
	// not used
	//protected Cls ClsFamInst = new Cls();
	internal Cls ClsAsmInst = new Cls();
	protected internal Cls ClsFoaInst = new Cls();
	
	/////////////////////////////////////
	// Static fields of nested classes
	public static Cls ClsPubStat = new Cls();

	// not used
	//private static Cls ClsPrivStat = new Cls();
	
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
	
	public class Cls
	{
		
		// TODO: CHECK IF THIS IS TESTED
		//////////////////////////////
		// Instance Fields
		// public int NestFldPubInst;
		// private int NestFldPrivInst;
		// protected int NestFldFamInst;          //Translates to "family"
		// internal int NestFldAsmInst;           //Translates to "assembly"
		// protected internal int NestFldFoaInst; //Translates to "famorassem"
		
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

		// not used
		// private Cls2 Cls2PrivInst = new Cls2();
		// protected Cls2 Cls2FamInst = new Cls2();
		internal Cls2 Cls2AsmInst = new Cls2();
		protected internal Cls2 Cls2FoaInst = new Cls2();
		
		/////////////////////////////////////
		// Static fields of nested classes
		public static Cls2 ClsPubStat = new Cls2();

		// not used
		// private static Cls2 ClsPrivStat = new Cls2();
		
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
			public int Test_L_1_6_1(){
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
			//protected int Nest2FldFamInst;          //Translates to "family"
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

