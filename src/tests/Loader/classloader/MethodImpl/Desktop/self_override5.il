// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// MyBar is a normal reference type.
// MyBar::DoBarOverride explicitly overrides MyBar::DoBar
// MyFoo is a subclass of MyBar and it has a virtual method DoBarOverride(), which
// overrides MyBar::DoBarOverride(), but not explicitly, that is, it doesn't not involve a MethodImpl.
//
// 4/6: See also VSWhidbey 165006.  This test has been modified from its original form in light of this bug. -mwilk

// 8/28: Even though we invoke MyBar::DoBar virtually and non-virtually in both cases MyBar::DoBarOverride() should be 
// invoked since this is an explicit override and as such MyBar::DoBar()'s method body is substituted by that of 
// MyBar::DoBarOverride().
// MyFoo::DoBarOverride() implicitly overrides MyBar::DoBarOverride() but it doesn't affect MyBar::DoBar() in any way.
// -dmilirud


// 11/15: Per VSW 393898 we've reverted back to the old behavior.
// - dmilirud

.assembly extern mscorlib{}
.assembly extern xunit.core {}
.assembly self_override5{}

.class public MyBar extends [mscorlib]System.Object
{
  .method public virtual newslot instance int32 DoBar()
  {
    IL_0000:  ldstr      "In MyBar::DoBar"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.4
    IL_000f:  ret
  }

  .method public virtual newslot instance int32 DoBarOverride()
  {
    .override MyBar::DoBar
    ldstr      "In MyBar::DoBarOverride"
    call       void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.5
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor()
  {
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }
}

.class public beforefieldinit MyFoo extends MyBar{
  .method public specialname rtspecialname 
          instance void  .ctor()
  {
    IL_0000:  ldarg.0
    IL_0001:  call       instance void MyBar::.ctor()
    IL_0006:  ret
  } 

    .method public virtual instance int32 DoBarOverride(){
        ldstr "In MyFoo::DoBarOverride"
        call       void [mscorlib]System.Console::WriteLine(string)
        ldc.i4.6
        ret
    }

}

.class public beforefieldinit CMain extends [mscorlib]System.Object
{

  .method public hidebysig static int32  Main(string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    .locals init (bool V_0,
             class MyBar V_1,
             class MyFoo V_2,
             int32 V_3)

    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0

    // Invoke mf.DoBar() virtually where b is a MyBar reference
    // and mf is an instance of MyFoo.
    // expect MyFoo::DoBarOverride to execute.

    IL_0002:  newobj     instance void MyFoo::.ctor()
    dup
    IL_0007:  stloc.1
    stloc.2
    IL_0008:  ldc.i4.6
    IL_0009:  ldloc.1
    IL_000a:  callvirt   instance int32 MyBar::DoBar()
    IL_000f:  beq.s      L0

    IL_0011:  ldstr      "FAIL: expected MyFoo::DoBarOverride() to execute, but anoth"
    + "er method was executed instead."
    IL_0016:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_001b:  ldc.i4.0
    IL_001c:  stloc.0

L0:
    // Invoke mb.DoBar() non-virtually where b is a MyBar reference and
    // mb is an instance of MyFoo
    // expect MyBar::DoBarOverride to execute.
    IL_001f:  ldc.i4.5
    IL_0020:  ldloc.1
    call instance int32 MyBar::DoBar()
    beq.s      L1
    ldstr      "FAIL: expected MyBar::DoBarOverride to execute, but ano"
        + "ther method was executed instead."
    call       void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.0
    stloc.0

L1:

    // return a status
    IL_0034:  ldloc.0
    IL_0035:  brtrue.s   IL_003b

    IL_0037:  ldc.i4.s   101
    stloc.3
    ldstr "FAIL"
    call void [mscorlib]System.Console::WriteLine(string)
    IL_0039:  br.s       IL_0040

    IL_003b:  ldc.i4.s   100
    IL_003d:  stloc.3
    ldstr "PASS"
    call void [mscorlib]System.Console::WriteLine(string)
    IL_003e:  br.s       IL_0040

    IL_0040:  ldloc.3
    IL_0041:  ret
  } // end of method CMain::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method CMain::.ctor

} // end of class CMain
