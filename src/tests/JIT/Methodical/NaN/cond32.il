// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly cond32 { }
.assembly extern xunit.core {}
.namespace JitTest_cond32_NaN_il {
.class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
{
.method private hidebysig static void RunTests(float32 nan,
                     	float32 plusinf, float32 zero, float32 minusinf) cil managed
{
.maxstack  8
	ldarg.s 'nan'
	ldarg.s 'nan'
	beq.s IL_FAIL_1
	br.s IL_1
IL_FAIL_1:
	ldstr "nan beq nan"
	br IL_THROW
IL_1:

	ldarg.s 'nan'
	ldarg.s 'plusinf'
	beq.s IL_FAIL_2
	br.s IL_2
IL_FAIL_2:
	ldstr "nan beq plusinf"
	br IL_THROW
IL_2:

	ldarg.s 'nan'
	ldarg.s 'zero'
	beq.s IL_FAIL_3
	br.s IL_3
IL_FAIL_3:
	ldstr "nan beq zero"
	br IL_THROW
IL_3:

	ldarg.s 'nan'
	ldarg.s 'minusinf'
	beq.s IL_FAIL_4
	br.s IL_4
IL_FAIL_4:
	ldstr "nan beq minusinf"
	br IL_THROW
IL_4:

	ldarg.s 'plusinf'
	ldarg.s 'nan'
	beq.s IL_FAIL_5
	br.s IL_5
IL_FAIL_5:
	ldstr "plusinf beq nan"
	br IL_THROW
IL_5:

	ldarg.s 'plusinf'
	ldarg.s 'plusinf'
	beq.s IL_6
	ldstr "plusinf beq plusinf"
	br IL_THROW
IL_6:

	ldarg.s 'plusinf'
	ldarg.s 'zero'
	beq.s IL_FAIL_7
	br.s IL_7
IL_FAIL_7:
	ldstr "plusinf beq zero"
	br IL_THROW
IL_7:

	ldarg.s 'plusinf'
	ldarg.s 'minusinf'
	beq.s IL_FAIL_8
	br.s IL_8
IL_FAIL_8:
	ldstr "plusinf beq minusinf"
	br IL_THROW
IL_8:

	ldarg.s 'zero'
	ldarg.s 'nan'
	beq.s IL_FAIL_9
	br.s IL_9
IL_FAIL_9:
	ldstr "zero beq nan"
	br IL_THROW
IL_9:

	ldarg.s 'zero'
	ldarg.s 'plusinf'
	beq.s IL_FAIL_10
	br.s IL_10
IL_FAIL_10:
	ldstr "zero beq plusinf"
	br IL_THROW
IL_10:

	ldarg.s 'zero'
	ldarg.s 'zero'
	beq.s IL_11
	ldstr "zero beq zero"
	br IL_THROW
IL_11:

	ldarg.s 'zero'
	ldarg.s 'minusinf'
	beq.s IL_FAIL_12
	br.s IL_12
IL_FAIL_12:
	ldstr "zero beq minusinf"
	br IL_THROW
IL_12:

	ldarg.s 'minusinf'
	ldarg.s 'nan'
	beq.s IL_FAIL_13
	br.s IL_13
IL_FAIL_13:
	ldstr "minusinf beq nan"
	br IL_THROW
IL_13:

	ldarg.s 'minusinf'
	ldarg.s 'plusinf'
	beq.s IL_FAIL_14
	br.s IL_14
IL_FAIL_14:
	ldstr "minusinf beq plusinf"
	br IL_THROW
IL_14:

	ldarg.s 'minusinf'
	ldarg.s 'zero'
	beq.s IL_FAIL_15
	br.s IL_15
IL_FAIL_15:
	ldstr "minusinf beq zero"
	br IL_THROW
IL_15:

	ldarg.s 'minusinf'
	ldarg.s 'minusinf'
	beq.s IL_16
	ldstr "minusinf beq minusinf"
	br IL_THROW
IL_16:

	ldarg.s 'nan'
	ldarg.s 'nan'
	bge.s IL_FAIL_17
	br.s IL_17
IL_FAIL_17:
	ldstr "nan bge nan"
	br IL_THROW
IL_17:

	ldarg.s 'nan'
	ldarg.s 'plusinf'
	bge.s IL_FAIL_18
	br.s IL_18
IL_FAIL_18:
	ldstr "nan bge plusinf"
	br IL_THROW
IL_18:

	ldarg.s 'nan'
	ldarg.s 'zero'
	bge.s IL_FAIL_19
	br.s IL_19
IL_FAIL_19:
	ldstr "nan bge zero"
	br IL_THROW
IL_19:

	ldarg.s 'nan'
	ldarg.s 'minusinf'
	bge.s IL_FAIL_20
	br.s IL_20
IL_FAIL_20:
	ldstr "nan bge minusinf"
	br IL_THROW
IL_20:

	ldarg.s 'plusinf'
	ldarg.s 'nan'
	bge.s IL_FAIL_21
	br.s IL_21
IL_FAIL_21:
	ldstr "plusinf bge nan"
	br IL_THROW
IL_21:

	ldarg.s 'plusinf'
	ldarg.s 'plusinf'
	bge.s IL_22
	ldstr "plusinf bge plusinf"
	br IL_THROW
IL_22:

	ldarg.s 'plusinf'
	ldarg.s 'zero'
	bge.s IL_23
	ldstr "plusinf bge zero"
	br IL_THROW
IL_23:

	ldarg.s 'plusinf'
	ldarg.s 'minusinf'
	bge.s IL_24
	ldstr "plusinf bge minusinf"
	br IL_THROW
IL_24:

	ldarg.s 'zero'
	ldarg.s 'nan'
	bge.s IL_FAIL_25
	br.s IL_25
IL_FAIL_25:
	ldstr "zero bge nan"
	br IL_THROW
IL_25:

	ldarg.s 'zero'
	ldarg.s 'plusinf'
	bge.s IL_FAIL_26
	br.s IL_26
IL_FAIL_26:
	ldstr "zero bge plusinf"
	br IL_THROW
IL_26:

	ldarg.s 'zero'
	ldarg.s 'zero'
	bge.s IL_27
	ldstr "zero bge zero"
	br IL_THROW
IL_27:

	ldarg.s 'zero'
	ldarg.s 'minusinf'
	bge.s IL_28
	ldstr "zero bge minusinf"
	br IL_THROW
IL_28:

	ldarg.s 'minusinf'
	ldarg.s 'nan'
	bge.s IL_FAIL_29
	br.s IL_29
IL_FAIL_29:
	ldstr "minusinf bge nan"
	br IL_THROW
IL_29:

	ldarg.s 'minusinf'
	ldarg.s 'plusinf'
	bge.s IL_FAIL_30
	br.s IL_30
IL_FAIL_30:
	ldstr "minusinf bge plusinf"
	br IL_THROW
IL_30:

	ldarg.s 'minusinf'
	ldarg.s 'zero'
	bge.s IL_FAIL_31
	br.s IL_31
IL_FAIL_31:
	ldstr "minusinf bge zero"
	br IL_THROW
IL_31:

	ldarg.s 'minusinf'
	ldarg.s 'minusinf'
	bge.s IL_32
	ldstr "minusinf bge minusinf"
	br IL_THROW
IL_32:

	ldarg.s 'nan'
	ldarg.s 'nan'
	bge.un.s IL_33
	ldstr "nan bge.un nan"
	br IL_THROW
IL_33:

	ldarg.s 'nan'
	ldarg.s 'plusinf'
	bge.un.s IL_34
	ldstr "nan bge.un plusinf"
	br IL_THROW
IL_34:

	ldarg.s 'nan'
	ldarg.s 'zero'
	bge.un.s IL_35
	ldstr "nan bge.un zero"
	br IL_THROW
IL_35:

	ldarg.s 'nan'
	ldarg.s 'minusinf'
	bge.un.s IL_36
	ldstr "nan bge.un minusinf"
	br IL_THROW
IL_36:

	ldarg.s 'plusinf'
	ldarg.s 'nan'
	bge.un.s IL_37
	ldstr "plusinf bge.un nan"
	br IL_THROW
IL_37:

	ldarg.s 'plusinf'
	ldarg.s 'plusinf'
	bge.un.s IL_38
	ldstr "plusinf bge.un plusinf"
	br IL_THROW
IL_38:

	ldarg.s 'plusinf'
	ldarg.s 'zero'
	bge.un.s IL_39
	ldstr "plusinf bge.un zero"
	br IL_THROW
IL_39:

	ldarg.s 'plusinf'
	ldarg.s 'minusinf'
	bge.un.s IL_40
	ldstr "plusinf bge.un minusinf"
	br IL_THROW
IL_40:

	ldarg.s 'zero'
	ldarg.s 'nan'
	bge.un.s IL_41
	ldstr "zero bge.un nan"
	br IL_THROW
IL_41:

	ldarg.s 'zero'
	ldarg.s 'plusinf'
	bge.un.s IL_FAIL_42
	br.s IL_42
IL_FAIL_42:
	ldstr "zero bge.un plusinf"
	br IL_THROW
IL_42:

	ldarg.s 'zero'
	ldarg.s 'zero'
	bge.un.s IL_43
	ldstr "zero bge.un zero"
	br IL_THROW
IL_43:

	ldarg.s 'zero'
	ldarg.s 'minusinf'
	bge.un.s IL_44
	ldstr "zero bge.un minusinf"
	br IL_THROW
IL_44:

	ldarg.s 'minusinf'
	ldarg.s 'nan'
	bge.un.s IL_45
	ldstr "minusinf bge.un nan"
	br IL_THROW
IL_45:

	ldarg.s 'minusinf'
	ldarg.s 'plusinf'
	bge.un.s IL_FAIL_46
	br.s IL_46
IL_FAIL_46:
	ldstr "minusinf bge.un plusinf"
	br IL_THROW
IL_46:

	ldarg.s 'minusinf'
	ldarg.s 'zero'
	bge.un.s IL_FAIL_47
	br.s IL_47
IL_FAIL_47:
	ldstr "minusinf bge.un zero"
	br IL_THROW
IL_47:

	ldarg.s 'minusinf'
	ldarg.s 'minusinf'
	bge.un.s IL_48
	ldstr "minusinf bge.un minusinf"
	br IL_THROW
IL_48:

	ldarg.s 'nan'
	ldarg.s 'nan'
	bgt.s IL_FAIL_49
	br.s IL_49
IL_FAIL_49:
	ldstr "nan bgt nan"
	br IL_THROW
IL_49:

	ldarg.s 'nan'
	ldarg.s 'plusinf'
	bgt.s IL_FAIL_50
	br.s IL_50
IL_FAIL_50:
	ldstr "nan bgt plusinf"
	br IL_THROW
IL_50:

	ldarg.s 'nan'
	ldarg.s 'zero'
	bgt.s IL_FAIL_51
	br.s IL_51
IL_FAIL_51:
	ldstr "nan bgt zero"
	br IL_THROW
IL_51:

	ldarg.s 'nan'
	ldarg.s 'minusinf'
	bgt.s IL_FAIL_52
	br.s IL_52
IL_FAIL_52:
	ldstr "nan bgt minusinf"
	br IL_THROW
IL_52:

	ldarg.s 'plusinf'
	ldarg.s 'nan'
	bgt.s IL_FAIL_53
	br.s IL_53
IL_FAIL_53:
	ldstr "plusinf bgt nan"
	br IL_THROW
IL_53:

	ldarg.s 'plusinf'
	ldarg.s 'plusinf'
	bgt.s IL_FAIL_54
	br.s IL_54
IL_FAIL_54:
	ldstr "plusinf bgt plusinf"
	br IL_THROW
IL_54:

	ldarg.s 'plusinf'
	ldarg.s 'zero'
	bgt.s IL_55
	ldstr "plusinf bgt zero"
	br IL_THROW
IL_55:

	ldarg.s 'plusinf'
	ldarg.s 'minusinf'
	bgt.s IL_56
	ldstr "plusinf bgt minusinf"
	br IL_THROW
IL_56:

	ldarg.s 'zero'
	ldarg.s 'nan'
	bgt.s IL_FAIL_57
	br.s IL_57
IL_FAIL_57:
	ldstr "zero bgt nan"
	br IL_THROW
IL_57:

	ldarg.s 'zero'
	ldarg.s 'plusinf'
	bgt.s IL_FAIL_58
	br.s IL_58
IL_FAIL_58:
	ldstr "zero bgt plusinf"
	br IL_THROW
IL_58:

	ldarg.s 'zero'
	ldarg.s 'zero'
	bgt.s IL_FAIL_59
	br.s IL_59
IL_FAIL_59:
	ldstr "zero bgt zero"
	br IL_THROW
IL_59:

	ldarg.s 'zero'
	ldarg.s 'minusinf'
	bgt.s IL_60
	ldstr "zero bgt minusinf"
	br IL_THROW
IL_60:

	ldarg.s 'minusinf'
	ldarg.s 'nan'
	bgt.s IL_FAIL_61
	br.s IL_61
IL_FAIL_61:
	ldstr "minusinf bgt nan"
	br IL_THROW
IL_61:

	ldarg.s 'minusinf'
	ldarg.s 'plusinf'
	bgt.s IL_FAIL_62
	br.s IL_62
IL_FAIL_62:
	ldstr "minusinf bgt plusinf"
	br IL_THROW
IL_62:

	ldarg.s 'minusinf'
	ldarg.s 'zero'
	bgt.s IL_FAIL_63
	br.s IL_63
IL_FAIL_63:
	ldstr "minusinf bgt zero"
	br IL_THROW
IL_63:

	ldarg.s 'minusinf'
	ldarg.s 'minusinf'
	bgt.s IL_FAIL_64
	br.s IL_64
IL_FAIL_64:
	ldstr "minusinf bgt minusinf"
	br IL_THROW
IL_64:

	ldarg.s 'nan'
	ldarg.s 'nan'
	bgt.un.s IL_65
	ldstr "nan bgt.un nan"
	br IL_THROW
IL_65:

	ldarg.s 'nan'
	ldarg.s 'plusinf'
	bgt.un.s IL_66
	ldstr "nan bgt.un plusinf"
	br IL_THROW
IL_66:

	ldarg.s 'nan'
	ldarg.s 'zero'
	bgt.un.s IL_67
	ldstr "nan bgt.un zero"
	br IL_THROW
IL_67:

	ldarg.s 'nan'
	ldarg.s 'minusinf'
	bgt.un.s IL_68
	ldstr "nan bgt.un minusinf"
	br IL_THROW
IL_68:

	ldarg.s 'plusinf'
	ldarg.s 'nan'
	bgt.un.s IL_69
	ldstr "plusinf bgt.un nan"
	br IL_THROW
IL_69:

	ldarg.s 'plusinf'
	ldarg.s 'plusinf'
	bgt.un.s IL_FAIL_70
	br.s IL_70
IL_FAIL_70:
	ldstr "plusinf bgt.un plusinf"
	br IL_THROW
IL_70:

	ldarg.s 'plusinf'
	ldarg.s 'zero'
	bgt.un.s IL_71
	ldstr "plusinf bgt.un zero"
	br IL_THROW
IL_71:

	ldarg.s 'plusinf'
	ldarg.s 'minusinf'
	bgt.un.s IL_72
	ldstr "plusinf bgt.un minusinf"
	br IL_THROW
IL_72:

	ldarg.s 'zero'
	ldarg.s 'nan'
	bgt.un.s IL_73
	ldstr "zero bgt.un nan"
	br IL_THROW
IL_73:

	ldarg.s 'zero'
	ldarg.s 'plusinf'
	bgt.un.s IL_FAIL_74
	br.s IL_74
IL_FAIL_74:
	ldstr "zero bgt.un plusinf"
	br IL_THROW
IL_74:

	ldarg.s 'zero'
	ldarg.s 'zero'
	bgt.un.s IL_FAIL_75
	br.s IL_75
IL_FAIL_75:
	ldstr "zero bgt.un zero"
	br IL_THROW
IL_75:

	ldarg.s 'zero'
	ldarg.s 'minusinf'
	bgt.un.s IL_76
	ldstr "zero bgt.un minusinf"
	br IL_THROW
IL_76:

	ldarg.s 'minusinf'
	ldarg.s 'nan'
	bgt.un.s IL_77
	ldstr "minusinf bgt.un nan"
	br IL_THROW
IL_77:

	ldarg.s 'minusinf'
	ldarg.s 'plusinf'
	bgt.un.s IL_FAIL_78
	br.s IL_78
IL_FAIL_78:
	ldstr "minusinf bgt.un plusinf"
	br IL_THROW
IL_78:

	ldarg.s 'minusinf'
	ldarg.s 'zero'
	bgt.un.s IL_FAIL_79
	br.s IL_79
IL_FAIL_79:
	ldstr "minusinf bgt.un zero"
	br IL_THROW
IL_79:

	ldarg.s 'minusinf'
	ldarg.s 'minusinf'
	bgt.un.s IL_FAIL_80
	br.s IL_80
IL_FAIL_80:
	ldstr "minusinf bgt.un minusinf"
	br IL_THROW
IL_80:

	ldarg.s 'nan'
	ldarg.s 'nan'
	ble.s IL_FAIL_81
	br.s IL_81
IL_FAIL_81:
	ldstr "nan ble nan"
	br IL_THROW
IL_81:

	ldarg.s 'nan'
	ldarg.s 'plusinf'
	ble.s IL_FAIL_82
	br.s IL_82
IL_FAIL_82:
	ldstr "nan ble plusinf"
	br IL_THROW
IL_82:

	ldarg.s 'nan'
	ldarg.s 'zero'
	ble.s IL_FAIL_83
	br.s IL_83
IL_FAIL_83:
	ldstr "nan ble zero"
	br IL_THROW
IL_83:

	ldarg.s 'nan'
	ldarg.s 'minusinf'
	ble.s IL_FAIL_84
	br.s IL_84
IL_FAIL_84:
	ldstr "nan ble minusinf"
	br IL_THROW
IL_84:

	ldarg.s 'plusinf'
	ldarg.s 'nan'
	ble.s IL_FAIL_85
	br.s IL_85
IL_FAIL_85:
	ldstr "plusinf ble nan"
	br IL_THROW
IL_85:

	ldarg.s 'plusinf'
	ldarg.s 'plusinf'
	ble.s IL_86
	ldstr "plusinf ble plusinf"
	br IL_THROW
IL_86:

	ldarg.s 'plusinf'
	ldarg.s 'zero'
	ble.s IL_FAIL_87
	br.s IL_87
IL_FAIL_87:
	ldstr "plusinf ble zero"
	br IL_THROW
IL_87:

	ldarg.s 'plusinf'
	ldarg.s 'minusinf'
	ble.s IL_FAIL_88
	br.s IL_88
IL_FAIL_88:
	ldstr "plusinf ble minusinf"
	br IL_THROW
IL_88:

	ldarg.s 'zero'
	ldarg.s 'nan'
	ble.s IL_FAIL_89
	br.s IL_89
IL_FAIL_89:
	ldstr "zero ble nan"
	br IL_THROW
IL_89:

	ldarg.s 'zero'
	ldarg.s 'plusinf'
	ble.s IL_90
	ldstr "zero ble plusinf"
	br IL_THROW
IL_90:

	ldarg.s 'zero'
	ldarg.s 'zero'
	ble.s IL_91
	ldstr "zero ble zero"
	br IL_THROW
IL_91:

	ldarg.s 'zero'
	ldarg.s 'minusinf'
	ble.s IL_FAIL_92
	br.s IL_92
IL_FAIL_92:
	ldstr "zero ble minusinf"
	br IL_THROW
IL_92:

	ldarg.s 'minusinf'
	ldarg.s 'nan'
	ble.s IL_FAIL_93
	br.s IL_93
IL_FAIL_93:
	ldstr "minusinf ble nan"
	br IL_THROW
IL_93:

	ldarg.s 'minusinf'
	ldarg.s 'plusinf'
	ble.s IL_94
	ldstr "minusinf ble plusinf"
	br IL_THROW
IL_94:

	ldarg.s 'minusinf'
	ldarg.s 'zero'
	ble.s IL_95
	ldstr "minusinf ble zero"
	br IL_THROW
IL_95:

	ldarg.s 'minusinf'
	ldarg.s 'minusinf'
	ble.s IL_96
	ldstr "minusinf ble minusinf"
	br IL_THROW
IL_96:

	ldarg.s 'nan'
	ldarg.s 'nan'
	ble.un.s IL_97
	ldstr "nan ble.un nan"
	br IL_THROW
IL_97:

	ldarg.s 'nan'
	ldarg.s 'plusinf'
	ble.un.s IL_98
	ldstr "nan ble.un plusinf"
	br IL_THROW
IL_98:

	ldarg.s 'nan'
	ldarg.s 'zero'
	ble.un.s IL_99
	ldstr "nan ble.un zero"
	br IL_THROW
IL_99:

	ldarg.s 'nan'
	ldarg.s 'minusinf'
	ble.un.s IL_100
	ldstr "nan ble.un minusinf"
	br IL_THROW
IL_100:

	ldarg.s 'plusinf'
	ldarg.s 'nan'
	ble.un.s IL_101
	ldstr "plusinf ble.un nan"
	br IL_THROW
IL_101:

	ldarg.s 'plusinf'
	ldarg.s 'plusinf'
	ble.un.s IL_102
	ldstr "plusinf ble.un plusinf"
	br IL_THROW
IL_102:

	ldarg.s 'plusinf'
	ldarg.s 'zero'
	ble.un.s IL_FAIL_103
	br.s IL_103
IL_FAIL_103:
	ldstr "plusinf ble.un zero"
	br IL_THROW
IL_103:

	ldarg.s 'plusinf'
	ldarg.s 'minusinf'
	ble.un.s IL_FAIL_104
	br.s IL_104
IL_FAIL_104:
	ldstr "plusinf ble.un minusinf"
	br IL_THROW
IL_104:

	ldarg.s 'zero'
	ldarg.s 'nan'
	ble.un.s IL_105
	ldstr "zero ble.un nan"
	br IL_THROW
IL_105:

	ldarg.s 'zero'
	ldarg.s 'plusinf'
	ble.un.s IL_106
	ldstr "zero ble.un plusinf"
	br IL_THROW
IL_106:

	ldarg.s 'zero'
	ldarg.s 'zero'
	ble.un.s IL_107
	ldstr "zero ble.un zero"
	br IL_THROW
IL_107:

	ldarg.s 'zero'
	ldarg.s 'minusinf'
	ble.un.s IL_FAIL_108
	br.s IL_108
IL_FAIL_108:
	ldstr "zero ble.un minusinf"
	br IL_THROW
IL_108:

	ldarg.s 'minusinf'
	ldarg.s 'nan'
	ble.un.s IL_109
	ldstr "minusinf ble.un nan"
	br IL_THROW
IL_109:

	ldarg.s 'minusinf'
	ldarg.s 'plusinf'
	ble.un.s IL_110
	ldstr "minusinf ble.un plusinf"
	br IL_THROW
IL_110:

	ldarg.s 'minusinf'
	ldarg.s 'zero'
	ble.un.s IL_111
	ldstr "minusinf ble.un zero"
	br IL_THROW
IL_111:

	ldarg.s 'minusinf'
	ldarg.s 'minusinf'
	ble.un.s IL_112
	ldstr "minusinf ble.un minusinf"
	br IL_THROW
IL_112:

	ldarg.s 'nan'
	ldarg.s 'nan'
	blt.s IL_FAIL_113
	br.s IL_113
IL_FAIL_113:
	ldstr "nan blt nan"
	br IL_THROW
IL_113:

	ldarg.s 'nan'
	ldarg.s 'plusinf'
	blt.s IL_FAIL_114
	br.s IL_114
IL_FAIL_114:
	ldstr "nan blt plusinf"
	br IL_THROW
IL_114:

	ldarg.s 'nan'
	ldarg.s 'zero'
	blt.s IL_FAIL_115
	br.s IL_115
IL_FAIL_115:
	ldstr "nan blt zero"
	br IL_THROW
IL_115:

	ldarg.s 'nan'
	ldarg.s 'minusinf'
	blt.s IL_FAIL_116
	br.s IL_116
IL_FAIL_116:
	ldstr "nan blt minusinf"
	br IL_THROW
IL_116:

	ldarg.s 'plusinf'
	ldarg.s 'nan'
	blt.s IL_FAIL_117
	br.s IL_117
IL_FAIL_117:
	ldstr "plusinf blt nan"
	br IL_THROW
IL_117:

	ldarg.s 'plusinf'
	ldarg.s 'plusinf'
	blt.s IL_FAIL_118
	br.s IL_118
IL_FAIL_118:
	ldstr "plusinf blt plusinf"
	br IL_THROW
IL_118:

	ldarg.s 'plusinf'
	ldarg.s 'zero'
	blt.s IL_FAIL_119
	br.s IL_119
IL_FAIL_119:
	ldstr "plusinf blt zero"
	br IL_THROW
IL_119:

	ldarg.s 'plusinf'
	ldarg.s 'minusinf'
	blt.s IL_FAIL_120
	br.s IL_120
IL_FAIL_120:
	ldstr "plusinf blt minusinf"
	br IL_THROW
IL_120:

	ldarg.s 'zero'
	ldarg.s 'nan'
	blt.s IL_FAIL_121
	br.s IL_121
IL_FAIL_121:
	ldstr "zero blt nan"
	br IL_THROW
IL_121:

	ldarg.s 'zero'
	ldarg.s 'plusinf'
	blt.s IL_122
	ldstr "zero blt plusinf"
	br IL_THROW
IL_122:

	ldarg.s 'zero'
	ldarg.s 'zero'
	blt.s IL_FAIL_123
	br.s IL_123
IL_FAIL_123:
	ldstr "zero blt zero"
	br IL_THROW
IL_123:

	ldarg.s 'zero'
	ldarg.s 'minusinf'
	blt.s IL_FAIL_124
	br.s IL_124
IL_FAIL_124:
	ldstr "zero blt minusinf"
	br IL_THROW
IL_124:

	ldarg.s 'minusinf'
	ldarg.s 'nan'
	blt.s IL_FAIL_125
	br.s IL_125
IL_FAIL_125:
	ldstr "minusinf blt nan"
	br IL_THROW
IL_125:

	ldarg.s 'minusinf'
	ldarg.s 'plusinf'
	blt.s IL_126
	ldstr "minusinf blt plusinf"
	br IL_THROW
IL_126:

	ldarg.s 'minusinf'
	ldarg.s 'zero'
	blt.s IL_127
	ldstr "minusinf blt zero"
	br IL_THROW
IL_127:

	ldarg.s 'minusinf'
	ldarg.s 'minusinf'
	blt.s IL_FAIL_128
	br.s IL_128
IL_FAIL_128:
	ldstr "minusinf blt minusinf"
	br IL_THROW
IL_128:

	ldarg.s 'nan'
	ldarg.s 'nan'
	blt.un.s IL_129
	ldstr "nan blt.un nan"
	br IL_THROW
IL_129:

	ldarg.s 'nan'
	ldarg.s 'plusinf'
	blt.un.s IL_130
	ldstr "nan blt.un plusinf"
	br IL_THROW
IL_130:

	ldarg.s 'nan'
	ldarg.s 'zero'
	blt.un.s IL_131
	ldstr "nan blt.un zero"
	br IL_THROW
IL_131:

	ldarg.s 'nan'
	ldarg.s 'minusinf'
	blt.un.s IL_132
	ldstr "nan blt.un minusinf"
	br IL_THROW
IL_132:

	ldarg.s 'plusinf'
	ldarg.s 'nan'
	blt.un.s IL_133
	ldstr "plusinf blt.un nan"
	br IL_THROW
IL_133:

	ldarg.s 'plusinf'
	ldarg.s 'plusinf'
	blt.un.s IL_FAIL_134
	br.s IL_134
IL_FAIL_134:
	ldstr "plusinf blt.un plusinf"
	br IL_THROW
IL_134:

	ldarg.s 'plusinf'
	ldarg.s 'zero'
	blt.un.s IL_FAIL_135
	br.s IL_135
IL_FAIL_135:
	ldstr "plusinf blt.un zero"
	br IL_THROW
IL_135:

	ldarg.s 'plusinf'
	ldarg.s 'minusinf'
	blt.un.s IL_FAIL_136
	br.s IL_136
IL_FAIL_136:
	ldstr "plusinf blt.un minusinf"
	br IL_THROW
IL_136:

	ldarg.s 'zero'
	ldarg.s 'nan'
	blt.un.s IL_137
	ldstr "zero blt.un nan"
	br IL_THROW
IL_137:

	ldarg.s 'zero'
	ldarg.s 'plusinf'
	blt.un.s IL_138
	ldstr "zero blt.un plusinf"
	br IL_THROW
IL_138:

	ldarg.s 'zero'
	ldarg.s 'zero'
	blt.un.s IL_FAIL_139
	br.s IL_139
IL_FAIL_139:
	ldstr "zero blt.un zero"
	br IL_THROW
IL_139:

	ldarg.s 'zero'
	ldarg.s 'minusinf'
	blt.un.s IL_FAIL_140
	br.s IL_140
IL_FAIL_140:
	ldstr "zero blt.un minusinf"
	br IL_THROW
IL_140:

	ldarg.s 'minusinf'
	ldarg.s 'nan'
	blt.un.s IL_141
	ldstr "minusinf blt.un nan"
	br IL_THROW
IL_141:

	ldarg.s 'minusinf'
	ldarg.s 'plusinf'
	blt.un.s IL_142
	ldstr "minusinf blt.un plusinf"
	br IL_THROW
IL_142:

	ldarg.s 'minusinf'
	ldarg.s 'zero'
	blt.un.s IL_143
	ldstr "minusinf blt.un zero"
	br IL_THROW
IL_143:

	ldarg.s 'minusinf'
	ldarg.s 'minusinf'
	blt.un.s IL_FAIL_144
	br.s IL_144
IL_FAIL_144:
	ldstr "minusinf blt.un minusinf"
	br IL_THROW
IL_144:

	ldarg.s 'nan'
	ldarg.s 'nan'
	bne.un.s IL_145
	ldstr "nan bne.un nan"
	br IL_THROW
IL_145:

	ldarg.s 'nan'
	ldarg.s 'plusinf'
	bne.un.s IL_146
	ldstr "nan bne.un plusinf"
	br IL_THROW
IL_146:

	ldarg.s 'nan'
	ldarg.s 'zero'
	bne.un.s IL_147
	ldstr "nan bne.un zero"
	br IL_THROW
IL_147:

	ldarg.s 'nan'
	ldarg.s 'minusinf'
	bne.un.s IL_148
	ldstr "nan bne.un minusinf"
	br IL_THROW
IL_148:

	ldarg.s 'plusinf'
	ldarg.s 'nan'
	bne.un.s IL_149
	ldstr "plusinf bne.un nan"
	br IL_THROW
IL_149:

	ldarg.s 'plusinf'
	ldarg.s 'plusinf'
	bne.un.s IL_FAIL_150
	br.s IL_150
IL_FAIL_150:
	ldstr "plusinf bne.un plusinf"
	br IL_THROW
IL_150:

	ldarg.s 'plusinf'
	ldarg.s 'zero'
	bne.un.s IL_151
	ldstr "plusinf bne.un zero"
	br IL_THROW
IL_151:

	ldarg.s 'plusinf'
	ldarg.s 'minusinf'
	bne.un.s IL_152
	ldstr "plusinf bne.un minusinf"
	br IL_THROW
IL_152:

	ldarg.s 'zero'
	ldarg.s 'nan'
	bne.un.s IL_153
	ldstr "zero bne.un nan"
	br IL_THROW
IL_153:

	ldarg.s 'zero'
	ldarg.s 'plusinf'
	bne.un.s IL_154
	ldstr "zero bne.un plusinf"
	br IL_THROW
IL_154:

	ldarg.s 'zero'
	ldarg.s 'zero'
	bne.un.s IL_FAIL_155
	br.s IL_155
IL_FAIL_155:
	ldstr "zero bne.un zero"
	br IL_THROW
IL_155:

	ldarg.s 'zero'
	ldarg.s 'minusinf'
	bne.un.s IL_156
	ldstr "zero bne.un minusinf"
	br IL_THROW
IL_156:

	ldarg.s 'minusinf'
	ldarg.s 'nan'
	bne.un.s IL_157
	ldstr "minusinf bne.un nan"
	br IL_THROW
IL_157:

	ldarg.s 'minusinf'
	ldarg.s 'plusinf'
	bne.un.s IL_158
	ldstr "minusinf bne.un plusinf"
	br IL_THROW
IL_158:

	ldarg.s 'minusinf'
	ldarg.s 'zero'
	bne.un.s IL_159
	ldstr "minusinf bne.un zero"
	br IL_THROW
IL_159:

	ldarg.s 'minusinf'
	ldarg.s 'minusinf'
	bne.un.s IL_FAIL_160
	br.s IL_160
IL_FAIL_160:
	ldstr "minusinf bne.un minusinf"
	br IL_THROW
IL_160:

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
  	ldc.r4     (00 00 C0 FF)
  	ldc.r4     (00 00 80 7F)
  	ldc.r4     0
	ldc.r4     (00 00 80 FF)
	call       void JitTest_cond32_NaN_il.Test::RunTests(float32,
                                           float32,
                                           float32,
                                           float32)
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
