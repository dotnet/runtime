#!/bin/bash

rm -f /etc/samba/smb.conf
# Configure domain and start daemons
samba-tool domain  provision --use-rfc2307 --domain=linux --adminpass=password20. --realm=LINUX.CONTOSO.COM  --server-role=dc
/etc/init.d/samba-ad-dc start

# make sure Apache can connect
usermod -a -G winbindd_priv daemon

# Add user for testing
samba-tool user create user1 Password20

