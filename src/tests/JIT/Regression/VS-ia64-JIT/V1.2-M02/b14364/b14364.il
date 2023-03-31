// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console { auto }
.assembly extern xunit.core {}
.assembly extern System.Runtime.Extensions { auto }
.assembly extern mscorlib { auto }

.assembly 'b14364'
{
  .ver 0:0:0:0
}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001


.class public auto ansi sealed c
       extends [mscorlib]System.Object
{
  .field privatescope int32 a$PST04000001
  .field privatescope int32 b$PST04000002
  .method public specialname rtspecialname
          instance void  .ctor(int32 A_0,
                               int32 A_1) cil managed
  {
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      int32 c::a$PST04000001
    IL_000d:  ldarg.0
    IL_000e:  ldarg.2
    IL_000f:  stfld      int32 c::b$PST04000002
    IL_0014:  ret
  }

}

.class public auto ansi sealed b
       extends [mscorlib]System.Object
{
  .field privatescope class c a$PST04000003
  .field privatescope class b b$PST04000004
  .method public specialname rtspecialname
          instance void  .ctor(class c A_0,
                               class b A_1) cil managed
  {
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      class c b::a$PST04000003
    IL_000d:  ldarg.0
    IL_000e:  ldarg.2
    IL_000f:  stfld      class b b::b$PST04000004
    IL_0014:  ret
  }

}

.class public auto ansi sealed g
       extends [mscorlib]System.Object
{
  .field privatescope string a$PST04000005
  .field privatescope class g b$PST04000006
  .method public specialname rtspecialname
          instance void  .ctor(string A_0,
                               class g A_1) cil managed
  {
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      string g::a$PST04000005
    IL_000d:  ldarg.0
    IL_000e:  ldarg.2
    IL_000f:  stfld      class g g::b$PST04000006
    IL_0014:  ret
  }

}

.class public auto ansi Benchmark
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname static
          void  .cctor() cil managed
  {
    .maxstack  0
    IL_0000:  ret
  }

  .method public static int32  main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  18
    .locals init (class [mscorlib]System.Exception V_0,
             class [mscorlib]System.IO.TextWriter V_1,
             class a V_2,
             class b V_3,
             class a V_4,
             class a V_5,
             int32 V_6,
             int64 V_7,
             int64 V_8,
             class b V_9,
             int32 V_10,
             class d V_11,
             class e V_12,
             class b V_13,
             class f V_14,
             int32 V_15,
             class b V_16,
             class b V_17,
             class b V_18,
             class b V_19,
             class c V_20,
             int32 V_21,
             int32 V_22,
             class b V_23,
             class b V_24,
             class b V_25,
             class b V_26,
             class b V_27,
             class c V_28,
             class b V_29,
             class b V_30,
             class b V_31,
             class b V_32,
             class b V_33,
             class b V_34,
             class b V_35,
             class b V_36,
             class g V_37,
             int32 V_38,
             int64 V_39,
             int64 V_40,
             int64 V_41,
             int64 V_42,
             string V_43,
             string V_44,
             string V_45,
             int64 V_46,
             int64 V_47,
             int64 V_48,
             class [mscorlib]System.Text.StringBuilder V_49,
             string V_50,
             class [mscorlib]System.Text.StringBuilder V_51,
             string V_52)
    IL_0000:  ldsfld     class [mscorlib]System.Exception $::i$PST04000014
    IL_0005:  stloc.0
    IL_0006:  ldsfld     class [mscorlib]System.IO.TextWriter $::j$PST04000015
    IL_000b:  stloc.1
    IL_000c:  ldsfld     class a $::e$PST04000010
    IL_0011:  stloc.2
    IL_0012:  ldsfld     class b $::h$PST04000013
    IL_0017:  stloc.3
    IL_0018:  ldsfld     class a $::d$PST0400000F
    IL_001d:  stloc.s    V_4
    IL_001f:  ldsfld     class a $::c$PST0400000E
    IL_0024:  stloc.s    V_5
    IL_0026:  call       int32 [System.Runtime.Extensions]System.Environment::get_TickCount()
    IL_002b:  stloc.s    V_6
    IL_002d:  ldloc.s    V_6
    IL_002f:  conv.i8
    IL_0030:  stloc.s    V_7
    IL_0032:  ldloc.s    V_7
    IL_0034:  ldc.i8     0x3e8
    IL_003d:  mul
    IL_003e:  stloc.s    V_8
    IL_0040:  ldloc.s    V_8
    IL_0042:  stsfld     int64 $::b$PST0400000D
    IL_0047:  ldloc.3
    IL_0048:  ldc.i4     0x190
    IL_004d:  stloc.s    V_10
    IL_004f:  stloc.s    V_9
    IL_0051:  ldloc.s    V_10
    IL_0053:  brtrue     IL_005d

    IL_0058:  br         IL_0062

    IL_005d:  br         IL_016d

    IL_0062:  ldloc.s    V_4
    IL_0064:  ldloc.s    V_9
    IL_0066:  call       class b $::e(class a,
                                      class b)
    IL_006b:  stloc.s    V_36
    IL_006d:  ldc.i4.0
    IL_006e:  ldc.i4.0
    IL_006f:  ldnull
    IL_0070:  ldloc.s    V_36
    IL_0072:  call       class g $::j(int32,
                                      int32,
                                      string,
                                      class b)
    IL_0077:  stloc.s    V_37
    IL_0079:  ldloc.2
    IL_007a:  ldloc.s    V_37
    IL_007c:  call       void $::b(class a,
                                   class g)
    IL_0081:  call       int32 [System.Runtime.Extensions]System.Environment::get_TickCount()
    IL_0086:  stloc.s    V_38
    IL_0088:  ldloc.s    V_38
    IL_008a:  conv.i8
    IL_008b:  stloc.s    V_39
    IL_008d:  ldloc.s    V_39
    IL_008f:  ldc.i8     0x3e8
    IL_0098:  mul
    IL_0099:  stloc.s    V_40
    IL_009b:  ldsfld     int64 $::b$PST0400000D
    IL_00a0:  stloc.s    V_41
    IL_00a2:  ldloc.s    V_40
    IL_00a4:  ldloc.s    V_41
    IL_00a6:  blt        IL_00b0

    IL_00ab:  br         IL_00b2

    IL_00b0:  ldloc.0
    IL_00b1:  throw

    IL_00b2:  ldloc.s    V_40
    IL_00b4:  ldloc.s    V_41
    IL_00b6:  sub
    IL_00b7:  stloc.s    V_42
    IL_00b9:  ldloc.s    V_42
    IL_00bb:  ldc.i8     0x64
    IL_00c4:  div
    IL_00c5:  stloc.s    V_46
    IL_00c7:  ldloc.s    V_46
    IL_00c9:  ldc.i8     0x5
    IL_00d2:  add
    IL_00d3:  stloc.s    V_47
    IL_00d5:  ldloc.s    V_47
    IL_00d7:  ldc.i8     0x0
    IL_00e0:  blt        IL_00ea

    IL_00e5:  br         IL_00ec

    IL_00ea:  ldloc.0
    IL_00eb:  throw

    IL_00ec:  ldloc.s    V_47
    IL_00ee:  ldc.i8     0xa
    IL_00f7:  div
    IL_00f8:  stloc.s    V_48
    IL_00fa:  ldc.i4.s   20
    IL_00fc:  newobj     instance void [mscorlib]System.Text.StringBuilder::.ctor(int32)
    IL_0101:  stloc.s    V_49
    IL_0103:  ldc.i4.3
    IL_0104:  ldc.i4.0
    IL_0105:  beq        IL_010f

    IL_010a:  br         IL_012c

    IL_010f:  ldloc.s    V_49
    IL_0111:  ldloc.s    V_48
    IL_0113:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::Append(int64)
    IL_0118:  stloc.s    V_51
    IL_011a:  ldloc.s    V_49
    IL_011c:  callvirt   instance string [mscorlib]System.Text.StringBuilder::ToString()
    IL_0121:  stloc.s    V_52
    IL_0123:  ldloc.s    V_52
    IL_0125:  stloc.s    V_43
    IL_0127:  br         IL_0143

    IL_012c:  ldloc.s    V_49
    IL_012e:  ldc.i4.3
    IL_012f:  ldloc.s    V_48
    IL_0131:  call       void $::c(class [mscorlib]System.Text.StringBuilder,
                                   int32,
                                   int64)
    IL_0136:  ldloc.s    V_49
    IL_0138:  callvirt   instance string [mscorlib]System.Text.StringBuilder::ToString()
    IL_013d:  stloc.s    V_50
    IL_013f:  ldloc.s    V_50
    IL_0141:  stloc.s    V_43
    IL_0143:  ldstr      "\nTime: "
    IL_0148:  ldloc.s    V_43
    IL_014a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_014f:  stloc.s    V_44
    IL_0151:  ldloc.s    V_44
    IL_0153:  ldstr      "\n"
    IL_0158:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_015d:  stloc.s    V_45
    IL_015f:  ldloc.1
    IL_0160:  ldloc.s    V_45
    IL_0162:  callvirt   instance void [mscorlib]System.IO.TextWriter::Write(string)
    IL_0167:  ldc.i4     0x64
    IL_016c:  ret

    IL_016d:  ldloc.s    V_9
    IL_016f:  newobj     instance void d::.ctor(class b)
    IL_0174:  stloc.s    V_11
    IL_0176:  ldloc.s    V_11
    IL_0178:  newobj     instance void e::.ctor(class a)
    IL_017d:  stloc.s    V_12
    IL_017f:  ldloc.s    V_12
    IL_0181:  ldloc.s    V_9
    IL_0183:  call       class b $::e(class a,
                                      class b)
    IL_0188:  stloc.s    V_13
    IL_018a:  ldloc.s    V_9
    IL_018c:  newobj     instance void f::.ctor(class b)
    IL_0191:  stloc.s    V_14
    IL_0193:  ldloc.s    V_10
    IL_0195:  ldc.i4.1
    IL_0196:  sub
    IL_0197:  stloc.s    V_15
    IL_0199:  ldnull
    IL_019a:  ldloc.s    V_9
    IL_019c:  stloc.s    V_17
    IL_019e:  stloc.s    V_16
    IL_01a0:  ldloc.s    V_17
    IL_01a2:  brtrue     IL_01ac

    IL_01a7:  br         IL_01b9

    IL_01ac:  ldloc.s    V_17
    IL_01ae:  stloc.s    V_35
    IL_01b0:  ldloc.s    V_35
    IL_01b2:  stloc.s    V_18
    IL_01b4:  br         IL_01e9

    IL_01b9:  ldloc.s    V_5
    IL_01bb:  ldnull
    IL_01bc:  ldnull
    IL_01bd:  ldnull
    IL_01be:  ldnull
    IL_01bf:  ldloc.s    V_16
    IL_01c1:  call       class b $::n(class a,
                                      class b,
                                      class b,
                                      class b,
                                      class b,
                                      class b)
    IL_01c6:  stloc.s    V_32
    IL_01c8:  ldloc.s    V_13
    IL_01ca:  ldloc.s    V_32
    IL_01cc:  call       class b $::a(class b,
                                      class b)
    IL_01d1:  stloc.s    V_33
    IL_01d3:  ldloc.s    V_33
    IL_01d5:  call       class b $::h(class b)
    IL_01da:  stloc.s    V_34
    IL_01dc:  ldloc.s    V_34
    IL_01de:  ldloc.s    V_15
    IL_01e0:  stloc.s    V_10
    IL_01e2:  stloc.s    V_9
    IL_01e4:  br         IL_0051

    IL_01e9:  ldloc.s    V_18
    IL_01eb:  ldfld      class b b::b$PST04000004
    IL_01f0:  stloc.s    V_19
    IL_01f2:  ldloc.s    V_18
    IL_01f4:  ldfld      class c b::a$PST04000003
    IL_01f9:  stloc.s    V_20
    IL_01fb:  ldloc.s    V_20
    IL_01fd:  ldfld      int32 c::b$PST04000002
    IL_0202:  stloc.s    V_21
    IL_0204:  ldloc.s    V_20
    IL_0206:  ldfld      int32 c::a$PST04000001
    IL_020b:  stloc.s    V_22
    IL_020d:  ldloc.s    V_22
    IL_020f:  ldloc.s    V_21
    IL_0211:  call       class b $::i(int32,
                                      int32)
    IL_0216:  stloc.s    V_23
    IL_0218:  ldloc.s    V_14
    IL_021a:  ldloc.s    V_23
    IL_021c:  call       class b $::e(class a,
                                      class b)
    IL_0221:  stloc.s    V_24
    IL_0223:  ldloc.s    V_16
    IL_0225:  ldloc.s    V_24
    IL_0227:  stloc.s    V_26
    IL_0229:  stloc.s    V_25
    IL_022b:  ldloc.s    V_26
    IL_022d:  brtrue     IL_023f

    IL_0232:  ldloc.s    V_25
    IL_0234:  ldloc.s    V_19
    IL_0236:  stloc.s    V_17
    IL_0238:  stloc.s    V_16
    IL_023a:  br         IL_01a0

    IL_023f:  ldloc.s    V_26
    IL_0241:  stloc.s    V_31
    IL_0243:  ldloc.s    V_31
    IL_0245:  stloc.s    V_27
    IL_0247:  ldloc.s    V_27
    IL_0249:  ldfld      class c b::a$PST04000003
    IL_024e:  stloc.s    V_28
    IL_0250:  ldloc.s    V_27
    IL_0252:  ldfld      class b b::b$PST04000004
    IL_0257:  stloc.s    V_29
    IL_0259:  ldloc.s    V_28
    IL_025b:  ldloc.s    V_25
    IL_025d:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0262:  stloc.s    V_30
    IL_0264:  ldloc.s    V_30
    IL_0266:  ldloc.s    V_29
    IL_0268:  stloc.s    V_26
    IL_026a:  stloc.s    V_25
    IL_026c:  br         IL_022b
  }

}

