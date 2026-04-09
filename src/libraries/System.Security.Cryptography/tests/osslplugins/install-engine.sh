#!/bin/sh

enginepath=`openssl version -a | grep ENGINESDIR | cut -d '"' -f 2`

if [ -z "$enginepath" ]; then
  enginepath="/usr/lib/x86_64-linux-gnu/engines-3/"
  echo "WARNING: enginepath was not determined and following value will be used: $enginepath"
  echo "WARNING: Please update install-engine.sh script."
fi

if ! openssl engine -t -c `pwd`/dntest.so > /dev/null 2>&1; then
  echo 'ERROR: Unable to load dntest.so engine.'
  exit 1
fi
echo 'INFO: dntest loading test successful'

enginepath="${enginepath%/}/"

echo "INFO: Installing dntest.so engine to $enginepath"
sudo cp dntest.so $enginepath && echo 'INFO: Installation finished successfuly'
