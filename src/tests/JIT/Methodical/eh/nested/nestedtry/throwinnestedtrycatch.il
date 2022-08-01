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
.assembly 'throwinnestedtrycatch' {}



.class public auto ansi Test_throwinnestedtrycatch extends [mscorlib] System.Object {
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
  ldstr      "In catch [mscorlib]System.Exception 0"
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
                                                                                                      newobj     instance void [mscorlib]System.Exception::.ctor()
                                                                                                      throw
                                                                                                    } catch [mscorlib]System.SystemException  {
                                                                                                      ldstr      "In catch [mscorlib]System.SystemException 49"
                                                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                                                      leave done
                                                                                                      leave begin
                                                                                                    }
                                                                                                  } catch [mscorlib]System.SystemException  {
                                                                                                    ldstr      "In catch [mscorlib]System.SystemException 48"
                                                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                                                    leave done
                                                                                                    leave begin
                                                                                                  }
                                                                                                } catch [mscorlib]System.SystemException  {
                                                                                                  ldstr      "In catch [mscorlib]System.SystemException 47"
                                                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                                                  leave done
                                                                                                  leave begin
                                                                                                }
                                                                                              } catch [mscorlib]System.SystemException  {
                                                                                                ldstr      "In catch [mscorlib]System.SystemException 46"
                                                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                                                leave done
                                                                                                leave begin
                                                                                              }
                                                                                            } catch [mscorlib]System.SystemException  {
                                                                                              ldstr      "In catch [mscorlib]System.SystemException 45"
                                                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                                                              leave done
                                                                                              leave begin
                                                                                            }
                                                                                          } catch [mscorlib]System.SystemException  {
                                                                                            ldstr      "In catch [mscorlib]System.SystemException 44"
                                                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                                                            leave done
                                                                                            leave begin
                                                                                          }
                                                                                        } catch [mscorlib]System.SystemException  {
                                                                                          ldstr      "In catch [mscorlib]System.SystemException 43"
                                                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                                                          leave done
                                                                                          leave begin
                                                                                        }
                                                                                      } catch [mscorlib]System.SystemException  {
                                                                                        ldstr      "In catch [mscorlib]System.SystemException 42"
                                                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                                                        leave done
                                                                                        leave begin
                                                                                      }
                                                                                    } catch [mscorlib]System.SystemException  {
                                                                                      ldstr      "In catch [mscorlib]System.SystemException 41"
                                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                                      leave done
                                                                                      leave begin
                                                                                    }
                                                                                  } catch [mscorlib]System.SystemException  {
                                                                                    ldstr      "In catch [mscorlib]System.SystemException 40"
                                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                                    leave done
                                                                                    leave begin
                                                                                  }
                                                                                } catch [mscorlib]System.SystemException  {
                                                                                  ldstr      "In catch [mscorlib]System.SystemException 39"
                                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                                  leave done
                                                                                  leave begin
                                                                                }
                                                                              } catch [mscorlib]System.SystemException  {
                                                                                ldstr      "In catch [mscorlib]System.SystemException 38"
                                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                                leave done
                                                                                leave begin
                                                                              }
                                                                            } catch [mscorlib]System.SystemException  {
                                                                              ldstr      "In catch [mscorlib]System.SystemException 37"
                                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                                              leave done
                                                                              leave begin
                                                                            }
                                                                          } catch [mscorlib]System.SystemException  {
                                                                            ldstr      "In catch [mscorlib]System.SystemException 36"
                                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                                            leave done
                                                                            leave begin
                                                                          }
                                                                        } catch [mscorlib]System.SystemException  {
                                                                          ldstr      "In catch [mscorlib]System.SystemException 35"
                                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                                          leave done
                                                                          leave begin
                                                                        }
                                                                      } catch [mscorlib]System.SystemException  {
                                                                        ldstr      "In catch [mscorlib]System.SystemException 34"
                                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                                        leave done
                                                                        leave begin
                                                                      }
                                                                    } catch [mscorlib]System.SystemException  {
                                                                      ldstr      "In catch [mscorlib]System.SystemException 33"
                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                      leave done
                                                                      leave begin
                                                                    }
                                                                  } catch [mscorlib]System.SystemException  {
                                                                    ldstr      "In catch [mscorlib]System.SystemException 32"
                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                    leave done
                                                                    leave begin
                                                                  }
                                                                } catch [mscorlib]System.SystemException  {
                                                                  ldstr      "In catch [mscorlib]System.SystemException 31"
                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                  leave done
                                                                  leave begin
                                                                }
                                                              } catch [mscorlib]System.SystemException  {
                                                                ldstr      "In catch [mscorlib]System.SystemException 30"
                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                leave done
                                                                leave begin
                                                              }
                                                            } catch [mscorlib]System.SystemException  {
                                                              ldstr      "In catch [mscorlib]System.SystemException 29"
                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                              leave done
                                                              leave begin
                                                            }
                                                          } catch [mscorlib]System.SystemException  {
                                                            ldstr      "In catch [mscorlib]System.SystemException 28"
                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                            leave done
                                                            leave begin
                                                          }
                                                        } catch [mscorlib]System.SystemException  {
                                                          ldstr      "In catch [mscorlib]System.SystemException 27"
                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                          leave done
                                                          leave begin
                                                        }
                                                      } catch [mscorlib]System.SystemException  {
                                                        ldstr      "In catch [mscorlib]System.SystemException 26"
                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                        leave done
                                                        leave begin
                                                      }
                                                    } catch [mscorlib]System.SystemException  {
                                                      ldstr      "In catch [mscorlib]System.SystemException 25"
                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                      leave done
                                                      leave begin
                                                    }
                                                  } catch [mscorlib]System.SystemException  {
                                                    ldstr      "In catch [mscorlib]System.SystemException 24"
                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                    leave done
                                                    leave begin
                                                  }
                                                } catch [mscorlib]System.SystemException  {
                                                  ldstr      "In catch [mscorlib]System.SystemException 23"
                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                  leave done
                                                  leave begin
                                                }
                                              } catch [mscorlib]System.SystemException  {
                                                ldstr      "In catch [mscorlib]System.SystemException 22"
                                                call       void [System.Console]System.Console::WriteLine(string)
                                                leave done
                                                leave begin
                                              }
                                            } catch [mscorlib]System.SystemException  {
                                              ldstr      "In catch [mscorlib]System.SystemException 21"
                                              call       void [System.Console]System.Console::WriteLine(string)
                                              leave done
                                              leave begin
                                            }
                                          } catch [mscorlib]System.SystemException  {
                                            ldstr      "In catch [mscorlib]System.SystemException 20"
                                            call       void [System.Console]System.Console::WriteLine(string)
                                            leave done
                                            leave begin
                                          }
                                        } catch [mscorlib]System.SystemException  {
                                          ldstr      "In catch [mscorlib]System.SystemException 19"
                                          call       void [System.Console]System.Console::WriteLine(string)
                                          leave done
                                          leave begin
                                        }
                                      } catch [mscorlib]System.SystemException  {
                                        ldstr      "In catch [mscorlib]System.SystemException 18"
                                        call       void [System.Console]System.Console::WriteLine(string)
                                        leave done
                                        leave begin
                                      }
                                    } catch [mscorlib]System.SystemException  {
                                      ldstr      "In catch [mscorlib]System.SystemException 17"
                                      call       void [System.Console]System.Console::WriteLine(string)
                                      leave done
                                      leave begin
                                    }
                                  } catch [mscorlib]System.SystemException  {
                                    ldstr      "In catch [mscorlib]System.SystemException 16"
                                    call       void [System.Console]System.Console::WriteLine(string)
                                    leave done
                                    leave begin
                                  }
                                } catch [mscorlib]System.SystemException  {
                                  ldstr      "In catch [mscorlib]System.SystemException 15"
                                  call       void [System.Console]System.Console::WriteLine(string)
                                  leave done
                                  leave begin
                                }
                              } catch [mscorlib]System.SystemException  {
                                ldstr      "In catch [mscorlib]System.SystemException 14"
                                call       void [System.Console]System.Console::WriteLine(string)
                                leave done
                                leave begin
                              }
                            } catch [mscorlib]System.SystemException  {
                              ldstr      "In catch [mscorlib]System.SystemException 13"
                              call       void [System.Console]System.Console::WriteLine(string)
                              leave done
                              leave begin
                            }
                          } catch [mscorlib]System.SystemException  {
                            ldstr      "In catch [mscorlib]System.SystemException 12"
                            call       void [System.Console]System.Console::WriteLine(string)
                            leave done
                            leave begin
                          }
                        } catch [mscorlib]System.SystemException  {
                          ldstr      "In catch [mscorlib]System.SystemException 11"
                          call       void [System.Console]System.Console::WriteLine(string)
                          leave done
                          leave begin
                        }
                      } catch [mscorlib]System.SystemException  {
                        ldstr      "In catch [mscorlib]System.SystemException 10"
                        call       void [System.Console]System.Console::WriteLine(string)
                        leave done
                        leave begin
                      }
                    } catch [mscorlib]System.SystemException  {
                      ldstr      "In catch [mscorlib]System.SystemException 9"
                      call       void [System.Console]System.Console::WriteLine(string)
                      leave done
                      leave begin
                    }
                  } catch [mscorlib]System.SystemException  {
                    ldstr      "In catch [mscorlib]System.SystemException 8"
                    call       void [System.Console]System.Console::WriteLine(string)
                    leave done
                    leave begin
                  }
                } catch [mscorlib]System.SystemException  {
                  ldstr      "In catch [mscorlib]System.SystemException 7"
                  call       void [System.Console]System.Console::WriteLine(string)
                  leave done
                  leave begin
                }
              } catch [mscorlib]System.SystemException  {
                ldstr      "In catch [mscorlib]System.SystemException 6"
                call       void [System.Console]System.Console::WriteLine(string)
                leave done
                leave begin
              }
            } catch [mscorlib]System.SystemException  {
              ldstr      "In catch [mscorlib]System.SystemException 5"
              call       void [System.Console]System.Console::WriteLine(string)
              leave done
              leave begin
            }
          } catch [mscorlib]System.SystemException  {
            ldstr      "In catch [mscorlib]System.SystemException 4"
            call       void [System.Console]System.Console::WriteLine(string)
            leave done
            leave begin
          }
        } catch [mscorlib]System.SystemException  {
          ldstr      "In catch [mscorlib]System.SystemException 3"
          call       void [System.Console]System.Console::WriteLine(string)
          leave done
          leave begin
        }
      } catch [mscorlib]System.SystemException  {
        ldstr      "In catch [mscorlib]System.SystemException 2"
        call       void [System.Console]System.Console::WriteLine(string)
        leave done
        leave begin
      }
    } catch [mscorlib]System.SystemException  {
      ldstr      "In catch [mscorlib]System.SystemException 1"
      call       void [System.Console]System.Console::WriteLine(string)
      leave done
      leave begin
    }
  } catch [mscorlib]System.Exception  {
    ldstr      "In catch [mscorlib]System.Exception 0"
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
