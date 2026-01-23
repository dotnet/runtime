// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       xzdec.c
/// \brief      Simple single-threaded tool to uncompress .xz or .lzma files
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#include "sysdefs.h"
#include "lzma.h"

#include <stdarg.h>
#include <errno.h>
#include <locale.h>
#include <stdio.h>

#ifndef _MSC_VER
#	include <unistd.h>
#endif

#ifdef HAVE_CAP_RIGHTS_LIMIT
#	include <sys/capsicum.h>
#endif

#ifdef HAVE_LINUX_LANDLOCK
#	include "my_landlock.h"
#endif

#if defined(HAVE_CAP_RIGHTS_LIMIT) || defined(HAVE_PLEDGE) \
		|| defined(HAVE_LINUX_LANDLOCK)
#	define ENABLE_SANDBOX 1
#endif

#include "getopt.h"
#include "tuklib_progname.h"
#include "tuklib_mbstr_nonprint.h"
#include "tuklib_exit.h"

#ifdef TUKLIB_DOSLIKE
#	include <fcntl.h>
#	include <io.h>
#	ifdef _MSC_VER
#		define fileno _fileno
#		define setmode _setmode
#	endif
#endif


#ifdef LZMADEC
#	define TOOL_FORMAT "lzma"
#else
#	define TOOL_FORMAT "xz"
#endif


/// Error messages are suppressed if this is zero, which is the case when
/// --quiet has been given at least twice.
static int display_errors = 2;


lzma_attribute((__format__(__printf__, 1, 2)))
static void
my_errorf(const char *fmt, ...)
{
	va_list ap;
	va_start(ap, fmt);

	if (display_errors) {
		fprintf(stderr, "%s: ", progname);
		vfprintf(stderr, fmt, ap);
		fprintf(stderr, "\n");
	}

	va_end(ap);
	return;
}


tuklib_attr_noreturn
static void
help(void)
{
	printf(
"Usage: %s [OPTION]... [FILE]...\n"
"Decompress files in the ." TOOL_FORMAT " format to standard output.\n"
"\n"
"  -d, --decompress   (ignored, only decompression is supported)\n"
"  -k, --keep         (ignored, files are never deleted)\n"
"  -c, --stdout       (ignored, output is always written to standard output)\n"
"  -q, --quiet        specify *twice* to suppress errors\n"
"  -Q, --no-warn      (ignored, the exit status 2 is never used)\n"
"  -h, --help         display this help and exit\n"
"  -V, --version      display the version number and exit\n"
"\n"
"With no FILE, or when FILE is -, read standard input.\n"
"\n"
"Report bugs to <" PACKAGE_BUGREPORT "> (in English or Finnish).\n"
PACKAGE_NAME " home page: <" PACKAGE_URL ">\n", progname);

	tuklib_exit(EXIT_SUCCESS, EXIT_FAILURE, display_errors);
}


tuklib_attr_noreturn
static void
version(void)
{
	printf(TOOL_FORMAT "dec (" PACKAGE_NAME ") " LZMA_VERSION_STRING "\n"
			"liblzma %s\n", lzma_version_string());

	tuklib_exit(EXIT_SUCCESS, EXIT_FAILURE, display_errors);
}


/// Parses command line options.
static void
parse_options(int argc, char **argv)
{
	static const char short_opts[] = "cdkhqQV";
	static const struct option long_opts[] = {
		{ "stdout",       no_argument,         NULL, 'c' },
		{ "to-stdout",    no_argument,         NULL, 'c' },
		{ "decompress",   no_argument,         NULL, 'd' },
		{ "uncompress",   no_argument,         NULL, 'd' },
		{ "keep",         no_argument,         NULL, 'k' },
		{ "quiet",        no_argument,         NULL, 'q' },
		{ "no-warn",      no_argument,         NULL, 'Q' },
		{ "help",         no_argument,         NULL, 'h' },
		{ "version",      no_argument,         NULL, 'V' },
		{ NULL,           0,                   NULL, 0   }
	};

	int c;

	while ((c = getopt_long(argc, argv, short_opts, long_opts, NULL))
			!= -1) {
		switch (c) {
		case 'c':
		case 'd':
		case 'k':
		case 'Q':
			break;

		case 'q':
			if (display_errors > 0)
				--display_errors;

			break;

		case 'h':
			help();

		case 'V':
			version();

		default:
			exit(EXIT_FAILURE);
		}
	}

	return;
}


