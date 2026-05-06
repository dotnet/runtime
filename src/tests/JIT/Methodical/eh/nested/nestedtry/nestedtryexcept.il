// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib {}
.assembly extern eh_common {}
.assembly ASSEMBLY_NAME {}



.class public auto ansi Test_nestedtryexcept extends [mscorlib] System.Object {
.method public static int32 main() {
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  2
  .locals init (
    class [mscorlib]System.IO.StringWriter expectedOut,
    class [eh_common]TestUtil.TestLog testLog
  )

  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
  stloc.s    expectedOut

  ldloc.s    expectedOut
  ldstr      "In try 0"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 1"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 2"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 3"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 4"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 5"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 6"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 7"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 8"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 9"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 10"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 11"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 12"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 13"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 14"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 15"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 16"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 17"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 18"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 19"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 20"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 21"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 22"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 23"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 24"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 25"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 26"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 27"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 28"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 29"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 30"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 31"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 32"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 33"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 34"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 35"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 36"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 37"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 38"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 39"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 40"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 41"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 42"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 43"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 44"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 45"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 46"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 47"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 48"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 49"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In innermost try"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)

  ldloc.s    expectedOut
  newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
  stloc.s    testLog

  ldloc.s    testLog
  callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()

begin:
  .try {
    ldstr      "In try 0"
    call       void [System.Console]System.Console::WriteLine(string)
    .try {
      ldstr      "In try 1"
      call       void [System.Console]System.Console::WriteLine(string)
      .try {
        ldstr      "In try 2"
        call       void [System.Console]System.Console::WriteLine(string)
        .try {
          ldstr      "In try 3"
          call       void [System.Console]System.Console::WriteLine(string)
          .try {
            ldstr      "In try 4"
            call       void [System.Console]System.Console::WriteLine(string)
            .try {
              ldstr      "In try 5"
              call       void [System.Console]System.Console::WriteLine(string)
              .try {
                ldstr      "In try 6"
                call       void [System.Console]System.Console::WriteLine(string)
                .try {
                  ldstr      "In try 7"
                  call       void [System.Console]System.Console::WriteLine(string)
                  .try {
                    ldstr      "In try 8"
                    call       void [System.Console]System.Console::WriteLine(string)
                    .try {
                      ldstr      "In try 9"
                      call       void [System.Console]System.Console::WriteLine(string)
                      .try {
                        ldstr      "In try 10"
                        call       void [System.Console]System.Console::WriteLine(string)
                        .try {
                          ldstr      "In try 11"
                          call       void [System.Console]System.Console::WriteLine(string)
                          .try {
                            ldstr      "In try 12"
                            call       void [System.Console]System.Console::WriteLine(string)
                            .try {
                              ldstr      "In try 13"
                              call       void [System.Console]System.Console::WriteLine(string)
                              .try {
                                ldstr      "In try 14"
                                call       void [System.Console]System.Console::WriteLine(string)
                                .try {
                                  ldstr      "In try 15"
                                  call       void [System.Console]System.Console::WriteLine(string)
                                  .try {
                                    ldstr      "In try 16"
                                    call       void [System.Console]System.Console::WriteLine(string)
                                    .try {
                                      ldstr      "In try 17"
                                      call       void [System.Console]System.Console::WriteLine(string)
                                      .try {
                                        ldstr      "In try 18"
                                        call       void [System.Console]System.Console::WriteLine(string)
                                        .try {
                                          ldstr      "In try 19"
                                          call       void [System.Console]System.Console::WriteLine(string)
                                          .try {
                                            ldstr      "In try 20"
                                            call       void [System.Console]System.Console::WriteLine(string)
                                            .try {
                                              ldstr      "In try 21"
                                              call       void [System.Console]System.Console::WriteLine(string)
                                              .try {
                                                ldstr      "In try 22"
                                                call       void [System.Console]System.Console::WriteLine(string)
                                                .try {
                                                  ldstr      "In try 23"
                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                  .try {
                                                    ldstr      "In try 24"
                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                    .try {
                                                      ldstr      "In try 25"
                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                      .try {
                                                        ldstr      "In try 26"
                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                        .try {
                                                          ldstr      "In try 27"
                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                          .try {
                                                            ldstr      "In try 28"
                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                            .try {
                                                              ldstr      "In try 29"
                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                              .try {
                                                                ldstr      "In try 30"
                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                .try {
                                                                  ldstr      "In try 31"
                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                  .try {
                                                                    ldstr      "In try 32"
                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                    .try {
                                                                      ldstr      "In try 33"
                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                      .try {
                                                                        ldstr      "In try 34"
                                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                                        .try {
                                                                          ldstr      "In try 35"
                                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                                          .try {
                                                                            ldstr      "In try 36"
                                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                                            .try {
                                                                              ldstr      "In try 37"
                                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                                              .try {
                                                                                ldstr      "In try 38"
                                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                                .try {
                                                                                  ldstr      "In try 39"
                                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                                  .try {
                                                                                    ldstr      "In try 40"
                                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                                    .try {
                                                                                      ldstr      "In try 41"
                                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                                      .try {
                                                                                        ldstr      "In try 42"
                                                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                                                        .try {
                                                                                          ldstr      "In try 43"
                                                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                                                          .try {
                                                                                            ldstr      "In try 44"
                                                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                                                            .try {
                                                                                              ldstr      "In try 45"
                                                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                                                              .try {
                                                                                                ldstr      "In try 46"
                                                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                                                .try {
                                                                                                  ldstr      "In try 47"
                                                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                                                  .try {
                                                                                                    ldstr      "In try 48"
                                                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                                                    .try {
                                                                                                      ldstr      "In try 49"
                                                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                                                      ldstr      "In innermost try"
                                                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                                                      leave    done
                                                                                                    } filter {
                                                                                                      pop
                                                                                                      ldstr      "In filter 50"
                                                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                                                      ldc.i4 0
                                                                                                      endfilter
                                                                                                    }
                                                                                                    {
                                                                                                      ldstr      "In except 49"
                                                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                                                      leave done
                                                                                                      leave begin
                                                                                                    }
                                                                                                  } filter {
                                                                                                    pop
                                                                                                    ldstr      "In filter 49"
                                                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                                                    ldc.i4 0
                                                                                                    endfilter
                                                                                                  }
                                                                                                  {
                                                                                                    ldstr      "In except 48"
                                                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                                                    leave done
                                                                                                    leave begin
                                                                                                  }
                                                                                                } filter {
                                                                                                  pop
                                                                                                  ldstr      "In filter 48"
                                                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                                                  ldc.i4 0
                                                                                                  endfilter
                                                                                                }
                                                                                                {
                                                                                                  ldstr      "In except 47"
                                                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                                                  leave done
                                                                                                  leave begin
                                                                                                }
                                                                                              } filter {
                                                                                                pop
                                                                                                ldstr      "In filter 47"
                                                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                                                ldc.i4 0
                                                                                                endfilter
                                                                                              }
                                                                                              {
                                                                                                ldstr      "In except 46"
                                                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                                                leave done
                                                                                                leave begin
                                                                                              }
                                                                                            } filter {
                                                                                              pop
                                                                                              ldstr      "In filter 46"
                                                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                                                              ldc.i4 0
                                                                                              endfilter
                                                                                            }
                                                                                            {
                                                                                              ldstr      "In except 45"
                                                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                                                              leave done
                                                                                              leave begin
                                                                                            }
                                                                                          } filter {
                                                                                            pop
                                                                                            ldstr      "In filter 45"
                                                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                                                            ldc.i4 0
                                                                                            endfilter
                                                                                          }
                                                                                          {
                                                                                            ldstr      "In except 44"
                                                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                                                            leave done
                                                                                            leave begin
                                                                                          }
                                                                                        } filter {
                                                                                          pop
                                                                                          ldstr      "In filter 44"
                                                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                                                          ldc.i4 0
                                                                                          endfilter
                                                                                        }
                                                                                        {
                                                                                          ldstr      "In except 43"
                                                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                                                          leave done
                                                                                          leave begin
                                                                                        }
                                                                                      } filter {
                                                                                        pop
                                                                                        ldstr      "In filter 43"
                                                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                                                        ldc.i4 0
                                                                                        endfilter
                                                                                      }
                                                                                      {
                                                                                        ldstr      "In except 42"
                                                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                                                        leave done
                                                                                        leave begin
                                                                                      }
                                                                                    } filter {
                                                                                      pop
                                                                                      ldstr      "In filter 42"
                                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                                      ldc.i4 0
                                                                                      endfilter
                                                                                    }
                                                                                    {
                                                                                      ldstr      "In except 41"
                                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                                      leave done
                                                                                      leave begin
                                                                                    }
                                                                                  } filter {
                                                                                    pop
                                                                                    ldstr      "In filter 41"
                                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                                    ldc.i4 0
                                                                                    endfilter
                                                                                  }
                                                                                  {
                                                                                    ldstr      "In except 40"
                                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                                    leave done
                                                                                    leave begin
                                                                                  }
                                                                                } filter {
                                                                                  pop
                                                                                  ldstr      "In filter 40"
                                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                                  ldc.i4 0
                                                                                  endfilter
                                                                                }
                                                                                {
                                                                                  ldstr      "In except 39"
                                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                                  leave done
                                                                                  leave begin
                                                                                }
                                                                              } filter {
                                                                                pop
                                                                                ldstr      "In filter 39"
                                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                                ldc.i4 0
                                                                                endfilter
                                                                              }
                                                                              {
                                                                                ldstr      "In except 38"
                                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                                leave done
                                                                                leave begin
                                                                              }
                                                                            } filter {
                                                                              pop
                                                                              ldstr      "In filter 38"
                                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                                              ldc.i4 0
                                                                              endfilter
                                                                            }
                                                                            {
                                                                              ldstr      "In except 37"
                                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                                              leave done
                                                                              leave begin
                                                                            }
                                                                          } filter {
                                                                            pop
                                                                            ldstr      "In filter 37"
                                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                                            ldc.i4 0
                                                                            endfilter
                                                                          }
                                                                          {
                                                                            ldstr      "In except 36"
                                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                                            leave done
                                                                            leave begin
                                                                          }
                                                                        } filter {
                                                                          pop
                                                                          ldstr      "In filter 36"
                                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                                          ldc.i4 0
                                                                          endfilter
                                                                        }
                                                                        {
                                                                          ldstr      "In except 35"
                                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                                          leave done
                                                                          leave begin
                                                                        }
                                                                      } filter {
                                                                        pop
                                                                        ldstr      "In filter 35"
                                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                                        ldc.i4 0
                                                                        endfilter
                                                                      }
                                                                      {
                                                                        ldstr      "In except 34"
                                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                                        leave done
                                                                        leave begin
                                                                      }
                                                                    } filter {
                                                                      pop
                                                                      ldstr      "In filter 34"
                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                      ldc.i4 0
                                                                      endfilter
                                                                    }
                                                                    {
                                                                      ldstr      "In except 33"
                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                      leave done
                                                                      leave begin
                                                                    }
                                                                  } filter {
                                                                    pop
                                                                    ldstr      "In filter 33"
                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                    ldc.i4 0
                                                                    endfilter
                                                                  }
                                                                  {
                                                                    ldstr      "In except 32"
                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                    leave done
                                                                    leave begin
                                                                  }
                                                                } filter {
                                                                  pop
                                                                  ldstr      "In filter 32"
                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                  ldc.i4 0
                                                                  endfilter
                                                                }
                                                                {
                                                                  ldstr      "In except 31"
                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                  leave done
                                                                  leave begin
                                                                }
                                                              } filter {
                                                                pop
                                                                ldstr      "In filter 31"
                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                ldc.i4 0
                                                                endfilter
                                                              }
                                                              {
                                                                ldstr      "In except 30"
                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                leave done
                                                                leave begin
                                                              }
                                                            } filter {
                                                              pop
                                                              ldstr      "In filter 30"
                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                              ldc.i4 0
                                                              endfilter
                                                            }
                                                            {
                                                              ldstr      "In except 29"
                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                              leave done
                                                              leave begin
                                                            }
                                                          } filter {
                                                            pop
                                                            ldstr      "In filter 29"
                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                            ldc.i4 0
                                                            endfilter
                                                          }
                                                          {
                                                            ldstr      "In except 28"
                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                            leave done
                                                            leave begin
                                                          }
                                                        } filter {
                                                          pop
                                                          ldstr      "In filter 28"
                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                          ldc.i4 0
                                                          endfilter
                                                        }
                                                        {
                                                          ldstr      "In except 27"
                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                          leave done
                                                          leave begin
                                                        }
                                                      } filter {
                                                        pop
                                                        ldstr      "In filter 27"
                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                        ldc.i4 0
                                                        endfilter
                                                      }
                                                      {
                                                        ldstr      "In except 26"
                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                        leave done
                                                        leave begin
                                                      }
                                                    } filter {
                                                      pop
                                                      ldstr      "In filter 26"
                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                      ldc.i4 0
                                                      endfilter
                                                    }
                                                    {
                                                      ldstr      "In except 25"
                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                      leave done
                                                      leave begin
                                                    }
                                                  } filter {
                                                    pop
                                                    ldstr      "In filter 25"
                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                    ldc.i4 0
                                                    endfilter
                                                  }
                                                  {
                                                    ldstr      "In except 24"
                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                    leave done
                                                    leave begin
                                                  }
                                                } filter {
                                                  pop
                                                  ldstr      "In filter 24"
                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                  ldc.i4 0
                                                  endfilter
                                                }
                                                {
                                                  ldstr      "In except 23"
                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                  leave done
                                                  leave begin
                                                }
                                              } filter {
                                                pop
                                                ldstr      "In filter 23"
                                                call       void [System.Console]System.Console::WriteLine(string)
                                                ldc.i4 0
                                                endfilter
                                              }
                                              {
                                                ldstr      "In except 22"
                                                call       void [System.Console]System.Console::WriteLine(string)
                                                leave done
                                                leave begin
                                              }
                                            } filter {
                                              pop
                                              ldstr      "In filter 22"
                                              call       void [System.Console]System.Console::WriteLine(string)
                                              ldc.i4 0
                                              endfilter
                                            }
                                            {
                                              ldstr      "In except 21"
                                              call       void [System.Console]System.Console::WriteLine(string)
                                              leave done
                                              leave begin
                                            }
                                          } filter {
                                            pop
                                            ldstr      "In filter 21"
                                            call       void [System.Console]System.Console::WriteLine(string)
                                            ldc.i4 0
                                            endfilter
                                          }
                                          {
                                            ldstr      "In except 20"
                                            call       void [System.Console]System.Console::WriteLine(string)
                                            leave done
                                            leave begin
                                          }
                                        } filter {
                                          pop
                                          ldstr      "In filter 20"
                                          call       void [System.Console]System.Console::WriteLine(string)
                                          ldc.i4 0
                                          endfilter
                                        }
                                        {
                                          ldstr      "In except 19"
                                          call       void [System.Console]System.Console::WriteLine(string)
                                          leave done
                                          leave begin
                                        }
                                      } filter {
                                        pop
                                        ldstr      "In filter 19"
                                        call       void [System.Console]System.Console::WriteLine(string)
                                        ldc.i4 0
                                        endfilter
                                      }
                                      {
                                        ldstr      "In except 18"
                                        call       void [System.Console]System.Console::WriteLine(string)
                                        leave done
                                        leave begin
                                      }
                                    } filter {
                                      pop
                                      ldstr      "In filter 18"
                                      call       void [System.Console]System.Console::WriteLine(string)
                                      ldc.i4 0
                                      endfilter
                                    }
                                    {
                                      ldstr      "In except 17"
                                      call       void [System.Console]System.Console::WriteLine(string)
                                      leave done
                                      leave begin
                                    }
                                  } filter {
                                    pop
                                    ldstr      "In filter 17"
                                    call       void [System.Console]System.Console::WriteLine(string)
                                    ldc.i4 0
                                    endfilter
                                  }
                                  {
                                    ldstr      "In except 16"
                                    call       void [System.Console]System.Console::WriteLine(string)
                                    leave done
                                    leave begin
                                  }
                                } filter {
                                  pop
                                  ldstr      "In filter 16"
                                  call       void [System.Console]System.Console::WriteLine(string)
                                  ldc.i4 0
                                  endfilter
                                }
                                {
                                  ldstr      "In except 15"
                                  call       void [System.Console]System.Console::WriteLine(string)
                                  leave done
                                  leave begin
                                }
                              } filter {
                                pop
                                ldstr      "In filter 15"
                                call       void [System.Console]System.Console::WriteLine(string)
                                ldc.i4 0
                                endfilter
                              }
                              {
                                ldstr      "In except 14"
                                call       void [System.Console]System.Console::WriteLine(string)
                                leave done
                                leave begin
                              }
                            } filter {
                              pop
                              ldstr      "In filter 14"
                              call       void [System.Console]System.Console::WriteLine(string)
                              ldc.i4 0
                              endfilter
                            }
                            {
                              ldstr      "In except 13"
                              call       void [System.Console]System.Console::WriteLine(string)
                              leave done
                              leave begin
                            }
                          } filter {
                            pop
                            ldstr      "In filter 13"
                            call       void [System.Console]System.Console::WriteLine(string)
                            ldc.i4 0
                            endfilter
                          }
                          {
                            ldstr      "In except 12"
                            call       void [System.Console]System.Console::WriteLine(string)
                            leave done
                            leave begin
                          }
                        } filter {
                          pop
                          ldstr      "In filter 12"
                          call       void [System.Console]System.Console::WriteLine(string)
                          ldc.i4 0
                          endfilter
                        }
                        {
                          ldstr      "In except 11"
                          call       void [System.Console]System.Console::WriteLine(string)
                          leave done
                          leave begin
                        }
                      } filter {
                        pop
                        ldstr      "In filter 11"
                        call       void [System.Console]System.Console::WriteLine(string)
                        ldc.i4 0
                        endfilter
                      }
                      {
                        ldstr      "In except 10"
                        call       void [System.Console]System.Console::WriteLine(string)
                        leave done
                        leave begin
                      }
                    } filter {
                      pop
                      ldstr      "In filter 10"
                      call       void [System.Console]System.Console::WriteLine(string)
                      ldc.i4 0
                      endfilter
                    }
                    {
                      ldstr      "In except 9"
                      call       void [System.Console]System.Console::WriteLine(string)
                      leave done
                      leave begin
                    }
                  } filter {
                    pop
                    ldstr      "In filter 9"
                    call       void [System.Console]System.Console::WriteLine(string)
                    ldc.i4 0
                    endfilter
                  }
                  {
                    ldstr      "In except 8"
                    call       void [System.Console]System.Console::WriteLine(string)
                    leave done
                    leave begin
                  }
                } filter {
                  pop
                  ldstr      "In filter 8"
                  call       void [System.Console]System.Console::WriteLine(string)
                  ldc.i4 0
                  endfilter
                }
                {
                  ldstr      "In except 7"
                  call       void [System.Console]System.Console::WriteLine(string)
                  leave done
                  leave begin
                }
              } filter {
                pop
                ldstr      "In filter 7"
                call       void [System.Console]System.Console::WriteLine(string)
                ldc.i4 0
                endfilter
              }
              {
                ldstr      "In except 6"
                call       void [System.Console]System.Console::WriteLine(string)
                leave done
                leave begin
              }
            } filter {
              pop
              ldstr      "In filter 6"
              call       void [System.Console]System.Console::WriteLine(string)
              ldc.i4 0
              endfilter
            }
            {
              ldstr      "In except 5"
              call       void [System.Console]System.Console::WriteLine(string)
              leave done
              leave begin
            }
          } filter {
            pop
            ldstr      "In filter 5"
            call       void [System.Console]System.Console::WriteLine(string)
            ldc.i4 0
            endfilter
          }
          {
            ldstr      "In except 4"
            call       void [System.Console]System.Console::WriteLine(string)
            leave done
            leave begin
          }
        } filter {
          pop
          ldstr      "In filter 4"
          call       void [System.Console]System.Console::WriteLine(string)
          ldc.i4 0
          endfilter
        }
        {
          ldstr      "In except 3"
          call       void [System.Console]System.Console::WriteLine(string)
          leave done
          leave begin
        }
      } filter {
        pop
        ldstr      "In filter 3"
        call       void [System.Console]System.Console::WriteLine(string)
        ldc.i4 0
        endfilter
      }
      {
        ldstr      "In except 2"
        call       void [System.Console]System.Console::WriteLine(string)
        leave done
        leave begin
      }
    } filter {
      pop
      ldstr      "In filter 2"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4 0
      endfilter
    }
    {
      ldstr      "In except 1"
      call       void [System.Console]System.Console::WriteLine(string)
      leave done
      leave begin
    }
  } filter {
    pop
    ldstr      "In filter 1"
    call       void [System.Console]System.Console::WriteLine(string)
    ldc.i4 1
    endfilter
  }
  {
    ldstr      "In except 0"
    call       void [System.Console]System.Console::WriteLine(string)
    leave done
    leave begin
  }

  done:
    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
  
    ldloc.s    testLog
    callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
    
    ret
}
}
