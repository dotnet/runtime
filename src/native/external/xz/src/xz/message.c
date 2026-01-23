// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       message.c
/// \brief      Printing messages
//
//  Authors:    Lasse Collin
//              Jia Tan
//
///////////////////////////////////////////////////////////////////////////////

#include "private.h"
#include "tuklib_mbstr_wrap.h"
#include <stdarg.h>


/// Number of the current file
static unsigned int files_pos = 0;

/// Total number of input files; zero if unknown.
static unsigned int files_total;

/// Verbosity level
static enum message_verbosity verbosity = V_WARNING;

/// Filename which we will print with the verbose messages
static const char *filename;

/// True once the a filename has been printed to stderr as part of progress
/// message. If automatic progress updating isn't enabled, this becomes true
/// after the first progress message has been printed due to user sending
/// SIGINFO, SIGUSR1, or SIGALRM. Once this variable is true, we will print
/// an empty line before the next filename to make the output more readable.
static bool first_filename_printed = false;

/// This is set to true when we have printed the current filename to stderr
/// as part of a progress message. This variable is useful only if not
/// updating progress automatically: if user sends many SIGINFO, SIGUSR1, or
/// SIGALRM signals, we won't print the name of the same file multiple times.
static bool current_filename_printed = false;

/// True if we should print progress indicator and update it automatically
/// if also verbose >= V_VERBOSE.
static bool progress_automatic = false;

/// True if message_progress_start() has been called but
/// message_progress_end() hasn't been called yet.
static bool progress_started = false;

/// This is true when a progress message was printed and the cursor is still
/// on the same line with the progress message. In that case, a newline has
/// to be printed before any error messages.
static bool progress_active = false;

/// Pointer to lzma_stream used to do the encoding or decoding.
static lzma_stream *progress_strm;

/// This is true if we are in passthru mode (not actually compressing or
/// decompressing) and thus cannot use lzma_get_progress(progress_strm, ...).
/// That is, we are using coder_passthru() in coder.c.
static bool progress_is_from_passthru;

/// Expected size of the input stream is needed to show completion percentage
/// and estimate remaining time.
static uint64_t expected_in_size;


// Use alarm() and SIGALRM when they are supported. This has two minor
// advantages over the alternative of polling gettimeofday():
//  - It is possible for the user to send SIGINFO, SIGUSR1, or SIGALRM to
//    get intermediate progress information even when --verbose wasn't used
//    or stderr is not a terminal.
//  - alarm() + SIGALRM seems to have slightly less overhead than polling
//    gettimeofday().
#ifdef SIGALRM

const int message_progress_sigs[] = {
	SIGALRM,
#ifdef SIGINFO
	SIGINFO,
#endif
#ifdef SIGUSR1
	SIGUSR1,
#endif
	0
};

/// The signal handler for SIGALRM sets this to true. It is set back to false
/// once the progress message has been updated.
static volatile sig_atomic_t progress_needs_updating = false;

/// Signal handler for SIGALRM
static void
progress_signal_handler(int sig lzma_attribute((__unused__)))
{
	progress_needs_updating = true;
	return;
}

#else

/// This is true when progress message printing is wanted. Using the same
/// variable name as above to avoid some ifdefs.
static bool progress_needs_updating = false;

/// Elapsed time when the next progress message update should be done.
static uint64_t progress_next_update;

#endif


extern void
message_init(void)
{
	// If --verbose is used, we use a progress indicator if and only
	// if stderr is a terminal. If stderr is not a terminal, we print
	// verbose information only after finishing the file. As a special
	// exception, even if --verbose was not used, user can send SIGALRM
	// to make us print progress information once without automatic
	// updating.
	progress_automatic = is_tty(STDERR_FILENO);

#ifdef SIGALRM
	// Establish the signal handlers which set a flag to tell us that
	// progress info should be updated.
	struct sigaction sa;
	sigemptyset(&sa.sa_mask);
	sa.sa_flags = 0;
	sa.sa_handler = &progress_signal_handler;

	for (size_t i = 0; message_progress_sigs[i] != 0; ++i)
		if (sigaction(message_progress_sigs[i], &sa, NULL))
			message_signal_handler();
#endif

	return;
}


extern void
message_verbosity_increase(void)
{
	if (verbosity < V_DEBUG)
		++verbosity;

	return;
}


extern void
message_verbosity_decrease(void)
{
	if (verbosity > V_SILENT)
		--verbosity;

	return;
}


extern enum message_verbosity
message_verbosity_get(void)
{
	return verbosity;
}


extern void
message_set_files(unsigned int files)
{
	files_total = files;
	return;
}


/// Prints the name of the current file if it hasn't been printed already,
/// except if we are processing exactly one stream from stdin to stdout.
/// I think it looks nicer to not print "(stdin)" when --verbose is used
/// in a pipe and no other files are processed.
static void
print_filename(void)
{
	if (!opt_robot && (files_total != 1 || filename != stdin_filename)) {
		signals_block();

		FILE *file = opt_mode == MODE_LIST ? stdout : stderr;

		// If a file was already processed, put an empty line
		// before the next filename to improve readability.
		if (first_filename_printed)
			fputc('\n', file);

		first_filename_printed = true;
		current_filename_printed = true;

		// If we don't know how many files there will be due
		// to usage of --files or --files0.
		if (files_total == 0)
			fprintf(file, "%s (%u)\n",
					tuklib_mask_nonprint(filename),
					files_pos);
		else
			fprintf(file, "%s (%u/%u)\n",
					tuklib_mask_nonprint(filename),
					files_pos, files_total);

		signals_unblock();
	}

	return;
}


