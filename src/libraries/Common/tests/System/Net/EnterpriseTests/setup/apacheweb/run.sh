#!/bin/bash -x

echo "$@" > /tmp/args

cp /SHARED/apacheweb.keytab /etc/krb5.keytab

if [ "$1" == "-debug" ]; then
  while [ 1 ];do
    sleep 10000
  done
fi

if [ "$1" == "-DNTLM" ]; then
  ./setup-pdc.sh
  /usr/sbin/apache2 -DALTPORT "$@"
  shift
fi

./setup-digest.sh

exec /usr/sbin/apache2 -DFOREGROUND "$@"
