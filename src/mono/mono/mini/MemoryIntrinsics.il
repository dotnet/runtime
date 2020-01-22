.assembly extern mscorlib
{
	.publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
	.ver 4:0:0:0
}

.assembly 'MemoryIntrinsics'
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78  63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )
	.hash algorithm 0x00008004
	.ver 0:0:0:0
}
.module 'instrics-lib.dll'
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003
.corflags 0x00000001

.namespace Mono {
	.class public abstract auto ansi sealed beforefieldinit Intrinsics extends [mscorlib]System.Object
	{
		.method public hidebysig static void  Cpobj<T>(void* 'to', void* from) cil managed
		{
			ldarg.0
			ldarg.1
			cpobj !!T
			ret
		}

		.method public hidebysig static !!T Ldobj<T>(void* 'from') cil managed
		{
			ldarg.0
			ldobj !!T
			ret
		}

		.method public hidebysig static void Stobj<T>(void* 'to', !!T 'value') cil managed
		{
			ldarg.0
			ldarg.1
			stobj !!T
			ret
		}

		.method public hidebysig static void LdobjStObjPair<T>(void* 'to', void* 'from') cil managed
		{
			ldarg.0
			ldarg.1
			ldobj !!T
			stobj !!T
			ret
		}

		.method public hidebysig static void Cpblk(void* 'to', void* 'from', int32 size) cil managed
		{
			ldarg.0
			ldarg.1
			ldarg.2
			cpblk
			ret
		}

		.method public hidebysig static void Initblk<T>(void* 'to', int32 'value', int32 'size') cil managed
		{
			ldarg.0
			ldarg.1
			ldarg.2
			initblk
			ret
		}

		//Unaligned intrinsics
		.method public hidebysig static void UnalignedCpobj<T>(void* 'to', void* from) cil managed
		{
			ldarg.0
			ldarg.1
			unaligned. 1
			cpobj !!T
			ret
		}

		.method public hidebysig static !!T UnalignedLdobj<T>(void* 'from') cil managed
		{
			ldarg.0
			unaligned. 1
			ldobj !!T
			ret
		}

		.method public hidebysig static void UnalignedStobj<T>(void* 'to', !!T 'value') cil managed
		{
			ldarg.0
			ldarg.1
			unaligned. 1
			stobj !!T
			ret
		}

		.method public hidebysig static void UnalignedLdobjStObjPair<T>(void* 'to', void* 'from') cil managed
		{
			ldarg.0
			ldarg.1
			unaligned. 1
			ldobj !!T
			stobj !!T
			ret
		}

		.method public hidebysig static void UnalignedCpblk(void* 'to', void* 'from', int32 size) cil managed
		{
			ldarg.0
			ldarg.1
			ldarg.2
			unaligned. 1
			cpblk
			ret
		}

		.method public hidebysig static void UnalignedInit(void* 'to', int32 'value', int32 'size') cil managed
		{
			ldarg.0
			ldarg.1
			ldarg.2
			unaligned. 1
			initblk
			ret
		}

		//Unaligned ldind
		.method public hidebysig static int16 UnalignedLdInd2(void* 'from') cil managed
		{
			ldarg.0
			unaligned. 1
			ldind.i2
			ret
		}

		.method public hidebysig static int32 UnalignedLdInd4(void* 'from') cil managed
		{
			ldarg.0
			unaligned. 1
			ldind.i4
			ret
		}

		.method public hidebysig static int64 UnalignedLdInd8(void* 'from') cil managed
		{
			ldarg.0
			unaligned. 1
			ldind.i8
			ret
		}

		.method public hidebysig static float32 UnalignedLdIndR4(void* 'from') cil managed
		{
			ldarg.0
			unaligned. 1
			ldind.r4
			ret
		}

		.method public hidebysig static float64 UnalignedLdIndR8(void* 'from') cil managed
		{
			ldarg.0
			unaligned. 1
			ldind.r8
			ret
		}

		.method public hidebysig static native int UnalignedLdIndI(void* 'from') cil managed
		{
			ldarg.0
			unaligned. 1
			ldind.i
			ret
		}

	}
}
