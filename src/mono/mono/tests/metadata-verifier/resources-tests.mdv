
resources-master-directory {
	#This assembly has a regular resource and a linked resource
	#LAMEIMPL MS doesn't validate those.
	assembly assembly-with-resource.exe

	#the resource directory table is 16 bytes long
	invalid offset pe-optional-header + 116 set-uint 0
	invalid offset pe-optional-header + 116 set-uint 15

	#the resources directory table has too many entries that it overflows the directory size
	invalid offset translate.rva.ind ( pe-optional-header + 112	) + 12 set-ushort 0x9999

	#the resources directory table has too many entries that it overflows the directory size
	invalid offset translate.rva.ind ( pe-optional-header + 112	) + 14 set-ushort 0x9999

	#I won't check anything more than that for now as this is only used by out asp.net stack.
}
