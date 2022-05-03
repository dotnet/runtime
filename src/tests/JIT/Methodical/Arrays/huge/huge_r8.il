// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.






.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
                           
  
           
  
}
.assembly 'huge_r8'
{


  
  
}
.assembly extern xunit.core {}
.namespace JitTest_huge_r8_huge_il
{
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
      .maxstack  70
      .locals (float64[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] matrix,
               int32 count,
			   int32[] dim)
               
      			ldstr     "Creating 32-dimension array"
      			call       void [System.Console]System.Console::WriteLine(string)

      			ldc.i4	32
      			dup
				stloc count
				
				dup
				add
				newarr    int32
			again:
				dup
				dup
				ldloc   count
				ldc.i4.1
				sub
				dup
				stloc   count
				dup
				add
				dup
				ldc.i4 20
				mul
				stelem.i4
				ldloc count
				dup
				add
				ldc.i4.1
				add
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
				
				ldc.i4.0
				stloc count
			again2:
				dup
				ldloc count
				ldelem.i4
				call       void [System.Console]System.Console::Write(int32)
				ldstr " "
				call       void [System.Console]System.Console::Write(string)
				ldloc count
				ldc.i4.1
				add
				dup
				stloc count
				ldc.i4 64
				ceq
				brfalse again2
				
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
				newobj		instance void float64[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::.ctor(
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
      			ldc.i4 	0x57751795
      			conv.r8
      			callvirt 	instance void float64[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::Set(
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					float64)

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
      			callvirt 	instance float64 float64[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::Get(
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32)
      			conv.ovf.i4
      			ldc.i4 1
      			div
      			dup
      			call  void [System.Console]System.Console::WriteLine(int32)
      			ldc.i4 	0x57751795
      			ldc.i4 1
      			div
      			dup
      			call  void [System.Console]System.Console::WriteLine(int32)
      			ceq
      			brtrue pass1
      			br throw0
				
			cont1:
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
      			callvirt instance float64 float64[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::Get(
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32)
      			conv.ovf.i4
      			ldc.i4 1
      			div
      			dup
      			call  void [System.Console]System.Console::WriteLine(int32)
      			ldc.i4 	0x57751795
      			ldc.i4 1
      			div
      			dup
      			call  void [System.Console]System.Console::WriteLine(int32)
      			ceq
      			brtrue pass1_5
      		throw0:
      			newobj		instance void [mscorlib]System.Exception::.ctor()
      		throw1:
				throw
				
      		pass1:
      			br cont0
      			
      		pass2:
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
      			ldc.i4 	0x57769795
      			conv.r8
      			callvirt 	instance void float64[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::Set(
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					float64)
      			ldnull
      			stloc matrix
      			call void [mscorlib]System.GC::Collect()
      			
      			ldc.i4 100
      			ldstr     "TEST PASSED"
      			call       void [System.Console]System.Console::Write(string)
      			ret
      			
			cont0:
      			br cont2
      			
      		cont3:
      			br cont1
      			
      		pass1_5:
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
      			callvirt instance float64& float64[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::Address(
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32,
      					int32, int32, int32, int32)

      			ldind.r8
      			conv.ovf.i4
      			ldc.i4 1
      			div
      			dup
      			call  void [System.Console]System.Console::WriteLine(int32)
      			ldc.i4 	0x57751795
      			ldc.i4 1
      			div
      			dup
      			call  void [System.Console]System.Console::WriteLine(int32)
      			ceq
     			call void [mscorlib]System.GC::Collect()
      			brtrue pass2
      			
      			newobj		instance void [mscorlib]System.Exception::.ctor()
				br throw1
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'
    
  } // end of class 'Test'
  
} // end of namespace 'JitTest_huge_r8_huge_il'
