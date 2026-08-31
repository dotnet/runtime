// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace JitTest_comp64_NaN_il {
.class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
{
.method private hidebysig static void RunTests(float64 nan,
                     	float64 plusinf, float64 zero, float64 minusinf) cil managed
{
.maxstack  8
	ldarg.s 'nan'
	ldarg.s 'nan'
	clt
	brfalse.s IL_1
	ldstr "nan clt nan"
	br IL_THROW
IL_1:

	ldarg.s 'nan'
	ldarg.s 'plusinf'
	clt
	brfalse.s IL_2
	ldstr "nan clt plusinf"
	br IL_THROW
IL_2:

	ldarg.s 'nan'
	ldarg.s 'zero'
	clt
	brfalse.s IL_3
	ldstr "nan clt zero"
	br IL_THROW
IL_3:

	ldarg.s 'nan'
	ldarg.s 'minusinf'
	clt
	brfalse.s IL_4
	ldstr "nan clt minusinf"
	br IL_THROW
IL_4:

	ldarg.s 'plusinf'
	ldarg.s 'nan'
	clt
	brfalse.s IL_5
	ldstr "plusinf clt nan"
	br IL_THROW
IL_5:

	ldarg.s 'plusinf'
	ldarg.s 'plusinf'
	clt
	brfalse.s IL_6
	ldstr "plusinf clt plusinf"
	br IL_THROW
IL_6:

	ldarg.s 'plusinf'
	ldarg.s 'zero'
	clt
	brfalse.s IL_7
	ldstr "plusinf clt zero"
	br IL_THROW
IL_7:

	ldarg.s 'plusinf'
	ldarg.s 'minusinf'
	clt
	brfalse.s IL_8
	ldstr "plusinf clt minusinf"
	br IL_THROW
IL_8:

	ldarg.s 'zero'
	ldarg.s 'nan'
	clt
	brfalse.s IL_9
	ldstr "zero clt nan"
	br IL_THROW
IL_9:

	ldarg.s 'zero'
	ldarg.s 'plusinf'
	clt
	brtrue.s IL_10
	ldstr "zero clt plusinf"
	br IL_THROW
IL_10:

	ldarg.s 'zero'
	ldarg.s 'zero'
	clt
	brfalse.s IL_11
	ldstr "zero clt zero"
	br IL_THROW
IL_11:

	ldarg.s 'zero'
	ldarg.s 'minusinf'
	clt
	brfalse.s IL_12
	ldstr "zero clt minusinf"
	br IL_THROW
IL_12:

	ldarg.s 'minusinf'
	ldarg.s 'nan'
	clt
	brfalse.s IL_13
	ldstr "minusinf clt nan"
	br IL_THROW
IL_13:

	ldarg.s 'minusinf'
	ldarg.s 'plusinf'
	clt
	brtrue.s IL_14
	ldstr "minusinf clt plusinf"
	br IL_THROW
IL_14:

	ldarg.s 'minusinf'
	ldarg.s 'zero'
	clt
	brtrue.s IL_15
	ldstr "minusinf clt zero"
	br IL_THROW
IL_15:

	ldarg.s 'minusinf'
	ldarg.s 'minusinf'
	clt
	brfalse.s IL_16
	ldstr "minusinf clt minusinf"
	br IL_THROW
IL_16:

	ldarg.s 'nan'
	ldarg.s 'nan'
	ceq
	brfalse.s IL_17
	ldstr "nan ceq nan"
	br IL_THROW
IL_17:

	ldarg.s 'nan'
	ldarg.s 'plusinf'
	ceq
	brfalse.s IL_18
	ldstr "nan ceq plusinf"
	br IL_THROW
IL_18:

	ldarg.s 'nan'
	ldarg.s 'zero'
	ceq
	brfalse.s IL_19
	ldstr "nan ceq zero"
	br IL_THROW
IL_19:

	ldarg.s 'nan'
	ldarg.s 'minusinf'
	ceq
	brfalse.s IL_20
	ldstr "nan ceq minusinf"
	br IL_THROW
IL_20:

	ldarg.s 'plusinf'
	ldarg.s 'nan'
	ceq
	brfalse.s IL_21
	ldstr "plusinf ceq nan"
	br IL_THROW
IL_21:

	ldarg.s 'plusinf'
	ldarg.s 'plusinf'
	ceq
	brtrue.s IL_22
	ldstr "plusinf ceq plusinf"
	br IL_THROW
IL_22:

	ldarg.s 'plusinf'
	ldarg.s 'zero'
	ceq
	brfalse.s IL_23
	ldstr "plusinf ceq zero"
	br IL_THROW
IL_23:

	ldarg.s 'plusinf'
	ldarg.s 'minusinf'
	ceq
	brfalse.s IL_24
	ldstr "plusinf ceq minusinf"
	br IL_THROW
IL_24:

	ldarg.s 'zero'
	ldarg.s 'nan'
	ceq
	brfalse.s IL_25
	ldstr "zero ceq nan"
	br IL_THROW
IL_25:

	ldarg.s 'zero'
	ldarg.s 'plusinf'
	ceq
	brfalse.s IL_26
	ldstr "zero ceq plusinf"
	br IL_THROW
IL_26:

	ldarg.s 'zero'
	ldarg.s 'zero'
	ceq
	brtrue.s IL_27
	ldstr "zero ceq zero"
	br IL_THROW
IL_27:

	ldarg.s 'zero'
	ldarg.s 'minusinf'
	ceq
	brfalse.s IL_28
	ldstr "zero ceq minusinf"
	br IL_THROW
IL_28:

	ldarg.s 'minusinf'
	ldarg.s 'nan'
	ceq
	brfalse.s IL_29
	ldstr "minusinf ceq nan"
	br IL_THROW
IL_29:

	ldarg.s 'minusinf'
	ldarg.s 'plusinf'
	ceq
	brfalse.s IL_30
	ldstr "minusinf ceq plusinf"
	br IL_THROW
IL_30:

	ldarg.s 'minusinf'
	ldarg.s 'zero'
	ceq
	brfalse.s IL_31
	ldstr "minusinf ceq zero"
	br IL_THROW
IL_31:

	ldarg.s 'minusinf'
	ldarg.s 'minusinf'
	ceq
	brtrue.s IL_32
	ldstr "minusinf ceq minusinf"
	br IL_THROW
IL_32:

	ldarg.s 'nan'
	ldarg.s 'nan'
	cgt
	brfalse.s IL_33
	ldstr "nan cgt nan"
	br IL_THROW
IL_33:

	ldarg.s 'nan'
	ldarg.s 'plusinf'
	cgt
	brfalse.s IL_34
	ldstr "nan cgt plusinf"
	br IL_THROW
IL_34:

	ldarg.s 'nan'
	ldarg.s 'zero'
	cgt
	brfalse.s IL_35
	ldstr "nan cgt zero"
	br IL_THROW
IL_35:

	ldarg.s 'nan'
	ldarg.s 'minusinf'
	cgt
	brfalse.s IL_36
	ldstr "nan cgt minusinf"
	br IL_THROW
IL_36:

	ldarg.s 'plusinf'
	ldarg.s 'nan'
	cgt
	brfalse.s IL_37
	ldstr "plusinf cgt nan"
	br IL_THROW
IL_37:

	ldarg.s 'plusinf'
	ldarg.s 'plusinf'
	cgt
	brfalse.s IL_38
	ldstr "plusinf cgt plusinf"
	br IL_THROW
IL_38:

	ldarg.s 'plusinf'
	ldarg.s 'zero'
	cgt
	brtrue.s IL_39
	ldstr "plusinf cgt zero"
	br IL_THROW
IL_39:

	ldarg.s 'plusinf'
	ldarg.s 'minusinf'
	cgt
	brtrue.s IL_40
	ldstr "plusinf cgt minusinf"
	br IL_THROW
IL_40:

	ldarg.s 'zero'
	ldarg.s 'nan'
	cgt
	brfalse.s IL_41
	ldstr "zero cgt nan"
	br IL_THROW
IL_41:

	ldarg.s 'zero'
	ldarg.s 'plusinf'
	cgt
	brfalse.s IL_42
	ldstr "zero cgt plusinf"
	br IL_THROW
IL_42:

	ldarg.s 'zero'
	ldarg.s 'zero'
	cgt
	brfalse.s IL_43
	ldstr "zero cgt zero"
	br IL_THROW
IL_43:

	ldarg.s 'zero'
	ldarg.s 'minusinf'
	cgt
	brtrue.s IL_44
	ldstr "zero cgt minusinf"
	br IL_THROW
IL_44:

	ldarg.s 'minusinf'
	ldarg.s 'nan'
	cgt
	brfalse.s IL_45
	ldstr "minusinf cgt nan"
	br IL_THROW
IL_45:

	ldarg.s 'minusinf'
	ldarg.s 'plusinf'
	cgt
	brfalse.s IL_46
	ldstr "minusinf cgt plusinf"
	br IL_THROW
IL_46:

	ldarg.s 'minusinf'
	ldarg.s 'zero'
	cgt
	brfalse.s IL_47
	ldstr "minusinf cgt zero"
	br IL_THROW
IL_47:

	ldarg.s 'minusinf'
	ldarg.s 'minusinf'
	cgt
	brfalse.s IL_48
	ldstr "minusinf cgt minusinf"
	br IL_THROW
IL_48:

	ldarg.s 'nan'
	ldarg.s 'nan'
	clt.un
	brtrue.s IL_49
	ldstr "nan clt.un nan"
	br IL_THROW
IL_49:

	ldarg.s 'nan'
	ldarg.s 'plusinf'
	clt.un
	brtrue.s IL_50
	ldstr "nan clt.un plusinf"
	br IL_THROW
IL_50:

	ldarg.s 'nan'
	ldarg.s 'zero'
	clt.un
	brtrue.s IL_51
	ldstr "nan clt.un zero"
	br IL_THROW
IL_51:

	ldarg.s 'nan'
	ldarg.s 'minusinf'
	clt.un
	brtrue.s IL_52
	ldstr "nan clt.un minusinf"
	br IL_THROW
IL_52:

	ldarg.s 'plusinf'
	ldarg.s 'nan'
	clt.un
	brtrue.s IL_53
	ldstr "plusinf clt.un nan"
	br IL_THROW
IL_53:

	ldarg.s 'plusinf'
	ldarg.s 'plusinf'
	clt.un
	brfalse.s IL_54
	ldstr "plusinf clt.un plusinf"
	br IL_THROW
IL_54:

	ldarg.s 'plusinf'
	ldarg.s 'zero'
	clt.un
	brfalse.s IL_55
	ldstr "plusinf clt.un zero"
	br IL_THROW
IL_55:

	ldarg.s 'plusinf'
	ldarg.s 'minusinf'
	clt.un
	brfalse.s IL_56
	ldstr "plusinf clt.un minusinf"
	br IL_THROW
IL_56:

	ldarg.s 'zero'
	ldarg.s 'nan'
	clt.un
	brtrue.s IL_57
	ldstr "zero clt.un nan"
	br IL_THROW
IL_57:

	ldarg.s 'zero'
	ldarg.s 'plusinf'
	clt.un
	brtrue.s IL_58
	ldstr "zero clt.un plusinf"
	br IL_THROW
IL_58:

	ldarg.s 'zero'
	ldarg.s 'zero'
	clt.un
	brfalse.s IL_59
	ldstr "zero clt.un zero"
	br IL_THROW
IL_59:

	ldarg.s 'zero'
	ldarg.s 'minusinf'
	clt.un
	brfalse.s IL_60
	ldstr "zero clt.un minusinf"
	br IL_THROW
IL_60:

	ldarg.s 'minusinf'
	ldarg.s 'nan'
	clt.un
	brtrue.s IL_61
	ldstr "minusinf clt.un nan"
	br IL_THROW
IL_61:

	ldarg.s 'minusinf'
	ldarg.s 'plusinf'
	clt.un
	brtrue.s IL_62
	ldstr "minusinf clt.un plusinf"
	br IL_THROW
IL_62:

	ldarg.s 'minusinf'
	ldarg.s 'zero'
	clt.un
	brtrue.s IL_63
	ldstr "minusinf clt.un zero"
	br IL_THROW
IL_63:

	ldarg.s 'minusinf'
	ldarg.s 'minusinf'
	clt.un
	brfalse.s IL_64
	ldstr "minusinf clt.un minusinf"
	br IL_THROW
IL_64:

	ldarg.s 'nan'
	ldarg.s 'nan'
	cgt.un
	brtrue.s IL_65
	ldstr "nan cgt.un nan"
	br IL_THROW
IL_65:

	ldarg.s 'nan'
	ldarg.s 'plusinf'
	cgt.un
	brtrue.s IL_66
	ldstr "nan cgt.un plusinf"
	br IL_THROW
IL_66:

	ldarg.s 'nan'
	ldarg.s 'zero'
	cgt.un
	brtrue.s IL_67
	ldstr "nan cgt.un zero"
	br IL_THROW
IL_67:

	ldarg.s 'nan'
	ldarg.s 'minusinf'
	cgt.un
	brtrue.s IL_68
	ldstr "nan cgt.un minusinf"
	br IL_THROW
IL_68:

	ldarg.s 'plusinf'
	ldarg.s 'nan'
	cgt.un
	brtrue.s IL_69
	ldstr "plusinf cgt.un nan"
	br IL_THROW
IL_69:

	ldarg.s 'plusinf'
	ldarg.s 'plusinf'
	cgt.un
	brfalse.s IL_70
	ldstr "plusinf cgt.un plusinf"
	br IL_THROW
IL_70:

	ldarg.s 'plusinf'
	ldarg.s 'zero'
	cgt.un
	brtrue.s IL_71
	ldstr "plusinf cgt.un zero"
	br IL_THROW
IL_71:

	ldarg.s 'plusinf'
	ldarg.s 'minusinf'
	cgt.un
	brtrue.s IL_72
	ldstr "plusinf cgt.un minusinf"
	br IL_THROW
IL_72:

	ldarg.s 'zero'
	ldarg.s 'nan'
	cgt.un
	brtrue.s IL_73
	ldstr "zero cgt.un nan"
	br IL_THROW
IL_73:

	ldarg.s 'zero'
	ldarg.s 'plusinf'
	cgt.un
	brfalse.s IL_74
	ldstr "zero cgt.un plusinf"
	br IL_THROW
IL_74:

	ldarg.s 'zero'
	ldarg.s 'zero'
	cgt.un
	brfalse.s IL_75
	ldstr "zero cgt.un zero"
	br IL_THROW
IL_75:

	ldarg.s 'zero'
	ldarg.s 'minusinf'
	cgt.un
	brtrue.s IL_76
	ldstr "zero cgt.un minusinf"
	br IL_THROW
IL_76:

	ldarg.s 'minusinf'
	ldarg.s 'nan'
	cgt.un
	brtrue.s IL_77
	ldstr "minusinf cgt.un nan"
	br IL_THROW
IL_77:

	ldarg.s 'minusinf'
	ldarg.s 'plusinf'
	cgt.un
	brfalse.s IL_78
	ldstr "minusinf cgt.un plusinf"
	br IL_THROW
IL_78:

	ldarg.s 'minusinf'
	ldarg.s 'zero'
	cgt.un
	brfalse.s IL_79
	ldstr "minusinf cgt.un zero"
	br IL_THROW
IL_79:

	ldarg.s 'minusinf'
	ldarg.s 'minusinf'
	cgt.un
	brfalse.s IL_80
	ldstr "minusinf cgt.un minusinf"
	br IL_THROW
IL_80:

	ret
IL_THROW:
	newobj instance void [mscorlib]System.Exception::.ctor(string)
	throw
}

.method public hidebysig static int32 Main() cil managed
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack  4
	.locals (int32 V_0)
	ldc.r8     (00 00 00 00 00 00 F8 FF)
	ldc.r8     (00 00 00 00 00 00 F0 7F)
	ldc.r8     0
	ldc.r8     (00 00 00 00 00 00 F0 FF)
	call       void JitTest_comp64_NaN_il.Test::RunTests(float64,
                                           float64,
                                           float64,
                                           float64)
	ldstr      "=== PASSED ==="
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.s   100
	ret
}
.method public hidebysig specialname rtspecialname 
instance void  .ctor() cil managed
{
	.maxstack  8
	ldarg.0
	call       instance void [mscorlib]System.Object::.ctor()
	ret
}
}
}
