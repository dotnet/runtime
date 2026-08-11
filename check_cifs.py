import urllib.request
url = "https://raw.githubusercontent.com/torvalds/linux/master/fs/smb/client/inode.c"
try:
    req = urllib.request.urlopen(url)
    for line in req.read().decode('utf-8').split('\n'):
        if 'STATX' in line:
            print(line)
except Exception as e:
    print(e)
