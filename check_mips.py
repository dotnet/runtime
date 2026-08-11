import urllib.request
url = "https://raw.githubusercontent.com/torvalds/linux/master/arch/mips/kernel/syscalls/syscall_n32.tbl"
try:
    req = urllib.request.urlopen(url)
    for line in req.read().decode('utf-8').split('\n'):
        if 'statx' in line:
            print(f"mips n32: {line}")
except:
    pass

url = "https://raw.githubusercontent.com/torvalds/linux/master/arch/mips/kernel/syscalls/syscall_n64.tbl"
try:
    req = urllib.request.urlopen(url)
    for line in req.read().decode('utf-8').split('\n'):
        if 'statx' in line:
            print(f"mips n64: {line}")
except:
    pass
