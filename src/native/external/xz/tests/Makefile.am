## SPDX-License-Identifier: 0BSD
## Author: Lasse Collin

EXTRA_DIST = \
	files \
	ossfuzz \
	tests.cmake \
	tuktest.h \
	tests.h \
	tests_w32res.rc \
	test_files.sh \
	test_compress.sh \
	test_compress_generated_abc \
	test_compress_generated_random \
	test_compress_generated_text \
	test_scripts.sh \
	test_suffix.sh \
	xzgrep_expected_output

AM_CPPFLAGS = \
	-I$(top_srcdir)/src/common \
	-I$(top_srcdir)/src/liblzma/api \
	-I$(top_srcdir)/src/liblzma

LDADD = $(top_builddir)/src/liblzma/liblzma.la

LDADD += $(LTLIBINTL)

# Windows resource compiler support
.rc.o:
	$(RC) $(DEFS) $(DEFAULT_INCLUDES) $(INCLUDES) \
		$(AM_CPPFLAGS) $(CPPFLAGS) $(RCFLAGS) -i $< -o $@

if COND_W32
LDADD += tests_w32res.o
endif

check_PROGRAMS = \
	create_compress_files \
	test_check \
	test_hardware \
	test_stream_flags \
	test_filter_flags \
	test_filter_str \
	test_block_header \
	test_index \
	test_index_hash \
	test_bcj_exact_size \
	test_memlimit \
	test_lzip_decoder \
	test_vli

TESTS = \
	test_check \
	test_hardware \
	test_stream_flags \
	test_filter_flags \
	test_filter_str \
	test_block_header \
	test_index \
	test_index_hash \
	test_bcj_exact_size \
	test_memlimit \
	test_lzip_decoder \
	test_vli \
	test_files.sh \
	test_suffix.sh \
	test_compress_generated_abc \
	test_compress_generated_random \
	test_compress_generated_text

if COND_MICROLZMA
check_PROGRAMS += test_microlzma
TESTS += test_microlzma
endif

if COND_SCRIPTS
TESTS += test_scripts.sh
endif

clean-local:
	-rm -f compress_generated_* \
		xzgrep_test_output xzgrep_test_1.xz xzgrep_test_2.xz
