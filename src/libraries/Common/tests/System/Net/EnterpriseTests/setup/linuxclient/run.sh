#!/usr/bin/env bash

cp /SHARED/linuxclient.keytab /etc/krb5.keytab

# Keep the container running
tail -f /dev/null
