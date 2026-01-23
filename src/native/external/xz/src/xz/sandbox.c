// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       sandbox.c
/// \brief      Sandbox support
///
/// \note       When sandbox_init() is called, gettext hasn't been
///             initialized yet, and thus wrapping error messages
///             in _("...") is pointless in that function. In other
///             functions gettext can be used, but the only error message
///             we have is "Failed to enable the sandbox" which should
///             (almost) never occur. If it does occur anyway, leaving
///             the message untranslated can make it easier to find
///             bug reports about the issue.
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#include "private.h"


#ifndef ENABLE_SANDBOX

// Prevent an empty translation unit when no sandboxing is supported.
typedef int dummy;

#else

/// If the conditions for strict sandboxing (described in main())
/// have been met, sandbox_allow_strict() can be called to set this
/// variable to true.
static bool strict_sandbox_allowed = false;


extern void
sandbox_allow_strict(void)
{
	strict_sandbox_allowed = true;
	return;
}


// Strict sandboxing prevents opening any files. This *tries* to ensure
// that any auxiliary files that might be required are already open.
//
// Returns true if strict sandboxing is allowed, false otherwise.
static bool
prepare_for_strict_sandbox(void)
{
	if (!strict_sandbox_allowed)
		return false;

	const char dummy_str[] = "x";

	// Try to ensure that both libc and xz locale files have been
	// loaded when NLS is enabled.
	snprintf(NULL, 0, "%s%s", _(dummy_str), strerror(EINVAL));

	// Try to ensure that iconv data files needed for handling multibyte
	// characters have been loaded. This is needed at least with glibc.
	tuklib_mbstr_width(dummy_str, NULL);

	return true;
}

#endif


#if defined(HAVE_PLEDGE)

///////////////
// pledge(2) //
///////////////

#include <unistd.h>


extern void
sandbox_init(void)
{
	if (pledge("stdio rpath wpath cpath fattr", ""))
		message_fatal("Failed to enable the sandbox");

	return;
}


extern void
sandbox_enable_read_only(void)
{
	// We will be opening files for reading but
	// won't create or remove any files.
	if (pledge("stdio rpath", ""))
		message_fatal("Failed to enable the sandbox");

	return;
}


extern void
sandbox_enable_strict_if_allowed(int src_fd lzma_attribute((__unused__)),
		int pipe_event_fd lzma_attribute((__unused__)),
		int pipe_write_fd lzma_attribute((__unused__)))
{
	if (!prepare_for_strict_sandbox())
		return;

	// All files that need to be opened have already been opened.
	if (pledge("stdio", ""))
		message_fatal("Failed to enable the sandbox");

	return;
}


#elif defined(HAVE_LINUX_LANDLOCK)

//////////////
// Landlock //
//////////////

#include "my_landlock.h"


// The required_rights should have those bits set that must not be restricted.
// This function will then bitwise-and ~required_rights with a mask matching
// the Landlock ABI version, leaving only those bits set that are supported
// by the ABI and allowed to be restricted by the function argument.
static void
enable_landlock(uint64_t required_rights)
{
	// Initialize the ruleset to forbid all actions that the available
	// Landlock ABI version supports. Return if Landlock isn't supported
	// at all.
	struct landlock_ruleset_attr attr;
	if (my_landlock_ruleset_attr_forbid_all(&attr) == -1)
		return;

	// Allow the required rights.
	attr.handled_access_fs &= ~required_rights;

	// Create the ruleset in the kernel. This shouldn't fail.
	const int ruleset_fd = my_landlock_create_ruleset(
			&attr, sizeof(attr), 0);
	if (ruleset_fd < 0)
		message_fatal("Failed to enable the sandbox");

	// All files we need should have already been opened. Thus,
	// we don't need to add any rules using landlock_add_rule(2)
	// before activating the sandbox.
	//
	// NOTE: It's possible that the hack prepare_for_strict_sandbox()
	// isn't be good enough. It tries to get translations and
	// libc-specific files loaded but if it's not good enough
	// then perhaps a Landlock rule to allow reading from /usr
	// and/or the xz installation prefix would be needed.
	//
	// prctl(PR_SET_NO_NEW_PRIVS, ...) was already called in
	// sandbox_init() so we don't do it here again.
	if (my_landlock_restrict_self(ruleset_fd, 0) != 0)
		message_fatal("Failed to enable the sandbox");

	(void)close(ruleset_fd);
	return;
}


