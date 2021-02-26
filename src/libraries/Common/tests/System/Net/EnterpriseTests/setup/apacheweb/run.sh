#!/bin/sh

cp /SHARED/apacheweb.keytab /etc/krb5.keytab

exec /usr/sbin/apache2 -DFOREGROUND "$@"
