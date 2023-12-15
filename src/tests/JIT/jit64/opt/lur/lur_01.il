// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'lur_01' {}


// The loop sould get unrolled and the else ran twice.
// In the origial bug there was a fall through with the if.


.class public test extends [mscorlib]System.Object
{

.method public static int32 main()
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .locals init (float32 V_0,
           float32 V_1,
           int32 V_2,
           uint32 V_3)
  IL_0000:  ldc.r4     1.2
  IL_0005:  stloc      V_0  //v_0=1.2
  IL_0009:  ldc.r4     1.5
  IL_000e:  stloc      V_1 //v_1=1.5
  IL_0012:  ldc.i4     98   
  IL_0017:  stloc      V_2  //v_2=98
  IL_001b:  ldc.i4.2
  IL_001c:  stloc.s    V_3  //v_3=2
  
  IL_001e:  ldloc.s    V_0
  IL_0020:  call       float32 test::fabs(float32)
  IL_0025:  ldloc.s    V_1  
  IL_0027:  blt.un.s   IL_002e // if (fabs(v_0)<= v_1) goto 2e
  IL_0029:  ldc.i4.0
  IL_002a:  stloc.s    V_2
  IL_002c:  br.s       IL_0034
  
  IL_002e:  ldloc.s    V_2
  IL_0030:  ldc.i4.1
  IL_0031:  add
  IL_0032:  stloc.s    V_2 //v_2+=1
  
  IL_0034:  ldloc.s    V_3
  IL_0036:  ldc.i4.1
  IL_0037:  sub
  IL_0038:  stloc.s    V_3 //v_2+=-1
  
  IL_003a:  ldloc.s    V_3
  IL_003c:  ldc.i4.0
  IL_003d:  bgt.un.s   IL_001e //if (v_3>0) goto  i1
  IL_003f:  ldloc      V_2
  IL_0043:  ret

}

.method public static float32 fabs(float32)
{
	ldarg 0
	call       float32 [mscorlib]System.Math::Abs(float32)
	ret	
}


}
