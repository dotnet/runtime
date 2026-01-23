// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       private.h
/// \brief      Common includes, definitions, and prototypes
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#include "sysdefs.h"
#include "mythread.h"

#include "lzma.h"

#include <sys/types.h>
#include <sys/stat.h>
#include <errno.h>
#include <signal.h>
#include <locale.h>
#include <stdio.h>

#ifndef _MSC_VER
#	include <unistd.h>
#endif

#include "tuklib_gettext.h"
#include "tuklib_progname.h"
#include "tuklib_exit.h"
#include "tuklib_mbstr_nonprint.h"
#include "tuklib_mbstr.h"

#if defined(_WIN32) && !defined(__CYGWIN__)
#	define WIN32_LEAN_AND_MEAN
#	include <windows.h>
#endif

#ifdef _MSC_VER
#	define fileno _fileno
#endif

#ifndef STDIN_FILENO
#	define STDIN_FILENO (fileno(stdin))
#endif

#ifndef STDOUT_FILENO
#	define STDOUT_FILENO (fileno(stdout))
#endif

#ifndef STDERR_FILENO
#	define STDERR_FILENO (fileno(stderr))
#endif

// Handling SIGTSTP keeps time-keeping for progress indicator correct
// if xz is stopped. It requires use of clock_gettime() as that is
// async-signal safe in POSIX. Require also SIGALRM support since
// on systems where SIGALRM isn't available, progress indicator code
// polls the time and the SIGTSTP handling adds slight overhead to
// that code. Most (all?) systems that have SIGTSTP also have SIGALRM
// so this requirement won't exclude many systems.
#if defined(HAVE_CLOCK_GETTIME) && defined(SIGTSTP) && defined(SIGALRM)
#	define USE_SIGTSTP_HANDLER 1
#endif

#include "main.h"
#include "mytime.h"
#include "coder.h"
#include "message.h"
#include "args.h"
#include "hardware.h"
#include "file_io.h"
#include "options.h"
#include "sandbox.h"
#include "signals.h"
#include "suffix.h"
#include "util.h"

#ifdef HAVE_DECODERS
#	include "list.h"
#endif
