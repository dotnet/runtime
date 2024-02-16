// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
// this test is a regression case for VSWhidbey 163172
// Class A has method f()
// Class B extends A and explicitly implements A::f() (with B::g()) B::g() is final 
// Class C extends B and implicitly implements A::f()
// Class D extends A and implicitly implements A::f()

// The final on B::g() causd the target slot (A::f()) to be marked final as well and we got 
// TypeLoadException when instantiating C and D



// Metadata version: v1.2.30801
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .hash = (BB F5 50 CF 5C DC 3F F4 03 2C 76 78 5A 79 37 C7   // ..P.\.?..,vxZy7.
           A4 08 4B CE )                                     // ..K.
  .ver 1:2:3400:0
}
.assembly MethodImplFinal
{
  .permissionset reqmin
            "<PermissionSet class=\"System.Security.PermissionSe"
  + "t\"\r\nversion=\"1\">\r\n<IPermission class=\"System.Security.Permis"
  + "sions.SecurityPermission, mscorlib, Version=1.2.3400.0, Cult"
  + "ure=neutral, PublicKeyToken=b77a5c561934e089\"\r\nversion=\"1\"\r\n"
  + "Flags=\"SkipVerification\"/>\r\n</PermissionSet>\r\n"
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module MethodImplFinal.exe


// ================== GLOBAL METHODS =========================

.class public auto ansi MethodImplFinal {
.method public static int32 modopt([mscorlib]System.Runtime.CompilerServices.CallConvCdecl) 
        main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    // Code size       42 (0x2a)
    .maxstack  2
    .locals init (class [mscorlib]System.Exception V_0,
             int32 V_1)
    .try
    {
    	
    	newobj     instance void C::.ctor()
  		pop
  		newobj     instance void D::.ctor()
  		pop
      ldstr      "PASS"
      call       void [System.Console]System.Console::WriteLine(string)
      nop
      ldc.i4.s   100
      stloc.1
      leave.s    IL_0027

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      stloc.0
      ldstr      "FAIL: Caught unexpected exception: "
      ldloc.0
      call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      call       void [System.Console]System.Console::WriteLine(string)
      nop
      ldc.i4.s   101
      stloc.1
      IL_0025:  leave.s    IL_0027

    }  // end handler
    IL_0027:  nop
    ldloc.1
    ret
} // end of global method main
}   

// =============================================================


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi A
       extends [mscorlib]System.Object
{
  .method public newslot virtual instance void 
          f() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  0
    ret
  } // end of method A::f

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // end of method A::.ctor

} // end of class A

.class public auto ansi B
       extends A
{
  .method public newslot virtual final instance void 
          g() cil managed
  {
    .override A::f
    // Code size       1 (0x1)
    .maxstack  0
    ret
  } // end of method B::g

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    ldarg.0
    call       instance void A::.ctor()
    ret
  } // end of method B::.ctor

} // end of class B

.class public auto ansi C
       extends B
{
  .method public virtual instance void  f() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  0
    ret
  } // end of method C::f

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    ldarg.0
    call       instance void B::.ctor()
    ret
  } // end of method C::.ctor

} // end of class C


.class public auto ansi D
       extends A
{
  .method public virtual instance void  f() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  0
    ret
  } // end of method C::f

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    ldarg.0
    call       instance void A::.ctor()
    ret
  } // end of method C::.ctor

} // end of class C


// =============================================================

.data D_00008030 = bytearray (
                 01 00 00 06) 
//*********** DISASSEMBLY COMPLETE ***********************