.class public auto ansi a
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }

  .method public virtual instance int32  a(class c A_0,
                                           class c A_1) cil managed
  {
    .maxstack  1
    IL_0000:  ldnull
    IL_0001:  throw
  }

  .method public virtual instance int32  b(class c A_0) cil managed
  {
    .maxstack  1
    IL_0000:  ldnull
    IL_0001:  throw
  }

  .method public virtual instance class c
          c(class c A_0) cil managed
  {
    .maxstack  1
    IL_0000:  ldnull
    IL_0001:  throw
  }

  .method public virtual instance void  d(string A_0) cil managed
  {
    .maxstack  1
    IL_0000:  ldnull
    IL_0001:  throw
  }

}

.class public auto ansi sealed d
       extends a
{
  .field privatescope class b a$PST04000007
  .method public specialname rtspecialname
          instance void  .ctor(class b A_0) cil managed
  {
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  call       instance void a::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      class b d::a$PST04000007
    IL_000d:  ret
  }

  .method public virtual final instance int32
          b(class c A_0) cil managed
  {
    .maxstack  5
    .locals init (class a V_0,
             class b V_1)
    IL_0000:  ldsfld     class a $::c$PST0400000E
    IL_0005:  stloc.0
    IL_0006:  ldarg.0
    IL_0007:  ldfld      class b d::a$PST04000007
    IL_000c:  stloc.1
    IL_000d:  ldloc.0
    IL_000e:  ldloc.1
    IL_000f:  ldarg.1
    IL_0010:  tail.
    IL_0012:  call       int32 $::f(class a,
                                    class b,
                                    class c)
    IL_0017:  ret
  }

}

.class public auto ansi sealed e
       extends a
{
  .field privatescope class a a$PST04000008
  .method public specialname rtspecialname
          instance void  .ctor(class a A_0) cil managed
  {
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  call       instance void a::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      class a e::a$PST04000008
    IL_000d:  ret
  }

  .method public virtual final instance int32
          b(class c A_0) cil managed
  {
    .maxstack  5
    .locals init (class a V_0,
             int32 V_1,
             int32 V_2,
             class b V_3,
             class b V_4,
             int32 V_5,
             class b V_6,
             class b V_7,
             class b V_8,
             int32 V_9,
             class b V_10)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class a e::a$PST04000008
    IL_0006:  stloc.0
    IL_0007:  ldarg.1
    IL_0008:  ldfld      int32 c::b$PST04000002
    IL_000d:  stloc.1
    IL_000e:  ldarg.1
    IL_000f:  ldfld      int32 c::a$PST04000001
    IL_0014:  stloc.2
    IL_0015:  ldloc.2
    IL_0016:  ldloc.1
    IL_0017:  call       class b $::i(int32,
                                      int32)
    IL_001c:  stloc.3
    IL_001d:  ldloc.0
    IL_001e:  ldloc.3
    IL_001f:  call       class b $::e(class a,
                                      class b)
    IL_0024:  stloc.s    V_4
    IL_0026:  ldc.i4.0
    IL_0027:  ldloc.s    V_4
    IL_0029:  stloc.s    V_6
    IL_002b:  stloc.s    V_5
    IL_002d:  ldloc.s    V_6
    IL_002f:  brtrue     IL_0039

    IL_0034:  br         IL_0046

    IL_0039:  ldloc.s    V_6
    IL_003b:  stloc.s    V_10
    IL_003d:  ldloc.s    V_10
    IL_003f:  stloc.s    V_7
    IL_0041:  br         IL_005b

    IL_0046:  ldloc.s    V_5
    IL_0048:  ldc.i4.2
    IL_0049:  beq        IL_0053

    IL_004e:  br         IL_0055

    IL_0053:  ldc.i4.1
    IL_0054:  ret

    IL_0055:  ldloc.s    V_5
    IL_0057:  ldc.i4.3
    IL_0058:  ceq
    IL_005a:  ret

    IL_005b:  ldloc.s    V_7
    IL_005d:  ldfld      class b b::b$PST04000004
    IL_0062:  stloc.s    V_8
    IL_0064:  ldloc.s    V_5
    IL_0066:  ldc.i4.1
    IL_0067:  add
    IL_0068:  stloc.s    V_9
    IL_006a:  ldloc.s    V_9
    IL_006c:  ldloc.s    V_8
    IL_006e:  stloc.s    V_6
    IL_0070:  stloc.s    V_5
    IL_0072:  br         IL_002d
  }

}

.class public auto ansi sealed f
       extends a
{
  .field privatescope class b a$PST04000009
  .method public specialname rtspecialname
          instance void  .ctor(class b A_0) cil managed
  {
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  call       instance void a::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      class b f::a$PST04000009
    IL_000d:  ret
  }

  .method public virtual final instance int32
          b(class c A_0) cil managed
  {
    .maxstack  5
    .locals init (class a V_0,
             class b V_1,
             int32 V_2)
    IL_0000:  ldsfld     class a $::c$PST0400000E
    IL_0005:  stloc.0
    IL_0006:  ldarg.0
    IL_0007:  ldfld      class b f::a$PST04000009
    IL_000c:  stloc.1
    IL_000d:  ldloc.0
    IL_000e:  ldloc.1
    IL_000f:  ldarg.1
    IL_0010:  call       int32 $::f(class a,
                                    class b,
                                    class c)
    IL_0015:  stloc.2
    IL_0016:  ldloc.2
    IL_0017:  ldc.i4.1
    IL_0018:  bne.un     IL_0022

    IL_001d:  br         IL_0024

    IL_0022:  ldc.i4.1
    IL_0023:  ret

    IL_0024:  ldc.i4.0
    IL_0025:  ret
  }

}

