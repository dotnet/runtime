#!/usr/bin/env bash

# This script runs the steps required to create the debugger disassembly tables.
# After running, and checking the result, replace the existing ../amd64InstrDecode.h
# with the newly generated new_amd64InstrDecode.h.

set -x

# Create the program createOpcodes
gcc createOpcodes.cpp -o createOpcodes

# Execute the program to create opcodes.cpp
./createOpcodes > opcodes.cpp

# Compile opcodes.cpp to opcodes
gcc -g opcodes.cpp -o opcodes

# Disassemble opcodes
gdb opcodes -batch -ex "set disassembly-flavor intel" -ex "disass /r opcodes" > opcodes.intel

# Parse disassembly and generate code.
# Build as a separate step so it will display build errors, if any.
../../../../../../dotnet.sh build
cat opcodes.intel | ../../../../../../dotnet.sh run > new_amd64InstrDecode.h
