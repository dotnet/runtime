method-def-sig {
	assembly assembly-with-methods.exe

	#bad first byte
	#method zero is a default ctor
	#0 -> default 5 -> vararg

	#signature size, zero is invalid
	invalid offset blob.i (table-row (6 0) + 10) set-byte 0

	#cconv
	invalid offset blob.i (table-row (6 0) + 10) + 1 set-byte 0x26
	invalid offset blob.i (table-row (6 0) + 10) + 1 set-byte 0x27
	invalid offset blob.i (table-row (6 0) + 10) + 1 set-byte 0x28
	invalid offset blob.i (table-row (6 0) + 10) + 1 set-byte 0x29
	invalid offset blob.i (table-row (6 0) + 10) + 1 set-byte 0x2A
	invalid offset blob.i (table-row (6 0) + 10) + 1 set-byte 0x2B
	invalid offset blob.i (table-row (6 0) + 10) + 1 set-byte 0x2C
	invalid offset blob.i (table-row (6 0) + 10) + 1 set-byte 0x2D
	invalid offset blob.i (table-row (6 0) + 10) + 1 set-byte 0x2E
	invalid offset blob.i (table-row (6 0) + 10) + 1 set-byte 0x2F

	#upper nimble flags 0x80 is invalid	
	invalid offset blob.i (table-row (6 0) + 10) + 1 set-bit 7

	#sig is too small to decode param count
	invalid offset blob.i (table-row (6 0) + 10) set-byte 1

	#sig is too small to decode return type
	invalid offset blob.i (table-row (6 0) + 10) set-byte 2

	#zero generic args
	#method 1 is generic
	#bytes: size cconv gen_param_count
	invalid offset blob.i (table-row (6 1) + 10) + 2 set-byte 0

	#set ret type to an invalid value
	invalid offset blob.i (table-row (6 0) + 10) + 3 set-byte 0
	invalid offset blob.i (table-row (6 0) + 10) + 3 set-byte 0x17
	invalid offset blob.i (table-row (6 0) + 10) + 3 set-byte 0x1A
	invalid offset blob.i (table-row (6 0) + 10) + 3 set-byte 0x21 #mono doesn't support internal type
	invalid offset blob.i (table-row (6 0) + 10) + 3 set-byte 0x40 #modifier
	invalid offset blob.i (table-row (6 0) + 10) + 3 set-byte 0x41 #sentinel
	invalid offset blob.i (table-row (6 0) + 10) + 3 set-byte 0x45 #pinner
	invalid offset blob.i (table-row (6 0) + 10) + 3 set-byte 0x50 #type
	invalid offset blob.i (table-row (6 0) + 10) + 3 set-byte 0x51 #boxed
	invalid offset blob.i (table-row (6 0) + 10) + 3 set-byte 0x52 #reserved
	invalid offset blob.i (table-row (6 0) + 10) + 3 set-byte 0x53 #field
	invalid offset blob.i (table-row (6 0) + 10) + 3 set-byte 0x54 #property
	invalid offset blob.i (table-row (6 0) + 10) + 3 set-byte 0x55 #enum

	#bad args
	#method 12 has sig void (int,int,int)
	#bytes: size cconv param_count void int32 int32 int32
	valid offset blob.i (table-row (6 12) + 10) + 4 set-byte 0x05
	valid offset blob.i (table-row (6 12) + 10) + 5 set-byte 0x06
	valid offset blob.i (table-row (6 12) + 10) + 6 set-byte 0x07

	#void
	invalid offset blob.i (table-row (6 12) + 10) + 5 set-byte 0x01

	#byref without anything after
	invalid offset blob.i (table-row (6 12) + 10) + 4 set-byte 0x10
	invalid offset blob.i (table-row (6 12) + 10) + 5 set-byte 0x10
	invalid offset blob.i (table-row (6 12) + 10) + 6 set-byte 0x10
}

#Test for stuff in the ret that can't be expressed with C#
method-def-ret-misc {
	assembly assembly-with-custommod.exe

	#method 0 has a modreq
	#bytes: size cconv param_count mod_req compressed_token
	invalid offset blob.i (table-row (6 0) + 10) + 4 set-byte 0x7C
	invalid offset blob.i (table-row (6 0) + 10) + 4 set-byte 0x07

	#switch modreq to modopt
	valid offset blob.i (table-row (6 0) + 10) + 3 set-byte 0x20

	#2 times byref
	#method 4 returns byref
	#bytes: size cconv param_count byref int32
	invalid offset blob.i (table-row (6 4) + 10) + 4 set-byte 0x10
	#byref of typedref
	invalid offset blob.i (table-row (6 4) + 10) + 4 set-byte 0x16

}

