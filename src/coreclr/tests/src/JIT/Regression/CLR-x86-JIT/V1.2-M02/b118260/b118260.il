// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
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
         call      void [System.Console]System.Console::WriteLine(string)

         ldc.i4 100
         ret
    }
  }
}
