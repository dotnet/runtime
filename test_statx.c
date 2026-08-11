#define _GNU_SOURCE
#include <fcntl.h>
#include <stdio.h>
#include <sys/stat.h>
#include <unistd.h>
#include <sys/syscall.h>

int main() {
    struct statx stx;
    int ret = syscall(__NR_statx, AT_FDCWD, ".", 0, 0x7ff, &stx);
    if (ret == 0) {
        printf("mask: %x\n", stx.stx_mask);
        printf("BASIC: %x\n", 0x7ff);
    } else {
        perror("statx");
    }
    return 0;
}
