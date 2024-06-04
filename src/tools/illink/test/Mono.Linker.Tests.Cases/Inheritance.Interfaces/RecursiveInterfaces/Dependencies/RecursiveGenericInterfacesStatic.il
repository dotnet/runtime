.assembly extern mscorlib { }

.assembly 'library' { }

.class interface public auto ansi abstract beforefieldinit IBase`3<T, U, V>
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
        01 00 01 00 00
    )
    .param type T
        .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
            01 00 02 00 00
        )
    .param type U
        .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
            01 00 02 00 00
        )
    .param type V
        .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
            01 00 02 00 00
        )
    // Methods
    .method public hidebysig newslot abstract virtual 
        static !T GetT () cil managed 
    {
    } // end of method IBase`3::GetT

    .method public hidebysig newslot virtual 
        static !U GetU () cil managed 
    {
        // Method begins at RVA 0x2050
        // Code size 10 (0xa)
        .maxstack 1
        .locals init (
            [0] !U
        )

        IL_0000: ldloca.s 0
        IL_0002: initobj !U
        IL_0008: ldloc.0
        IL_0009: ret
    } // end of method IBase`3::GetU

    .method public hidebysig newslot virtual 
        static !V GetV () cil managed 
    {
        // Method begins at RVA 0x2068
        // Code size 10 (0xa)
        .maxstack 1
        .locals init (
            [0] !V
        )

        IL_0000: ldloca.s 0
        IL_0002: initobj !V
        IL_0008: ldloc.0
        IL_0009: ret
    } // end of method IBase`3::GetV

} // end of class IBase`3

.class interface public auto ansi abstract beforefieldinit IMiddle`2<T, U>
    implements class IBase`3<!T, !U, int32>,
               class IBase`3<!T, !U, float32>
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
        01 00 02 00 00
    )
    // Methods
    .method private hidebysig
        static int32 'IBase<T,U,System.Int32>.GetV' () cil managed 
    {
        .override method !2 class IBase`3<!T, !U, int32>::GetV()
        // Method begins at RVA 0x207e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method IMiddle`2::'IBase<T,U,System.Int32>.GetV'

    .method private hidebysig
        static float32 'IBase<T,U,System.Single>.GetV' () cil managed 
    {
        .override method !2 class IBase`3<!T, !U, float32>::GetV()
        // Method begins at RVA 0x207e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method IMiddle`2::'IBase<T,U,System.Single>.GetV'

} // end of class IMiddle`2

