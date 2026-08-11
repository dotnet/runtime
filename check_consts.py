import urllib.request

url = "https://raw.githubusercontent.com/torvalds/linux/master/include/uapi/linux/stat.h"
req = urllib.request.urlopen(url)
for line in req.read().decode('utf-8').split('\n'):
    if 'STATX_BASIC_STATS' in line or 'STATX_BTIME' in line:
        print(line)

url2 = "https://raw.githubusercontent.com/torvalds/linux/master/include/uapi/linux/fcntl.h"
req = urllib.request.urlopen(url2)
for line in req.read().decode('utf-8').split('\n'):
    if 'AT_EMPTY_PATH' in line or 'AT_STATX_SYNC_AS_STAT' in line:
        print(line)
