// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 4:0:0:0
}
.assembly helper
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
}

.class public abstract auto ansi sealed beforefieldinit Helper
       extends [mscorlib]System.Object
{
  .class auto ansi nested private beforefieldinit ExceptionHelper`1<T>
         extends [mscorlib]System.Exception
  {
    .field public !T payload
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Exception::.ctor()
      IL_0006:  ret
    } 

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(!T t) cil managed
    {
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Exception::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldarg.1
      IL_0008:  stfld      !0 class Helper/ExceptionHelper`1<!T>::payload
      IL_000d:  ret
    } 

  } 

  .method private hidebysig static void OverwriteRDX<SrcType>(!!SrcType src) cil managed noinlining
  {
    ret
  }

  .method public hidebysig static !!DestType 
          RetypeObject<DestType,SrcType>(!!SrcType src) cil managed noinlining
  {
    .maxstack  2
    .locals init (!!DestType retVal)
    .try
    {
      newobj     instance void class Helper/ExceptionHelper`1<!!DestType>::.ctor()
      throw
    }
    catch class Helper/ExceptionHelper`1<!!DestType> 
    {
      ldarg.0
      newobj     instance void class Helper/ExceptionHelper`1<!!SrcType>::.ctor(!0)
      call       void Helper::OverwriteRDX<class Helper/ExceptionHelper`1<!!SrcType>>(!!0)
      ldfld      !0 class Helper/ExceptionHelper`1<!!DestType>::payload
      stloc.0
      leave      END_CATCH

    }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
    .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH } .try { leave END_CATCH } catch class [mscorlib]System.Exception { leave END_CATCH }
  END_CATCH:
    ldloc.0
    ret
  } 

} 
