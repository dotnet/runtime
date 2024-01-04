use strict;
use warnings;
use File::Find::Rule;
use File::Copy::Recursive qw(dircopy);
use Cwd;

my $num_args = $#ARGV + 1;
my $dir1;
my $dir2;
my $output_dir;

if ($num_args == 0 || "$ARGV[0]" eq "--help")
{
    print (<<HELP);
The Liponator will lipo two directories together. It will find all dylibs and combine them to create
a universal x64arm64 fat library for use. Arguments 1 and 2 are the input directories while the
optional third argument is the output directory. If argument 3 is not supplied the directory will
be created in the current working directory

Assumptions:
1. The provided directories for parameter 1 and 2 will share the same structure
2. The provided directories target directory will be in the form of "osx-<architecture>"
HELP
    die;
}

if ($num_args == 1 || $num_args > 3)
{
    die ("Incorrect number of arguments provided");
}

for my $dir (@ARGV)
{
    if (! -d "$dir")
    {
        die("$dir is not a valid directory");
    }
}

$dir1 = "$ARGV[0]";
$dir2 = "$ARGV[1]";

$output_dir = getcwd();

if ($num_args == 3)
{
    $output_dir = "$ARGV[2]";
}

print (">>The Liponator will create universal libraries in directory: $output_dir\n");
print (getcwd() . "\n");

# Mush both directories into one so we have all the files
dircopy("$dir1/*", "$output_dir/.");
dircopy("$dir2/*", "$output_dir/.");

my @files = File::Find::Rule->file()
                            ->name("*.dylib")
                            ->in("$output_dir");

$dir1 =~ m/([^-]+)$/;
my $arch1 = $1;
$dir2 =~ m/([^-]+)$/;
my $arch2 = $1;
$output_dir =~ m/([^-]+)$/;
my $arch3 = $1;
print (">>arch1 = $arch1 -- arch2 = $arch2 -- arch3 = $arch3\n");

for my $file (@files)
{
    my $file1 = $file;
    $file1 =~ s/$arch3/$arch1/g;
    my $file2 = $file;
    $file2 =~ s/$arch3/$arch2/g;

    print (">> lipo $file1 $file2 -create -output $file\n");
    system ("lipo", "$file1", "$file2", "-create", "-output", "$file") eq 0 or die ("Failed to lipo");
    system ("codesign", "-s", "-", "-f", "$file") eq 0 or die ("Failed to codesign file $file");
}