extern void
message_filename(const char *src_name)
{
	// Start numbering the files starting from one.
	++files_pos;
	filename = src_name;

	if (verbosity >= V_VERBOSE
			&& (progress_automatic || opt_mode == MODE_LIST))
		print_filename();
	else
		current_filename_printed = false;

	return;
}


extern void
message_progress_start(lzma_stream *strm, bool is_passthru, uint64_t in_size)
{
	// Store the pointer to the lzma_stream used to do the coding.
	// It is needed to find out the position in the stream.
	progress_strm = strm;
	progress_is_from_passthru = is_passthru;

	// Store the expected size of the file. If we aren't printing any
	// statistics, then is will be unused. But since it is possible
	// that the user sends us a signal to show statistics, we need
	// to have it available anyway.
	expected_in_size = in_size;

	// Indicate that progress info may need to be printed before
	// printing error messages.
	progress_started = true;

	// If progress indicator is wanted, print the filename and possibly
	// the file count now.
	if (verbosity >= V_VERBOSE && progress_automatic) {
		// Start the timer to display the first progress message
		// after one second. An alternative would be to show the
		// first message almost immediately, but delaying by one
		// second looks better to me, since extremely early
		// progress info is pretty much useless.
#ifdef SIGALRM
		// First disable a possibly existing alarm.
		alarm(0);
		progress_needs_updating = false;
		alarm(1);
#else
		progress_needs_updating = true;
		progress_next_update = 1000;
#endif
	}

	return;
}


/// Make the string indicating completion percentage.
static const char *
progress_percentage(uint64_t in_pos)
{
	// If the size of the input file is unknown or the size told us is
	// clearly wrong since we have processed more data than the alleged
	// size of the file, show a static string indicating that we have
	// no idea of the completion percentage.
	if (expected_in_size == 0 || in_pos > expected_in_size)
		return "--- %";

	// Never show 100.0 % before we actually are finished.
	double percentage = (double)(in_pos) / (double)(expected_in_size)
			* 99.9;

	// Use big enough buffer to hold e.g. a multibyte decimal point.
	static char buf[16];
	snprintf(buf, sizeof(buf), "%.1f %%", percentage);

	return buf;
}


/// Make the string containing the amount of input processed, amount of
/// output produced, and the compression ratio.
static const char *
progress_sizes(uint64_t compressed_pos, uint64_t uncompressed_pos, bool final)
{
	// Use big enough buffer to hold e.g. a multibyte thousand separators.
	static char buf[128];
	char *pos = buf;
	size_t left = sizeof(buf);

	// Print the sizes. If this the final message, use more reasonable
	// units than MiB if the file was small.
	const enum nicestr_unit unit_min = final ? NICESTR_B : NICESTR_MIB;
	my_snprintf(&pos, &left, "%s / %s",
			uint64_to_nicestr(compressed_pos,
				unit_min, NICESTR_TIB, false, 0),
			uint64_to_nicestr(uncompressed_pos,
				unit_min, NICESTR_TIB, false, 1));

	// Avoid division by zero. If we cannot calculate the ratio, set
	// it to some nice number greater than 10.0 so that it gets caught
	// in the next if-clause.
	const double ratio = uncompressed_pos > 0
			? (double)(compressed_pos) / (double)(uncompressed_pos)
			: 16.0;

	// If the ratio is very bad, just indicate that it is greater than
	// 9.999. This way the length of the ratio field stays fixed.
	if (ratio > 9.999)
		snprintf(pos, left, " > %.3f", 9.999);
	else
		snprintf(pos, left, " = %.3f", ratio);

	return buf;
}


/// Make the string containing the processing speed of uncompressed data.
static const char *
progress_speed(uint64_t uncompressed_pos, uint64_t elapsed)
{
	// Don't print the speed immediately, since the early values look
	// somewhat random.
	if (elapsed < 3000)
		return "";

	// The first character of KiB/s, MiB/s, or GiB/s:
	static const char unit[] = { 'K', 'M', 'G' };

	size_t unit_index = 0;

	// Calculate the speed as KiB/s.
	double speed = (double)(uncompressed_pos)
			/ ((double)(elapsed) * (1024.0 / 1000.0));

	// Adjust the unit of the speed if needed.
	while (speed > 999.0) {
		speed /= 1024.0;
		if (++unit_index == ARRAY_SIZE(unit))
			return ""; // Way too fast ;-)
	}

	// Use decimal point only if the number is small. Examples:
	//  - 0.1 KiB/s
	//  - 9.9 KiB/s
	//  - 99 KiB/s
	//  - 999 KiB/s
	// Use big enough buffer to hold e.g. a multibyte decimal point.
	static char buf[16];
	snprintf(buf, sizeof(buf), "%.*f %ciB/s",
			speed > 9.9 ? 0 : 1, speed, unit[unit_index]);
	return buf;
}


