cli-header-basic {
	assembly simple-assembly.exe

	#the section dir must point to a valid rva
	invalid offset pe-optional-header + 208 set-uint 0x88888

	#the cli header must be there
	invalid offset pe-optional-header + 208 set-uint 0

	#the cli header size must be == 72
	invalid offset pe-optional-header + 212 set-uint 71

}