#!/bin/bash

# On Linux/Mac, run the emit unit tests and compare the output against capstone.
#
# args:
# -s : Setup the tools. Checkout and build capstone. Build the dummy app.
# -c : Sets CORE_ROOT used to run the tests.
# -g : The Group of tests to run. Used for DOTNET_JitDumpEmitUnitTests.
# -v : Use regex to search the generated clr output. Useful when adding new instructions during development.
#

# Show all commands run and exit on any errors

set -xeuo pipefail
cd "$(dirname "$0")"

# Parse command line

usage="$0 [-s] [-c core_root] [-g group] [-v ins_regex]"
group=all
search=
setup=
verbose=
while getopts sc:g:v: line
do
    case $line in
    c) export CORE_ROOT=$OPTARG;;
    g) group=$OPTARG;;
    s) setup=1;;
    v) verbose=$OPTARG;;
    *) echo $usage; exit 2;;
    esac
done

arch=$(uname -m)
if [ "$arch" == 'aarch64' ]; then
  clr_arch=arm64
elif [ "$arch" == 'x86_64' ]; then
  arch=x64
  clr_arch=x64
fi

artifacts_dir="$(pwd)/../../../artifacts/bin/emitUnitTests"

# Setup capstone

capstone_directory=$artifacts_dir/capstone

if [ -n "$setup" ] || [ ! -d "$capstone_directory" ]; then
    rm -fr "$capstone_directory"
    git clone --quiet --depth 1 -b capstone-jit2-formatting https://github.com/TIHan/capstone.git "$capstone_directory"
    cd "$capstone_directory"
    ./make.sh
    cd -
fi

cstool=$(realpath "$capstone_directory/cstool/cstool")

# Setup dummy app

app_dir="$artifacts_dir/$clr_arch/Release/publish"

if [ -n "$setup" ] || [ ! -d "$app_dir" ]; then
    cd emitUnitTests
    dotnet publish -c Release
    cd -
fi

app_dll="$app_dir/emitUnitTests.dll"
app_dll=$(realpath $app_dll)

# Set env

output_dir=$(mktemp -d)

# Add the emit tests to the end of Main
export DOTNET_JitEmitUnitTests=Main
export DOTNET_JitEmitUnitTestsSections=$group
# Dump the disassembly for Main to a file
export DOTNET_JitDisasm=Main
export DOTNET_JitStdOutFile=$output_dir/clr_output.txt
# Dump the hex for Main to a file
export DOTNET_JitRawHexCode=Main
export DOTNET_JitRawHexCodeFile=$output_dir/clr_hex.txt

# Run the dummy app in clr

$CORE_ROOT/corerun $app_dll

# Extract the instructions from the clr output, from the first NOP to the first set of 2 NOPS.

grep "^         " $output_dir/clr_output.txt \
| tr -s ' \t' ' ' \
| awk '{ $1=$1; if ($1=="nop") { go=1; if (prev=="nop") fin=1; } if (go && !fin) print $0; prev=$1 }' \
> $output_dir/clr_instrs.txt

# Run the raw hex through capstone

$cstool $arch $output_dir/clr_hex.txt > $output_dir/capstone_output.txt

# Extract the instructions from the capstone output, from the first NOP to the first set of 2 NOPS.

cut -f 3- -d ' ' $output_dir/capstone_output.txt \
| tr -s ' \t' ' ' \
| awk '{ $1=$1; if ($1=="nop") { go=1; if (prev=="nop") fin=1; } if (go && !fin) print $0; prev=$1 }' \
> $output_dir/capstone_instrs.txt

# Show some of the output

if [ -n "$verbose" ]; then
    egrep "$verbose" $output_dir/clr_instrs.txt
else
    (head -n 5; tail -n 5) < $output_dir/clr_instrs.txt
fi

# Diff capstone and clr

diff $output_dir/clr_instrs.txt $output_dir/capstone_instrs.txt

rm -fr $output_dir
echo PASSED
