# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
# a simple script that extracts the grammar from a yacc file

undef $/;			# read in the whole file
my $file = <>;
$file =~ /^(.*)%%(.*)%%/s || die "Could not find %% markers";
my $prefix = $1;
my $grammar = $2;

#my $line;
#foreach $line (split /\n/s, $prefix) {
#	if ($line =~ /^\s*%token/) {
#		$line =~ s/\s*<.*>//g;
#		print "$line\n"
#	}
#}

	# remove any text in {}
while ($grammar =~ s/\s*([^']){[^{}]*}/$1/sg) {}

	# change keyword identifiers into the string they represent
$grammar =~ s/\b([A-Z0-9_]+)_\b/'\L$1\E'/sg;

	# change assembler directives into their string
$grammar =~ s/\b_([A-Z0-9]+)\b/'\L.$1\E'/sg;

	# do the special punctuation by hand
$grammar =~ s/\bELLIPSIS\b/'...'/sg;
$grammar =~ s/\bDCOLON\b/'::'/sg;

#<STRIP>
	# remove TODO comments
$grammar =~ s/\n\s*\/\*[^\n]*TODO[^\n]*\*\/\s*\n/\n/sg;
#</STRIP>

print "Lexical tokens\n";
print "    ID - C style alphaNumeric identifier (e.g. Hello_There2)\n";
print "    DOTTEDNAME - Sequence of dot-separated IDs (e.g. System.Object)\n";
print "    QSTRING  - C style quoted string (e.g.  \"hi\\n\")\n";
print "    SQSTRING - C style singlely quoted string(e.g.  'hi')\n";
print "    INT32    - C style 32 bit integer (e.g.  235,  03423, 0x34FFF)\n";
print "    INT64    - C style 64 bit integer (e.g.  -2353453636235234,  0x34FFFFFFFFFF)\n";
print "    FLOAT64  - C style floating point number (e.g.  -0.2323, 354.3423, 3435.34E-5)\n";
print "    INSTR_*  - IL instructions of a particular class (see opcode.def).\n";
print "    HEXBYTE  - 1- or 2-digit hexadecimal number (e.g., A2, F0).\n";
print "Auxiliary lexical tokens\n";
print "    TYPEDEF_T - Aliased class (TypeDef or TypeRef).\n";
print "    TYPEDEF_M - Aliased method.\n";
print "    TYPEDEF_F - Aliased field.\n";
print "    TYPEDEF_TS - Aliased type specification (TypeSpec).\n";
print "    TYPEDEF_MR - Aliased field/method reference (MemberRef).\n";
print "    TYPEDEF_CA - Aliased Custom Attribute.\n";
print "----------------------------------------------------------------------------------\n";
print "START           : decls\n";
print "                ;";

print $grammar;
