# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
# GENREFOPS.PL
#
# PERL script used to generate the numbering of the reference opcodes
#
#use strict 'vars';
#use strict 'subs';
#use strict 'refs';

print "Reference opcodes\n";
print "This file is presently only for human consumption\n";
print "This file is generated from opcode.def using the genrops.pl script\n\n";
print "Name                     String Name              refop    encode\n";
print "-----------------------------------------------------------------\n";

my $ret = 0;
my %oneByte;
my %twoByte;
$count = 0;
while (<>)
{
   # Process only OPDEF(....) lines
   if (/OPDEF\(\s*/)
   {
      chop;               # Strip off trailing CR
       s/^OPDEF\(\s*//;    # Strip off "OP("
       s/\)$//;            # Strip off ")" at end
       s/,\s*/,/g;         # Remove whitespace

       # Split the line up into its basic parts
       ($enumname, $stringname, $pop, $push, $operand, $type, $size, $s1, $s2, $ctrl) = split(/,/);
        $s1 =~ s/0x//;
        $s1 = hex($s1);
        $s2 =~ s/0x//;
        $s2 = hex($s2);


        my $line = sprintf("%-24s %-24s 0x%03x",
                           $enumname, $stringname, $count);
        if ($size == 1) {
            $line .=  sprintf("    0x%02x\n", $s2);
            if ($oneByte{$s2}) {
                printf("Error opcode 0x%x  already defined!\n", $s2);
                print "   Old = $oneByte{$s2}";
                print "   New = $line";
                $ret = -1;
                }
            $oneByte{$s2} = $line;
            }
        elsif ($size == 2) {
            if ($twoByte{$s2}) {
                printf("Error opcode 0x%x 0x%x  already defined!\n", $s1, $s2);
                print "   Old = $twoByte{$s2}";
                print "   New = $line";
                $ret = -1;
                }
            $line .= sprintf("    0x%02x 0x%02x\n", $s1, $s2);
            $twoByte{$s2 + 256 * $s1} = $line;
            }
        else {
            $line .= "\n";
            push(@deprecated, $line);
            }
        $count++;
   }
}

my $opcode;
my $lastOp = -1;
foreach $opcode (sort {$a <=> $b} keys(%oneByte)) {
    printf("***** GAP %d instrs ****\n", $opcode - $lastOp) if ($lastOp + 1 != $opcode && $lastOp > 0);
    print $oneByte{$opcode};
    $lastOp = $opcode;
}

$lastOp = -1;
foreach $opcode (sort {$a <=> $b} keys(%twoByte)) {
    printf("***** GAP %d instrs ****\n", $opcode - $lastOp) if ($lastOp + 1 != $opcode && $lastOp > 0);
    print $twoByte{$opcode};
    $lastOp = $opcode;
}

print @deprecated;

exit($ret);



