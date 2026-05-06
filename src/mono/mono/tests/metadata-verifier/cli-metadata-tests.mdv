cli-metadata-root {
	assembly simple-assembly.exe

	#signature
	valid offset translate.rva.ind ( cli-header + 8 ) set-uint 0x424A5342
	valid offset cli-metadata + 0 set-uint 0x424A5342

	invalid offset cli-metadata + 0 set-uint 0x434A5342
	invalid offset cli-metadata + 0 set-uint 0x42455342
	invalid offset cli-metadata + 0 set-uint 0x424A0342
	invalid offset cli-metadata + 0 set-uint 0x424A5332

	#we don't care about major/minor versions no runtime cares about them

	#size too small
	invalid offset cli-header + 12 set-uint 15
	invalid offset cli-header + 12 set-uint 20
	invalid offset cli-header + 12 set-uint 30

	#version name is irrelevant as well

	#the stream must have exactly 5 streams
	valid offset cli-metadata + 16 + read.uint ( cli-metadata + 12 ) + 2 set-ushort 5
	invalid offset cli-metadata + 16 + read.uint ( cli-metadata + 12 ) + 2 set-ushort 4
}

cli-metadata-stream-headers {
	assembly simple-assembly.exe
	#simple-assembly has version v2.0.50727 so the heade takes 32 bytes

	#just to make sure
	valid offset cli-metadata + 32 set-uint 0x6c
	valid offset stream-header ( 0 ) + 0 set-uint 0x6c

	#size too small
	invalid offset cli-header + 12 set-uint 34
	invalid offset cli-header + 12 set-uint 39


	#offset doesn't bounds check
	invalid offset stream-header ( 0 ) + 0 set-uint 0x888888
	invalid offset stream-header ( 1 ) + 0 set-uint 0x888888
	invalid offset stream-header ( 2 ) + 0 set-uint 0x888888
	invalid offset stream-header ( 3 ) + 0 set-uint 0x888888
	invalid offset stream-header ( 4 ) + 0 set-uint 0x888888

	#size doesn't bounds check
	invalid offset stream-header ( 0 ) + 4 set-uint 0x888888
	invalid offset stream-header ( 1 ) + 4 set-uint 0x888888
	invalid offset stream-header ( 2 ) + 4 set-uint 0x888888
	invalid offset stream-header ( 3 ) + 4 set-uint 0x888888
	invalid offset stream-header ( 4 ) + 4 set-uint 0x888888

	#unkwnown name
	invalid offset stream-header ( 0 ) + 8 set-byte 0x42

	#duplicate name, change #~ to #US
	invalid offset stream-header ( 0 ) + 9 set-byte 0x55 , offset stream-header ( 0 ) + 10 set-byte 0x53
}
