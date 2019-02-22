.assembly mono_local_emulate_ops_av{}
.assembly extern mscorlib{auto}
.class mono_local_emulate_ops_av{
.method static int32 Main() {
.entrypoint
.maxstack       32

    ldc.r8 42.58279566

Start_Orphan:
    ldc.i8 33
    ldc.i8 32
   cgt
  pop
End_Orphan:

   conv.ovf.i4
   ldc.i4 42
   sub
   /* should be return code 0 */
  ret
 }
}