static void
uncompress(lzma_stream *strm, FILE *file, const char *filename)
{
	lzma_ret ret;

	// Initialize the decoder
#ifdef LZMADEC
	ret = lzma_alone_decoder(strm, UINT64_MAX);
#else
	ret = lzma_stream_decoder(strm, UINT64_MAX, LZMA_CONCATENATED);
#endif

	// The only reasonable error here is LZMA_MEM_ERROR.
	if (ret != LZMA_OK) {
		my_errorf("%s", ret == LZMA_MEM_ERROR ? strerror(ENOMEM)
				: "Internal error (bug)");
		exit(EXIT_FAILURE);
	}

	// Input and output buffers
	uint8_t in_buf[BUFSIZ];
	uint8_t out_buf[BUFSIZ];

	strm->avail_in = 0;
	strm->next_out = out_buf;
	strm->avail_out = BUFSIZ;

	lzma_action action = LZMA_RUN;

	while (true) {
		if (strm->avail_in == 0) {
			strm->next_in = in_buf;
			strm->avail_in = fread(in_buf, 1, BUFSIZ, file);

			if (ferror(file)) {
				// POSIX says that fread() sets errno if
				// an error occurred. ferror() doesn't
				// touch errno.
				my_errorf("%s: Error reading input file: %s",
					tuklib_mask_nonprint(filename),
					strerror(errno));
				exit(EXIT_FAILURE);
			}

#ifndef LZMADEC
			// When using LZMA_CONCATENATED, we need to tell
			// liblzma when it has got all the input.
			if (feof(file))
				action = LZMA_FINISH;
#endif
		}

		ret = lzma_code(strm, action);

		// Write and check write error before checking decoder error.
		// This way as much data as possible gets written to output
		// even if decoder detected an error.
		if (strm->avail_out == 0 || ret != LZMA_OK) {
			const size_t write_size = BUFSIZ - strm->avail_out;

			if (fwrite(out_buf, 1, write_size, stdout)
					!= write_size) {
				// Wouldn't be a surprise if writing to stderr
				// would fail too but at least try to show an
				// error message.
#if defined(_WIN32) && !defined(__CYGWIN__)
				// On native Windows, broken pipe is reported
				// as EINVAL. Don't show an error message
				// in this case.
				if (errno != EINVAL)
#endif
				{
					my_errorf("Cannot write to "
						"standard output: "
						"%s", strerror(errno));
				}
				exit(EXIT_FAILURE);
			}

			strm->next_out = out_buf;
			strm->avail_out = BUFSIZ;
		}

		if (ret != LZMA_OK) {
			if (ret == LZMA_STREAM_END) {
#ifdef LZMADEC
				// Check that there's no trailing garbage.
				if (strm->avail_in != 0
						|| fread(in_buf, 1, 1, file)
							!= 0
						|| !feof(file))
					ret = LZMA_DATA_ERROR;
				else
					return;
#else
				// lzma_stream_decoder() already guarantees
				// that there's no trailing garbage.
				assert(strm->avail_in == 0);
				assert(action == LZMA_FINISH);
				assert(feof(file));
				return;
#endif
			}

			const char *msg;
			switch (ret) {
			case LZMA_MEM_ERROR:
				msg = strerror(ENOMEM);
				break;

			case LZMA_FORMAT_ERROR:
				msg = "File format not recognized";
				break;

			case LZMA_OPTIONS_ERROR:
				// FIXME: Better message?
				msg = "Unsupported compression options";
				break;

			case LZMA_DATA_ERROR:
				msg = "File is corrupt";
				break;

			case LZMA_BUF_ERROR:
				msg = "Unexpected end of input";
				break;

			default:
				msg = "Internal error (bug)";
				break;
			}

			my_errorf("%s: %s", tuklib_mask_nonprint(filename),
					msg);
			exit(EXIT_FAILURE);
		}
	}
}