.class public auto ansi $
       extends [mscorlib]System.Object
{
  .field static privatescope string $l$PST0400000A
  .field static privatescope int32 $g$PST0400000B
  .field static privatescope int32 a$PST0400000C
  .field static privatescope int64 b$PST0400000D
  .field static privatescope class a c$PST0400000E
  .field static privatescope class a d$PST0400000F
  .field static privatescope class a e$PST04000010
  .field static privatescope class [mscorlib]System.IO.TextWriter f$PST04000011
  .field static privatescope class [mscorlib]System.Exception g$PST04000012
  .field static privatescope class b h$PST04000013
  .field static privatescope class [mscorlib]System.Exception i$PST04000014
  .field static privatescope class [mscorlib]System.IO.TextWriter j$PST04000015
  .field static privatescope class [mscorlib]System.IO.TextWriter k$PST04000016
  .field static privatescope class [mscorlib]System.Exception l$PST04000017
  .field static privatescope class b m$PST04000018
  .method public specialname rtspecialname static
          void  .cctor() cil managed
  {
    .maxstack  12
    .locals init (class l V_0,
             class [mscorlib]System.Exception V_1,
             class l V_2,
             class l V_3,
             class c V_4,
             class c V_5,
             class c V_6,
             class c V_7,
             class c V_8,
             class b V_9,
             class b V_10,
             class b V_11,
             class b V_12,
             class b V_13,
             class c V_14,
             class c V_15,
             class b V_16,
             class b V_17,
             class b V_18,
             class b V_19,
             class c V_20,
             class b V_21,
             class c V_22,
             class b V_23,
             class c V_24,
             class c V_25,
             class b V_26,
             class b V_27,
             class b V_28,
             class b V_29,
             class b V_30,
             class b V_31,
             class c V_32,
             class b V_33,
             class b V_34,
             class b V_35,
             class b V_36,
             class c V_37,
             class c V_38,
             class c V_39,
             class c V_40,
             class c V_41,
             class c V_42,
             class c V_43,
             class c V_44,
             class c V_45,
             class c V_46,
             class c V_47,
             class c V_48,
             class c V_49,
             class c V_50,
             class c V_51,
             class c V_52,
             class c V_53,
             class c V_54,
             class c V_55,
             class c V_56,
             class c V_57,
             class c V_58,
             class c V_59,
             class c V_60,
             class c V_61,
             class c V_62,
             class c V_63,
             class c V_64,
             class c V_65,
             class c V_66,
             class c V_67,
             class c V_68,
             class c V_69,
             class c V_70,
             class c V_71,
             class c V_72,
             class c V_73,
             class c V_74,
             class c V_75,
             class c V_76,
             class c V_77,
             class c V_78,
             class c V_79,
             class c V_80,
             class b V_81,
             class b V_82,
             class b V_83,
             class b V_84,
             class b V_85,
             class b V_86,
             class b V_87,
             class b V_88,
             class b V_89,
             class b V_90,
             class b V_91,
             class b V_92,
             class b V_93,
             class b V_94,
             class b V_95,
             class b V_96,
             class b V_97,
             class b V_98,
             class b V_99,
             class b V_100,
             class b V_101,
             class b V_102,
             class b V_103,
             class b V_104,
             class b V_105,
             class b V_106,
             class b V_107,
             class b V_108,
             class b V_109,
             class b V_110,
             class b V_111,
             class b V_112,
             class b V_113,
             class b V_114,
             class b V_115,
             class b V_116,
             class b V_117,
             class b V_118,
             class b V_119,
             class b V_120,
             class b V_121,
             class b V_122,
             class b V_123,
             class b V_124,
             class b V_125,
             class [mscorlib]System.IO.TextWriter V_126,
             class [mscorlib]System.IO.TextWriter V_127,
             class l V_128,
             class [mscorlib]System.Exception V_129,
             class [mscorlib]System.IO.TextWriter V_130,
             int32 V_131,
             class [mscorlib]System.Exception V_132)
ldstr "Here1"
call	void [System.Console]System.Console::WriteLine(string)
    IL_0000:  newobj     instance void l::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  stsfld     class a $::c$PST0400000E
    IL_000c:  ldc.i8     0x0
    IL_0015:  stsfld     int64 $::b$PST0400000D
ldstr "Here2"
call	void [System.Console]System.Console::WriteLine(string)
    IL_001a:  ldstr      "repeat<0"
    IL_001f:  newobj     instance void m::.ctor(string)
    IL_0024:  stloc.1
    IL_0025:  ldloc.1
    IL_0026:  stsfld     class [mscorlib]System.Exception $::g$PST04000012
    IL_002b:  newobj     instance void l::.ctor()
    IL_0030:  stloc.2
    IL_0031:  ldloc.2
    IL_0032:  stsfld     class a $::d$PST0400000F
    IL_0037:  newobj     instance void l::.ctor()
    IL_003c:  stloc.3
ldstr "Here3"
call	void [System.Console]System.Console::WriteLine(string)
    IL_003d:  ldc.i4.0
    IL_003e:  ldc.i4.0
    IL_003f:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0044:  stloc.s    V_4
    IL_0046:  ldc.i4.0
    IL_0047:  ldc.i4.2
    IL_0048:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_004d:  stloc.s    V_5
    IL_004f:  ldc.i4.1
    IL_0050:  ldc.i4.1
    IL_0051:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0056:  stloc.s    V_6
    IL_0058:  ldc.i4.1
    IL_0059:  ldc.i4.2
    IL_005a:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_005f:  stloc.s    V_7
    IL_0061:  ldc.i4.2
    IL_0062:  ldc.i4.1
    IL_0063:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0068:  stloc.s    V_8
    IL_006a:  ldloc.s    V_8
    IL_006c:  ldnull
    IL_006d:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0072:  stloc.s    V_9
    IL_0074:  ldloc.s    V_7
    IL_0076:  ldloc.s    V_9
    IL_0078:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_007d:  stloc.s    V_10
    IL_007f:  ldloc.s    V_6
    IL_0081:  ldloc.s    V_10
    IL_0083:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0088:  stloc.s    V_11
    IL_008a:  ldloc.s    V_5
    IL_008c:  ldloc.s    V_11
    IL_008e:  newobj     instance void b::.ctor(class c,
                                                class b)
ldstr "Here3.5"
call	void [System.Console]System.Console::WriteLine(string)
    IL_0093:  stloc.s    V_12
    IL_0095:  ldloc.s    V_4
    IL_0097:  ldloc.s    V_12
    IL_0099:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_009e:  stloc.s    V_13
    IL_00a0:  ldc.i4.0
    IL_00a1:  ldc.i4.1
    IL_00a2:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_00a7:  stloc.s    V_14
    IL_00a9:  ldc.i4.1
    IL_00aa:  ldc.i4.0
    IL_00ab:  newobj     instance void c::.ctor(int32,
                                                int32)
ldstr "Here3.6"
call	void [System.Console]System.Console::WriteLine(string)
    IL_00b0:  stloc.s    V_15
    IL_00b2:  ldloc.s    V_6
    IL_00b4:  ldnull
    IL_00b5:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_00ba:  stloc.s    V_16
    IL_00bc:  ldloc.s    V_15
    IL_00be:  ldloc.s    V_16
    IL_00c0:  newobj     instance void b::.ctor(class c,
                                                class b)
ldstr "Here3.7"
call	void [System.Console]System.Console::WriteLine(string)
    IL_00c5:  stloc.s    V_17
    IL_00c7:  ldloc.s    V_14
    IL_00c9:  ldloc.s    V_17
    IL_00cb:  newobj     instance void b::.ctor(class c,
                                                class b)
ldstr "Here3.8"
call	void [System.Console]System.Console::WriteLine(string)
    IL_00d0:  stloc.s    V_18
    IL_00d2:  ldloc.s    V_4
    IL_00d4:  ldloc.s    V_18
    IL_00d6:  newobj     instance void b::.ctor(class c,
                                                class b)
ldstr "Here3.9"
call	void [System.Console]System.Console::WriteLine(string)
    IL_00db:  stloc.s    V_19
    IL_00dd:  ldc.i4.2
    IL_00de:  ldc.i4.2
    IL_00df:  newobj     instance void c::.ctor(int32,
                                                int32)
ldstr "Here3.91"
call	void [System.Console]System.Console::WriteLine(string)
    IL_00e4:  stloc.s    V_20
    IL_00e6:  ldloc.s    V_13
dup
call	void [System.Console]System.Console::WriteLine(object)
    IL_00e8:  ldloc.s    V_20
dup
call	void [System.Console]System.Console::WriteLine(object)
    IL_00ea:  call       class b $::l(class b,
                                      class c)
ldstr "Here4"
call	void [System.Console]System.Console::WriteLine(string)
    IL_00ef:  stloc.s    V_21
    IL_00f1:  ldc.i4.2
    IL_00f2:  ldc.i4.s   12
    IL_00f4:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_00f9:  stloc.s    V_22
    IL_00fb:  ldloc.s    V_19
    IL_00fd:  ldloc.s    V_22
    IL_00ff:  call       class b $::l(class b,
                                      class c)
    IL_0104:  stloc.s    V_23
    IL_0106:  ldc.i4.7
    IL_0107:  ldc.i4.8
    IL_0108:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_010d:  stloc.s    V_24
    IL_010f:  ldc.i4.8
    IL_0110:  ldc.i4.8
    IL_0111:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0116:  stloc.s    V_25
    IL_0118:  ldloc.s    V_25
    IL_011a:  ldnull
    IL_011b:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0120:  stloc.s    V_26
    IL_0122:  ldloc.s    V_24
    IL_0124:  ldloc.s    V_26
    IL_0126:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_012b:  stloc.s    V_27
    IL_012d:  ldloc.s    V_27
    IL_012f:  stsfld     class b $::m$PST04000018
    IL_0134:  ldc.i4.0
    IL_0135:  call       class b $::m(int32)
    IL_013a:  stloc.s    V_28
    IL_013c:  ldloc.s    V_15
    IL_013e:  ldloc.s    V_28
    IL_0140:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0145:  stloc.s    V_29
    IL_0147:  ldloc.s    V_4
    IL_0149:  ldloc.s    V_29
    IL_014b:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0150:  stloc.s    V_30
    IL_0152:  ldloc.3
    IL_0153:  ldloc.s    V_30
    IL_0155:  call       class b $::d(class a,
                                      class b)
    IL_015a:  stloc.s    V_31
    IL_015c:  ldc.i4.5
    IL_015d:  ldc.i4.s   20
    IL_015f:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0164:  stloc.s    V_32
    IL_0166:  ldloc.s    V_31
    IL_0168:  ldloc.s    V_32
    IL_016a:  call       class b $::l(class b,
                                      class c)
    IL_016f:  stloc.s    V_33
    IL_0171:  ldloc.s    V_23
    IL_0173:  ldloc.s    V_33
    IL_0175:  call       class b $::a(class b,
                                      class b)
    IL_017a:  stloc.s    V_34
    IL_017c:  ldloc.s    V_21
    IL_017e:  ldloc.s    V_34
    IL_0180:  call       class b $::a(class b,
                                      class b)
    IL_0185:  stloc.s    V_35
    IL_0187:  ldloc.s    V_35
    IL_0189:  call       class b $::h(class b)
    IL_018e:  stloc.s    V_36
    IL_0190:  ldc.i4.2
    IL_0191:  ldc.i4.s   20
    IL_0193:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0198:  stloc.s    V_37
    IL_019a:  ldc.i4.3
    IL_019b:  ldc.i4.s   19
    IL_019d:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_01a2:  stloc.s    V_38
    IL_01a4:  ldc.i4.3
    IL_01a5:  ldc.i4.s   21
    IL_01a7:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_01ac:  stloc.s    V_39
    IL_01ae:  ldc.i4.4
    IL_01af:  ldc.i4.s   18
    IL_01b1:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_01b6:  stloc.s    V_40
    IL_01b8:  ldc.i4.4
    IL_01b9:  ldc.i4.s   22
    IL_01bb:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_01c0:  stloc.s    V_41
    IL_01c2:  ldc.i4.4
    IL_01c3:  ldc.i4.s   23
    IL_01c5:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_01ca:  stloc.s    V_42
    IL_01cc:  ldc.i4.4
    IL_01cd:  ldc.i4.s   32
    IL_01cf:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_01d4:  stloc.s    V_43
    IL_01d6:  ldc.i4.5
    IL_01d7:  ldc.i4.7
    IL_01d8:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_01dd:  stloc.s    V_44
    IL_01df:  ldc.i4.5
    IL_01e0:  ldc.i4.8
    IL_01e1:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_01e6:  stloc.s    V_45
    IL_01e8:  ldc.i4.5
    IL_01e9:  ldc.i4.s   18
    IL_01eb:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_01f0:  stloc.s    V_46
    IL_01f2:  ldc.i4.5
    IL_01f3:  ldc.i4.s   22
    IL_01f5:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_01fa:  stloc.s    V_47
    IL_01fc:  ldc.i4.5
    IL_01fd:  ldc.i4.s   23
    IL_01ff:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0204:  stloc.s    V_48
    IL_0206:  ldc.i4.5
    IL_0207:  ldc.i4.s   29
    IL_0209:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_020e:  stloc.s    V_49
    IL_0210:  ldc.i4.5
    IL_0211:  ldc.i4.s   30
    IL_0213:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0218:  stloc.s    V_50
    IL_021a:  ldc.i4.5
    IL_021b:  ldc.i4.s   31
    IL_021d:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0222:  stloc.s    V_51
    IL_0224:  ldc.i4.5
    IL_0225:  ldc.i4.s   32
    IL_0227:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_022c:  stloc.s    V_52
    IL_022e:  ldc.i4.5
    IL_022f:  ldc.i4.s   36
    IL_0231:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0236:  stloc.s    V_53
    IL_0238:  ldc.i4.6
    IL_0239:  ldc.i4.7
    IL_023a:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_023f:  stloc.s    V_54
    IL_0241:  ldc.i4.6
    IL_0242:  ldc.i4.8
    IL_0243:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0248:  stloc.s    V_55
    IL_024a:  ldc.i4.6
    IL_024b:  ldc.i4.s   18
    IL_024d:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0252:  stloc.s    V_56
    IL_0254:  ldc.i4.6
    IL_0255:  ldc.i4.s   22
    IL_0257:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_025c:  stloc.s    V_57
    IL_025e:  ldc.i4.6
    IL_025f:  ldc.i4.s   23
    IL_0261:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0266:  stloc.s    V_58
    IL_0268:  ldc.i4.6
    IL_0269:  ldc.i4.s   28
    IL_026b:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0270:  stloc.s    V_59
    IL_0272:  ldc.i4.6
    IL_0273:  ldc.i4.s   29
    IL_0275:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_027a:  stloc.s    V_60
    IL_027c:  ldc.i4.6
    IL_027d:  ldc.i4.s   30
    IL_027f:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0284:  stloc.s    V_61
    IL_0286:  ldc.i4.6
    IL_0287:  ldc.i4.s   31
    IL_0289:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_028e:  stloc.s    V_62
    IL_0290:  ldc.i4.6
    IL_0291:  ldc.i4.s   36
    IL_0293:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0298:  stloc.s    V_63
    IL_029a:  ldc.i4.7
    IL_029b:  ldc.i4.s   19
    IL_029d:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_02a2:  stloc.s    V_64
    IL_02a4:  ldc.i4.7
    IL_02a5:  ldc.i4.s   21
    IL_02a7:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_02ac:  stloc.s    V_65
    IL_02ae:  ldc.i4.7
    IL_02af:  ldc.i4.s   28
    IL_02b1:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_02b6:  stloc.s    V_66
    IL_02b8:  ldc.i4.7
    IL_02b9:  ldc.i4.s   31
    IL_02bb:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_02c0:  stloc.s    V_67
    IL_02c2:  ldc.i4.7
    IL_02c3:  ldc.i4.s   40
    IL_02c5:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_02ca:  stloc.s    V_68
    IL_02cc:  ldc.i4.7
    IL_02cd:  ldc.i4.s   41
    IL_02cf:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_02d4:  stloc.s    V_69
    IL_02d6:  ldc.i4.8
    IL_02d7:  ldc.i4.s   20
    IL_02d9:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_02de:  stloc.s    V_70
    IL_02e0:  ldc.i4.8
    IL_02e1:  ldc.i4.s   28
    IL_02e3:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_02e8:  stloc.s    V_71
    IL_02ea:  ldc.i4.8
    IL_02eb:  ldc.i4.s   29
    IL_02ed:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_02f2:  stloc.s    V_72
    IL_02f4:  ldc.i4.8
    IL_02f5:  ldc.i4.s   30
    IL_02f7:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_02fc:  stloc.s    V_73
    IL_02fe:  ldc.i4.8
    IL_02ff:  ldc.i4.s   31
    IL_0301:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0306:  stloc.s    V_74
    IL_0308:  ldc.i4.8
    IL_0309:  ldc.i4.s   40
    IL_030b:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0310:  stloc.s    V_75
    IL_0312:  ldc.i4.8
    IL_0313:  ldc.i4.s   41
    IL_0315:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_031a:  stloc.s    V_76
    IL_031c:  ldc.i4.s   9
    IL_031e:  ldc.i4.s   29
    IL_0320:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0325:  stloc.s    V_77
    IL_0327:  ldc.i4.s   9
    IL_0329:  ldc.i4.s   30
    IL_032b:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0330:  stloc.s    V_78
    IL_0332:  ldc.i4.s   9
    IL_0334:  ldc.i4.s   31
    IL_0336:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_033b:  stloc.s    V_79
    IL_033d:  ldc.i4.s   9
    IL_033f:  ldc.i4.s   32
    IL_0341:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0346:  stloc.s    V_80
    IL_0348:  ldloc.s    V_80
    IL_034a:  ldnull
    IL_034b:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0350:  stloc.s    V_81
    IL_0352:  ldloc.s    V_79
    IL_0354:  ldloc.s    V_81
    IL_0356:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_035b:  stloc.s    V_82
    IL_035d:  ldloc.s    V_78
    IL_035f:  ldloc.s    V_82
    IL_0361:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0366:  stloc.s    V_83
    IL_0368:  ldloc.s    V_77
    IL_036a:  ldloc.s    V_83
    IL_036c:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0371:  stloc.s    V_84
    IL_0373:  ldloc.s    V_76
    IL_0375:  ldloc.s    V_84
    IL_0377:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_037c:  stloc.s    V_85
    IL_037e:  ldloc.s    V_75
    IL_0380:  ldloc.s    V_85
    IL_0382:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0387:  stloc.s    V_86
    IL_0389:  ldloc.s    V_74
    IL_038b:  ldloc.s    V_86
    IL_038d:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0392:  stloc.s    V_87
    IL_0394:  ldloc.s    V_73
    IL_0396:  ldloc.s    V_87
    IL_0398:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_039d:  stloc.s    V_88
    IL_039f:  ldloc.s    V_72
    IL_03a1:  ldloc.s    V_88
    IL_03a3:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_03a8:  stloc.s    V_89
    IL_03aa:  ldloc.s    V_71
    IL_03ac:  ldloc.s    V_89
    IL_03ae:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_03b3:  stloc.s    V_90
    IL_03b5:  ldloc.s    V_70
    IL_03b7:  ldloc.s    V_90
    IL_03b9:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_03be:  stloc.s    V_91
    IL_03c0:  ldloc.s    V_69
    IL_03c2:  ldloc.s    V_91
    IL_03c4:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_03c9:  stloc.s    V_92
    IL_03cb:  ldloc.s    V_68
    IL_03cd:  ldloc.s    V_92
    IL_03cf:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_03d4:  stloc.s    V_93
    IL_03d6:  ldloc.s    V_67
    IL_03d8:  ldloc.s    V_93
    IL_03da:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_03df:  stloc.s    V_94
    IL_03e1:  ldloc.s    V_66
    IL_03e3:  ldloc.s    V_94
    IL_03e5:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_03ea:  stloc.s    V_95
    IL_03ec:  ldloc.s    V_65
    IL_03ee:  ldloc.s    V_95
    IL_03f0:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_03f5:  stloc.s    V_96
    IL_03f7:  ldloc.s    V_64
    IL_03f9:  ldloc.s    V_96
    IL_03fb:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0400:  stloc.s    V_97
    IL_0402:  ldloc.s    V_63
    IL_0404:  ldloc.s    V_97
    IL_0406:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_040b:  stloc.s    V_98
    IL_040d:  ldloc.s    V_62
    IL_040f:  ldloc.s    V_98
    IL_0411:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0416:  stloc.s    V_99
    IL_0418:  ldloc.s    V_61
    IL_041a:  ldloc.s    V_99
    IL_041c:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0421:  stloc.s    V_100
    IL_0423:  ldloc.s    V_60
    IL_0425:  ldloc.s    V_100
    IL_0427:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_042c:  stloc.s    V_101
    IL_042e:  ldloc.s    V_59
    IL_0430:  ldloc.s    V_101
    IL_0432:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0437:  stloc.s    V_102
    IL_0439:  ldloc.s    V_58
    IL_043b:  ldloc.s    V_102
    IL_043d:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0442:  stloc.s    V_103
    IL_0444:  ldloc.s    V_57
    IL_0446:  ldloc.s    V_103
    IL_0448:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_044d:  stloc.s    V_104
    IL_044f:  ldloc.s    V_56
    IL_0451:  ldloc.s    V_104
    IL_0453:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0458:  stloc.s    V_105
    IL_045a:  ldloc.s    V_55
    IL_045c:  ldloc.s    V_105
    IL_045e:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0463:  stloc.s    V_106
    IL_0465:  ldloc.s    V_54
    IL_0467:  ldloc.s    V_106
    IL_0469:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_046e:  stloc.s    V_107
    IL_0470:  ldloc.s    V_53
    IL_0472:  ldloc.s    V_107
    IL_0474:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0479:  stloc.s    V_108
    IL_047b:  ldloc.s    V_52
    IL_047d:  ldloc.s    V_108
    IL_047f:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0484:  stloc.s    V_109
    IL_0486:  ldloc.s    V_51
    IL_0488:  ldloc.s    V_109
    IL_048a:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_048f:  stloc.s    V_110
    IL_0491:  ldloc.s    V_50
    IL_0493:  ldloc.s    V_110
    IL_0495:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_049a:  stloc.s    V_111
    IL_049c:  ldloc.s    V_49
    IL_049e:  ldloc.s    V_111
    IL_04a0:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_04a5:  stloc.s    V_112
    IL_04a7:  ldloc.s    V_48
    IL_04a9:  ldloc.s    V_112
    IL_04ab:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_04b0:  stloc.s    V_113
    IL_04b2:  ldloc.s    V_47
    IL_04b4:  ldloc.s    V_113
    IL_04b6:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_04bb:  stloc.s    V_114
    IL_04bd:  ldloc.s    V_46
    IL_04bf:  ldloc.s    V_114
    IL_04c1:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_04c6:  stloc.s    V_115
    IL_04c8:  ldloc.s    V_45
    IL_04ca:  ldloc.s    V_115
    IL_04cc:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_04d1:  stloc.s    V_116
    IL_04d3:  ldloc.s    V_44
    IL_04d5:  ldloc.s    V_116
    IL_04d7:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_04dc:  stloc.s    V_117
    IL_04de:  ldloc.s    V_43
    IL_04e0:  ldloc.s    V_117
    IL_04e2:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_04e7:  stloc.s    V_118
    IL_04e9:  ldloc.s    V_42
    IL_04eb:  ldloc.s    V_118
    IL_04ed:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_04f2:  stloc.s    V_119
    IL_04f4:  ldloc.s    V_41
    IL_04f6:  ldloc.s    V_119
    IL_04f8:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_04fd:  stloc.s    V_120
    IL_04ff:  ldloc.s    V_40
    IL_0501:  ldloc.s    V_120
    IL_0503:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0508:  stloc.s    V_121
    IL_050a:  ldloc.s    V_39
    IL_050c:  ldloc.s    V_121
    IL_050e:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0513:  stloc.s    V_122
    IL_0515:  ldloc.s    V_38
    IL_0517:  ldloc.s    V_122
    IL_0519:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_051e:  stloc.s    V_123
    IL_0520:  ldloc.s    V_37
    IL_0522:  ldloc.s    V_123
    IL_0524:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0529:  stloc.s    V_124
    IL_052b:  ldloc.s    V_124
    IL_052d:  call       class b $::h(class b)
    IL_0532:  stloc.s    V_125
    IL_0534:  ldloc.s    V_125
    IL_0536:  stsfld     class b $::h$PST04000013
    IL_053b:  call       class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
    IL_0540:  stloc.s    V_126
    IL_0542:  ldloc.s    V_126
    IL_0544:  stsfld     class [mscorlib]System.IO.TextWriter $::f$PST04000011
    IL_0549:  call       class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
    IL_054e:  stloc.s    V_127
    IL_0550:  ldloc.s    V_127
    IL_0552:  stsfld     class [mscorlib]System.IO.TextWriter $::k$PST04000016
    IL_0557:  newobj     instance void l::.ctor()
    IL_055c:  stloc.s    V_128
    IL_055e:  ldloc.s    V_128
    IL_0560:  stsfld     class a $::e$PST04000010
    IL_0565:  newobj     instance void n::.ctor()
    IL_056a:  stloc.s    V_129
    IL_056c:  ldloc.s    V_129
    IL_056e:  stsfld     class [mscorlib]System.Exception $::i$PST04000014
    IL_0573:  call       class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
    IL_0578:  stloc.s    V_130
    IL_057a:  ldloc.s    V_130
    IL_057c:  stsfld     class [mscorlib]System.IO.TextWriter $::j$PST04000015
    IL_0581:  ldc.i4     0x30
    IL_0586:  conv.i4
    IL_0587:  stloc.s    V_131
    IL_0589:  ldloc.s    V_131
    IL_058b:  stsfld     int32 $::a$PST0400000C
    IL_0590:  newobj     instance void o::.ctor()
    IL_0595:  stloc.s    V_132
    IL_0597:  ldloc.s    V_132
    IL_0599:  stsfld     class [mscorlib]System.Exception $::l$PST04000017
    IL_059e:  ret
  }

  .method public static class b  a(class b A_0,
                                   class b A_1) cil managed
  {
    .maxstack  4
    .locals init (class b V_0,
             class b V_1,
             class c V_2,
             class b V_3,
             class b V_4,
             class b V_5)
    IL_0000:  ldarg.0
    IL_0001:  brtrue     IL_000b

    IL_0006:  br         IL_0016

    IL_000b:  ldarg.0
    IL_000c:  stloc.s    V_5
    IL_000e:  ldloc.s    V_5
    IL_0010:  stloc.0
    IL_0011:  br         IL_0018

    IL_0016:  ldarg.1
    IL_0017:  ret

    IL_0018:  ldloc.0
    IL_0019:  ldfld      class b b::b$PST04000004
    IL_001e:  stloc.1
    IL_001f:  ldloc.0
    IL_0020:  ldfld      class c b::a$PST04000003
    IL_0025:  stloc.2
    IL_0026:  ldloc.1
    IL_0027:  ldarg.1
    IL_0028:  call       class b $::a(class b,
                                      class b)
    IL_002d:  stloc.3
    IL_002e:  ldloc.2
    IL_002f:  ldloc.3
    IL_0030:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0035:  stloc.s    V_4
    IL_0037:  ldloc.s    V_4
    IL_0039:  ret
  }

  .method public static void  b(class a A_0,
                                class g A_1) cil managed
  {
    .maxstack  3
    .locals init (class g V_0,
             class g V_1,
             string V_2,
             class g V_3)
    IL_0000:  ldarg.1
    IL_0001:  brtrue     IL_000b

    IL_0006:  br         IL_0014

    IL_000b:  ldarg.1
    IL_000c:  stloc.3
    IL_000d:  ldloc.3
    IL_000e:  stloc.0
    IL_000f:  br         IL_0015

    IL_0014:  ret

    IL_0015:  ldloc.0
    IL_0016:  ldfld      class g g::b$PST04000006
    IL_001b:  stloc.1
    IL_001c:  ldloc.0
    IL_001d:  ldfld      string g::a$PST04000005
    IL_0022:  stloc.2
    IL_0023:  ldarg.0
    IL_0024:  ldloc.2
    IL_0025:  callvirt   instance void a::d(string)
    IL_002a:  ldarg.0
    IL_002b:  ldloc.1
    IL_002c:  tail.
    IL_002e:  call       void $::b(class a,
                                   class g)
    IL_0033:  ret
  }

  .method public static void  c(class [mscorlib]System.Text.StringBuilder A_0,
                                int32 A_1,
                                int64 A_2) cil managed
  {
    .maxstack  8
    .locals init (class [mscorlib]System.Exception V_0,
             int32 V_1,
             int64 V_2,
             int64 V_3,
             int32 V_4,
             int32 V_5,
             int32 V_6,
             char V_7,
             class [mscorlib]System.Text.StringBuilder V_8,
             class [mscorlib]System.Text.StringBuilder V_9,
             class [mscorlib]System.Text.StringBuilder V_10)
    IL_0000:  ldsfld     class [mscorlib]System.Exception $::l$PST04000017
    IL_0005:  stloc.0
    IL_0006:  ldsfld     int32 $::a$PST0400000C
    IL_000b:  stloc.1
    IL_000c:  ldarg.2
    IL_000d:  ldc.i8     0xa
    IL_0016:  rem
    IL_0017:  stloc.2
    IL_0018:  ldarg.2
    IL_0019:  ldc.i8     0xa
    IL_0022:  div
    IL_0023:  stloc.3
    IL_0024:  ldarg.1
    IL_0025:  ldc.i4.1
    IL_0026:  sub
    IL_0027:  stloc.s    V_4
    IL_0029:  ldloc.s    V_4
    IL_002b:  ldc.i4.0
    IL_002c:  beq        IL_0036

    IL_0031:  br         IL_0051

    IL_0036:  ldarg.0
    IL_0037:  ldloc.3
    IL_0038:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::Append(int64)
    IL_003d:  stloc.s    V_9
    IL_003f:  ldarg.0
    IL_0040:  ldc.i4     0x2e
    IL_0045:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::Append(char)
    IL_004a:  stloc.s    V_10
    IL_004c:  br         IL_005a

    IL_0051:  ldarg.0
    IL_0052:  ldloc.s    V_4
    IL_0054:  ldloc.3
    IL_0055:  call       void $::c(class [mscorlib]System.Text.StringBuilder,
                                   int32,
                                   int64)
    IL_005a:  ldloc.2
    IL_005b:  conv.i4
    IL_005c:  stloc.s    V_5
    IL_005e:  ldloc.s    V_5
    IL_0060:  ldloc.1
    IL_0061:  add
    IL_0062:  stloc.s    V_6
    IL_0064:  ldloc.s    V_6
    IL_0066:  ldc.i4.0
    IL_0067:  blt        IL_0071

    IL_006c:  br         IL_0073

    IL_0071:  ldloc.0
    IL_0072:  throw

    IL_0073:  ldloc.s    V_6
    IL_0075:  ldc.i4     0xffff
    IL_007a:  bgt        IL_0084

    IL_007f:  br         IL_0086

    IL_0084:  ldloc.0
    IL_0085:  throw

    IL_0086:  ldloc.s    V_6
    IL_0088:  conv.i4
    IL_0089:  stloc.s    V_7
    IL_008b:  ldarg.0
    IL_008c:  ldloc.s    V_7
    IL_008e:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::Append(char)
    IL_0093:  stloc.s    V_8
    IL_0095:  ret
  }

  .method public static class b  d(class a A_0,
                                   class b A_1) cil managed
  {
    .maxstack  5
    .locals init (class b V_0,
             class b V_1,
             class c V_2,
             class c V_3,
             class b V_4,
             class b V_5,
             class b V_6)
    IL_0000:  ldarg.1
dup
call	void [System.Console]System.Console::WriteLine(object)
ldstr "really1 - above is the second param to $::d after call"
call	void [System.Console]System.Console::WriteLine(string)
    IL_0001:  brtrue     IL_000b

    IL_0006:  br         IL_0016

    IL_000b:  ldarg.1
    IL_000c:  stloc.s    V_6
    IL_000e:  ldloc.s    V_6
    IL_0010:  stloc.0
    IL_0011:  br         IL_0018

    IL_0016:  ldnull
    IL_0017:  ret

    IL_0018:  ldloc.0
dup
call	void [System.Console]System.Console::WriteLine(object)

ldstr "really2"
call	void [System.Console]System.Console::WriteLine(string)
    IL_0019:  ldfld      class b b::b$PST04000004
dup
call	void [System.Console]System.Console::WriteLine(object)
    IL_001e:  stloc.1
    IL_001f:  ldloc.0
    IL_0020:  ldfld      class c b::a$PST04000003
dup
call	void [System.Console]System.Console::WriteLine(object)
    IL_0025:  stloc.2
    IL_0026:  ldarg.0
dup
call	void [System.Console]System.Console::WriteLine(object)
    IL_0027:  ldloc.2
dup
call	void [System.Console]System.Console::WriteLine(object)
    IL_0028:  callvirt   instance class c a::c(class c)
ldstr "really3"
call	void [System.Console]System.Console::WriteLine(string)
    IL_002d:  stloc.3
    IL_002e:  ldarg.0
    IL_002f:  ldloc.1
ldstr "really4"
call	void [System.Console]System.Console::WriteLine(string)
    IL_0030:  call       class b $::d(class a,
                                      class b)
    IL_0035:  stloc.s    V_4
    IL_0037:  ldloc.3
    IL_0038:  ldloc.s    V_4
ldstr "really4"
call	void [System.Console]System.Console::WriteLine(string)
    IL_003a:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_003f:  stloc.s    V_5
    IL_0041:  ldloc.s    V_5
ldstr "really5"
call	void [System.Console]System.Console::WriteLine(string)
    IL_0043:  ret
  }

  .method public static class b  e(class a A_0,
                                   class b A_1) cil managed
  {
    .maxstack  4
    .locals init (class b V_0,
             class b V_1,
             class c V_2,
             class b V_3,
             int32 V_4,
             class b V_5,
             class b V_6,
             class b V_7,
             class b V_8,
             class b V_9,
             class c V_10,
             class b V_11,
             class b V_12,
             class b V_13)
    IL_0000:  ldnull
    IL_0001:  stloc.0
    IL_0002:  ldarg.1
    IL_0003:  brtrue     IL_000d

    IL_0008:  br         IL_0018

    IL_000d:  ldarg.1
    IL_000e:  stloc.s    V_13
    IL_0010:  ldloc.s    V_13
    IL_0012:  stloc.1
    IL_0013:  br         IL_0064

    IL_0018:  ldloc.0
    IL_0019:  ldnull
    IL_001a:  stloc.s    V_7
    IL_001c:  stloc.s    V_6
    IL_001e:  ldloc.s    V_6
    IL_0020:  brtrue     IL_002a

    IL_0025:  br         IL_0037

    IL_002a:  ldloc.s    V_6
    IL_002c:  stloc.s    V_12
    IL_002e:  ldloc.s    V_12
    IL_0030:  stloc.s    V_8
    IL_0032:  br         IL_003a

    IL_0037:  ldloc.s    V_7
    IL_0039:  ret

    IL_003a:  ldloc.s    V_8
    IL_003c:  ldfld      class b b::b$PST04000004
    IL_0041:  stloc.s    V_9
    IL_0043:  ldloc.s    V_8
    IL_0045:  ldfld      class c b::a$PST04000003
    IL_004a:  stloc.s    V_10
    IL_004c:  ldloc.s    V_10
    IL_004e:  ldloc.s    V_7
    IL_0050:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0055:  stloc.s    V_11
    IL_0057:  ldloc.s    V_9
    IL_0059:  ldloc.s    V_11
    IL_005b:  stloc.s    V_7
    IL_005d:  stloc.s    V_6
    IL_005f:  br         IL_001e

    IL_0064:  ldloc.1
    IL_0065:  ldfld      class c b::a$PST04000003
    IL_006a:  stloc.2
    IL_006b:  ldloc.1
    IL_006c:  ldfld      class b b::b$PST04000004
    IL_0071:  stloc.3
    IL_0072:  ldarg.0
    IL_0073:  ldloc.2
    IL_0074:  callvirt   instance int32 a::b(class c)
    IL_0079:  stloc.s    V_4
    IL_007b:  ldloc.s    V_4
    IL_007d:  brtrue     IL_008c

    IL_0082:  ldloc.0
    IL_0083:  ldloc.3
    IL_0084:  starg.s    A_1
    IL_0086:  stloc.0
    IL_0087:  br         IL_0002

    IL_008c:  ldloc.2
    IL_008d:  ldloc.0
    IL_008e:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0093:  stloc.s    V_5
    IL_0095:  ldloc.s    V_5
    IL_0097:  ldloc.3
    IL_0098:  starg.s    A_1
    IL_009a:  stloc.0
    IL_009b:  br         IL_0002
  }

  .method public static int32  f(class a A_0,
                                 class b A_1,
                                 class c A_2) cil managed
  {
    .maxstack  4
    .locals init (class b V_0,
             class c V_1,
             int32 V_2,
             class b V_3,
             class b V_4)
    IL_0000:  ldarg.1
    IL_0001:  brtrue     IL_000b

    IL_0006:  br         IL_0016

    IL_000b:  ldarg.1
    IL_000c:  stloc.s    V_4
    IL_000e:  ldloc.s    V_4
    IL_0010:  stloc.0
    IL_0011:  br         IL_0018

    IL_0016:  ldc.i4.0
    IL_0017:  ret

    IL_0018:  ldloc.0
    IL_0019:  ldfld      class c b::a$PST04000003
    IL_001e:  stloc.1
    IL_001f:  ldarg.0
    IL_0020:  ldarg.2
    IL_0021:  ldloc.1
    IL_0022:  callvirt   instance int32 a::a(class c,
                                             class c)
    IL_0027:  stloc.2
    IL_0028:  ldloc.2
    IL_0029:  brtrue     IL_0033

    IL_002e:  br         IL_0035

    IL_0033:  ldc.i4.1
    IL_0034:  ret

    IL_0035:  ldloc.0
    IL_0036:  ldfld      class b b::b$PST04000004
    IL_003b:  stloc.3
    IL_003c:  ldloc.3
    IL_003d:  starg.s    A_1
    IL_003f:  br         IL_0000
  }

  .method public static int32  g(int32 A_0,
                                 int32 A_1,
                                 class c A_2) cil managed
  {
    .maxstack  4
    .locals init (int32 V_0,
             int32 V_1)
    IL_0000:  ldarg.2
    IL_0001:  ldfld      int32 c::a$PST04000001
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldarg.0
    IL_0009:  blt        IL_0013

    IL_000e:  br         IL_0015

    IL_0013:  ldc.i4.1
    IL_0014:  ret

    IL_0015:  ldloc.0
    IL_0016:  ldarg.0
    IL_0017:  beq        IL_0021

    IL_001c:  br         IL_002d

    IL_0021:  ldarg.2
    IL_0022:  ldfld      int32 c::b$PST04000002
    IL_0027:  stloc.1
    IL_0028:  ldloc.1
    IL_0029:  ldarg.1
    IL_002a:  clt
    IL_002c:  ret

    IL_002d:  ldc.i4.0
    IL_002e:  ret
  }

  .method public static class b  h(class b A_0) cil managed
  {
    .maxstack  8
    .locals init (class b V_0,
             class b V_1,
             class c V_2,
             int32 V_3,
             int32 V_4,
             class j V_5,
             class b V_6,
             class b V_7,
             class b V_8,
             class i V_9,
             class b V_10,
             class b V_11,
             class b V_12,
             class b V_13)
    IL_0000:  ldarg.0
    IL_0001:  brtrue     IL_000b

    IL_0006:  br         IL_0016

    IL_000b:  ldarg.0
    IL_000c:  stloc.s    V_13
    IL_000e:  ldloc.s    V_13
    IL_0010:  stloc.0
    IL_0011:  br         IL_0018

    IL_0016:  ldnull
    IL_0017:  ret

    IL_0018:  ldloc.0
    IL_0019:  ldfld      class b b::b$PST04000004
    IL_001e:  stloc.1
    IL_001f:  ldloc.0
    IL_0020:  ldfld      class c b::a$PST04000003
    IL_0025:  stloc.2
    IL_0026:  ldloc.2
    IL_0027:  ldfld      int32 c::b$PST04000002
    IL_002c:  stloc.3
    IL_002d:  ldloc.2
    IL_002e:  ldfld      int32 c::a$PST04000001
    IL_0033:  stloc.s    V_4
    IL_0035:  ldloc.3
    IL_0036:  ldloc.s    V_4
    IL_0038:  newobj     instance void j::.ctor(int32,
                                                int32)
    IL_003d:  stloc.s    V_5
    IL_003f:  ldloc.s    V_5
    IL_0041:  ldloc.1
    IL_0042:  call       class b $::e(class a,
                                      class b)
    IL_0047:  stloc.s    V_6
    IL_0049:  ldloc.s    V_6
    IL_004b:  call       class b $::h(class b)
    IL_0050:  stloc.s    V_7
    IL_0052:  ldloc.2
    IL_0053:  ldnull
    IL_0054:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0059:  stloc.s    V_8
    IL_005b:  ldloc.2
    IL_005c:  newobj     instance void i::.ctor(class c)
    IL_0061:  stloc.s    V_9
    IL_0063:  ldloc.s    V_9
    IL_0065:  ldloc.1
    IL_0066:  call       class b $::e(class a,
                                      class b)
    IL_006b:  stloc.s    V_10
    IL_006d:  ldloc.s    V_10
    IL_006f:  call       class b $::h(class b)
    IL_0074:  stloc.s    V_11
    IL_0076:  ldloc.s    V_8
    IL_0078:  ldloc.s    V_11
    IL_007a:  call       class b $::a(class b,
                                      class b)
    IL_007f:  stloc.s    V_12
    IL_0081:  ldloc.s    V_7
    IL_0083:  ldloc.s    V_12
    IL_0085:  tail.
    IL_0087:  call       class b $::a(class b,
                                      class b)
    IL_008c:  ret
  }

  .method public static class b  i(int32 A_0,
                                   int32 A_1) cil managed
  {
    .maxstack  4
    .locals init (int32 V_0,
             int32 V_1,
             class c V_2,
             int32 V_3,
             class c V_4,
             int32 V_5,
             int32 V_6,
             class c V_7,
             int32 V_8,
             class c V_9,
             int32 V_10,
             class c V_11,
             int32 V_12,
             int32 V_13,
             class c V_14,
             int32 V_15,
             class c V_16,
             int32 V_17,
             int32 V_18,
             class c V_19,
             class b V_20,
             class b V_21,
             class b V_22,
             class b V_23,
             class b V_24,
             class b V_25,
             class b V_26,
             class b V_27)
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  sub
    IL_0003:  stloc.0
    IL_0004:  ldarg.1
    IL_0005:  ldc.i4.1
    IL_0006:  sub
    IL_0007:  stloc.1
    IL_0008:  ldloc.0
    IL_0009:  ldloc.1
    IL_000a:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_000f:  stloc.2
    IL_0010:  ldarg.0
    IL_0011:  ldc.i4.1
    IL_0012:  sub
    IL_0013:  stloc.3
    IL_0014:  ldloc.3
    IL_0015:  ldarg.1
    IL_0016:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_001b:  stloc.s    V_4
    IL_001d:  ldarg.0
    IL_001e:  ldc.i4.1
    IL_001f:  sub
    IL_0020:  stloc.s    V_5
    IL_0022:  ldarg.1
    IL_0023:  ldc.i4.1
    IL_0024:  add
    IL_0025:  stloc.s    V_6
    IL_0027:  ldloc.s    V_5
    IL_0029:  ldloc.s    V_6
    IL_002b:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0030:  stloc.s    V_7
    IL_0032:  ldarg.1
    IL_0033:  ldc.i4.1
    IL_0034:  sub
    IL_0035:  stloc.s    V_8
    IL_0037:  ldarg.0
    IL_0038:  ldloc.s    V_8
    IL_003a:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_003f:  stloc.s    V_9
    IL_0041:  ldarg.1
    IL_0042:  ldc.i4.1
    IL_0043:  add
    IL_0044:  stloc.s    V_10
    IL_0046:  ldarg.0
    IL_0047:  ldloc.s    V_10
    IL_0049:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_004e:  stloc.s    V_11
    IL_0050:  ldarg.0
    IL_0051:  ldc.i4.1
    IL_0052:  add
    IL_0053:  stloc.s    V_12
    IL_0055:  ldarg.1
    IL_0056:  ldc.i4.1
    IL_0057:  sub
    IL_0058:  stloc.s    V_13
    IL_005a:  ldloc.s    V_12
    IL_005c:  ldloc.s    V_13
    IL_005e:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0063:  stloc.s    V_14
    IL_0065:  ldarg.0
    IL_0066:  ldc.i4.1
    IL_0067:  add
    IL_0068:  stloc.s    V_15
    IL_006a:  ldloc.s    V_15
    IL_006c:  ldarg.1
    IL_006d:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0072:  stloc.s    V_16
    IL_0074:  ldarg.0
    IL_0075:  ldc.i4.1
    IL_0076:  add
    IL_0077:  stloc.s    V_17
    IL_0079:  ldarg.1
    IL_007a:  ldc.i4.1
    IL_007b:  add
    IL_007c:  stloc.s    V_18
    IL_007e:  ldloc.s    V_17
    IL_0080:  ldloc.s    V_18
    IL_0082:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0087:  stloc.s    V_19
    IL_0089:  ldloc.s    V_19
    IL_008b:  ldnull
    IL_008c:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0091:  stloc.s    V_20
    IL_0093:  ldloc.s    V_16
    IL_0095:  ldloc.s    V_20
    IL_0097:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_009c:  stloc.s    V_21
    IL_009e:  ldloc.s    V_14
    IL_00a0:  ldloc.s    V_21
    IL_00a2:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_00a7:  stloc.s    V_22
    IL_00a9:  ldloc.s    V_11
    IL_00ab:  ldloc.s    V_22
    IL_00ad:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_00b2:  stloc.s    V_23
    IL_00b4:  ldloc.s    V_9
    IL_00b6:  ldloc.s    V_23
    IL_00b8:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_00bd:  stloc.s    V_24
    IL_00bf:  ldloc.s    V_7
    IL_00c1:  ldloc.s    V_24
    IL_00c3:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_00c8:  stloc.s    V_25
    IL_00ca:  ldloc.s    V_4
    IL_00cc:  ldloc.s    V_25
    IL_00ce:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_00d3:  stloc.s    V_26
    IL_00d5:  ldloc.2
    IL_00d6:  ldloc.s    V_26
    IL_00d8:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_00dd:  stloc.s    V_27
    IL_00df:  ldloc.s    V_27
    IL_00e1:  ret
  }

  .method public static class g  j(int32 A_0,
                                   int32 A_1,
                                   string A_2,
                                   class b A_3) cil managed
  {
    .maxstack  9
    .locals init (class [mscorlib]System.Exception V_0,
             class g V_1,
             class b V_2,
             class b V_3,
             class c V_4,
             int32 V_5,
             int32 V_6,
             int32 V_7,
             class c V_8,
             class b V_9,
             class g V_10,
             class g V_11,
             int32 V_12,
             class g V_13,
             string V_14,
             int32 V_15,
             string V_16,
             string V_17,
             class g V_18,
             class g V_19,
             class g V_20,
             class g V_21,
             class g V_22,
             string V_23,
             class [mscorlib]System.Text.StringBuilder V_24,
             class g V_25,
             class g V_26,
             class g V_27,
             string V_28,
             class [mscorlib]System.Text.StringBuilder V_29,
             string V_30,
             class g V_31,
             string V_32,
             string V_33,
             string V_34,
             class g V_35,
             string V_36,
             class g V_37,
             class g V_38,
             class b V_39)
    IL_0000:  ldsfld     class [mscorlib]System.Exception $::g$PST04000012
    IL_0005:  stloc.0
    IL_0006:  ldarg.3
    IL_0007:  brtrue     IL_0011

    IL_000c:  br         IL_01c3

    IL_0011:  ldarg.3
    IL_0012:  stloc.s    V_39
    IL_0014:  ldloc.s    V_39
    IL_0016:  stloc.2
    IL_0017:  ldloc.2
    IL_0018:  ldfld      class b b::b$PST04000004
    IL_001d:  stloc.3
    IL_001e:  ldloc.2
    IL_001f:  ldfld      class c b::a$PST04000003
    IL_0024:  stloc.s    V_4
    IL_0026:  ldloc.s    V_4
    IL_0028:  ldfld      int32 c::b$PST04000002
    IL_002d:  stloc.s    V_5
    IL_002f:  ldloc.s    V_4
    IL_0031:  ldfld      int32 c::a$PST04000001
    IL_0036:  stloc.s    V_6
    IL_0038:  ldarg.0
    IL_0039:  ldloc.s    V_6
    IL_003b:  beq        IL_0045

    IL_0040:  br         IL_018f

    IL_0045:  ldloc.s    V_5
    IL_0047:  ldarg.1
    IL_0048:  sub
    IL_0049:  stloc.s    V_12
    IL_004b:  ldloc.s    V_12
    IL_004d:  ldc.i4.0
    IL_004e:  blt        IL_0058

    IL_0053:  br         IL_005a

    IL_0058:  ldloc.0
    IL_0059:  throw

    IL_005a:  ldloc.s    V_12
    IL_005c:  ldnull
    IL_005d:  call       class g $::k(int32,
                                      class g)
    IL_0062:  stloc.s    V_13
    IL_0064:  ldloc.s    V_13
    IL_0066:  brtrue     IL_0073

    IL_006b:  ldnull
    IL_006c:  stloc.s    V_14
    IL_006e:  br         IL_015e

    IL_0073:  ldloc.s    V_13
    IL_0075:  stloc.s    V_38
    IL_0077:  ldloc.s    V_38
    IL_0079:  stloc.s    V_18
    IL_007b:  ldloc.s    V_18
    IL_007d:  ldfld      class g g::b$PST04000006
    IL_0082:  stloc.s    V_19
    IL_0084:  ldloc.s    V_19
    IL_0086:  brtrue     IL_0090

    IL_008b:  br         IL_009d

    IL_0090:  ldloc.s    V_19
    IL_0092:  stloc.s    V_37
    IL_0094:  ldloc.s    V_37
    IL_0096:  stloc.s    V_20
    IL_0098:  br         IL_00af

    IL_009d:  ldloc.s    V_18
    IL_009f:  ldfld      string g::a$PST04000005
    IL_00a4:  stloc.s    V_36
    IL_00a6:  ldloc.s    V_36
    IL_00a8:  stloc.s    V_14
    IL_00aa:  br         IL_015e

    IL_00af:  ldloc.s    V_20
    IL_00b1:  ldfld      class g g::b$PST04000006
    IL_00b6:  stloc.s    V_21
    IL_00b8:  ldloc.s    V_21
    IL_00ba:  brtrue     IL_00c4

    IL_00bf:  br         IL_00d1

    IL_00c4:  ldloc.s    V_21
    IL_00c6:  stloc.s    V_35
    IL_00c8:  ldloc.s    V_35
    IL_00ca:  stloc.s    V_22
    IL_00cc:  br         IL_00f7

    IL_00d1:  ldloc.s    V_20
    IL_00d3:  ldfld      string g::a$PST04000005
    IL_00d8:  stloc.s    V_32
    IL_00da:  ldloc.s    V_18
    IL_00dc:  ldfld      string g::a$PST04000005
    IL_00e1:  stloc.s    V_33
    IL_00e3:  ldloc.s    V_33
    IL_00e5:  ldloc.s    V_32
    IL_00e7:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_00ec:  stloc.s    V_34
    IL_00ee:  ldloc.s    V_34
    IL_00f0:  stloc.s    V_14
    IL_00f2:  br         IL_015e

    IL_00f7:  ldloc.s    V_18
    IL_00f9:  ldfld      string g::a$PST04000005
    IL_00fe:  stloc.s    V_23
    IL_0100:  ldloc.s    V_23
    IL_0102:  newobj     instance void [mscorlib]System.Text.StringBuilder::.ctor(string)
    IL_0107:  stloc.s    V_24
    IL_0109:  ldloc.s    V_20
    IL_010b:  stloc.s    V_25
    IL_010d:  ldloc.s    V_25
    IL_010f:  brtrue     IL_0119

    IL_0114:  br         IL_0126

    IL_0119:  ldloc.s    V_25
    IL_011b:  stloc.s    V_31
    IL_011d:  ldloc.s    V_31
    IL_011f:  stloc.s    V_26
    IL_0121:  br         IL_0138

    IL_0126:  ldloc.s    V_24
    IL_0128:  callvirt   instance string [mscorlib]System.Text.StringBuilder::ToString()
    IL_012d:  stloc.s    V_30
    IL_012f:  ldloc.s    V_30
    IL_0131:  stloc.s    V_14
    IL_0133:  br         IL_015e

    IL_0138:  ldloc.s    V_26
    IL_013a:  ldfld      class g g::b$PST04000006
    IL_013f:  stloc.s    V_27
    IL_0141:  ldloc.s    V_26
    IL_0143:  ldfld      string g::a$PST04000005
    IL_0148:  stloc.s    V_28
    IL_014a:  ldloc.s    V_24
    IL_014c:  ldloc.s    V_28
    IL_014e:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::Append(string)
    IL_0153:  stloc.s    V_29
    IL_0155:  ldloc.s    V_27
    IL_0157:  stloc.s    V_25
    IL_0159:  br         IL_010d

    IL_015e:  ldloc.s    V_5
    IL_0160:  ldc.i4.1
    IL_0161:  add
    IL_0162:  stloc.s    V_15
    IL_0164:  ldarg.2
    IL_0165:  ldloc.s    V_14
    IL_0167:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_016c:  stloc.s    V_16
    IL_016e:  ldloc.s    V_16
    IL_0170:  ldstr      "0"
    IL_0175:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_017a:  stloc.s    V_17
    IL_017c:  ldarg.0
    IL_017d:  ldloc.s    V_15
    IL_017f:  ldloc.s    V_17
    IL_0181:  ldloc.3
    IL_0182:  starg.s    A_3
    IL_0184:  starg.s    A_2
    IL_0186:  starg.s    A_1
    IL_0188:  starg.s    A_0
    IL_018a:  br         IL_0006

    IL_018f:  ldarg.0
    IL_0190:  ldc.i4.1
    IL_0191:  add
    IL_0192:  stloc.s    V_7
    IL_0194:  ldloc.s    V_6
    IL_0196:  ldloc.s    V_5
    IL_0198:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_019d:  stloc.s    V_8
    IL_019f:  ldloc.s    V_8
    IL_01a1:  ldloc.3
    IL_01a2:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_01a7:  stloc.s    V_9
    IL_01a9:  ldloc.s    V_7
    IL_01ab:  ldc.i4.0
    IL_01ac:  ldnull
    IL_01ad:  ldloc.s    V_9
    IL_01af:  call       class g $::j(int32,
                                      int32,
                                      string,
                                      class b)
    IL_01b4:  stloc.s    V_10
    IL_01b6:  ldarg.2
    IL_01b7:  ldloc.s    V_10
    IL_01b9:  newobj     instance void g::.ctor(string,
                                                class g)
    IL_01be:  stloc.s    V_11
    IL_01c0:  ldloc.s    V_11
    IL_01c2:  ret

    IL_01c3:  ldarg.2
    IL_01c4:  ldnull
    IL_01c5:  newobj     instance void g::.ctor(string,
                                                class g)
    IL_01ca:  stloc.1
    IL_01cb:  ldloc.1
    IL_01cc:  ret
  }

  .method public static class g  k(int32 A_0,
                                   class g A_1) cil managed
  {
    .maxstack  4
    .locals init (int32 V_0,
             class g V_1)
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  beq        IL_000c

    IL_0007:  br         IL_000e

    IL_000c:  ldarg.1
    IL_000d:  ret

    IL_000e:  ldarg.0
    IL_000f:  ldc.i4.1
    IL_0010:  sub
    IL_0011:  stloc.0
    IL_0012:  ldstr      " "
    IL_0017:  ldarg.1
    IL_0018:  newobj     instance void g::.ctor(string,
                                                class g)
    IL_001d:  stloc.1
    IL_001e:  ldloc.0
    IL_001f:  ldloc.1
    IL_0020:  tail.
    IL_0022:  call       class g $::k(int32,
                                      class g)
    IL_0027:  ret
  }

  .method public static class b  l(class b A_0,
                                   class c A_1) cil managed
  {
    .maxstack  10
    .locals init (int32 V_0,
             int32 V_1,
             class j V_2)
ldstr "hit1"
call	void [System.Console]System.Console::WriteLine(string)
    IL_0000:  ldarg.1
    IL_0001:  ldfld      int32 c::b$PST04000002
    IL_0006:  stloc.0
    IL_0007:  ldarg.1
    IL_0008:  ldfld      int32 c::a$PST04000001
    IL_000d:  stloc.1
    IL_000e:  ldloc.0
    IL_000f:  ldloc.1
ldstr "hit2 - before call to $::d"
call	void [System.Console]System.Console::WriteLine(string)
    IL_0010:  newobj     instance void j::.ctor(int32,
                                                int32)
    IL_0015:  stloc.2
    IL_0016:  ldloc.2
    IL_0017:  ldarg.0
    IL_0018:  tail.
    IL_001a:  call       class b $::d(class a,
                                      class b)
    IL_001f:  ret
  }

  .method public static class b  m(int32 A_0) cil managed
  {
    .maxstack  6
    .locals init (class b V_0,
             int32 V_1,
             int32 V_2,
             int32 V_3,
             class c V_4,
             int32 V_5,
             int32 V_6,
             int32 V_7,
             int32 V_8,
             class c V_9,
             int32 V_10,
             class b V_11,
             class b V_12,
             class b V_13)
    IL_0000:  ldsfld     class b $::m$PST04000018
    IL_0005:  stloc.0
    IL_0006:  ldarg.0
    IL_0007:  ldc.i4.4
    IL_0008:  beq        IL_0012

    IL_000d:  br         IL_0014

    IL_0012:  ldloc.0
    IL_0013:  ret

    IL_0014:  ldarg.0
    IL_0015:  ldarg.0
    IL_0016:  add
    IL_0017:  stloc.1
    IL_0018:  ldarg.0
    IL_0019:  ldarg.0
    IL_001a:  add
    IL_001b:  stloc.2
    IL_001c:  ldloc.2
    IL_001d:  ldc.i4.1
    IL_001e:  add
    IL_001f:  stloc.3
    IL_0020:  ldloc.1
    IL_0021:  ldloc.3
    IL_0022:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0027:  stloc.s    V_4
    IL_0029:  ldarg.0
    IL_002a:  ldarg.0
    IL_002b:  add
    IL_002c:  stloc.s    V_5
    IL_002e:  ldloc.s    V_5
    IL_0030:  ldc.i4.2
    IL_0031:  add
    IL_0032:  stloc.s    V_6
    IL_0034:  ldarg.0
    IL_0035:  ldarg.0
    IL_0036:  add
    IL_0037:  stloc.s    V_7
    IL_0039:  ldloc.s    V_7
    IL_003b:  ldc.i4.1
    IL_003c:  add
    IL_003d:  stloc.s    V_8
    IL_003f:  ldloc.s    V_6
    IL_0041:  ldloc.s    V_8
    IL_0043:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0048:  stloc.s    V_9
    IL_004a:  ldarg.0
    IL_004b:  ldc.i4.1
    IL_004c:  add
    IL_004d:  stloc.s    V_10
    IL_004f:  ldloc.s    V_10
    IL_0051:  call       class b $::m(int32)
    IL_0056:  stloc.s    V_11
    IL_0058:  ldloc.s    V_9
    IL_005a:  ldloc.s    V_11
    IL_005c:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_0061:  stloc.s    V_12
    IL_0063:  ldloc.s    V_4
    IL_0065:  ldloc.s    V_12
    IL_0067:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_006c:  stloc.s    V_13
    IL_006e:  ldloc.s    V_13
    IL_0070:  ret
  }

  .method public static class b  n(class a A_0,
                                   class b A_1,
                                   class b A_2,
                                   class b A_3,
                                   class b A_4,
                                   class b A_5) cil managed
  {
    .maxstack  11
    .locals init (class b V_0,
             class b V_1,
             class c V_2,
             int32 V_3,
             int32 V_4,
             int32 V_5,
             int32 V_6,
             class b V_7,
             class b V_8,
             class b V_9,
             class b V_10,
             class k V_11,
             class b V_12)
    IL_0000:  ldarg.s    A_5
    IL_0002:  brtrue     IL_000c

    IL_0007:  br         IL_0018

    IL_000c:  ldarg.s    A_5
    IL_000e:  stloc.s    V_12
    IL_0010:  ldloc.s    V_12
    IL_0012:  stloc.0
    IL_0013:  br         IL_002c

    IL_0018:  ldarg.0
    IL_0019:  ldarg.1
    IL_001a:  newobj     instance void k::.ctor(class a,
                                                class b)
    IL_001f:  stloc.s    V_11
    IL_0021:  ldloc.s    V_11
    IL_0023:  ldarg.2
    IL_0024:  tail.
    IL_0026:  call       class b $::e(class a,
                                      class b)
    IL_002b:  ret

    IL_002c:  ldloc.0
    IL_002d:  ldfld      class b b::b$PST04000004
    IL_0032:  stloc.1
    IL_0033:  ldloc.0
    IL_0034:  ldfld      class c b::a$PST04000003
    IL_0039:  stloc.2
    IL_003a:  ldarg.0
    IL_003b:  ldarg.1
    IL_003c:  ldloc.2
    IL_003d:  call       int32 $::f(class a,
                                    class b,
                                    class c)
    IL_0042:  stloc.3
    IL_0043:  ldloc.3
    IL_0044:  brtrue     IL_004e

    IL_0049:  br         IL_005d

    IL_004e:  ldarg.0
    IL_004f:  ldarg.1
    IL_0050:  ldarg.2
    IL_0051:  ldarg.3
    IL_0052:  ldarg.s    A_4
    IL_0054:  ldloc.1
    IL_0055:  tail.
    IL_0057:  call       class b $::n(class a,
                                      class b,
                                      class b,
                                      class b,
                                      class b,
                                      class b)
    IL_005c:  ret

    IL_005d:  ldarg.0
    IL_005e:  ldarg.2
    IL_005f:  ldloc.2
    IL_0060:  call       int32 $::f(class a,
                                    class b,
                                    class c)
    IL_0065:  stloc.s    V_4
    IL_0067:  ldloc.s    V_4
    IL_0069:  brtrue     IL_0073

    IL_006e:  br         IL_008c

    IL_0073:  ldloc.2
    IL_0074:  ldarg.1
    IL_0075:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_007a:  stloc.s    V_10
    IL_007c:  ldarg.0
    IL_007d:  ldloc.s    V_10
    IL_007f:  ldarg.2
    IL_0080:  ldarg.3
    IL_0081:  ldarg.s    A_4
    IL_0083:  ldloc.1
    IL_0084:  tail.
    IL_0086:  call       class b $::n(class a,
                                      class b,
                                      class b,
                                      class b,
                                      class b,
                                      class b)
    IL_008b:  ret

    IL_008c:  ldarg.0
    IL_008d:  ldarg.3
    IL_008e:  ldloc.2
    IL_008f:  call       int32 $::f(class a,
                                    class b,
                                    class c)
    IL_0094:  stloc.s    V_5
    IL_0096:  ldloc.s    V_5
    IL_0098:  brtrue     IL_00a2

    IL_009d:  br         IL_00bb

    IL_00a2:  ldloc.2
    IL_00a3:  ldarg.2
    IL_00a4:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_00a9:  stloc.s    V_9
    IL_00ab:  ldarg.0
    IL_00ac:  ldarg.1
    IL_00ad:  ldloc.s    V_9
    IL_00af:  ldarg.3
    IL_00b0:  ldarg.s    A_4
    IL_00b2:  ldloc.1
    IL_00b3:  tail.
    IL_00b5:  call       class b $::n(class a,
                                      class b,
                                      class b,
                                      class b,
                                      class b,
                                      class b)
    IL_00ba:  ret

    IL_00bb:  ldarg.0
    IL_00bc:  ldarg.s    A_4
    IL_00be:  ldloc.2
    IL_00bf:  call       int32 $::f(class a,
                                    class b,
                                    class c)
    IL_00c4:  stloc.s    V_6
    IL_00c6:  ldloc.s    V_6
    IL_00c8:  brtrue     IL_00d2

    IL_00cd:  br         IL_00eb

    IL_00d2:  ldloc.2
    IL_00d3:  ldarg.3
    IL_00d4:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_00d9:  stloc.s    V_8
    IL_00db:  ldarg.0
    IL_00dc:  ldarg.1
    IL_00dd:  ldarg.2
    IL_00de:  ldloc.s    V_8
    IL_00e0:  ldarg.s    A_4
    IL_00e2:  ldloc.1
    IL_00e3:  tail.
    IL_00e5:  call       class b $::n(class a,
                                      class b,
                                      class b,
                                      class b,
                                      class b,
                                      class b)
    IL_00ea:  ret

    IL_00eb:  ldloc.2
    IL_00ec:  ldarg.s    A_4
    IL_00ee:  newobj     instance void b::.ctor(class c,
                                                class b)
    IL_00f3:  stloc.s    V_7
    IL_00f5:  ldarg.0
    IL_00f6:  ldarg.1
    IL_00f7:  ldarg.2
    IL_00f8:  ldarg.3
    IL_00f9:  ldloc.s    V_7
    IL_00fb:  ldloc.1
    IL_00fc:  tail.
    IL_00fe:  call       class b $::n(class a,
                                      class b,
                                      class b,
                                      class b,
                                      class b,
                                      class b)
    IL_0103:  ret
  }

}

