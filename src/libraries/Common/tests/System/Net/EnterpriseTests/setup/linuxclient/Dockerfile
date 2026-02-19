FROM mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-22.04

# Prevents dialog prompting when installing packages
ARG DEBIAN_FRONTEND=noninteractive

# Install Kerberos, NTLM, and diagnostic tools
COPY ./common/krb5.conf /etc/krb5.conf
RUN apt-get update && \
    apt-get install -y --no-install-recommends krb5-user gss-ntlmssp iputils-ping dnsutils nano

# Enable openssl legacy provider in system openssl config
RUN fixOpensslConf=$(mktemp) && \
    printf "#!/usr/bin/env sh\n\
        sed -i '\n\
            # Append 'legacy = legacy_sect' after 'default = default_sect' under [provider_sect]
            /^default = default_sect/a legacy = legacy_sect\n\
            # Search for [default_sect]
            /\[default_sect\]/ {\n\
                # Go to next line
                n\n\
                # Uncomment '# activate = 1'
                s/# //\n\
                # Append new [legacy_sect], with 'activate = 1'
                a\n\
                a [legacy_sect]\n\
                a activate = 1\n\
            }\n\
            ' /etc/ssl/openssl.cnf\n" > $fixOpensslConf && \
    sh $fixOpensslConf && \
    rm $fixOpensslConf

# Set environment variable to turn on enterprise tests
ENV DOTNET_RUNTIME_ENTERPRISETESTS_ENABLED 1

WORKDIR /setup
COPY ./linuxclient/*.sh ./
RUN chmod +x *.sh

WORKDIR /repo

ENTRYPOINT ["/bin/bash", "/setup/run.sh"]
