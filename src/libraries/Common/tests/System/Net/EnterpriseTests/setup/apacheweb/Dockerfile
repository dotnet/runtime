FROM mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-24.04

ARG DEBIAN_FRONTEND=noninteractive

# Install Kerberos client, ProFTPD with SSL and HTTP with NTLM and Negotiate
RUN apt-get update && \
    apt-get install -y --no-install-recommends apache2 procps krb5-user iputils-ping dnsutils nano \
                                               samba samba-dsdb-modules samba-vfs-modules \
                                               proftpd-basic proftpd-mod-crypto openssl \
                                               apache2-dev libapache2-mod-auth-gssapi samba-ad-provision winbind
WORKDIR /setup

COPY ./common/krb5.conf /etc/
COPY ./apacheweb/apache2.conf /setup/apache2.conf
COPY ./apacheweb/*.sh ./
# This module is obsolete and no longer available as binary
COPY ./apacheweb/mod_auth_ntlm_winbind/mod_auth_ntlm_winbind.c /tmp
RUN chmod +x *.sh ; \
    mkdir -p /setup/htdocs/auth/ntlm /setup/altdocs/auth/ntlm /setup/htdocs/auth/kerberos /setup/altdocs/auth/kerberos /setup/htdocs/auth/digest ;\
    touch /setup/htdocs/index.html /setup/htdocs/auth/kerberos/index.html /setup/htdocs/auth/ntlm/index.html /setup/htdocs/auth/digest/index.html ;\
    touch /setup/altdocs/auth/kerberos/index.html /setup/altdocs/auth/ntlm/index.html ;\
    cp /etc/apache2/apache2.conf /etc/apache2/apache2.conf.ORIG ;\
    mv -f apache2.conf /etc/apache2/apache2.conf ;\
    cd /tmp && apxs -DAPACHE2 -c -i mod_auth_ntlm_winbind.c

# Setup FTP user and directories
ARG FTP_PASSWORD=PLACEHOLDER_FTP_PASSWORD
RUN useradd -m -s /bin/bash ftpuser && \
    echo "ftpuser:${FTP_PASSWORD}" | chpasswd && \
    mkdir -p /home/ftpuser/ftp && \
    chown ftpuser:ftpuser /home/ftpuser/ftp && \
    mkdir -p /var/log/proftpd && \
    chmod 755 /var/log/proftpd

# Generate self-signed certificate for FTP TLS
RUN mkdir -p /etc/proftpd/ssl && \
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout /etc/proftpd/ssl/proftpd.key \
    -out /etc/proftpd/ssl/proftpd.crt \
    -subj "/C=US/ST=State/L=City/O=Test/CN=apacheweb.linux.contoso.com"

# Copy ProFTPD configuration
COPY ./apacheweb/proftpd.conf /etc/proftpd/proftpd.conf

EXPOSE 80/tcp
EXPOSE 8080/tcp
EXPOSE 21/tcp
EXPOSE 50000-50100/tcp

ENTRYPOINT ["/bin/bash", "/setup/run.sh"]
