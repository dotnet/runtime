// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }


.assembly extern mscorlib {}

.assembly UntrustedCodeBFI {}


.class public auto ansi beforefieldinit B
       extends [mscorlib]System.Object
{
  .field public static class C c_field
  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       50 (0x32)
    .maxstack  2
    .locals init (class [mscorlib]System.Exception V_0)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  ldstr      "In B.cctor"
      IL_0007:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000c:  nop
      IL_000d:  newobj     instance void C::.ctor()
      IL_0012:  stsfld     class C B::c_field
      IL_0017:  nop
      IL_0018:  leave.s    IL_0030

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_001a:  stloc.0
      IL_001b:  nop
      IL_001c:  ldstr      "PASS: Caught exception : "
      IL_0021:  ldloc.0
      IL_0022:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_0027:  call       void [System.Console]System.Console::WriteLine(string)
      IL_002c:  nop
      IL_002d:  nop
      IL_002e:  leave.s    IL_0030

    }  // end handler
    IL_0030:  nop
    IL_0031:  ret
  } // end of method B::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method B::.ctor

} // end of class B

.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       22 (0x16)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldc.i4.1
    IL_0009:  newobj     instance void [mscorlib]System.Security.Permissions.FileIOPermission::.ctor(valuetype [mscorlib]System.Security.Permissions.PermissionState)
    IL_000e:  call       instance void [mscorlib]System.Security.CodeAccessPermission::Demand()
    IL_0013:  nop
    IL_0014:  nop
    IL_0015:  ret
  } // end of method C::.ctor

} // end of class C

.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       98 (0x62)
    .maxstack  2
    .locals init (class B V_0,
             class A V_1,
             class [mscorlib]System.Exception V_2,
             int32 V_3)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  newobj     instance void B::.ctor()
      IL_0007:  stloc.0
      IL_0008:  newobj     instance void A::.ctor()
      IL_000d:  stloc.1
      IL_000e:  ldloc.1
      IL_000f:  callvirt   instance void A::methodA()
      IL_0014:  nop
      IL_0015:  ldsfld     class C B::c_field
      IL_001a:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_001f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0024:  nop
      IL_0025:  ldstr      "FAIL: Did not catch NullReferenceException expecte"
      + "d exception"
      IL_002a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_002f:  nop
      IL_0030:  ldc.i4.s   101
      IL_0032:  stloc.3
      IL_0033:  leave.s    IL_005f

    }  // end .try
    catch [mscorlib]System.NullReferenceException 
    {
      IL_0035:  pop
      IL_0036:  nop
      IL_0037:  ldstr      "PASS: Caught expected NullReferenceException"
      IL_003c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0041:  nop
      IL_0042:  ldc.i4.s   100
      IL_0044:  stloc.3
      IL_0045:  leave.s    IL_005f

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_0047:  stloc.2
      IL_0048:  nop
      IL_0049:  ldstr      "PASS: Caught exception : "
      IL_004e:  ldloc.2
      IL_004f:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_0054:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0059:  nop
      IL_005a:  ldc.i4.s   100
      IL_005c:  stloc.3
      IL_005d:  leave.s    IL_005f

    }  // end handler
    IL_005f:  nop
    IL_0060:  ldloc.3
    IL_0061:  ret
  } // end of method Test::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test

.class public auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .permissionset deny
             = {[mscorlib]System.Security.Permissions.FileIOPermissionAttribute = {property bool 'Unrestricted' = bool(true)}}
  .method public hidebysig instance void 
          methodA() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  stsfld     class C B::c_field
    IL_0007:  ret
  } // end of method A::methodA

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A::.ctor

} // end of class A


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file UntrustedCodeBFI.res
