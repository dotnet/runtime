// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console { auto }
.assembly extern mscorlib { auto }

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly ASSEMBLY_NAME { }
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary {}
.assembly extern xunit.core {}
.namespace JitTest_tailcall_boxunbox
{
  .class public auto ansi beforefieldinit App
         extends [mscorlib]System.Object
  {
    .method private hidebysig static int64
            Test(bool path) cil managed
    {
      .maxstack  4
      .locals (int32 V_0)
      IL_0000:  ldc.i4.s   100
      IL_0002:  stloc.0
      IL_0003:  ldloc.0
      			ldarg.0
      			brfalse    IL_000f
      IL_0004:  box        [mscorlib]System.Int32
      IL_0009:  unbox      [mscorlib]System.Int32
      IL_000e:  ldind.i4
      IL_000f:  conv.r4
      			ldarg.0
      			brfalse    IL_01c8
      IL_0010:  box        [mscorlib]System.Single
      IL_0015:  unbox      [mscorlib]System.Single
      IL_001a:  ldind.r4
      IL_001b:  conv.i1
      IL_001c:  box        [mscorlib]System.SByte
      			ldarg.0
      			brfalse    IL_002c
      IL_0021:  unbox      [mscorlib]System.SByte
      IL_0026:  ldind.i1
      IL_0027:  box        [mscorlib]System.SByte
      IL_002c:  unbox      [mscorlib]System.SByte
      IL_0031:  ldind.i1
      IL_0032:  conv.i4
      IL_0038:  box        [mscorlib]System.IntPtr
      IL_003d:  unbox      [mscorlib]System.IntPtr
      			ldarg.0
      			brfalse    IL_00e0
      IL_0042:  ldobj      [mscorlib]System.IntPtr
      IL_004c:  conv.u1
      IL_004d:  box        [mscorlib]System.Byte
      IL_0052:  unbox      [mscorlib]System.Byte
      IL_0057:  ldind.u1
      IL_0058:  conv.i1
      IL_0059:  box        [mscorlib]System.SByte
      IL_005e:  unbox      [mscorlib]System.SByte
      IL_0063:  ldind.i1
      IL_0064:  conv.i8
      IL_0065:  box        [mscorlib]System.UInt64
      IL_006a:  unbox      [mscorlib]System.UInt64
      IL_006f:  ldind.i8
      			conv.i
      IL_0075:  box        [mscorlib]System.IntPtr
      IL_007a:  unbox      [mscorlib]System.IntPtr
      IL_007f:  ldobj      [mscorlib]System.IntPtr
      IL_0089:  conv.u8
      			ldarg.0
      			brfalse    IL_00c6
      IL_008a:  box        [mscorlib]System.UInt64
      IL_008f:  unbox      [mscorlib]System.UInt64
      IL_0094:  ldind.i8
      IL_0095:  conv.u1
      IL_0096:  box        [mscorlib]System.Byte
      IL_009b:  unbox      [mscorlib]System.Byte
      IL_00a0:  ldind.u1
      IL_00a1:  conv.u8
      IL_00a2:  box        [mscorlib]System.Int64
      IL_00a7:  unbox      [mscorlib]System.Int64
      IL_00ac:  ldind.i8
      IL_00ad:  conv.i1
      IL_00ae:  box        [mscorlib]System.SByte
      IL_00b3:  unbox      [mscorlib]System.SByte
      IL_00b8:  ldind.i1
      IL_00b9:  conv.u4
      IL_00ba:  box        [mscorlib]System.UInt32
      IL_00bf:  unbox      [mscorlib]System.UInt32
      IL_00c4:  ldind.u4
      IL_00c5:  conv.u8
      IL_00c6:  box        [mscorlib]System.Int64
      IL_00cb:  unbox      [mscorlib]System.Int64
      IL_00d0:  ldind.i8
      			conv.u
      IL_00d6:  box        [mscorlib]System.UIntPtr
      IL_00db:  unbox      [mscorlib]System.UIntPtr
      IL_00e0:  ldobj      [mscorlib]System.UIntPtr
      IL_00ea:  conv.r.un
      IL_00eb:  conv.r4
      IL_00ec:  box        [mscorlib]System.Single
      IL_00f1:  unbox      [mscorlib]System.Single
      IL_00f6:  ldind.r4
      IL_00f7:  conv.i1
      IL_00f8:  box        [mscorlib]System.SByte
      IL_00fd:  unbox      [mscorlib]System.SByte
      IL_0102:  ldind.i1
      IL_0103:  box        [mscorlib]System.SByte
      IL_0108:  unbox      [mscorlib]System.SByte
      IL_010d:  ldind.i1
      IL_010e:  conv.i4
      IL_0114:  box        [mscorlib]System.IntPtr
      IL_0119:  unbox      [mscorlib]System.IntPtr
      IL_011e:  ldobj      [mscorlib]System.IntPtr
      IL_0128:  conv.u1
      IL_0129:  box        [mscorlib]System.Byte
      IL_012e:  unbox      [mscorlib]System.Byte
      IL_0133:  ldind.u1
      IL_0134:  conv.i1
      IL_0135:  box        [mscorlib]System.SByte
      IL_013a:  unbox      [mscorlib]System.SByte
      IL_013f:  ldind.i1
      IL_0140:  conv.i8
      IL_0141:  box        [mscorlib]System.UInt64
      IL_0146:  unbox      [mscorlib]System.UInt64
      IL_014b:  ldind.i8
      			conv.i
      IL_0151:  box        [mscorlib]System.IntPtr
      IL_0156:  unbox      [mscorlib]System.IntPtr
      IL_015b:  ldobj      [mscorlib]System.IntPtr
      IL_0165:  conv.u8
      IL_0166:  box        [mscorlib]System.UInt64
      IL_016b:  unbox      [mscorlib]System.UInt64
      IL_0170:  ldind.i8
      IL_0171:  conv.u1
      			ldarg.0
      			brfalse    IL_018a
      IL_0172:  box        [mscorlib]System.Byte
      IL_0177:  unbox      [mscorlib]System.Byte
      IL_017c:  ldind.u1
      IL_017d:  conv.u8
      IL_017e:  box        [mscorlib]System.Int64
      IL_0183:  unbox      [mscorlib]System.Int64
      IL_0188:  ldind.i8
      IL_0189:  conv.i1
      IL_018a:  box        [mscorlib]System.SByte
      IL_018f:  unbox      [mscorlib]System.SByte
      IL_0194:  ldind.i1
      IL_0195:  conv.u4
      IL_0196:  box        [mscorlib]System.UInt32
      IL_019b:  unbox      [mscorlib]System.UInt32
      IL_01a0:  ldind.u4
      IL_01a1:  conv.u8
      IL_01a2:  box        [mscorlib]System.Int64
      IL_01a7:  unbox      [mscorlib]System.Int64
      IL_01ac:  ldind.i8
      			conv.u
      IL_01b2:  box        [mscorlib]System.UIntPtr
      IL_01b7:  unbox      [mscorlib]System.UIntPtr
      IL_01bc:  ldobj      [mscorlib]System.UIntPtr
      IL_01c6:  conv.r.un
      IL_01c7:  conv.r8
      IL_01c8:  box        [mscorlib]System.Double
      IL_01cd:  unbox      [mscorlib]System.Double
      IL_01d2:  ldind.r8
      IL_01d3:  ldc.r8     100.
      IL_01dc:  beq.s      IL_END

      IL_01de:  ldstr      "Loss of precision or unbox error"
      IL_01e3:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
      IL_01e8:  throw

		IL_END:
				ldc.i8 100
				ldarg.0
				brfalse.s IL_RET
				pop
				ldc.i4.0
		tail.   call       int64 JitTest_tailcall_boxunbox.App::Test(bool)
      IL_RET:  ret
    } // end of method App::Test

    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      // [ActiveIssue("https://github.com/dotnet/runtimelab/issues/", typeof(PlatformDetection), nameof(PlatformDetection.IsAppleMobile))]
      .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string,
                                                                                            class [System.Runtime]System.Type,
                                                                                            string[]) = (01 00 2F 68 74 74 70 73 3A 2F 2F 67 69 74 68 75
                                                                                                        62 2E 63 6F 6D 2F 64 6F 74 6E 65 74 2F 72 75 6E
                                                                                                        74 69 6D 65 6C 61 62 2F 69 73 73 75 65 73 2F 61
                                                                                                        54 65 73 74 4C 69 62 72 61 72 79 2E 50 6C 61 74
                                                                                                        66 6F 72 6D 44 65 74 65 63 74 69 6F 6E 2C 20 54
                                                                                                        65 73 74 4C 69 62 72 61 72 79 2C 20 56 65 72 73
                                                                                                        69 6F 6E 3D 30 2E 30 2E 30 2E 30 2C 20 43 75 6C
                                                                                                        74 75 72 65 3D 6E 65 75 74 72 61 6C 2C 20 50 75
                                                                                                        62 6C 69 63 4B 65 79 54 6F 6B 65 6E 3D 6E 75 6C
                                                                                                        6C 01 00 00 00 24 49 73 41 70 70 6C 65 4D 6F 62
                                                                                                        69 6C 65 00 00 00 )
      .entrypoint
      .maxstack  4
      .locals (int32 V_0)
      IL_0000:  ldc.i4.0
      IL_0001:  call       int64 JitTest_tailcall_boxunbox.App::Test(bool)
      IL_0006:  ldc.i4.1
      IL_0007:  call       int64 JitTest_tailcall_boxunbox.App::Test(bool)
      			pop
      			pop
      IL_000c:  ldstr      "=== TEST PASSED ==="
      IL_0011:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0016:  ldc.i4.s   100
      IL_0018:  stloc.0
      IL_0019:  br.s       IL_001b

      IL_001b:  ldloc.0
      IL_001c:  ret
    } // end of method App::Main

    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method App::.ctor

  } // end of class App

} // end of namespace JitTest_tailcall_boxunbox

