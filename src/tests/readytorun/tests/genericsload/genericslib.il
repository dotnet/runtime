// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

.assembly genericslib { }
.module genericslib.dll

.assembly extern mscorlib { }

// A module constructor that sets GenClass`1<string>::StaticField to true.
.method assembly specialname rtspecialname static 
          void  .cctor() cil managed
{
    ldc.i4.1
    stsfld     bool class GenClass`1<string>::StaticField
    ret
}

.class public auto ansi beforefieldinit GenClass`1<T>
       extends [mscorlib]System.Object
{
    .field static public bool StaticField

    /*
    public GenClass()
    {
        if (!StaticField)
            throw new System.Exception();
    }
    */
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
        ldarg.0
        call       instance void [mscorlib]System.Object::.ctor()

        ldsfld     bool class GenClass`1<!T>::StaticField
        brtrue.s   OK

        newobj     instance void [mscorlib]System.Exception::.ctor()
        throw

    OK: ret
    }

    /*
    public static bool StaticMethod()
    {
        return StaticField;
    }
    */
    .method public hidebysig static bool StaticMethod() cil managed
    {
        ldsfld     bool class GenClass`1<!T>::StaticField
        ret
    }
}
