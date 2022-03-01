#!/usr/bin/env bash

kdestroy
echo password | kinit user1
curl --verbose --negotiate -u: http://apacheweb.linux.contoso.com
kdestroy

nslookup github.com
if [ $? -ne 0 ]; then
  # try to fix-up DNS by adding public server
  echo nameserver 8.8.8.8 >> /etc/resolv.conf
fi
nslookup github.com
