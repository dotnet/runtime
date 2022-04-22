// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'seq_funcptr_gc'
{

}
.assembly extern xunit.core {}
// MVID: {CF23CD68-3FF2-4D1F-B49B-866DA656D91D}


.file alignment 512


// =============== CLASS MEMBERS DECLARATION ===================
//   note that class flags, 'extends' and 'implements' clauses
//          are provided here for information only

.namespace Test_seq_funcptr_gc
{
.class public sequential ansi sealed beforefieldinit AA
       extends [mscorlib]System.Object
// =============================================================

{
  .field public unsigned int8 tmp1
  .field public unsigned int8 tmp2
  .field public unsigned int8 tmp3
  .field private native int func_ptr
  .field public unsigned int8 tmp4

  .method private hidebysig static int32 ret_code() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    ldc.i4.s   100
    ret
  } // end of method Test_seq_funcptr_gc.AA::ret_code

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method TestApp::.ctor
  
  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       31 (0x1f)
    .maxstack  3
    .locals init (class Test_seq_funcptr_gc.AA V_0, int32 V_1)
    newobj    instance void Test_seq_funcptr_gc.AA::.ctor()
    stloc.0
    ldloc.0
    ldftn      int32 Test_seq_funcptr_gc.AA::ret_code()
    stfld      native int Test_seq_funcptr_gc.AA::func_ptr
    ldloc.0
    ldfld      native int Test_seq_funcptr_gc.AA::func_ptr
    calli       int32 *()
    ret
  } // end of method Test_seq_funcptr_gc.AA::Main

} // end of class Test_seq_funcptr_gc.AA


// =============================================================

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file funcptr_val.res
}
