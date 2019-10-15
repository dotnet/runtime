// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib {}
.assembly a {}
.module a.exe

// Test case triggers reimportation of a leave,
// leading to an unreachable callfinally pair.

.class FinallyReimp
{
.method static int32 Main(class [mscorlib]System.String[]) cil managed
{
   .entrypoint
   .locals init (int32 V0, native int V1)
   .try 
   {
   ldarg.0
   ldlen
   ldc.i4 1
   beq FOO
   ldloca V0
   br JOIN
FOO:
   ldc.i4 0
JOIN:
   stloc V1
   leave AFTER
   }
   finally
   {
    ldc.i4 100
    stloc.0
    endfinally
   }
AFTER:
   ldloc.1
   ldind.i
   ret
}
}

