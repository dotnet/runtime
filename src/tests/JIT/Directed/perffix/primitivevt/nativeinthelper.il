.assembly extern mscorlib {}
.assembly nativeinthelper {}
.module nativeinthelper.dll
.namespace PrimitiveVT
{
  .class public sequential ansi sealed beforefieldinit VT1A
         extends [mscorlib]System.ValueType
  {
    .field public native int m
  } // end of class VT1A

  .class public sequential ansi sealed beforefieldinit VT1B
         extends [mscorlib]System.ValueType
  {
    .field public native int m
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(native int x) cil managed
    {
      // Code size       8 (0x8)
      .maxstack  2
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  stfld      native int PrimitiveVT.VT1B::m
      IL_0007:  ret
    } // end of method VT1B::.ctor

    .method public hidebysig specialname static 
            native int  op_Implicit(valuetype PrimitiveVT.VT1B x) cil managed
    {
      // Code size       12 (0xc)
      .maxstack  1
      .locals init (native int V_0)
      IL_0000:  ldarga.s   x
      IL_0002:  ldfld      native int PrimitiveVT.VT1B::m
      IL_0007:  stloc.0
      IL_0008:  br.s       IL_000a

      IL_000a:  ldloc.0
      IL_000b:  ret
    } // end of method VT1B::op_Implicit

    .method public hidebysig specialname static 
            valuetype PrimitiveVT.VT1B  op_Implicit(native int x) cil managed
    {
      // Code size       14 (0xe)
      .maxstack  2
      .locals init (valuetype PrimitiveVT.VT1B V_0,
               valuetype PrimitiveVT.VT1B V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldarg.0
      IL_0003:  stfld      native int PrimitiveVT.VT1B::m
      IL_0008:  ldloc.0
      IL_0009:  stloc.1
      IL_000a:  br.s       IL_000c

      IL_000c:  ldloc.1
      IL_000d:  ret
    } // end of method VT1B::op_Implicit

    .method public hidebysig specialname static 
            valuetype PrimitiveVT.VT1A  op_Explicit(valuetype PrimitiveVT.VT1B x) cil managed
    {
      // Code size       20 (0x14)
      .maxstack  2
      .locals init (valuetype PrimitiveVT.VT1A V_0,
               valuetype PrimitiveVT.VT1A V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldarga.s   x
      IL_0004:  ldfld      native int PrimitiveVT.VT1B::m
      IL_0009:  stfld      native int PrimitiveVT.VT1A::m
      IL_000e:  ldloc.0
      IL_000f:  stloc.1
      IL_0010:  br.s       IL_0012

      IL_0012:  ldloc.1
      IL_0013:  ret
    } // end of method VT1B::op_Explicit

    .method public hidebysig specialname static 
            valuetype PrimitiveVT.VT1B  op_Explicit(valuetype PrimitiveVT.VT1A x) cil managed
    {
      // Code size       20 (0x14)
      .maxstack  2
      .locals init (valuetype PrimitiveVT.VT1B V_0,
               valuetype PrimitiveVT.VT1B V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldarga.s   x
      IL_0004:  ldfld      native int PrimitiveVT.VT1A::m
      IL_0009:  stfld      native int PrimitiveVT.VT1B::m
      IL_000e:  ldloc.0
      IL_000f:  stloc.1
      IL_0010:  br.s       IL_0012

      IL_0012:  ldloc.1
      IL_0013:  ret
    } // end of method VT1B::op_Explicit

    .method public hidebysig specialname static 
            native int  op_Addition(valuetype PrimitiveVT.VT1B x,
                               valuetype PrimitiveVT.VT1B y) cil managed
    {
      // Code size       20 (0x14)
      .maxstack  2
      .locals init (native int V_0)
      IL_0000:  ldarga.s   x
      IL_0002:  ldfld      native int PrimitiveVT.VT1B::m
      IL_0007:  ldarga.s   y
      IL_0009:  ldfld      native int PrimitiveVT.VT1B::m
      IL_000e:  add
      IL_000f:  stloc.0
      IL_0010:  br.s       IL_0012

      IL_0012:  ldloc.0
      IL_0013:  ret
    } // end of method VT1B::op_Addition

    .method public hidebysig specialname static 
            valuetype PrimitiveVT.VT1B  op_Addition(valuetype PrimitiveVT.VT1A x,
                                                    valuetype PrimitiveVT.VT1B y) cil managed
    {
      // Code size       28 (0x1c)
      .maxstack  3
      .locals init (valuetype PrimitiveVT.VT1B V_0,
               valuetype PrimitiveVT.VT1B V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldarga.s   x
      IL_0004:  ldfld      native int PrimitiveVT.VT1A::m
      IL_0009:  ldarga.s   y
      IL_000b:  ldfld      native int PrimitiveVT.VT1B::m
      IL_0010:  add
      IL_0011:  stfld      native int PrimitiveVT.VT1B::m
      IL_0016:  ldloc.0
      IL_0017:  stloc.1
      IL_0018:  br.s       IL_001a

      IL_001a:  ldloc.1
      IL_001b:  ret
    } // end of method VT1B::op_Addition

    .method public hidebysig specialname static 
            native int  op_Subtraction(valuetype PrimitiveVT.VT1B x,
                                  valuetype PrimitiveVT.VT1B y) cil managed
    {
      // Code size       20 (0x14)
      .maxstack  2
      .locals init (native int V_0)
      IL_0000:  ldarga.s   x
      IL_0002:  ldfld      native int PrimitiveVT.VT1B::m
      IL_0007:  ldarga.s   y
      IL_0009:  ldfld      native int PrimitiveVT.VT1B::m
      IL_000e:  sub
      IL_000f:  stloc.0
      IL_0010:  br.s       IL_0012

      IL_0012:  ldloc.0
      IL_0013:  ret
    } // end of method VT1B::op_Subtraction

    .method public hidebysig specialname static 
            valuetype PrimitiveVT.VT1B  op_Subtraction(valuetype PrimitiveVT.VT1A x,
                                                       valuetype PrimitiveVT.VT1B y) cil managed
    {
      // Code size       28 (0x1c)
      .maxstack  3
      .locals init (valuetype PrimitiveVT.VT1B V_0,
               valuetype PrimitiveVT.VT1B V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldarga.s   x
      IL_0004:  ldfld      native int PrimitiveVT.VT1A::m
      IL_0009:  ldarga.s   y
      IL_000b:  ldfld      native int PrimitiveVT.VT1B::m
      IL_0010:  sub
      IL_0011:  stfld      native int PrimitiveVT.VT1B::m
      IL_0016:  ldloc.0
      IL_0017:  stloc.1
      IL_0018:  br.s       IL_001a

      IL_001a:  ldloc.1
      IL_001b:  ret
    } // end of method VT1B::op_Subtraction

    .method public hidebysig specialname static 
            native int  op_Multiply(valuetype PrimitiveVT.VT1B x,
                               valuetype PrimitiveVT.VT1A y) cil managed
    {
      // Code size       20 (0x14)
      .maxstack  2
      .locals init (native int V_0)
      IL_0000:  ldarga.s   x
      IL_0002:  ldfld      native int PrimitiveVT.VT1B::m
      IL_0007:  ldarga.s   y
      IL_0009:  ldfld      native int PrimitiveVT.VT1A::m
      IL_000e:  mul
      IL_000f:  stloc.0
      IL_0010:  br.s       IL_0012

      IL_0012:  ldloc.0
      IL_0013:  ret
    } // end of method VT1B::op_Multiply

    .method public hidebysig specialname static 
            native int  op_Multiply(valuetype PrimitiveVT.VT1B x,
                               valuetype PrimitiveVT.VT1B y) cil managed
    {
      // Code size       20 (0x14)
      .maxstack  2
      .locals init (native int V_0)
      IL_0000:  ldarga.s   x
      IL_0002:  ldfld      native int PrimitiveVT.VT1B::m
      IL_0007:  ldarga.s   y
      IL_0009:  ldfld      native int PrimitiveVT.VT1B::m
      IL_000e:  mul
      IL_000f:  stloc.0
      IL_0010:  br.s       IL_0012

      IL_0012:  ldloc.0
      IL_0013:  ret
    } // end of method VT1B::op_Multiply

    .method public hidebysig specialname static 
            valuetype PrimitiveVT.VT1B  op_Multiply(valuetype PrimitiveVT.VT1A x,
                                                    valuetype PrimitiveVT.VT1B y) cil managed
    {
      // Code size       28 (0x1c)
      .maxstack  3
      .locals init (valuetype PrimitiveVT.VT1B V_0,
               valuetype PrimitiveVT.VT1B V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldarga.s   x
      IL_0004:  ldfld      native int PrimitiveVT.VT1A::m
      IL_0009:  ldarga.s   y
      IL_000b:  ldfld      native int PrimitiveVT.VT1B::m
      IL_0010:  mul
      IL_0011:  stfld      native int PrimitiveVT.VT1B::m
      IL_0016:  ldloc.0
      IL_0017:  stloc.1
      IL_0018:  br.s       IL_001a

      IL_001a:  ldloc.1
      IL_001b:  ret
    } // end of method VT1B::op_Multiply

    .method public hidebysig specialname static 
            float32  op_Division(valuetype PrimitiveVT.VT1A x,
                                 valuetype PrimitiveVT.VT1B y) cil managed
    {
      // Code size       21 (0x15)
      .maxstack  2
      .locals init (float32 V_0)
      IL_0000:  ldarga.s   x
      IL_0002:  ldfld      native int PrimitiveVT.VT1A::m
      IL_0007:  ldarga.s   y
      IL_0009:  ldfld      native int PrimitiveVT.VT1B::m
      IL_000e:  div
      IL_000f:  conv.r4
      IL_0010:  stloc.0
      IL_0011:  br.s       IL_0013

      IL_0013:  ldloc.0
      IL_0014:  ret
    } // end of method VT1B::op_Division

    .method public hidebysig specialname static 
            valuetype PrimitiveVT.VT1B  op_Increment(valuetype PrimitiveVT.VT1B x) cil managed
    {
      // Code size       28 (0x1c)
      .maxstack  3
      .locals init (valuetype PrimitiveVT.VT1B V_0,
               native int V_1)
      IL_0000:  ldarga.s   x
      IL_0002:  dup
      IL_0003:  ldfld      native int PrimitiveVT.VT1B::m
      IL_0008:  ldc.i4.1
      IL_0009:  add
      IL_000a:  dup
      IL_000b:  stloc.1
      IL_000c:  stfld      native int PrimitiveVT.VT1B::m
      IL_0011:  ldloc.1
      IL_0012:  call       valuetype PrimitiveVT.VT1B PrimitiveVT.VT1B::op_Implicit(native int)
      IL_0017:  stloc.0
      IL_0018:  br.s       IL_001a

      IL_001a:  ldloc.0
      IL_001b:  ret
    } // end of method VT1B::op_Increment

  } // end of class VT1B

  .class public sequential ansi sealed beforefieldinit VT2A
         extends [mscorlib]System.ValueType
  {
    .field public native int m
  } // end of class VT2A

  .class public sequential ansi sealed beforefieldinit VT2B
         extends [mscorlib]System.ValueType
  {
    .field public native int m
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(native int x) cil managed
    {
      // Code size       8 (0x8)
      .maxstack  2
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  stfld      native int PrimitiveVT.VT2B::m
      IL_0007:  ret
    } // end of method VT2B::.ctor

    .method public hidebysig specialname static 
            native int  op_Implicit(valuetype PrimitiveVT.VT2B x) cil managed
    {
      // Code size       12 (0xc)
      .maxstack  1
      .locals init (native int V_0)
      IL_0000:  ldarga.s   x
      IL_0002:  ldfld      native int PrimitiveVT.VT2B::m
      IL_0007:  stloc.0
      IL_0008:  br.s       IL_000a

      IL_000a:  ldloc.0
      IL_000b:  ret
    } // end of method VT2B::op_Implicit

    .method public hidebysig specialname static 
            valuetype PrimitiveVT.VT2B  op_Implicit(native int x) cil managed
    {
      // Code size       14 (0xe)
      .maxstack  2
      .locals init (valuetype PrimitiveVT.VT2B V_0,
               valuetype PrimitiveVT.VT2B V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldarg.0
      IL_0003:  stfld      native int PrimitiveVT.VT2B::m
      IL_0008:  ldloc.0
      IL_0009:  stloc.1
      IL_000a:  br.s       IL_000c

      IL_000c:  ldloc.1
      IL_000d:  ret
    } // end of method VT2B::op_Implicit

    .method public hidebysig specialname static 
            valuetype PrimitiveVT.VT2A  op_Explicit(valuetype PrimitiveVT.VT2B x) cil managed
    {
      // Code size       20 (0x14)
      .maxstack  2
      .locals init (valuetype PrimitiveVT.VT2A V_0,
               valuetype PrimitiveVT.VT2A V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldarga.s   x
      IL_0004:  ldfld      native int PrimitiveVT.VT2B::m
      IL_0009:  stfld      native int PrimitiveVT.VT2A::m
      IL_000e:  ldloc.0
      IL_000f:  stloc.1
      IL_0010:  br.s       IL_0012

      IL_0012:  ldloc.1
      IL_0013:  ret
    } // end of method VT2B::op_Explicit

    .method public hidebysig specialname static 
            valuetype PrimitiveVT.VT2B  op_Explicit(valuetype PrimitiveVT.VT2A x) cil managed
    {
      // Code size       20 (0x14)
      .maxstack  2
      .locals init (valuetype PrimitiveVT.VT2B V_0,
               valuetype PrimitiveVT.VT2B V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldarga.s   x
      IL_0004:  ldfld      native int PrimitiveVT.VT2A::m
      IL_0009:  stfld      native int PrimitiveVT.VT2B::m
      IL_000e:  ldloc.0
      IL_000f:  stloc.1
      IL_0010:  br.s       IL_0012

      IL_0012:  ldloc.1
      IL_0013:  ret
    } // end of method VT2B::op_Explicit

    .method public hidebysig specialname static 
            native int  op_Addition(valuetype PrimitiveVT.VT2B x,
                                valuetype PrimitiveVT.VT2B y) cil managed
    {
      // Code size       20 (0x14)
      .maxstack  2
      .locals init (native int V_0)
      IL_0000:  ldarga.s   x
      IL_0002:  ldfld      native int PrimitiveVT.VT2B::m
      IL_0007:  ldarga.s   y
      IL_0009:  ldfld      native int PrimitiveVT.VT2B::m
      IL_000e:  add
      IL_000f:  stloc.0
      IL_0010:  br.s       IL_0012

      IL_0012:  ldloc.0
      IL_0013:  ret
    } // end of method VT2B::op_Addition

    .method public hidebysig specialname static 
            valuetype PrimitiveVT.VT2B  op_Addition(valuetype PrimitiveVT.VT2A x,
                                                    valuetype PrimitiveVT.VT2B y) cil managed
    {
      // Code size       28 (0x1c)
      .maxstack  3
      .locals init (valuetype PrimitiveVT.VT2B V_0,
               valuetype PrimitiveVT.VT2B V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldarga.s   x
      IL_0004:  ldfld      native int PrimitiveVT.VT2A::m
      IL_0009:  ldarga.s   y
      IL_000b:  ldfld      native int PrimitiveVT.VT2B::m
      IL_0010:  add
      IL_0011:  stfld      native int PrimitiveVT.VT2B::m
      IL_0016:  ldloc.0
      IL_0017:  stloc.1
      IL_0018:  br.s       IL_001a

      IL_001a:  ldloc.1
      IL_001b:  ret
    } // end of method VT2B::op_Addition

    .method public hidebysig specialname static 
            native int  op_Subtraction(valuetype PrimitiveVT.VT2B x,
                                   valuetype PrimitiveVT.VT2B y) cil managed
    {
      // Code size       20 (0x14)
      .maxstack  2
      .locals init (native int V_0)
      IL_0000:  ldarga.s   x
      IL_0002:  ldfld      native int PrimitiveVT.VT2B::m
      IL_0007:  ldarga.s   y
      IL_0009:  ldfld      native int PrimitiveVT.VT2B::m
      IL_000e:  sub
      IL_000f:  stloc.0
      IL_0010:  br.s       IL_0012

      IL_0012:  ldloc.0
      IL_0013:  ret
    } // end of method VT2B::op_Subtraction

    .method public hidebysig specialname static 
            valuetype PrimitiveVT.VT2B  op_Subtraction(valuetype PrimitiveVT.VT2A x,
                                                       valuetype PrimitiveVT.VT2B y) cil managed
    {
      // Code size       28 (0x1c)
      .maxstack  3
      .locals init (valuetype PrimitiveVT.VT2B V_0,
               valuetype PrimitiveVT.VT2B V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldarga.s   x
      IL_0004:  ldfld      native int PrimitiveVT.VT2A::m
      IL_0009:  ldarga.s   y
      IL_000b:  ldfld      native int PrimitiveVT.VT2B::m
      IL_0010:  sub
      IL_0011:  stfld      native int PrimitiveVT.VT2B::m
      IL_0016:  ldloc.0
      IL_0017:  stloc.1
      IL_0018:  br.s       IL_001a

      IL_001a:  ldloc.1
      IL_001b:  ret
    } // end of method VT2B::op_Subtraction

    .method public hidebysig specialname static 
            native int  op_Multiply(valuetype PrimitiveVT.VT2B x,
                                valuetype PrimitiveVT.VT2A y) cil managed
    {
      // Code size       20 (0x14)
      .maxstack  2
      .locals init (native int V_0)
      IL_0000:  ldarga.s   x
      IL_0002:  ldfld      native int PrimitiveVT.VT2B::m
      IL_0007:  ldarga.s   y
      IL_0009:  ldfld      native int PrimitiveVT.VT2A::m
      IL_000e:  mul
      IL_000f:  stloc.0
      IL_0010:  br.s       IL_0012

      IL_0012:  ldloc.0
      IL_0013:  ret
    } // end of method VT2B::op_Multiply

    .method public hidebysig specialname static 
            native int  op_Multiply(valuetype PrimitiveVT.VT2B x,
                                valuetype PrimitiveVT.VT2B y) cil managed
    {
      // Code size       20 (0x14)
      .maxstack  2
      .locals init (native int V_0)
      IL_0000:  ldarga.s   x
      IL_0002:  ldfld      native int PrimitiveVT.VT2B::m
      IL_0007:  ldarga.s   y
      IL_0009:  ldfld      native int PrimitiveVT.VT2B::m
      IL_000e:  mul
      IL_000f:  stloc.0
      IL_0010:  br.s       IL_0012

      IL_0012:  ldloc.0
      IL_0013:  ret
    } // end of method VT2B::op_Multiply

    .method public hidebysig specialname static 
            valuetype PrimitiveVT.VT2B  op_Multiply(valuetype PrimitiveVT.VT2A x,
                                                    valuetype PrimitiveVT.VT2B y) cil managed
    {
      // Code size       28 (0x1c)
      .maxstack  3
      .locals init (valuetype PrimitiveVT.VT2B V_0,
               valuetype PrimitiveVT.VT2B V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldarga.s   x
      IL_0004:  ldfld      native int PrimitiveVT.VT2A::m
      IL_0009:  ldarga.s   y
      IL_000b:  ldfld      native int PrimitiveVT.VT2B::m
      IL_0010:  mul
      IL_0011:  stfld      native int PrimitiveVT.VT2B::m
      IL_0016:  ldloc.0
      IL_0017:  stloc.1
      IL_0018:  br.s       IL_001a

      IL_001a:  ldloc.1
      IL_001b:  ret
    } // end of method VT2B::op_Multiply

    .method public hidebysig specialname static 
            float32  op_Division(valuetype PrimitiveVT.VT2A x,
                                 valuetype PrimitiveVT.VT2B y) cil managed
    {
      // Code size       22 (0x16)
      .maxstack  2
      .locals init (float32 V_0)
      IL_0000:  ldarga.s   x
      IL_0002:  ldfld      native int PrimitiveVT.VT2A::m
      IL_0007:  ldarga.s   y
      IL_0009:  ldfld      native int PrimitiveVT.VT2B::m
      IL_000e:  div.un
      IL_000f:  conv.r.un
      IL_0010:  conv.r4
      IL_0011:  stloc.0
      IL_0012:  br.s       IL_0014

      IL_0014:  ldloc.0
      IL_0015:  ret
    } // end of method VT2B::op_Division

    .method public hidebysig specialname static 
            valuetype PrimitiveVT.VT2B  op_Decrement(valuetype PrimitiveVT.VT2B x) cil managed
    {
      // Code size       28 (0x1c)
      .maxstack  3
      .locals init (valuetype PrimitiveVT.VT2B V_0,
               native int V_1)
      IL_0000:  ldarga.s   x
      IL_0002:  dup
      IL_0003:  ldfld      native int PrimitiveVT.VT2B::m
      IL_0008:  ldc.i4.1
      IL_0009:  sub
      IL_000a:  dup
      IL_000b:  stloc.1
      IL_000c:  stfld      native int PrimitiveVT.VT2B::m
      IL_0011:  ldloc.1
      IL_0012:  call       valuetype PrimitiveVT.VT2B PrimitiveVT.VT2B::op_Implicit(native int)
      IL_0017:  stloc.0
      IL_0018:  br.s       IL_001a

      IL_001a:  ldloc.0
      IL_001b:  ret
    } // end of method VT2B::op_Decrement

    .method public hidebysig specialname static 
            valuetype PrimitiveVT.VT2B  op_Increment(valuetype PrimitiveVT.VT2B x) cil managed
    {
      // Code size       19 (0x13)
      .maxstack  2
      .locals init (valuetype PrimitiveVT.VT2B V_0)
      IL_0000:  ldarga.s   x
      IL_0002:  ldfld      native int PrimitiveVT.VT2B::m
      IL_0007:  ldc.i4.1
      IL_0008:  add
      IL_0009:  call       valuetype PrimitiveVT.VT2B PrimitiveVT.VT2B::op_Implicit(native int)
      IL_000e:  stloc.0
      IL_000f:  br.s       IL_0011

      IL_0011:  ldloc.0
      IL_0012:  ret
    } // end of method VT2B::op_Increment

  } // end of class VT2B


// =============================================================


} // end of namespace PrimitiveVT
