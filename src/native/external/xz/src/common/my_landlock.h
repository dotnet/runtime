// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       my_landlock.h
/// \brief      Linux Landlock sandbox helper functions
///
/// \note       This uses static variables to cache the Landlock ABI version.
///             Only one file in an application should include this header.
///             Only one thread should call these functions.
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef MY_LANDLOCK_H
#define MY_LANDLOCK_H

#include "sysdefs.h"

#include <linux/landlock.h>
#include <sys/syscall.h>
#include <sys/prctl.h>
#include <sys/utsname.h>


/// \brief      Initialize Landlock ruleset attributes to forbid everything
///
/// The supported Landlock ABI is checked at runtime and only the supported
/// actions are forbidden in the attributes. Thus, if the attributes are
/// used with my_landlock_create_ruleset(), it shouldn't fail.
///
/// \return     On success, the Landlock ABI version is returned (a positive
///             integer). If Landlock isn't supported, -1 is returned.
static int
my_landlock_ruleset_attr_forbid_all(struct landlock_ruleset_attr *attr)
{
	memzero(attr, sizeof(*attr));

	// Cache the Landlock ABI version:
	//  0 = not checked yet
	// -1 = Landlock not supported
	// >0 = Landlock ABI version
	static int abi_version = 0;

#ifdef LANDLOCK_SCOPE_SIGNAL
	// Red Hat Enterprise Linux 9 kernel since 5.14.0-603.el9 (2025-07-30)
	// claims ABI version 6 support, but as of 5.14.0-643.el9 (2025-11-22)
	// it lacks LANDLOCK_SCOPE_SIGNAL. ABI version 6 was added in upstream
	// Linux 6.12 while RHEL 9 has Linux 5.14 with lots of backports.
	// We assume that any kernel version 5.14 with ABI version 6 is buggy.
	static bool is_rhel9 = false;
#endif

	if (abi_version == 0) {
		abi_version = syscall(SYS_landlock_create_ruleset,
			(void *)NULL, 0, LANDLOCK_CREATE_RULESET_VERSION);

#ifdef LANDLOCK_SCOPE_SIGNAL
		if (abi_version == 6) {
			static const char rel[] = "5.14.";
			const size_t rel_len = sizeof(rel) - 1;

			struct utsname un;
			if (uname(&un) == 0 && strncmp(
					un.release, rel, rel_len) == 0)
				is_rhel9 = true;
		}
#endif
	}

	if (abi_version <= 0)
		return -1;

	// ABI 1 except the few at the end
	attr->handled_access_fs
			= LANDLOCK_ACCESS_FS_EXECUTE
			| LANDLOCK_ACCESS_FS_WRITE_FILE
			| LANDLOCK_ACCESS_FS_READ_FILE
			| LANDLOCK_ACCESS_FS_READ_DIR
			| LANDLOCK_ACCESS_FS_REMOVE_DIR
			| LANDLOCK_ACCESS_FS_REMOVE_FILE
			| LANDLOCK_ACCESS_FS_MAKE_CHAR
			| LANDLOCK_ACCESS_FS_MAKE_DIR
			| LANDLOCK_ACCESS_FS_MAKE_REG
			| LANDLOCK_ACCESS_FS_MAKE_SOCK
			| LANDLOCK_ACCESS_FS_MAKE_FIFO
			| LANDLOCK_ACCESS_FS_MAKE_BLOCK
			| LANDLOCK_ACCESS_FS_MAKE_SYM
#ifdef LANDLOCK_ACCESS_FS_REFER
			| LANDLOCK_ACCESS_FS_REFER // ABI 2
#endif
#ifdef LANDLOCK_ACCESS_FS_TRUNCATE
			| LANDLOCK_ACCESS_FS_TRUNCATE // ABI 3
#endif
#ifdef LANDLOCK_ACCESS_FS_IOCTL_DEV
			| LANDLOCK_ACCESS_FS_IOCTL_DEV // ABI 5
#endif
			;

#ifdef LANDLOCK_ACCESS_NET_BIND_TCP
	// ABI 4
	attr->handled_access_net
			= LANDLOCK_ACCESS_NET_BIND_TCP
			| LANDLOCK_ACCESS_NET_CONNECT_TCP;
#endif

#ifdef LANDLOCK_SCOPE_SIGNAL
	 // ABI 6
	 attr->scoped
			 = LANDLOCK_SCOPE_ABSTRACT_UNIX_SOCKET
			 | LANDLOCK_SCOPE_SIGNAL;
#endif

	// Disable flags that require a new ABI version.
	switch (abi_version) {
	case 1:
#ifdef LANDLOCK_ACCESS_FS_REFER
		attr->handled_access_fs &= ~LANDLOCK_ACCESS_FS_REFER;
#endif
		FALLTHROUGH;

	case 2:
#ifdef LANDLOCK_ACCESS_FS_TRUNCATE
		attr->handled_access_fs &= ~LANDLOCK_ACCESS_FS_TRUNCATE;
#endif
		FALLTHROUGH;

	case 3:
#ifdef LANDLOCK_ACCESS_NET_BIND_TCP
		attr->handled_access_net = 0;
#endif
		FALLTHROUGH;

	case 4:
#ifdef LANDLOCK_ACCESS_FS_IOCTL_DEV
		attr->handled_access_fs &= ~LANDLOCK_ACCESS_FS_IOCTL_DEV;
#endif
		FALLTHROUGH;

	case 5:
#ifdef LANDLOCK_SCOPE_SIGNAL
		attr->scoped = 0;
#endif
		FALLTHROUGH;

	case 6:
#ifdef LANDLOCK_SCOPE_SIGNAL
		if (is_rhel9)
			attr->scoped &= ~LANDLOCK_SCOPE_SIGNAL;
#endif

		FALLTHROUGH;

	default:
		// We only know about the features of the ABIs 1-6.
		break;
	}

	return abi_version;
}


/// \brief      Wrapper for the landlock_create_ruleset(2) syscall
///
/// Syscall wrappers provide argument type checking.
///
/// \note       Remember to call `prctl(PR_SET_NO_NEW_PRIVS, 1, 0, 0, 0)` too!
static inline int
my_landlock_create_ruleset(const struct landlock_ruleset_attr *attr,
		size_t size, uint32_t flags)
{
	return syscall(SYS_landlock_create_ruleset, attr, size, flags);
}


/// \brief      Wrapper for the landlock_restrict_self(2) syscall
static inline int
my_landlock_restrict_self(int ruleset_fd, uint32_t flags)
{
	return syscall(SYS_landlock_restrict_self, ruleset_fd, flags);
}

#endif
