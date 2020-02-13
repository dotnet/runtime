.assembly extern mscorlib {} 



.assembly test {} 



.module test.exe 



.class public EntryPoint extends [mscorlib]System.Object { 



  .field public static int32 m_test = int32(2) 



  .method public static int32 Main() cil managed {

      .entrypoint

      ldsfld int32 EntryPoint::m_test
      brtrue fail
      ldc.i4 0
      ret
      fail:
      ldc.i4 1
      ret

  }

} 

