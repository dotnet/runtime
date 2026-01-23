// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       sandbox.h
/// \brief      Sandbox support
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#if defined(HAVE_PLEDGE) || defined(HAVE_LINUX_LANDLOCK) \
		|| defined(HAVE_CAP_RIGHTS_LIMIT)
#	define ENABLE_SANDBOX 1
#endif


/// \brief      Enables early sandboxing that can always be enabled
///
/// This requires that tuklib_progname() and io_init() have been called.
extern void sandbox_init(void);


/// \brief      Enable sandboxing that only allows opening files for reading
extern void sandbox_enable_read_only(void);


/// \brief      Tell sandboxing code that strict sandboxing can be used
///
/// This function only sets a flag which will be read by
/// sandbox_enable_strict_if_allowed().
extern void sandbox_allow_strict(void);


/// \brief      Enable sandboxing that allows reading from one file
///
/// This does nothing if sandbox_allow_strict() hasn't been called.
///
/// \param      src_fd          File descriptor open for reading
/// \param      pipe_event_fd   user_abort_pipe[0] from file_io.c
/// \param      pipe_write_fd   user_abort_pipe[1] from file_io.c
extern void sandbox_enable_strict_if_allowed(
		int src_fd, int pipe_event_fd, int pipe_write_fd);
