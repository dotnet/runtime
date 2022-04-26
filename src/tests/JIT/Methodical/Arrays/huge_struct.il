// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.






.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'huge_struct'
{


  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {23E3D49B-6CFD-4DA0-8C9D-A611CE0BA103}
.namespace JitTest_huge_struct_Arrays_il
{
  .class value private auto ansi sealed TestStruct extends [mscorlib]System.ValueType
  {
    .field public float64[] dbl_ptr
    .field public bool[] b_ptr
    .field public int32 magic
    .field public unsigned int64[] ul_ptr
  } // end of class 'TestStruct'
  
  .class public auto ansi Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       38 (0x26)
      .maxstack  70
      .locals init (value class JitTest_huge_struct_Arrays_il.TestStruct[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] matrix,
               int32 count,
			   int32[] dim,
			   value class JitTest_huge_struct_Arrays_il.TestStruct t,
			   value class JitTest_huge_struct_Arrays_il.TestStruct r)


      			ldc.i4	32
      			dup
				stloc count
				
				dup
				add
				newarr    int32
			again:
				//we'll need two refs of array
				dup
				dup
				//decrement counter
				ldloc   count
				ldc.i4.1
				sub
				dup
				stloc   count
				//multiple counter by 2
				dup
				add
				//calculate lower bound for this dimension
				dup
				ldc.i4 20
				mul
				stelem.i4
				//calculate index again
				ldloc count
				dup
				add
				ldc.i4.1
				add
				//calculate number of elements for this dimension
				//	1 1 2 1 1 2 1 1 2 etc.
				ldloc count
				ldc.i4.3
				rem
				dup
				brtrue	cool
				pop
				ldc.i4.1
			cool:
				stelem.i4
				ldloc count
				brtrue again
				
				//print out an array bounds
				ldc.i4.0
				stloc count
			again2:
				ldloc count
				ldc.i4.1
				add
				dup
				stloc count
				ldc.i4 64
				ceq
				brfalse again2
				
				//it would be so cool if I could push
				//the values on the stack in a loop ;-)
				stloc dim
				ldloc dim ldc.i4.0 ldelem.i4
				ldloc dim ldc.i4.1 ldelem.i4
				ldloc dim ldc.i4.2 ldelem.i4
				ldloc dim ldc.i4.3 ldelem.i4
				ldloc dim ldc.i4.4 ldelem.i4
				ldloc dim ldc.i4.5 ldelem.i4
				ldloc dim ldc.i4.6 ldelem.i4
				ldloc dim ldc.i4.7 ldelem.i4
				ldloc dim ldc.i4.8 ldelem.i4
				ldloc dim ldc.i4 9 ldelem.i4
				ldloc dim ldc.i4 10 ldelem.i4
				ldloc dim ldc.i4 11 ldelem.i4
				ldloc dim ldc.i4 12 ldelem.i4
				ldloc dim ldc.i4 13 ldelem.i4
				ldloc dim ldc.i4 14 ldelem.i4
				ldloc dim ldc.i4 15 ldelem.i4
				ldloc dim ldc.i4 16 ldelem.i4
				ldloc dim ldc.i4 17 ldelem.i4
				ldloc dim ldc.i4 18 ldelem.i4
				ldloc dim ldc.i4 19 ldelem.i4
				ldloc dim ldc.i4 20 ldelem.i4
				ldloc dim ldc.i4 21 ldelem.i4
				ldloc dim ldc.i4 22 ldelem.i4
				ldloc dim ldc.i4 23 ldelem.i4
				ldloc dim ldc.i4 24 ldelem.i4
				ldloc dim ldc.i4 25 ldelem.i4
				ldloc dim ldc.i4 26 ldelem.i4
				ldloc dim ldc.i4 27 ldelem.i4
				ldloc dim ldc.i4 28 ldelem.i4
				ldloc dim ldc.i4 29 ldelem.i4
				ldloc dim ldc.i4 30 ldelem.i4
				ldloc dim ldc.i4 31 ldelem.i4
				ldloc dim ldc.i4 32 ldelem.i4
				ldloc dim ldc.i4 33 ldelem.i4
				ldloc dim ldc.i4 34 ldelem.i4
				ldloc dim ldc.i4 35 ldelem.i4
				ldloc dim ldc.i4 36 ldelem.i4
				ldloc dim ldc.i4 37 ldelem.i4
				ldloc dim ldc.i4 38 ldelem.i4
				ldloc dim ldc.i4 39 ldelem.i4
				ldloc dim ldc.i4 40 ldelem.i4
				ldloc dim ldc.i4 41 ldelem.i4
				ldloc dim ldc.i4 42 ldelem.i4
				ldloc dim ldc.i4 43 ldelem.i4
				ldloc dim ldc.i4 44 ldelem.i4
				ldloc dim ldc.i4 45 ldelem.i4
				ldloc dim ldc.i4 46 ldelem.i4
				ldloc dim ldc.i4 47 ldelem.i4
				ldloc dim ldc.i4 48 ldelem.i4
				ldloc dim ldc.i4 49 ldelem.i4
				ldloc dim ldc.i4 50 ldelem.i4
				ldloc dim ldc.i4 51 ldelem.i4
				ldloc dim ldc.i4 52 ldelem.i4
				ldloc dim ldc.i4 53 ldelem.i4
				ldloc dim ldc.i4 54 ldelem.i4
				ldloc dim ldc.i4 55 ldelem.i4
				ldloc dim ldc.i4 56 ldelem.i4
				ldloc dim ldc.i4 57 ldelem.i4
				ldloc dim ldc.i4 58 ldelem.i4
				ldloc dim ldc.i4 59 ldelem.i4
				ldloc dim ldc.i4 60 ldelem.i4
				ldloc dim ldc.i4 61 ldelem.i4
				ldloc dim ldc.i4 62 ldelem.i4
				ldloc dim ldc.i4 63 ldelem.i4
				newobj		instance void value class JitTest_huge_struct_Arrays_il.TestStruct[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::.ctor(
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32)
      			stloc matrix
      			
				//---------------------------------------------------------------
				//	Test typed array's Set method
				//---------------------------------------------------------------
      			ldloc matrix
				ldloc dim ldc.i4.0 ldelem.i4
				ldloc dim ldc.i4.2 ldelem.i4
				ldloc dim ldc.i4.4 ldelem.i4
				ldloc dim ldc.i4.6 ldelem.i4
				ldloc dim ldc.i4.8 ldelem.i4
				ldloc dim ldc.i4 10 ldelem.i4
				ldloc dim ldc.i4 12 ldelem.i4
				ldloc dim ldc.i4 14 ldelem.i4
				ldloc dim ldc.i4 16 ldelem.i4
				ldloc dim ldc.i4 18 ldelem.i4
				ldloc dim ldc.i4 20 ldelem.i4
				ldloc dim ldc.i4 22 ldelem.i4
				ldloc dim ldc.i4 24 ldelem.i4
				ldloc dim ldc.i4 26 ldelem.i4
				ldloc dim ldc.i4 28 ldelem.i4
				ldloc dim ldc.i4 30 ldelem.i4
				ldloc dim ldc.i4 32 ldelem.i4
				ldloc dim ldc.i4 34 ldelem.i4
				ldloc dim ldc.i4 36 ldelem.i4
				ldloc dim ldc.i4 38 ldelem.i4
				ldloc dim ldc.i4 40 ldelem.i4
				ldloc dim ldc.i4 42 ldelem.i4
				ldloc dim ldc.i4 44 ldelem.i4
				ldloc dim ldc.i4 46 ldelem.i4
				ldloc dim ldc.i4 48 ldelem.i4
				ldloc dim ldc.i4 50 ldelem.i4
				ldloc dim ldc.i4 52 ldelem.i4
				ldloc dim ldc.i4 54 ldelem.i4
				ldloc dim ldc.i4 56 ldelem.i4
				ldloc dim ldc.i4 58 ldelem.i4
				ldloc dim ldc.i4 60 ldelem.i4
				ldloc dim ldc.i4 62 ldelem.i4
				
      			ldloca	t
				initobj		JitTest_huge_struct_Arrays_il.TestStruct
      			ldloca	t
      			dup
				ldc.i4	99
				newarr		float64
				dup
				ldc.i4  98
				ldc.r8	8.0
				stelem.r8
				stfld	float64[] JitTest_huge_struct_Arrays_il.TestStruct::dbl_ptr
				ldc.i4	0x05071975
				stfld	int32 JitTest_huge_struct_Arrays_il.TestStruct::magic

				call    void [mscorlib]System.GC::Collect()

				ldloc	t
      			callvirt 	instance void value class JitTest_huge_struct_Arrays_il.TestStruct[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::Set(
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					value class JitTest_huge_struct_Arrays_il.TestStruct)

				//---------------------------------------------------------------
				//	Test typed array's Get method
				//---------------------------------------------------------------
				ldloc matrix
				ldloc dim ldc.i4.0 ldelem.i4
				ldloc dim ldc.i4.2 ldelem.i4
				ldloc dim ldc.i4.4 ldelem.i4
				ldloc dim ldc.i4.6 ldelem.i4
				ldloc dim ldc.i4.8 ldelem.i4
				ldloc dim ldc.i4 10 ldelem.i4
				ldloc dim ldc.i4 12 ldelem.i4
				ldloc dim ldc.i4 14 ldelem.i4
				ldloc dim ldc.i4 16 ldelem.i4
				ldloc dim ldc.i4 18 ldelem.i4
				ldloc dim ldc.i4 20 ldelem.i4
				ldloc dim ldc.i4 22 ldelem.i4
				ldloc dim ldc.i4 24 ldelem.i4
				ldloc dim ldc.i4 26 ldelem.i4
				ldloc dim ldc.i4 28 ldelem.i4
				ldloc dim ldc.i4 30 ldelem.i4
				ldloc dim ldc.i4 32 ldelem.i4
				ldloc dim ldc.i4 34 ldelem.i4
				ldloc dim ldc.i4 36 ldelem.i4
				ldloc dim ldc.i4 38 ldelem.i4
				ldloc dim ldc.i4 40 ldelem.i4
				ldloc dim ldc.i4 42 ldelem.i4
				ldloc dim ldc.i4 44 ldelem.i4
				ldloc dim ldc.i4 46 ldelem.i4
				ldloc dim ldc.i4 48 ldelem.i4
				ldloc dim ldc.i4 50 ldelem.i4
				ldloc dim ldc.i4 52 ldelem.i4
				ldloc dim ldc.i4 54 ldelem.i4
				ldloc dim ldc.i4 56 ldelem.i4
				ldloc dim ldc.i4 58 ldelem.i4
				ldloc dim ldc.i4 60 ldelem.i4
				ldloc dim ldc.i4 62 ldelem.i4
      			callvirt 	instance value class JitTest_huge_struct_Arrays_il.TestStruct value class JitTest_huge_struct_Arrays_il.TestStruct[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::Get(
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32)
      			
      			stloc r
      			ldloca r
      			dup
      			ldfld   float64[] JitTest_huge_struct_Arrays_il.TestStruct::dbl_ptr
      			ldc.i4 98
      			ldelem.r8
      			ldc.r8 8.0
      			ceq
      			brtrue next0_0
      			pop
      			br  throw0
			next0_0:
      			ldfld   int32 JitTest_huge_struct_Arrays_il.TestStruct::magic
      			ldc.i4  0x05071975
      			ceq
      			brfalse throw0
      			br pass1

			cont1:
				//---------------------------------------------------------------
				//	Test Get method
				//---------------------------------------------------------------
				ldloc matrix
				ldloc dim ldc.i4.0 ldelem.i4
				ldloc dim ldc.i4.2 ldelem.i4
				ldloc dim ldc.i4.4 ldelem.i4
				ldloc dim ldc.i4.6 ldelem.i4
				ldloc dim ldc.i4.8 ldelem.i4
				ldloc dim ldc.i4 10 ldelem.i4
				ldloc dim ldc.i4 12 ldelem.i4
				ldloc dim ldc.i4 14 ldelem.i4
				ldloc dim ldc.i4 16 ldelem.i4
				ldloc dim ldc.i4 18 ldelem.i4
				ldloc dim ldc.i4 20 ldelem.i4
				ldloc dim ldc.i4 22 ldelem.i4
				ldloc dim ldc.i4 24 ldelem.i4
				ldloc dim ldc.i4 26 ldelem.i4
				ldloc dim ldc.i4 28 ldelem.i4
				ldloc dim ldc.i4 30 ldelem.i4
				ldloc dim ldc.i4 32 ldelem.i4
				ldloc dim ldc.i4 34 ldelem.i4
				ldloc dim ldc.i4 36 ldelem.i4
				ldloc dim ldc.i4 38 ldelem.i4
				ldloc dim ldc.i4 40 ldelem.i4
				ldloc dim ldc.i4 42 ldelem.i4
				ldloc dim ldc.i4 44 ldelem.i4
				ldloc dim ldc.i4 46 ldelem.i4
				ldloc dim ldc.i4 48 ldelem.i4
				ldloc dim ldc.i4 50 ldelem.i4
				ldloc dim ldc.i4 52 ldelem.i4
				ldloc dim ldc.i4 54 ldelem.i4
				ldloc dim ldc.i4 56 ldelem.i4
				ldloc dim ldc.i4 58 ldelem.i4
				ldloc dim ldc.i4 60 ldelem.i4
				ldloc dim ldc.i4 62 ldelem.i4
				br  go1
				
      		cont2:
      			br cont3
      			
      		go1:
      			callvirt instance value class JitTest_huge_struct_Arrays_il.TestStruct value class JitTest_huge_struct_Arrays_il.TestStruct[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::Get(
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32)
      			stloc r
      			ldloca r
      			dup
      			ldfld   float64[] JitTest_huge_struct_Arrays_il.TestStruct::dbl_ptr
      			ldc.i4 98
      			ldelem.r8
      			ldc.r8 8.0
      			ceq
      			brtrue next0_1
      			pop
      			br  throw0
			next0_1:
      			ldfld   int32 JitTest_huge_struct_Arrays_il.TestStruct::magic
      			ldc.i4  0x05071975
      			ceq
      			brfalse throw0
      			br pass1_5
      			
      		throw0:
      			newobj		instance void [mscorlib]System.Exception::.ctor()
      		throw1:
				throw
				
      		pass1:
      			br cont0
      			
      		pass2:
				//---------------------------------------------------------------
				//	Test Set method
				//---------------------------------------------------------------
      			ldloc matrix
				ldloc dim ldc.i4.0 ldelem.i4
				ldloc dim ldc.i4.2 ldelem.i4
				ldloc dim ldc.i4.4 ldelem.i4
				ldloc dim ldc.i4.6 ldelem.i4
				ldloc dim ldc.i4.8 ldelem.i4
				ldloc dim ldc.i4 10 ldelem.i4
				ldloc dim ldc.i4 12 ldelem.i4
				ldloc dim ldc.i4 14 ldelem.i4
				ldloc dim ldc.i4 16 ldelem.i4
				ldloc dim ldc.i4 18 ldelem.i4
				ldloc dim ldc.i4 20 ldelem.i4
				ldloc dim ldc.i4 22 ldelem.i4
				ldloc dim ldc.i4 24 ldelem.i4
				ldloc dim ldc.i4 26 ldelem.i4
				ldloc dim ldc.i4 28 ldelem.i4
				ldloc dim ldc.i4 30 ldelem.i4
				ldloc dim ldc.i4 32 ldelem.i4
				ldloc dim ldc.i4 34 ldelem.i4
				ldloc dim ldc.i4 36 ldelem.i4
				ldloc dim ldc.i4 38 ldelem.i4
				ldloc dim ldc.i4 40 ldelem.i4
				ldloc dim ldc.i4 42 ldelem.i4
				ldloc dim ldc.i4 44 ldelem.i4
				ldloc dim ldc.i4 46 ldelem.i4
				ldloc dim ldc.i4 48 ldelem.i4
				ldloc dim ldc.i4 50 ldelem.i4
				ldloc dim ldc.i4 52 ldelem.i4
				ldloc dim ldc.i4 54 ldelem.i4
				ldloc dim ldc.i4 56 ldelem.i4
				ldloc dim ldc.i4 58 ldelem.i4
				ldloc dim ldc.i4 60 ldelem.i4
				ldloc dim ldc.i4 62 ldelem.i4
				ldloc	t
      			callvirt 	instance void value class JitTest_huge_struct_Arrays_il.TestStruct[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::Set(
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					value class JitTest_huge_struct_Arrays_il.TestStruct)
      			ldnull
      			stloc matrix
      			call void [mscorlib]System.GC::Collect()
      			
      			ldc.i4 100
      			ret
      			
			cont0:
      			br cont2
      			
      		cont3:
      			br cont1
      			
      		pass1_5:
				//---------------------------------------------------------------
				//	Test typed Address method
				//---------------------------------------------------------------
				ldloc matrix
				ldloc dim ldc.i4.0 ldelem.i4
				ldloc dim ldc.i4.2 ldelem.i4
				ldloc dim ldc.i4.4 ldelem.i4
				ldloc dim ldc.i4.6 ldelem.i4
				ldloc dim ldc.i4.8 ldelem.i4
				ldloc dim ldc.i4 10 ldelem.i4
				ldloc dim ldc.i4 12 ldelem.i4
				ldloc dim ldc.i4 14 ldelem.i4
				ldloc dim ldc.i4 16 ldelem.i4
				ldloc dim ldc.i4 18 ldelem.i4
				ldloc dim ldc.i4 20 ldelem.i4
				ldloc dim ldc.i4 22 ldelem.i4
				ldloc dim ldc.i4 24 ldelem.i4
				ldloc dim ldc.i4 26 ldelem.i4
				ldloc dim ldc.i4 28 ldelem.i4
				ldloc dim ldc.i4 30 ldelem.i4
				ldloc dim ldc.i4 32 ldelem.i4
				ldloc dim ldc.i4 34 ldelem.i4
				ldloc dim ldc.i4 36 ldelem.i4
				ldloc dim ldc.i4 38 ldelem.i4
				ldloc dim ldc.i4 40 ldelem.i4
				ldloc dim ldc.i4 42 ldelem.i4
				ldloc dim ldc.i4 44 ldelem.i4
				ldloc dim ldc.i4 46 ldelem.i4
				ldloc dim ldc.i4 48 ldelem.i4
				ldloc dim ldc.i4 50 ldelem.i4
				ldloc dim ldc.i4 52 ldelem.i4
				ldloc dim ldc.i4 54 ldelem.i4
				ldloc dim ldc.i4 56 ldelem.i4
				ldloc dim ldc.i4 58 ldelem.i4
				ldloc dim ldc.i4 60 ldelem.i4
				ldloc dim ldc.i4 62 ldelem.i4
      			callvirt instance value class JitTest_huge_struct_Arrays_il.TestStruct& value class JitTest_huge_struct_Arrays_il.TestStruct[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::Address(
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32)
      			dup
      			ldfld   float64[] JitTest_huge_struct_Arrays_il.TestStruct::dbl_ptr
      			ldc.i4 98
      			ldelem.r8
      			ldc.r8 8.0
      			ceq
      			brtrue next0_2
      			pop
      			br  throw0
			next0_2:
      			ldfld   int32 JitTest_huge_struct_Arrays_il.TestStruct::magic
      			ldc.i4  0x05071975
      			ceq
      			brfalse throw0
      			br pass2
      			
      			newobj		instance void [mscorlib]System.Exception::.ctor()
				br throw1
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'
    
  } // end of class 'Test'
  
} // end of namespace 'JitTest_huge_struct_Arrays_il'
//*********** DISASSEMBLY COMPLETE ***********************
