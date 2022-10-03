
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern legacy library mscorlib {}
.assembly 'Object-Pin'
{
}



.class auto ansi Space
{
  .field public int32[] foo
  .method public specialname rtspecialname instance void .ctor() il managed
  {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class auto ansi Consume
{
  .field private class Space[] junk
  .field public int32 yucko
  .method public specialname rtspecialname instance void .ctor(int32 size) il managed
  {
    .maxstack  3
    .locals (int32 V_0)

    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ldarg.0
    ldarg.1
    newarr     Space
    stfld      class Space[] Consume::junk
    ldc.i4.0
    stloc.0
    br.s       LOOP1
LOOP2:
  ldarg.0
    ldfld      class Space[] Consume::junk
    ldloc.0
    newobj     instance void Space::.ctor()
    stelem.ref
    ldarg.0
    ldfld      class Space[] Consume::junk
    ldloc.0
    ldelem.ref
    ldc.i4     0x100
    newarr     [mscorlib]System.Int32
    stfld      int32[] Space::foo
    ldarg.0
    ldfld      class Space[] Consume::junk
    ldloc.0
    ldelem.ref
    ldfld      int32[] Space::foo
    ldc.i4.0
    ldarg.1
    stelem.i4
    ldarg.0
    ldfld      class Space[] Consume::junk
    ldloc.0
    ldelem.ref
    ldfld      int32[] Space::foo
    ldc.i4     0xff
    ldarg.1
    stelem.i4
    ldloc.0
    ldc.i4.1
    add
    stloc.0
LOOP1:
    ldloc.0
    ldarg.1
    blt.s      LOOP2
    ret
  }
}


.class auto ansi Pin1_Test
{
  .method public static pinvokeimpl("mirror" winapi) int32 Ret_Ptr(void *) il managed preservesig{ }
  .method public static pinvokeimpl("mirror" winapi) void Set_Val(void*,int32 c) il managed preservesig { }
  .method public static int32 Main(class [mscorlib]System.String[] args) il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  400
    ldstr "Calling Not_Pinned() to assure that GC actually moves things"
    call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    call       int32 Pin1_Test::Not_Pinned()
    brtrue FAILURE1
SOFT1:
    ldstr "Calling Pinned() to assure that GC doesnt move things that are pinned"
    call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    call       int32 Pin1_Test::Pinned()
    brtrue FAILURE2
    ldstr "Trying a field assignment to a pinned object from native(with GC)"
    call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    call       int32 Pin1_Test::Pinned_Test_Assign()
    brtrue FAILURE3
    ldstr "Yippie, everything worked as advertised"
    call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    ldc.i4 0x64
    ret

FAILURE1:
    ldstr "Something is wrong, things didnt move! Maybe you should set GCSTRESS to 1"
    call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	br SOFT1
FAILURE2:
    ldstr "Something is wrong, things moved! Pinned must not be working"
    call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    ldc.i4 1
    ret
FAILURE3:
    ldstr "Something is wrong, the assignment didnt work!"
    call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    ldc.i4 1
    ret
  }

  .method public static int32 Not_Pinned() il managed
  {
    .maxstack  10
    .locals (class Consume, class Consume , class Consume, int32, int32 )

    call       void [mscorlib]System.GC::Collect()
    ldc.i4     256
    newobj     instance void Consume::.ctor(int32)
    stloc.0

    ldc.i4     2
    newobj     instance void Consume::.ctor(int32)
    stloc.1

    ldloc 1
    conv.i
    call       int32 Pin1_Test::Ret_Ptr(void*)
    stloc 3

    ldc.i4     2
    newobj     instance void Consume::.ctor(int32)
    stloc.2

    ldloc 2
    conv.i
    call       int32 Pin1_Test::Ret_Ptr(void*)
    stloc 4

    call       void [mscorlib]System.GC::Collect()

    ldnull
    stloc.0

    call       void [mscorlib]System.GC::Collect()
    ldloc 1
    conv.i
    call       int32 Pin1_Test::Ret_Ptr(void*)
    ldloc 3
    conv.i
    beq AA_1

SOFT2:
    ldloc 2
    conv.i
    call       int32 Pin1_Test::Ret_Ptr(void*)
    ldloc 4
    conv.i
    beq AA_2

SOFT3:
    ldc.i4 0
    br AA_3

AA_1:
    ldstr "Something is wrong, 1st one didnt move (Not_Pinned)"
    call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    br SOFT2

AA_2:
    ldstr "Something is wrong, 2nd one didnt move(Not_Pinned)"
    call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	br SOFT3
AA_3:
    ret
  }

  .method public static int32 Pinned() il managed
  {
    .maxstack  10
    .locals (class Consume, class Consume, class Consume pinned, int32, int32 )

    call       void [mscorlib]System.GC::Collect()
    ldc.i4     256
    newobj     instance void Consume::.ctor(int32)
    stloc.0

    ldc.i4     2
    newobj     instance void Consume::.ctor(int32)
    stloc.1

    ldloc 1
    conv.i
    call       int32 Pin1_Test::Ret_Ptr(void*)
    stloc 3

    ldc.i4     2
    newobj     instance void Consume::.ctor(int32)
    stloc.2

    ldloc 2
    conv.i
    call       int32 Pin1_Test::Ret_Ptr(void*)
    stloc 4

    call       void [mscorlib]System.GC::Collect()

    ldnull
    stloc.0

    call       void [mscorlib]System.GC::Collect()
    ldloc 1
    conv.i
    call       int32 Pin1_Test::Ret_Ptr(void*)
    ldloc 3
    conv.i
    beq AA_1

SOFT4:
    ldloc 2
    conv.i
    call       int32 Pin1_Test::Ret_Ptr(void*)
    ldloc 4
    conv.i
    beq AA_2

    ldstr "Something is wrong, 2nd one moved!(Pinned)"
    call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    ldc.i4 1
    br AA_3

AA_1:
    ldstr "Something is wrong, 1st one didnt move(Pinned)"
    call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    br SOFT4

AA_2:
    ldc.i4 0

AA_3:
    ret
  }
  .method public static int32 Pinned_Test_Assign() il managed
  {
    .maxstack  10
    .locals (class Consume, class Consume, class Consume pinned, int32, int32, int32 )

    call       void [mscorlib]System.GC::Collect()
    ldc.i4     256
    newobj     instance void Consume::.ctor(int32)
    stloc.0

    ldc.i4     2
    newobj     instance void Consume::.ctor(int32)
    stloc.1

    ldloc 1
    conv.i
    call       int32 Pin1_Test::Ret_Ptr(void*)
    stloc 3

    ldc.i4     2
    newobj     instance void Consume::.ctor(int32)
    stloc.2

    ldloc 2
    conv.i
    call       int32 Pin1_Test::Ret_Ptr(void*)
    stloc 4

    call       void [mscorlib]System.GC::Collect()

    ldnull
    stloc.0

    ldc.i4 55
    stloc 5
    ldloc 2
    ldloc 5
    stfld      int32 Consume::yucko

    ldloc 2
    conv.i
    ldflda      int32 Consume::yucko
    ldc.i4 66
    call       void Pin1_Test::Set_Val(void*,int32)
    ldloc 2
    ldfld      int32 Consume::yucko
    ldc.i4 66
    beq Yippie1
    ldc.i4 1
    ret
Yippie1:
    call       void [mscorlib]System.GC::Collect()
    ldloc 2
    ldloc 5
    stfld      int32 Consume::yucko

    ldloc 2
    conv.i
    ldflda      int32 Consume::yucko
    ldc.i4 88
    call       void Pin1_Test::Set_Val(void*,int32)
    ldloc 2
    ldfld      int32 Consume::yucko
    ldc.i4 88
    beq Yippie2
    ldc.i4 1
    ret
Yippie2:
    call       void [mscorlib]System.GC::Collect()
    ldloc 1
    conv.i
    call       int32 Pin1_Test::Ret_Ptr(void*)
    ldloc 3
    conv.i
    beq AA_1

SOFT5:
    ldloc 2
    conv.i
    call       int32 Pin1_Test::Ret_Ptr(void*)
    ldloc 4
    conv.i
    beq AA_2

    ldstr "Something is wrong, 2nd one moved!(Pinned)"
    call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    ldc.i4 1
    br AA_3

AA_1:
    ldstr "Something is wrong, 1st one didnt move(Pinned)"
    call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    br SOFT5

AA_2:
    ldc.i4 0

AA_3:
    ret
  }
}

