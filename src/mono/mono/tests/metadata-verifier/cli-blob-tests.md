method-def-sig {
	assembly assembly-with-methods.exe

	#bad first byte
	#method zero is a default ctor
	#0 -> default 5 -> vararg

	#signature size, zero is invalid
	invalid offset blob.i (table-row (6 0) + 12) set-byte 0

	#cconv
	invalid offset blob.i (table-row (6 0) + 12) + 1 set-byte 0x26
	invalid offset blob.i (table-row (6 0) + 12) + 1 set-byte 0x27
	invalid offset blob.i (table-row (6 0) + 12) + 1 set-byte 0x28
	invalid offset blob.i (table-row (6 0) + 12) + 1 set-byte 0x29
	invalid offset blob.i (table-row (6 0) + 12) + 1 set-byte 0x2A
	invalid offset blob.i (table-row (6 0) + 12) + 1 set-byte 0x2B
	invalid offset blob.i (table-row (6 0) + 12) + 1 set-byte 0x2C
	invalid offset blob.i (table-row (6 0) + 12) + 1 set-byte 0x2D
	invalid offset blob.i (table-row (6 0) + 12) + 1 set-byte 0x2E
	invalid offset blob.i (table-row (6 0) + 12) + 1 set-byte 0x2F

	#upper nimble flags 0x80 is invalid	
	invalid offset blob.i (table-row (6 0) + 12) + 1 set-bit 7

	#sig is too small to decode param count
	invalid offset blob.i (table-row (6 0) + 12) set-byte 1

	#sig is too small to decode return type
	invalid offset blob.i (table-row (6 0) + 12) set-byte 2
}