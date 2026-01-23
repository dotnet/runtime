// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       test_microlzma.c
/// \brief      Tests MicroLZMA encoding and decoding
//
//  Author:     Jia Tan
//
///////////////////////////////////////////////////////////////////////////////

#include "tests.h"

#define BUFFER_SIZE 1024


#ifdef HAVE_ENCODER_LZMA1

// MicroLZMA encoded "Hello\nWorld\n" output size in bytes.
#define ENCODED_OUTPUT_SIZE 17

// Byte array of "Hello\nWorld\n". This is used for various encoder tests.
static const uint8_t hello_world[] = { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x0A,
		0x57, 0x6F, 0x72, 0x6C, 0x64, 0x0A };

// This is the CRC32 value of the MicroLZMA encoding of "Hello\nWorld\n".
// The settings used were based on LZMA_PRESET_DEFAULT as of liblzma 5.6.0.
// This assumes MicroLZMA is correct in liblzma 5.6.0, which is safe
// considering the encoded "Hello\nWorld\n" can successfully be decoded at
// this time. This is to test for regressions that cause MicroLZMA output
// to change.
static const uint32_t hello_world_encoded_crc = 0x3CDE40A8;


// Function implementation borrowed from lzma_decoder.c. It is needed to
// ensure the first byte of a MicroLZMA stream is set correctly with the
// negation of the LZMA properties.
static bool
lzma_lzma_lclppb_decode(lzma_options_lzma *options, uint8_t byte)
{
	if (byte > (4 * 5 + 4) * 9 + 8)
		return true;

	// See the file format specification to understand this.
	options->pb = byte / (9 * 5);
	byte -= options->pb * 9 * 5;
	options->lp = byte / 9;
	options->lc = byte - options->lp * 9;

	return options->lc + options->lp > LZMA_LCLP_MAX;
}


///////////////////
// Encoder tests //
///////////////////

// This tests a few of the basic options. These options are not unique to
// MicroLZMA in any way, its mostly ensuring that the options are actually
// being checked before initializing the decoder internals.
static void
test_encode_options(void)
{
	lzma_stream strm = LZMA_STREAM_INIT;
	lzma_options_lzma opt_lzma;

	// Initialize with default options.
	assert_false(lzma_lzma_preset(&opt_lzma, LZMA_PRESET_DEFAULT));

	// NULL stream
	assert_lzma_ret(lzma_microlzma_encoder(NULL, &opt_lzma),
			LZMA_PROG_ERROR);

	// lc/lp/pb = 5/0/2 (lc invalid)
	opt_lzma.lc = 5;
	opt_lzma.lp = 0;
	opt_lzma.pb = 2;
	assert_lzma_ret(lzma_microlzma_encoder(&strm, &opt_lzma),
			LZMA_OPTIONS_ERROR);

	// lc/lp/pb = 0/5/2 (lp invalid)
	opt_lzma.lc = 0;
	opt_lzma.lp = 5;
	opt_lzma.pb = 2;
	assert_lzma_ret(lzma_microlzma_encoder(&strm, &opt_lzma),
			LZMA_OPTIONS_ERROR);

	// lc/lp/pb = 3/2/2 (lc + lp invalid)
	opt_lzma.lc = 3;
	opt_lzma.lp = 2;
	opt_lzma.pb = 2;
	assert_lzma_ret(lzma_microlzma_encoder(&strm, &opt_lzma),
			LZMA_OPTIONS_ERROR);

	// lc/lp/pb = 3/0/5 (pb invalid)
	opt_lzma.lc = 3;
	opt_lzma.lp = 0;
	opt_lzma.pb = 5;
	assert_lzma_ret(lzma_microlzma_encoder(&strm, &opt_lzma),
			LZMA_OPTIONS_ERROR);

	// Zero out lp, pb, lc options to not interfere with later tests.
	opt_lzma.lp = 0;
	opt_lzma.pb = 0;
	opt_lzma.lc = 0;

	// Set invalid dictionary size.
	opt_lzma.dict_size = LZMA_DICT_SIZE_MIN - 1;
	assert_lzma_ret(lzma_microlzma_encoder(&strm, &opt_lzma),
			LZMA_OPTIONS_ERROR);

	// Maximum dictionary size for the encoder, as described in lzma12.h
	// is 1.5 GiB.
	opt_lzma.dict_size = (UINT32_C(1) << 30) + (UINT32_C(1) << 29) + 1;
	assert_lzma_ret(lzma_microlzma_encoder(&strm, &opt_lzma),
			LZMA_OPTIONS_ERROR);

	lzma_end(&strm);
}


