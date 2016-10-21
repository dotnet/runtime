#!/bin/sh -x

#This script removes Mono from an OS X System.  It must be run as root

rm -r /Library/Frameworks/Mono.framework

# In 10.6+ the receipts are stored here
rm /var/db/receipts/com.ximian.mono*

for dir in /usr/local/bin; do
   (cd ${dir};
    for i in `ls -al | grep /Library/Frameworks/Mono.framework/ | awk '{print $9}'`; do
      rm ${i}
    done);
done
