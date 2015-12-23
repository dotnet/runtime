
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly threads3 {}
.module threads3.EXE
.namespace Precise
{
  .class public auto ansi beforefieldinit measure
         extends [mscorlib]System.Object
  {
    .field public static int32 a
    .method private hidebysig specialname rtspecialname static 
            void  .cctor() cil managed
    {
      .maxstack  1
      IL_0000:  ldc.i4.0
      IL_0001:  stsfld     int32 Precise.measure::a
      IL_0006:  ret
    } 
  } 

  .class public auto ansi test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname static 
            void  .cctor() cil managed
    {
      .maxstack  3
      IL_0000:  ldstr      "{0} in .cctor(), measure.a is {1}"
      IL_0005:  call       class [mscorlib]System.Threading.Thread [mscorlib]System.Threading.Thread::get_CurrentThread()
      IL_000a:  callvirt   instance string [mscorlib]System.Threading.Thread::get_Name()
      IL_000f:  ldsfld     int32 Precise.measure::a
      IL_0014:  box        [mscorlib]System.Int32
      IL_0019:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object,
                                                                    object)
      IL_001e:  ldsfld     int32 Precise.measure::a
      IL_0023:  ldc.i4.1
      IL_0024:  add
      IL_0025:  stsfld     int32 Precise.measure::a
      IL_002a:  ldstr      "{0} in .cctor(), after +=8, measure.a is {1}"
      IL_002f:  call       class [mscorlib]System.Threading.Thread [mscorlib]System.Threading.Thread::get_CurrentThread()
      IL_0034:  callvirt   instance string [mscorlib]System.Threading.Thread::get_Name()
      IL_0039:  ldsfld     int32 Precise.measure::a
      IL_003e:  box        [mscorlib]System.Int32
      IL_0043:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object,
                                                                    object)
      IL_0048:  call       void [System.Console]System.Console::WriteLine()
      IL_004d:  ret
    }
  } 

  .class private auto ansi beforefieldinit Driver
         extends [mscorlib]System.Object
  {
    .method public hidebysig static void 
            f() cil managed
    {
      .maxstack  0
      call void Precise.test::.cctor()

      IL_0002:  ret
    } 

    .method public hidebysig static int32 
            Main() cil managed
    {
      .entrypoint
      .maxstack  4
      .locals init (class [mscorlib]System.Threading.Thread[] V_0,
               int32 V_1,
               class [mscorlib]System.Threading.ThreadStart V_2,
               class [mscorlib]System.Threading.Thread V_3,
               class [mscorlib]System.Threading.Thread V_4,
               class [mscorlib]System.Exception V_5,
               int32 V_6,
               class [mscorlib]System.Threading.Thread[] V_7,
               int32 V_8)
      .try
      {
        IL_0000:  ldstr      "Before calling static field"
        IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
        IL_000a:  ldsfld     int32 Precise.measure::a
        IL_000f:  brfalse.s  IL_0037

        IL_0011:  ldstr      "in Main(), measure.a is {0}"
        IL_0016:  ldsfld     int32 Precise.measure::a
        IL_001b:  box        [mscorlib]System.Int32
        IL_0020:  call       void [System.Console]System.Console::WriteLine(string,
                                                                      object)
        IL_0025:  ldstr      "FAILED"
        IL_002a:  call       void [System.Console]System.Console::WriteLine(string)
        IL_002f:  ldc.i4.1
        IL_0030:  stloc.s    V_6
        IL_0032:  leave      IL_0124

        IL_0037:  ldc.i4.5
        IL_0038:  newarr     [mscorlib]System.Threading.Thread
        IL_003d:  stloc.0
        IL_003e:  ldc.i4.0
        IL_003f:  stloc.1
        IL_0040:  br.s       IL_0074

        IL_0042:  ldnull
        IL_0043:  ldftn      void Precise.Driver::f()
        IL_0049:  newobj     instance void [mscorlib]System.Threading.ThreadStart::.ctor(object,
                                                                                         native int)
        IL_004e:  stloc.2
        IL_004f:  ldloc.0
        IL_0050:  ldloc.1
        IL_0051:  ldloc.2
        IL_0052:  newobj     instance void [mscorlib]System.Threading.Thread::.ctor(class [mscorlib]System.Threading.ThreadStart)
        IL_0057:  stelem.ref
        IL_0058:  ldloc.0
        IL_0059:  ldloc.1
        IL_005a:  ldelem.ref
        IL_005b:  ldstr      "Thread #"
        IL_0060:  ldloc.1
        IL_0061:  box        [mscorlib]System.Int32
        IL_0066:  call       string [mscorlib]System.String::Concat(object,
                                                                    object)
        IL_006b:  callvirt   instance void [mscorlib]System.Threading.Thread::set_Name(string)
        IL_0070:  ldloc.1
        IL_0071:  ldc.i4.1
        IL_0072:  add
        IL_0073:  stloc.1
        IL_0074:  ldloc.1
        IL_0075:  ldc.i4.5
        IL_0076:  blt.s      IL_0042

        IL_0078:  ldloc.0
        IL_0079:  stloc.s    V_7
        IL_007b:  ldc.i4.0
        IL_007c:  stloc.s    V_8
        IL_007e:  br.s       IL_0092

        IL_0080:  ldloc.s    V_7
        IL_0082:  ldloc.s    V_8
        IL_0084:  ldelem.ref
        IL_0085:  stloc.3
        IL_0086:  ldloc.3
        IL_0087:  callvirt   instance void [mscorlib]System.Threading.Thread::Start()
        IL_008c:  ldloc.s    V_8
        IL_008e:  ldc.i4.1
        IL_008f:  add
        IL_0090:  stloc.s    V_8
        IL_0092:  ldloc.s    V_8
        IL_0094:  ldloc.s    V_7
        IL_0096:  ldlen
        IL_0097:  conv.i4
        IL_0098:  blt.s      IL_0080

        IL_009a:  ldloc.0
        IL_009b:  stloc.s    V_7
        IL_009d:  ldc.i4.0
        IL_009e:  stloc.s    V_8
        IL_00a0:  br.s       IL_00b6

        IL_00a2:  ldloc.s    V_7
        IL_00a4:  ldloc.s    V_8
        IL_00a6:  ldelem.ref
        IL_00a7:  stloc.s    V_4
        IL_00a9:  ldloc.s    V_4
        IL_00ab:  callvirt   instance void [mscorlib]System.Threading.Thread::Join()
        IL_00b0:  ldloc.s    V_8
        IL_00b2:  ldc.i4.1
        IL_00b3:  add
        IL_00b4:  stloc.s    V_8
        IL_00b6:  ldloc.s    V_8
        IL_00b8:  ldloc.s    V_7
        IL_00ba:  ldlen
        IL_00bb:  conv.i4
        IL_00bc:  blt.s      IL_00a2

        IL_00be:  ldstr      "After calling static field"
        IL_00c3:  call       void [System.Console]System.Console::WriteLine(string)
        IL_00c8:  ldsfld     int32 Precise.measure::a
        IL_00cd:  ldc.i4 6
        IL_00ce:  beq.s      IL_00f3

        IL_00d0:  ldstr      "in Main(), measure.a is {0}"
        IL_00d5:  ldsfld     int32 Precise.measure::a
        IL_00da:  box        [mscorlib]System.Int32
        IL_00df:  call       void [System.Console]System.Console::WriteLine(string,
                                                                      object)
        IL_00e4:  ldstr      "FAILED"
        IL_00e9:  call       void [System.Console]System.Console::WriteLine(string)
        IL_00ee:  ldc.i4.m1
        IL_00ef:  stloc.s    V_6
        IL_00f1:  leave.s    IL_0124

        IL_00f3:  leave.s    IL_010f

      }  
      catch [mscorlib]System.Exception 
      {
        IL_00f5:  stloc.s    V_5
        IL_00f7:  ldloc.s    V_5
        IL_00f9:  call       void [System.Console]System.Console::WriteLine(object)
        IL_00fe:  ldloc.s    V_5
        IL_0100:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
        IL_0105:  call       void [System.Console]System.Console::WriteLine(string)
        IL_010a:  ldc.i4.m1
        IL_010b:  stloc.s    V_6
        IL_010d:  leave.s    IL_0124

      }  
      IL_010f:  call       void [System.Console]System.Console::WriteLine()
      IL_0114:  ldstr      "PASSED"
      IL_0119:  call       void [System.Console]System.Console::WriteLine(string)
      IL_011e:  ldc.i4.s   100
      IL_0120:  stloc.s    V_6
      IL_0122:  br.s       IL_0124

      IL_0124:  ldloc.s    V_6
      IL_0126:  ret
    } 
  } 
} 