method-ref-sig {
	assembly assembly-with-signatures.exe

	#member ref 0 is has a vararg sig 
	#member ref 1 don't use vararg

	#2 sentinels
	#bytes: size cconv pcount void str obj obj obj obj ... i32 i32 i32
	invalid offset blob.i (table-row (0xA 0) + 4) + 10 set-byte 0x41
	invalid offset blob.i (table-row (0xA 0) + 4) + 11 set-byte 0x41

	#sentinel but not vararg
	invalid offset blob.i (table-row (0xA 0) + 4) + 1 set-byte 0
}

stand-alone-method-sig {
	assembly assembly-with-calli.exe

	#standalone sig 0x2 points to a calli sig
	valid offset blob.i (table-row (0x11 0)) + 1 set-byte 0x0
	valid offset blob.i (table-row (0x11 0)) + 1 set-byte 0x1
	valid offset blob.i (table-row (0x11 0)) + 1 set-byte 0x2
	valid offset blob.i (table-row (0x11 0)) + 1 set-byte 0x3
	valid offset blob.i (table-row (0x11 0)) + 1 set-byte 0x4
	valid offset blob.i (table-row (0x11 0)) + 1 set-byte 0x5

	#sig is int32 (int32)
	#size cconv pcount(1) int32 int32 ->
	#size cconv gcount(1) pcount(0) int32
	#cannot have generics
	invalid offset blob.i (table-row (0x11 0)) + 1 set-byte 0x10,
			offset blob.i (table-row (0x11 0)) + 2 set-byte 1,
			offset blob.i (table-row (0x11 0)) + 3 set-byte 0
}

field-sig {
	assembly assembly-with-complex-type.exe

	#first byte must be 6
	invalid offset blob.i (table-row (4 0) + 4) + 1 set-byte 0x0
	invalid offset blob.i (table-row (4 0) + 4) + 1 set-byte 0x5
	invalid offset blob.i (table-row (4 0) + 4) + 1 set-byte 0x7
	invalid offset blob.i (table-row (4 0) + 4) + 1 set-byte 0x16
	invalid offset blob.i (table-row (4 0) + 4) + 1 set-byte 0x26
}

property-sig {
	assembly assembly-with-properties.exe

	#bad size
	invalid offset blob.i (table-row (0x17 0) + 4) set-byte 0x0
	invalid offset blob.i (table-row (0x17 0) + 4) set-byte 0x1

	#cconv must be 0x08 or 0x28
	valid offset blob.i (table-row (0x17 0) + 4) + 1 set-byte 0x08
	valid offset blob.i (table-row (0x17 0) + 4) + 1 set-byte 0x28

	invalid offset blob.i (table-row (0x17 0) + 4) + 1 set-byte 0x09
	invalid offset blob.i (table-row (0x17 0) + 4) + 1 set-byte 0x29
	invalid offset blob.i (table-row (0x17 0) + 4) + 1 set-byte 0x48
	invalid offset blob.i (table-row (0x17 0) + 4) + 1 set-byte 0x18
	invalid offset blob.i (table-row (0x17 0) + 4) + 1 set-byte 0x07
	invalid offset blob.i (table-row (0x17 0) + 4) + 1 set-byte 0x00
}

locals-sig {
	assembly assembly-with-locals.exe

	#bad local sig
	#row 0 has tons of locals
	#row 1 is int32&, int32 
	#row 2 is typedref

	#typedref with byref
	#row 1 is:      cconv pcount(2) byref int32      int32 
	#row 1 goes to: cconv pcount(2) byref typedbyref int32
	invalid offset blob.i (table-row (0x11 1)) + 4 set-byte 0x16

	#byref pinned int32
	#row 1 is:      cconv pcount(2) byref int32  int32 
	#row 1 goes to: cconv pcount(1) byref pinned int32

	invalid offset blob.i (table-row (0x11 1)) + 2 set-byte 0x01,
			offset blob.i (table-row (0x11 1)) + 4 set-byte 0x45

	#pinned pinned int32
	#row 1 is:      cconv pcount(2) byref  int32  int32 
	#row 1 goes to: cconv pcount(1) pinned pinned int32
	#LAMEIMPL MS doesn't care about this
	valid offset blob.i (table-row (0x11 1)) + 2 set-byte 0x01,
			offset blob.i (table-row (0x11 1)) + 3 set-byte 0x45,
			offset blob.i (table-row (0x11 1)) + 4 set-byte 0x45
}