extern void
sandbox_init(void)
{
	// Prevent the process from gaining new privileges. This must be done
	// before landlock_restrict_self(2) but since we will never need new
	// privileges, this call can be done here already.
	//
	// This is supported since Linux 3.5. Ignore the return value to
	// keep compatibility with old kernels. landlock_restrict_self(2)
	// will fail if the no_new_privs attribute isn't set, thus if prctl()
	// fails here the error will still be detected when it matters.
	(void)prctl(PR_SET_NO_NEW_PRIVS, 1, 0, 0, 0);

	// These are all in ABI version 1 already. We don't need truncate
	// rights because files are created with open() using O_EXCL and
	// without O_TRUNC.
	//
	// LANDLOCK_ACCESS_FS_READ_DIR is required to synchronize the
	// directory before removing the source file.
	//
	// LANDLOCK_ACCESS_FS_READ_DIR is also helpful to show a clear error
	// message if xz is given a directory name. Without this permission
	// the message would be "Permission denied" but with this permission
	// it's "Is a directory, skipping". It could be worked around with
	// stat()/lstat() but just giving this permission is simpler and
	// shouldn't make the sandbox much weaker in practice.
	const uint64_t required_rights
			= LANDLOCK_ACCESS_FS_WRITE_FILE
			| LANDLOCK_ACCESS_FS_READ_FILE
			| LANDLOCK_ACCESS_FS_READ_DIR
			| LANDLOCK_ACCESS_FS_REMOVE_FILE
			| LANDLOCK_ACCESS_FS_MAKE_REG;

	enable_landlock(required_rights);
	return;
}


extern void
sandbox_enable_read_only(void)
{
	// We will be opening files for reading but
	// won't create or remove any files.
	const uint64_t required_rights
			= LANDLOCK_ACCESS_FS_READ_FILE
			| LANDLOCK_ACCESS_FS_READ_DIR;
	enable_landlock(required_rights);
	return;
}


extern void
sandbox_enable_strict_if_allowed(int src_fd lzma_attribute((__unused__)),
		int pipe_event_fd lzma_attribute((__unused__)),
		int pipe_write_fd lzma_attribute((__unused__)))
{
	if (!prepare_for_strict_sandbox())
		return;

	// Allow all restrictions that the kernel supports with the
	// highest Landlock ABI version that the kernel or xz supports.
	//
	// NOTE: LANDLOCK_ACCESS_FS_READ_DIR isn't needed here because
	// the only input file has already been opened.
	enable_landlock(0);
	return;
}


#elif defined(HAVE_CAP_RIGHTS_LIMIT)

//////////////
// Capsicum //
//////////////

#include <sys/capsicum.h>


extern void
sandbox_init(void)
{
	// Nothing to do.
	return;
}


extern void
sandbox_enable_read_only(void)
{
	// Nothing to do.
	return;
}


extern void
sandbox_enable_strict_if_allowed(
		int src_fd, int pipe_event_fd, int pipe_write_fd)
{
	if (!prepare_for_strict_sandbox())
		return;

	// Capsicum needs FreeBSD 10.2 or later.
	cap_rights_t rights;

	if (cap_enter())
		goto error;

	if (cap_rights_limit(src_fd, cap_rights_init(&rights,
			CAP_EVENT, CAP_FCNTL, CAP_LOOKUP, CAP_READ, CAP_SEEK)))
		goto error;

	// If not reading from stdin, remove all capabilities from it.
	if (src_fd != STDIN_FILENO && cap_rights_limit(
			STDIN_FILENO, cap_rights_init(&rights)))
		goto error;

	if (cap_rights_limit(STDOUT_FILENO, cap_rights_init(&rights,
			CAP_EVENT, CAP_FCNTL, CAP_FSTAT, CAP_LOOKUP,
			CAP_WRITE, CAP_SEEK)))
		goto error;

	if (cap_rights_limit(STDERR_FILENO, cap_rights_init(&rights,
			CAP_WRITE)))
		goto error;

	if (cap_rights_limit(pipe_event_fd, cap_rights_init(&rights,
			CAP_EVENT)))
		goto error;

	if (cap_rights_limit(pipe_write_fd, cap_rights_init(&rights,
			CAP_WRITE)))
		goto error;

	return;

error:
	// If a kernel is configured without capability mode support or
	// used in an emulator that does not implement the capability
	// system calls, then the Capsicum system calls will fail and set
	// errno to ENOSYS. In that case xz will silently run without
	// the sandbox.
	if (errno == ENOSYS)
		return;

	message_fatal("Failed to enable the sandbox");
}

#endif
