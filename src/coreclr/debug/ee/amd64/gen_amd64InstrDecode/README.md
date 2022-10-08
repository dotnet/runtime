# Generating `amd64InstrDecode.h`

## TL;DR - What do I do? Process

The following process was executed on an amd64 Linux host in this directory.

```bash
# Create the program createOpcodes
gcc createOpcodes.cpp -o createOpcodes

# Execute the program to create opcodes.cpp
./createOpcodes > opcodes.cpp

# Compile opcodes.cpp to opcodes
gcc -g opcodes.cpp -o opcodes

# Disassemble opcodes
gdb opcodes -batch -ex "set disassembly-flavor intel" -ex "disass /r opcodes" > opcodes.intel

# Parse disassembly and generate code
cat opcodes.intel | dotnet run > ../amd64InstrDecode.h
```

## Technical design

`amd64InstrDecode.h`'s primary purpose is to provide a reliable
and accurately mechanism to implement
`Amd64 NativeWalker::DecodeInstructionForPatchSkip(..)`.

This function needs to be able to decode an arbitrary `amd64`
instruction.  The decoder currently must be able to identify:

- Whether the instruction includes an instruction pointer relative memory access
- The location of the memory displacement within the instruction
- The instruction length in bytes
- The size of the memory operation in bytes

To get this right is complicated, because the `amd64` instruction set is
complicated.

A high level view of the `amd64` instruction set can be seen by looking at
`AMD64 Architecture Programmer’s
 Manual Volume 3:
 General-Purpose and System Instructions`
 `Section 1.1 Instruction Encoding Overview`
 `Figure 1-1.  Instruction Encoding Syntax`

The general behavior of each instruction can be modified by many of the
bytes in the 1-15 byte instruction.

This set of files generates a metadata table by extracting the data from
sample instruction disassembly.

The process entails
- Generating a necessary set of instructions
- Generating parsable disassembly for the instructions
- Parsing the disassembly

### Generating a necessary set of instructions

#### The necessary set

- All instruction forms which use instruction pointer relative memory accesses.
- All combinations of modifier bits which affect the instruction form
    - presence and/or size of the memory access
    - size or presence of immediates

So with modrm.mod = 0, modrm.rm = 0x5 (instruction pointer relative memory access)
we need all combinations of:
- `opcodemap`
- `opcode`
- `modrm.reg`
- `pp`, `W`, `L`
- Some combinations of `vvvv`
- Optional prefixes: `repe`, `repne`, `opSize`

#### Padding

We will iterate through all the necessary set. Many of these combinations
will lead to invalid/undefined encodings.  This will cause the disassembler
to give up and mark the disassemble as bad.

The disassemble will then resume trying to disassemble at the next boundary.

To make sure the disassembler attempts to disassemble every instruction,
we need to make sure the preceding instruction is always valid and terminates
at our desired instruction boundary.

Through examination of the `Primary` opcode map, it is observed that
0x50-0x5f are all 1 byte instructions.  These become convenient padding.

After each necessary instruction we insert enough padding bytes to fill
the maximum instruction length and leave at least one additional one byte
instruction.

#### Fixed suffix

Using a fixed suffix makes disassembly parsing simpler.

After the modrm byte, the generated instructions always include a
`postamble`,

```C++
const char* postamble = "0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,\n";
```

This meets the padding consistency needs.

#### Ordering

As a convenience to the parser the encoded instructions are logically
ordered.  The ordering is generally, but can vary slightly depending on
the needs of the particular opcode map:

- map
- opcode
- pp & some prefixes
- modrm.reg
- W, L, vvvv

This is to keep related instruction grouped together.

#### Encoding the instructions

The simplest way to get these instructions into an object file for
disassembly is to place them into a C++ BYTE array.

The file `createOpcodes.cpp` is the source for a program which will
generate `opcodes.cpp`

```bash
# Create the program createOpcodes
gcc createOpcodes.cpp -o createOpcodes

# Execute the program to create opcodes.cpp
./createOpcodes > opcodes.cpp
```

`opcodes.cpp` will now be a C++ source file with `uint8_t opcodes[]`
initialized with our set of necessary instructions and padding.

We need to compile this to an executable to prepare for disassembly.

```bash
# Compile opcodes.cpp to opcodes
gcc -g opcodes.cpp -o opcodes
```

### Generating parsable disassembly

In investigating the various disassembly formats, the `intel`
disassembly format is superior to the `att` format. This is because the
`intel` format clearly marks the instruction relative accesses and
their sizes. For instance:

- "BYTE PTR [rip+0x53525150]"
- "WORD PTR [rip+0x53525150]"
- "DWORD PTR [rip+0x53525150]"
- "QWORD PTR [rip+0x53525150]"
- "OWORD PTR [rip+0x53525150]"
- "XMMWORD PTR [rip+0x53525150]"
- "YMMWORD PTR [rip+0x53525150]"
- "FWORD PTR [rip+0x53525150]"
- "TBYTE PTR [rip+0x53525150]"

Also it is important to have all the raw bytes in the disassembly.  This
allows accurately determining the instruction length.

It also helps identifying which instructions are from our needed set.

I happened to have used `gdb` as a disassembler.

```bash
# Disassemble opcodes
gdb opcodes -batch -ex "set disassembly-flavor intel" -ex "disass /r opcodes" > opcodes.intel
```

#### Alternative disassemblers

It seems `objdump` could provide similar results. Untested, the parser may need to
be modified for subtle differences.
```bash
objdump -D -M intel -b --insn-width=15 -j .data opcodes
```

