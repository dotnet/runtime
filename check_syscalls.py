import urllib.request
import re

url_base = "https://raw.githubusercontent.com/torvalds/linux/master/arch/"
arch_paths = {
    "x86": "x86/entry/syscalls/syscall_64.tbl",
    "i386": "x86/entry/syscalls/syscall_32.tbl",
    "arm": "arm/tools/syscall.tbl",
    "s390x": "s390/kernel/syscalls/syscall.tbl",
    "powerpc": "powerpc/kernel/syscalls/syscall.tbl",
    "asm-generic": "include/uapi/asm-generic/unistd.h"
}

def get_syscall(arch, path):
    try:
        if arch == "asm-generic":
            # For aarch64, riscv, loongarch
            req = urllib.request.urlopen("https://raw.githubusercontent.com/torvalds/linux/master/" + path)
            for line in req.read().decode('utf-8').split('\n'):
                if 'statx' in line and '__NR_statx' in line:
                    print(f"{arch}: {line}")
        else:
            req = urllib.request.urlopen(url_base + path)
            for line in req.read().decode('utf-8').split('\n'):
                if 'statx' in line and not line.startswith('#'):
                    print(f"{arch}: {line}")
    except Exception as e:
        print(f"Error {arch}: {e}")

for arch, path in arch_paths.items():
    get_syscall(arch, path)
