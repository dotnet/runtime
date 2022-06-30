FROM mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-20220421022739-9c434db

COPY ./kdc/kadm5.acl /etc/krb5kdc/
COPY ./kdc/kdc.conf /etc/krb5kdc/
COPY ./common/krb5.conf /etc/

RUN mkdir /SHARED

WORKDIR /setup
COPY ./kdc/*.sh ./
RUN chmod +x *.sh

# Prevents dialog prompting when installing packages
ARG DEBIAN_FRONTEND=noninteractive

# Install KDC and diagnostic tools
RUN apt-get update && \
    apt-get install -y --no-install-recommends krb5-kdc krb5-admin-server iputils-ping dnsutils nano

RUN ./setup-kdc.sh

VOLUME /SHARED

EXPOSE 88/tcp
EXPOSE 88/udp

ENTRYPOINT ["/bin/bash", "/setup/run.sh"]
