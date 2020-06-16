.assembly extern System.Console { }
.assembly extern System.Collections { }
.assembly extern System.Runtime { }
.assembly extern mscorlib { }
.assembly UnitTest { }

.class public auto ansi abstract Base {}
.class public auto ansi abstract Derived extends Base {}

.class public auto ansi beforefieldinit C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 

    .method public hidebysig newslot virtual instance int32[] M1() 
    {
        ldnull
        ret
    }

    .method public hidebysig newslot virtual instance class [System.Runtime]System.Collections.Generic.IList`1<int32> M2() 
    {
        ldnull
        ret
    }

    .method public hidebysig newslot virtual instance int32 M3() 
    {
        ldc.i4.0
        ret
    }

    .method public hidebysig newslot virtual instance valuetype [mscorlib]System.Nullable`1<int32> M4() 
    {
        ldc.i4.0
        newobj instance void valuetype [mscorlib]System.Nullable`1<int32>::.ctor(!0)
        ret
    }

    .method public hidebysig newslot virtual instance class [System.Runtime]System.Collections.Generic.IList`1<class Base> M5() 
    {
        ldnull
        ret
    }

    .method public hidebysig newslot virtual instance native int[] M6() 
    {
        ldnull
        ret
    }

    .method public hidebysig newslot virtual instance class Base* M7()
    {
        ldc.i4.0
        conv.u
        ret
    }

    .method public hidebysig newslot virtual instance void* M8()
    {
        ldc.i4.0
        conv.u
        ret
    }

    .method public hidebysig newslot virtual instance method explicit instance int32 *(class Base) M9()
    {
        ldc.i4.0
        conv.u
        ret
    }

    .method public hidebysig newslot virtual instance int32* M10()
    {
        ldc.i4.0
        conv.u
        ret
    }

    .method public hidebysig newslot virtual instance native int* M11()
    {
        ldc.i4.0
        conv.u
        ret
    }

    .method public hidebysig newslot virtual instance uint32& M12()
    {
        ldc.i4.0
        conv.u
        ret
    }

    .method public hidebysig newslot virtual instance class Base& M13()
    {
        ldc.i4.0
        conv.u
        ret
    }

    .method public hidebysig newslot virtual instance char& M14()
    {
        ldc.i4.0
        conv.u
        ret
    }

    .method public hidebysig newslot virtual instance bool* M15()
    {
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C2 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 

    .method public hidebysig newslot virtual instance class [System.Runtime]System.Collections.Generic.IList`1<int32> M1() 
    {
        .override method instance int32[] C1::M1();
        ldnull
        ret
    }
}

.class public auto ansi beforefieldinit C3 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 

    .method public hidebysig newslot virtual instance int32[] M2() 
    {
        .override method instance class [System.Runtime]System.Collections.Generic.IList`1<int32> C1::M2();
        ldnull
        ret
    }
}

.class public auto ansi beforefieldinit C4 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 

    .method public hidebysig virtual instance valuetype [mscorlib]System.Nullable`1<int32> M3() 
    {
        .override method instance int32 C1::M3();
        ldc.i4.0
        newobj instance void valuetype [mscorlib]System.Nullable`1<int32>::.ctor(!0)
        ret
    }
}

.class public auto ansi beforefieldinit C5 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 

    .method public hidebysig virtual instance int32 M4() 
    {
        .override method instance valuetype [mscorlib]System.Nullable`1<int32> C1::M4();
        ldc.i4.0
        ret
    }
}

.class public auto ansi beforefieldinit C6 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 

    .method public hidebysig virtual instance uint32[] M1() 
    {
        .override method instance int32[] C1::M1();
        ldnull
        ret
    }
}

.class public auto ansi beforefieldinit C7 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 

    .method public hidebysig newslot virtual instance uint32[] M2() 
    {
        .override method instance class [System.Runtime]System.Collections.Generic.IList`1<int32> C1::M2();
        ldnull
        ret
    }
}

