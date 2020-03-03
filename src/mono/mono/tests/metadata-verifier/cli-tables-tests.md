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

	#Flags invalid bits: 6,9,14,15,19,21,24-31
	invalid offset table-row ( 2 1 ) set-bit 6
	invalid offset table-row ( 2 1 ) set-bit 9
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
	#invalid offset table-row ( 2 0 ) + 8 set-ushort 0x5
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

	#TODO enum and signature content
}

methoddef-table {
	assembly assembly-with-methods.exe

	#bad implflags (3)
	#unused bits 4,5,8,9,10,11,15
	#LAMEIMPL MS doesn't check invalid bits  8,9,10,11,13,14,15
	invalid offset table-row ( 6 0 ) + 4 set-bit 8
	invalid offset table-row ( 6 0 ) + 4 set-bit 9
	invalid offset table-row ( 6 0 ) + 4 set-bit 10
	invalid offset table-row ( 6 0 ) + 4 set-bit 11
	invalid offset table-row ( 6 0 ) + 4 set-bit 13
	invalid offset table-row ( 6 0 ) + 4 set-bit 14
	invalid offset table-row ( 6 0 ) + 4 set-bit 15

	#bad flags (4)
	#no unused bits
	
	#invalid .ctor with generic params and specialname (6)
	#method 0 is a .ctor, method 1 is generic
	invalid offset table-row ( 6 1 ) + 6 or-ushort 0x1800 , offset table-row ( 6 1 ) + 8 set-ushort read.ushort ( table-row ( 6 0 ) + 8 )

	#visibility 0x7 is invalid (6)
	invalid offset table-row ( 6 0 ) + 6 or-ushort 0x7

	#Invalid combination of flags (7)
	#static + final
	invalid offset table-row ( 6 2 ) + 6 set-ushort 0x0030
	#static + virtual
	invalid offset table-row ( 6 2 ) + 6 set-ushort 0x0050
	#static + newslot
	invalid offset table-row ( 6 2 ) + 6 set-ushort 0x0110
	#final + abstract
	invalid offset table-row ( 6 2 ) + 6 set-ushort 0x0420
	#abstract + pinvokeimpl
	invalid offset table-row ( 6 2 ) + 6 set-ushort 0x2400

	#LAMEIMPL MS doesn't care about this
	#compilercontrolled | specialname
	#invalid offset table-row ( 6 2 ) + 6 set-ushort 0x0800

	#LAMEIMPL MS doesn't care about this
	#compilercontrolled | rtspecialname
	invalid offset table-row ( 6 2 ) + 6 set-ushort 0x1800

	#Abstract method must be virtual (8)
	invalid offset table-row ( 6 2 ) + 6 set-ushort 0x0400

	#A rtspecialnamemethod must be special name (9)
	invalid offset table-row ( 6 2 ) + 6 set-ushort 0x1000

	#XXX we don't care about CAS security (10, 11,12,13)

	#name shall be non empty (14)
	invalid offset table-row ( 6 2 ) + 8 set-ushort 0
	invalid offset table-row ( 6 2 ) + 8 set-ushort 0x9999

	#Interface cannot have .ctors (15)
	#method 3 belongs to an inteface
	invalid offset table-row ( 6 3 ) + 8 set-ushort read.ushort ( table-row ( 6 0 ) + 8 )
	#Interface methods can't be static 
	invalid offset table-row ( 6 3 ) + 6 or-ushort 0x0010 

	#XXX we don't care about CLS names (17)

	#signature shall be good (18)
	invalid offset table-row ( 6 2 ) + 10 set-ushort 0x9999

	#TODO type kind check for valuetypes (21)
	#TODO implement duplicate detection (22)

	#if (final,newslot or stric) then it must be virtual (24)
	#final
	valid offset table-row ( 6 2 ) + 6 set-ushort 0x0060
	invalid offset table-row ( 6 2 ) + 6 set-ushort 0x0020

	#newslot
	valid offset table-row ( 6 2 ) + 6 set-ushort 0x0140
	invalid offset table-row ( 6 2 ) + 6 set-ushort 0x0100

	#strict
	valid offset table-row ( 6 2 ) + 6 set-ushort 0x0240
	invalid offset table-row ( 6 2 ) + 6 set-ushort 0x0200

	#if pinvoke then it must not be virtual (25)
	#this is a pretty stupid test as all pinvokes must be static, which disallows virtual
	#method 5 is a pinvoke
	invalid offset table-row ( 6 5 ) + 6 or-ushort 0x0040

	#if !abstract then only one of: rva != 0, pinvoke or implruntime (26)
	#pinvoke with rva != 0
	invalid offset table-row ( 6 5 ) set-uint read.uint ( table-row ( 6 2 ) )

	#pinvoke with runtime
	#LAMEIMPL/SPEC either MS ignores it or the spec is ill defined 
	#invalid offset table-row ( 6 5 ) + 4 or-ushort 0x1000

	#if compilercontroled (0x0) it must have an RVA or a pinvoke
	#let's change method 3 which is part of an interface
	invalid offset table-row ( 6 3 ) + 6 set-ushort 0x05c0

	#TODO check signature (28,29,30,31,32,33)

	#if RVA = 0 then one of (abstract, runtime, pinvoke) (34)
	#let's test with an abstract class, method 6 is abstract and belongs to one.
	invalid offset table-row ( 6 7 ) + 6 set-ushort 0x0006
	#icall 
	valid offset table-row ( 6 7 ) + 6 set-ushort 0x01c6 , offset table-row ( 6 7 ) + 4 or-ushort 0x1000

	#if rva != 0 then abstract == 0 and codetypemask must be (native,cil,runtime) and rva shall be valid  (35)
	#rva != 0 and abstract == 0
	invalid offset table-row ( 6 2 ) + 6 or-ushort 0x0400
	#rva != 0 and codetypemask == OPTIL
	invalid offset table-row ( 6 2 ) + 4 set-ushort 0x0002
	#invalid rva
	invalid offset table-row ( 6 2 ) set-uint 0x999999

	#if pinvoke the rva == 0 and has a row in implmap (36)
	#method 5 is a pinvoke
	#pinvoke with rva !=0
	invalid offset table-row ( 6 5 ) set-uint 0x20f8
	#pinvoke without an implmap row
	invalid offset table-row ( 0x1C 0 ) + 2 set-ushort 0xF

	#if rtspecialname = 1 then name must be: .ctor and .cctor (37)
	#is not .ctor or .cctor
	invalid offset table-row ( 6 2 ) + 6 or-ushort 0x1800

	#.ctor or .cctor without rtspecialname (38)
	#method 9 is .ctor method 10 is .cctor
	invalid offset table-row ( 6 9 ) + 6 set-ushort 0x0006
	invalid offset table-row ( 6 10 ) + 6 set-ushort 0x0016

	#TODO do all .ctor and .cctor validation (39, 40)

	#pinvoke must be static
	invalid offset table-row ( 6 5 ) + 6 set-ushort 0x2086

	#abstract + final (set to public virtual final newslot abstract)
	invalid offset table-row ( 6 7 ) + 6 set-ushort 0x0566
}

