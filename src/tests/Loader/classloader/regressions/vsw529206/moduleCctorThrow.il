// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}

.assembly moduleCctorThrow {}


 .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    .maxstack  8
    nop
    ldstr      "Inside module .cctor"
    call       void [System.Console]System.Console::WriteLine(string)

    newobj     instance void [mscorlib]System.Exception::.ctor()
    throw

    ret
  } // end of method Foo::.cctor



.class public auto ansi TriggerModuleCctorClass
       extends [mscorlib]System.Object
{
   .field public static int32 intStatic

   .method public static void CheckInit() noinlining
   {
       ret
   }


   .method public hidebysig specialname rtspecialname 
        instance void  .ctor() cil managed
   {
 
     .maxstack  8
     ldarg.0
     call       instance void [mscorlib]System.Object::.ctor()
     ret
   } // end of method Test1::.ctor
	
}

