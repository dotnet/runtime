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
.assembly 'cascadedcatch' {}



.class public auto ansi Test_cascadedcatch extends [mscorlib] System.Object {
.method public static int32 main() {
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  2
  .locals init (
    int32 V_0,
    int32 V1,
    class [mscorlib]System.IO.StringWriter expectedOut,
    class [eh_common]TestUtil.TestLog testLog
  )

  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
  stloc.s    expectedOut

  ldloc.s    expectedOut
  ldstr      "begin"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)

  ldloc.s    expectedOut
  ldstr      "In try 0"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 0, ldloc.0 is 1"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In finally 0"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)

  ldloc.s    expectedOut
  newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
  stloc.s    testLog

  ldloc.s    testLog
  callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()

  ldc.i4 0
  stloc.0
begin:
  ldstr      "begin"
  call       void [System.Console]System.Console::WriteLine(string)
  ldloc.0
  brtrue done
  .try {
    ldstr      "In try 0"
    call       void [System.Console]System.Console::WriteLine(string)
    ldloc.0
    ldc.i4.1
    add
    stloc.0
    label0:
    .try {
      ldstr      "In try 0, ldloc.0 is "
      call       void [System.Console]System.Console::Write(string)
      ldloc.0
      dup
      call       void [System.Console]System.Console::WriteLine(int32)
      ldc.i4.1
      add
      stloc.0
      leave    done
    } catch [mscorlib]System.Exception  {
      pop
      ldstr      "In catch [mscorlib]System.Exception 0"
      call       void [System.Console]System.Console::WriteLine(string)
      label1:
      .try {
        ldstr      "In try 1, ldloc.0 is "
        call       void [System.Console]System.Console::Write(string)
        ldloc.0
        dup
        call       void [System.Console]System.Console::WriteLine(int32)
        ldc.i4.1
        add
        stloc.0
        leave    done
      } catch [mscorlib]System.Exception  {
        pop
        ldstr      "In catch [mscorlib]System.Exception 1"
        call       void [System.Console]System.Console::WriteLine(string)
        label2:
        .try {
          ldstr      "In try 2, ldloc.0 is "
          call       void [System.Console]System.Console::Write(string)
          ldloc.0
          dup
          call       void [System.Console]System.Console::WriteLine(int32)
          ldc.i4.1
          add
          stloc.0
          leave    done
        } catch [mscorlib]System.Exception  {
          pop
          ldstr      "In catch [mscorlib]System.Exception 2"
          call       void [System.Console]System.Console::WriteLine(string)
          label3:
          .try {
            ldstr      "In try 3, ldloc.0 is "
            call       void [System.Console]System.Console::Write(string)
            ldloc.0
            dup
            call       void [System.Console]System.Console::WriteLine(int32)
            ldc.i4.1
            add
            stloc.0
            leave    done
          } catch [mscorlib]System.Exception  {
            pop
            ldstr      "In catch [mscorlib]System.Exception 3"
            call       void [System.Console]System.Console::WriteLine(string)
            label4:
            .try {
              ldstr      "In try 4, ldloc.0 is "
              call       void [System.Console]System.Console::Write(string)
              ldloc.0
              dup
              call       void [System.Console]System.Console::WriteLine(int32)
              ldc.i4.1
              add
              stloc.0
              leave    done
            } catch [mscorlib]System.Exception  {
              pop
              ldstr      "In catch [mscorlib]System.Exception 4"
              call       void [System.Console]System.Console::WriteLine(string)
              label5:
              .try {
                ldstr      "In try 5, ldloc.0 is "
                call       void [System.Console]System.Console::Write(string)
                ldloc.0
                dup
                call       void [System.Console]System.Console::WriteLine(int32)
                ldc.i4.1
                add
                stloc.0
                leave    done
              } catch [mscorlib]System.Exception  {
                pop
                ldstr      "In catch [mscorlib]System.Exception 5"
                call       void [System.Console]System.Console::WriteLine(string)
                label6:
                .try {
                  ldstr      "In try 6, ldloc.0 is "
                  call       void [System.Console]System.Console::Write(string)
                  ldloc.0
                  dup
                  call       void [System.Console]System.Console::WriteLine(int32)
                  ldc.i4.1
                  add
                  stloc.0
                  leave    done
                } catch [mscorlib]System.Exception  {
                  pop
                  ldstr      "In catch [mscorlib]System.Exception 6"
                  call       void [System.Console]System.Console::WriteLine(string)
                  label7:
                  .try {
                    ldstr      "In try 7, ldloc.0 is "
                    call       void [System.Console]System.Console::Write(string)
                    ldloc.0
                    dup
                    call       void [System.Console]System.Console::WriteLine(int32)
                    ldc.i4.1
                    add
                    stloc.0
                    leave    done
                  } catch [mscorlib]System.Exception  {
                    pop
                    ldstr      "In catch [mscorlib]System.Exception 7"
                    call       void [System.Console]System.Console::WriteLine(string)
                    label8:
                    .try {
                      ldstr      "In try 8, ldloc.0 is "
                      call       void [System.Console]System.Console::Write(string)
                      ldloc.0
                      dup
                      call       void [System.Console]System.Console::WriteLine(int32)
                      ldc.i4.1
                      add
                      stloc.0
                      leave    done
                    } catch [mscorlib]System.Exception  {
                      pop
                      ldstr      "In catch [mscorlib]System.Exception 8"
                      call       void [System.Console]System.Console::WriteLine(string)
                      label9:
                      .try {
                        ldstr      "In try 9, ldloc.0 is "
                        call       void [System.Console]System.Console::Write(string)
                        ldloc.0
                        dup
                        call       void [System.Console]System.Console::WriteLine(int32)
                        ldc.i4.1
                        add
                        stloc.0
                        leave    done
                      } catch [mscorlib]System.Exception  {
                        pop
                        ldstr      "In catch [mscorlib]System.Exception 9"
                        call       void [System.Console]System.Console::WriteLine(string)
                        label10:
                        .try {
                          ldstr      "In try 10, ldloc.0 is "
                          call       void [System.Console]System.Console::Write(string)
                          ldloc.0
                          dup
                          call       void [System.Console]System.Console::WriteLine(int32)
                          ldc.i4.1
                          add
                          stloc.0
                          leave    done
                        } catch [mscorlib]System.Exception  {
                          pop
                          ldstr      "In catch [mscorlib]System.Exception 10"
                          call       void [System.Console]System.Console::WriteLine(string)
                          label11:
                          .try {
                            ldstr      "In try 11, ldloc.0 is "
                            call       void [System.Console]System.Console::Write(string)
                            ldloc.0
                            dup
                            call       void [System.Console]System.Console::WriteLine(int32)
                            ldc.i4.1
                            add
                            stloc.0
                            leave    done
                          } catch [mscorlib]System.Exception  {
                            pop
                            ldstr      "In catch [mscorlib]System.Exception 11"
                            call       void [System.Console]System.Console::WriteLine(string)
                            label12:
                            .try {
                              ldstr      "In try 12, ldloc.0 is "
                              call       void [System.Console]System.Console::Write(string)
                              ldloc.0
                              dup
                              call       void [System.Console]System.Console::WriteLine(int32)
                              ldc.i4.1
                              add
                              stloc.0
                              leave    done
                            } catch [mscorlib]System.Exception  {
                              pop
                              ldstr      "In catch [mscorlib]System.Exception 12"
                              call       void [System.Console]System.Console::WriteLine(string)
                              label13:
                              .try {
                                ldstr      "In try 13, ldloc.0 is "
                                call       void [System.Console]System.Console::Write(string)
                                ldloc.0
                                dup
                                call       void [System.Console]System.Console::WriteLine(int32)
                                ldc.i4.1
                                add
                                stloc.0
                                leave    done
                              } catch [mscorlib]System.Exception  {
                                pop
                                ldstr      "In catch [mscorlib]System.Exception 13"
                                call       void [System.Console]System.Console::WriteLine(string)
                                label14:
                                .try {
                                  ldstr      "In try 14, ldloc.0 is "
                                  call       void [System.Console]System.Console::Write(string)
                                  ldloc.0
                                  dup
                                  call       void [System.Console]System.Console::WriteLine(int32)
                                  ldc.i4.1
                                  add
                                  stloc.0
                                  leave    done
                                } catch [mscorlib]System.Exception  {
                                  pop
                                  ldstr      "In catch [mscorlib]System.Exception 14"
                                  call       void [System.Console]System.Console::WriteLine(string)
                                  label15:
                                  .try {
                                    ldstr      "In try 15, ldloc.0 is "
                                    call       void [System.Console]System.Console::Write(string)
                                    ldloc.0
                                    dup
                                    call       void [System.Console]System.Console::WriteLine(int32)
                                    ldc.i4.1
                                    add
                                    stloc.0
                                    leave    done
                                  } catch [mscorlib]System.Exception  {
                                    pop
                                    ldstr      "In catch [mscorlib]System.Exception 15"
                                    call       void [System.Console]System.Console::WriteLine(string)
                                    label16:
                                    .try {
                                      ldstr      "In try 16, ldloc.0 is "
                                      call       void [System.Console]System.Console::Write(string)
                                      ldloc.0
                                      dup
                                      call       void [System.Console]System.Console::WriteLine(int32)
                                      ldc.i4.1
                                      add
                                      stloc.0
                                      leave    done
                                    } catch [mscorlib]System.Exception  {
                                      pop
                                      ldstr      "In catch [mscorlib]System.Exception 16"
                                      call       void [System.Console]System.Console::WriteLine(string)
                                      label17:
                                      .try {
                                        ldstr      "In try 17, ldloc.0 is "
                                        call       void [System.Console]System.Console::Write(string)
                                        ldloc.0
                                        dup
                                        call       void [System.Console]System.Console::WriteLine(int32)
                                        ldc.i4.1
                                        add
                                        stloc.0
                                        leave    done
                                      } catch [mscorlib]System.Exception  {
                                        pop
                                        ldstr      "In catch [mscorlib]System.Exception 17"
                                        call       void [System.Console]System.Console::WriteLine(string)
                                        label18:
                                        .try {
                                          ldstr      "In try 18, ldloc.0 is "
                                          call       void [System.Console]System.Console::Write(string)
                                          ldloc.0
                                          dup
                                          call       void [System.Console]System.Console::WriteLine(int32)
                                          ldc.i4.1
                                          add
                                          stloc.0
                                          leave    done
                                        } catch [mscorlib]System.Exception  {
                                          pop
                                          ldstr      "In catch [mscorlib]System.Exception 18"
                                          call       void [System.Console]System.Console::WriteLine(string)
                                          label19:
                                          .try {
                                            ldstr      "In try 19, ldloc.0 is "
                                            call       void [System.Console]System.Console::Write(string)
                                            ldloc.0
                                            dup
                                            call       void [System.Console]System.Console::WriteLine(int32)
                                            ldc.i4.1
                                            add
                                            stloc.0
                                            leave    done
                                          } catch [mscorlib]System.Exception  {
                                            pop
                                            ldstr      "In catch [mscorlib]System.Exception 19"
                                            call       void [System.Console]System.Console::WriteLine(string)
                                            label20:
                                            .try {
                                              ldstr      "In try 20, ldloc.0 is "
                                              call       void [System.Console]System.Console::Write(string)
                                              ldloc.0
                                              dup
                                              call       void [System.Console]System.Console::WriteLine(int32)
                                              ldc.i4.1
                                              add
                                              stloc.0
                                              leave    done
                                            } catch [mscorlib]System.Exception  {
                                              pop
                                              ldstr      "In catch [mscorlib]System.Exception 20"
                                              call       void [System.Console]System.Console::WriteLine(string)
                                              label21:
                                              .try {
                                                ldstr      "In try 21, ldloc.0 is "
                                                call       void [System.Console]System.Console::Write(string)
                                                ldloc.0
                                                dup
                                                call       void [System.Console]System.Console::WriteLine(int32)
                                                ldc.i4.1
                                                add
                                                stloc.0
                                                leave    done
                                              } catch [mscorlib]System.Exception  {
                                                pop
                                                ldstr      "In catch [mscorlib]System.Exception 21"
                                                call       void [System.Console]System.Console::WriteLine(string)
                                                label22:
                                                .try {
                                                  ldstr      "In try 22, ldloc.0 is "
                                                  call       void [System.Console]System.Console::Write(string)
                                                  ldloc.0
                                                  dup
                                                  call       void [System.Console]System.Console::WriteLine(int32)
                                                  ldc.i4.1
                                                  add
                                                  stloc.0
                                                  leave    done
                                                } catch [mscorlib]System.Exception  {
                                                  pop
                                                  ldstr      "In catch [mscorlib]System.Exception 22"
                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                  label23:
                                                  .try {
                                                    ldstr      "In try 23, ldloc.0 is "
                                                    call       void [System.Console]System.Console::Write(string)
                                                    ldloc.0
                                                    dup
                                                    call       void [System.Console]System.Console::WriteLine(int32)
                                                    ldc.i4.1
                                                    add
                                                    stloc.0
                                                    leave    done
                                                  } catch [mscorlib]System.Exception  {
                                                    pop
                                                    ldstr      "In catch [mscorlib]System.Exception 23"
                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                    label24:
                                                    .try {
                                                      ldstr      "In try 24, ldloc.0 is "
                                                      call       void [System.Console]System.Console::Write(string)
                                                      ldloc.0
                                                      dup
                                                      call       void [System.Console]System.Console::WriteLine(int32)
                                                      ldc.i4.1
                                                      add
                                                      stloc.0
                                                      leave    done
                                                    } catch [mscorlib]System.Exception  {
                                                      pop
                                                      ldstr      "In catch [mscorlib]System.Exception 24"
                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                      label25:
                                                      .try {
                                                        ldstr      "In try 25, ldloc.0 is "
                                                        call       void [System.Console]System.Console::Write(string)
                                                        ldloc.0
                                                        dup
                                                        call       void [System.Console]System.Console::WriteLine(int32)
                                                        ldc.i4.1
                                                        add
                                                        stloc.0
                                                        leave    done
                                                      } catch [mscorlib]System.Exception  {
                                                        pop
                                                        ldstr      "In catch [mscorlib]System.Exception 25"
                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                        label26:
                                                        .try {
                                                          ldstr      "In try 26, ldloc.0 is "
                                                          call       void [System.Console]System.Console::Write(string)
                                                          ldloc.0
                                                          dup
                                                          call       void [System.Console]System.Console::WriteLine(int32)
                                                          ldc.i4.1
                                                          add
                                                          stloc.0
                                                          leave    done
                                                        } catch [mscorlib]System.Exception  {
                                                          pop
                                                          ldstr      "In catch [mscorlib]System.Exception 26"
                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                          label27:
                                                          .try {
                                                            ldstr      "In try 27, ldloc.0 is "
                                                            call       void [System.Console]System.Console::Write(string)
                                                            ldloc.0
                                                            dup
                                                            call       void [System.Console]System.Console::WriteLine(int32)
                                                            ldc.i4.1
                                                            add
                                                            stloc.0
                                                            leave    done
                                                          } catch [mscorlib]System.Exception  {
                                                            pop
                                                            ldstr      "In catch [mscorlib]System.Exception 27"
                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                            label28:
                                                            .try {
                                                              ldstr      "In try 28, ldloc.0 is "
                                                              call       void [System.Console]System.Console::Write(string)
                                                              ldloc.0
                                                              dup
                                                              call       void [System.Console]System.Console::WriteLine(int32)
                                                              ldc.i4.1
                                                              add
                                                              stloc.0
                                                              leave    done
                                                            } catch [mscorlib]System.Exception  {
                                                              pop
                                                              ldstr      "In catch [mscorlib]System.Exception 28"
                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                              label29:
                                                              .try {
                                                                ldstr      "In try 29, ldloc.0 is "
                                                                call       void [System.Console]System.Console::Write(string)
                                                                ldloc.0
                                                                dup
                                                                call       void [System.Console]System.Console::WriteLine(int32)
                                                                ldc.i4.1
                                                                add
                                                                stloc.0
                                                                leave    done
                                                              } catch [mscorlib]System.Exception  {
                                                                pop
                                                                ldstr      "In catch [mscorlib]System.Exception 29"
                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                label30:
                                                                .try {
                                                                  ldstr      "In try 30, ldloc.0 is "
                                                                  call       void [System.Console]System.Console::Write(string)
                                                                  ldloc.0
                                                                  dup
                                                                  call       void [System.Console]System.Console::WriteLine(int32)
                                                                  ldc.i4.1
                                                                  add
                                                                  stloc.0
                                                                  leave    done
                                                                } catch [mscorlib]System.Exception  {
                                                                  pop
                                                                  ldstr      "In catch [mscorlib]System.Exception 30"
                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                  label31:
                                                                  .try {
                                                                    ldstr      "In try 31, ldloc.0 is "
                                                                    call       void [System.Console]System.Console::Write(string)
                                                                    ldloc.0
                                                                    dup
                                                                    call       void [System.Console]System.Console::WriteLine(int32)
                                                                    ldc.i4.1
                                                                    add
                                                                    stloc.0
                                                                    leave    done
                                                                  } catch [mscorlib]System.Exception  {
                                                                    pop
                                                                    ldstr      "In catch [mscorlib]System.Exception 31"
                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                    label32:
                                                                    .try {
                                                                      ldstr      "In try 32, ldloc.0 is "
                                                                      call       void [System.Console]System.Console::Write(string)
                                                                      ldloc.0
                                                                      dup
                                                                      call       void [System.Console]System.Console::WriteLine(int32)
                                                                      ldc.i4.1
                                                                      add
                                                                      stloc.0
                                                                      leave    done
                                                                    } catch [mscorlib]System.Exception  {
                                                                      pop
                                                                      ldstr      "In catch [mscorlib]System.Exception 32"
                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                      label33:
                                                                      .try {
                                                                        ldstr      "In try 33, ldloc.0 is "
                                                                        call       void [System.Console]System.Console::Write(string)
                                                                        ldloc.0
                                                                        dup
                                                                        call       void [System.Console]System.Console::WriteLine(int32)
                                                                        ldc.i4.1
                                                                        add
                                                                        stloc.0
                                                                        leave    done
                                                                      } catch [mscorlib]System.Exception  {
                                                                        pop
                                                                        ldstr      "In catch [mscorlib]System.Exception 33"
                                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                                        label34:
                                                                        .try {
                                                                          ldstr      "In try 34, ldloc.0 is "
                                                                          call       void [System.Console]System.Console::Write(string)
                                                                          ldloc.0
                                                                          dup
                                                                          call       void [System.Console]System.Console::WriteLine(int32)
                                                                          ldc.i4.1
                                                                          add
                                                                          stloc.0
                                                                          leave    done
                                                                        } catch [mscorlib]System.Exception  {
                                                                          pop
                                                                          ldstr      "In catch [mscorlib]System.Exception 34"
                                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                                          label35:
                                                                          .try {
                                                                            ldstr      "In try 35, ldloc.0 is "
                                                                            call       void [System.Console]System.Console::Write(string)
                                                                            ldloc.0
                                                                            dup
                                                                            call       void [System.Console]System.Console::WriteLine(int32)
                                                                            ldc.i4.1
                                                                            add
                                                                            stloc.0
                                                                            leave    done
                                                                          } catch [mscorlib]System.Exception  {
                                                                            pop
                                                                            ldstr      "In catch [mscorlib]System.Exception 35"
                                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                                            label36:
                                                                            .try {
                                                                              ldstr      "In try 36, ldloc.0 is "
                                                                              call       void [System.Console]System.Console::Write(string)
                                                                              ldloc.0
                                                                              dup
                                                                              call       void [System.Console]System.Console::WriteLine(int32)
                                                                              ldc.i4.1
                                                                              add
                                                                              stloc.0
                                                                              leave    done
                                                                            } catch [mscorlib]System.Exception  {
                                                                              pop
                                                                              ldstr      "In catch [mscorlib]System.Exception 36"
                                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                                              label37:
                                                                              .try {
                                                                                ldstr      "In try 37, ldloc.0 is "
                                                                                call       void [System.Console]System.Console::Write(string)
                                                                                ldloc.0
                                                                                dup
                                                                                call       void [System.Console]System.Console::WriteLine(int32)
                                                                                ldc.i4.1
                                                                                add
                                                                                stloc.0
                                                                                leave    done
                                                                              } catch [mscorlib]System.Exception  {
                                                                                pop
                                                                                ldstr      "In catch [mscorlib]System.Exception 37"
                                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                                label38:
                                                                                .try {
                                                                                  ldstr      "In try 38, ldloc.0 is "
                                                                                  call       void [System.Console]System.Console::Write(string)
                                                                                  ldloc.0
                                                                                  dup
                                                                                  call       void [System.Console]System.Console::WriteLine(int32)
                                                                                  ldc.i4.1
                                                                                  add
                                                                                  stloc.0
                                                                                  leave    done
                                                                                } catch [mscorlib]System.Exception  {
                                                                                  pop
                                                                                  ldstr      "In catch [mscorlib]System.Exception 38"
                                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                                  label39:
                                                                                  .try {
                                                                                    ldstr      "In try 39, ldloc.0 is "
                                                                                    call       void [System.Console]System.Console::Write(string)
                                                                                    ldloc.0
                                                                                    dup
                                                                                    call       void [System.Console]System.Console::WriteLine(int32)
                                                                                    ldc.i4.1
                                                                                    add
                                                                                    stloc.0
                                                                                    leave    done
                                                                                  } catch [mscorlib]System.Exception  {
                                                                                    pop
                                                                                    ldstr      "In catch [mscorlib]System.Exception 39"
                                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                                    label40:
                                                                                    .try {
                                                                                      ldstr      "In try 40, ldloc.0 is "
                                                                                      call       void [System.Console]System.Console::Write(string)
                                                                                      ldloc.0
                                                                                      dup
                                                                                      call       void [System.Console]System.Console::WriteLine(int32)
                                                                                      ldc.i4.1
                                                                                      add
                                                                                      stloc.0
                                                                                      leave    done
                                                                                    } catch [mscorlib]System.Exception  {
                                                                                      pop
                                                                                      ldstr      "In catch [mscorlib]System.Exception 40"
                                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                                      label41:
                                                                                      .try {
                                                                                        ldstr      "In try 41, ldloc.0 is "
                                                                                        call       void [System.Console]System.Console::Write(string)
                                                                                        ldloc.0
                                                                                        dup
                                                                                        call       void [System.Console]System.Console::WriteLine(int32)
                                                                                        ldc.i4.1
                                                                                        add
                                                                                        stloc.0
                                                                                        leave    done
                                                                                      } catch [mscorlib]System.Exception  {
                                                                                        pop
                                                                                        ldstr      "In catch [mscorlib]System.Exception 41"
                                                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                                                        label42:
                                                                                        .try {
                                                                                          ldstr      "In try 42, ldloc.0 is "
                                                                                          call       void [System.Console]System.Console::Write(string)
                                                                                          ldloc.0
                                                                                          dup
                                                                                          call       void [System.Console]System.Console::WriteLine(int32)
                                                                                          ldc.i4.1
                                                                                          add
                                                                                          stloc.0
                                                                                          leave    done
                                                                                        } catch [mscorlib]System.Exception  {
                                                                                          pop
                                                                                          ldstr      "In catch [mscorlib]System.Exception 42"
                                                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                                                          label43:
                                                                                          .try {
                                                                                            ldstr      "In try 43, ldloc.0 is "
                                                                                            call       void [System.Console]System.Console::Write(string)
                                                                                            ldloc.0
                                                                                            dup
                                                                                            call       void [System.Console]System.Console::WriteLine(int32)
                                                                                            ldc.i4.1
                                                                                            add
                                                                                            stloc.0
                                                                                            leave    done
                                                                                          } catch [mscorlib]System.Exception  {
                                                                                            pop
                                                                                            ldstr      "In catch [mscorlib]System.Exception 43"
                                                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                                                            label44:
                                                                                            .try {
                                                                                              ldstr      "In try 44, ldloc.0 is "
                                                                                              call       void [System.Console]System.Console::Write(string)
                                                                                              ldloc.0
                                                                                              dup
                                                                                              call       void [System.Console]System.Console::WriteLine(int32)
                                                                                              ldc.i4.1
                                                                                              add
                                                                                              stloc.0
                                                                                              leave    done
                                                                                            } catch [mscorlib]System.Exception  {
                                                                                              pop
                                                                                              ldstr      "In catch [mscorlib]System.Exception 44"
                                                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                                                              label45:
                                                                                              .try {
                                                                                                ldstr      "In try 45, ldloc.0 is "
                                                                                                call       void [System.Console]System.Console::Write(string)
                                                                                                ldloc.0
                                                                                                dup
                                                                                                call       void [System.Console]System.Console::WriteLine(int32)
                                                                                                ldc.i4.1
                                                                                                add
                                                                                                stloc.0
                                                                                                leave    done
                                                                                              } catch [mscorlib]System.Exception  {
                                                                                                pop
                                                                                                ldstr      "In catch [mscorlib]System.Exception 45"
                                                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                                                label46:
                                                                                                .try {
                                                                                                  ldstr      "In try 46, ldloc.0 is "
                                                                                                  call       void [System.Console]System.Console::Write(string)
                                                                                                  ldloc.0
                                                                                                  dup
                                                                                                  call       void [System.Console]System.Console::WriteLine(int32)
                                                                                                  ldc.i4.1
                                                                                                  add
                                                                                                  stloc.0
                                                                                                  leave    done
                                                                                                } catch [mscorlib]System.Exception  {
                                                                                                  pop
                                                                                                  ldstr      "In catch [mscorlib]System.Exception 46"
                                                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                                                  label47:
                                                                                                  .try {
                                                                                                    ldstr      "In try 47, ldloc.0 is "
                                                                                                    call       void [System.Console]System.Console::Write(string)
                                                                                                    ldloc.0
                                                                                                    dup
                                                                                                    call       void [System.Console]System.Console::WriteLine(int32)
                                                                                                    ldc.i4.1
                                                                                                    add
                                                                                                    stloc.0
                                                                                                    leave    done
                                                                                                  } catch [mscorlib]System.Exception  {
                                                                                                    pop
                                                                                                    ldstr      "In catch [mscorlib]System.Exception 47"
                                                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                                                    label48:
                                                                                                    .try {
                                                                                                      ldstr      "In try 48, ldloc.0 is "
                                                                                                      call       void [System.Console]System.Console::Write(string)
                                                                                                      ldloc.0
                                                                                                      dup
                                                                                                      call       void [System.Console]System.Console::WriteLine(int32)
                                                                                                      ldc.i4.1
                                                                                                      add
                                                                                                      stloc.0
                                                                                                      leave    done
                                                                                                    } catch [mscorlib]System.Exception  {
                                                                                                      pop
                                                                                                      ldstr      "In catch [mscorlib]System.Exception 48"
                                                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                                                      ldloc.0
                                                                                                      brfalse.s next0
                                                                                                      ldloc.0
                                                                                                      ldc.i4 1
                                                                                                      beq.s next0a
                                                                                                      leave begin
                                                                                                    next0a:
                                                                                                      ldstr "Unreached !!"
                                                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                                                      leave done
                                                                                                      leave label47
                                                                                                    next0:
                                                                                                      leave done
                                                                                                    }
                                                                                                    ldloc.0
                                                                                                    brfalse.s next1
                                                                                                    ldloc.0
                                                                                                    ldc.i4 1
                                                                                                    beq.s next1a
                                                                                                    leave begin
                                                                                                  next1a:
                                                                                                    ldstr "Unreached !!"
                                                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                                                    leave done
                                                                                                    leave label46
                                                                                                  next1:
                                                                                                    leave done
                                                                                                  }
                                                                                                  ldloc.0
                                                                                                  brfalse.s next2
                                                                                                  ldloc.0
                                                                                                  ldc.i4 1
                                                                                                  beq.s next2a
                                                                                                  leave begin
                                                                                                next2a:
                                                                                                  ldstr "Unreached !!"
                                                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                                                  leave done
                                                                                                  leave label45
                                                                                                next2:
                                                                                                  leave done
                                                                                                }
                                                                                                ldloc.0
                                                                                                brfalse.s next3
                                                                                                ldloc.0
                                                                                                ldc.i4 1
                                                                                                beq.s next3a
                                                                                                leave begin
                                                                                              next3a:
                                                                                                ldstr "Unreached !!"
                                                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                                                leave done
                                                                                                leave label44
                                                                                              next3:
                                                                                                leave done
                                                                                              }
                                                                                              ldloc.0
                                                                                              brfalse.s next4
                                                                                              ldloc.0
                                                                                              ldc.i4 1
                                                                                              beq.s next4a
                                                                                              leave begin
                                                                                            next4a:
                                                                                              ldstr "Unreached !!"
                                                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                                                              leave done
                                                                                              leave label43
                                                                                            next4:
                                                                                              leave done
                                                                                            }
                                                                                            ldloc.0
                                                                                            brfalse.s next5
                                                                                            ldloc.0
                                                                                            ldc.i4 1
                                                                                            beq.s next5a
                                                                                            leave begin
                                                                                          next5a:
                                                                                            ldstr "Unreached !!"
                                                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                                                            leave done
                                                                                            leave label42
                                                                                          next5:
                                                                                            leave done
                                                                                          }
                                                                                          ldloc.0
                                                                                          brfalse.s next6
                                                                                          ldloc.0
                                                                                          ldc.i4 1
                                                                                          beq.s next6a
                                                                                          leave begin
                                                                                        next6a:
                                                                                          ldstr "Unreached !!"
                                                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                                                          leave done
                                                                                          leave label41
                                                                                        next6:
                                                                                          leave done
                                                                                        }
                                                                                        ldloc.0
                                                                                        brfalse.s next7
                                                                                        ldloc.0
                                                                                        ldc.i4 1
                                                                                        beq.s next7a
                                                                                        leave begin
                                                                                      next7a:
                                                                                        ldstr "Unreached !!"
                                                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                                                        leave done
                                                                                        leave label40
                                                                                      next7:
                                                                                        leave done
                                                                                      }
                                                                                      ldloc.0
                                                                                      brfalse.s next8
                                                                                      ldloc.0
                                                                                      ldc.i4 1
                                                                                      beq.s next8a
                                                                                      leave begin
                                                                                    next8a:
                                                                                      ldstr "Unreached !!"
                                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                                      leave done
                                                                                      leave label39
                                                                                    next8:
                                                                                      leave done
                                                                                    }
                                                                                    ldloc.0
                                                                                    brfalse.s next9
                                                                                    ldloc.0
                                                                                    ldc.i4 1
                                                                                    beq.s next9a
                                                                                    leave begin
                                                                                  next9a:
                                                                                    ldstr "Unreached !!"
                                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                                    leave done
                                                                                    leave label38
                                                                                  next9:
                                                                                    leave done
                                                                                  }
                                                                                  ldloc.0
                                                                                  brfalse.s next10
                                                                                  ldloc.0
                                                                                  ldc.i4 1
                                                                                  beq.s next10a
                                                                                  leave begin
                                                                                next10a:
                                                                                  ldstr "Unreached !!"
                                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                                  leave done
                                                                                  leave label37
                                                                                next10:
                                                                                  leave done
                                                                                }
                                                                                ldloc.0
                                                                                brfalse.s next11
                                                                                ldloc.0
                                                                                ldc.i4 1
                                                                                beq.s next11a
                                                                                leave begin
                                                                              next11a:
                                                                                ldstr "Unreached !!"
                                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                                leave done
                                                                                leave label36
                                                                              next11:
                                                                                leave done
                                                                              }
                                                                              ldloc.0
                                                                              brfalse.s next12
                                                                              ldloc.0
                                                                              ldc.i4 1
                                                                              beq.s next12a
                                                                              leave begin
                                                                            next12a:
                                                                              ldstr "Unreached !!"
                                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                                              leave done
                                                                              leave label35
                                                                            next12:
                                                                              leave done
                                                                            }
                                                                            ldloc.0
                                                                            brfalse.s next13
                                                                            ldloc.0
                                                                            ldc.i4 1
                                                                            beq.s next13a
                                                                            leave begin
                                                                          next13a:
                                                                            ldstr "Unreached !!"
                                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                                            leave done
                                                                            leave label34
                                                                          next13:
                                                                            leave done
                                                                          }
                                                                          ldloc.0
                                                                          brfalse.s next14
                                                                          ldloc.0
                                                                          ldc.i4 1
                                                                          beq.s next14a
                                                                          leave begin
                                                                        next14a:
                                                                          ldstr "Unreached !!"
                                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                                          leave done
                                                                          leave label33
                                                                        next14:
                                                                          leave done
                                                                        }
                                                                        ldloc.0
                                                                        brfalse.s next15
                                                                        ldloc.0
                                                                        ldc.i4 1
                                                                        beq.s next15a
                                                                        leave begin
                                                                      next15a:
                                                                        ldstr "Unreached !!"
                                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                                        leave done
                                                                        leave label32
                                                                      next15:
                                                                        leave done
                                                                      }
                                                                      ldloc.0
                                                                      brfalse.s next16
                                                                      ldloc.0
                                                                      ldc.i4 1
                                                                      beq.s next16a
                                                                      leave begin
                                                                    next16a:
                                                                      ldstr "Unreached !!"
                                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                                      leave done
                                                                      leave label31
                                                                    next16:
                                                                      leave done
                                                                    }
                                                                    ldloc.0
                                                                    brfalse.s next17
                                                                    ldloc.0
                                                                    ldc.i4 1
                                                                    beq.s next17a
                                                                    leave begin
                                                                  next17a:
                                                                    ldstr "Unreached !!"
                                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                                    leave done
                                                                    leave label30
                                                                  next17:
                                                                    leave done
                                                                  }
                                                                  ldloc.0
                                                                  brfalse.s next18
                                                                  ldloc.0
                                                                  ldc.i4 1
                                                                  beq.s next18a
                                                                  leave begin
                                                                next18a:
                                                                  ldstr "Unreached !!"
                                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                                  leave done
                                                                  leave label29
                                                                next18:
                                                                  leave done
                                                                }
                                                                ldloc.0
                                                                brfalse.s next19
                                                                ldloc.0
                                                                ldc.i4 1
                                                                beq.s next19a
                                                                leave begin
                                                              next19a:
                                                                ldstr "Unreached !!"
                                                                call       void [System.Console]System.Console::WriteLine(string)
                                                                leave done
                                                                leave label28
                                                              next19:
                                                                leave done
                                                              }
                                                              ldloc.0
                                                              brfalse.s next20
                                                              ldloc.0
                                                              ldc.i4 1
                                                              beq.s next20a
                                                              leave begin
                                                            next20a:
                                                              ldstr "Unreached !!"
                                                              call       void [System.Console]System.Console::WriteLine(string)
                                                              leave done
                                                              leave label27
                                                            next20:
                                                              leave done
                                                            }
                                                            ldloc.0
                                                            brfalse.s next21
                                                            ldloc.0
                                                            ldc.i4 1
                                                            beq.s next21a
                                                            leave begin
                                                          next21a:
                                                            ldstr "Unreached !!"
                                                            call       void [System.Console]System.Console::WriteLine(string)
                                                            leave done
                                                            leave label26
                                                          next21:
                                                            leave done
                                                          }
                                                          ldloc.0
                                                          brfalse.s next22
                                                          ldloc.0
                                                          ldc.i4 1
                                                          beq.s next22a
                                                          leave begin
                                                        next22a:
                                                          ldstr "Unreached !!"
                                                          call       void [System.Console]System.Console::WriteLine(string)
                                                          leave done
                                                          leave label25
                                                        next22:
                                                          leave done
                                                        }
                                                        ldloc.0
                                                        brfalse.s next23
                                                        ldloc.0
                                                        ldc.i4 1
                                                        beq.s next23a
                                                        leave begin
                                                      next23a:
                                                        ldstr "Unreached !!"
                                                        call       void [System.Console]System.Console::WriteLine(string)
                                                        leave done
                                                        leave label24
                                                      next23:
                                                        leave done
                                                      }
                                                      ldloc.0
                                                      brfalse.s next24
                                                      ldloc.0
                                                      ldc.i4 1
                                                      beq.s next24a
                                                      leave begin
                                                    next24a:
                                                      ldstr "Unreached !!"
                                                      call       void [System.Console]System.Console::WriteLine(string)
                                                      leave done
                                                      leave label23
                                                    next24:
                                                      leave done
                                                    }
                                                    ldloc.0
                                                    brfalse.s next25
                                                    ldloc.0
                                                    ldc.i4 1
                                                    beq.s next25a
                                                    leave begin
                                                  next25a:
                                                    ldstr "Unreached !!"
                                                    call       void [System.Console]System.Console::WriteLine(string)
                                                    leave done
                                                    leave label22
                                                  next25:
                                                    leave done
                                                  }
                                                  ldloc.0
                                                  brfalse.s next26
                                                  ldloc.0
                                                  ldc.i4 1
                                                  beq.s next26a
                                                  leave begin
                                                next26a:
                                                  ldstr "Unreached !!"
                                                  call       void [System.Console]System.Console::WriteLine(string)
                                                  leave done
                                                  leave label21
                                                next26:
                                                  leave done
                                                }
                                                ldloc.0
                                                brfalse.s next27
                                                ldloc.0
                                                ldc.i4 1
                                                beq.s next27a
                                                leave begin
                                              next27a:
                                                ldstr "Unreached !!"
                                                call       void [System.Console]System.Console::WriteLine(string)
                                                leave done
                                                leave label20
                                              next27:
                                                leave done
                                              }
                                              ldloc.0
                                              brfalse.s next28
                                              ldloc.0
                                              ldc.i4 1
                                              beq.s next28a
                                              leave begin
                                            next28a:
                                              ldstr "Unreached !!"
                                              call       void [System.Console]System.Console::WriteLine(string)
                                              leave done
                                              leave label19
                                            next28:
                                              leave done
                                            }
                                            ldloc.0
                                            brfalse.s next29
                                            ldloc.0
                                            ldc.i4 1
                                            beq.s next29a
                                            leave begin
                                          next29a:
                                            ldstr "Unreached !!"
                                            call       void [System.Console]System.Console::WriteLine(string)
                                            leave done
                                            leave label18
                                          next29:
                                            leave done
                                          }
                                          ldloc.0
                                          brfalse.s next30
                                          ldloc.0
                                          ldc.i4 1
                                          beq.s next30a
                                          leave begin
                                        next30a:
                                          ldstr "Unreached !!"
                                          call       void [System.Console]System.Console::WriteLine(string)
                                          leave done
                                          leave label17
                                        next30:
                                          leave done
                                        }
                                        ldloc.0
                                        brfalse.s next31
                                        ldloc.0
                                        ldc.i4 1
                                        beq.s next31a
                                        leave begin
                                      next31a:
                                        ldstr "Unreached !!"
                                        call       void [System.Console]System.Console::WriteLine(string)
                                        leave done
                                        leave label16
                                      next31:
                                        leave done
                                      }
                                      ldloc.0
                                      brfalse.s next32
                                      ldloc.0
                                      ldc.i4 1
                                      beq.s next32a
                                      leave begin
                                    next32a:
                                      ldstr "Unreached !!"
                                      call       void [System.Console]System.Console::WriteLine(string)
                                      leave done
                                      leave label15
                                    next32:
                                      leave done
                                    }
                                    ldloc.0
                                    brfalse.s next33
                                    ldloc.0
                                    ldc.i4 1
                                    beq.s next33a
                                    leave begin
                                  next33a:
                                    ldstr "Unreached !!"
                                    call       void [System.Console]System.Console::WriteLine(string)
                                    leave done
                                    leave label14
                                  next33:
                                    leave done
                                  }
                                  ldloc.0
                                  brfalse.s next34
                                  ldloc.0
                                  ldc.i4 1
                                  beq.s next34a
                                  leave begin
                                next34a:
                                  ldstr "Unreached !!"
                                  call       void [System.Console]System.Console::WriteLine(string)
                                  leave done
                                  leave label13
                                next34:
                                  leave done
                                }
                                ldloc.0
                                brfalse.s next35
                                ldloc.0
                                ldc.i4 1
                                beq.s next35a
                                leave begin
                              next35a:
                                ldstr "Unreached !!"
                                call       void [System.Console]System.Console::WriteLine(string)
                                leave done
                                leave label12
                              next35:
                                leave done
                              }
                              ldloc.0
                              brfalse.s next36
                              ldloc.0
                              ldc.i4 1
                              beq.s next36a
                              leave begin
                            next36a:
                              ldstr "Unreached !!"
                              call       void [System.Console]System.Console::WriteLine(string)
                              leave done
                              leave label11
                            next36:
                              leave done
                            }
                            ldloc.0
                            brfalse.s next37
                            ldloc.0
                            ldc.i4 1
                            beq.s next37a
                            leave begin
                          next37a:
                            ldstr "Unreached !!"
                            call       void [System.Console]System.Console::WriteLine(string)
                            leave done
                            leave label10
                          next37:
                            leave done
                          }
                          ldloc.0
                          brfalse.s next38
                          ldloc.0
                          ldc.i4 1
                          beq.s next38a
                          leave begin
                        next38a:
                          ldstr "Unreached !!"
                          call       void [System.Console]System.Console::WriteLine(string)
                          leave done
                          leave label9
                        next38:
                          leave done
                        }
                        ldloc.0
                        brfalse.s next39
                        ldloc.0
                        ldc.i4 1
                        beq.s next39a
                        leave begin
                      next39a:
                        ldstr "Unreached !!"
                        call       void [System.Console]System.Console::WriteLine(string)
                        leave done
                        leave label8
                      next39:
                        leave done
                      }
                      ldloc.0
                      brfalse.s next40
                      ldloc.0
                      ldc.i4 1
                      beq.s next40a
                      leave begin
                    next40a:
                      ldstr "Unreached !!"
                      call       void [System.Console]System.Console::WriteLine(string)
                      leave done
                      leave label7
                    next40:
                      leave done
                    }
                    ldloc.0
                    brfalse.s next41
                    ldloc.0
                    ldc.i4 1
                    beq.s next41a
                    leave begin
                  next41a:
                    ldstr "Unreached !!"
                    call       void [System.Console]System.Console::WriteLine(string)
                    leave done
                    leave label6
                  next41:
                    leave done
                  }
                  ldloc.0
                  brfalse.s next42
                  ldloc.0
                  ldc.i4 1
                  beq.s next42a
                  leave begin
                next42a:
                  ldstr "Unreached !!"
                  call       void [System.Console]System.Console::WriteLine(string)
                  leave done
                  leave label5
                next42:
                  leave done
                }
                ldloc.0
                brfalse.s next43
                ldloc.0
                ldc.i4 1
                beq.s next43a
                leave begin
              next43a:
                ldstr "Unreached !!"
                call       void [System.Console]System.Console::WriteLine(string)
                leave done
                leave label4
              next43:
                leave done
              }
              ldloc.0
              brfalse.s next44
              ldloc.0
              ldc.i4 1
              beq.s next44a
              leave begin
            next44a:
              ldstr "Unreached !!"
              call       void [System.Console]System.Console::WriteLine(string)
              leave done
              leave label3
            next44:
              leave done
            }
            ldloc.0
            brfalse.s next45
            ldloc.0
            ldc.i4 1
            beq.s next45a
            leave begin
          next45a:
            ldstr "Unreached !!"
            call       void [System.Console]System.Console::WriteLine(string)
            leave done
            leave label2
          next45:
            leave done
          }
          ldloc.0
          brfalse.s next46
          ldloc.0
          ldc.i4 1
          beq.s next46a
          leave begin
        next46a:
          ldstr "Unreached !!"
          call       void [System.Console]System.Console::WriteLine(string)
          leave done
          leave label1
        next46:
          leave done
        }
        ldloc.0
        brfalse.s next47
        ldloc.0
        ldc.i4 1
        beq.s next47a
        leave begin
      next47a:
        ldstr "Unreached !!"
        call       void [System.Console]System.Console::WriteLine(string)
        leave done
        leave label0
      next47:
        leave done
      }
      ldloc.0
      brfalse.s next48
      ldloc.0
      ldc.i4 1
      beq.s next48a
      leave begin
    next48a:
      ldstr "Unreached !!"
      call       void [System.Console]System.Console::WriteLine(string)
      leave done
      leave label0
    next48:
      leave done
    }
  } finally  {
    ldstr      "In finally 0"
    call       void [System.Console]System.Console::WriteLine(string)
    endfinally
  }

  done:
    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
  
    ldloc.s    testLog
    callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
    
    ret
}
}