static void
test_encode_basic(void)
{
	lzma_stream strm = LZMA_STREAM_INIT;
	lzma_options_lzma opt_lzma;

	// The lzma_lzma_preset return value is inverse of what it perhaps
	// should be, that is, it returns false on success.
	assert_false(lzma_lzma_preset(&opt_lzma, LZMA_PRESET_DEFAULT));

	// Initialize the encoder using the default options.
	assert_lzma_ret(lzma_microlzma_encoder(&strm, &opt_lzma), LZMA_OK);

	uint8_t output[BUFFER_SIZE];

	strm.next_in = hello_world;
	strm.avail_in = sizeof(hello_world);
	strm.next_out = output;
	strm.avail_out = sizeof(output);

	// Everything must be encoded in one lzma_code() call.
	assert_lzma_ret(lzma_code(&strm, LZMA_FINISH), LZMA_STREAM_END);

	// Check that the entire input was consumed.
	assert_uint_eq(strm.total_in, sizeof(hello_world));

	// Check that the first byte in the output stream is not 0x00.
	// In a regular raw LZMA stream the first byte is always 0x00.
	// In MicroLZMA the first byte replaced by the bitwise-negation
	// of the LZMA properties.
	assert_uint(output[0], !=, 0x00);

	const uint8_t props = ~output[0];

	lzma_options_lzma test_options;
	assert_false(lzma_lzma_lclppb_decode(&test_options, props));

	assert_uint_eq(opt_lzma.lc, test_options.lc);
	assert_uint_eq(opt_lzma.lp, test_options.lp);
	assert_uint_eq(opt_lzma.pb, test_options.pb);

	// Compute the check over the output data. This is compared to
	// the expected check value.
	const uint32_t check_val = lzma_crc32(output, strm.total_out, 0);

	assert_uint_eq(check_val, hello_world_encoded_crc);

	lzma_end(&strm);
}


// This tests the behavior when strm.avail_out is so small it cannot hold
// the header plus 1 encoded byte (< 6).
static void
test_encode_small_out(void)
{
	lzma_stream strm = LZMA_STREAM_INIT;
	lzma_options_lzma opt_lzma;

	assert_false(lzma_lzma_preset(&opt_lzma, LZMA_PRESET_DEFAULT));

	assert_lzma_ret(lzma_microlzma_encoder(&strm, &opt_lzma), LZMA_OK);

	uint8_t output[BUFFER_SIZE];

	strm.next_in = hello_world;
	strm.avail_in = sizeof(hello_world);
	strm.next_out = output;
	strm.avail_out = 5;

	// LZMA_PROG_ERROR is expected when strm.avail_out < 6
	assert_lzma_ret(lzma_code(&strm, LZMA_FINISH), LZMA_PROG_ERROR);

	// The encoder must be reset because coders cannot be used again
	// after returning LZMA_PROG_ERROR.
	assert_lzma_ret(lzma_microlzma_encoder(&strm, &opt_lzma), LZMA_OK);

	// Reset strm.avail_out to be > 6, but not enough to hold all of the
	// compressed data.
	strm.avail_out = ENCODED_OUTPUT_SIZE - 1;

	// Encoding should not return an error now.
	assert_lzma_ret(lzma_code(&strm, LZMA_FINISH), LZMA_STREAM_END);
	assert_uint(strm.total_in, <, sizeof(hello_world));

	lzma_end(&strm);
}


// LZMA_FINISH is the only supported action. All others must
// return LZMA_PROG_ERROR.
static void
test_encode_actions(void)
{
	lzma_stream strm = LZMA_STREAM_INIT;
	lzma_options_lzma opt_lzma;

	assert_false(lzma_lzma_preset(&opt_lzma, LZMA_PRESET_DEFAULT));

	const lzma_action actions[] = {
		LZMA_RUN,
		LZMA_SYNC_FLUSH,
		LZMA_FULL_FLUSH,
		LZMA_FULL_BARRIER,
	};

	for (size_t i = 0; i < ARRAY_SIZE(actions); ++i) {
		assert_lzma_ret(lzma_microlzma_encoder(&strm, &opt_lzma),
				LZMA_OK);

		uint8_t output[BUFFER_SIZE];

		strm.next_in = hello_world;
		strm.avail_in = sizeof(hello_world);
		strm.next_out = output;
		strm.avail_out = sizeof(output);

		assert_lzma_ret(lzma_code(&strm, actions[i]),
				LZMA_PROG_ERROR);
	}

	lzma_end(&strm);
}
#endif // HAVE_ENCODER_LZMA1


