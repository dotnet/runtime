#!/usr/bin/perl -w

# perl replacement of genmdesc.c for use when cross-compiling

use strict;
no locale;

# must keep in sync with mini.h
my @spec_names = qw(dest src1 src2 src3 len clob nacl);
sub INST_DEST  () {return 0;}
sub INST_SRC1  () {return 1;}
sub INST_SRC2  () {return 2;}
sub INST_SRC3  () {return 3;}
sub INST_LEN   () {return 4;}
sub INST_CLOB  () {return 5;}
# making INST_NACL the same as INST_MAX is not a mistake,
# INST_NACL writes over INST_LEN, it's not its own field
sub INST_NACL  () {return 6;}
sub INST_MAX   () {return 6;}

# this must include all the #defines used in mini-ops.h
my @defines = qw (__i386__ __x86_64__ __ppc__ __powerpc__ __ppc64__ __arm__ 
	__sparc__ sparc __s390__ s390 __ia64__ __alpha__ __mips__);
my %table =();
my %template_table =();
my @opcodes = ();

my $nacl = 0;

sub parse_file
{
	my ($define, $file) = @_;
	my @enabled = (1);
	my $i = 0;
	open (OPS, $file) || die "Cannot open $file: $!";
	while (<OPS>) {
		if (/^\s*#\s*if\s+(.*)/) {
			my $defines = $1;
			die "fix the genmdesc.pl cpp parser to handle all operators" if /(&&)|([!<>=])/;
			unshift @enabled, scalar ($defines =~ /defined\s*\(\s*$define\s*\)/);
			next;
		}
		if (/^\s*#\s*ifdef\s+(\S+)/) {
			my $defines = $1;
			unshift @enabled, $defines eq $define;
			next;
		}
		if (/^\s*#\s*endif/) {
			shift @enabled;
			next;
		}
		next unless $enabled [0];
		next unless /MINI_OP3?\s*\(\s*(\S+?)\s*,\s*"(.*?)"/;
		my ($sym, $name) = ($1, $2);
		push @opcodes, [$sym, $name];
		$table{$name} = {num => $i, name => $name};
		$i++;
	}
	close (OPS);
}

sub load_opcodes
{
	my ($srcdir, $arch) = @_;
	my $opcodes_def = "$srcdir/../cil/opcode.def";
	my $i = 0;
	my $arch_found = 0;

	my $cpp = $ENV{"CPP"};
	$cpp = "cpp" unless defined $cpp;
	$cpp .= " -undef ";
	foreach (@defines) {
		$cpp .= " -U$_";
		$arch_found = 1 if $arch eq $_;
	}
	die "$arch arch is not supported.\n" unless $arch_found;

	my $arch_define = $arch;
	if ($arch =~ "__i386__") {
		$arch_define = "TARGET_X86";
	}
	if ($arch =~ "__x86_64__") {
		$arch_define = "TARGET_AMD64";
	}
	if ($arch =~ "__arm__") {
		$arch_define = "TARGET_ARM";
	}

	parse_file ($arch_define, "$srcdir/mini-ops.h");
	return;
	$cpp .= " -D$arch_define $srcdir/mini-ops.h|";
	#print "Running: $cpp\n";
	open (OPS, $cpp) || die "Cannot execute cpp: $!";
	while (<OPS>) {
		next unless /MINI_OP3?\s*\(\s*(\S+?)\s*,\s*"(.*?)"/;
		my ($sym, $name) = ($1, $2);
		push @opcodes, [$sym, $name];
		$table{$name} = {num => $i, name => $name};
		$i++;
	}
	close (OPS);
}

sub load_file {
	my ($name) = @_;
	my $line = 0;
	my $comment = "";

	open (DESC, $name) || die "Cannot open $name: $!";
	while (<DESC>) {
		my $is_template = 0;
		$line++;
		next if /^\s*$/;
		if (/^\s*(#.*)?$/) {
			$comment .= "$1\n";
			next;
		}
		my @values = split (/\s+/);
		next unless ($values [0] =~ /(\S+?):/);
		my $name = $1;
		my $desc;
		if ($name eq "template") {
			$is_template = 1;
			$desc = {};
		} else {
			$desc = $table {$name};
			die "Invalid opcode $name at line $line\n" unless defined $desc;
			die "Duplicated opcode $name at line $line\n" if $desc->{"desc"};
		}
		shift @values;
		$desc->{"desc"} = $_;
		$desc->{"comment"} = $comment;
		$desc->{"spec"} = {};
		$comment = "";
		#print "values for $name: " . join (' ', @values) . " num: " . int(@values), "\n";
		for my $val (@values) {
			if ($val =~ /(\S+):(.*)/) {
				if ($1 eq "name") {
					die "name tag only valid in templates at line $line\n" unless $is_template;
					die "Duplicated name tag in template $desc->{'name'} at line $line\n" if defined $desc->{'name'};
					die "Duplicated template $2 at line $line\n" if defined $template_table {$2};
					$desc->{'name'} = $2;
					$template_table {$2} = $desc;
				} elsif ($1 eq "template") {
					my $tdesc = $template_table {$2};
					die "Invalid template name $2 at line $line\n" unless defined $tdesc;
					$desc->{"spec"} = {%{$tdesc->{"spec"}}};
				} else {
					$desc->{"spec"}->{$1} = $2;
				}
			}
		}
		die "Template without name at line $1" if ($is_template && !defined ($desc->{'name'}));
	}
	close (DESC);
}

sub build_spec {
	my ($spec) = shift;
	my %spec = %{$spec};
	my @vals = ();
	foreach (@spec_names) {
		my $val = $spec->{$_};
		if (defined $val) {
			push @vals, $val;
		} else {
			push @vals, undef;
		}
	}
	#print "vals: " . join (' ', @vals) . "\n";
	my $res = "";
	my $n = 0;
	for (my $i = 0; $i < @vals; ++$i) {
		next if $i == INST_NACL;
		if (defined $vals [$i]) {
			if ($i == INST_LEN) {
			        $n = $vals [$i];
			        if ((defined $vals [INST_NACL]) and $nacl == 1){
				    $n = $vals [INST_NACL];
			        }
				$res .= sprintf ("\\x%x\" \"", + $n);
			} else {
				if ($vals [$i] =~ /^[a-zA-Z0-9]$/) {
					$res .= $vals [$i];
				} else {
					$res .= sprintf ("\\x%x\" \"", $vals [$i]);
				}
			}
		} else {
			$res .= "\\x0\" \"";
		}
	}
	return $res;
}

sub build_table {
	my ($fname, $name) = @_;
	my $i;
	my $idx;
	my $idx_array = "const guint16 ${name}_idx [] = {\n";

	open (OUT, ">$fname") || die "Cannot open file $fname: $!";
	print OUT "/* File automatically generated by genmdesc, don't change */\n\n";
	print OUT "const char $name [] = {\n";
	print OUT "\t\"" . ("\\x0" x INST_MAX) . "\"\t/* null entry */\n";
	$idx = 1;

	for ($i = 0; $i < @opcodes; ++$i) {
		my $name = $opcodes [$i]->[1];
		my $desc = $table {$name};
		my $spec = $desc->{"spec"};
		if (defined $spec) {
			print OUT "\t\"";
			print OUT build_spec ($spec);
			print OUT "\"\t/* $name */\n";
			my $pos = $idx * INST_MAX;
			$idx_array .= "\t$pos,\t/* $name */\n";
			++$idx;
		} else {
			$idx_array .= "\t0,\t/* $name */\n";
		}
	}
	print OUT "};\n\n";
	print OUT "$idx_array};\n\n";
	close (OUT);
}

sub usage {
	die "genmdesc.pl arch srcdir [--nacl] output name desc [desc2 ...]\n";
}

my $arch = shift || usage ();
my $srcdir = shift || usage ();
my $output = shift || usage ();
if ($output eq "--nacl")
{
  $nacl = 1;  
  $output = shift || usage();
}
my $name = shift || usage ();
usage () unless @ARGV;
my @files = @ARGV;

load_opcodes ($srcdir, $arch);
foreach my $file (@files) {
	load_file ($file);
}
build_table ($output, $name);

