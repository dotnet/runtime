#define _GNU_SOURCE
#include <fcntl.h>
#include <stdio.h>
#include <sys/stat.h>
#include <unistd.h>
#include <sys/syscall.h>

int main() {
    struct statx stx;
    int ret = syscall(__NR_statx, AT_FDCWD, ".", 0, 0x7ff | 0x800, &stx);
    if (ret == 0) {
        printf("mask: %x\n", stx.stx_mask);
        printf("BTIME: %x\n", stx.stx_mask & 0x800);
    }
    return 0;
}