The lldb parser aborts parsing when it observes bad instruction. It
might be usable with additional python scripts.

Windows disassembler may also work.  Not attempted.

### Parsing the disassembly
```bash
# Parse disassembly and generate code
cat opcodes.intel | dotnet run > ../amd64InstrDecode.h
```
#### Finding relevant disassembly lines

We are not interested in all lines in the disassembly. The disassembler
stray comments, recovery and our padding introduce lines we need to ignore.

We filter out and ignore non-disassembly lines using a `Regex` for a
disassembly line.

We expect the generated instruction samples to be in a group.  The first
instruction in the group is the only one we are interested in.  This is
the one we are interested in.

The group is terminated by a pair of instructions.  The first terminal
instruction must have `0x58` as the last byte in it encoding. The final
terminal instruction must be a `0x59\tpop`.

We continue parsing the first line of each group.

#### Ignoring bad encodings

Many encodings are not valid.  For `gdb`, these instructions are marked
`(bad)`.  We filter and ignore these.

#### Parsing the disassambly for each instruction sample

For each sample, we need to calculate the important properties:
- mnemonic
- raw encoding
- disassembly (for debug)
- Encoding
    - map
    - opcode position
- Encoding Flags
    - pp, W, L, prefix and encoding flags
- `SuffixFlags`
    - presence of instruction relative accesses
        - size of operation
        - position in the list of operands
        - number of immediate bytes

##### Supplementing the disassembler

In a few cases it was observed the disassembly of some memory operations
did not include a size. These were manually researched.  For the ones with
reasonable sizes, these were added to a table to manually override these
unknown sizes.

#### `opCodeExt`

To facilitate identifying sets of instructions, the creates an `opCodeExt`.

For the `Primary` map this is simply the encoded opcode from the instruction
shifted left by 4 bits.

For the 3D Now `NOW3D` map this is simply the encoded immediate from the
instruction shifted left by 4 bits.

For the `Secondary` `F38`, and `F39` maps this is the encoded opcode from
the instruction shifted left by 4 bits orred with a synthetic `pp`. The
synthetic `pp` is constructed to match the rules of
`Table 1-22. VEX/XOP.pp Encoding` from the
`AMD64 Architecture Programmer’s
 Manual Volume 3:
 General-Purpose and System Instructions`.  For the case where the opSize
 0x66 prefix is present with a `rep*` prefix, the `rep*` prefix is used
 to encode `pp`.

For the `VEX*` and `XOP*` maps this is the encoded opcode from
the instruction shifted left by 4 bits orred with `pp`.

#### Identifying sets of instructions

For most instructions, the opCodeExt will uniquely identify the instruction.

For many instructions, `modrm.reg` is used to uniquely identify the instruction.
These instruction typically change mnemonic and behavior as `modrm.reg`
changes. These become problematic, when the form of these instructions vary.

For a few other instructions the `L`, `W`, `vvvv` value may the instruction
change behavior. Usually these do not change mnemonic.

The set of instructions is therefore usually grouped by the opcode map and
`opCodeExt` generated above.  For these a change in `opCodeExt` or `map`
will start a new group.

For select problematic groups of `modrm.reg` sensitive instructions, a
change in modrm.reg will start a new group.

#### For each set of instructions

- Calculate the `intersection` and `union` of the `SuffixFlags` for the set.
- The flags in the `intersection` are common to all instructions in the set.
- The ones in the `union`, but not in the `intersection` vary within the
set based on the encoding flags.  These are the `sometimesFlags`
- Determine the rules for the `sometimesFlags`.  For each combination of
`sometimesFlags`, check each rule by calling `TestHypothesis`.  This
determines if the rule corresponds to the set of observations.

Encode the rule as a string.

Add the rule to the set of all observed rules.
Add the set's rule with comment to a dictionary.

#### Generating the C++ code

At this point generating the code is rather simple.

Iterate through the set of rules to create an enumeration of `InstrForm`.

For each map iterate through the dictionary, filling missing instructions
with an appropriate pattern for undefined instructions.

##### Encoding

The design uses a simple fully populated direct look up table to
provide a nice simple means of looking up. This direct map approach is
expected to consume ~10K bytes.

Other approaches like a sparse list may reduce total memory usage. The
added complexity did not seem worth it.


## Benefits

This approach is intended to reduce the human error introduced by
manually parsing and encoding the various instruction forms from their
respective descriptions.

## Limitations

The approach of using a single object file as the source of disassembly
samples, is restricted to a max compilation/link unit size. Early drafts
were generating more instructions, and couldn't be compiled.

However, there is no restriction that all the samples must come from
single executable.  These could easily be separated by opcode map...

## Risks

### New instruction sets

This design is for existing instruction sets.  New instruction sets will
require more work.

Further this methodology uses the disassembler to generate the tables.
Until a reasonably featured disassembler is created, the new instruction
set can not be supported by this methodology.

The previous methodology of manually encoding these new instruction set
would still be possible....

### Disassembler errors

This design presumes the disassembler is correct. The specific version
of the disassembler may have disassembly bugs.  Using newer disassemblers
would mitigate this to some extent.

### Bugs
- Inadequate samples.  Are there other bits which modify instruction
behavior which we missed?
- Parser/Table generator implementation bugs. Does the parser do what it
was intended to do?

## Reasons to regenerate the file

### Disassembler error discovered

Add a patch to the parser to workaround the bug and regenerate the table

### Newer disassembler available

Regenerate and compare.

### New debugger feature requires more metadata

Add new feature code, regenerate