.class public auto ansi beforefieldinit C8 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 

    .method public hidebysig newslot virtual instance class Derived[] M5() 
    {
        .override method instance class [System.Runtime]System.Collections.Generic.IList`1<class Base> C1::M5();
        ldnull
        ret
    }
}

.class public auto ansi beforefieldinit C9 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 

    .method public hidebysig virtual instance uint64[] M6() 
    {
        .override method instance native int[] C1::M6();
        ldnull
        ret
    }
}

.class public auto ansi beforefieldinit C10 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 

    .method public hidebysig virtual instance uint32[] M6() 
    {
        .override method instance native int[] C1::M6();
        ldnull
        ret
    }
}

.class public auto ansi beforefieldinit C11 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance class Derived* M7()
    {
        .override method instance class Base* C1::M7();
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C12 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance int32* M8()
    {
        .override method instance void* C1::M8();
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C13 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance method explicit instance int32 *(class Derived) M9()
    {
        .override method instance method explicit instance int32 *(class Base) C1::M9()
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C14 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance method explicit instance int32 *(class C1) M9()
    {
        .override method instance method explicit instance int32 *(class Base) C1::M9()
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C15 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance method explicit instance int32 *(class Base) M9()
    {
        .override method instance method explicit instance int32 *(class Base) C1::M9()
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C16 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance uint32* M10()
    {
        .override method instance int32* C1::M10()
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C17 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance native uint* M11()
    {
        .override method instance native int* C1::M11()
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C18 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance int32& M12()
    {
        .override method instance uint32& C1::M12()
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C19 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance class Derived& M13()
    {
        .override method instance class Base& C1::M13()

        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C20 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance uint16& M14()
    {
        .override method instance char& C1::M14()
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C21 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance uint8* M15()
    {
        .override method instance bool* C1::M15()
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C22 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance uint8& M14()
    {
        .override method instance char& C1::M14()
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C23 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance uint32* M15()
    {
        .override method instance bool* C1::M15()
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C24 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance bool& M15()
    {
        .override method instance bool* C1::M15()
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C25 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance method explicit instance int32 *(class Base) M15()
    {
        .override method instance bool* C1::M15()
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C26 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance char* M14()
    {
        .override method instance char& C1::M14()
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C27 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance method explicit instance int32 *(class Base) M14()
    {
        .override method instance char& C1::M14()
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C28 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance int32* M9()
    {
        .override method instance method explicit instance int32 *(class Base) C1::M9()
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit C29 extends C1
{
    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

    .method public hidebysig newslot virtual instance int32& M9()
    {
        .override method instance method explicit instance int32 *(class Base) C1::M9()
        ldc.i4.0
        conv.u
        ret
    }
}

.class public auto ansi beforefieldinit Main extends [mscorlib]System.Object
{
    .method public static void RunTestC1() noinlining 
    {
        newobj instance void class C1::.ctor()
        callvirt instance int32[] class C1::M1()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC2() noinlining 
    {
        newobj instance void class C2::.ctor()
        callvirt instance int32[] class C1::M1()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC3() noinlining 
    {
        newobj instance void class C3::.ctor()
        callvirt instance class [System.Runtime]System.Collections.Generic.IList`1<int32> class C1::M2()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC4() noinlining 
    {
        newobj instance void class C4::.ctor()
        callvirt instance int32 class C1::M3()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC5() noinlining 
    {
        newobj instance void class C5::.ctor()
        callvirt instance valuetype [mscorlib]System.Nullable`1<int32> C1::M4()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC6() noinlining 
    {
        newobj instance void class C6::.ctor()
        callvirt instance int32[] class C1::M1()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC7() noinlining 
    {
        newobj instance void class C7::.ctor()
        callvirt instance class [System.Runtime]System.Collections.Generic.IList`1<int32> class C1::M2()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC8() noinlining 
    {
        newobj instance void class C8::.ctor()
        callvirt instance class [System.Runtime]System.Collections.Generic.IList`1<class Base> class C1::M5()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC9() noinlining 
    {
        newobj instance void class C9::.ctor()
        callvirt instance native int[] class C1::M6()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC10() noinlining 
    {
        newobj instance void class C10::.ctor()
        callvirt instance native int[] class C1::M6()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC11() noinlining
    {
        newobj instance void class C11::.ctor()
        callvirt instance class Base* class C1::M7()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC12() noinlining
    {
        newobj instance void class C12::.ctor()
        callvirt instance void* class C1::M8()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC13() noinlining
    {
        newobj instance void class C13::.ctor()
        callvirt instance method explicit instance int32 *(class Base) class C1::M9()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC14() noinlining
    {
        newobj instance void class C14::.ctor()
        callvirt instance method explicit instance int32 *(class Base) class C1::M9()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC15() noinlining
    {
        newobj instance void class C15::.ctor()
        callvirt instance method explicit instance int32 *(class Base) class C1::M9()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC16() noinlining
    {
        newobj instance void class C16::.ctor()
        callvirt instance int32* class C1::M10()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC17() noinlining
    {
        newobj instance void class C17::.ctor()
        callvirt instance native int* class C1::M11()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC18() noinlining
    {
        newobj instance void class C18::.ctor()
        callvirt instance uint32& class C1::M12()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC19() noinlining
    {
        newobj instance void class C19::.ctor()
        callvirt instance class Base& class C1::M13()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC20() noinlining
    {
        newobj instance void class C20::.ctor()
        callvirt instance char& class C1::M14()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC21() noinlining
    {
        newobj instance void class C21::.ctor()
        callvirt instance bool* class C1::M15()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC22() noinlining
    {
        newobj instance void class C22::.ctor()
        callvirt instance char& class C1::M14()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC23() noinlining
    {
        newobj instance void class C23::.ctor()
        callvirt instance bool* class C1::M15()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC24() noinlining
    {
        newobj instance void class C24::.ctor()
        callvirt instance bool* class C1::M15()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC25() noinlining
    {
        newobj instance void class C25::.ctor()
        callvirt instance bool* class C1::M15()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC26() noinlining
    {
        newobj instance void class C26::.ctor()
        callvirt instance char& class C1::M14()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC27() noinlining
    {
        newobj instance void class C27::.ctor()
        callvirt instance char& class C1::M14()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC28() noinlining
    {
        newobj instance void class C28::.ctor()
        callvirt instance method explicit instance int32 *(class Base) class C1::M9()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void RunTestC29() noinlining
    {
        newobj instance void class C29::.ctor()
        callvirt instance method explicit instance int32 *(class Base) class C1::M9()
        pop
        ldstr "Succeeded"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public hidebysig static int32 Main(string[] args) cil managed
    {
        .entrypoint
        .locals init ( bool result )

        ldc.i4.1
        stloc.0

        ldstr "C1"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC1()
            leave.s CC2
        }
        catch [mscorlib]System.TypeLoadException
        {   
            ldstr "Caught unexpected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)        
            ldc.i4.0
            stloc.0
            leave.s CC2
        }  

CC2:
        ldstr "C2: override int32[] by IList<int32>"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC2()
            ldc.i4.0
            stloc.0
            leave.s CC3
        }
        catch [mscorlib]System.TypeLoadException
        {   
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)        
            leave.s CC3
        }  
CC3:
        ldstr "C3: override IList<int32> by int32[]"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC3()
            leave.s CC4
        }
        catch [mscorlib]System.TypeLoadException
        {   
            ldstr "Caught unexpected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)        
            ldc.i4.0
            stloc.0
            leave.s CC4
        }  

CC4:
        ldstr "C4: override int32 by Nullable`1<int32>"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC4()
            ldc.i4.0
            stloc.0
            leave.s CC5
        }
        catch [mscorlib]System.TypeLoadException
        {   
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)        
            leave.s CC5
        }  

CC5:
        ldstr "C5: override Nullable<int32> by int32"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC5()
            ldc.i4.0
            stloc.0
            leave.s CC6
        }
        catch [mscorlib]System.TypeLoadException
        {   
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)        
            leave.s CC6
        }  
