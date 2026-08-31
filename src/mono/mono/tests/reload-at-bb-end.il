.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'out'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'out' // GUID = {D45045F2-EC64-4624-8552-EB20DDE3767F}


  .class private auto ansi sealed T
        extends [mscorlib]System.ValueType
  {
    .field  public   int32 a
    .field  public   int32 b

    // method line 1
    .method public hidebysig  specialname  rtspecialname
           instance default void .ctor (int32 a, int32 b)  cil managed
    {
        // Method begins at RVA 0x20ec
        // Code size 15 (0xf)
        .maxstack 8
        IL_0000:  ldarg.0
        IL_0001:  ldarg.2
        IL_0002:  stfld  int32 T::b
        IL_0007:  ldarg.0
        IL_0008:  ldarg.1
        IL_0009:  stfld  int32 T::a
        IL_000e:  ret
    } // end of method T::instance default void .ctor (int32 a, int32 b)

  } // end of class T

  .class private auto ansi abstract M
        extends [mscorlib]System.Object
  {

    // method line 2
    .method private static  hidebysig
           default void foo (valuetype T _N_u470, bool _N_u471)  cil managed
    {
        // Method begins at RVA 0x20fc
        // Code size 1 (0x1)
        .maxstack 8
        IL_0000:  ret
    } // end of method M::default void foo (valuetype T _N_u470, bool _N_u471)

    // method line 3
    .method private static  hidebysig
           default void IsBetterType (object t1, object t2)  cil managed
    {
        // Method begins at RVA 0x2100
        // Code size 42 (0x2a)
        .maxstack 6
        .locals init (
                valuetype T     V_0)
        IL_0000:  ldc.i4.3
        IL_0001:  ldc.i4.4
        IL_0002:  newobj instance void valuetype T::.ctor(int32, int32)
        IL_0007:  stloc.0
        IL_0008:  ldarg.0
        IL_0009:  ldarg.1
        IL_000a:  call bool class M::_N_real_check750(object, object)
        IL_000f:  pop
        IL_0010:  ldloc.0
        IL_0011:  ldarg.0
        IL_0012:  ldarg.1
        IL_0013:  call bool class M::_N_real_check750(object, object)
        IL_0018:  brfalse IL_0023

        IL_001d:  ldc.i4.1
        IL_001e:  br IL_0024

        IL_0023:  ldc.i4.1
        IL_0024:  call void class M::foo(valuetype T, bool)
        IL_0029:  ret
    } // end of method M::default void IsBetterType (object t1, object t2)

    // method line 4
    .method private static  hidebysig
           default void Main ()  cil managed
    {
        // Method begins at RVA 0x2138
        .entrypoint
        // Code size 16 (0x10)
        .maxstack 8
        IL_0000:  newobj instance void object::.ctor()
        IL_0005:  newobj instance void object::.ctor()
        IL_000a:  call void class M::IsBetterType(object, object)
        IL_000f:  ret
    } // end of method M::default void Main ()

    // method line 5
    .method private static  hidebysig
           default bool _N_real_check750 (object t1, object t2)  cil managed
    {
        // Method begins at RVA 0x214c
        // Code size 14 (0xe)
        .maxstack 8
        IL_0000:  ldarg.0
        IL_0001:  call void class [mscorlib]System.Console::WriteLine(object)
        IL_0006:  ldarg.1
        IL_0007:  call void class [mscorlib]System.Console::WriteLine(object)
        IL_000c:  ldc.i4.1
        IL_000d:  ret
    } // end of method M::default bool _N_real_check750 (object t1, object t2)

  } // end of class M
