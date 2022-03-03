#!/usr/bin/env bash

kdestroy
echo password | kinit user1
curl --verbose --negotiate -u: http://apacheweb.linux.contoso.com
kdestroy

nslookup github.com
if [ $? -ne 0 ]; then
  cp /etc/resolv.conf /etc/resolv.conf.ORI
  # try to fix-up DNS by adding public server
  echo nameserver 8.8.8.8 >> /etc/resolv.conf

  nslookup github.com
  curl --verbose http://apacheweb.linux.contoso.com
fi

