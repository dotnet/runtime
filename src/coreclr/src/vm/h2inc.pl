# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

# C to MASM include file translator
# This is replacement for the deprecated h2inc tool that used to be part of VS.

use File::Basename;

sub ProcessFile($) {
    my ($input_file) = @_;

    local *INPUT_FILE;
    if (!open(INPUT_FILE, $input_file))
    {
        print "#error: File can not be opened: $input_file\n";
        return;
    }

    print ("// File start: $input_file\n");

    while(<INPUT_FILE>) {
        # Skip all pragmas
        if (m/^\s*#\s*pragma/) {
            next;
        }

        # Expand includes.
        if (m/\s*#\s*include\s*\"(.+)\"/) {
            ProcessFile(dirname($input_file) . "/" . $1);
            next;
        }

        # Augment #defines with their MASM equivalent
        if (m/^\s*#\s*define\s+(\S+)\s+(.*)/) {
            my $name = $1;
            my $value = $2;

            # Note that we do not handle multiline constants

            # Strip comments from value
            $value =~ s/\/\/.*//;
            $value =~ s/\/\*.*\*\///g;

            # Strip whitespaces from value
            $value =~ s/\s+$//;

            # ignore #defines with arguments
            if (!($name =~ m/\(/)) {
                my $number = 0;
                $number |= ($value =~ s/\b0x(\w+)\b/0\1h/g);    # Convert hex constants
                $number |= ($value =~ s/(-?\b\d+\b)/\1t/g);     # Convert dec constants
                print $number ? "$name EQU $value\n" : "$name TEXTEQU <$value>\n";
            }
        }
        print;
    }

    print ("// File end: $input_file\n");
}

ProcessFile($ARGV[0]);
