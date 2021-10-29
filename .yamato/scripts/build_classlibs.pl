use strict;
use warnings;
use File::Copy::Recursive qw(dircopy);
use Cwd qw();

my $path = Cwd::cwd();
print("cwsd: $path\n");

unless (-e "incomingbuilds" or mkdir("incomingbuilds"))
{
    die "Unable to create directory incomingbuilds";
}

my @hostPlatforms = ("windows", "OSX", "Linux");

foreach my $hostPlatform(@hostPlatforms)
{
    system ("build.cmd", "libs", "-os $hostPlatform", "-c release") eq 0 or die ("Failed to make $hostPlatform host platform class libraries\n");

    dircopy ("artifacts/bin/runtime/net7.0-$hostPlatform-Release-x64", "incomingbuilds/coreclrjit-$hostPlatform") or die $!;

    system ("taskkill", "/IM", "\"dotnet.exe\"", "/F");

    system ("build.cmd", "-clean");
}