.class public abstract auto ansi h
       extends [mscorlib]System.Exception
{
  .field privatescope string s$PST04000019
  .method public specialname rtspecialname
          instance void  .ctor(string A_0) cil managed
  {
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void [mscorlib]System.Exception::.ctor(string)
    IL_0007:  ldarg.0
    IL_0008:  ldarg.1
    IL_0009:  stfld      string h::s$PST04000019
    IL_000e:  ret
  }

  .method public virtual instance string
          ToString() cil managed
  {
    .maxstack  6
    .locals init (string V_0,
             string V_1,
             string V_2,
             string V_3,
             string V_4)
    IL_0000:  ldarg.0
    IL_0001:  callvirt   instance string h::ExnMessage()
    IL_0006:  stloc.0
    IL_0007:  ldsfld     string $::$l$PST0400000A
    IL_000c:  stloc.1
    IL_000d:  ldloc.1
    IL_000e:  brtrue     IL_0018

    IL_0013:  br         IL_0023

    IL_0018:  ldloc.1
    IL_0019:  stloc.s    V_4
    IL_001b:  ldloc.s    V_4
    IL_001d:  stloc.2
    IL_001e:  br         IL_0025

    IL_0023:  ldloc.0
    IL_0024:  ret

    IL_0025:  ldloc.0
    IL_0026:  ldloc.2
    IL_0027:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_002c:  stloc.3
    IL_002d:  ldloc.3
    IL_002e:  ret
  }

  .method public virtual instance string
          ExnMessage() cil managed
  {
    .maxstack  6
    .locals init (string V_0,
             string V_1,
             string V_2)
    IL_0000:  ldarg.0
    IL_0001:  callvirt   instance string h::ExnName()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldstr      ": "
    IL_000d:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0012:  stloc.1
    IL_0013:  ldarg.0
    IL_0014:  callvirt   instance string [mscorlib]System.Exception::get_Message()
    IL_0019:  stloc.2
    IL_001a:  ldloc.1
    IL_001b:  ldloc.2
    IL_001c:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0021:  ret
  }

  .method public virtual instance string
          ExnName() cil managed
  {
    .maxstack  2
    IL_0000:  ldstr      "SML exception"
    IL_0005:  ret
  }

}

