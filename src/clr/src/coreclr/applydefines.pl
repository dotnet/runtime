#!/usr/bin/env perl

use strict;

my $sourceFile;
my $outputFile="";
my $definesFile="";

#parse arguments

if (@ARGV == 0)
{
   Usage();
}

my %Defines;

# parse args

while (@ARGV)
{
    my $nextArg=shift;
    if($nextArg eq '-s')
    {
        NeedNextArg($nextArg, 'file name');
        $sourceFile=shift;
    }
    elsif ($nextArg eq '-o')
    {
        NeedNextArg($nextArg, 'file name');
        $outputFile=shift;
    }
    elsif ($nextArg eq '-f')
    {
        NeedNextArg($nextArg, 'file name');
        $definesFile=shift;
    }
    elsif ($nextArg eq '-d')
    {
        NeedNextArg($nextArg, 'value');
        my $customDefine=shift;
        if ( $customDefine=~m/^\"?(\S+)=(\S*)\"?$/ )
        {
           $Defines{$1}=$2;
        }
        else
        {
           print "-d expects name=value\n";
           Usage();
        }
    }
    elsif ($nextArg eq '-h')
    {
        Usage();
    }
    else
    {
        print "Unknown argument '$nextArg'\n";
        Usage();
    }
}

# check if we have what we need

if ($sourceFile eq "" || $outputFile eq "" || $definesFile eq "")
{
	Usage();
}

open (SOURCEFILE,$sourceFile) or die "Cannot open $sourceFile for reading\n";
open (DEFINESFILE,$definesFile) or die "Cannot open $definesFile for reading\n";
open (OUTPUTFILE,"> $outputFile") or die "Cannot open $outputFile for writing\n";

#load defines

while (<DEFINESFILE>)
{
	chomp;
	if (/^\s*#define\s+(\S+)\s+(\S*)\s*$/)
	{
		if (defined $2)
		{
			$Defines{$1}=$2;
		}
		else
		{
			$Defines{$1}="";
		}
	}
}

while (<SOURCEFILE>)
{
    my $string=$_;
    my $processed="";
    while ($string=~m/\$\(([^)]+)\)/)
    {
		if (! defined $Defines{$1})
		{
			die "'$1' is not defined.\n";
		}
		$string=~s/\$\(([^)]+)\)/$Defines{$1}/;
    }
    print OUTPUTFILE $string ;
}


# functions
sub Usage()
{
    print "Usage: applydefines [options]\n";
    print "\t-s <file>\t: the source file to process\n";
    print "\t-f <file>\t: the file containing #define settings\n";
    print "\t-o <file>\t: the output file\n";
    print "\t-d <name>=<Value>\t: additional define\n";
    
    exit 1;
}

sub NeedNextArg()
{
    if (@ARGV == 0)
    {
        print "'@_[0]' requires @_[1]\n";
        Usage();
    }
}

