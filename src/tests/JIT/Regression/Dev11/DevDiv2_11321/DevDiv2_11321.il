// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}

.assembly CrashTest
{
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.assembly extern xunit.core
{
    .publickeytoken = (
        8d 05 b1 bb 7a 6f db 6c
     )
     .ver 2:4:2:0
}

.class private auto ansi beforefieldinit CrashTest.Program
{
  .method assembly hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  8

        br.s       startoftry

    return:
	ldc.i4 100
        ret

    catch1:
        pop
        leave.s    return
    endofcatch1:

    catch2:
        pop
        leave.s    return
    endofcatch2:

    startoftry:
        leave.s    return
    endoftry:

    .try startoftry to endoftry catch [mscorlib]System.InvalidOperationException handler catch1 to endofcatch1
    .try startoftry to endoftry catch [mscorlib]System.ArgumentNullException handler catch2 to endofcatch2
  }
}

