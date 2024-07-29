// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console { }
.assembly extern System.Globalization { }
.assembly GitHub_23411
{
}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit GitHub_23411
	extends [System.Runtime]System.Object
{
.method public hidebysig static bool 
        IsValidName (
		string name
	) cil managed 
{
	// Method begins at RVA 0x35c8
	// Code size 431 (0x1af)
	.maxstack 2
	.locals init (
		[0] int32,
		[1] char,
		[2] valuetype [System.Globalization]System.Globalization.UnicodeCategory,
		[3] valuetype [System.Globalization]System.Globalization.UnicodeCategory,
		[4] int32,
		[5] int32
	)

	IL_0000: ldc.i4.0
	IL_0001: stloc 5
	IL_0005: ldloca 5
	IL_0009: ldind.i4
	IL_000a: stloc.s 4
	IL_000c: ldloc.s 4
	IL_000e: switch (IL_0017)

	IL_0017: ldc.i4.0
	IL_0018: switch (IL_0048)

	IL_0021: ldloc.s 4
	IL_0023: switch (IL_0170, IL_00a1, IL_005c, IL_013c, IL_0125, IL_01a8, IL_0077, IL_015d)

	IL_0048: ldc.i4.0
	IL_0049: stloc.0
	IL_004a: ldc.i4 2
	IL_004f: stloc 5
	IL_0053: ldloca 5
	IL_0057: ldind.i4
	IL_0058: stloc.s 4
	IL_005a: br.s IL_0021

	IL_005c: br IL_0127

	IL_0061: ldloc.0
	IL_0062: ldc.i4.1
	IL_0063: add
	IL_0064: stloc.0
	IL_0065: ldc.i4 6
	IL_006a: stloc 5
	IL_006e: ldloca 5
	IL_0072: ldind.i4
	IL_0073: stloc.s 4
	IL_0075: br.s IL_0021

	IL_0077: br IL_0127

	IL_007c: ldc.i4.0
	IL_007d: ret

	IL_007e: ldarg.0
	IL_007f: ldloc.0
	IL_0080: callvirt instance char [System.Runtime]System.String::get_Chars(int32)
	IL_0085: stloc.1
	IL_0086: ldloc.1
	IL_0087: call valuetype [System.Globalization]System.Globalization.UnicodeCategory [System.Runtime]System.Char::GetUnicodeCategory(char)
	IL_008c: stloc.2
	IL_008d: ldloc.2
	IL_008e: stloc.3
	IL_008f: ldc.i4 1
	IL_0094: stloc 5
	IL_0098: ldloca 5
	IL_009c: ldind.i4
	IL_009d: stloc.s 4
	IL_009f: br.s IL_0021

	IL_00a1: ldc.i4 -2385
	IL_00a6: stloc 5
	IL_00aa: ldloca 5
	IL_00ae: ldind.i4
	IL_00af: ldc.i4 -2385
	IL_00b4: stloc 5
	IL_00b8: ldloca 5
	IL_00bc: ldind.i4
	IL_00bd: ceq
	IL_00bf: switch (IL_015d, IL_00d1, IL_015d)

	IL_00d0: nop

	IL_00d1: ldc.i4 1
	IL_00d6: stloc 5
	IL_00da: ldloca 5
	IL_00de: ldind.i4
	IL_00df: br.s IL_00ed

	IL_00e1: ldc.i4.0
	IL_00e2: stloc 5
	IL_00e6: ldloca 5
	IL_00ea: ldind.i4
	IL_00eb: br.s IL_00ed

	IL_00ed: brfalse.s IL_00ef

	IL_00ef: ldc.i4.0
	IL_00f0: stloc 5
	IL_00f4: ldloca 5
	IL_00f8: ldind.i4
	IL_00f9: brfalse IL_00fe

	IL_00fe: ldloc.3
	IL_00ff: switch (IL_0061, IL_0061, IL_0061)

	IL_0110: ldc.i4 4
	IL_0115: stloc 5
	IL_0119: ldloca 5
	IL_011d: ldind.i4
	IL_011e: stloc.s 4
	IL_0120: br IL_0021

	IL_0125: br.s IL_015f

	IL_0127: ldc.i4 3
	IL_012c: stloc 5
	IL_0130: ldloca 5
	IL_0134: ldind.i4
	IL_0135: stloc.s 4
	IL_0137: br IL_0021

	IL_013c: ldloc.0
	IL_013d: ldarg.0
	IL_013e: callvirt instance int32 [System.Runtime]System.String::get_Length()
	IL_0143: blt IL_007e

	IL_0148: ldc.i4 7
	IL_014d: stloc 5
	IL_0151: ldloca 5
	IL_0155: ldind.i4
	IL_0156: stloc.s 4
	IL_0158: br IL_0021

	IL_015d: br.s IL_01ad

	IL_015f: ldc.i4.0
	IL_0160: stloc 5
	IL_0164: ldloca 5
	IL_0168: ldind.i4
	IL_0169: stloc.s 4
	IL_016b: br IL_0021

	IL_0170: ldloc.3
	IL_0171: ldc.i4.8
	IL_0172: beq IL_0061

	IL_0177: br.s IL_0186

	IL_0179: ldc.i4.0
	IL_017a: stloc 5
	IL_017e: ldloca 5
	IL_0182: ldind.i4

	IL_0183: pop
	IL_0184: br.s IL_0192

	IL_0186: ldc.i4.0
	IL_0187: stloc 5
	IL_018b: ldloca 5
	IL_018f: ldind.i4
	IL_0190: br.s IL_0183

	IL_0192: nop
	IL_0193: ldc.i4 5
	IL_0198: stloc 5
	IL_019c: ldloca 5
	IL_01a0: ldind.i4
	IL_01a1: stloc.s 4
	IL_01a3: br IL_0021

	IL_01a8: br IL_007c

	IL_01ad: ldc.i4.1
	IL_01ae: ret
  }

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       59 (0x3b)
    .maxstack  8
    .locals init (bool V_0)

    ldstr      "Test"
    call       bool GitHub_23411::IsValidName(string)
    stloc      0
    ldloc      0
    brfalse.s  FAIL

    ldstr      "Pass"
    call       void [System.Console]System.Console::WriteLine(string)
    ldc.i4.s   100
    ret
FAIL:
    ldstr      "FAIL"
    call       void [System.Console]System.Console::WriteLine(string)
    ldc.i4.m1
    ret
  } // end of method GitHub_23411::Main


} // end of class GitHub_23411
