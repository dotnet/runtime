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
            add_I_i32(native int a,
                     int32 b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      add
      ret
    } // end of method 'Operator::add_I_i32'

    .method public hidebysig static native int 
            add_i32_I(int32 a,
                    native int b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      add
      ret
    } // end of method 'Operator::add_i32_I'

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
    } // end of method 'Operator::add_ovf_I_i32'

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
    } // end of method 'Operator::add_ovf_i32_I'

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
    } // end of method 'Operator::add_ovf_un_I_i32'

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
    } // end of method 'Operator::add_ovf_un_i32_I'

    .method public hidebysig static native int 
            sub_I_i32(native int a,
                     int32 b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      sub
      ret
    } // end of method 'Operator::sub_I_i32'

    .method public hidebysig static native int 
            sub_i32_I(int32 a,
                    native int b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      sub
      ret
    } // end of method 'Operator::sub_i32_I'

    .method public hidebysig static native int 
            sub_ovf_I_i32(native int a,
                         int32 b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      sub.ovf
      ret
    } // end of method 'Operator::sub_ovf_I_i32'

    .method public hidebysig static native int 
            sub_ovf_i32_I(int32 a,
                        native int b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      sub.ovf
      ret
    } // end of method 'Operator::sub_ovf_i32_I'

    .method public hidebysig static native int 
            sub_ovf_un_I_i32(native int a,
                            int32 b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      sub.ovf.un
      ret
    } // end of method 'Operator::sub_ovf_un_I_i32'

    .method public hidebysig static native int 
            sub_ovf_un_i32_I(int32 a,
                           native int b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      sub.ovf.un
      ret
    } // end of method 'Operator::sub_ovf_un_i32'

    .method public hidebysig static native int 
            mul_I_i32(native int a,
                     int32 b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      mul
      ret
    } // end of method 'Operator::mul_I_i32'

    .method public hidebysig static native int 
            mul_i32_I(int32 a,
                    native int b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      mul
      ret
    } // end of method 'Operator::mul_i32_I'

    .method public hidebysig static native int 
            mul_ovf_I_i32(native int a,
                         int32 b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      mul.ovf
      ret
    } // end of method 'Operator::mul_ovf_I_i32'

    .method public hidebysig static native int 
            mul_ovf_i32_I(int32 a,
                        native int b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      mul.ovf
      ret
    } // end of method 'Operator::mul_ovf_i32_I'

    .method public hidebysig static native int 
            mul_ovf_un_I_i32(native int a,
                            int32 b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      mul.ovf.un
      ret
    } // end of method 'Operator::mul_ovf_un_I_i32'

    .method public hidebysig static native int 
            mul_ovf_un_i32_I(int32 a,
                           native int b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      mul.ovf.un
      ret
    } // end of method 'Operator::mul_ovf_un_i32'

    .method public hidebysig static native int 
            div_I_i32(native int a,
                     int32 b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      div
      ret
    } // end of method 'Operator::div_I_i32'

    .method public hidebysig static native int 
            div_i32_I(int32 a,
                    native int b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      div
      ret
    } // end of method 'Operator::div_i32_I'

    .method public hidebysig static native int 
            div_un_I_i32(native int a,
                        int32 b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      div.un
      ret
    } // end of method 'Operator::div_un_I_i32'

    .method public hidebysig static native int 
            div_un_i32_I(int32 a,
                       native int b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      div.un
      ret
    } // end of method 'Operator::div_un_i32'

    .method public hidebysig static native int 
            rem_I_i32(native int a,
                     int32 b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      rem
      ret
    } // end of method 'Operator::rem_I_i32'

    .method public hidebysig static native int 
            rem_i32_I(int32 a,
                    native int b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      rem
      ret
    } // end of method 'Operator::rem_i32_I'

    .method public hidebysig static native int 
            rem_un_I_i32(native int a,
                        int32 b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      rem.un
      ret
    } // end of method 'Operator::rem_un_I_i32'

    .method public hidebysig static native int 
            rem_un_i32_I(int32 a,
                       native int b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  2
      ldarg.0
      ldarg.1
      rem.un
      ret
    } // end of method 'Operator::rem_un_i32'

    .method public hidebysig static native int 
            and_I_i32(native int a,
                     int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      and
      ret
    } // end of method 'Operator::and_I_i32'

    .method public hidebysig static native int 
            and_i32_I(int32 a,
                    native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      and
      ret
    } // end of method 'Operator::and_i32_I'

    .method public hidebysig static native int 
            or_I_i32(native int a,
                    int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      or
      ret
    } // end of method 'Operator::or_I_i32'

    .method public hidebysig static native int 
            or_i32_I(int32 a,
                   native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      or
      ret
    } // end of method 'Operator::or_i32_I'

    .method public hidebysig static native int 
            xor_I_i32(native int a,
                     int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      xor
      ret
    } // end of method 'Operator::xor_I_i32'

    .method public hidebysig static native int 
            xor_i32_I(int32 a,
                    native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      xor
      ret
    } // end of method 'Operator::xor_i32_I'

    .method public hidebysig static native int 
            ceq_I_i32(native int a,
                     int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      ceq
      ret
    } // end of method 'Operator::ceq_I_i32'

    .method public hidebysig static native int 
            ceq_i32_I(int32 a,
                    native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      ceq
      ret
    } // end of method 'Operator::ceq_i32_I'

    .method public hidebysig static native int 
            cgt_I_i32(native int a,
                     int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      cgt
      ret
    } // end of method 'Operator::cgt_I_i32'

    .method public hidebysig static native int 
            cgt_i32_I(int32 a,
                    native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      cgt
      ret
    } // end of method 'Operator::cgt_i32_I'

    .method public hidebysig static native int 
            cgt_un_I_i32(native int a,
                        int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      cgt.un
      ret
    } // end of method 'Operator::cgt_un_I_i32'

    .method public hidebysig static native int 
            cgt_un_i32_I(int32 a,
                       native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      cgt.un
      ret
    } // end of method 'Operator::cgt_un_i32_I'

    .method public hidebysig static native int 
            clt_I_i32(native int a,
                     int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      clt
      ret
    } // end of method 'Operator::clt_I_i32'

    .method public hidebysig static native int 
            clt_i32_I(int32 a,
                    native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      clt
      ret
    } // end of method 'Operator::clt_i32_I'

    .method public hidebysig static native int 
            clt_un_I_i32(native int a,
                        int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      clt.un
      ret
    } // end of method 'Operator::clt_un_I_i32'

    .method public hidebysig static native int 
            clt_un_i32_I(int32 a,
                       native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      clt.un
      ret
    } // end of method 'Operator::clt_un_i32_I'

    .method public hidebysig static native int 
            beq_I_i32(native int a,
                     int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      beq TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::beq_I_i32'

    .method public hidebysig static native int 
            beq_i32_I(int32 a,
                    native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      beq TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::beq_i32_I'

    .method public hidebysig static native int 
            bne_un_I_i32(native int a,
                        int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      bne.un TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::bne_un_I_i32'

    .method public hidebysig static native int 
            bne_un_i32_I(int32 a,
                       native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      bne.un TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::bne_un_i32_I'

    .method public hidebysig static native int 
            blt_I_i32(native int a,
                     int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      blt TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::blt_I_i32'

    .method public hidebysig static native int 
            blt_i32_I(int32 a,
                    native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      blt TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::blt_i32_I'

    .method public hidebysig static native int 
            blt_un_I_i32(native int a,
                        int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      blt.un TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::blt_un_I_i32'

    .method public hidebysig static native int 
            blt_un_i32_I(int32 a,
                       native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      blt.un TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::blt_un_i32_I'

    .method public hidebysig static native int 
            ble_I_i32(native int a,
                     int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      ble TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::ble_I_i32'

    .method public hidebysig static native int 
            ble_i32_I(int32 a,
                    native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      ble TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::ble_i32_I'

    .method public hidebysig static native int 
            ble_un_I_i32(native int a,
                        int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      ble.un TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::ble_un_I_i32'

    .method public hidebysig static native int 
            ble_un_i32_I(int32 a,
                       native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      ble.un TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::ble_un_i32_I'

    .method public hidebysig static native int 
            bgt_I_i32(native int a,
                     int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      bgt TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::bgt_I_i32'

    .method public hidebysig static native int 
            bgt_i32_I(int32 a,
                    native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      bgt TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::bgt_i32_I'

    .method public hidebysig static native int 
            bgt_un_I_i32(native int a,
                        int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      bgt.un TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::bgt_un_I_i32'

    .method public hidebysig static native int 
            bgt_un_i32_I(int32 a,
                       native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      bgt.un TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::bgt_un_i32_I'

    .method public hidebysig static native int 
            bge_I_i32(native int a,
                     int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      bge TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::bge_I_i32'

    .method public hidebysig static native int 
            bge_i32_I(int32 a,
                    native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      bge TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::bge_i32_I'

    .method public hidebysig static native int 
            bge_un_I_i32(native int a,
                        int32 b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      bge.un TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::bge_un_I_i32'

    .method public hidebysig static native int 
            bge_un_i32_I(int32 a,
                       native int b) il managed
    {
      .maxstack  2
      ldarg.0
      ldarg.1
      bge.un TAKEN
      ldc.i4.0
      ret
      TAKEN:
      ldc.i4.1
      ret
    } // end of method 'Operator::bge_un_i32_I'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Operator::.ctor'

  } // end of class 'Test'

} // end of namespace 'implicit_promotion'

//*********** DISASSEMBLY COMPLETE ***********************
