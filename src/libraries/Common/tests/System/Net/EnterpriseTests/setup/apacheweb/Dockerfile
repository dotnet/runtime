FROM httpd:2.4

COPY ./common/krb5.conf /etc/

WORKDIR /setup
COPY ./apacheweb/*.sh ./
RUN chmod +x *.sh

# Prevents dialog prompting when installing packages
ARG DEBIAN_FRONTEND=noninteractive

# Install Kerberos client, apache Negotiate auth plugin, and diagnostics
RUN apt-get update && \
    apt-get install -y --no-install-recommends libapache2-mod-auth-kerb procps krb5-user iputils-ping dnsutils nano

# Link apache2 kerb module to the right place since the apt-get install puts it in the wrong place for this docker image
RUN ln -s /usr/lib/apache2/modules/mod_auth_kerb.so /usr/local/apache2/modules

# Modify httpd.conf to add Negotiate auth as required
RUN echo "LoadModule auth_kerb_module modules/mod_auth_kerb.so" >> /usr/local/apache2/conf/httpd.conf && \
    sed -i 's/Require all granted/AuthType Kerberos\nAuthName "Kerberos Login"\nKrbAuthRealm LINUX\.CONTOSO\.COM\nKrb5Keytab \/etc\/krb5\.keytab\nKrbMethodK5Passwd off\nRequire valid-user/' /usr/local/apache2/conf/httpd.conf

EXPOSE 80

ENTRYPOINT ["/bin/sh", "/setup/run.sh"]
