/*
 * Copyright 2022-2023 BlackBerry Limited.
 *
 * This file is part of libunwind.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#ifndef OS_QNX_H
#define OS_QNX_H

#include <errno.h>
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <sys/stat.h>
#include <sys/types.h>


/**
 * Return a buffer contaiing the procfs file name, or NULL on failure.
 * @param[in]  pid   Identify the target process
 * @param[in]  ftype The type of procfs file to name.
 *
 * @returns a pointer to a newly-allocated string containing the name of the
 * procfs file of type @p ftype for process @p pid. The returned buffer needs to
 * be free()d.  Returns NULL on error and errno is set appropriately.
 */
static inline char *
get_proc_filename(pid_t pid, char const *ftype)
{
  char *filename = NULL;
  int pathlen = snprintf(NULL, 0, "/proc/%d/%s", pid, ftype);
  int saved_errno = errno;
  if (pathlen > 0)
    {
      ++pathlen;
      filename = malloc(pathlen);
      saved_errno = errno;
      if (filename != NULL)
        {
          int len = snprintf(filename, pathlen, "/proc/%d/%s", pid, ftype);
          saved_errno = errno;
          if (len < 0)
            {
              free(filename);
              filename = NULL;
            }
        }
    }
  errno = saved_errno;
  return filename;
}

/**
 * Opens the procfs address space file for a process.
 * @param[in]  pid  Identify the target process.
 *
 * @returns a valid file descriptor for the opened procfs address space file or
 * -1 on failure (and errno is set appropriately).
 */
static inline int
unw_nto_procfs_open_as(pid_t pid)
{
  int   as_fd = -1;
  char *as_filename = get_proc_filename(pid, "as");
  if (as_filename != NULL)
    {
      as_fd = open(as_filename, O_CLOEXEC | O_RDONLY);
      int saved_errno = errno;
      free(as_filename);
      errno = saved_errno;
    }
  return as_fd;
}


/**
 * Opens the procfs control file for a process.
 * @param[in]  pid  Identify the target process.
 *
 * @returns a valid file descriptor for the opened procfs control file or
 * -1 on failure (and errno is set appropriately).
 */
static inline int
unw_nto_procfs_open_ctl(pid_t pid)
{
  int   ctl_fd = -1;
  char *ctl_filename = get_proc_filename(pid, "ctl");
  if (ctl_filename != NULL)
    {
      ctl_fd = open(ctl_filename, O_CLOEXEC | O_RDWR);
      int saved_errno = errno;
      free(ctl_filename);
      errno = saved_errno;
    }
  return ctl_fd;
}



#endif /* OS_QNX_H */
