#!/bin/sh

cp /SHARED/apacheweb.keytab /etc/krb5.keytab

exec httpd -DFOREGROUND "$@"
