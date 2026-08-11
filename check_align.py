import urllib.request
url = "https://raw.githubusercontent.com/torvalds/linux/master/include/uapi/linux/stat.h"
req = urllib.request.urlopen(url)
for line in req.read().decode('utf-8').split('\n'):
    if 'struct statx {' in line:
        print(line)
