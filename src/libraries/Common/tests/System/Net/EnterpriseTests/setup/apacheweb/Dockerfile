FROM mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-20220421022739-9c434db

ARG DEBIAN_FRONTEND=noninteractive

# Install Kerberos client, apache Negotiate auth plugin, and diagnostics
RUN apt-get update && \
    apt-get install -y --no-install-recommends apache2 libapache2-mod-auth-kerb procps krb5-user iputils-ping dnsutils nano \
                                               libapache2-mod-auth-ntlm-winbind samba samba-dsdb-modules samba-vfs-modules
WORKDIR /setup

COPY ./common/krb5.conf /etc/
COPY ./apacheweb/apache2.conf /setup/apache2.conf
COPY ./apacheweb/*.sh ./
RUN chmod +x *.sh ; \
    mkdir -p /setup/htdocs/auth/ntlm /setup/altdocs/auth/ntlm /setup/htdocs/auth/kerberos /setup/altdocs/auth/kerberos /setup/htdocs/auth/digest ;\
    touch /setup/htdocs/index.html /setup/htdocs/auth/kerberos/index.html /setup/htdocs/auth/ntlm/index.html /setup/htdocs/auth/digest/index.html ;\
    touch /setup/altdocs/auth/kerberos/index.html /setup/altdocs/auth/ntlm/index.html ;\
    cp /etc/apache2/apache2.conf /etc/apache2/apache2.conf.ORIG ;\
    mv -f apache2.conf /etc/apache2/apache2.conf

EXPOSE 80/tcp
EXPOSE 8080/tcp

ENTRYPOINT ["/bin/bash", "/setup/run.sh"]
