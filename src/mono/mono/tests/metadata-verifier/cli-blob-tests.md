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

}

method-def-sig2 {
	assembly assembly-with-custommod.exe

	#method 0 has a modreq
	#bytes: size cconv param_count mod_req compressed_token
	invalid offset blob.i (table-row (6 0) + 10) + 4 set-byte 0x7C
	invalid offset blob.i (table-row (6 0) + 10) + 4 set-byte 0x07

	#switch modreq to modopt
	valid offset blob.i (table-row (6 0) + 10) + 3 set-byte 0x20


}