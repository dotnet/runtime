FROM httpd:2.4

COPY ./common/krb5.conf /etc/
COPY ./apacheweb/httpd.conf /usr/local/apache2/conf/httpd.conf

WORKDIR /setup
COPY ./apacheweb/*.sh ./
RUN chmod +x *.sh ; \
    mkdir -p /usr/local/apache2/altdocs ; \
    cp /usr/local/apache2/htdocs/index.html /usr/local/apache2/altdocs

# Prevents dialog prompting when installing packages
ARG DEBIAN_FRONTEND=noninteractive

# Install Kerberos client, apache Negotiate auth plugin, and diagnostics
RUN apt-get update && \
    apt-get install -y --no-install-recommends libapache2-mod-auth-kerb procps krb5-user iputils-ping dnsutils nano

# Link apache2 kerb module to the right place since the apt-get install puts it in the wrong place for this docker image
RUN ln -s /usr/lib/apache2/modules/mod_auth_kerb.so /usr/local/apache2/modules

EXPOSE 80/tcp
EXPOSE 8080/tcp

ENTRYPOINT ["/bin/sh", "/setup/run.sh"]
