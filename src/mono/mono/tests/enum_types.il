//
// enum_types.il: Tests for all the primitive types an enum basetype can have
// 
// It just initialize enum values, as this is enouth to cause the type to be loaded

.assembly extern mscorlib
{
	.ver 2:0:0:0
	.publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'd'
{
	.hash algorithm 0x00008004
	.ver	0:0:0:0
}
.module d.exe


.namespace Example
{
	.class public auto ansi sealed Int8Enum
		extends [mscorlib]System.Enum
	{
		.field public specialname rtspecialname int8 value__
		.field public static literal valuetype Example.Int8Enum A = int8(0x00000000)
	}

	.class public auto ansi sealed UInt8Enum
		extends [mscorlib]System.Enum
	{
		.field public specialname rtspecialname uint8 value__
		.field public static literal valuetype Example.Int8Enum A = uint8(0x00000000)
	}

	.class public auto ansi sealed BoolEnum
		extends [mscorlib]System.Enum
	{
		.field public specialname rtspecialname bool value__
		.field public static literal valuetype Example.BoolEnum A = int8(0x00000000)
	}

	.class public auto ansi sealed Int16Enum
		extends [mscorlib]System.Enum
	{
		.field public specialname rtspecialname int16 value__
		.field public static literal valuetype Example.Int16Enum A = int16(0x00000000)
	}

	.class public auto ansi sealed UInt16Enum
		extends [mscorlib]System.Enum
	{
		.field public specialname rtspecialname uint16 value__
		.field public static literal valuetype Example.UInt16Enum A = uint16(0x00000000)
	}

	.class public auto ansi sealed CharEnum
		extends [mscorlib]System.Enum
	{
		.field public specialname rtspecialname char value__
		.field public static literal valuetype Example.CharEnum A = char(0x00000000)
	}

	.class public auto ansi sealed Int32Enum
		extends [mscorlib]System.Enum
	{
		.field public specialname rtspecialname int32 value__
		.field public static literal valuetype Example.Int32Enum A = int32(0x00000000)
	}

	.class public auto ansi sealed UInt32Enum
		extends [mscorlib]System.Enum
	{
		.field public specialname rtspecialname uint32 value__
		.field public static literal valuetype Example.UInt32Enum A = uint32(0x00000000)
	}

	.class public auto ansi sealed Int64Enum
		extends [mscorlib]System.Enum
	{
		.field public specialname rtspecialname int64 value__
		.field public static literal valuetype Example.Int64Enum A = int64(0x00000000)
	}

	.class public auto ansi sealed UInt64Enum
		extends [mscorlib]System.Enum
	{
		.field public specialname rtspecialname uint64 value__
		.field public static literal valuetype Example.UInt32Enum A = uint64(0x00000000)
	}


	.class public auto ansi sealed IntPtrEnum
		extends [mscorlib]System.Enum
	{
		.field public specialname rtspecialname native int value__
		.field public static literal valuetype Example.IntPtrEnum A = int32(0x00000000)
	}

	.class public auto ansi sealed UIntPtrEnum
		extends [mscorlib]System.Enum
	{
		.field public specialname rtspecialname native unsigned int value__
		.field public static literal valuetype Example.UIntPtrEnum A = int32(0x00000000)
	}

	.class public auto ansi beforefieldinit Bla
		extends [mscorlib]System.Object
	{
		.method public hidebysig specialname rtspecialname 
			instance default void .ctor () cil managed 
		{
			.maxstack 8
			ldarg.0 
			call instance void object::.ctor()
			ret 
		}
	
		.method private static	hidebysig 
			 default int32 Main (string[] args)	cil managed 
		{
			.entrypoint
			.maxstack 2
			.locals init (
				valuetype Example.Int8Enum		V_0,
				valuetype Example.UInt8Enum		V_1,
				valuetype Example.BoolEnum		V_2,

				valuetype Example.Int16Enum		V_3,
				valuetype Example.UInt16Enum 	V_4,
				valuetype Example.CharEnum		V_5,
				
				valuetype Example.Int32Enum		V_6,
				valuetype Example.UInt32Enum 	V_7,

				valuetype Example.Int64Enum		V_8,
				valuetype Example.UInt64Enum 	V_9,
				
				valuetype Example.IntPtrEnum	V_10,
				valuetype Example.UIntPtrEnum 	V_11
				)

			ldc.i4.0
			stloc.0

			ldc.i4.0
			stloc.1

			ldc.i4.0
			stloc.2

			ldc.i4.0
			stloc.3

			ldc.i4.0
			stloc.s 4

			ldc.i4.0
			stloc.s 5

			ldc.i4.0
			stloc.s 6

			ldc.i4.0
			stloc.s 7

			ldc.i8 0
			stloc.s 8

			ldc.i8 0
			stloc.s 9

			ldc.i4.0
			stloc.s 10

			ldc.i4.0
			stloc.s 11

			ldc.i4.0
			ret
		}
	
	}
	

}