.class public auto ansi sealed i
       extends a
{
  .field privatescope class c a$PST0400001A
  .method public specialname rtspecialname
          instance void  .ctor(class c A_0) cil managed
  {
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  call       instance void a::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      class c i::a$PST0400001A
    IL_000d:  ret
  }

  .method public virtual final instance int32
          b(class c A_0) cil managed
  {
    .maxstack  4
    .locals init (class c V_0,
             int32 V_1,
             int32 V_2)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class c i::a$PST0400001A
    IL_0006:  stloc.0
    IL_0007:  ldarg.1
    IL_0008:  ldfld      int32 c::b$PST04000002
    IL_000d:  stloc.1
    IL_000e:  ldarg.1
    IL_000f:  ldfld      int32 c::a$PST04000001
    IL_0014:  stloc.2
    IL_0015:  ldloc.2
    IL_0016:  ldloc.1
    IL_0017:  ldloc.0
    IL_0018:  tail.
    IL_001a:  call       int32 $::g(int32,
                                    int32,
                                    class c)
    IL_001f:  ret
  }

}

.class public auto ansi sealed j
       extends a
{
  .field privatescope int32 a$PST0400001B
  .field privatescope int32 b$PST0400001C
  .method public specialname rtspecialname
          instance void  .ctor(int32 A_0,
                               int32 A_1) cil managed
  {
    .maxstack  3
    IL_0000:  ldarg.0
ldstr "now1"
call	void [System.Console]System.Console::WriteLine(string)
    IL_0001:  call       instance void a::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      int32 j::a$PST0400001B
    IL_000d:  ldarg.0
    IL_000e:  ldarg.2
    IL_000f:  stfld      int32 j::b$PST0400001C
ldstr "now2"
call	void [System.Console]System.Console::WriteLine(string)
    IL_0014:  ret
  }

  .method public virtual final instance int32
          b(class c A_0) cil managed
  {
    .maxstack  4
    .locals init (int32 V_0,
             int32 V_1)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      int32 j::b$PST0400001C
    IL_0006:  stloc.0
    IL_0007:  ldarg.0
    IL_0008:  ldfld      int32 j::a$PST0400001B
    IL_000d:  stloc.1
    IL_000e:  ldloc.0
    IL_000f:  ldloc.1
    IL_0010:  ldarg.1
    IL_0011:  tail.
    IL_0013:  call       int32 $::g(int32,
                                    int32,
                                    class c)
    IL_0018:  ret
  }

  .method public virtual final instance class c
          c(class c A_0) cil managed
  {
    .maxstack  4
    .locals init (int32 V_0,
             int32 V_1,
             int32 V_2,
             int32 V_3,
             int32 V_4,
             int32 V_5)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      int32 j::b$PST0400001C
    IL_0006:  stloc.0
    IL_0007:  ldarg.0
    IL_0008:  ldfld      int32 j::a$PST0400001B
    IL_000d:  stloc.1
    IL_000e:  ldarg.1
    IL_000f:  ldfld      int32 c::b$PST04000002
    IL_0014:  stloc.2
    IL_0015:  ldarg.1
    IL_0016:  ldfld      int32 c::a$PST04000001
    IL_001b:  stloc.3
    IL_001c:  ldloc.3
    IL_001d:  ldloc.0
    IL_001e:  add
    IL_001f:  stloc.s    V_4
    IL_0021:  ldloc.2
    IL_0022:  ldloc.1
    IL_0023:  add
    IL_0024:  stloc.s    V_5
    IL_0026:  ldloc.s    V_4
    IL_0028:  ldloc.s    V_5
    IL_002a:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_002f:  ret
  }

}

