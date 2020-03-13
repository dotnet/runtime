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
# or maybe just an XML::Parser... - lupus

use strict;
use XML::Parser;

my %valid_flow;
# the XML file also includes "throw"
@valid_flow{qw(next call return branch meta cond-branch)} = ();

open OUTPUT, ">$ARGV[1]" || die "Can not create $ARGV[1] file: $!";

my $parser = new XML::Parser (Handlers => {Start => \&handle_opcode});
print_header();
$parser->parsefile($ARGV[0]);
print_trailer();
close(OUTPUT) || die "Can not close file: $!";

sub handle_opcode {
    my ($parser, $elem, %attrs) = @_;
    my ($count);
	
    return if ($elem ne 'opcode');

    my ($name, $input, $output, $args, $o1, $o2, $flow, $constant) =
		@attrs{qw(name input output args o1 o2 flow constant)};

    $constant ||= 0;
    my $uname = uc $name;
    $uname =~ tr/./_/;
    if (hex($o1) == 0xff) {
	$count = 1;
    } else {
	$count = 2;
    }

    my $ff = "ERROR";
    if (exists $valid_flow{$flow}) {
	$ff = uc $flow;
	$ff =~ tr/-/_/;
    }

    print OUTPUT "OPDEF(CEE_$uname, \"$name\", $input, $output, $args, $constant, $count, $o1, $o2, $ff)\n";
    
}

sub print_header {
print OUTPUT<<EOF;
/* GENERATED FILE, DO NOT EDIT. Edit cil-opcodes.xml instead and run "make opcode.def" to regenerate. */
EOF
}

sub print_trailer {
print OUTPUT<<EOF;
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
}

