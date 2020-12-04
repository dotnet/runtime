MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

EXTRA_DIST = arm64-codegen.h codegen-test.c

test-codegen:
	$(CC) $(CFLAGS) -I../../.. -I../../eglib/ ../../eglib/.libs/libeglib.a -o codegen-test codegen-test.c
	./codegen-test > tmp.s
	$(CC) $(CFLAGS) -c -o tmp.o tmp.s
	objdump -d --triple=arm64e tmp.o
