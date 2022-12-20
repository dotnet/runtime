cattr-without-named-args {
	assembly assembly-with-cattr-enc.exe

	#bad prolog
	valid offset blob.i (table-row (0x0C 0) + 4) + 1 set-ushort 0x0001
	invalid offset blob.i (table-row (0x0C 0) + 4) + 1 set-ushort 0x0000
	invalid offset blob.i (table-row (0x0C 0) + 4) + 1 set-ushort 0x0099
	invalid offset blob.i (table-row (0x0C 0) + 4) + 1 set-ushort 0x0101

	#WARNING: peverify don't check custom attributes format beyond the prolog
	#so it's pointless to use it for this.
	#We'll take the easy road as well and when verifying the encoded data
	#assume that the target constructor can be decoded and use the runtime signature.

	#bad size
	invalid offset blob.i (table-row (0x0C 0) + 4) + 0 set-byte 0x0
	invalid offset blob.i (table-row (0x0C 0) + 4) + 0 set-byte 0x1

	#set size to something huge
	invalid offset blob.i (table-row (0x0C 0) + 4) + 0 set-byte 0xBF,
			offset blob.i (table-row (0x0C 0) + 4) + 1 set-byte 0xFF,
			offset blob.i (table-row (0x0C 0) + 4) + 2 set-ushort 0x0001

}
