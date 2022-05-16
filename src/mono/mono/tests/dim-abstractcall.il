.assembly abstractcalls { }

.class interface public abstract auto ansi I1
{
  .method public hidebysig newslot virtual abstract
          instance int32 Add(int32 x) cil managed
  {
  }
}

.class public abstract auto ansi C1
  extends [mscorlib]System.Object
  implements I1
{
  .method public hidebysig specialname rtspecialname 
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig newslot virtual abstract
          instance int32 Remove(int32 x) cil managed
  {
  }
}

.class public auto ansi C2
  extends [mscorlib]System.Object
  implements I1
{
  .method public hidebysig specialname rtspecialname 
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void C1::.ctor()
    ret
  }

  .method public hidebysig virtual
          instance int32 Remove(int32 x) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig newslot virtual
          instance int32 Add(int32 x) cil managed
   {
    ldc.i4.5
    ret
  }
}

.method private hidebysig static void CallClass() cil managed
{
  newobj instance void C2::.ctor()
  ldc.i4.0
  call instance int32 C1::Remove(int32)
  pop
  ret
}

.method private hidebysig static void CallInterface() cil managed
{
  newobj instance void C2::.ctor()
  ldc.i4.0
  call instance int32 I1::Add(int32)
  pop
  ret
}

.method private hidebysig static int32 Main() cil managed
{
  .entrypoint

  .try
  {
    call void CallClass()
    leave Fail
  }
  catch [System.Runtime]System.BadImageFormatException
  {
      leave AbstractClassOK
  }

AbstractClassOK:

  .try
  {
    call void CallInterface()
    leave Fail
  }
  catch [System.Runtime]System.BadImageFormatException
  {
    leave AbstractInterfaceOK
  }

AbstractInterfaceOK:
  ldc.i4 0
  ret

Fail:
	ldc.i4 155
    ret
}

