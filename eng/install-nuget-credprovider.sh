#!/usr/bin/env bash
set -e

# This script installs the NuGet Credential Provider.

# Install curl if necessary. Dependency exists inside downloaded script.
if command -v curl > /dev/null; then
  echo "curl found."
else
  echo "curl not found, trying to install..."
  (
    set +e
    set -x
    apt update && apt install -y curl
    apk update && apk upgrade && apk add curl
    exit 0
  )
fi

# Install. Ported from https://gist.github.com/shubham90/ad85f2546a72caa20d57bce03ec3890f
install_credprovider() {
  # Download the provider and install.
  cred_provider_url='https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh'
  curl "$cred_provider_url" -s -S -L | bash

  # Environment variable to enable session token cache. More on this here: https://github.com/Microsoft/artifacts-credprovider#help
  export NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED=true
}

install_credprovider

# Additional setup to try to avoid flakiness: https://github.com/dotnet/arcade/issues/3932
export DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
export NUGET_PLUGIN_HANDSHAKE_TIMEOUT_IN_SECONDS=20
export NUGET_PLUGIN_REQUEST_TIMEOUT_IN_SECONDS=20
