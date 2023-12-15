.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly legacy library ASSEMBLY_NAME { }
.class public auto ansi beforefieldinit calli2
       extends [mscorlib]System.Object
{
  .field private static int32 a
  .field private static int32 b
  .method public static int32 f1()
  {
    ldsfld     int32 calli2::a
    ret
  }
  .method public static int32 f2()
  {
    ldsfld     int32 calli2::b
    ret
  }
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1)
    IL_0000:  ldftn int32 calli2::f1()
	      calli int32 ()
    IL_0005:  ldftn int32 calli2::f2()
	      calli int32 ()	
    IL_000a:  beq.s      IL_0013

    IL_000c:  ldftn int32 calli2::f1()
	      calli int32 ()
    IL_0011:  br.s       IL_0018

    IL_0013:  ldftn int32 calli2::f2()
	      calli int32 ()
    IL_0018:  stloc.0
    IL_0019:  ldloc.0
    IL_001a:  stloc.1
    IL_001b:  br.s       IL_001d

    IL_001d:  ldloc.1
		ldc.i4 100
		add
    IL_001e:  ret
  }
}
