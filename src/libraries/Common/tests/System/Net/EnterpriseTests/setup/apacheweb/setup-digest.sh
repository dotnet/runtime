#!/bin/bash

echo -n 'user1:Digest Login:' > /setup/digest_pw
echo -n 'user1:Digest Login:Password20' | md5sum | cut -d ' '  -f 1 >> /setup/digest_pw