methoddef-table-global-methods {
	assembly assembly-with-global-method.exe

	#checks for methods owned by <module> (20)
	
	#static + public
	valid offset table-row ( 6 0 ) + 6 set-ushort 0x0010
	#static + private
	valid offset table-row ( 6 0 ) + 6 set-ushort 0x0011
	#static + compiler controled
	valid offset table-row ( 6 0 ) + 6 set-ushort 0x0016

	#must be static
	invalid offset table-row ( 6 0 ) + 6 set-ushort 0x0006

	#must not be abstract
	invalid offset table-row ( 6 0 ) + 6 set-ushort 0x0416

	#must not be virtual
	invalid offset table-row ( 6 0 ) + 6 set-ushort 0x0056

	#can only be compiler controled, public or private
	#which leaves out: famandassem assem family famorassem
	#LAMEIMPL MS doesn't care about those bits.
	invalid offset table-row ( 6 0 ) + 6 set-ushort 0x0012
	invalid offset table-row ( 6 0 ) + 6 set-ushort 0x0013
	invalid offset table-row ( 6 0 ) + 6 set-ushort 0x0014
	invalid offset table-row ( 6 0 ) + 6 set-ushort 0x0015
}

methoddef-table-params {
	assembly assembly-with-methods.exe

	#method 12,13,14 have 3 params and params: 2,5,8	
	#method 13 has 3 params and params: 5
	invalid offset table-row ( 6 12 ) + 12 set-ushort 6
	invalid offset table-row ( 6 13 ) + 12 set-ushort 99 
}


param-table {
	assembly assembly-with-params.exe

	#Flags should only have valid bits (3)
	#bits not used: 2,3,5,6,7,8,910,11,14,15
	invalid offset table-row ( 8 0 ) set-bit 2
	invalid offset table-row ( 8 0 ) set-bit 3
	invalid offset table-row ( 8 0 ) set-bit 5
	invalid offset table-row ( 8 0 ) set-bit 6
	invalid offset table-row ( 8 0 ) set-bit 7
	invalid offset table-row ( 8 0 ) set-bit 8
	invalid offset table-row ( 8 0 ) set-bit 9
	invalid offset table-row ( 8 0 ) set-bit 10
	invalid offset table-row ( 8 0 ) set-bit 11
	invalid offset table-row ( 8 0 ) set-bit 14
	invalid offset table-row ( 8 0 ) set-bit 15

	#TODO verify if sequence is < number of params, requires to decode signature (4)

	#ordering
	valid offset table-row ( 8 0 ) + 2 set-ushort 0
	invalid offset table-row ( 8 0 ) + 2 set-ushort 2
	invalid offset table-row ( 8 1 ) + 2 set-ushort 1

	
	#if HasDefault = 1 then there must be a row in the constant table (6)
	#param 2 doesn't have a default
	invalid offset table-row ( 8 2 ) or-ushort 0x1000

	#if HasDefault = 0 then there must be no row in the constant table (7)
	#param 0 have a default
	invalid offset table-row ( 8 0 ) set-ushort 0x000

	#if FieldMarshal = 1 the there must be a row in the FieldMarshal table
	invalid offset table-row ( 8 1 ) set-ushort 0x2000

	invalid offset table-row ( 8 1 ) + 4 set-ushort 0x99999
	#ok to be empty
	valid offset table-row ( 8 1 ) + 4 set-ushort 0
}

interfaceimpl-table {
	assembly assembly-with-complex-type.exe

	#class cannot be null (2)
	#LAMEIMPL MS allows a null class
	valid offset table-row ( 9 0 ) set-ushort 0

	#class must be a valid row (3.a)
	invalid offset table-row ( 9 0 ) set-ushort 0x9999

	#interface must be a valid token (3.b)
	#null
	invalid offset table-row ( 9 0 ) + 2 set-ushort 0
	#invalid table bit 0x3
	invalid offset table-row ( 9 0 ) + 2 set-ushort 0x7
	#invalid token typedef
	invalid offset table-row ( 9 0 ) + 2 set-ushort 0x8800
	#invalid token typeref
	invalid offset table-row ( 9 0 ) + 2 set-ushort 0x8801
	#invalid token typespec
	invalid offset table-row ( 9 0 ) + 2 set-ushort 0x8802

	#TODO verify if the target is an interface (3.c)

}