type-enc {
	assembly assembly-with-types.exe

	#valid
	#change type from int to string
	valid offset blob.i (table-row (0x04 0) + 4) + 2 set-byte 0x0E

	#field 10 is cconv PTR int32
	#make it: cconv PTR modreq
	invalid offset blob.i (table-row (0x04 11) + 4) + 3 set-byte 0x1f

	#pointer to pointer (not enought room to parse pointed to type)
	#make it: cconv PTR PTR
	invalid offset blob.i (table-row (0x04 11) + 4) + 3 set-byte 0x0f

	#value type / class
	#make it not have room for the token
	invalid offset blob.i (table-row (0x04 0) + 4) + 2 set-byte 0x11
	invalid offset blob.i (table-row (0x04 0) + 4) + 2 set-byte 0x12

	#var / mvar
	#make it not have room for the token
	invalid offset blob.i (table-row (0x04 0) + 4) + 2 set-byte 0x13
	invalid offset blob.i (table-row (0x04 0) + 4) + 2 set-byte 0x1e

	#general array
	#field 3 is a int32[,,]: cconv ARRAY int32 rank(3) nsizes(0) nlowb(0)
	#make the array type invalid (byref/typedref/void/plain wrong)
	invalid offset blob.i (table-row (0x04 3) + 4) + 3 set-byte 0x00
	invalid offset blob.i (table-row (0x04 3) + 4) + 3 set-byte 0x01
	invalid offset blob.i (table-row (0x04 3) + 4) + 3 set-byte 0x10
	#LAMEIMPL MS accepts arrays of typedbyref, which is illegal and unsafe
	invalid offset blob.i (table-row (0x04 3) + 4) + 3 set-byte 0x16

	#LAMEIMPL MS verifier doesn't catch this one (runtime does)
	#rank 0 
	invalid offset blob.i (table-row (0x04 3) + 4) + 4 set-byte 0x00
	#large nsizes
	invalid offset blob.i (table-row (0x04 3) + 4) + 5 set-byte 0x1F
	#large nlowb
	invalid offset blob.i (table-row (0x04 3) + 4) + 6 set-byte 0x1F


	#generic inst
	#field 20 is Test<int32>; 21 is class [mscorlib]System.IComparable`1<object>; 22 is valuetype Test2<!0>
	#format is cconc GINST KIND token arg_count type*

	#make bad kind
	invalid offset blob.i (table-row (0x04 20) + 4) + 3 set-byte 0x05

	#bad token
	invalid offset blob.i (table-row (0x04 20) + 4) + 4 set-byte 0x3F
	#zero arg_count
	invalid offset blob.i (table-row (0x04 20) + 4) + 5 set-byte 0x0
	#bad arg_count
	invalid offset blob.i (table-row (0x04 20) + 4) + 5 set-byte 0x10

	#fnptr
	#field 10 is a fnptr
	#format is: cconv FNPTR cconv pcount ret param* sentinel? param*
	#LAMESPEC, it lacks the fact that fnptr allows for unmanaged call conv 
	#bad callconv
	invalid offset blob.i (table-row (0x04 10) + 4) + 3 set-byte 0x88

	#szarray
	#field 17 is an array with modreq on target
	#format is: cconv SZARRAY cmod* type
	#array type is void
	invalid offset blob.i (table-row (0x04 17) + 4) + 3 set-byte 0x01
}

typespec-sig {
	assembly assembly-with-typespec.exe

	#LAMESPEC
	#ecma spec doesn't allow simple types such as uint32. But MS does and there
	#is no harm into supporting it.
	#row zero is "void*" encoded as PTR VOID
	valid offset blob.i (table-row (0x1B 0)) + 1 set-byte 0x09

	#type zero is invalid
	invalid offset blob.i (table-row (0x1B 0)) + 1 set-byte 0x0

	#LAMESPEC part II, MS allows for cmods on a typespec as well 
	#modreq int32 is invalid
	#typespec 2 is "modreq int32*" encoded as: PTR CMOD_REQD token INT32
	#change int to CMOD_REQD token INT32 
	valid offset blob.i (table-row (0x1B 2)) + 1 set-byte 0x1f, #CMOD_REQD
			offset blob.i (table-row (0x1B 2)) + 2 set-byte read.byte (blob.i (table-row (0x1B 2)) + 3), #token
			offset blob.i (table-row (0x1B 2)) + 3 set-byte 0x08 #int8

	#typedref is fine too.
	valid offset blob.i (table-row (0x1B 2)) + 0 set-byte 0x16
}

