# Contract ExecutionManager

This contract is for mapping a PC address to information about the
managed method corresponding to that address.


## APIs of contract

**TODO**

## Version 1

**TODO** Methods

### NibbleMap

Version 1 of this contract depends on a "nibble map" data structure
that allows mapping of a code address in a contiguous subsection of
the address space to the pointer to the start of that a code sequence.
It takes advantage of the fact that the code starts are aligned and
are spaced apart to represent their addresses as a 4-bit nibble value.
