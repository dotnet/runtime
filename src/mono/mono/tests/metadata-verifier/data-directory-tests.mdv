pe-data-directories-export-table {
	#Simple assembly has 2 sections since it doesn't have any resources
	assembly simple-assembly.exe

	#zero is fine
	valid offset pe-optional-header + 96 set-uint 0
	valid offset pe-optional-header + 100 set-uint 0

	#RVA must be zero
	invalid offset pe-optional-header + 96 set-uint 0x2000 , offset pe-optional-header + 100 set-uint 10
}


pe-data-directories-import-table {
	#Simple assembly has 2 sections since it doesn't have any resources
	assembly simple-assembly.exe

	#The IT is 40 bytes long
	invalid offset pe-optional-header + 108 set-uint 0
	invalid offset pe-optional-header + 108 set-uint 8
	invalid offset pe-optional-header + 108 set-uint 0x10
	invalid offset pe-optional-header + 108 set-uint 0x20
	invalid offset pe-optional-header + 108 set-uint 0x27
	valid offset pe-optional-header + 108 set-uint 0x28

	#RVA + size must bounds check against the size of the entry.
	invalid offset pe-optional-header + 108 set-uint 0x900
}

pe-data-directories-bad-tables {
	#Simple assembly has 2 sections since it doesn't have any resources
	assembly simple-assembly.exe

	#export
	invalid offset pe-optional-header + 96 set-uint 0x2000

	#exception
	invalid offset pe-optional-header + 120 set-uint 0x2000

	#certificate  some assemblies have it.
	#invalid offset pe-optional-header + 128 set-uint 0x2000

	#debug MS uses it for putting debug info in the assembly
	#invalid offset pe-optional-header + 144 set-uint 0x2000

	#copyright
	invalid offset pe-optional-header + 152 set-uint 0x2000

	#global ptr
	invalid offset pe-optional-header + 160 set-uint 0x2000

	#tls table
	invalid offset pe-optional-header + 168 set-uint 0x2000

	#load config
	invalid offset pe-optional-header + 176 set-uint 0x2000

	#bound import
	invalid offset pe-optional-header + 184 set-uint 0x2000

	#delay import
	invalid offset pe-optional-header + 200 set-uint 0x2000

	#reserved import
	invalid offset pe-optional-header + 216 set-uint 0x2000
}


pe-import-table {
	#Simple assembly has 2 sections since it doesn't have any resources
	assembly simple-assembly.exe

	#Invalid rva for the import lookup table
	invalid offset translate.rva.ind ( pe-optional-header + 104 ) + 0 set-uint 0x88888
	#Invalid rva for the name
	invalid offset translate.rva.ind ( pe-optional-header + 104 ) + 12 set-uint 0x88888
	#Invalid rva for the import address table
	invalid offset translate.rva.ind ( pe-optional-header + 104 ) + 16 set-uint 0x88888
}

pe-import-table-ILT {
	#Simple assembly has 2 sections since it doesn't have any resources
	assembly simple-assembly.exe

	#Bad RVA for the Hint/Name table
	invalid offset translate.rva.ind ( translate.rva.ind ( pe-optional-header + 104 ) + 0 ) set-uint 0x88888

	#Bad content in the Hint/Name table
	invalid offset translate.rva.ind ( translate.rva.ind ( translate.rva.ind ( pe-optional-header + 104 ) ) ) + 2 set-uint 0x454c
}

pe-import-table-IAT {
	#Simple assembly has 2 sections since it doesn't have any resources
	assembly simple-assembly.exe

	#Bad RVA for the Hint/Name table
	#LAMEIMPL - MS ignores this
	invalid offset translate.rva.ind ( translate.rva.ind ( pe-optional-header + 104 ) + 16 ) set-uint 0x88888

	#Bad content in the Hint/Name table
	invalid offset translate.rva.ind ( translate.rva.ind ( translate.rva.ind ( pe-optional-header + 104 ) + 16 ) ) + 2 set-uint 0x454c
}

pe-import-table-name {
	#Simple assembly has 2 sections since it doesn't have any resources
	assembly simple-assembly.exe

	#Invalid symbol name
	invalid offset translate.rva.ind ( translate.rva.ind ( pe-optional-header + 104 ) + 12 ) set-uint 0x454c
}

pe-IAT {
	#Simple assembly has 2 sections since it doesn't have any resources
	assembly simple-assembly.exe

	#Bad RVA to the Hint/Name table
	#LAMEIMPL - MS ignores this
	invalid offset translate.rva.ind ( pe-optional-header + 192 ) set-uint 0x88880

	#Bad content in the Hint/Name table
	invalid offset translate.rva.ind ( translate.rva.ind ( pe-optional-header + 192 ) ) + 2 set-uint 0x454c

}