methodspec-sig {
	assembly assembly-with-generics.exe

	#LAMESPEC spec is completelly wrong on this one. method spec holds simply a generic instantation
	#no type on it

	#first byte is the genericinst callconv 0xA
	#row zero is Gen<!1> or: GENRICINST gcount(1) type*
	invalid offset blob.i (table-row (0x2B 0) + 2) + 1 set-byte 0x08

	#zero arg count
	invalid offset blob.i (table-row (0x2B 0) + 2) + 2 set-byte 0x0

	#bad argument
	invalid offset blob.i (table-row (0x2B 0) + 2) + 3 set-byte 0x01
}

method-header {
	assembly assembly-with-methods.exe

	#invalid header kind
	#method zero is an empty .ctor (), so it takes 7 bytes (call super + ret) so we do 7 << 2 | header kind
	invalid offset translate.rva.ind (table-row (0x06 0)) + 0 set-byte 0x1C
	invalid offset translate.rva.ind (table-row (0x06 0)) + 0 set-byte 0x1D

	#method 1 has fat header
	#size must be 3
	invalid offset translate.rva.ind (table-row (0x06 1)) + 0 set-ushort 0x0013
	invalid offset translate.rva.ind (table-row (0x06 1)) + 0 set-ushort 0x1013
	invalid offset translate.rva.ind (table-row (0x06 1)) + 0 set-ushort 0x2013
	invalid offset translate.rva.ind (table-row (0x06 1)) + 0 set-ushort 0x5013
	invalid offset translate.rva.ind (table-row (0x06 1)) + 0 set-ushort 0xF013

	#maxstack can be anything between 0-2^16-1, it's up to the IL verifier to use it.

	#make codesize huge enought to overflow
	invalid offset translate.rva.ind (table-row (0x06 1)) + 4 set-uint 0x1FFFFFF0

	#bad local vars token
	#out of bounds
	invalid offset translate.rva.ind (table-row (0x06 1)) + 8 set-uint 0x1100FFFF
	#wrong table
	invalid offset translate.rva.ind (table-row (0x06 1)) + 8 set-uint 0x1B000001

	#bad fat header flags
	#only 0x08 and 0x10 allowed
	#regular value is 
	invalid offset translate.rva.ind (table-row (0x06 1)) + 0 set-ushort 0x3033 #or 0x20
	invalid offset translate.rva.ind (table-row (0x06 1)) + 0 set-ushort 0x3053
	invalid offset translate.rva.ind (table-row (0x06 1)) + 0 set-ushort 0x3093
	invalid offset translate.rva.ind (table-row (0x06 1)) + 0 set-ushort 0x3113
	invalid offset translate.rva.ind (table-row (0x06 1)) + 0 set-ushort 0x3213
	invalid offset translate.rva.ind (table-row (0x06 1)) + 0 set-ushort 0x3413
	invalid offset translate.rva.ind (table-row (0x06 1)) + 0 set-ushort 0x3813

	#methods 2, 4 and 6 have EH tables. 2 and 4 are regular, 6 is fat 4 has 2 EH entries
	#LAMEIMPL (our) well, 2 and 4 could be thin, but mono's SRE isn't keen to use small form
	#thin format must have size that is n*12+4 fat n*24+4

	#set invalid flags
	valid offset translate.rva.ind (table-row (0x06 2)) + 4 set-ushort 0x1C #set the code size to be sure

	invalid offset translate.rva.ind (table-row (0x06 2)) + 40 or-byte 0x02
	invalid offset translate.rva.ind (table-row (0x06 2)) + 40 or-byte 0x04
	invalid offset translate.rva.ind (table-row (0x06 2)) + 40 or-byte 0x08
	invalid offset translate.rva.ind (table-row (0x06 2)) + 40 or-byte 0x10
	invalid offset translate.rva.ind (table-row (0x06 2)) + 40 or-byte 0x20

	#set invalid size
	#not multiple of n*24+4
	invalid offset translate.rva.ind (table-row (0x06 2)) + 41 set-byte 0x10
	invalid offset translate.rva.ind (table-row (0x06 2)) + 41 set-byte 0x1F

	#out of bound
	invalid offset translate.rva.ind (table-row (0x06 2)) + 40 set-uint 0x5FFFFF41

	#extra section is at + 40, so EH table at + 44, class token at + 64
	#bad table
	invalid offset translate.rva.ind (table-row (0x06 2)) + 64 set-uint 0x11000001
	#bad token idx
	invalid offset translate.rva.ind (table-row (0x06 2)) + 64 set-uint 0x010FF001
	invalid offset translate.rva.ind (table-row (0x06 2)) + 64 set-uint 0x020FF001
	invalid offset translate.rva.ind (table-row (0x06 2)) + 64 set-uint 0x1B0FF001

}
