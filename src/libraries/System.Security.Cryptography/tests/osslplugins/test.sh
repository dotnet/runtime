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

export DOTNET_CRYPTOGRAPHY_TESTS_ENGINE_ENABLE=true

if [ -z "$DOTNET_CRYPTOGRAPHY_TESTS_ENGINE_TPM_ECDSA_KEY_HANDLE" ]; then
  echo "WARNING: TPM tests will not be run"
  echo "WARNING: Use following environmental variable to enable them:"
  echo "WARNING:   export DOTNET_CRYPTOGRAPHY_TESTS_ENGINE_TPM_ECDSA_KEY_HANDLE=YourHandleHere"
  echo "WARNING: For example:"
  echo "WARNING:   export DOTNET_CRYPTOGRAPHY_TESTS_ENGINE_TPM_ECDSA_KEY_HANDLE=0x81000007"
  echo "WARNING: Refer to README.md for more information on how to get handle."
fi

if [ "$1" == "--self-check" ]; then
  export DOTNET_CRYPTOGRAPHY_TESTS_ENGINE_ENSURE_FAILING=true
else
  echo "INFO: To run self-check use:"
  echo "INFO: ./test.sh --self-check"
  echo "INFO: Expect two test failures."
fi

set -e

cd "$nativelibs_path"
$dotnet build ./build-native.proj

cd "$ssc_src_path"
$dotnet build

cd "$ssc_tests_path"
$dotnet test --filter "FullyQualifiedName~System.Security.Cryptography.Tests.OpenSslNamedKeysTests."
