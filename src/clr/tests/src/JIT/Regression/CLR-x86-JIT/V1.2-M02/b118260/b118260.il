// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly b118260 {}
.namespace Test
{
  .class public auto ansi Test extends [mscorlib]System.Object
  {
    .method public static int32 Main() cil managed
    {
      .entrypoint

      LoadStr:
          ldstr    "SUCCESS"
          ldc.i4.1
          brtrue   Path1

      Path2:
          ldc.i4.1
          brfalse  Dead1	// Branch never taken

      Path3:
         ldc.i4.1
         brtrue    Path4

      Path1:
         ldc.i4.1
         brtrue    Path2

      Dead1:
         ldc.i4.1
         brtrue    Dead2

      Dead2:
         ldc.i4.1
         brtrue    Path4

      Dead3:
         ldc.i4.1
         brtrue    Path3

      Path4:
         call      void [mscorlib]System.Console::WriteLine(string)

         ldc.i4 100
         ret
    }
  }
}