/// Make a string indicating elapsed time. The format is either
/// M:SS or H:MM:SS depending on if the time is an hour or more.
static const char *
progress_time(uint64_t mseconds)
{
	// 9999 hours = 416 days
	static char buf[sizeof("9999:59:59")];

	// 32-bit variable is enough for elapsed time (136 years).
	uint32_t seconds = (uint32_t)(mseconds / 1000);

	// Don't show anything if the time is zero or ridiculously big.
	if (seconds == 0 || seconds > ((9999 * 60) + 59) * 60 + 59)
		return "";

	uint32_t minutes = seconds / 60;
	seconds %= 60;

	if (minutes >= 60) {
		const uint32_t hours = minutes / 60;
		minutes %= 60;
		snprintf(buf, sizeof(buf),
				"%" PRIu32 ":%02" PRIu32 ":%02" PRIu32,
				hours, minutes, seconds);
	} else {
		snprintf(buf, sizeof(buf), "%" PRIu32 ":%02" PRIu32,
				minutes, seconds);
	}

	return buf;
}


/// Return a string containing estimated remaining time when
/// reasonably possible.
static const char *
progress_remaining(uint64_t in_pos, uint64_t elapsed)
{
	// Don't show the estimated remaining time when it wouldn't
	// make sense:
	//  - Input size is unknown.
	//  - Input has grown bigger since we started (de)compressing.
	//  - We haven't processed much data yet, so estimate would be
	//    too inaccurate.
	//  - Only a few seconds has passed since we started (de)compressing,
	//    so estimate would be too inaccurate.
	if (expected_in_size == 0 || in_pos > expected_in_size
			|| in_pos < (UINT64_C(1) << 19) || elapsed < 8000)
		return "";

	// Calculate the estimate. Don't give an estimate of zero seconds,
	// since it is possible that all the input has been already passed
	// to the library, but there is still quite a bit of output pending.
	uint32_t remaining = (uint32_t)((double)(expected_in_size - in_pos)
			* ((double)(elapsed) / 1000.0) / (double)(in_pos));
	if (remaining < 1)
		remaining = 1;

	static char buf[sizeof("9 h 55 min")];

	// Select appropriate precision for the estimated remaining time.
	if (remaining <= 10) {
		// A maximum of 10 seconds remaining.
		// Show the number of seconds as is.
		snprintf(buf, sizeof(buf), "%" PRIu32 " s", remaining);

	} else if (remaining <= 50) {
		// A maximum of 50 seconds remaining.
		// Round up to the next multiple of five seconds.
		remaining = (remaining + 4) / 5 * 5;
		snprintf(buf, sizeof(buf), "%" PRIu32 " s", remaining);

	} else if (remaining <= 590) {
		// A maximum of 9 minutes and 50 seconds remaining.
		// Round up to the next multiple of ten seconds.
		remaining = (remaining + 9) / 10 * 10;
		snprintf(buf, sizeof(buf), "%" PRIu32 " min %" PRIu32 " s",
				remaining / 60, remaining % 60);

	} else if (remaining <= 59 * 60) {
		// A maximum of 59 minutes remaining.
		// Round up to the next multiple of a minute.
		remaining = (remaining + 59) / 60;
		snprintf(buf, sizeof(buf), "%" PRIu32 " min", remaining);

	} else if (remaining <= 9 * 3600 + 50 * 60) {
		// A maximum of 9 hours and 50 minutes left.
		// Round up to the next multiple of ten minutes.
		remaining = (remaining + 599) / 600 * 10;
		snprintf(buf, sizeof(buf), "%" PRIu32 " h %" PRIu32 " min",
				remaining / 60, remaining % 60);

	} else if (remaining <= 23 * 3600) {
		// A maximum of 23 hours remaining.
		// Round up to the next multiple of an hour.
		remaining = (remaining + 3599) / 3600;
		snprintf(buf, sizeof(buf), "%" PRIu32 " h", remaining);

	} else if (remaining <= 9 * 24 * 3600 + 23 * 3600) {
		// A maximum of 9 days and 23 hours remaining.
		// Round up to the next multiple of an hour.
		remaining = (remaining + 3599) / 3600;
		snprintf(buf, sizeof(buf), "%" PRIu32 " d %" PRIu32 " h",
				remaining / 24, remaining % 24);

	} else if (remaining <= 999 * 24 * 3600) {
		// A maximum of 999 days remaining. ;-)
		// Round up to the next multiple of a day.
		remaining = (remaining + 24 * 3600 - 1) / (24 * 3600);
		snprintf(buf, sizeof(buf), "%" PRIu32 " d", remaining);

	} else {
		// The estimated remaining time is too big. Don't show it.
		return "";
	}

	return buf;
}


/// Get how much uncompressed and compressed data has been processed.
static void
progress_pos(uint64_t *in_pos,
		uint64_t *compressed_pos, uint64_t *uncompressed_pos)
{
	uint64_t out_pos;
	if (progress_is_from_passthru) {
		// In passthru mode the progress info is in total_in/out but
		// the *progress_strm itself isn't initialized and thus we
		// cannot use lzma_get_progress().
		*in_pos = progress_strm->total_in;
		out_pos = progress_strm->total_out;
	} else {
		lzma_get_progress(progress_strm, in_pos, &out_pos);
	}

	// It cannot have processed more input than it has been given.
	assert(*in_pos <= progress_strm->total_in);

	// It cannot have produced more output than it claims to have ready.
	assert(out_pos >= progress_strm->total_out);

	if (opt_mode == MODE_COMPRESS) {
		*compressed_pos = out_pos;
		*uncompressed_pos = *in_pos;
	} else {
		*compressed_pos = *in_pos;
		*uncompressed_pos = out_pos;
	}

	return;
}


