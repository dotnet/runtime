

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

	#truncate the file before and after
	invalid offset 3c truncate
	invalid offset 3d truncate
	invalid offset 3e truncate
	invalid offset 3f truncate

	#not enough space for the PE water mark
	invalid offset 3c set-uint 0xffffffff 
	invalid offset 3c set-uint file-size - 1 
	invalid offset 3c set-uint file-size - 2
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
	invalid offset pe-header + 018 truncate
	invalid offset pe-header + 019 truncate

	#machine
	valid offset pe-header set-ushort 14c
	invalid offset pe-header set-ushort 14d
	invalid offset pe-header set-ushort 24c

}