memberref-table {
	assembly assembly-with-complex-type.exe
	
	#class must be a valid token (1 2)
	#null
	invalid offset table-row ( 10 0 ) set-ushort 0
	#invalid coded table
	invalid offset table-row ( 10 0 ) set-ushort 0x0015
	invalid offset table-row ( 10 0 ) set-ushort 0x0016
	invalid offset table-row ( 10 0 ) set-ushort 0x0017
	#invalid code index
	invalid offset table-row ( 10 0 ) set-ushort 0x1000
	invalid offset table-row ( 10 0 ) set-ushort 0x1001
	invalid offset table-row ( 10 0 ) set-ushort 0x1002
	invalid offset table-row ( 10 0 ) set-ushort 0x1003
	invalid offset table-row ( 10 0 ) set-ushort 0x1004

	#name must be valid and non-empty (3)
	invalid offset table-row ( 10 0 ) + 2 set-ushort 0x0000
	invalid offset table-row ( 10 0 ) + 2 set-ushort 0x9900

	#signature must be valid (5)
	invalid offset table-row ( 10 0 ) + 4 set-ushort 0x9900
	

	#TODO validate the signature (5)

	#LAMESPEC CompilerControled visibility (9,10) is nice but no impl care about

	#LAMESPEC what does (11) mean? 
}

constant-table {
	assembly assembly-with-constants.exe

	#type must be one of (bool, char, i1, u1, i2, u2, i4, u4, i8, u8, r4, r8, string or class
	#class must have value zero (1)
	#this means (type >= 0x02 && type <= 0x0e) or (type == 0x12 && value == 0)
	#bad type
	invalid offset table-row ( 0xB 0 ) set-byte 0x00
	invalid offset table-row ( 0xB 0 ) set-byte 0x01
	invalid offset table-row ( 0xB 0 ) set-byte 0x01
	invalid offset table-row ( 0xB 0 ) set-byte 0x0F
	invalid offset table-row ( 0xB 0 ) set-byte 0x10
	invalid offset table-row ( 0xB 0 ) set-byte 0x11
	invalid offset table-row ( 0xB 0 ) set-byte 0x13
	invalid offset table-row ( 0xB 0 ) set-byte 0x20

	#type == class && value != 0
	invalid offset table-row ( 0xB 2 ) set-byte 0x12 , offset table-row ( 0xB 2 ) + 4 set-ushort 0x0001

	#parent is a valid row in the field, property or param table (3)
	#Test for a property with a valid default value
	#First remove default from param 'a' (param table idx 0)
	#Then set the has default flag in the property table
	#Finally, make the first constant point from the part to the property (const 1, prop 0, token 0x6)
	valid offset table-row ( 0x8 0 ) set-ushort 0 , offset table-row ( 0x17 0 ) or-ushort 0x1000 , offset table-row ( 0xB 1 ) + 2 set-ushort 0x6 

	#Invalid coded table
	invalid offset table-row ( 0xB 0 ) + 2 set-ushort 0x0013 , offset table-row ( 0x04 0 ) set-ushort 0x16
	#null
	invalid offset table-row ( 0xB 0 ) + 2 set-ushort 0x0000 , offset table-row ( 0x04 0 ) set-ushort 0x16
	#bad field
	invalid offset table-row ( 0xB 0 ) + 2 set-ushort 0x00F0 , offset table-row ( 0x04 0 ) set-ushort 0x16
	#bad param
	invalid offset table-row ( 0xB 0 ) + 2 set-ushort 0x00F1 , offset table-row ( 0x04 0 ) set-ushort 0x16
	#bad property
	invalid offset table-row ( 0xB 0 ) + 2 set-ushort 0x00F2 , offset table-row ( 0x04 0 ) set-ushort 0x16

	#TODO check for dups

	#TODO check value range
	#we set it to 1 less the end of the blob heap
	invalid offset table-row ( 0xB 0 ) + 4 set-ushort read.uint ( stream-header ( 3 ) + 4 )

	#LAMEIMPL, MS doesn't bound check the constant size. Lame of them.
	invalid offset table-row ( 0xB 0 ) + 4 set-ushort read.uint ( stream-header ( 3 ) + 4 ) - 1 
}

