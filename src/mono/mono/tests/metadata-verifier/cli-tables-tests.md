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
	#LAMEIMPL MS ignore garbage on the upper bits.
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
	#FALESPEC this field is ignored
	valid offset table-row ( 0 0 ) set-ushort 9999

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


typeref-table {
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

	#Empty TypeName
	invalid offset table-row ( 1 0 ) + 2 set-ushort 0

	#Invalid TypeName
	invalid offset table-row ( 1 0 ) + 2 set-ushort 0x8080

	#Empty TypeNamespace
	invalid offset table-row ( 1 0 ) + 4 set-ushort 0x8080
}

typedef-table {
	assembly simple-assembly.exe

	#rows
	valid offset tables-header + 32 set-uint 2
	invalid offset tables-header + 32 set-uint 0

	#This part of the test suite only verifies structural properties, not table relationships	

	#Flags invalid bits: 9,11,14,15,19,21,24-31
	invalid offset table-row ( 2 1 ) set-bit 9
	invalid offset table-row ( 2 1 ) set-bit 11
	invalid offset table-row ( 2 1 ) set-bit 14
	invalid offset table-row ( 2 1 ) set-bit 15
	invalid offset table-row ( 2 1 ) set-bit 19
	invalid offset table-row ( 2 1 ) set-bit 21
	invalid offset table-row ( 2 1 ) set-bit 24
	invalid offset table-row ( 2 1 ) set-bit 25
	invalid offset table-row ( 2 1 ) set-bit 26
	invalid offset table-row ( 2 1 ) set-bit 27
	invalid offset table-row ( 2 1 ) set-bit 28
	invalid offset table-row ( 2 1 ) set-bit 29
	invalid offset table-row ( 2 1 ) set-bit 30
	invalid offset table-row ( 2 1 ) set-bit 31

	#invalid class layout
	invalid offset table-row ( 2 1 ) or-uint 0x18

	#invalid StringFormatMask - mono doesn't support CustomFormatMask
	invalid offset table-row ( 2 1 ) or-uint 0x30000

	#CustomStringFormatMask must be zero
	invalid offset table-row ( 2 1 ) or-uint 0xC00000

	#We ignore all validation requited by HasSecurity

	#TypeName
	invalid offset table-row ( 2 1 ) + 4 set-ushort 0
	invalid offset table-row ( 2 1 ) + 4 set-ushort 0x9999

	#TypeNameSpace
	invalid offset table-row ( 2 1 ) + 6 set-ushort 0x9999

	#Extends is a TypeDefOrRef coded token (uses 2 bits to code typedef, typeref and typespec)
	#invalid coded table
	invalid offset table-row ( 2 1 ) + 8 set-ushort 0x33003

	#null token (except system.obj, <module> and interfaces)
	invalid offset table-row ( 2 1 ) + 8 set-ushort 0x0
	invalid offset table-row ( 2 1 ) + 8 set-ushort 0x01
	invalid offset table-row ( 2 1 ) + 8 set-ushort 0x02

	#make type 1 an inteface but let it remain extending something
	invalid offset table-row ( 2 1 ) or-uint 0x20
	#interface must extend nothing
	valid offset table-row ( 2 1 ) or-uint 0xA0 , offset table-row ( 2 1 ) + 8 set-ushort 0x0

	#interface must be abstract
	invalid offset table-row ( 2 1 ) or-uint 0x20 , offset table-row ( 2 1 ) + 8 set-ushort 0x0

	#TODO add a test for sys.obj (we should test for mscorlib as well)

	valid offset table-row ( 2 0 ) + 8 set-ushort 0
	#make <module> extend the first typeref entry, which usually is sys.obj
	#LAMEIMPL MS ignores if <module> extend something.
	invalid offset table-row ( 2 0 ) + 8 set-ushort 0x5
}

typedef-table-field-list {
	assembly assembly-with-complex-type.exe

	valid offset table-row ( 2 1 ) + 10 set-ushort 1

	#bad field list 
	invalid offset table-row ( 2 1 ) + 10 set-ushort 999

	#this type is bigger than the next
	invalid offset table-row ( 2 1 ) + 10 set-ushort 4

	#can't be zero
	invalid offset table-row ( 2 0 ) + 10 set-ushort 0

}