CC6:
        ldstr "C6: override int32[] by uint32[]"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC6()
            leave.s CC7
        }
        catch [mscorlib]System.TypeLoadException
        {   
            ldstr "Caught unexpected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)        
            ldc.i4.0
            stloc.0
            leave.s CC7
        }  
CC7:
        ldstr "C7: override IList<int32> by uint32[]"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC7()
            leave.s CC8
        }
        catch [mscorlib]System.TypeLoadException
        {   
            ldstr "Caught unexpected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)        
            ldc.i4.0
            stloc.0
            leave.s CC8
        }  
CC8:
        ldstr "C8: override IList<Base> by Derived[]"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC8()
            leave.s CC9
        }
        catch [mscorlib]System.TypeLoadException
        {   
            ldstr "Caught unexpected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)        
            ldc.i4.0
            stloc.0
            leave.s CC9
        }  
CC9:
        ldstr "C9: override native int[] by uint64[]"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC9()
            ldc.i4.0
            stloc.0
            leave.s CC10
        }
        catch [mscorlib]System.TypeLoadException
        {   
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)        
            leave.s CC10
        }  
CC10:
        ldstr "C10: override native int[] by uint32[]"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC10()
            ldc.i4.0
            stloc.0
            leave.s CC11
        }
        catch [mscorlib]System.TypeLoadException
        {   
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)        
            leave.s CC11
        }