extern void
message_progress_update(void)
{
	if (!progress_needs_updating)
		return;

	// Calculate how long we have been processing this file.
	const uint64_t elapsed = mytime_get_elapsed();

#ifndef SIGALRM
	if (progress_next_update > elapsed)
		return;

	progress_next_update = elapsed + 1000;
#endif

	// Get our current position in the stream.
	uint64_t in_pos;
	uint64_t compressed_pos;
	uint64_t uncompressed_pos;
	progress_pos(&in_pos, &compressed_pos, &uncompressed_pos);

	// Block signals so that fprintf() doesn't get interrupted.
	signals_block();

	// Print the filename if it hasn't been printed yet.
	if (!current_filename_printed)
		print_filename();

	// Print the actual progress message. The idea is that there is at
	// least three spaces between the fields in typical situations, but
	// even in rare situations there is at least one space.
	const char *cols[5] = {
		progress_percentage(in_pos),
		progress_sizes(compressed_pos, uncompressed_pos, false),
		progress_speed(uncompressed_pos, elapsed),
		progress_time(elapsed),
		progress_remaining(in_pos, elapsed),
	};
	fprintf(stderr, "\r %*s %*s   %*s %10s   %10s\r",
			tuklib_mbstr_fw(cols[0], 6), cols[0],
			tuklib_mbstr_fw(cols[1], 35), cols[1],
			tuklib_mbstr_fw(cols[2], 9), cols[2],
			cols[3],
			cols[4]);

#ifdef SIGALRM
	// Updating the progress info was finished. Reset
	// progress_needs_updating to wait for the next SIGALRM.
	//
	// NOTE: This has to be done before alarm(1) or with (very) bad
	// luck we could be setting this to false after the alarm has already
	// been triggered.
	progress_needs_updating = false;

	if (verbosity >= V_VERBOSE && progress_automatic) {
		// Mark that the progress indicator is active, so if an error
		// occurs, the error message gets printed cleanly.
		progress_active = true;

		// Restart the timer so that progress_needs_updating gets
		// set to true after about one second.
		alarm(1);
	} else {
		// The progress message was printed because user had sent us
		// SIGALRM. In this case, each progress message is printed
		// on its own line.
		fputc('\n', stderr);
	}
#else
	// When SIGALRM isn't supported and we get here, it's always due to
	// automatic progress update. We set progress_active here too like
	// described above.
	assert(verbosity >= V_VERBOSE);
	assert(progress_automatic);
	progress_active = true;
#endif

	signals_unblock();

	return;
}


static void
progress_flush(bool finished)
{
	if (!progress_started || verbosity < V_VERBOSE)
		return;

	uint64_t in_pos;
	uint64_t compressed_pos;
	uint64_t uncompressed_pos;
	progress_pos(&in_pos, &compressed_pos, &uncompressed_pos);

	// Avoid printing intermediate progress info if some error occurs
	// in the beginning of the stream. (If something goes wrong later in
	// the stream, it is sometimes useful to tell the user where the
	// error approximately occurred, especially if the error occurs
	// after a time-consuming operation.)
	if (!finished && !progress_active
			&& (compressed_pos == 0 || uncompressed_pos == 0))
		return;

	progress_active = false;

	const uint64_t elapsed = mytime_get_elapsed();

	signals_block();

	// When using the auto-updating progress indicator, the final
	// statistics are printed in the same format as the progress
	// indicator itself.
	if (progress_automatic) {
		const char *cols[5] = {
			finished ? "100 %" : progress_percentage(in_pos),
			progress_sizes(compressed_pos, uncompressed_pos, true),
			progress_speed(uncompressed_pos, elapsed),
			progress_time(elapsed),
			finished ? "" : progress_remaining(in_pos, elapsed),
		};
		fprintf(stderr, "\r %*s %*s   %*s %10s   %10s\n",
				tuklib_mbstr_fw(cols[0], 6), cols[0],
				tuklib_mbstr_fw(cols[1], 35), cols[1],
				tuklib_mbstr_fw(cols[2], 9), cols[2],
				cols[3],
				cols[4]);
	} else {
		// The filename is always printed.
		fprintf(stderr, _("%s: "), tuklib_mask_nonprint(filename));

		// Percentage is printed only if we didn't finish yet.
		if (!finished) {
			// Don't print the percentage when it isn't known
			// (starts with a dash).
			const char *percentage = progress_percentage(in_pos);
			if (percentage[0] != '-')
				fprintf(stderr, "%s, ", percentage);
		}

		// Size information is always printed.
		fprintf(stderr, "%s", progress_sizes(
				compressed_pos, uncompressed_pos, true));

		// The speed and elapsed time aren't always shown.
		const char *speed = progress_speed(uncompressed_pos, elapsed);
		if (speed[0] != '\0')
			fprintf(stderr, ", %s", speed);

		const char *elapsed_str = progress_time(elapsed);
		if (elapsed_str[0] != '\0')
			fprintf(stderr, ", %s", elapsed_str);

		fputc('\n', stderr);
	}

	signals_unblock();

	return;
}


extern void
message_progress_end(bool success)
{
	assert(progress_started);
	progress_flush(success);
	progress_started = false;
	return;
}


