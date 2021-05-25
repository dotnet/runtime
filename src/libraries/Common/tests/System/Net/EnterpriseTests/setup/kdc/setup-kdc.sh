#!/usr/bin/env bash

set -e

# Kerbose Logging
mkdir -pv /var/log/kerberos/
touch /var/log/kerberos/krb5.log
touch /var/log/kerberos/kadmin.log
touch /var/log/kerberos/krb5lib.log

# Create Kerberos database
kdb5_util create -r LINUX.CONTOSO.COM -P PLACEHOLDERadmin. -s

# Start KDC service
krb5kdc

# Add users
kadmin.local -q "add_principal -pw PLACEHOLDERcorrect20 root/admin@LINUX.CONTOSO.COM"
kadmin.local -q "add_principal -pw PLACEHOLDERcorrect20 user1@LINUX.CONTOSO.COM"

# Add SPNs for services
kadmin.local -q "add_principal -pw PLACEHOLDERadmin. HTTP/apacheweb.linux.contoso.com"
kadmin.local -q "add_principal -pw PLACEHOLDERadmin. HTTP/altweb.linux.contoso.com:8080"
kadmin.local -q "add_principal -pw PLACEHOLDERadmin. HOST/linuxclient.linux.contoso.com"
kadmin.local -q "add_principal -pw PLACEHOLDERadmin. HOST/localhost"
kadmin.local -q "add_principal -pw PLACEHOLDERadmin. NEWSERVICE/localhost"

# Create keytab files for other machines
kadmin.local ktadd -k /SHARED/apacheweb.keytab -norandkey -glob "HTTP/*web*"
kadmin.local ktadd -k /SHARED/linuxclient.keytab -norandkey HOST/linuxclient.linux.contoso.com
kadmin.local ktadd -k /SHARED/linuxclient.keytab -norandkey HOST/localhost
