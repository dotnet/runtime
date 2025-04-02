#!/bin/bash

# Follow instructions from README.md

ossplugings_path="$(dirname "$(realpath "${BASH_SOURCE[0]}")")"

ssc_tests_path="$(dirname "$ossplugings_path")"

ssc_path="$(dirname "$ssc_tests_path")"
ssc_src_path="$ssc_path/src"

libsrc_path="$(dirname "$ssc_path")"

src_path="$(dirname "$libsrc_path")"
repo_root_path="$(dirname "$src_path")"
dotnet="$repo_root_path/dotnet.sh"

nativelibs_path="$src_path/native/libs"

if [ -z "$DOTNET_CRYPTOGRAPHY_TESTS_ENGINE_ENABLE" ]; then
  echo "WARNING: Engine tests will not be run"
  echo "WARNING: Use following variable to enable them:"
  echo "WARNING:   export DOTNET_CRYPTOGRAPHY_TESTS_ENGINE_ENABLE=true"
  echo "WARNING: Refer to README.md for more information."
  echo
fi

if [ -z "$DOTNET_CRYPTOGRAPHY_TESTS_TPM_ECDSA_KEY_HANDLE" ]; then
  echo "WARNING: TPM ECDSA tests will not be run"
  echo "WARNING: Use following environmental variable to enable them:"
  echo "WARNING:   export DOTNET_CRYPTOGRAPHY_TESTS_TPM_ECDSA_KEY_HANDLE=YourHandleHere"
  echo "WARNING: For example:"
  echo "WARNING:   export DOTNET_CRYPTOGRAPHY_TESTS_TPM_ECDSA_KEY_HANDLE=0x81000007"
  echo "WARNING: Refer to README.md for more information on how to get handle."
  echo
fi

if [ -z "$DOTNET_CRYPTOGRAPHY_TESTS_TPM_ECDH_KEY_HANDLE" ]; then
  echo "WARNING: TPM ECDH tests will not be run"
  echo "WARNING: Use following environmental variable to enable them:"
  echo "WARNING:   export DOTNET_CRYPTOGRAPHY_TESTS_TPM_ECDH_KEY_HANDLE=YourHandleHere"
  echo "WARNING: For example:"
  echo "WARNING:   export DOTNET_CRYPTOGRAPHY_TESTS_TPM_ECDH_KEY_HANDLE=0x8100000d"
  echo "WARNING: Refer to README.md for more information on how to get handle."
  echo
fi

if [ -z "$DOTNET_CRYPTOGRAPHY_TESTS_TPM_RSA_KEY_HANDLE" ]; then
  echo "WARNING: TPM RSA sign and decrypt tests will not be run"
  echo "WARNING: Use following environmental variable to enable them:"
  echo "WARNING:   export DOTNET_CRYPTOGRAPHY_TESTS_TPM_RSA_KEY_HANDLE=YourHandleHere"
  echo "WARNING: For example:"
  echo "WARNING:   export DOTNET_CRYPTOGRAPHY_TESTS_TPM_RSA_KEY_HANDLE=0x81000003"
  echo "WARNING: Refer to README.md for more information on how to get handle."
  echo
fi

set -e

cd "$nativelibs_path"
$dotnet build ./build-native.proj

cd "$ssc_src_path"
$dotnet build

cd "$ssc_tests_path"
$dotnet test --filter "FullyQualifiedName~System.Security.Cryptography.Tests.OpenSslNamedKeysTests."

# TLS tests are in System.Net.Security
cd "$libsrc_path/System.Net.Security/src"
$dotnet build

cd "$libsrc_path/System.Net.Security/tests/FunctionalTests"
$dotnet test --filter "FullyQualifiedName~System.Net.Security.Tests.SslStreamOpenSslNamedKeys."
