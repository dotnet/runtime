
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

#endif