///////////////////
// Decoder tests //
///////////////////

#if defined(HAVE_DECODER_LZMA1) && defined(HAVE_ENCODER_LZMA1)

// Byte array of "Goodbye World!". This is used for various decoder tests.
static const uint8_t goodbye_world[] = { 0x47, 0x6F, 0x6F, 0x64, 0x62,
		0x79, 0x65, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64, 0x21 };

static uint8_t *goodbye_world_encoded = NULL;
static size_t goodbye_world_encoded_size = 0;


// Helper function to encode data and return the compressed size.
static size_t
basic_microlzma_encode(const uint8_t *input, size_t in_size,
		uint8_t **compressed)
{
	lzma_stream strm = LZMA_STREAM_INIT;
	lzma_options_lzma opt_lzma;

	// Lazy way to set the output size since the input should never
	// inflate by much in these simple test cases. This is tested to
	// be large enough after encoding to fit the entire input, so if
	// this assumption does not hold then this will fail.
	const size_t out_size = in_size << 1;

	*compressed = tuktest_malloc(out_size);

	// Always encode with the default options for simplicity.
	if (lzma_lzma_preset(&opt_lzma, LZMA_PRESET_DEFAULT))
		goto decoder_setup_error;

	if (lzma_microlzma_encoder(&strm, &opt_lzma) != LZMA_OK)
		goto decoder_setup_error;

	strm.next_in = input;
	strm.avail_in = in_size;
	strm.next_out = *compressed;
	strm.avail_out = out_size;

	if (lzma_code(&strm, LZMA_FINISH) != LZMA_STREAM_END)
		goto decoder_setup_error;

	// Check that the entire input was consumed and that it fit into
	// the output buffer.
	if (strm.total_in != in_size)
		goto decoder_setup_error;

	lzma_end(&strm);

	// lzma_end() doesn't touch other members of lzma_stream than
	// lzma_stream.internal so using strm.total_out here is fine.
	return strm.total_out;

decoder_setup_error:
	tuktest_error("Failed to initialize decoder tests");
	return 0;
}


static void
test_decode_options(void)
{
	// NULL stream
	assert_lzma_ret(lzma_microlzma_decoder(NULL, BUFFER_SIZE,
			sizeof(hello_world), true,
			LZMA_DICT_SIZE_DEFAULT), LZMA_PROG_ERROR);

	// Uncompressed size larger than max
	lzma_stream strm = LZMA_STREAM_INIT;
	assert_lzma_ret(lzma_microlzma_decoder(&strm, BUFFER_SIZE,
			LZMA_VLI_MAX + 1, true, LZMA_DICT_SIZE_DEFAULT),
			LZMA_OPTIONS_ERROR);
}


// Test that decoding succeeds when uncomp_size is correct regardless of
// the value of uncomp_size_is_exact.
static void
test_decode_uncomp_size_is_exact(void)
{
	lzma_stream strm = LZMA_STREAM_INIT;

	assert_lzma_ret(lzma_microlzma_decoder(&strm,
			goodbye_world_encoded_size,
			sizeof(goodbye_world), true,
			LZMA_DICT_SIZE_DEFAULT), LZMA_OK);

	uint8_t output[BUFFER_SIZE];

	strm.next_in = goodbye_world_encoded;
	strm.avail_in = goodbye_world_encoded_size;
	strm.next_out = output;
	strm.avail_out = sizeof(output);

	assert_lzma_ret(lzma_code(&strm, LZMA_RUN), LZMA_STREAM_END);
	assert_uint_eq(strm.total_in, goodbye_world_encoded_size);

	assert_uint_eq(strm.total_out, sizeof(goodbye_world));
	assert_array_eq(goodbye_world, output, sizeof(goodbye_world));

	// Reset decoder with uncomp_size_is_exact set to false and
	// uncomp_size set to correct value. Also test using the
	// uncompressed size as the dictionary size.
	assert_lzma_ret(lzma_microlzma_decoder(&strm,
			goodbye_world_encoded_size,
			sizeof(goodbye_world), false,
			sizeof(goodbye_world)), LZMA_OK);

	strm.next_in = goodbye_world_encoded;
	strm.avail_in = goodbye_world_encoded_size;
	strm.next_out = output;
	strm.avail_out = sizeof(output);

	assert_lzma_ret(lzma_code(&strm, LZMA_RUN), LZMA_STREAM_END);
	assert_uint_eq(strm.total_in, goodbye_world_encoded_size);

	assert_uint_eq(strm.total_out, sizeof(goodbye_world));
	assert_array_eq(goodbye_world, output, sizeof(goodbye_world));

	lzma_end(&strm);
}


