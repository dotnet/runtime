TODO: update names of variables and review
TODO: add section with logging https://github.com/tpm2-software/tpm2-tss/blob/master/doc/logging.md
TODO:    export TSS2_LOG=all+NONE


# Testing instructions for OpenSSL ENGINE

Once everything is setup tests related to TPM and our engine can be run using:

```bash
export DOTNET_CRYPTOGRAPHY_TESTS_ENGINE_ENABLE=true
./test.sh
```

This script will re-build native components, rebuild the product and then build and run subset of manual tests related to our ENGINE.

If TPM environmental variable similar to following is defined:

```bash
# 0x81000007 is just an example, read further how to get it
export DOTNET_CRYPTOGRAPHY_TESTS_TPM_ECDSA_KEY_HANDLE=0x81000007
```

then tests using TPM will be run as well and they will use `0x81000007` handle.

Instructions how to get this handle are described further in this document.

To ensure you're actually running all tests run script with following argument:

```bash
./test.sh --self-check
```

that should cause 2 sanity tests to fail (one for TPM and one for our test engine). Those are meant only as a self-check that tests are actually running. The error message should say that this failure is expected.

If you're seeing single failure and TPM tests skipped it means you have not passed the handle through environmental variable.
If you're seeing no failures or different errors then debug.

## Building and installing test OpenSSL ENGINE

Source code for our test ENGINE implementation is in [e_dntest.c](e_dntest.c) file.

In order to build and install our test engine run:

```bash
./build.sh && ./install-engine.sh
```

You should see following output:

```
INFO: Building dntest ENGINE...
INFO: dntest ENGINE built successfully...
INFO: dntest loading test successful
INFO: Installing dntest.so engine to /usr/lib/x86_64-linux-gnu/engines-3/
INFO: Installation finished successfuly
```

After installation following environmental variable can be used to enable our test engine tests with `dotnet test`:

```bash
export DOTNET_CRYPTOGRAPHY_TESTS_ENGINE_ENABLE=true
```

This step is already done by `test.sh` script.

## TPM testing

In order to test TPM you need:

- tpm2-tss-engine installed
- a TPM handle and set it with environmental variable so that tests pick it up

### Building and installing tpm2-tss-engine

