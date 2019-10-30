// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 414
using System;


public class A{
	//////////////////////////////
	// Instance Fields
	public int FldAPubInst;
	private int FldAPrivInst;
	protected int FldAFamInst;          //Translates to "family"
	internal int FldAAsmInst;           //Translates to "assembly"
	protected internal int FldAFoaInst; //Translates to "famorassem"
	
	//////////////////////////////
	// Static Fields
	public static int FldAPubStat;
	private static int FldAPrivStat;
	protected static int FldAFamStat;   //family
	internal static int FldAAsmStat;    //assembly
	protected internal static int FldAFoaStat; //famorassem
	
	//////////////////////////////////////
	// Instance fields for nested classes
	public ClsA ClsAPubInst = new ClsA();
	private ClsA ClsAPrivInst = new ClsA();
	protected ClsA ClsAFamInst = new ClsA();
	internal ClsA ClsAAsmInst = new ClsA();
	protected internal ClsA ClsAFoaInst = new ClsA();
	
	/////////////////////////////////////
	// Static fields of nested classes
	public static ClsA ClsAPubStat = new ClsA();
	private static ClsA ClsAPrivStat = new ClsA();
	
	//////////////////////////////
	// Instance Methods
	public int MethAPubInst(){
		Console.WriteLine("A::MethAPubInst()");
		return 100;
	}
	
	private int MethAPrivInst(){
		Console.WriteLine("A::MethAPrivInst()");
		return 100;
	}
	
	protected int MethAFamInst(){
		Console.WriteLine("A::MethAFamInst()");
		return 100;
	}
	
	internal int MethAAsmInst(){
		Console.WriteLine("A::MethAAsmInst()");
		return 100;
	}
	
	protected internal int MethAFoaInst(){
		Console.WriteLine("A::MethAFoaInst()");
		return 100;
	}
	
	//////////////////////////////
	// Static Methods
	public static int MethAPubStat(){
		Console.WriteLine("A::MethAPubStat()");
		return 100;
	}
	
	private static int MethAPrivStat(){
		Console.WriteLine("A::MethAPrivStat()");
		return 100;
	}
	
	protected static int MethAFamStat(){
		Console.WriteLine("A::MethAFamStat()");
		return 100;
	}
	
	internal static int MethAAsmStat(){
		Console.WriteLine("A::MethAAsmStat()");
		return 100;
	}
	
	protected internal static int MethAFoaStat(){
		Console.WriteLine("A::MethAFoaStat()");
		return 100;
	}
	
	//////////////////////////////
	// Virtual Instance Methods
	public virtual int MethAPubVirt(){
		Console.WriteLine("A::MethAPubVirt()");
		return 100;
	}
	
	//@csharp - Note that C# won't compile an illegal private virtual function
	//So there is no negative testing MethAPrivVirt() here.
	
	protected virtual int MethAFamVirt(){
		Console.WriteLine("A::MethAFamVirt()");
		return 100;
	}
	
	internal virtual int MethAAsmVirt(){
		Console.WriteLine("A::MethAAsmVirt()");
		return 100;
	}
	
	protected internal virtual int MethAFoaVirt(){
		Console.WriteLine("A::MethAFoaVirt()");
		return 100;
	}
	
	public class ClsA{
		//////////////////////////////
		// Instance Fields
		public int NestFldAPubInst;
		private int NestFldAPrivInst;
		protected int NestFldAFamInst;          //Translates to "family"
		internal int NestFldAAsmInst;           //Translates to "assembly"
		protected internal int NestFldAFoaInst; //Translates to "famorassem"
		
		//////////////////////////////
		// Static Fields
		public static int NestFldAPubStat;
		private static int NestFldAPrivStat;
		protected static int NestFldAFamStat;   //family
		internal static int NestFldAAsmStat;    //assembly
		protected internal static int NestFldAFoaStat; //famorassem
		
		//////////////////////////////////////
		// Instance fields for nested classes
		public ClsA2 ClsA2PubInst = new ClsA2();
		private ClsA2 ClsA2PrivInst = new ClsA2();
		protected ClsA2 ClsA2FamInst = new ClsA2();
		internal ClsA2 ClsA2AsmInst = new ClsA2();
		protected internal ClsA2 ClsA2FoaInst = new ClsA2();
		
		/////////////////////////////////////
		// Static fields of nested classes
		public static ClsA2 ClsA2PubStat = new ClsA2();
		private static ClsA2 ClsA2PrivStat = new ClsA2();
		
		//////////////////////////////
		// Instance NestMethAods
		public int NestMethAPubInst(){
			Console.WriteLine("A::NestMethAPubInst()");
			return 100;
		}
		
		private int NestMethAPrivInst(){
			Console.WriteLine("A::NestMethAPrivInst()");
			return 100;
		}
		
		protected int NestMethAFamInst(){
			Console.WriteLine("A::NestMethAFamInst()");
			return 100;
		}
		
		internal int NestMethAAsmInst(){
			Console.WriteLine("A::NestMethAAsmInst()");
			return 100;
		}
		
		protected internal int NestMethAFoaInst(){
			Console.WriteLine("A::NestMethAFoaInst()");
			return 100;
		}
		
		//////////////////////////////
		// Static NestMethods
		public static int NestMethAPubStat(){
			Console.WriteLine("A::NestMethAPubStat()");
			return 100;
		}
		
