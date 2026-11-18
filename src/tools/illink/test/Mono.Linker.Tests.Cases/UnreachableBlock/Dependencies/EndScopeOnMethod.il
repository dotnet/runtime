.assembly extern mscorlib
{
}
.assembly 'library'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module library.dll

.namespace Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies
{

  .class public auto ansi beforefieldinit EndScopeOnMethod
    extends [mscorlib]System.Object
  {
    .field private static int32 marker

    .method public hidebysig specialname rtspecialname
           instance default void '.ctor' ()  cil managed
    {
      IL_0000:  ldarg.0
      IL_0001:  call instance void class [mscorlib]System.Object::'.ctor'()
      IL_0006:  ret
    }

    .method public static hidebysig object TryFinally() cil managed
    {
      .try
      {
        ldnull
        ret
      }
      finally
      {
        ldnull
        ret
      }
    }

    .method private static bool AlwaysFalse() cil managed
    {
      ldc.i4.0
      ret
    }

    .method private static void HandlerWasReached() cil managed
    {
      ldc.i4.1
      stsfld int32 Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.EndScopeOnMethod::marker
      ret
    }

    .method private static void UnreachableHandlerWasReached() cil managed
    {
      ldc.i4.2
      stsfld int32 Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.EndScopeOnMethod::marker
      ret
    }

    .method public static int32 TryEndingAtMethodEnd() cil managed
    {
      .maxstack 1
      .locals init ([0] int32 result)
      br.s BEFORE_TRY
    HANDLER_START:
      pop
      call void Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.EndScopeOnMethod::HandlerWasReached()
      ldc.i4.s 42
      stloc.0
      leave.s DONE
    HANDLER_END:
    DONE:
      ldloc.0
      ret
    BEFORE_TRY:
      nop
    TRY_START:
      call bool Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.EndScopeOnMethod::AlwaysFalse()
      brfalse.s THROW
      ldc.i4.0
      stloc.0
      leave.s DONE
    THROW:
      newobj instance void [mscorlib]System.Exception::.ctor()
      throw
    TRY_END:
      .try TRY_START to TRY_END catch [mscorlib]System.Exception handler HANDLER_START to HANDLER_END
    }

    .method public static void UnreachableTryEndingAtMethodEnd() cil managed
    {
      .maxstack 1
      br.s BEFORE_TRY
    HANDLER_START:
      pop
      call void Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.EndScopeOnMethod::UnreachableHandlerWasReached()
      leave.s DONE
    HANDLER_END:
    DONE:
      ret
    BEFORE_TRY:
      ldc.i4.0
      stsfld int32 Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.EndScopeOnMethod::marker
      call bool Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.EndScopeOnMethod::AlwaysFalse()
      brfalse.s DONE
    TRY_START:
      newobj instance void [mscorlib]System.Exception::.ctor()
      throw
    TRY_END:
      .try TRY_START to TRY_END catch [mscorlib]System.Exception handler HANDLER_START to HANDLER_END
    }
  }
}