You should follow [tpm2-tss-engine INSTALL.md](https://github.com/tpm2-software/tpm2-tss-engine/blob/master/INSTALL.md) page.
These instructions have worked for me for the most part except:

```bash
./configure
```

produced bunch of warnings which got treated as errors. I have suppressed them and was successfully able to build using:

```bash
./configure CFLAGS='-DOPENSSL_SUPPRESS_DEPRECATED -Wno-incompatible-pointer-types -Wno-discarded-qualifiers'
```

instead.

### Verifying installation

After installation (`sudo make install`) run following command:

```bash
openssl engine -t -c tpm2tss
```

On my installation this has printed following output:

 ```
(tpm2tss) TPM2-TSS engine for OpenSSL
 [RSA, RAND]
     [ available ]
4007A032E27F0000:error:1280006A:DSO support routines:dlfcn_bind_func:could not bind to the requested symbol name:../crypto/dso/dso_dlfcn.c:188:symname(EVP_PKEY_base_id): /usr/lib/x86_64-linux-gnu/engines-3/tpm2tss.so: undefined symbol: EVP_PKEY_base_id
4007A032E27F0000:error:1280006A:DSO support routines:DSO_bind_func:could not bind to the requested symbol name:../crypto/dso/dso_lib.c:176:
 ```

which tells me tpm2tss engine is now available.

Per https://github.com/openssl/openssl/issues/17962 those errors showed in the end can be ignored.

### Debugging tpm2-tss-engine issues

To enable extra logging use following environment variable at runtime:

```
export TSS2_LOG=all+TRACE
```

Most of the time this should not be needed but it might be useful if you're seeing issues when interacting with the ENGINE.

# Testing instructions for OpenSSL Provider

## Installation

To install TPM2 provider refer to https://github.com/tpm2-software/tpm2-openssl - on Ubuntu following step can be used:

```bash
sudo apt install tpm2-openssl tpm2-tools tpm2-abrmd libtss2-tcti-tabrmd0
```

## Running provider tests

In order to run provider tests you need to have TPM handles and set one or more of the following environmental variables:

```csharp
# Handle values are just an example - refer to 'Getting TPM handle' section for instructions on how to create or get them
export DOTNET_CRYPTOGRAPHY_TESTS_TPM_ECDSA_KEY_HANDLE=0x81000007
export DOTNET_CRYPTOGRAPHY_TESTS_TPM_ECDH_KEY_HANDLE=0x8100000d
export DOTNET_CRYPTOGRAPHY_TESTS_TPM_RSA_DECRYPT_KEY_HANDLE=0x8100000c

# RSA-PSS tests will always fail due to following issues but they can be run still
# https://github.com/dotnet/runtime/issues/104080
# https://github.com/tpm2-software/tpm2-openssl/issues/115
export DOTNET_CRYPTOGRAPHY_TESTS_TPM_RSA_SIGN_KEY_HANDLE=0x8100000a
```

# Getting TPM handle

First, we will need `tpm2-tools`` installed:

```bash
sudo apt install tpm2-tools
```

## Getting TPM handles

If you already have a handle but you forgot what it is you can list all available handles using following command:

```bash
tpm2_getcap handles-persistent
```

it can be also used to verify if your handle got created correctly.

Command by default will only list handles but no information about them.
To get information about specific handle:

```bash
tpm2_readpublic -c 0x81000007
```

You can also extract public key like this if needed:

```bash
tpm2_readpublic -c 0x81000007 -o /tmp/key.pub
```

### Testing handle with OpenSSL CLI

In case you find issues with your handle you can test it using OpenSSL CLI, for example `0x81000004` can be tested like following:

#### RSA key

```bash
# create testdata file with some content
echo 'content' > testdata

# hash & sign data
openssl dgst -engine tpm2tss -keyform engine -sha256 -sign 0x81000007 -out testdata.sig testdata

# sign digest
openssl pkeyutl -engine tpm2tss -keyform engine -inkey 0x81000007 -sign -pkeyopt digest:sha256 -in testdata.dgst -out testdata.sig

# get public key (PEM)
openssl pkey -engine tpm2tss -inform engine -in '0x81000007' -pubout -out testkey.pub

# verify data
openssl pkeyutl -verify -in testdata.dgst -sigfile testdata.sig -inkey testkey.pub -pubin -pkeyopt digest:sha256
```

## Creating keys and handles

### ECDSA key

```bash
tpm2_createprimary -C o -g sha256 -G ecc256:ecdsa-sha256:null -c primary.ctx -a 'fixedtpm|fixedparent|sensitivedataorigin|userwithauth|noda|sign'

# To create permenent handle and print it:
tpm2_evictcontrol -C o -c primary.ctx
```

### ECDH key

```bash
tpm2_createprimary -C o -g sha256 -G ecc256 -c primary.ctx -a 'fixedtpm|fixedparent|sensitivedataorigin|userwithauth|noda|decrypt'

# To create permenent handle and print it:
tpm2_evictcontrol -C o -c primary.ctx
```

### RSA key (RSAPSS + SHA256)

This is not used by tests but if needed for further testing:

```bash
# To create key
tpm2_createprimary -C o -g sha256 -G rsa2048:rsapss:null -c primary.ctx -a 'fixedtpm|fixedparent|sensitivedataorigin|userwithauth|noda|sign'

# To create permenent handle and print it:
tpm2_evictcontrol -C o -c primary.ctx
```

### RSA key (decryption)

This is not used by tests but if needed for further testing:

```bash
# To create key
tpm2_createprimary -C o -g sha256 -G rsa2048 -c primary.ctx -a 'fixedtpm|fixedparent|sensitivedataorigin|userwithauth|noda|decrypt'

# To create permenent handle and print it:
tpm2_evictcontrol -C o -c primary.ctx
```
