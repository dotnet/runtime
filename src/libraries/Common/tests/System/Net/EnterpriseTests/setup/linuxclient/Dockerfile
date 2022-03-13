FROM mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-20211022152710-047508b

# Prevents dialog prompting when installing packages
ARG DEBIAN_FRONTEND=noninteractive

# Install Kerberos, NTLM, and diagnostic tools
COPY ./common/krb5.conf /etc/krb5.conf
RUN apt-get update && \
    apt-get install -y --no-install-recommends krb5-user gss-ntlmssp iputils-ping dnsutils nano

# Set environment variable to turn on enterprise tests
ENV DOTNET_RUNTIME_ENTERPRISETESTS_ENABLED 1

WORKDIR /setup
COPY ./linuxclient/*.sh ./
RUN chmod +x *.sh

WORKDIR /repo

ENTRYPOINT ["/bin/bash", "/setup/run.sh"]
