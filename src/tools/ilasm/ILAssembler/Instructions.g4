lexer grammar Instructions;

INSTR_NONE: ('nop'|'break'|'ldarg.0'|'ldarg.1'|'ldarg.2'|'ldarg.3'|'ldloc.0'|'ldloc.1'|'ldloc.2'|'ldloc.3'|'stloc.0'|'stloc.1'|'stloc.2'|'stloc.3'|'ldnull'|'ldc.i4.m1'|'ldc.i4.0'|'ldc.i4.1'|'ldc.i4.2'|'ldc.i4.3'|'ldc.i4.4'|'ldc.i4.5'|'ldc.i4.6'|'ldc.i4.7'|'ldc.i4.8'|'dup'|'pop'|'ret'|'ldind.i1'|'ldind.u1'|'ldind.i2'|'ldind.u2'|'ldind.i4'|'ldind.u4'|'ldind.i8'|'ldind.i'|'ldind.r4'|'ldind.r8'|'ldind.ref'|'stind.ref'|'stind.i1'|'stind.i2'|'stind.i4'|'stind.i8'|'stind.r4'|'stind.r8'|'add'|'sub'|'mul'|'div'|'div.un'|'rem'|'rem.un'|'and'|'or'|'xor'|'shl'|'shr'|'shr.un'|'neg'|'not'|'conv.i1'|'conv.i2'|'conv.i4'|'conv.i8'|'conv.r4'|'conv.r8'|'conv.u4'|'conv.u8'|'conv.r.un'|'throw'|'conv.ovf.i1.un'|'conv.ovf.i2.un'|'conv.ovf.i4.un'|'conv.ovf.i8.un'|'conv.ovf.u1.un'|'conv.ovf.u2.un'|'conv.ovf.u4.un'|'conv.ovf.u8.un'|'conv.ovf.i.un'|'conv.ovf.u.un'|'ldlen'|'ldelem.i1'|'ldelem.u1'|'ldelem.i2'|'ldelem.u2'|'ldelem.i4'|'ldelem.u4'|'ldelem.i8'|'ldelem.i'|'ldelem.r4'|'ldelem.r8'|'ldelem.ref'|'stelem.i'|'stelem.i1'|'stelem.i2'|'stelem.i4'|'stelem.i8'|'stelem.r4'|'stelem.r8'|'stelem.ref'|'conv.ovf.i1'|'conv.ovf.u1'|'conv.ovf.i2'|'conv.ovf.u2'|'conv.ovf.i4'|'conv.ovf.u4'|'conv.ovf.i8'|'conv.ovf.u8'|'ckfinite'|'conv.u2'|'conv.u1'|'conv.i'|'conv.ovf.i'|'conv.ovf.u'|'add.ovf'|'add.ovf.un'|'mul.ovf'|'mul.ovf.un'|'sub.ovf'|'sub.ovf.un'|'endfinally'|'stind.i'|'conv.u'|'prefix7'|'prefix6'|'prefix5'|'prefix4'|'prefix3'|'prefix2'|'prefix1'|'prefixref'|'arglist'|'ceq'|'cgt'|'cgt.un'|'clt'|'clt.un'|'localloc'|'endfilter'|'volatile.'|'tail.'|'cpblk'|'initblk'|'rethrow'|'refanytype'|'readonly.'|'illegal'|'endmac');
INSTR_VAR: ('ldarg.s'|'ldarga.s'|'starg.s'|'ldloc.s'|'ldloca.s'|'stloc.s'|'ldarg'|'ldarga'|'starg'|'ldloc'|'ldloca'|'stloc');
INSTR_I: ('ldc.i4.s'|'ldc.i4'|'unaligned.'|'no.');
INSTR_I8: ('ldc.i8');
INSTR_R: ('ldc.r4'|'ldc.r8');
INSTR_METHOD: ('jmp'|'call'|'callvirt'|'newobj'|'ldftn'|'ldvirtftn');
INSTR_SIG: ('calli');
INSTR_BRTARGET: ('br.s'|'brfalse.s'|'brtrue.s'|'beq.s'|'bge.s'|'bgt.s'|'ble.s'|'blt.s'|'bne.un.s'|'bge.un.s'|'bgt.un.s'|'ble.un.s'|'blt.un.s'|'br'|'brfalse'|'brtrue'|'beq'|'bge'|'bgt'|'ble'|'blt'|'bne.un'|'bge.un'|'bgt.un'|'ble.un'|'blt.un'|'leave'|'leave.s');
INSTR_SWITCH: ('switch');
INSTR_TYPE: ('cpobj'|'ldobj'|'castclass'|'isinst'|'unbox'|'stobj'|'box'|'newarr'|'ldelema'|'ldelem'|'stelem'|'unbox.any'|'refanyval'|'mkrefany'|'initobj'|'constrained.'|'sizeof');
INSTR_STRING: ('ldstr');
INSTR_FIELD: ('ldfld'|'ldflda'|'stfld'|'ldsfld'|'ldsflda'|'stsfld');
INSTR_TOK: ('ldtoken');
