lang en_US.UTF-8
keyboard us
timezone --utc Asia/Seoul

part / --fstype="ext4" --size=3500 --ondisk=mmcblk0 --label rootfs --fsoptions=defaults,noatime

repo --name=mobile  --baseurl=http://download.tizen.org/releases/weekly/tizen/mobile/latest/repos/arm-wayland/packages/ --ssl_verify=no
repo --name=base    --baseurl=http://download.tizen.org/releases/weekly/tizen/base/latest/repos/arm/packages/           --ssl_verify=no

%packages
tar
gzip

sed
grep
gawk
perl

binutils
findutils
util-linux
procps-ng
tzdata
ca-certificates

### Core FX
libicu
libuuid
libunwind
iputils
zlib
krb5
libcurl
libopenssl

%end

%post

### Update /tmp privilege
chmod 777 /tmp
####################################

%end
