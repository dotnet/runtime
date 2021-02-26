FROM ubuntu:18.04

ARG DEBIAN_FRONTEND=noninteractive

# Install Kerberos client, apache Negotiate auth plugin, and diagnostics
RUN apt-get update && \
    apt-get install -y --no-install-recommends apache2 libapache2-mod-auth-kerb procps krb5-user iputils-ping dnsutils nano \
                                               libapache2-mod-auth-ntlm-winbind samba
WORKDIR /setup

COPY ./common/krb5.conf /etc/
COPY ./apacheweb/apache2.conf /setup/apache2.conf
COPY ./apacheweb/*.sh ./
RUN chmod +x *.sh ; \
    mkdir -p /setup/htdocs/auth/kerberos /setup/altdocs/auth/kerberos ; touch /setup/htdocs/index.html /setup/htdocs/auth/kerberos/index.html /setup/altdocs/auth/kerberos/index.html ;\
    cp /etc/apache2/apache2.conf /etc/apache2/apache2.conf.ORIG ;\
    mv -f apache2.conf /etc/apache2/apache2.conf

EXPOSE 80/tcp
EXPOSE 8080/tcp

ENTRYPOINT ["/bin/sh", "/setup/run.sh"]
