cli-header-basic {
	assembly simple-assembly.exe

	#the section dir must point to a valid rva
	invalid offset pe-optional-header + 208 set-uint 0x88888

	#the cli header must be there
	invalid offset pe-optional-header + 208 set-uint 0

	#the cli header size must be == 72
	invalid offset pe-optional-header + 212 set-uint 71

	#the cli header size must be == 72 (again, but now on the header itself)
	invalid offset cli-header set-uint 71

	#Framework version is irrelevant

	#Metadata RVA and size
	#no metadata
	invalid offset cli-header + 8 set-uint 0

	#invalid
	invalid offset cli-header + 8 set-uint 0x777777

	#empty metadata
	invalid offset cli-header + 12 set-uint 0

	#not bounds checking
	invalid offset cli-header + 12 set-uint 0x12345678

	#Flags valid mask: 0x0001000B
	invalid offset cli-header + 16 set-uint 0x0011000B

	#TODO verify entry point token

	#Resources
	invalid offset cli-header + 24 set-uint 0x777777
	invalid offset cli-header + 24 set-uint 0x2000 , offset cli-header + 28 set-uint 0x999999

	#Strong Name
	invalid offset cli-header + 32 set-uint 0x777777
	invalid offset cli-header + 32 set-uint 0x2000 , offset cli-header + 36 set-uint 0x999999

	#Code Manager Table
	invalid offset cli-header + 40 set-uint 0x777777
	invalid offset cli-header + 40 set-uint 0x2000 , offset cli-header + 44 set-uint 0x999999

	#VTable fixups
	invalid offset cli-header + 48 set-uint 0x777777
	invalid offset cli-header + 48 set-uint 0x2000 , offset cli-header + 52 set-uint 0x999999

	#Export Address Table
	invalid offset cli-header + 56 set-uint 0x777777
	invalid offset cli-header + 56 set-uint 0x2000 , offset cli-header + 60 set-uint 0x999999

	#Managed native header
	invalid offset cli-header + 64 set-uint 0x777777
	invalid offset cli-header + 64 set-uint 0x2000 , offset cli-header + 68 set-uint 0x999999
}