cattr-table {
	assembly assembly-with-cattr.exe

	#parent is a valid coded index (2)
	#The spec say any table can be used, but only 19 tables are allowed on the coded token
	#Actually 20 tables are allowed, the spec doesn't mention, but generic param is allowed
	valid offset table-row ( 0xC 0 ) set-ushort 0x33
	#bad table
	invalid offset table-row ( 0xC 0 ) set-ushort 0x34
	invalid offset table-row ( 0xC 0 ) set-ushort 0x35
	invalid offset table-row ( 0xC 0 ) set-ushort 0x36

	#LAMEIMPL MS doesn't test this error
	invalid offset table-row ( 0xC 0 ) set-ushort 0x37
	#LAMEIMPL MS doesn't test this error
	invalid offset table-row ( 0xC 0 ) set-ushort 0x38

	invalid offset table-row ( 0xC 0 ) set-ushort 0x39
	invalid offset table-row ( 0xC 0 ) set-ushort 0x3A
	invalid offset table-row ( 0xC 0 ) set-ushort 0x3B
	invalid offset table-row ( 0xC 0 ) set-ushort 0x3C
	invalid offset table-row ( 0xC 0 ) set-ushort 0x3D
	invalid offset table-row ( 0xC 0 ) set-ushort 0x3E
	invalid offset table-row ( 0xC 0 ) set-ushort 0x3F

	#bad index
	invalid offset table-row ( 0xC 0 ) set-ushort 0x8801
	invalid offset table-row ( 0xC 0 ) set-ushort 0x8832

	#type is a valid token (3)
	#this uses 3 bits and only 0x2/0x3 are valid
	invalid offset table-row ( 0xC 0 ) + 2 set-ushort 0x0008
	invalid offset table-row ( 0xC 0 ) + 2 set-ushort 0x0009
	#those two tests are invalid since they result in broken cattr
	#valid offset table-row ( 0xC 0 ) + 2 set-ushort 0x000A
	#valid offset table-row ( 0xC 0 ) + 2 set-ushort 0x000B
	invalid offset table-row ( 0xC 0 ) + 2 set-ushort 0x000C
	invalid offset table-row ( 0xC 0 ) + 2 set-ushort 0x000D
	invalid offset table-row ( 0xC 0 ) + 2 set-ushort 0x000E
	invalid offset table-row ( 0xC 0 ) + 2 set-ushort 0x000F

	#value is optional (4)
	valid offset table-row ( 0xC 0 ) + 4 set-ushort 0

	#Valid is a valid blob index (5)
	invalid offset table-row ( 0xC 0 ) + 4 set-ushort 0x8888

	#TODO validate the cattr blob (6,7,8,9)
	#TODO verify is Type is a .ctor.
}

field-marshal-table {
	assembly assembly-with-complex-type.exe

	#part must be valid (2)
	#LAMEIMPL MS doesn't verify for null
	invalid offset table-row ( 0xd 0 ) set-ushort 0x0000 , offset table-row ( 0x4 5 ) set-ushort 0x0001
	invalid offset table-row ( 0xd 0 ) set-ushort 0x8800 , offset table-row ( 0x4 5 ) set-ushort 0x0001
	invalid offset table-row ( 0xd 0 ) set-ushort 0x8801 , offset table-row ( 0x4 5 ) set-ushort 0x0001

	#native type must index non null valid blob index (3)
	#LAMEIMPL MS doesn't verify for null
	invalid offset table-row ( 0xd 0 ) + 2 set-ushort 0
	invalid offset table-row ( 0xd 0 ) + 2 set-ushort 0x9900

	#TODO check for dups (4)

	#TODO check the marshalspec blob (5)
}

decl-security-table {
	assembly assembly-with-cas.exe

	#bad parent (2)
	invalid offset table-row ( 0xe 0 ) + 2 set-ushort 0x0000
	invalid offset table-row ( 0xe 0 ) + 2 set-ushort 0x0007
	invalid offset table-row ( 0xe 0 ) + 2 set-ushort 0x1000
	invalid offset table-row ( 0xe 0 ) + 2 set-ushort 0x1001
	invalid offset table-row ( 0xe 0 ) + 2 set-ushort 0x1002

	#bad permission set (6)
	invalid offset table-row ( 0xe 0 ) + 4 set-ushort 0x8800
}

class-layout-table {
	assembly assembly-with-complex-type.exe

	#valid parent row (2)
	invalid offset table-row ( 0xF 0 ) + 6 set-ushort 0x0000
	invalid offset table-row ( 0xF 0 ) + 6 set-ushort 0x0880

	#TODO check that the type is not an interface (2)
	#TODO parent must not have auto layout (3)

	#packing must be (0,1,2,4,8,16,32,64,128) (4)
	invalid offset table-row ( 0xF 0 ) set-ushort 0x0003

	#TODO do checks depending on the kind of parent (4) 

	#Check layout along the inheritance chain. (7)
}

field-layout-table {
	assembly assembly-with-complex-type.exe

	#TODO check properties of the field (2, 5, 7, 8, 9)

	#Field must be valid (4)
	invalid offset table-row ( 0x10 0 ) + 4 set-ushort 0x0000
	invalid offset table-row ( 0x10 0 ) + 4 set-ushort 0x8800

}

stand-alone-sig-table {
	assembly assembly-with-complex-type.exe

	#signature has a valid blob index (2)

	#TODO validate the blob content. (3)
	invalid offset table-row ( 0x11 0 ) set-ushort 0x8800

}

event-map-table {
	assembly assembly-with-events.exe

	#parent must be a valid typedef token
	invalid offset table-row ( 0x12 0 ) set-ushort 0x8800

	#bad eventlist
	invalid offset table-row ( 0x12 0 ) + 2 set-ushort 0x0000
	invalid offset table-row ( 0x12 0 ) + 2 set-ushort 0x8800

	#eventlist must not be duplicated and increase monotonically
	#evt list is 1,3,7 we change the first to 4
	invalid offset table-row ( 0x12 0 ) + 2 set-ushort 4
}

