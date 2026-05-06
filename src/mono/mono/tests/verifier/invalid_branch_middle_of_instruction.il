
.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.assembly 'prefix_test'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.module cmmp.exe


.class ClassA extends [mscorlib]System.Object
{
    .field public int32 valid

	.method public hidebysig  specialname  rtspecialname instance default void .ctor ()  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method public virtual void VirtTest ()
	{
		ret
	}
}

.class public Template`1<T>
  	extends [mscorlib]System.Object
{
}

.class sealed public StructTemplate`1<T>
  	extends [mscorlib]System.ValueType
{
	.field public !0 t
}

.class sealed public StructTemplate2`1<T>
  	extends [mscorlib]System.ValueType
{
	.field public !0 t
}


.class public auto ansi sealed MyStruct
  	extends [mscorlib]System.ValueType
{
	.field public int32 foo
	.field public static int32 stFoo
	.field public native int ptr

	.method public static void Test ()
	{
		ret
	}

	.method public static void Test (int32&)
	{
		ret
	}
}


.class public auto ansi sealed MyEnum
  	extends [mscorlib]System.Enum
{
    .field public specialname  rtspecialname  int32 value__
    .field public static  literal  valuetype MyEnum B = int32(0x00000000)
    .field public static  literal  valuetype MyEnum C = int32(0x00000001)
}

.method public static void TestMethod ()
{
	.maxstack 8
	.locals init (int32 V_0)

	ldloca 0

	newobj instance void object::.ctor()
	callvirt instance int32 object::GetHashCode()
	brtrue MIDDLE

	
	volatile.
MIDDLE:
	ldind.i4
AFTER:

	leave END
END:
	
	ret 
}


.method public static int32 Main ()
{
	.entrypoint
	.maxstack 8
	.locals init ()

	call void TestMethod()

	leave END
END:
	ldc.i4.0
	ret 
}

