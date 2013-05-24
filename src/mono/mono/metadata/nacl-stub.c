#if defined(__native_client__)

#include "nacl-stub.h"

struct group *getgrnam(const char *name)
{
	return NULL;
}

struct group *getgrgid(gid_t gid)
{
	errno = EIO;
	return NULL;
}

int fsync(int fd)
{
	errno = EINVAL;
	return -1;
}

#ifdef USE_NEWLIB
dev_t makedev(int maj, int min)
{
	return (maj)*256+(min);
}

int utime(const char *filename, const void *times)
{
	errno = EACCES;
	return -1;
}

int kill(pid_t pid, int sig)
{
	errno = EACCES;
	return -1;
}

int getrusage(int who, void *usage)
{
	errno = EACCES;
	return -1;
}

int lstat(const char *path, struct stat *buf)
{
	return stat (path, buf);
}

int getdtablesize(void)
{
#ifdef OPEN_MAX
  return OPEN_MAX;
#else
  return 256;
#endif
}

size_t getpagesize(void)
{
#ifdef PAGE_SIZE
  return PAGE_SIZE;
#else
  return 4096;
#endif
}

int sem_trywait(sem_t *sem) {
  g_assert_not_reached ();
  return -1;
}

int sem_timedwait(sem_t *sem, const struct timespec *abs_timeout) {
  g_assert_not_reached ();
  return -1;
}

#endif

#endif