		private static int NestMethAPrivStat(){
			Console.WriteLine("A::NestMethAPrivStat()");
			return 100;
		}
		
		protected static int NestMethAFamStat(){
			Console.WriteLine("A::NestMethAFamStat()");
			return 100;
		}
		
		internal static int NestMethAAsmStat(){
			Console.WriteLine("A::NestMethAAsmStat()");
			return 100;
		}
		
		protected internal static int NestMethAFoaStat(){
			Console.WriteLine("A::NestMethAFoaStat()");
			return 100;
		}
		
		//////////////////////////////
		// Virtual Instance NestMethods
		public virtual int NestMethAPubVirt(){
			Console.WriteLine("A::NestMethAPubVirt()");
			return 100;
		}
		
		//@csharp - Note that C# won't compile an illegal private virtual function
		//So there is no negative testing NestMethAPrivVirt() here.
		
		protected virtual int NestMethAFamVirt(){
			Console.WriteLine("A::NestMethAFamVirt()");
			return 100;
		}
		
		internal virtual int NestMethAAsmVirt(){
			Console.WriteLine("A::NestMethAAsmVirt()");
			return 100;
		}
		
		protected internal virtual int NestMethAFoaVirt(){
			Console.WriteLine("A::NestMethAFoaVirt()");
			return 100;
		}
		
		public class ClsA2{
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
				NestFldA2PubStat = 100;
				if(NestFldA2PubStat != 100)
					mi_RetCode = 0;
				
				NestFldA2FamStat = 100;
				if(NestFldA2FamStat != 100)
					mi_RetCode = 0;
				
				NestFldA2AsmStat = 100;
				if(NestFldA2AsmStat != 100)
					mi_RetCode = 0;
				
				NestFldA2FoaStat = 100;
				if(NestFldA2FoaStat != 100)
					mi_RetCode = 0;
				
				/////////////////////////////////
				// Test static method access
				if(NestMethA2PubStat() != 100)
					mi_RetCode = 0;
				
				if(NestMethA2FamStat() != 100)
					mi_RetCode = 0;
				
				if(NestMethA2AsmStat() != 100)
					mi_RetCode = 0;
				
				if(NestMethA2FoaStat() != 100)
					mi_RetCode = 0;  
				
				return mi_RetCode;
			}
			
			
			//////////////////////////////
			// Instance Fields
			public int NestFldA2PubInst;
			private int NestFldA2PrivInst;
			protected int NestFldA2FamInst;          //Translates to "family"
			internal int NestFldA2AsmInst;           //Translates to "assembly"
			protected internal int NestFldA2FoaInst; //Translates to "famorassem"
			
			//////////////////////////////
			// Static Fields
			public static int NestFldA2PubStat;
			private static int NestFldA2PrivStat;
			protected static int NestFldA2FamStat;   //family
			internal static int NestFldA2AsmStat;    //assembly
			protected internal static int NestFldA2FoaStat; //famorassem
			
			//////////////////////////////
			// Instance NestMethA2ods
			public int NestMethA2PubInst(){
				Console.WriteLine("A::NestMethA2PubInst()");
				return 100;
			}
			
			private int NestMethA2PrivInst(){
				Console.WriteLine("A::NestMethA2PrivInst()");
				return 100;
			}
			
			protected int NestMethA2FamInst(){
				Console.WriteLine("A::NestMethA2FamInst()");
				return 100;
			}
			
			internal int NestMethA2AsmInst(){
				Console.WriteLine("A::NestMethA2AsmInst()");
				return 100;
			}
			
			protected internal int NestMethA2FoaInst(){
				Console.WriteLine("A::NestMethA2FoaInst()");
				return 100;
			}
			
			//////////////////////////////
			// Static NestMethods
			public static int NestMethA2PubStat(){
				Console.WriteLine("A::NestMethA2PubStat()");
				return 100;
			}
			
			private static int NestMethA2PrivStat(){
				Console.WriteLine("A::NestMethA2PrivStat()");
				return 100;
			}
			
			protected static int NestMethA2FamStat(){
				Console.WriteLine("A::NestMethA2FamStat()");
				return 100;
			}
			
			internal static int NestMethA2AsmStat(){
				Console.WriteLine("A::NestMethA2AsmStat()");
				return 100;
			}
			
			protected internal static int NestMethA2FoaStat(){
				Console.WriteLine("A::NestMethA2FoaStat()");
				return 100;
			}
			
			//////////////////////////////
			// Virtual Instance NestMethods
			public virtual int NestMethA2PubVirt(){
				Console.WriteLine("A::NestMethA2PubVirt()");
				return 100;
			}
			
			//@csharp - Note that C# won't compile an illegal private virtual function
			//So there is no negative testing NestMethA2PrivVirt() here.
			
			protected virtual int NestMethA2FamVirt(){
				Console.WriteLine("A::NestMethA2FamVirt()");
				return 100;
			}
			
			internal virtual int NestMethA2AsmVirt(){
				Console.WriteLine("A::NestMethA2AsmVirt()");
				return 100;
			}
			
			protected internal virtual int NestMethA2FoaVirt(){
				Console.WriteLine("A::NestMethA2FoaVirt()");
				return 100;
			}
			
		}
	}
}

