// This is an incomplete & imprecice implementation of the Posix
// standard file by the same name

// Since this is only intended for VC++ compilers
// use #pragma once instead of guard macros
#pragma once

#ifdef _MSC_VER // Only for cross compilation to windows

#include <sys/types.h>

#define S_IFMT  00170000
#define S_IFDIR  0040000

#define S_ISDIR(m)      (((m) & S_IFMT) == S_IFDIR)

struct stat
{
    unsigned short st_dev;
    unsigned short st_ino;
    unsigned short st_mode;
    short st_nlink;
    short st_uid;
    short st_gid;
    unsigned short st_rdev;
    unsigned short st_size;
    time_t st_atime;
    time_t st_mtime;
    time_t st_ctime;
};

int stat(const char *path, struct stat *buf);
int fstat(int fd, struct stat *buf);

#endif // _MSC_VER
