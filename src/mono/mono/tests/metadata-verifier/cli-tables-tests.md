tables-header {
	assembly simple-assembly.exe

	#table schema major version
	valid offset cli-metadata + read.uint ( stream-header ( 0 ) ) + 4  set-byte 2
	valid offset tables-header + 4  set-byte 2

	#major/minor versions	
	invalid offset tables-header + 4 set-byte 22
	invalid offset tables-header + 5 set-byte 1

	#table schema size
	invalid offset stream-header ( 0 ) + 4 set-uint 23

	#heap sizes
	invalid offset tables-header + 6 set-byte 0x8
	invalid offset tables-header + 6 set-byte 0x10
	invalid offset tables-header + 6 set-byte 0xF

	#present tables
	#ECMA-335 defines 39 tables, the empty slows are the following:
	# MS Extensions: 0x3 0x5 0x7 0x13 0x16
	# Unused: 0x1E 0x1F 0x2D-0x3F
	# We don't care about the MS extensions.

	invalid offset tables-header + 8 set-bit 0x3
	invalid offset tables-header + 8 set-bit 0x5
	invalid offset tables-header + 8 set-bit 0x7
	invalid offset tables-header + 8 set-bit 0x13
	invalid offset tables-header + 8 set-bit 0x16

	invalid offset tables-header + 8 set-bit 0x1E
	invalid offset tables-header + 8 set-bit 0x1F

	invalid offset tables-header + 8 set-bit 0x2D
	invalid offset tables-header + 8 set-bit 0x2F
	invalid offset tables-header + 8 set-bit 0x30
	invalid offset tables-header + 8 set-bit 0x35
	invalid offset tables-header + 8 set-bit 0x38
	invalid offset tables-header + 8 set-bit 0x3F

	#simple-assembly.exe feature 6 tables (modules, typeref, typedef, method, assembly and assemblyref)
	#This means that there must be 24 + 6 *4 bytes to hold the schemata + rows -> 48 bytes

	#table schema size
	invalid offset stream-header ( 0 ) + 4 set-uint 24
	invalid offset stream-header ( 0 ) + 4 set-uint 33
	invalid offset stream-header ( 0 ) + 4 set-uint 39
	invalid offset stream-header ( 0 ) + 4 set-uint 44
	invalid offset stream-header ( 0 ) + 4 set-uint 47

	#total size of the tables
	invalid offset stream-header ( 0 ) + 4 set-uint 60
	invalid offset stream-header ( 0 ) + 4 set-uint 93
}

module-table {
	assembly simple-assembly.exe

	#generation
	valid offset table-row ( 0 0 ) set-ushort 0
	invalid offset table-row ( 0 0 ) set-ushort 9999

	#rows
	valid offset tables-header + 24 set-uint 1
	invalid offset tables-header + 24 set-uint 0
	invalid offset tables-header + 24 set-uint 2 , offset tables-header + 32 set-uint 1
	
	#name
	#invalid string
	invalid offset table-row ( 0 0 ) + 2 set-ushort 0x8888
	#point to an empty string
	invalid offset table-row ( 0 0 ) + 2 set-ushort 0

	#mvid
	invalid offset table-row ( 0 0 ) + 4 set-ushort 0x8888

	#encId
	invalid offset table-row ( 0 0 ) + 6 set-ushort 0x8888

	#encBaseId
	invalid offset table-row ( 0 0 ) + 8 set-ushort 0x8888
}


typedef-table {
	assembly simple-assembly.exe

	#Resolution Scope

	#all table indexes are valid
	#Invalid module
	invalid offset table-row ( 1 0 ) set-ushort 0x8000

	#Invalid moduleref
	invalid offset table-row ( 1 0 ) set-ushort 0x8001

	#Invalid assemblyref
	invalid offset table-row ( 1 0 ) set-ushort 0x8002

	#Invalid typeref
	invalid offset table-row ( 1 0 ) set-ushort 0x8003

}
