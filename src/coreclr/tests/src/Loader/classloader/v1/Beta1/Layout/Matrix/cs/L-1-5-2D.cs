// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable 414
using System;

public class A{
	//////////////////////////////
	// Instance Fields
	public int FldPubInst;
	private int FldPrivInst;
	protected int FldFamInst;          //Translates to "family"
	internal int FldAsmInst;           //Translates to "assembly"
	protected internal int FldFoaInst; //Translates to "famorassem"
	
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
			
			return mi_RetCode;
		}
		
		//////////////////////////////
		// Instance Fields
		public int NestFldPubInst;
		private int NestFldPrivInst;
		protected int NestFldFamInst;          //Translates to "family"
		internal int NestFldAsmInst;           //Translates to "assembly"
		protected internal int NestFldFoaInst; //Translates to "famorassem"
		
		//////////////////////////////
		// Static Fields
		public static int NestFldPubStat;
		private static int NestFldPrivStat;
		protected static int NestFldFamStat;   //family
		internal static int NestFldAsmStat;    //assembly
		protected internal static int NestFldFoaStat; //famorassem
		
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
		
	}
}