typedef-table-method-list {
	assembly assembly-with-complex-type.exe

	valid offset table-row ( 2 1 ) + 12 set-ushort 1

	#bad field list 
	invalid offset table-row ( 2 1 ) + 12 set-ushort 999

	#this type is bigger than the next
	invalid offset table-row ( 2 1 ) + 12 set-ushort 5

	#can't be zero
	invalid offset table-row ( 2 0 ) + 12 set-ushort 0

}

field-table {
	assembly assembly-with-complex-type.exe

	#This tests only verify basic structural properties, they don't verify relationship between tables
	#flags

	#invalid bits 11, 14 (4)
	invalid offset table-row ( 4 1 ) set-bit 3
	invalid offset table-row ( 4 1 ) set-bit 11
	invalid offset table-row ( 4 1 ) set-bit 14

	#invalid visibility (5)
	invalid offset table-row ( 4 0 ) or-ushort 0x7

	#field with initonly and literal (6)
	invalid offset table-row ( 4 0 ) or-ushort 0x60

	#field with literal must be static (7)
	valid offset table-row ( 4 4 ) or-ushort 0x50
	invalid offset table-row ( 4 0 ) or-ushort 0x40

	#field with rt special name must have special name (8)
	#special name
	valid offset table-row ( 4 0 ) or-ushort 0x0200
	#special name + rt special name

	#LAMEIMPL MS requires that fields marked rtspecialname to be named value__ even if they are not enums
	valid offset table-row ( 4 0 ) or-ushort 0x0600
	#only rt special name
	invalid offset table-row ( 4 0 ) or-ushort 0x0400

	#no row in the field marshal table for field 0 (9)
	invalid offset table-row ( 4 0 ) or-ushort 0x1000

	#no row in the constant table for field 0 (10)
	invalid offset table-row ( 4 0 ) or-ushort 0x8000

	#no row in the field rva table for field 0 (11)
	invalid offset table-row ( 4 0 ) or-ushort 0x0100

	#name can't be empty or invalid (12)
	invalid offset table-row ( 4 1 ) + 2 set-ushort 0
	invalid offset table-row ( 4 1 ) + 2 set-ushort 0x9999

	#invalid signature
	invalid offset table-row ( 4 1 ) + 4 set-ushort 0x4666

	#if it's a global variable, it must be static and (public|compiler controler|private) (16)
	#static + compiler controled
	valid offset table-row ( 2 1 ) + 10 set-ushort 2 , offset table-row ( 4 0 ) set-ushort 0x10 
	#static + private
	valid offset table-row ( 2 1 ) + 10 set-ushort 2 , offset table-row ( 4 0 ) set-ushort 0x11
	#static + public
	valid offset table-row ( 2 1 ) + 10 set-ushort 2 , offset table-row ( 4 0 ) set-ushort 0x16 
	#static + bad visibility
	#LAMEIMPL MS doesn't verify visibility
	invalid offset table-row ( 2 1 ) + 10 set-ushort 2 , offset table-row ( 4 0 ) set-ushort 0x12
	invalid offset table-row ( 2 1 ) + 10 set-ushort 2 , offset table-row ( 4 0 ) set-ushort 0x13
	invalid offset table-row ( 2 1 ) + 10 set-ushort 2 , offset table-row ( 4 0 ) set-ushort 0x14
	invalid offset table-row ( 2 1 ) + 10 set-ushort 2 , offset table-row ( 4 0 ) set-ushort 0x15

	#public and not static
	invalid offset table-row ( 2 1 ) + 10 set-ushort 2 , offset table-row ( 4 0 ) set-ushort 0x06 

	#field is constant but has no row in the contant table
	#LAMESPEC this check is missing from the spec
	invalid offset table-row ( 4 0 ) or-ushort 0x50

	#TODO test enum condition and signature content

}

