#!/usr/bin/env bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Debian Packaging Script
# Currently Intended to build on ubuntu14.04

set -e

SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ "$SOURCE" != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

source "$DIR/../common/_common.sh"
REPOROOT="$DIR/../.."

help(){
    echo "Usage: $0"
    echo ""
    echo "Options:"
    echo "  --input <input directory>          Package the entire contents of the directory tree."
    echo "  --output <output debfile>          The full path to which the package will be written."
    echo "  --package-name <package name>      Package to identify during installation. Example - 'dotnet-sharedframework'"
    echo "  --brand-name <brand name>          Brand name of the package, used for 'short_description' of the deb file. Example - '.NET Core'"
    echo "  --framework-nuget-name <name>      The name of the nuget package that produced this shared framework."
    echo "  --framework-nuget-version <ver>    The versionf of the nuget package that produced this shared framework."
    echo "  --obj-root <object root>           Root folder for intermediate objects."
    echo "  --version <version>                Version for the debain package."
    exit 1
}

while [[ $# > 0 ]]; do
    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        -o|--output)
            OUTPUT_DEBIAN_FILE=$2
            shift
            ;;
        -i|--input)
            REPO_BINARIES_DIR=$2
            shift
            ;;
        -p|--package-name)
            SHARED_FRAMEWORK_DEBIAN_PACKAGE_NAME=$2
            shift
            ;;
        -b|--brand-name)
            SHARED_FRAMEWORK_BRAND_NAME=$2
            shift
            ;;
        --framework-nuget-name)
            SHARED_FRAMEWORK_NUGET_NAME=$2
            shift
            ;;
        --framework-nuget-version)
            SHARED_FRAMEWORK_NUGET_VERSION=$2
            shift
            ;;
        --obj-root)
            OBJECT_DIR=$2
            shift
            ;;
        --version)
            SHARED_FRAMEWORK_DEBIAN_VERSION=$2
            shift
            ;;
        --help)
            help
            ;;
        *)
            break
            ;;
    esac
    shift
done

PACKAGING_ROOT="$REPOROOT/packaging/sharedframework/debian"
PACKAGING_TOOL_DIR="$REPOROOT/tools/DebianPackageTool"

PACKAGE_OUTPUT_DIR="$OBJECT_DIR/deb_output"
PACKAGE_LAYOUT_DIR="$OBJECT_DIR/deb_intermediate"
TEST_STAGE_DIR="$OBJECT_DIR/debian_tests"

execute_build(){
    create_empty_debian_layout
    copy_files_to_debian_layout
    update_debian_json
    create_debian_package
}

create_empty_debian_layout(){
    header "Creating empty debian package layout"

    rm -rf "$PACKAGE_LAYOUT_DIR"
    mkdir -p "$PACKAGE_LAYOUT_DIR"

    mkdir "$PACKAGE_LAYOUT_DIR/\$"
    mkdir "$PACKAGE_LAYOUT_DIR/package_root"
    mkdir "$PACKAGE_LAYOUT_DIR/samples"
    mkdir "$PACKAGE_LAYOUT_DIR/docs"
}

copy_files_to_debian_layout(){
    header "Copying files to debian layout"

    # Copy Built Binaries
    cp -a "$REPO_BINARIES_DIR/." "$PACKAGE_LAYOUT_DIR/package_root"

    # Copy config file
    cp "$PACKAGING_ROOT/dotnet-sharedframework-debian_config.json" "$PACKAGE_LAYOUT_DIR/debian_config.json"
}

create_debian_package(){
    header "Packing .deb"

    mkdir -p "$PACKAGE_OUTPUT_DIR"

    "$PACKAGING_TOOL_DIR/package_tool" -i "$PACKAGE_LAYOUT_DIR" -o "$PACKAGE_OUTPUT_DIR" -n "$SHARED_FRAMEWORK_DEBIAN_PACKAGE_NAME" -v "$SHARED_FRAMEWORK_DEBIAN_VERSION"
}

update_debian_json()
{
    header "Updating debian.json file"
    sed -i "s/%SHARED_FRAMEWORK_DEBIAN_PACKAGE_NAME%/$SHARED_FRAMEWORK_DEBIAN_PACKAGE_NAME/g" "$PACKAGE_LAYOUT_DIR"/debian_config.json
    sed -i "s/%SHARED_FRAMEWORK_NUGET_NAME%/$SHARED_FRAMEWORK_NUGET_NAME/g" "$PACKAGE_LAYOUT_DIR"/debian_config.json
    sed -i "s/%SHARED_FRAMEWORK_NUGET_VERSION%/$SHARED_FRAMEWORK_NUGET_VERSION/g" "$PACKAGE_LAYOUT_DIR"/debian_config.json
    sed -i "s/%SHARED_FRAMEWORK_BRAND_NAME%/$SHARED_FRAMEWORK_BRAND_NAME/g" "$PACKAGE_LAYOUT_DIR"/debian_config.json
}

test_debian_package(){
    header "Testing debian Shared Framework package"

    install_bats
    run_package_integrity_tests
}

install_bats() {
    rm -rf $TEST_STAGE_DIR
    git clone https://github.com/sstephenson/bats.git $TEST_STAGE_DIR
}

run_package_integrity_tests() {
    # Set LAST_VERSION_URL to enable upgrade tests
    # export LAST_VERSION_URL="$PREVIOUS_VERSION_URL"

    $TEST_STAGE_DIR/bin/bats $PACKAGE_OUTPUT_DIR/test_package.bats
}

execute_build

DEBIAN_FILE=$(find $PACKAGE_OUTPUT_DIR -iname "*.deb")

test_debian_package

mv -f "$DEBIAN_FILE" "$OUTPUT_DEBIAN_FILE"