CC11:
        ldstr "C11: override Base* by Derived*"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC11()
            ldc.i4.0
            stloc.0
            leave.s CC12
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            leave.s CC12
        }
CC12:
        ldstr "C12: override void* by int32*"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC12()
            ldc.i4.0
            stloc.0
            leave.s CC13
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            leave.s CC13
        }
CC13:
        ldstr "C13: override int32 *(class Base) by int32 *(class Derived)"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC13()
            ldc.i4.0
            stloc.0
            leave.s CC14
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            leave.s CC14
        }
CC14:
        ldstr "C14: override int32 *(class Base) by int32 *(class C1)"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC14()
            ldc.i4.0
            stloc.0
            leave.s CC15
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            leave.s CC15
        }
CC15:
        ldstr "C15: override int32 *(class Base) by int32 *(class Base)"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC15()
            leave.s CC16
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught unexpected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            ldc.i4.0
            stloc.0
            leave.s CC16
        }
CC16:
        ldstr "C16: override int32* M10() by uint32* M10()"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC16()
            leave.s CC17
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught unexpected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            ldc.i4.0
            stloc.0
            leave.s CC17
        }
CC17:
        ldstr "C17: override native int* M11() by native uint* M11()"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC17()
            leave.s CC18
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught unexpected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            ldc.i4.0
            stloc.0
            leave.s CC18
        }
CC18:
        ldstr "C18: override uint32& M12() by int32& M12()"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC18()
            leave.s CC19
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught unexpected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            ldc.i4.0
            stloc.0
            leave.s CC19
        }
CC19:
        ldstr "C19: override Base& M13() by Derived& M13()"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC19()
            ldc.i4.0
            stloc.0
            leave.s CC20
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            leave.s CC20
        }
CC20:
        ldstr "C20: override char& M14() by uint16& M14()"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC20()
            leave.s CC21
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught unexpected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            ldc.i4.0
            stloc.0
            leave.s CC21
        }
CC21:
        ldstr "C21: override bool* M15() by uint8* M15()"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC21()
            leave.s CC22
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught unexpected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            ldc.i4.0
            stloc.0
            leave.s CC22
        }
CC22:
        ldstr "C22: override char& M14() by uint8& M14()"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC22()
            ldc.i4.0
            stloc.0
            leave.s CC23
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            leave.s CC23
        }
CC23:
        ldstr "C23: override bool* M15() by uint32* M15()"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC23()
            ldc.i4.0
            stloc.0
            leave.s CC24
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            leave.s CC24
        }
CC24:
        ldstr "C24: override bool* M15() by bool& M15()"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC24()
            ldc.i4.0
            stloc.0
            leave.s CC25
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            leave.s CC25
        }
CC25:
        ldstr "C25: override bool* M15() by int32 *(class Base) M15()"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC25()
            ldc.i4.0
            stloc.0
            leave.s CC26
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            leave.s CC26
        }
CC26:
        ldstr "C26: override char& M14() by char* M14()"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC26()
            ldc.i4.0
            stloc.0
            leave.s CC27
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            leave.s CC27
        }
CC27:
        ldstr "C27: override char& M14() by int32 *(class Base) M14()"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC27()
            ldc.i4.0
            stloc.0
            leave.s CC28
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            leave.s CC28
        }
CC28:
        ldstr "C28: override int32 *(class Base) M9() by int32* M9()"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC28()
            ldc.i4.0
            stloc.0
            leave.s CC29
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            leave.s CC29
        }
CC29:
        ldstr "C29: override int32 *(class Base) M9() by int32& M9()"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC29()
            ldc.i4.0
            stloc.0
            leave.s DONE
        }
        catch [mscorlib]System.TypeLoadException
        {
            ldstr "Caught expected TypeLoadException:"
            call       void [System.Console]System.Console::WriteLine(string)
            call       void [System.Console]System.Console::WriteLine(object)
            leave.s DONE
        }
DONE:

        ldloc.0
        brtrue.s   PASS

        ldstr      "Test FAILED"
        call       void [System.Console]System.Console::WriteLine(string)
        ldc.i4.s   101
        ret
        
        PASS:
        ldstr      "Test PASSED"
        call       void [System.Console]System.Console::WriteLine(string)
        ldc.i4.s   100
        ret
    }
}
