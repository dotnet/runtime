#!/usr/bin/env bash -x

echo "$@" > /tmp/args

cp /SHARED/apacheweb.keytab /etc/krb5.keytab

if [[ "$1" == "-debug" ]]; then
  while [ 1 ];do
    sleep 10000
  done
fi

if [[ "$1" == "-DNTLM" ]]; then
  # NTLM/Winbind is aggressive and eats Negotiate so it cannot be combined with Kerberos
  ./setup-pdc.sh
  /usr/sbin/apache2 -DALTPORT "$@"
  shift
fi

./setup-digest.sh

# Start ProFTPD in the background for FTP/SSL testing
echo "Starting ProFTPD..."
/usr/sbin/proftpd
sleep 1

# Check if ProFTPD is running
if ! pgrep -x proftpd > /dev/null; then
    echo "ProFTPD failed to start, checking logs..."
    cat /var/log/proftpd/proftpd.log 2>/dev/null || echo "No ProFTPD log found"
    echo "ProFTPD not running"
    exit 1
fi

exec /usr/sbin/apache2 -DFOREGROUND "$@"
