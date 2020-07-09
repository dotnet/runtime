#!/usr/bin/env bats
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#

# Tests for debian_build_lib.sh

setup(){
    DIR="$BATS_TEST_DIRNAME"
    PACKAGIFY_DIR="$(readlink -f $DIR/../../)"
}

@test "manpage generation is identical to lkg file" {
    # Output is file "tool1.1"
    # LKG file is "lkgtestman.1"
    python $PACKAGIFY_DIR/scripts/manpage_generator.py $PACKAGIFY_DIR/test/test_assets/testdocs.json $PACKAGIFY_DIR/test/test_assets

    # Test Output existence
    [ -f $PACKAGIFY_DIR/test/test_assets/tool1.1 ]
    
    # Test Output matches LKG
    # If this is failing double check line ending style
    [ -z "$(diff "$PACKAGIFY_DIR/test/test_assets/tool1.1" "$PACKAGIFY_DIR/test/test_assets/lkgtestman.1")" ]

    # Cleanup
    rm $PACKAGIFY_DIR/test/test_assets/tool1.1
}
