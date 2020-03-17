.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'ref-return'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'ref-return.dll' // GUID = {09E5E448-CA90-49A5-AFBA-0AA241F72FBF}


.class private auto ansi beforefieldinit Test extends [mscorlib]System.Object
{
	.field  private static  int32 X
	.field  private static  int32 Y

	.method private static hidebysig  default int32& invalid_branch1 (bool b)  cil managed 
	{
		.maxstack 1
		.locals init (int32&, int32)
		ldarg.0 
		brtrue.s LABEL_1

		ldsflda int32 Test::Y
		br.s LABEL_2

LABEL_1:
		ldloca 1

LABEL_2:
		stloc.0 

		ldloc.0 
		ret 
	}

	.method private static hidebysig  default int32& invalid_branch2 (bool b)  cil managed 
	{
		.maxstack 1
		.locals init (int32&, int32)
		ldarg.0 
		brtrue.s LABEL_1

		ldloca 1
		br.s LABEL_2

LABEL_1:
		ldsflda int32 Test::X

LABEL_2:
		stloc.0 

		ldloc.0 
		ret 
	}
}