// This tests decoding when MicroLZMA decoder is called with
// an incorrect uncompressed size.
static void
test_decode_uncomp_size_wrong(void)
{
	lzma_stream strm = LZMA_STREAM_INIT;
	assert_lzma_ret(lzma_microlzma_decoder(&strm,
			goodbye_world_encoded_size,
			sizeof(goodbye_world) + 1, false,
			LZMA_DICT_SIZE_DEFAULT), LZMA_OK);

	uint8_t output[BUFFER_SIZE];

	strm.next_in = goodbye_world_encoded;
	strm.avail_in = goodbye_world_encoded_size;
	strm.next_out = output;
	strm.avail_out = sizeof(output);

	// LZMA_OK should be returned because the input size given was
	// larger than the actual encoded size. The decoder is expecting
	// more input to possibly fill the uncompressed size that was set.
	assert_lzma_ret(lzma_code(&strm, LZMA_FINISH), LZMA_OK);

	assert_uint_eq(strm.total_out, sizeof(goodbye_world));

	assert_array_eq(goodbye_world, output, sizeof(goodbye_world));

	// Next, test with uncomp_size_is_exact set.
	assert_lzma_ret(lzma_microlzma_decoder(&strm,
			goodbye_world_encoded_size,
			sizeof(goodbye_world) + 1, true,
			LZMA_DICT_SIZE_DEFAULT), LZMA_OK);

	strm.next_in = goodbye_world_encoded;
	strm.avail_in = goodbye_world_encoded_size;
	strm.next_out = output;
	strm.avail_out = sizeof(output);

	// No error detected, even though all input was consumed and there
	// is more room in the output buffer.
	//
	// FIXME? LZMA_FINISH tells that no more input is coming and
	// the MicroLZMA decoder knows the exact compressed size from
	// the initialization as well. So should it return LZMA_DATA_ERROR
	// on the first call instead of relying on the generic lzma_code()
	// logic to eventually get LZMA_BUF_ERROR?
	assert_lzma_ret(lzma_code(&strm, LZMA_FINISH), LZMA_OK);
	assert_lzma_ret(lzma_code(&strm, LZMA_FINISH), LZMA_OK);
	assert_lzma_ret(lzma_code(&strm, LZMA_FINISH), LZMA_BUF_ERROR);

	assert_uint_eq(strm.total_out, sizeof(goodbye_world));
	assert_array_eq(goodbye_world, output, sizeof(goodbye_world));

	// Reset stream with uncomp_size smaller than the real
	// uncompressed size.
	assert_lzma_ret(lzma_microlzma_decoder(&strm,
			goodbye_world_encoded_size,
			ARRAY_SIZE(hello_world) - 1, true,
			LZMA_DICT_SIZE_DEFAULT), LZMA_OK);

	strm.next_in = goodbye_world_encoded;
	strm.avail_in = goodbye_world_encoded_size;
	strm.next_out = output;
	strm.avail_out = sizeof(output);

	// This case actually results in an error since it decodes the full
	// uncompressed size but the range coder is not in the proper state
	// for the stream to end.
	assert_lzma_ret(lzma_code(&strm, LZMA_RUN), LZMA_DATA_ERROR);

	lzma_end(&strm);
}