static void
vmessage(enum message_verbosity v, const char *fmt, va_list ap)
{
	if (v <= verbosity) {
		signals_block();

		progress_flush(false);

		// TRANSLATORS: This is the program name in the beginning
		// of the line in messages. Usually it becomes "xz: ".
		// This is a translatable string because French needs
		// a space before a colon.
		fprintf(stderr, _("%s: "), progname);

#ifdef __clang__
#	pragma GCC diagnostic push
#	pragma GCC diagnostic ignored "-Wformat-nonliteral"
#endif
		vfprintf(stderr, fmt, ap);
#ifdef __clang__
#	pragma GCC diagnostic pop
#endif

		fputc('\n', stderr);

		signals_unblock();
	}

	return;
}


extern void
message(enum message_verbosity v, const char *fmt, ...)
{
	va_list ap;
	va_start(ap, fmt);
	vmessage(v, fmt, ap);
	va_end(ap);
	return;
}


extern void
message_warning(const char *fmt, ...)
{
	va_list ap;
	va_start(ap, fmt);
	vmessage(V_WARNING, fmt, ap);
	va_end(ap);

	set_exit_status(E_WARNING);
	return;
}


extern void
message_error(const char *fmt, ...)
{
	va_list ap;
	va_start(ap, fmt);
	vmessage(V_ERROR, fmt, ap);
	va_end(ap);

	set_exit_status(E_ERROR);
	return;
}


extern void
message_fatal(const char *fmt, ...)
{
	va_list ap;
	va_start(ap, fmt);
	vmessage(V_ERROR, fmt, ap);
	va_end(ap);

	tuklib_exit(E_ERROR, E_ERROR, false);
}


extern void
message_bug(void)
{
	message_fatal(_("Internal error (bug)"));
}


extern void
message_signal_handler(void)
{
	message_fatal(_("Cannot establish signal handlers"));
}


extern const char *
message_strm(lzma_ret code)
{
	switch (code) {
	case LZMA_NO_CHECK:
		return _("No integrity check; not verifying file integrity");

	case LZMA_UNSUPPORTED_CHECK:
		return _("Unsupported type of integrity check; "
				"not verifying file integrity");

	case LZMA_MEM_ERROR:
		return strerror(ENOMEM);

	case LZMA_MEMLIMIT_ERROR:
		return _("Memory usage limit reached");

	case LZMA_FORMAT_ERROR:
		return _("File format not recognized");

	case LZMA_OPTIONS_ERROR:
		return _("Unsupported options");

	case LZMA_DATA_ERROR:
		return _("Compressed data is corrupt");

	case LZMA_BUF_ERROR:
		return _("Unexpected end of input");

	case LZMA_OK:
	case LZMA_STREAM_END:
	case LZMA_GET_CHECK:
	case LZMA_PROG_ERROR:
	case LZMA_SEEK_NEEDED:
	case LZMA_RET_INTERNAL1:
	case LZMA_RET_INTERNAL2:
	case LZMA_RET_INTERNAL3:
	case LZMA_RET_INTERNAL4:
	case LZMA_RET_INTERNAL5:
	case LZMA_RET_INTERNAL6:
	case LZMA_RET_INTERNAL7:
	case LZMA_RET_INTERNAL8:
		// Without "default", compiler will warn if new constants
		// are added to lzma_ret, it is not too easy to forget to
		// add the new constants to this function.
		break;
	}

	return _("Internal error (bug)");
}


extern void
message_mem_needed(enum message_verbosity v, uint64_t memusage)
{
	if (v > verbosity)
		return;

	// Convert memusage to MiB, rounding up to the next full MiB.
	// This way the user can always use the displayed usage as
	// the new memory usage limit. (If we rounded to the nearest,
	// the user might need to +1 MiB to get high enough limit.)
	memusage = round_up_to_mib(memusage);

	uint64_t memlimit = hardware_memlimit_get(opt_mode);

	// Handle the case when there is no memory usage limit.
	// This way we don't print a weird message with a huge number.
	if (memlimit == UINT64_MAX) {
		message(v, _("%s MiB of memory is required. "
				"The limiter is disabled."),
				uint64_to_str(memusage, 0));
		return;
	}

	// With US-ASCII:
	// 2^64 with thousand separators + " MiB" suffix + '\0' = 26 + 4 + 1
	// But there may be multibyte chars so reserve enough space.
	char memlimitstr[128];

	// Show the memory usage limit as MiB unless it is less than 1 MiB.
	// This way it's easy to notice errors where one has typed
	// --memory=123 instead of --memory=123MiB.
	if (memlimit < (UINT32_C(1) << 20)) {
		snprintf(memlimitstr, sizeof(memlimitstr), "%s B",
				uint64_to_str(memlimit, 1));
	} else {
		// Round up just like with memusage. If this function is
		// called for informational purposes (to just show the
		// current usage and limit), we should never show that
		// the usage is higher than the limit, which would give
		// a false impression that the memory usage limit isn't
		// properly enforced.
		snprintf(memlimitstr, sizeof(memlimitstr), "%s MiB",
				uint64_to_str(round_up_to_mib(memlimit), 1));
	}

	message(v, _("%s MiB of memory is required. The limit is %s."),
			uint64_to_str(memusage, 0), memlimitstr);

	return;
}


