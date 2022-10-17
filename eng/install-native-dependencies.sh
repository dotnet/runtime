#!/bin/sh

set -e

# This is a simple script primarily used for CI to install necessary dependencies
#
# For CI typical usage is
#
# ./install-native-dependencies.sh <OS> <arch> azDO
#
# For developer use it is not recommended to include the azDO final argument as that
# makes installation and configuration setting only required for azDO
#
# So simple developer usage would currently be
#
# ./install-native-dependencies.sh <OS>

os="$(echo "$1" | tr "[:upper:]" "[:lower:]")"

if [ -e /etc/os-release ]; then
    . /etc/os-release
fi

if [ "$os" = "linux" ] && { [ "$ID" = "debian" ] || [ "$ID_LIKE" = "debian" ]; }; then
    apt update

    apt install -y build-essential gettext locales cmake llvm clang lldb liblldb-dev libunwind8-dev libicu-dev liblttng-ust-dev \
        libssl-dev libkrb5-dev libnuma-dev libz-dev

    localedef -i en_US -c -f UTF-8 -A /usr/share/locale/locale.alias en_US.UTF-8
elif [ "$os" = "maccatalyst" ] || [ "$os" = "osx" ] || [ "$os" = "macos" ] || [ "$os" = "tvos" ] || [ "$os" = "ios" ]; then
    echo "Installed xcode version: $(xcode-select -p)"

    if [ "$3" = "azDO" ]; then
        # workaround for old osx images on hosted agents
        # piped in case we get an agent without these values installed
        if ! brew uninstall openssl@1.0.2t >/dev/null 2>&1; then
            echo "didn't uninstall openssl@1.0.2t"
        else
            echo "successfully uninstalled openssl@1.0.2t"
        fi
    fi

    brew update --preinstall
    brew bundle --no-upgrade --no-lock --file "$(dirname "$0")/Brewfile"
else
    echo "Must pass 'Linux', 'macOS', 'maccatalyst', 'iOS' or 'tvOS' as first argument."
    exit 1
fi
