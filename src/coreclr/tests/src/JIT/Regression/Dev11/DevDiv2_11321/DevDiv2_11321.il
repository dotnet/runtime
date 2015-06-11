// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

.class private auto ansi beforefieldinit CrashTest.Program
{
  .method assembly hidebysig static int32 Main() cil managed
  {
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

