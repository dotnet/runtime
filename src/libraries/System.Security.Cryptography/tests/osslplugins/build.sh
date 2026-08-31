#!/bin/sh

if [ -e /usr/include/openssl/engine.h ]; then
    echo "INFO: Building dntest ENGINE..."
    clang -fPIC -o e_dntest.o -c e_dntest.c &&
        ld -shared --no-undefined --build-id -o dntest.so e_dntest.o -lcrypto -lc &&
        echo "INFO: dntest ENGINE built successfully..."
else
    echo "ERROR: Cannot build dntest ENGINE, missing engine.h"
fi
