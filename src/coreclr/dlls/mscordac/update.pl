#!perl -w

#
# Renames the DAC to a long name form that windbg looks for
#

my $sSrcFile = shift or &Usage();
my $sDestName = shift or &Usage();
my $sHostMach = shift or &Usage();
my $sTargMach = shift or &Usage();
my $sVersion = shift or &Usage();
my $sDestDir = shift or &Usage();

my $sName = "$sDestDir\\${sDestName}_${sHostMach}_${sTargMach}_" .
            "$sVersion";

if ($ENV{'_BuildType'} eq "dbg" ||
    $ENV{'_BuildType'} eq "chk") {
    $sName .= "." . $ENV{'_BuildType'};
}

$sName .= ".dll";

if (system("copy $sSrcFile $sName") / 256) {
    die("$0: Unable to copy $sSrcFile to $sName\n");
}

exit 0;

sub Usage
{
    die("usage: $0 <srcfile> <destname> <hostmach> <targmach> " .
        "<version> <destdir> <applycommand>\n");
}
