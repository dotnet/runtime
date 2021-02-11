
.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.assembly 'ldobj_test'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.module ldobj.exe


.class Class extends [mscorlib]System.Object
{
    .field public int32 valid
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
}


.class public auto ansi sealed MyStruct2
  	extends [mscorlib]System.ValueType
{
	.field public int32 foo
}

.method public static int32 Main ()
{
	.entrypoint
	.maxstack 8
	.locals init (int32 V_0, int32& V_1 , int32 V_2)

	ldloca.s 2
	stloc.1

	ldloc.1
	ldloc.0

	.emitbyte 0x81
	.emitbyte 0x01
	.emitbyte 0x00
	.emitbyte 0x00
	.emitbyte 0x06



	ldc.i4.0
	ret 
}