event-table {
	assembly assembly-with-events.exe

	#event flags have valid bits (3)
	#only bits 9 and 10 are used 

	invalid offset table-row ( 0x14 0 ) set-bit 0
	invalid offset table-row ( 0x14 0 ) set-bit 1
	invalid offset table-row ( 0x14 0 ) set-bit 2
	invalid offset table-row ( 0x14 0 ) set-bit 3
	invalid offset table-row ( 0x14 0 ) set-bit 4
	invalid offset table-row ( 0x14 0 ) set-bit 5
	invalid offset table-row ( 0x14 0 ) set-bit 6
	invalid offset table-row ( 0x14 0 ) set-bit 7
	invalid offset table-row ( 0x14 0 ) set-bit 8
	invalid offset table-row ( 0x14 0 ) set-bit 11
	invalid offset table-row ( 0x14 0 ) set-bit 12
	invalid offset table-row ( 0x14 0 ) set-bit 13
	invalid offset table-row ( 0x14 0 ) set-bit 14
	invalid offset table-row ( 0x14 0 ) set-bit 15

	#name is a valid non empty string (4)
	invalid offset table-row ( 0x14 0 ) + 2 set-ushort 0
	invalid offset table-row ( 0x14 0 ) + 2 set-ushort 0x8880

	#event type can be null (6)
	valid offset table-row ( 0x14 0 ) + 4 set-ushort 0

	#event type is valid (7)
	#coded table 0x3 is invalid
	invalid offset table-row ( 0x14 0 ) + 4 set-ushort 0x7
	invalid offset table-row ( 0x14 0 ) + 4 set-ushort 0x8880
	invalid offset table-row ( 0x14 0 ) + 4 set-ushort 0x8881
	invalid offset table-row ( 0x14 0 ) + 4 set-ushort 0x8882

	#TODO eventtype must be a class (8)

	#TODO for each row, there shall be one add_ and one remove_ row in methodsemantics (9)
	#change AddOn to Other
	invalid offset table-row ( 0x18 0 ) set-ushort 0x0004
	#change RemoveOn to Other
	invalid offset table-row ( 0x18 1 ) set-ushort 0x0004

	#TODO for each row, there can be zero or one raise_ rows (10)

	#TODO check for dups
}

property-map-table {
	assembly assembly-with-properties.exe

	#parent must be a valid typedef token
	invalid offset table-row ( 0x15 0 ) set-ushort 0x8800

	#bad propertylist
	invalid offset table-row ( 0x15 0 ) + 2 set-ushort 0x0000
	invalid offset table-row ( 0x15 0 ) + 2 set-ushort 0x8800

	#propertylist must not be duplicated and increase monotonically
	#property list is 1,3,7 we change the first to 4
	invalid offset table-row ( 0x15 0 ) + 2 set-ushort 4
}

property-table {
	assembly assembly-with-properties.exe

	#valid flags (3)
	#only bits 9, 10 and 12 are used 
	invalid offset table-row ( 0x17 0 ) set-bit 0
	invalid offset table-row ( 0x17 0 ) set-bit 1
	invalid offset table-row ( 0x17 0 ) set-bit 2
	invalid offset table-row ( 0x17 0 ) set-bit 3
	invalid offset table-row ( 0x17 0 ) set-bit 4
	invalid offset table-row ( 0x17 0 ) set-bit 5
	invalid offset table-row ( 0x17 0 ) set-bit 6
	invalid offset table-row ( 0x17 0 ) set-bit 7
	invalid offset table-row ( 0x17 0 ) set-bit 8
	invalid offset table-row ( 0x17 0 ) set-bit 11
	invalid offset table-row ( 0x17 0 ) set-bit 13
	invalid offset table-row ( 0x17 0 ) set-bit 14
	invalid offset table-row ( 0x17 0 ) set-bit 15

	#valid non empty name (4)
	invalid offset table-row ( 0x17 0 ) + 2 set-ushort 0
	invalid offset table-row ( 0x17 0 ) + 2 set-ushort 0x8800

	#type must be a non null type signature in the blob heap (6)
	invalid offset table-row ( 0x17 0 ) + 4 set-ushort 0
	invalid offset table-row ( 0x17 0 ) + 4 set-ushort 0x8800

	#TODO signature must be of the right kind (7)

	#if property has default, there must be a row in the defaults table
	#we mark row zero as having default value
	#field zero has default value
	valid offset table-row (0x17 0) + 0 or-ushort  0x1000, #mark the property with hasdefault
		  offset table-row (0x04 0) + 0 set-ushort 0x0011, #clear literal and hasdefault from the field
		  offset table-row (0x0B 0) + 2 set-ushort 0x0006  #change the parent token to row 1 of the property table (0x2) 

	invalid offset table-row (0x17 0) + 0 or-ushort  0x1000

	#TODO check for dups
}

methodimpl-table {
	assembly assembly-with-complex-type.exe

	#class shall be valid (2)
	invalid offset table-row (0x19 0) set-ushort 0 
	invalid offset table-row (0x19 0) set-ushort 0x8800

	#methodbody shall be valid (3)
	#null
	invalid offset table-row (0x19 0) + 2 set-ushort 0x0000 
	invalid offset table-row (0x19 0) + 2 set-ushort 0x0001
	#out of range
	invalid offset table-row (0x19 0) + 2 set-ushort 0x8800 
	invalid offset table-row (0x19 0) + 2 set-ushort 0x8801

	#MethodDeclaration shall be valid
	#null
	invalid offset table-row (0x19 0) + 4 set-ushort 0x0000 
	invalid offset table-row (0x19 0) + 4 set-ushort 0x0001
	#out of range
	invalid offset table-row (0x19 0) + 4 set-ushort 0x8800 
	invalid offset table-row (0x19 0) + 4 set-ushort 0x8801
	

	#TODO check MethodDeclaration method for virtual and owner type for !sealed (4,5) 	
	#TODO check MethodBody for belonging to a super type of Class,been virtual and rva != 0 (6,7,8)
	#TODO check MethodBody must belong to any ancestor or iface of Class (9)
	#TODO check MethodDeclaration method shall not be final (10)
	#TODO if MethodDeclaration is strict, it must be visible to Class (11)
	#TODO the method signature of MethodBody must match of MethodDeclaration (12)	
	#TODO no dups
}

