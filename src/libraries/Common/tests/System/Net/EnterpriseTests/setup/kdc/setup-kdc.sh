#!/usr/bin/env bash

set -e

# Kerbose Logging
mkdir -pv /var/log/kerberos/
touch /var/log/kerberos/krb5.log
touch /var/log/kerberos/kadmin.log
touch /var/log/kerberos/krb5lib.log

# Create Kerberos database
kdb5_util create -r LINUX.CONTOSO.COM -P password -s

# Start KDC service
krb5kdc

# Add users
kadmin.local -q "add_principal -pw Password20 root/admin@LINUX.CONTOSO.COM"
kadmin.local -q "add_principal -pw Password20 user1@LINUX.CONTOSO.COM"

# Add SPNs for services
kadmin.local -q "add_principal -pw password HTTP/apacheweb.linux.contoso.com"
kadmin.local -q "add_principal -pw password HTTP/altweb.linux.contoso.com:8080"
kadmin.local -q "add_principal -pw password HOST/linuxclient.linux.contoso.com"
kadmin.local -q "add_principal -pw password HOST/localhost"
kadmin.local -q "add_principal -pw password NEWSERVICE/localhost"

# Create keytab files for other machines
kadmin.local ktadd -k /SHARED/apacheweb.keytab -norandkey -glob "HTTP/*web*"
kadmin.local ktadd -k /SHARED/linuxclient.keytab -norandkey HOST/linuxclient.linux.contoso.com
kadmin.local ktadd -k /SHARED/linuxclient.keytab -norandkey HOST/localhost