static void
test_decode_comp_size_wrong(void)
{
	lzma_stream strm = LZMA_STREAM_INIT;

	// goodbye_world_encoded_size + 1 is safe because extra space was
	// allocated for goodbye_world_encoded. The extra space isn't
	// initialized but it shouldn't be read either, thus Valgrind
	// has to remain happy with this code.
	assert_lzma_ret(lzma_microlzma_decoder(&strm,
			goodbye_world_encoded_size + 1,
			sizeof(goodbye_world), true,
			LZMA_DICT_SIZE_DEFAULT), LZMA_OK);

	uint8_t output[BUFFER_SIZE];

	strm.next_in = goodbye_world_encoded;
	strm.avail_in = goodbye_world_encoded_size;
	strm.next_out = output;
	strm.avail_out = sizeof(output);

	// When uncomp_size_is_exact is set, the compressed size must be
	// correct or else LZMA_DATA_ERROR is returned.
	assert_lzma_ret(lzma_code(&strm, LZMA_FINISH), LZMA_DATA_ERROR);

	assert_lzma_ret(lzma_microlzma_decoder(&strm,
			goodbye_world_encoded_size + 1,
			sizeof(goodbye_world), false,
			LZMA_DICT_SIZE_DEFAULT), LZMA_OK);

	strm.next_in = goodbye_world_encoded;
	strm.avail_in = goodbye_world_encoded_size;
	strm.next_out = output;
	strm.avail_out = sizeof(output);

	// When uncomp_size_is_exact is not set, the decoder does not
	// detect when the compressed size is wrong as long as all of the
	// expected output has been decoded. This is because the decoder
	// assumes that the real uncompressed size might be bigger than
	// the specified value and in that case more input might be needed
	// as well.
	assert_lzma_ret(lzma_code(&strm, LZMA_FINISH), LZMA_STREAM_END);

	lzma_end(&strm);
}


static void
test_decode_bad_lzma_properties(void)
{
	// Alter first byte to encode invalid LZMA properties.
	uint8_t *compressed = tuktest_malloc(goodbye_world_encoded_size);
	memcpy(compressed, goodbye_world_encoded, goodbye_world_encoded_size);

	// lc=3, lp=2, pb=2
	compressed[0] = (uint8_t)~0x6FU;

	lzma_stream strm = LZMA_STREAM_INIT;
	assert_lzma_ret(lzma_microlzma_decoder(&strm,
			goodbye_world_encoded_size,
			sizeof(goodbye_world), false,
			LZMA_DICT_SIZE_DEFAULT), LZMA_OK);

	uint8_t output[BUFFER_SIZE];

	strm.next_in = compressed;
	strm.avail_in = goodbye_world_encoded_size;
	strm.next_out = output;
	strm.avail_out = sizeof(output);

	assert_lzma_ret(lzma_code(&strm, LZMA_RUN), LZMA_OPTIONS_ERROR);

	// Use valid, but incorrect LZMA properties.
	// lc=3, lp=1, pb=2
	compressed[0] = (uint8_t)~0x66;

	assert_lzma_ret(lzma_microlzma_decoder(&strm,
			goodbye_world_encoded_size,
			ARRAY_SIZE(goodbye_world), true,
			LZMA_DICT_SIZE_DEFAULT), LZMA_OK);

	strm.next_in = compressed;
	strm.avail_in = goodbye_world_encoded_size;
	strm.next_out = output;
	strm.avail_out = sizeof(output);

	assert_lzma_ret(lzma_code(&strm, LZMA_RUN), LZMA_DATA_ERROR);

	lzma_end(&strm);
}
#endif


extern int
main(int argc, char **argv)
{
	tuktest_start(argc, argv);

#ifndef HAVE_ENCODER_LZMA1
	tuktest_early_skip("LZMA1 encoder disabled");
#else
	tuktest_run(test_encode_options);
	tuktest_run(test_encode_basic);
	tuktest_run(test_encode_small_out);
	tuktest_run(test_encode_actions);

	// MicroLZMA decoder tests require the basic encoder functionality.
#	ifdef HAVE_DECODER_LZMA1
	goodbye_world_encoded_size = basic_microlzma_encode(goodbye_world,
			sizeof(goodbye_world), &goodbye_world_encoded);

	tuktest_run(test_decode_options);
	tuktest_run(test_decode_uncomp_size_is_exact);
	tuktest_run(test_decode_uncomp_size_wrong);
	tuktest_run(test_decode_comp_size_wrong);
	tuktest_run(test_decode_bad_lzma_properties);
#	endif

	return tuktest_end();
#endif
}
