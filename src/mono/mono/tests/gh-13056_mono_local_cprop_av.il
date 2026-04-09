.assembly mono_local_cprop_av {}
.assembly extern mscorlib{auto}
.class mono_local_cprop_av {
.method static int32 Main() {
 .entrypoint
 .maxstack	12

    ldc.i8 4
    ldc.i8 2
   cgt
   ldc.i4 2
  div
 ret
}
}