#ifdef ENABLE_SANDBOX
static void
sandbox_enter(int src_fd)
{
#if defined(HAVE_CAP_RIGHTS_LIMIT)
	// Capsicum needs FreeBSD 10.2 or later.
	cap_rights_t rights;

	if (cap_enter())
		goto error;

	if (cap_rights_limit(src_fd, cap_rights_init(&rights, CAP_READ)))
		goto error;

	// If not reading from stdin, remove all capabilities from it.
	if (src_fd != STDIN_FILENO && cap_rights_limit(
			STDIN_FILENO, cap_rights_init(&rights)))
		goto error;

	if (cap_rights_limit(STDOUT_FILENO, cap_rights_init(&rights,
			CAP_WRITE)))
		goto error;

	if (cap_rights_limit(STDERR_FILENO, cap_rights_init(&rights,
			CAP_WRITE)))
		goto error;

#elif defined(HAVE_PLEDGE)
	// pledge() was introduced in OpenBSD 5.9.
	if (pledge("stdio", ""))
		goto error;

	(void)src_fd;

#elif defined(HAVE_LINUX_LANDLOCK)
	struct landlock_ruleset_attr attr;
	if (my_landlock_ruleset_attr_forbid_all(&attr) > 0) {
		const int ruleset_fd = my_landlock_create_ruleset(
				&attr, sizeof(attr), 0);
		if (ruleset_fd < 0)
			goto error;

		// All files we need should have already been opened. Thus,
		// we don't need to add any rules using landlock_add_rule(2)
		// before activating the sandbox.
		if (my_landlock_restrict_self(ruleset_fd, 0) != 0)
			goto error;

		(void)close(ruleset_fd);
	}

	(void)src_fd;

#else
#	error ENABLE_SANDBOX is defined but no sandboxing method was found.
#endif

	return;

error:
#ifdef HAVE_CAP_RIGHTS_LIMIT
	// If a kernel is configured without capability mode support or
	// used in an emulator that does not implement the capability
	// system calls, then the Capsicum system calls will fail and set
	// errno to ENOSYS. In that case xzdec will silently run without
	// the sandbox.
	if (errno == ENOSYS)
		return;
#endif

	my_errorf("Failed to enable the sandbox");
	exit(EXIT_FAILURE);
}
#endif


int
main(int argc, char **argv)
{
	// Initialize progname which will be used in error messages.
	tuklib_progname_init(argv);

#ifdef HAVE_PLEDGE
	// OpenBSD's pledge(2) sandbox.
	// Initially enable the sandbox slightly more relaxed so that
	// the process can still open files. This allows the sandbox to
	// be enabled when parsing command line arguments and decompressing
	// all files (the more strict sandbox only restricts the last file
	// that is decompressed).
	if (pledge("stdio rpath", "")) {
		my_errorf("Failed to enable the sandbox");
		exit(EXIT_FAILURE);
	}
#endif

#ifdef HAVE_LINUX_LANDLOCK
	// Prevent the process from gaining new privileges. This must be done
	// before landlock_restrict_self(2) but since we will never need new
	// privileges, this call can be done here already.
	//
	// This is supported since Linux 3.5. Ignore the return value to
	// keep compatibility with old kernels. landlock_restrict_self(2)
	// will fail if the no_new_privs attribute isn't set, thus if prctl()
	// fails here the error will still be detected when it matters.
	(void)prctl(PR_SET_NO_NEW_PRIVS, 1, 0, 0, 0);
#endif

	// We need to set the locale even though we don't have any
	// translated messages:
	//
	//   - tuklib_mask_nonprint() has locale-specific behavior (LC_CTYPE).
	//
	//   - This is needed on Windows to make non-ASCII filenames display
	//     properly when the active code page has been set to UTF-8
	//     in the application manifest.
	setlocale(LC_ALL, "");

	// Parse the command line options.
	parse_options(argc, argv);

	// The same lzma_stream is used for all files that we decode. This way
	// we don't need to reallocate memory for every file if they use same
	// compression settings.
	lzma_stream strm = LZMA_STREAM_INIT;

	// Some systems require setting stdin and stdout to binary mode.
#ifdef TUKLIB_DOSLIKE
	setmode(fileno(stdin), O_BINARY);
	setmode(fileno(stdout), O_BINARY);
#endif

	if (optind == argc) {
		// No filenames given, decode from stdin.
#ifdef ENABLE_SANDBOX
		sandbox_enter(STDIN_FILENO);
#endif
		uncompress(&strm, stdin, "(stdin)");
	} else {
		// Loop through the filenames given on the command line.
		do {
			FILE *src_file;
			const char *src_name;

			// "-" indicates stdin.
			if (strcmp(argv[optind], "-") == 0) {
				src_file = stdin;
				src_name = "(stdin)";
			} else {
				src_name = argv[optind];
				src_file = fopen(src_name, "rb");
				if (src_file == NULL) {
					my_errorf("%s: %s",
						tuklib_mask_nonprint(
							src_name),
						strerror(errno));
					exit(EXIT_FAILURE);
				}
			}
#ifdef ENABLE_SANDBOX
			// Enable the strict sandbox for the last file.
			// Then the process can no longer open additional
			// files. The typical xzdec use case is to decompress
			// a single file so this way the strictest sandboxing
			// is used in most cases.
			if (optind == argc - 1)
				sandbox_enter(fileno(src_file));
#endif
			uncompress(&strm, src_file, src_name);

			if (src_file != stdin)
				(void)fclose(src_file);
		} while (++optind < argc);
	}

#ifndef NDEBUG
	// Free the memory only when debugging. Freeing wastes some time,
	// but allows detecting possible memory leaks with Valgrind.
	lzma_end(&strm);
#endif

	tuklib_exit(EXIT_SUCCESS, EXIT_FAILURE, display_errors);
}
