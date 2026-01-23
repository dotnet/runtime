// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       coder.h
/// \brief      Compresses or uncompresses a file
//
//  Authors:    Lasse Collin
//              Jia Tan
//
///////////////////////////////////////////////////////////////////////////////

enum operation_mode {
	MODE_COMPRESS,
	MODE_DECOMPRESS,
	MODE_TEST,
	MODE_LIST,
};


// NOTE: The order of these is significant in suffix.c.
enum format_type {
	FORMAT_AUTO,
	FORMAT_XZ,
	FORMAT_LZMA,
#ifdef HAVE_LZIP_DECODER
	FORMAT_LZIP,
#endif
	FORMAT_RAW,
};


/// Array of these hold the entries specified with --block-list.
typedef struct {
	/// Uncompressed size of the Block
	uint64_t size;

	/// Filter chain to use for this Block (chains[chain_num])
	unsigned chain_num;
} block_list_entry;


/// Operation mode of the command line tool. This is set in args.c and read
/// in several files.
extern enum operation_mode opt_mode;

/// File format to use when encoding or what format(s) to accept when
/// decoding. This is a global because it's needed also in suffix.c.
/// This is set in args.c.
extern enum format_type opt_format;

/// If true, the compression settings are automatically adjusted down if
/// they exceed the memory usage limit.
extern bool opt_auto_adjust;

/// If true, stop after decoding the first stream.
extern bool opt_single_stream;

/// If non-zero, start a new .xz Block after every opt_block_size bytes
/// of input. This has an effect only when compressing to the .xz format.
extern uint64_t opt_block_size;

/// List of block size and filter chain pointer pairs.
extern block_list_entry *opt_block_list;

/// Size of the largest Block that was specified in --block-list.
/// This is used to limit the block_size option of multithreaded encoder.
/// It's waste of memory to specify a too large block_size and reducing
/// it might even allow using more threads in some cases.
///
/// NOTE: If the last entry in --block-list is the special value of 0
/// (which gets converted to UINT64_MAX), it counts here as UINT64_MAX too.
/// This way the multithreaded encoder's Block size won't be reduced.
extern uint64_t block_list_largest;

/// Bitmask indicating which filter chains we specified in --block-list.
extern uint32_t block_list_chain_mask;

/// Set the integrity check type used when compressing
extern void coder_set_check(lzma_check check);

/// Set preset number
extern void coder_set_preset(uint32_t new_preset);

/// Enable extreme mode
extern void coder_set_extreme(void);

/// Add a filter to the custom filter chain
extern void coder_add_filter(lzma_vli id, void *options);

/// Set and partially validate compression settings. This can also be used
/// in decompression or test mode with the raw format.
extern void coder_set_compression_settings(void);

/// Compress or decompress the given file
extern void coder_run(const char *filename);

#ifndef NDEBUG
/// Free the memory allocated for the coder and kill the worker threads.
extern void coder_free(void);
#endif

/// Create filter chain from string
extern void coder_add_filters_from_str(const char *filter_str);

/// Add or overwrite a filter that can be used by the block-list.
extern void coder_add_block_filters(const char *str, size_t slot);