.class public auto ansi sealed k
       extends a
{
  .field privatescope class a a$PST0400001D
  .field privatescope class b b$PST0400001E
  .method public specialname rtspecialname
          instance void  .ctor(class a A_0,
                               class b A_1) cil managed
  {
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  call       instance void a::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      class a k::a$PST0400001D
    IL_000d:  ldarg.0
    IL_000e:  ldarg.2
    IL_000f:  stfld      class b k::b$PST0400001E
    IL_0014:  ret
  }

  .method public virtual final instance int32
          b(class c A_0) cil managed
  {
    .maxstack  4
    .locals init (class b V_0,
             class a V_1,
             int32 V_2)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class b k::b$PST0400001E
    IL_0006:  stloc.0
    IL_0007:  ldarg.0
    IL_0008:  ldfld      class a k::a$PST0400001D
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldarg.1
    IL_0011:  call       int32 $::f(class a,
                                    class b,
                                    class c)
    IL_0016:  stloc.2
    IL_0017:  ldloc.2
    IL_0018:  ldc.i4.1
    IL_0019:  bne.un     IL_0023

    IL_001e:  br         IL_0025

    IL_0023:  ldc.i4.1
    IL_0024:  ret

    IL_0025:  ldc.i4.0
    IL_0026:  ret
  }

}

