#!/usr/bin/perl
#
# make-opcodes-def.pl: Loads the opcodes from the CIL-opcodes.xml and
# generates a spec compliant opcodes.def file
#
# Author: 
#   Miguel de Icaza (miguel@ximian.com)
#
# (C) 2001 Ximian, Inc.
#
# We should really be doing this with XSLT, but I know nothing about XSLT
# ;-)


open OPCODES, "cil-opcodes.xml" || die "Can not open cil-opcodes.xml";
open OUTPUT, ">opcode.def" || die "Can not create opcode.def file";

while (<OPCODES>){
    chop;
    next if (!/<opcode .*\/>/);

    ($name, $input, $output, $args, $o1, $o2, $flow) = $_ =~ /name=\"([\w\.]+)\"\s+input=\"([\w+]+)\"\s+output=\"([\w+]+)\"\s+args=\"(\w+)\"\s+o1=\"0x(\w+)\"\s+o2=\"0x(\w+)\"\s+flow=\"([\w-]+)\"\/>/;
    print "NAME: $1\n";
    $name = $1;
    $input = $2;
    $output = $3;
    $args = $4;
    $o1 = $5;
    $o2 = $6;
    $flow = $7;

    $uname = $name;
    $uname =~ s/\./_/g;
    $uname =~ tr [a-z] [A-Z];
    if ($o1 =~ /0xff/){
	$count = 1;
    } else {
	$count = 2;
    }

    $ff = "ERROR";
    $ff = "NEXT" if ($flow =~ /^next$/);
    $ff = "CALL" if ($flow =~ /^call$/);
    $ff = "RETURN" if ($flow =~ /^return$/);
    $ff = "BRANCH" if ($flow =~ /^branch$/);
    $ff = "COND_BRANCH" if ($flow =~ /^cond-branch$/);
    $ff = "META" if ($flow =~ /^meta$/);

    print OUTPUT "OPDEF(CEE_$uname, \"$name\", $input, $output, $args, X, $count, 0x$o1, 0x$o2, $ff)\n";
    
}

print OUTPUT<<EOF
#ifndef OPALIAS
#define _MONO_CIL_OPALIAS_DEFINED_
#define OPALIAS(a,s,r)
#endif

OPALIAS(CEE_BRNULL,     "brnull",    CEE_BRFALSE)
OPALIAS(CEE_BRNULL_S,   "brnull.s",  CEE_BRFALSE_S)
OPALIAS(CEE_BRZERO,     "brzero",    CEE_BRFALSE)
OPALIAS(CEE_BRZERO_S,   "brzero.s",  CEE_BRFALSE_S)
OPALIAS(CEE_BRINST,     "brinst",    CEE_BRTRUE)
OPALIAS(CEE_BRINST_S,   "brinst.s",  CEE_BRTRUE_S)
OPALIAS(CEE_LDIND_U8,   "ldind.u8",  CEE_LDIND_I8)
OPALIAS(CEE_LDELEM_U8,  "ldelem.u8", CEE_LDELEM_I8)
OPALIAS(CEE_LDX_I4_MIX, "ldc.i4.M1", CEE_LDC_I4_M1)
OPALIAS(CEE_ENDFAULT,   "endfault",  CEE_ENDFINALLY)

#ifdef _MONO_CIL_OPALIAS_DEFINED_
#undef OPALIAS
#undef _MONO_CIL_OPALIAS_DEFINED_
#endif
EOF
