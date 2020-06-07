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
        ldstr "C7: override IList1<int32> by uint32[]"
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
        ldstr "C10: : override native int[] by uint32[]"
        call void [System.Console]System.Console::WriteLine(string)

        .try
        {
            call void Main::RunTestC10()
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