moduleref-table {
	assembly assembly-with-module.exe

	#string must be valid (2)
	invalid offset table-row (0x1A 0) set-ushort 0
	invalid offset table-row (0x1A 0) set-ushort 0x8801

	#TODO there must be a row on the File table with the same name
}

typespec-table {
	assembly assembly-with-complex-type.exe

	#valid signature
	invalid offset table-row (0x1B 0) set-ushort 0
	invalid offset table-row (0x1B 0) set-ushort 0x8800
}

implmap-table {
	assembly assembly-with-methods.exe

	#flags has good values (2)
	#used bits: 0,1,2,7,8,9,10
	invalid offset table-row (0x1C 0) set-bit 3
	invalid offset table-row (0x1C 0) set-bit 4
	invalid offset table-row (0x1C 0) set-bit 5
	invalid offset table-row (0x1C 0) set-bit 7
	invalid offset table-row (0x1C 0) set-bit 11
	invalid offset table-row (0x1C 0) set-bit 12
	invalid offset table-row (0x1C 0) set-bit 13
	invalid offset table-row (0x1C 0) set-bit 14
	invalid offset table-row (0x1C 0) set-bit 15

	#call conv 0 and 6 are invalid
	invalid offset table-row (0x1C 0) set-ushort 0x0000
	invalid offset table-row (0x1C 0) set-ushort 0x0600
	invalid offset table-row (0x1C 0) set-ushort 0x0700

	#memberforwarded token is valid and indexes a method (3)
	#pinvoke is row 5
	invalid offset table-row (0x1C 0) + 2 set-ushort 0x0000, #null
			offset table-row (0x06 5) + 6 set-ushort 0x0444 #set method to abstract instead of pinvoke
	invalid offset table-row (0x1C 0) + 2 set-ushort 0x0002, #field
			offset table-row (0x06 5) + 6 set-ushort 0x0444 #set method to abstract instead of pinvoke
	invalid offset table-row (0x1C 0) + 2 set-ushort 0x8801, #bad method
			offset table-row (0x06 5) + 6 set-ushort 0x0444 #set method to abstract instead of pinvoke

	#charset rule is not required (4)

	#import name must be valid (5)
	invalid offset table-row (0x1C 0) + 4 set-ushort 0x0000 #null
	invalid offset table-row (0x1C 0) + 4 set-ushort 0x8800 #invalid

	#import scope must be valie (6)
	invalid offset table-row (0x1C 0) + 6 set-ushort 0x0000 #null
	invalid offset table-row (0x1C 0) + 6 set-ushort 0x8800 #invalid

	#TODO check methoddef for pinvokeimpl and state (7)
}

fieldrva-table {
	assembly assembly-with-complex-type.exe

	#rva non zero (1)
	invalid offset table-row (0x1D 0) set-uint 0
	#valid rva (2)
	invalid offset table-row (0x1D 0) set-uint 0x88880000

	#valid field (4)
	#field 17 has rva
	invalid offset table-row (0x1D 0) + 4 set-ushort 0,
			offset table-row (0x04 17) set-ushort 0x0013 #remove fieldrva from target field
	invalid offset table-row (0x1D 0) + 4 set-ushort 0x9901,
			offset table-row (0x04 17) set-ushort 0x0013 


	#TODO verify if the field is a blitable valuetype
	#TODO verify if the field.size + rva does boundcheck
}

assembly-table {
	assembly simple-assembly.exe

	#The table can have zero or 1 row (1)
	#rows
	invalid offset tables-header + 40 set-uint 2,
			offset stream-header (0) + 4 set-uint read.uint (stream-header (0) + 4) + 22 #increase the size of the #~ section

	#bad hasalg (2) 
	valid offset table-row (0x20 0) set-uint 0
	valid offset table-row (0x20 0) set-uint 0x8003
	valid offset table-row (0x20 0) set-uint 0x8004
	invalid offset table-row (0x20 0) set-uint 0x8005
	invalid offset table-row (0x20 0) set-uint 1

	#good flags (4)
	#only bits 0, 8, 14 and 15 are used
	invalid offset table-row (0x20 0) + 12 set-bit 1
	invalid offset table-row (0x20 0) + 12 set-bit 5
	invalid offset table-row (0x20 0) + 12 set-bit 9
	invalid offset table-row (0x20 0) + 12 set-bit 11
	invalid offset table-row (0x20 0) + 12 set-bit 16
	invalid offset table-row (0x20 0) + 12 set-bit 20
	invalid offset table-row (0x20 0) + 12 set-bit 23
	invalid offset table-row (0x20 0) + 12 set-bit 26
	invalid offset table-row (0x20 0) + 12 set-bit 29
	invalid offset table-row (0x20 0) + 12 set-bit 30
	invalid offset table-row (0x20 0) + 12 set-bit 31


	#valid pub key (5)
	valid offset table-row (0x20 0) + 16 set-ushort 0 
	invalid offset table-row (0x20 0) + 16 set-ushort 0x9990

	#name is a valid non-empty string (5)
	invalid offset table-row (0x20 0) + 18 set-ushort 0
	invalid offset table-row (0x20 0) + 18 set-ushort 0x9990

	#culture is an optional valid non-empty string (8)
	valid offset table-row (0x20 0) + 20 set-ushort 0 
	invalid offset table-row (0x20 0) + 20 set-ushort 0x9990

	#TODO check if culture is one of the listed cultures (9) (23.1.3)
}

