// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

.assembly extern legacy library mscorlib {}
.assembly mixed3
{
}
.namespace DD
{
  .class public auto ansi ZZ
  {
    .field public static int32 exitcode
    .method public static int32  Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  2

      IL_0000:  ldstr      "Throwing an exception..."
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_000f:  throw

      IL_0010:  leave.s    IL_0025

filter_begin:
				pop
				call       int32 DD.ZZ::Filter()
				endfilter

      IL_0012:  pop
      IL_0013:  ldstr      "In except handler."
      IL_0018:  call       void [System.Console]System.Console::WriteLine(string)
      IL_001d:  ldc.i4.0
      IL_001e:  stsfld     int32 DD.ZZ::exitcode
      IL_0023:  leave.s    IL_0025

      IL_0025:  ldsfld     int32 DD.ZZ::exitcode
      IL_002a:  ldc.i4.0
      IL_002b:  bne.un.s   IL_0041

      IL_002d:  ldstr      "Passed."
      IL_0032:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0037:  ldc.i4	   0x64
      IL_003C:  ret

      IL_0041:  ldstr      "Failed."
      IL_0046:  call       void [System.Console]System.Console::WriteLine(string)
      IL_004B:  ldsfld     int32 DD.ZZ::exitcode
      IL_0050:  ret
      .try IL_0000 to filter_begin filter filter_begin handler IL_0012 to IL_0025
    } // end of method 'ZZ::Main'

    .method public static int32 Filter() il managed
    {
      .maxstack  3
      .locals (int32 V_0,
               int32 V_1,
               int32 V_2,
               int32 V_3,
               class [mscorlib]System.DivideByZeroException V_4,
               class [mscorlib]System.ArgumentException V_5)

      IL_0000:  ldc.i4.0
      IL_0001:  stloc.0
      IL_0002:  ldc.i4     0xc8
      IL_0007:  stloc.1
      IL_0008:  ldc.i4.s   100
      IL_000a:  stloc.2
      IL_000b:  ldloc.1
      IL_000c:  ldc.i4.1
      IL_000d:  sub
      IL_000e:  ldloc.1
      IL_000f:  mul
      IL_0010:  ldc.i4.2
      IL_0011:  div
      IL_0012:  stloc.3
      IL_0013:  br.s       IL_001f

      IL_0015:  ldloc.3
      IL_0016:  ldloc.1
      IL_0017:  sub
      IL_0018:  stloc.3
      IL_001f:  ldloc.1
      IL_0020:  dup
      IL_0021:  ldc.i4.1
      IL_0022:  sub
      IL_0023:  stloc.1
      IL_0024:  ldc.i4.0
      IL_0025:  bgt.s      IL_0015

      IL_0027:  ldloc.2
      IL_0028:  ldloc.3
      IL_0029:  div
      IL_002a:  stloc.2
      IL_002b:  ldloc.1
      IL_002c:  ldloc.2
      IL_002d:  mul
      IL_002e:  stloc.3
      IL_002f:  ldstr      "Something wrong."
      IL_0034:  newobj     instance void [mscorlib]System.ArgumentException::.ctor(string)
      IL_0039:  throw

      IL_003a:  leave.s    IL_0074

      IL_003c:  stloc.s    V_4
      IL_003e:  ldstr      "DivideByZeroException exception caught, Message="
      IL_0043:  call       void [System.Console]System.Console::Write(string)
      IL_0048:  ldloc.s    V_4
      IL_004a:  callvirt   instance string [mscorlib]System.Exception::get_Message()
      IL_004f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0054:  leave.s    IL_0074

      IL_0056:  stloc.s    V_5
      IL_0058:  ldstr      "Application exception caught, Message="
      IL_005d:  call       void [System.Console]System.Console::Write(string)
      IL_0062:  ldloc.s    V_5
      IL_0064:  callvirt   instance string [mscorlib]System.Exception::get_Message()
      IL_0069:  call       void [System.Console]System.Console::WriteLine(string)
      IL_006e:  ldloc.0
      IL_006f:  ldc.i4.1
      IL_0070:  sub
      IL_0071:  stloc.0
      IL_0072:  leave.s    IL_0074

      IL_0074:  leave.s    IL_007b

      IL_0076:  ldloc.0
      IL_0077:  ldc.i4.1
      IL_0078:  add
      IL_0079:  stloc.0
      IL_007a:  endfinally
      IL_007b:  ldloc.0
      IL_007c:  ret
      .try IL_0002 to IL_003c catch [mscorlib]System.DivideByZeroException handler IL_003c to IL_0056
      .try IL_0002 to IL_003c catch [mscorlib]System.ArgumentException handler IL_0056 to IL_0074
      .try IL_0002 to IL_0076 finally handler IL_0076 to IL_007b
    } // end of method 'ZZ::Filter'

    .method public specialname rtspecialname static void .cctor() il managed
    {
      .maxstack  8

      IL_0000:  ldc.i4.1
      IL_0001:  stsfld     int32 DD.ZZ::exitcode
      IL_0006:  ret
    } // end of method 'ZZ::.cctor'

    .method public specialname rtspecialname instance void .ctor() il managed
    {
      .maxstack  8

      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'ZZ::.ctor'

  } // end of class 'ZZ'

} // end of namespace 'DD'