extern void
message_filters_show(enum message_verbosity v, const lzma_filter *filters)
{
	if (v > verbosity)
		return;

	char *buf;
	const lzma_ret ret = lzma_str_from_filters(&buf, filters,
			LZMA_STR_ENCODER | LZMA_STR_GETOPT_LONG, NULL);
	if (ret != LZMA_OK)
		message_fatal("%s", message_strm(ret));

	fprintf(stderr, _("%s: Filter chain: %s\n"), progname, buf);
	free(buf);
	return;
}


extern void
message_try_help(void)
{
	// Print this with V_WARNING instead of V_ERROR to prevent it from
	// showing up when --quiet has been specified.
	message(V_WARNING, _("Try '%s --help' for more information."),
			progname);
	return;
}


extern void
message_version(void)
{
	// It is possible that liblzma version is different than the command
	// line tool version, so print both.
	if (opt_robot) {
		printf("XZ_VERSION=%" PRIu32 "\nLIBLZMA_VERSION=%" PRIu32 "\n",
				LZMA_VERSION, lzma_version_number());
	} else {
		printf("xz (" PACKAGE_NAME ") " LZMA_VERSION_STRING "\n");
		printf("liblzma %s\n", lzma_version_string());
	}

	tuklib_exit(E_SUCCESS, E_ERROR, verbosity != V_SILENT);
}


static void
detect_wrapping_errors(int error_mask)
{
#ifndef NDEBUG
	// This might help in catching problematic strings in translations.
	// It's a debug message so don't translate this.
	if (error_mask & TUKLIB_WRAP_WARN_OVERLONG)
		message_fatal("The help text contains overlong lines");
#endif

	if (error_mask & ~TUKLIB_WRAP_WARN_OVERLONG)
		message_fatal(_("Error printing the help text "
				"(error code %d)"), error_mask);

	return;
}


