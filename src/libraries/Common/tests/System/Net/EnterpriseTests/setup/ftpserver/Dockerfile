FROM ubuntu:22.04

# Install ProFTPD with SSL support
RUN apt-get update && \
    DEBIAN_FRONTEND=noninteractive apt-get install -y \
    proftpd-basic \
    openssl \
    && rm -rf /var/lib/apt/lists/*

# Create FTP user
RUN useradd -m -s /bin/bash ftpuser && \
    echo "ftpuser:ftppass" | chpasswd && \
    mkdir -p /home/ftpuser/ftp && \
    chown ftpuser:ftpuser /home/ftpuser/ftp

# Generate self-signed certificate for TLS
RUN mkdir -p /etc/proftpd/ssl && \
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout /etc/proftpd/ssl/proftpd.key \
    -out /etc/proftpd/ssl/proftpd.crt \
    -subj "/C=US/ST=State/L=City/O=Test/CN=ftpserver.linux.contoso.com"

# Copy configuration
COPY ftpserver/proftpd.conf /etc/proftpd/proftpd.conf

# Expose FTP ports
EXPOSE 21 20 50000-50100

CMD ["proftpd", "--nodaemon"]
