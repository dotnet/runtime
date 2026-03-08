## SPDX-License-Identifier: 0BSD
## Author: Lasse Collin

EXTRA_DIST += rangecoder/price_tablegen.c

liblzma_la_SOURCES += rangecoder/range_common.h

if COND_ENCODER_LZMA1
liblzma_la_SOURCES += \
	rangecoder/range_encoder.h \
	rangecoder/price.h \
	rangecoder/price_table.c
endif

if COND_DECODER_LZMA1
liblzma_la_SOURCES += rangecoder/range_decoder.h
endif