.class public auto ansi sealed l
       extends a
{
  .method public specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  call       instance void a::.ctor()
    IL_0006:  ret
  }

  .method public virtual final instance int32
          a(class c A_0,
            class c A_1) cil managed
  {
    .maxstack  4
    .locals init (int32 V_0,
             int32 V_1,
             int32 V_2,
             int32 V_3)
    IL_0000:  ldarg.1
    IL_0001:  ldfld      int32 c::a$PST04000001
    IL_0006:  stloc.0
    IL_0007:  ldarg.2
    IL_0008:  ldfld      int32 c::a$PST04000001
    IL_000d:  stloc.1
    IL_000e:  ldloc.0
    IL_000f:  ldloc.1
    IL_0010:  beq        IL_001a

    IL_0015:  br         IL_002d

    IL_001a:  ldarg.1
    IL_001b:  ldfld      int32 c::b$PST04000002
    IL_0020:  stloc.2
    IL_0021:  ldarg.2
    IL_0022:  ldfld      int32 c::b$PST04000002
    IL_0027:  stloc.3
    IL_0028:  ldloc.2
    IL_0029:  ldloc.3
    IL_002a:  ceq
    IL_002c:  ret

    IL_002d:  ldc.i4.0
    IL_002e:  ret
  }

  .method public virtual final instance int32
          b(class c A_0) cil managed
  {
    .maxstack  4
    .locals init (int32 V_0,
             int32 V_1,
             int32 V_2,
             int32 V_3)
    IL_0000:  ldarg.1
    IL_0001:  ldfld      int32 c::a$PST04000001
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.0
    IL_0009:  blt        IL_0015

    IL_000e:  ldc.i4.1
    IL_000f:  stloc.1
    IL_0010:  br         IL_001e

    IL_0015:  ldarg.1
    IL_0016:  ldfld      int32 c::b$PST04000002
    IL_001b:  stloc.3
    IL_001c:  ldc.i4.0
    IL_001d:  stloc.1
    IL_001e:  ldloc.1
    IL_001f:  brtrue     IL_0029

    IL_0024:  br         IL_0040

    IL_0029:  ldarg.1
    IL_002a:  ldfld      int32 c::b$PST04000002
    IL_002f:  stloc.2
    IL_0030:  ldloc.2
    IL_0031:  ldc.i4.0
    IL_0032:  blt        IL_003c

    IL_0037:  br         IL_003e

    IL_003c:  ldc.i4.0
    IL_003d:  ret

    IL_003e:  ldc.i4.1
    IL_003f:  ret

    IL_0040:  ldc.i4.0
    IL_0041:  ret
  }

  .method public virtual final instance class c
          c(class c A_0) cil managed
  {
    .maxstack  4
    .locals init (int32 V_0,
             int32 V_1,
             int32 V_2)
    IL_0000:  ldarg.1
    IL_0001:  ldfld      int32 c::b$PST04000002
    IL_0006:  stloc.0
    IL_0007:  ldarg.1
    IL_0008:  ldfld      int32 c::a$PST04000001
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  neg
    IL_0010:  stloc.2
    IL_0011:  ldloc.0
    IL_0012:  ldloc.2
    IL_0013:  newobj     instance void c::.ctor(int32,
                                                int32)
    IL_0018:  ret
  }

  .method public virtual final instance void
          d(string A_0) cil managed
  {
    .maxstack  5
    .locals init (class [mscorlib]System.IO.TextWriter V_0,
             class [mscorlib]System.IO.TextWriter V_1)
    IL_0000:  ldsfld     class [mscorlib]System.IO.TextWriter $::k$PST04000016
    IL_0005:  stloc.0
    IL_0006:  ldsfld     class [mscorlib]System.IO.TextWriter $::f$PST04000011
    IL_000b:  stloc.1
    IL_000c:  ldloc.1
    IL_000d:  ldarg.1
    IL_000e:  callvirt   instance void [mscorlib]System.IO.TextWriter::Write(string)
    IL_0013:  ldloc.0
    IL_0014:  ldstr      "\n"
    IL_0019:  callvirt   instance void [mscorlib]System.IO.TextWriter::Write(string)
    IL_001e:  ret
  }

}

.class public auto ansi sealed m
       extends h
{
  .method public specialname rtspecialname
          instance void  .ctor(string A_0) cil managed
  {
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void h::.ctor(string)
    IL_0007:  ret
  }

  .method public virtual instance string
          ExnName() cil managed
  {
    .maxstack  3
    IL_0000:  ldstr      "Main.ex_undefined"
    IL_0005:  ret
  }

}

.class public auto ansi sealed n
       extends h
{
  .method public specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldnull
    IL_0002:  call       instance void h::.ctor(string)
    IL_0007:  ret
  }

  .method public virtual instance string
          ExnName() cil managed
  {
    .maxstack  3
    IL_0000:  ldstr      "Time.Time"
    IL_0005:  ret
  }

}

.class public auto ansi sealed o
       extends h
{
  .method public specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldnull
    IL_0002:  call       instance void h::.ctor(string)
    IL_0007:  ret
  }

  .method public virtual instance string
          ExnName() cil managed
  {
    .maxstack  3
    IL_0000:  ldstr      "PrimUtils.Char.Chr"
    IL_0005:  ret
  }

}