assembly-ref-table {
	assembly simple-assembly.exe

	#flags can only have publickey set (2)
	valid offset table-row (0x23 0) + 8 set-uint 0
	valid offset table-row (0x23 0) + 8 set-uint 1
	invalid offset table-row (0x23 0) + 8 set-uint 0x0100
	invalid offset table-row (0x23 0) + 8 set-uint 0x4000
	invalid offset table-row (0x23 0) + 8 set-uint 0x8000
	invalid offset table-row (0x23 0) + 8 set-bit 2
	invalid offset table-row (0x23 0) + 8 set-bit 5
	invalid offset table-row (0x23 0) + 8 set-bit 9
	invalid offset table-row (0x23 0) + 8 set-bit 20
	invalid offset table-row (0x23 0) + 8 set-bit 22
	invalid offset table-row (0x23 0) + 8 set-bit 30

	#PublicKeyToken is valid (3)
	valid offset table-row (0x23 0) + 12 set-ushort 0
	invalid offset table-row (0x23 0) + 12 set-ushort 0x9700

	#name is a valid non-empty string (5)
	invalid offset table-row (0x23 0) + 14 set-ushort 0x9700
	invalid offset table-row (0x23 0) + 14 set-ushort 0

	#culture is an optional valid non-empty string (6)
	valid offset table-row (0x23 0) + 16 set-ushort 0 
	invalid offset table-row (0x23 0) + 16 set-ushort 0x9990

	#TODO check if culture is one of the listed cultures (7) (23.1.3)

	#HashValue is an optinal valid blob item (9)
	valid offset table-row (0x23 0) + 18 set-ushort 0
	invalid offset table-row (0x23 0) + 18 set-ushort 0x9990

	#it's ok to have dups
}

file-table {
	assembly assembly-with-resource.exe

	#flags is valid (1)
	#only bit 0 is valid
	invalid offset table-row (0x26 0) set-bit 1
	invalid offset table-row (0x26 0) set-bit 4
	invalid offset table-row (0x26 0) set-bit 6
	invalid offset table-row (0x26 0) set-bit 8
	invalid offset table-row (0x26 0) set-bit 11
	invalid offset table-row (0x26 0) set-bit 17
	invalid offset table-row (0x26 0) set-bit 22
	invalid offset table-row (0x26 0) set-bit 27
	invalid offset table-row (0x26 0) set-bit 29
	invalid offset table-row (0x26 0) set-bit 31

	#name is a non empty string (2)
	invalid offset table-row (0x26 0) + 4 set-ushort 0
	invalid offset table-row (0x26 0) + 4 set-ushort 0x9999

	#hash is a valid blob item (3)
	invalid offset table-row (0x26 0) + 6 set-ushort 0
	invalid offset table-row (0x26 0) + 6 set-ushort 0x9999

	#TODO check name format (I belive only the lack of directory directives should be checked)
	#TODO check for dups based on name
	#TODO check for images with rows in file and assembly tables
}

exported-type-table {
	assembly assembly-with-module.exe

	#flags is valid (it's a TypeAttribute flag set) (3)
	#Flags invalid bits: 6,9,14,15,19,21,24-31
	invalid offset table-row (0x27 0) set-bit 6 #this is a mysterious bit on MS
	invalid offset table-row (0x27 0) set-bit 9
	invalid offset table-row (0x27 0) set-bit 14
	invalid offset table-row (0x27 0) set-bit 15
	invalid offset table-row (0x27 0) set-bit 19
	valid offset table-row (0x27 0) set-bit 21 #this is the non specified FORWARDER bit
	invalid offset table-row (0x27 0) set-bit 24
	invalid offset table-row (0x27 0) set-bit 25
	invalid offset table-row (0x27 0) set-bit 26
	invalid offset table-row (0x27 0) set-bit 27
	invalid offset table-row (0x27 0) set-bit 28
	invalid offset table-row (0x27 0) set-bit 29
	invalid offset table-row (0x27 0) set-bit 30
	invalid offset table-row (0x27 0) set-bit 31

	#type 0 is toplevel
	#type 1 is nested
	#if Implementation points to file table visibility must be public (4)
	#invalid offset table-row (0x27 0) set-uint 0x100005 #LAMEIMPL/SPEC this check is not really relevant

	#if Implementation points to exported type table visibility must be nested public (5)
	#invalid offset table-row (0x27 1) set-uint 0x100005 #LAMEIMPL/SPEC this check is not really relevant
	
	#typename is a valid non-empty string (7)
	invalid offset table-row (0x27 0) + 8 set-ushort 0
	invalid offset table-row (0x27 0) + 8 set-ushort 0x9900
	
	#typenamedpace is a valid string (8,9)
	invalid offset table-row (0x27 0) + 10 set-ushort 0x9900

	#nested types must have an empty typenamespace (11)
	valid offset table-row (0x27 1) + 10 set-ushort 0
	invalid offset table-row (0x27 1) + 10 set-ushort 1 #LAMEIMPL ms doesn't check this.

	#12 implementation is a valid non empty token (12)
	invalid offset table-row (0x27 0) + 12 set-ushort 0
	invalid offset table-row (0x27 0) + 12 set-ushort 0x8880

	#TODO check if a type in the exported table is not defined in the current module (2)
	#TODO check if target type is valid and public (6)
	#TODO check for dups (14,15,16)
}

