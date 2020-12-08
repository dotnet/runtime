pe-section-headers {
	#Simple assembly has 2 sections since it doesn't have any resources
	assembly simple-assembly.exe

	#truncate the section themselves
	invalid offset section-table + 0 truncate
	invalid offset section-table + 39 truncate
	invalid offset section-table + 41 truncate
	invalid offset section-table + 78 truncate


	#A section RVA maps [VirtAddress,VirtAddress + RawSize] starting at PointerToRawData
	#truncate before PointerToRawData
	invalid offset read.uint ( section-table + 20 ) - 1 truncate
	invalid offset read.uint ( section-table + 60 ) - 1 truncate

	#make it point to after EOF
	invalid offset section-table + 20 set-uint 90000
	invalid offset section-table + 60 set-uint 90000

	#make VirtualSize be huge
	valid offset section-table + 8 set-uint read.uint ( section-table + 8 )
	#invalid offset section-table + 8 + 0 set-uint 90000

	#VirtualSize = file size + PointerToRawData + 32
	invalid offset section-table + 16 set-uint file-size - read.uint ( section-table + 20 ) + 32
	invalid offset section-table + 56 set-uint file-size - read.uint ( section-table + 60 ) + 32

	invalid offset section-table + 60 set-uint 90000

	#FIXME add section relocation tests
}

pe-section-header-flags {
	#Simple assembly has 2 sections since it doesn't have any resources
	assembly simple-assembly.exe

	#first section is always text
	valid offset section-table + 36 set-uint 0x60000020

	valid offset section-table + 76 set-uint 0x42000040

	invalid offset section-table + 36 set-uint 0

	invalid offset section-table + 36 set-uint 0xFFFFFFFF
}