extern void
message_help(bool long_help)
{
	static const struct tuklib_wrap_opt wrap0 = {  0,  0,  0,  0, 79 };
	static const struct tuklib_wrap_opt wrap1 = {  1,  1,  1,  1, 79 };
	static const struct tuklib_wrap_opt wrap2 = {  2,  2, 22, 22, 79 };
	static const struct tuklib_wrap_opt wrap3 = { 24, 24, 36, 36, 79 };

	// Accumulated error codes from tuklib_wraps() and tuklib_wrapf()
	int e = 0;

	printf(_("Usage: %s [OPTION]... [FILE]...\n"), progname);
	e |= tuklib_wraps(stdout, &wrap0,
			W_("Compress or decompress FILEs in the .xz format."));
	putchar('\n');

	e |= tuklib_wraps(stdout, &wrap0,
			W_("Mandatory arguments to long options are "
			"mandatory for short options too."));
	putchar('\n');

	if (long_help) {
		e |= tuklib_wraps(stdout, &wrap1, W_("Operation mode:"));
		putchar('\n');
	}

	e |= tuklib_wrapf(stdout, &wrap2,
			"-z, --compress\v%s\r"
			"-d, --decompress\v%s\r"
			"-t, --test\v%s\r"
			"-l, --list\v%s",
			W_("force compression"),
			W_("force decompression"),
			W_("test compressed file integrity"),
			W_("list information about .xz files"));

	if (long_help) {
		putchar('\n');
		e |= tuklib_wraps(stdout, &wrap1, W_("Operation modifiers:"));
		putchar('\n');
	}

	e |= tuklib_wrapf(stdout, &wrap2,
		"-k, --keep\v%s\r"
		"-f, --force\v%s\r"
		"-c, --stdout\v%s",
		W_("keep (don't delete) input files"),
		W_("force overwrite of output file and (de)compress links"),
		W_("write to standard output and don't delete input files"));
	// NOTE: --to-stdout isn't included above because it's not
	// the recommended spelling. It was copied from gzip but other
	// compressors with gzip-like syntax don't support it.

	if (long_help) {
		e |= tuklib_wrapf(stdout, &wrap2,
			"    --no-sync\v%s\r"
			"    --single-stream\v%s\r"
			"    --no-sparse\v%s\r"
			"-S, --suffix=%s\v%s\r"
			"    --files[=%s]\v%s\r"
			"    --files0[=%s]\v%s\r",
			W_("don't synchronize the output file to the storage "
				"device before removing the input file"),
			W_("decompress only the first stream, and silently "
				"ignore possible remaining input data"),
			W_("do not create sparse files when decompressing"),
			_(".SUF"),
			W_("use the suffix '.SUF' on compressed files"),
			_("FILE"),
			W_("read filenames to process from FILE; "
				"if FILE is omitted, "
				"filenames are read from the standard input; "
				"filenames must be terminated with "
				"the newline character"),
			_("FILE"),
			W_("like --files but use the null character as "
				"terminator"));

		e |= tuklib_wraps(stdout, &wrap1,
			W_("Basic file format and compression options:"));

		e |= tuklib_wrapf(stdout, &wrap2,
			"\n"
			"-F, --format=%s\v%s\r"
			"-C, --check=%s\v%s\r"
			"    --ignore-check\v%s",
			_("FORMAT"),
			W_("file format to encode or decode; possible values "
				"are 'auto' (default), 'xz', 'lzma', 'lzip', "
				"and 'raw'"),
			_("NAME"),
			W_("integrity check type: 'none' (use with caution), "
				"'crc32', 'crc64' (default), or 'sha256'"),
			W_("don't verify the integrity check when "
				"decompressing"));
	}

	e |= tuklib_wrapf(stdout, &wrap2,
		"-0 ... -9\v%s\r"
		"-e, --extreme\v%s\r"
		"-T, --threads=%s\v%s",
		W_("compression preset; default is 6; take compressor *and* "
			"decompressor memory usage into account before "
			"using 7-9!"),
		W_("try to improve compression ratio by using more CPU time; "
			"does not affect decompressor memory requirements"),
		// TRANSLATORS: Short for NUMBER. A longer string is fine but
		// wider than 5 columns makes --long-help a few lines longer.
		_("NUM"),
		W_("use at most NUM threads; the default is 0 which uses "
			"as many threads as there are processor cores"));

	if (long_help) {
		e |= tuklib_wrapf(stdout, &wrap2,
			"    --block-size=%s\v%s\r"
			"    --block-list=%s\v%s\r"
			"    --flush-timeout=%s\v%s",
			_("SIZE"),
			W_("start a new .xz block after every SIZE bytes "
				"of input; use this to set the block size "
				"for threaded compression"),
			_("BLOCKS"),
			W_("start a new .xz block after the given "
				"comma-separated intervals of uncompressed "
				"data; optionally, specify a "
				"filter chain number (0-9) followed by "
				"a ':' before the uncompressed data size"),
			_("NUM"),
			W_("when compressing, if more than NUM "
				"milliseconds has passed since the previous "
				"flush and reading more input would block, "
				"all pending data is flushed out"));

		e |= tuklib_wrapf(stdout, &wrap2,
			"    --memlimit-compress=%s\n"
			"    --memlimit-decompress=%s\n"
			"    --memlimit-mt-decompress=%s\n"
			"-M, --memlimit=%s\v%s\r"
			"    --no-adjust\v%s",
			_("LIMIT"),
			_("LIMIT"),
			_("LIMIT"),
			_("LIMIT"),
			// xgettext:no-c-format
			W_("set memory usage limit for compression, "
				"decompression, threaded decompression, "
				"or all of these; LIMIT is in "
				"bytes, % of RAM, or 0 for defaults"),
			W_("if compression settings exceed the "
				"memory usage limit, "
				"give an error instead of adjusting "
				"the settings downwards"));
	}

	if (long_help) {
		putchar('\n');

		e |= tuklib_wraps(stdout, &wrap1,
			W_("Custom filter chain for compression "
				"(an alternative to using presets):"));

		e |= tuklib_wrapf(stdout, &wrap2,
			"\n"
			"--filters=%s\v%s\r"
			"--filters1=%s ... --filters9=%s\v%s\r"
			"--filters-help\v%s",
			_("FILTERS"),
			W_("set the filter chain using the "
				"liblzma filter string syntax; "
				"use --filters-help for more information"),
			_("FILTERS"),
			_("FILTERS"),
			W_("set additional filter chains using the "
				"liblzma filter string syntax to use "
				"with --block-list"),
			W_("display more information about the "
				"liblzma filter string syntax and exit"));

#if defined(HAVE_ENCODER_LZMA1) || defined(HAVE_DECODER_LZMA1) \
		|| defined(HAVE_ENCODER_LZMA2) || defined(HAVE_DECODER_LZMA2)
		e |= tuklib_wrapf(stdout, &wrap2,
			"\n"
			"--lzma1[=%s]\n"
			"--lzma2[=%s]\v%s",
			// TRANSLATORS: Short for OPTIONS.
			_("OPTS"),
			_("OPTS"),
			// TRANSLATORS: Use semicolon (or its fullwidth form)
			// in "(valid values; default)" even if it is weird in
			// your language. There are non-translatable strings
			// that look like "(foo, bar, baz; foo)" which list
			// the supported values and the default value.
			W_("LZMA1 or LZMA2; OPTS is a comma-separated list "
				"of zero or more of the following options "
				"(valid values; default):"));

		e |= tuklib_wrapf(stdout, &wrap3,
			"preset=%s\v%s (0-9[e])\r"
			"dict=%s\v%s \b(4KiB - 1536MiB; 8MiB)\b\r"
			"lc=%s\v%s \b(0-4; 3)\b\r"
			"lp=%s\v%s \b(0-4; 0)\b\r"
			"pb=%s\v%s \b(0-4; 2)\b\r"
			"mode=%s\v%s (fast, normal; normal)\r"
			"nice=%s\v%s \b(2-273; 64)\b\r"
			"mf=%s\v%s (hc3, hc4, bt2, bt3, bt4; bt4)\r"
			"depth=%s\v%s",
			// TRANSLATORS: Short for PRESET. A longer string is
			// fine but wider than 4 columns makes --long-help
			// one line longer.
			_("PRE"),
			W_("reset options to a preset"),
			_("NUM"), W_("dictionary size"),
			_("NUM"),
			// TRANSLATORS: The word "literal" in "literal context
			// bits" means how many "context bits" to use when
			// encoding literals. A literal is a single 8-bit
			// byte. It doesn't mean "literally" here.
			W_("number of literal context bits"),
			_("NUM"), W_("number of literal position bits"),
			_("NUM"), W_("number of position bits"),
			_("MODE"), W_("compression mode"),
			_("NUM"), W_("nice length of a match"),
			_("NAME"), W_("match finder"),
			_("NUM"), W_("maximum search depth; "
				"0=automatic (default)"));
#endif

		e |= tuklib_wrapf(stdout, &wrap2,
			"\n"
			"--x86[=%s]\v%s\r"
			"--arm[=%s]\v%s\r"
			"--armthumb[=%s]\v%s\r"
			"--arm64[=%s]\v%s\r"
			"--powerpc[=%s]\v%s\r"
			"--ia64[=%s]\v%s\r"
			"--sparc[=%s]\v%s\r"
			"--riscv[=%s]\v%s\r"
			"\v%s",
			_("OPTS"),
			W_("x86 BCJ filter (32-bit and 64-bit)"),
			_("OPTS"),
			W_("ARM BCJ filter"),
			_("OPTS"),
			W_("ARM-Thumb BCJ filter"),
			_("OPTS"),
			W_("ARM64 BCJ filter"),
			_("OPTS"),
			W_("PowerPC BCJ filter (big endian only)"),
			_("OPTS"),
			W_("IA-64 (Itanium) BCJ filter"),
			_("OPTS"),
			W_("SPARC BCJ filter"),
			_("OPTS"),
			W_("RISC-V BCJ filter"),
			W_("Valid OPTS for all BCJ filters:"));
		e |= tuklib_wrapf(stdout, &wrap3,
			"start=%s\v%s",
			_("NUM"),
			W_("start offset for conversions (default=0)"));

#if defined(HAVE_ENCODER_DELTA) || defined(HAVE_DECODER_DELTA)
		e |= tuklib_wrapf(stdout, &wrap2,
			"\n"
			"--delta[=%s]\v%s",
			_("OPTS"),
			W_("Delta filter; valid OPTS "
				"(valid values; default):"));
		e |= tuklib_wrapf(stdout, &wrap3,
			"dist=%s\v%s \b(1-256; 1)\b",
			_("NUM"),
			W_("distance between bytes being subtracted "
				"from each other"));
#endif
	}

	if (long_help) {
		putchar('\n');
		e |= tuklib_wraps(stdout, &wrap1, W_("Other options:"));
		putchar('\n');
	}

	e |= tuklib_wrapf(stdout, &wrap2,
		"-q, --quiet\v%s\r"
		"-v, --verbose\v%s",
		W_("suppress warnings; specify twice to suppress errors too"),
		W_("be verbose; specify twice for even more verbose"));

	if (long_help) {
		e |= tuklib_wrapf(stdout, &wrap2,
		"-Q, --no-warn\v%s\r"
		"    --robot\v%s\r"
		"\n"
		"    --info-memory\v%s\r"
		"-h, --help\v%s\r"
		"-H, --long-help\v%s",
		W_("make warnings not affect the exit status"),
		W_("use machine-parsable messages (useful for scripts)"),
		W_("display the total amount of RAM and the currently active "
			"memory usage limits, and exit"),
		W_("display the short help (lists only the basic options)"),
		W_("display this long help and exit"));
	} else {
		e |= tuklib_wrapf(stdout, &wrap2,
		"-h, --help\v%s\r"
		"-H, --long-help\v%s",
		W_("display this short help and exit"),
		W_("display the long help (lists also the advanced options)"));
	}

	e |= tuklib_wrapf(stdout, &wrap2, "-V, --version\v%s",
			W_("display the version number and exit"));

	putchar('\n');
	e |= tuklib_wraps(stdout, &wrap0,
		W_("With no FILE, or when FILE is -, read standard input."));
	putchar('\n');

	e |= tuklib_wrapf(stdout, &wrap0,
		// TRANSLATORS: This message indicates the bug reporting
		// address for this package. Please add another line saying
		// "\nReport translation bugs to <...>." with the email or WWW
		// address for translation bugs. Thanks!
		W_("Report bugs to <%s> (in English or Finnish)."),
		PACKAGE_BUGREPORT);

	e |= tuklib_wrapf(stdout, &wrap0,
		// TRANSLATORS: The first %s is the name of this software.
		// The second <%s> is an URL.
		W_("%s home page: <%s>"), PACKAGE_NAME, PACKAGE_URL);

#if LZMA_VERSION_STABILITY != LZMA_VERSION_STABILITY_STABLE
	e |= tuklib_wraps(stdout, &wrap0, W_(
"THIS IS A DEVELOPMENT VERSION NOT INTENDED FOR PRODUCTION USE."));
#endif

	detect_wrapping_errors(e);
	tuklib_exit(E_SUCCESS, E_ERROR, verbosity != V_SILENT);
}


extern void
message_filters_help(void)
{
	static const struct tuklib_wrap_opt wrap = { .right_margin = 76 };

	char *encoder_options;
	if (lzma_str_list_filters(&encoder_options, LZMA_VLI_UNKNOWN,
			LZMA_STR_ENCODER, NULL) != LZMA_OK)
		message_bug();

	if (!opt_robot) {
		int e = tuklib_wrapf(stdout, &wrap,
W_("Filter chains are set using the --filters=FILTERS or "
"--filters1=FILTERS ... --filters9=FILTERS options. "
"Each filter in the chain can be separated by spaces or '--'. "
"Alternatively a preset %s can be specified instead of a filter chain."),
				"<0-9>[e]");
		putchar('\n');
		e |= tuklib_wraps(stdout, &wrap,
			W_("The supported filters and their options are:"));

		detect_wrapping_errors(e);
	}

	puts(encoder_options);

	tuklib_exit(E_SUCCESS, E_ERROR, verbosity != V_SILENT);
}
