#!/bin/sh

set -e

# This is a simple script primarily used for CI to install necessary dependencies
#
# Usage:
#
# ./install-native-dependencies.sh <OS>

os="$(echo "$1" | tr "[:upper:]" "[:lower:]")"

if [ -e /etc/os-release ]; then
    . /etc/os-release
fi

if [ "$os" = "linux" ] && { [ "$ID" = "debian" ] || [ "$ID_LIKE" = "debian" ]; }; then
    apt update

    apt install -y build-essential gettext locales cmake llvm clang lldb liblldb-dev libunwind8-dev libicu-dev liblttng-ust-dev \
        libssl-dev libkrb5-dev libnuma-dev zlib1g-dev

    localedef -i en_US -c -f UTF-8 -A /usr/share/locale/locale.alias en_US.UTF-8
elif [ "$os" = "maccatalyst" ] || [ "$os" = "osx" ] || [ "$os" = "macos" ] || [ "$os" = "tvos" ] || [ "$os" = "ios" ]; then
    echo "Installed xcode version: $(xcode-select -p)"

    export HOMEBREW_NO_INSTALL_CLEANUP=1
    export HOMEBREW_NO_INSTALLED_DEPENDENTS_CHECK=1
    # Skip brew update for now, see https://github.com/actions/setup-python/issues/577
    # brew update --preinstall
    brew bundle --no-upgrade --no-lock --file "$(dirname "$0")/Brewfile"
else
    echo "Must pass 'linux', 'macos', 'maccatalyst', 'ios' or 'tvos' as first argument."
    exit 1
fi
