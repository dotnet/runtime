

#the first bytes of an image must be 4d 5a
msdos-signature {
	assembly simple-assembly.exe
	valid   offset 0 set-byte 4d
	invalid offset 0 set-byte 4e

	valid   offset 1 set-byte 5a
	invalid offset 1 set-byte 0

	#the spec says it should be 0x90 but no modern COFF loader cares about this.
	valid   offset 2 set-byte 00
}

#the offset to the pe-image
msdos-lfanew {
	assembly simple-assembly.exe

	invalid offset 3c set-uint 0xffffffff
}