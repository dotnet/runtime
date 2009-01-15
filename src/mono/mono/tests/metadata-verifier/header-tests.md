

#the first bytes of an image must be 4d 5a
msdos-signature {
	assembly simple-assembly.exe
	valid   offset 0 set-byte 0x4d
	invalid offset 0 set-byte 0x4e

	valid   offset 1 set-byte 0x5a
	invalid offset 1 set-byte 0

	#the spec says it should be 0x90 but no modern COFF loader cares about this.
	valid   offset 2 set-byte 0
}

#the offset to the pe-image
msdos-lfanew {
	assembly simple-assembly.exe

	#truncate the file before and after
	invalid offset 0x3c truncate
	invalid offset 0x3d truncate
	invalid offset 0x3e truncate
	invalid offset 0x3f truncate

	#not enough space for the PE water mark
	invalid offset 0x3c set-uint 0xffffffff 
	invalid offset 0x3c set-uint file-size - 1 
	invalid offset 0x3c set-uint file-size - 2
}

pe-signature {
	assembly simple-assembly.exe

	valid offset pe-signature + 0 set-byte 'P'	
	valid offset pe-signature + 1 set-byte 'E'	
	valid offset pe-signature + 2 set-byte 0
	valid offset pe-signature + 3 set-byte 0

	invalid offset pe-signature + 0 set-byte 'M'	
	invalid offset pe-signature + 1 set-byte 'K'	
	invalid offset pe-signature + 2 set-byte 1
	invalid offset pe-signature + 3 set-byte 2

	invalid offset pe-signature + 1 truncate
	invalid offset pe-signature + 2 truncate
}

pe-header {
	assembly simple-assembly.exe

	#size related checks
	invalid offset pe-header + 0 truncate
	invalid offset pe-header + 1 truncate
	invalid offset pe-header + 18 truncate
	invalid offset pe-header + 19 truncate

	#machine
	valid offset pe-header set-ushort 0x14c
	invalid offset pe-header set-ushort 0x14d
	invalid offset pe-header set-ushort 0x24c

	#symbol table value doesn't matter
	valid offset pe-header + 8 set-uint 0
	valid offset pe-header + 8 set-uint 99
	valid offset pe-header + 8 set-uint 0xffffffff

	#number of symbols value doesn't matter
	valid offset pe-header + 12 set-uint 0
	valid offset pe-header + 12 set-uint 99
	valid offset pe-header + 12 set-uint 0xffffffff

	#characteristics - it's value is not important
	valid offset pe-header + 18 set-ushort 0
	valid offset pe-header + 18 set-ushort 0x4000

	#FIXME 0x2000 is used for signaling it's a dll and peverify complains about the entrypoint signature. WHAT?
	#invalid offset pe-header + 18 set-ushort 0x2000
}

pe-optional-header {
	assembly simple-assembly.exe

	#this header is optional only in the names
	valid offset pe-header + 16 set-ushort 224
	invalid offset pe-header + 16 set-ushort 0
	invalid offset pe-header + 16 set-ushort 223

	invalid offset pe-header + 18 truncate
	invalid offset pe-header + 239 truncate

	#FIXME add tests for PE32+
}
