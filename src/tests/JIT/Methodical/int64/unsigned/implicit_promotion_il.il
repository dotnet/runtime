// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly implicit_promotion_il
{
}
.assembly extern xunit.core {}
// MVID: {964B45BB-9F5B-4A2B-9ECD-E062E2FE8E23}
.namespace implicit_promotion
{
  .class public auto ansi Operator
         extends ['mscorlib']System.Object
  {
    .method public hidebysig static native int 
            add_ovf_un_I_i32(native int a,
                         int32 b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      add.ovf.un
      ret
    } // end of method 'Test::add_ovf_un_I_i32'

    .method public hidebysig static native int 
            add_ovf_un_i32_I(int32 a,
                        native int b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      add.ovf.un
      ret
    } // end of method 'Test::add_ovf_un_i32_I'

    .method public hidebysig static native int 
            add_ovf_I_i32(native int a,
                         int32 b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      add.ovf
      ret
    } // end of method 'Test::add_ovf_I_i32'

    .method public hidebysig static native int 
            add_ovf_i32_I(int32 a,
                        native int b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      add.ovf
      ret
    } // end of method 'Test::add_ovf_i32_I'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'JitTest_addsub_unsigned_il'

//*********** DISASSEMBLY COMPLETE ***********************