.class interface public auto ansi abstract beforefieldinit IDerived`1<T>
    implements class IMiddle`2<!T, int64>,
               class IMiddle`2<!T, float64>
    {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
        01 00 02 00 00
    )
    // Methods
    .method private hidebysig
        static int32 'IBase<T,System.Int64,System.Int32>.GetV' () cil managed 
    {
        .override method !2 class IBase`3<!T, int64, int32>::GetV()
        // Method begins at RVA 0x207e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method IDerived`1::'IBase<T,System.Int64,System.Int32>.GetV'

    .method private hidebysig
        static float32 'IBase<T,System.Int64,System.Single>.GetV' () cil managed 
    {
        .override method !2 class IBase`3<!T, int64, float32>::GetV()
        // Method begins at RVA 0x207e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method IDerived`1::'IBase<T,System.Int64,System.Single>.GetV'

    .method private hidebysig
        static int64 'IBase<T,System.Int64,System.Int32>.GetU' () cil managed 
    {
        .override method !1 class IBase`3<!T, int64, int32>::GetU()
        // Method begins at RVA 0x207e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method IDerived`1::'IBase<T,System.Int64,System.Int32>.GetU'

    .method private hidebysig
        static int64 'IBase<T,System.Int64,System.Single>.GetU' () cil managed 
    {
        .override method !1 class IBase`3<!T, int64, float32>::GetU()
        // Method begins at RVA 0x207e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method IDerived`1::'IBase<T,System.Int64,System.Single>.GetU'

    .method private hidebysig
        static float64 'IBase<T,System.Double,System.Int32>.GetU' () cil managed 
    {
        .override method !1 class IBase`3<!T, float64, int32>::GetU()
        // Method begins at RVA 0x207e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method IDerived`1::'IBase<T,System.Double,System.Int32>.GetU'

    .method private hidebysig
        static float64 'IBase<T,System.Double,System.Single>.GetU' () cil managed 
    {
        .override method !1 class IBase`3<!T, float64, float32>::GetU()
        // Method begins at RVA 0x207e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method IDerived`1::'IBase<T,System.Double,System.Single>.GetU'

} // end of class IDerived`1

.class public auto ansi beforefieldinit MyClass
    extends [mscorlib]System.Object
    implements class IDerived`1<string>,
               class IDerived`1<char>
{
    // Methods
    .method private hidebysig newslot
        static string 'IBase<System.String,System.Int64,System.Single>.GetT' () cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
            01 00 01 00 00
        )
        .override method !0 class IBase`3<string, int64, float32>::GetT()
        // Method begins at RVA 0x207e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method MyClass::'IBase<System.String,System.Int64,System.Single>.GetT'

    .method private  hidebysig newslot
        static string 'IBase<System.String,System.Double,System.Int32>.GetT' () cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
            01 00 01 00 00
        )
        .override method !0 class IBase`3<string, float64, int32>::GetT()
        // Method begins at RVA 0x207e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method MyClass::'IBase<System.String,System.Double,System.Int32>.GetT'

    .method private hidebysig newslot
        static string 'IBase<System.String,System.Double,System.Single>.GetT' () cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
            01 00 01 00 00
        )
        .override method !0 class IBase`3<string, float64, float32>::GetT()
        // Method begins at RVA 0x207e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method MyClass::'IBase<System.String,System.Double,System.Single>.GetT'

    .method private hidebysig newslot
        static string 'IBase<System.String,System.Int64,System.Int32>.GetT' () cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
            01 00 01 00 00
        )
        .override method !0 class IBase`3<string, int64, int32>::GetT()
        // Method begins at RVA 0x207e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method MyClass::'IBase<System.String,System.Int64,System.Int32>.GetT'

    .method private hidebysig newslot
        static char 'IBase<System.Char,System.Int64,System.Int32>.GetT' () cil managed 
    {
        .override method !0 class IBase`3<char, int64, int32>::GetT()
        // Method begins at RVA 0x207e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method MyClass::'IBase<System.Char,System.Int64,System.Int32>.GetT'

    .method private hidebysig newslot
        static char 'IBase<System.Char,System.Int64,System.Single>.GetT' () cil managed 
    {
        .override method !0 class IBase`3<char, int64, float32>::GetT()
        // Method begins at RVA 0x207e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method MyClass::'IBase<System.Char,System.Int64,System.Single>.GetT'

    .method private hidebysig newslot
        static char 'IBase<System.Char,System.Double,System.Int32>.GetT' () cil managed 
    {
        .override method !0 class IBase`3<char, float64, int32>::GetT()
        // Method begins at RVA 0x207e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method MyClass::'IBase<System.Char,System.Double,System.Int32>.GetT'

    .method private hidebysig newslot
        static char 'IBase<System.Char,System.Double,System.Single>.GetT' () cil managed 
    {
        .override method !0 class IBase`3<char, float64, float32>::GetT()
        // Method begins at RVA 0x207e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method MyClass::'IBase<System.Char,System.Double,System.Single>.GetT'

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2081
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    } // end of method MyClass::.ctor

} // end of class MyClass

.class public auto ansi beforefieldinit MyDerivedClass
    extends MyClass
{
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x20c0
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void MyClass::.ctor()
        IL_0006: ret
    } // end of method MyDerivedClass::.ctor

} // end of class MyDerivedClass
