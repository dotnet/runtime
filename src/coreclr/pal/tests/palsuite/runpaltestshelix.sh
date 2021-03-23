#!/usr/bin/env bash
#
# This script executes the PAL tests from the specified build location.
#
cd $HELIX_WORKITEM_ROOT

TEST_OUTPUT_DIR_HELIX=$HELIX_WORKITEM_ROOT/testoutput
$HELIX_WORKITEM_ROOT/runpaltests.sh $HELIX_WORKITEM_ROOT $HELIX_WORKITEM_ROOT/testoutput $HELIX_WORKITEM_ROOT/testoutputtmp
exit_code_paltests=$?
cp $TEST_OUTPUT_DIR_HELIX/pal_tests.xml $HELIX_WORKITEM_ROOT/testResults.xml
exit $exit_code_paltests