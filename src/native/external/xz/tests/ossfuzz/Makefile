# SPDX-License-Identifier: 0BSD

FUZZ_TARGET_SRCS = $(wildcard *.c)
FUZZ_TARGET_BINS = $(FUZZ_TARGET_SRCS:.c=)

all: $(FUZZ_TARGET_BINS)

%: %.c
	$(CC) $(CFLAGS) -c $< -I ../../src/liblzma/api/ ;
	$(CXX) $(CXXFLAGS) $(LIB_FUZZING_ENGINE) $(<:.c=.o) -o $(OUT)/$@ \
		../../src/liblzma/.libs/liblzma.a ;

# The generated binaries are not removed, just the object files. The
# binaries are created to the $(OUT) directory and must be removed by the
# fuzzing framework.
clean:
	rm -f *.o
