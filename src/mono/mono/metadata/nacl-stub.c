
#if defined(__native_client__)

#ifdef HAVE_CONFIG_H
#include <config.h>
#endif
#include <eglib/src/glib.h>
#include <errno.h>
#include <sys/types.h>

struct group *getgrnam(const char *name) { return NULL; }
struct group *getgrgid(gid_t gid) { errno=EIO; return NULL; }
int fsync(int fd) { errno=EINVAL; return -1; }
dev_t makedev(guint32 maj, guint32 min) { return (maj)*256+(min); }

#ifdef USE_NEWLIB
int getdtablesize(void) {
#ifdef OPEN_MAX
  return OPEN_MAX;
#else
  return 256;
#endif
}

size_t getpagesize(void) {
#ifdef PAGE_SIZE
  return PAGE_SIZE;
#else
  return 4096;
#endif
}

#include <semaphore.h>

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
