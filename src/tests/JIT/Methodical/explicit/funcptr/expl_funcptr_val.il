// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'expl_funcptr_val'
{


  //                                                                                bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {CF23CD68-3FF2-4D1F-B49B-866DA656D91D}


.file alignment 512


// =============== CLASS MEMBERS DECLARATION ===================
//   note that class flags, 'extends' and 'implements' clauses
//          are provided here for information only

.namespace Test_expl_funcptr_val
{
.class public explicit ansi sealed beforefieldinit AA
       extends [mscorlib]System.ValueType
{
  .field [1] public unsigned int8 tmp1
  .field [2] public unsigned int8 tmp2
  .field [3] public unsigned int8 tmp3
  .field [8] private native int func_ptr
  .field [14] public unsigned int8 tmp4

  .method private hidebysig static int32 ret_code() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    ldc.i4.s   100
    ret
  } // end of method Test_expl_funcptr_val.AA::ret_code

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       31 (0x1f)
    .maxstack  3
    .locals init (valuetype Test_expl_funcptr_val.AA V_0, int32 V_1)
    ldloca.s   V_0
    ldftn      int32 Test_expl_funcptr_val.AA::ret_code()
    stfld      native int Test_expl_funcptr_val.AA::func_ptr
    ldloca.s   V_0
    ldfld      native int Test_expl_funcptr_val.AA::func_ptr
    calli       int32 *()
    ret
  } // end of method Test_expl_funcptr_val.AA::Main

} // end of class Test_expl_funcptr_val.AA


// =============================================================

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file funcptr_val.res
}