manifest-resource-table {
	assembly assembly-with-resource.exe

	#flags must have a valid value (3)
	#only bits 0-2 are used
	invalid offset table-row (0x28 0) + 4 set-bit 3
	invalid offset table-row (0x28 0) + 4 set-bit 7
	invalid offset table-row (0x28 0) + 4 set-bit 16
	invalid offset table-row (0x28 0) + 4 set-bit 31

	#inly 0x1 and 0x2 are valid visibility values (4)
	invalid offset table-row (0x28 0) + 4 set-uint 0
	invalid offset table-row (0x28 0) + 4 set-uint 3
	invalid offset table-row (0x28 0) + 4 set-uint 4
	invalid offset table-row (0x28 0) + 4 set-uint 5
	invalid offset table-row (0x28 0) + 4 set-uint 6
	invalid offset table-row (0x28 0) + 4 set-uint 7

	#name shall index a valid non-empty (5)
	invalid offset table-row (0x28 0) + 8 set-ushort 0
	invalid offset table-row (0x28 0) + 8 set-ushort 0x9900

	#if implementation is null the offset is a valid offset based on cli resource entry (7)
	valid offset table-row (0x28 0) + 10 set-ushort 0,
			offset table-row (0x28 0) + 0  set-uint 1

	#LAMEIMPL it doesn't check the resource offset! 
	invalid offset table-row (0x28 0) + 10 set-ushort 0,
			offset table-row (0x28 0) + 0  set-uint 0x990000
	

	#implementation is a valid token (8)
	#does it accept exported type? 
	invalid offset table-row (0x28 0) + 10 set-ushort 0x0006

	#coded table 4 is invalid
	invalid offset table-row (0x28 0) + 10 set-ushort 0x0007
	#bad index
	invalid offset table-row (0x28 0) + 10 set-ushort 0x8800
	invalid offset table-row (0x28 0) + 10 set-ushort 0x8801

	#if implementation point to a file it's index must be zero (10)
	#row 0 is a file resource
	invalid offset table-row (0x28 0) set-uint 1
	
	#TODO check for dups (9)
}

nested-class-table {
	assembly assembly-with-complex-type.exe

	#both nested and enclosing classes must index valid non-null rows in the typedef table (2,3)
	invalid offset table-row (0x29 0) set-ushort 0
	invalid offset table-row (0x29 0) set-ushort 0x9800

	invalid offset table-row (0x29 0) + 2 set-ushort 0
	invalid offset table-row (0x29 0) + 2 set-ushort 0x9800

	invalid offset table-row (0x29 0) + 2 set-ushort read.ushort (table-row (0x29 0))

	#TODO check for dups based on nestedclass (5) 
}


generic-param-table {
	assembly assembly-with-generics.exe

	#bad flags
	#only 0-4 are used
	invalid offset table-row (0x2A 0) + 2 set-bit 5
	invalid offset table-row (0x2A 0) + 2 set-bit 6
	invalid offset table-row (0x2A 0) + 2 set-bit 7
	invalid offset table-row (0x2A 0) + 2 set-bit 8
	invalid offset table-row (0x2A 0) + 2 set-bit 9
	invalid offset table-row (0x2A 0) + 2 set-bit 10
	invalid offset table-row (0x2A 0) + 2 set-bit 11
	invalid offset table-row (0x2A 0) + 2 set-bit 12
	invalid offset table-row (0x2A 0) + 2 set-bit 13
	invalid offset table-row (0x2A 0) + 2 set-bit 14
	invalid offset table-row (0x2A 0) + 2 set-bit 15

	#variance 0x3 is invalid
	invalid offset table-row (0x2A 0) + 2 set-ushort 0x3

	#bad or null owner
	invalid offset table-row (0x2A 0) + 4 set-ushort 0
	invalid offset table-row (0x2A 0) + 4 set-ushort 0x8800
	invalid offset table-row (0x2A 0) + 4 set-ushort 0x8801

	#bad or empty name	
	invalid offset table-row (0x2A 0) + 6 set-ushort 0
	invalid offset table-row (0x2A 0) + 6 set-ushort 0x8800

	#wrong order
	invalid offset table-row (0x2A 0)  set-ushort 1,
			offset table-row (0x2A 1)  set-ushort 0

	#not monotonically growing
	invalid offset table-row (0x2A 0)  set-ushort 0,
			offset table-row (0x2A 1)  set-ushort 0

	#start big
	invalid offset table-row (0x2A 0)  set-ushort 1,
			offset table-row (0x2A 1)  set-ushort 2

	#step bigger than 1
	invalid offset table-row (0x2A 0)  set-ushort 0,
			offset table-row (0x2A 1)  set-ushort 2
}

method-spec-table {
	assembly assembly-with-generics.exe

	#method is a valid token
	invalid offset table-row (0x2B 0) set-ushort 0
	invalid offset table-row (0x2B 0) set-ushort 0x8800
	invalid offset table-row (0x2B 0) set-ushort 0x8801

	#instantiation is invalid

	invalid offset table-row (0x2B 0) + 2 set-ushort 0
	invalid offset table-row (0x2B 0) + 2 set-ushort 0xABCD

	#TODO check the content of the blob sig and validate against the token.
}

generic-param-constraint-table {
	assembly assembly-with-generics.exe

	#owner is a valid rown in the gparam table
	invalid offset table-row (0x2C 0) set-ushort 0x0000
	invalid offset table-row (0x2C 0) set-ushort 0x2345

	#constaint is a valid token
	invalid offset table-row (0x2C 0) + 2 set-ushort 0x0000
	invalid offset table-row (0x2C 0) + 2 set-ushort 0x0007
	invalid offset table-row (0x2C 0) + 2 set-ushort 0x8800
	invalid offset table-row (0x2C 0) + 2 set-ushort 0x8801
	invalid offset table-row (0x2C 0) + 2 set-ushort 0x8802

	#TODO check for dups and sorting.
}
