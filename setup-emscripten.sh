#!/bin/bash

USE_SYSTEM=false
SYSTEM_EMSCRIPTEN_DIR="${SYSTEM_EMSDK_PATH:-"/usr/share/emscripten"}"
EMSDK_DIR="${EMSDK_PATH:-"$HOME/.dotnet-emsdk"}"
EMSCRIPTEN_DIR="$EMSDK_DIR/upstream/emscripten"
EMSCRIPTEN_SOURCE="${EMSCRIPTEN_REPO:-"https://github.com/emscripten-core/emscripten.git"}"

set -e

show_help() {
    echo "======== Automated Emscripten Setup ========"
    echo "-- Usage: $0 [command] <args...>"
    echo "=> $0 | Installs emscripten."
    echo "  + --system-emscripten || --system || -s"
    echo "    Installs for the entire system."
    echo "    Installs to \$SYSTEM_EMSDK_PATH or /usr/share/emscripten"
    echo "    If this is not enabled, it installs to \$USER_EMSDK_PATH"
    echo "        or $HOME/.dotnet-emscripten"
    echo "  + --help || -h"
    echo "    Shows this help menu."
    echo "=> $0 help | Shows this help menu."
    echo "--------------------------------------------"
    echo "=> Influental environment variables"
    echo "  + EMSDK_PATH | The path to install the EMSDK (shim) to."
    echo "    Defaults to \"$HOME/.dotnet-emsdk\""
    echo "  + EMSCRIPTEN_REPO | The repository to download emscripten from."
    echo "    Defaults to \"https://github.com/emscripten-core/emscripten.git\""

    exit 0
}

parse_args() {
    if [[ $1 == *"--system-emscripten"* ]] || [[ $1 == *"--system"* ]] || [[ $1 == *"-s"* ]]; then
        USE_SYSTEM=true
    fi

    if [[ $1 == *"--help"* ]] || [[ $1 == *"-h"* ]]; then
        show_help
    fi
}

emscripten_system_install() {
    echo "Beginning system installation of Emscripten..."
    echo ">> Note: This may ask for your sudo password to install emscripten."
    echo ">> Note: This may happen twice or even not at all depending on your system configuration."

    sudo git clone --recursive "$EMSCRIPTEN_SOURCE" "$SYSTEM_EMSCRIPTEN_DIR"
    sudo chmod -R u+rwx "$SYSTEM_EMSCRIPTEN_DIR"
}

emscripten_user_install() {
    SYSTEM_EMSCRIPTEN_DIR="${USER_EMSDK_PATH:-"$HOME/.dotnet-emscripten"}"

    echo "Beginning installation of Emscripten..."
    echo ">> Note: This may take a while, since we have to install dependencies with NPM."

    if [[ ! -d "$SYSTEM_EMSCRIPTEN_DIR" ]]; then
        git clone --recursive "$EMSCRIPTEN_SOURCE" "$SYSTEM_EMSCRIPTEN_DIR"
    else
        echo "Directory exists, assuming it's already been cloned..."
    fi

    chmod -R u+rwx "$SYSTEM_EMSCRIPTEN_DIR"
}

install_emscripten() {
    if [[ $USE_SYSTEM == true ]]; then
        emscripten_system_install
    else
        emscripten_user_install
    fi

    __TMP_DIR="$(pwd)"

    cd "$SYSTEM_EMSCRIPTEN_DIR"

    npm install > /dev/null

    cd "$__TMP_DIR"
    unset "$__TMP_DIR"
}

create_shim() {
    ln -sf "$SYSTEM_EMSCRIPTEN_DIR" "$EMSCRIPTEN_DIR"

    if [[ ! -d "$EMSDK_DIR/bin" ]]; then
        echo "Linking binaries..."

        mkdir "$EMSDK_DIR/bin"

        find "$SYSTEM_EMSCRIPTEN_DIR" -maxdepth 1 -type f \
            -or -name "*.py" -or -name "*.bat" -name "em*" \
            -exec ln -sf {} "$EMSDK_DIR/bin/"{} \;
    fi
}

create_env() {
    if [[ ! -f "$EMSDK_DIR/emsdk_env.sh" ]]; then
        echo "Creating emsdk_env.sh..."

        echo "#!/bin/bash" > "$EMSDK_DIR/emsdk_env.sh"
        echo "export PATH=\"\$PATH:$EMSDK_DIR/bin\"" >> "$EMSDK_DIR/emsdk_env.sh"
        echo "export EMSDK_PATH=\"$EMSDK_DIR\"" >> "$EMSDK_DIR/emsdk_env.sh"

        echo "Done! Try running \"source $EMSDK_DIR/emsdk_env.sh\"!"
        echo "+ Tip: Add \"source $EMSDK_DIR/emsdk_env.sh\" to your \".bashrc\" file!"
    fi
}

check_shim() {
    if [[ ! -d "$EMSDK_DIR/upstream" ]]; then
        echo "Creating Emscripten shim in $EMSCRIPTEN_DIR..."

        mkdir "$EMSDK_DIR/upstream"

        create_shim
    else
        if [[ ! -d "$EMSCRIPTEN_DIR" ]]; then
            echo "Creating Emscripten shim in $EMSCRIPTEN_DIR..."

            create_shim
        else
            echo "Shim already exists, skipping creation..."
        fi
    fi
}

create_emsdk() {
    if [[ ! -d "$EMSDK_DIR" ]]; then
        echo "Creating EMSDK directory ($EMSDK_DIR)..."

        mkdir "$EMSDK_DIR"
    else
        echo "EMSDK directory ($EMSDK_DIR) already exists, skipping creation..."
    fi

    check_shim
    create_env
}

run() {
    parse_args "$@"
    install_emscripten
    create_emsdk

    exit 0
}

run "$@"
