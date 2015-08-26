#!/usr/bin/env bash
#
# This script executes PAL tests from the specified build location.
#

if [ $# -lt 1 -o $# -gt 3 ]
then
  echo "Usage..."
  echo "runpaltests.sh <path to root build directory> [<path to temp folder for PAL tests>]"
  echo
  echo "For example:"
  echo "runpaltests.sh /projectk/build/debug"
  echo
  exit 1
fi

echo
echo "***** Testing PAL *****"
echo

# Store the location of the root of build directory
BUILD_ROOD_DIR=$1
# Create path to the compiled PAL tets in the build directory
PAL_TEST_BUILD=$BUILD_ROOD_DIR/src/pal/tests/palsuite
echo Running PAL tests from $PAL_TEST_BUILD

# Create absolute path to the file that contains a list of PAL tests to execute.
# This file is located next to this script in the source tree
RELATIVE_PATH_TO_PAL_TESTS=$0
# Remove the name of this script from the path
RELATIVE_PATH_TO_PAL_TESTS=${RELATIVE_PATH_TO_PAL_TESTS%/*.*}
# Change current directory to the location of this script
cd $RELATIVE_PATH_TO_PAL_TESTS
# Environment variable PWD contains absolute path to the current folder
# so use it to create absolute path to the file with a list of tests.
PAL_TEST_LIST=$PWD/paltestlist.txt
# Change current directory back to the original location
cd $OLDPWD
echo The list of PAL tests to run will be read from $PAL_TEST_LIST

# Create path to a folder where PAL tests will store their output
# First check if the output folder was passed as a parameter to the script.
# It is supposed be the second parameter so check if we have more than 1 argument.
if [ $# -gt 1 ]
then
PAL_TEST_OUTPUT_DIR=$2
else
PAL_TEST_OUTPUT_DIR=/tmp/PalTestOutput
fi
echo PAL tests will store their temporary files and output in $PAL_TEST_OUTPUT_DIR

# Path to a file that will contains a list PAL tests that failed during the test run.
PAL_FAILED_TEST_LIST=$PAL_TEST_OUTPUT_DIR/palfailedtests.txt

# Path to a file that will contain the XUnit style test result for Jenkins
# We use a temp file as at the end we have to prepend with the number of tests
# and failures
PAL_XUNIT_TEST_LIST_TMP=$PAL_TEST_OUTPUT_DIR/pal_tests.xml.tmp
PAL_XUNIT_TEST_LIST=$PAL_TEST_OUTPUT_DIR/pal_tests.xml

# Capturing stdout and stderr
PAL_OUT_FILE=$PAL_TEST_OUTPUT_DIR/pal_test_out

# Remove the temporary test output directory
rm -r $PAL_TEST_OUTPUT_DIR
mkdir $PAL_TEST_OUTPUT_DIR
cd $PAL_TEST_OUTPUT_DIR

echo
echo "Running tests..."
echo

NUMBER_OF_PASSED_TESTS=0
NUMBER_OF_FAILED_TESTS=0

# Read PAL tests names from the $PAL_TEST_LIST file and run them one by one.
while read TEST_NAME
do
  # Remove stdout/stderr file if it exists
  rm -f $PAL_OUT_FILE

  # Create path to a test executable to run
  TEST_COMMAND="$PAL_TEST_BUILD/$TEST_NAME"
  echo -n .
  # Redirect to temp file
  $TEST_COMMAND 2>&1 | tee ${PAL_OUT_FILE} ; ( exit ${PIPESTATUS[0]} )

  # Get exit code of the test process.
  TEST_EXIT_CODE=$?
  
  TEST_XUNIT_NAME=$(dirname $TEST_NAME)
  TEST_XUNIT_CLASSNAME=$(dirname $TEST_XUNIT_NAME)
  TEST_XUNIT_NAME=${TEST_XUNIT_NAME#*/}
  TEST_XUNIT_NAME=${TEST_XUNIT_NAME#*/}
  
  TEST_XUNIT_NAME=$(echo $TEST_XUNIT_NAME | tr / .)
  TEST_XUNIT_CLASSNAME=$(echo $TEST_XUNIT_CLASSNAME | tr / .)
  
  echo -n "<test name=\"$TEST_XUNIT_CLASSNAME.$TEST_XUNIT_NAME\" type=\"$TEST_XUNIT_CLASSNAME\" method=\"$TEST_XUNIT_NAME\" result=\"" >> $PAL_XUNIT_TEST_LIST_TMP

  # If the exit code is 0 then the test passed, otherwise record a failure.
  if [ "$TEST_EXIT_CODE" -eq "0" ]; then
    NUMBER_OF_PASSED_TESTS=$(($NUMBER_OF_PASSED_TESTS + 1))
    echo "Pass\" />" >> $PAL_XUNIT_TEST_LIST_TMP
  else
    echo "Fail\" >" >> $PAL_XUNIT_TEST_LIST_TMP
    echo "<failure exception-type=\"Exit code: $TEST_EXIT_CODE\">" >> $PAL_XUNIT_TEST_LIST_TMP  
    echo "<message><![CDATA[$(cat $PAL_OUT_FILE)]]></message>" >> $PAL_XUNIT_TEST_LIST_TMP  
    echo "</failure>" >> $PAL_XUNIT_TEST_LIST_TMP  
    echo "</test>" >> $PAL_XUNIT_TEST_LIST_TMP
    FAILED_TEST="$TEST_NAME. Exit code: $TEST_EXIT_CODE"
    echo
	echo FAILED: $FAILED_TEST
    echo

    # Store the name of the failed test in the list of failed tests.
    echo $FAILED_TEST >> $PAL_FAILED_TEST_LIST

    NUMBER_OF_FAILED_TESTS=$(($NUMBER_OF_FAILED_TESTS + 1))
  fi
done < $PAL_TEST_LIST

# We are done running tests.
echo
echo Finished running PAL tests.
echo

# Finish XUnit file, output to finished file with the number of failures, tests etc
NUMBER_OF_TESTS=$(($NUMBER_OF_PASSED_TESTS + $NUMBER_OF_FAILED_TESTS))

XUNIT_SUFFIX="</collection>\n"
XUNIT_SUFFIX+="</assembly>\n"
XUNIT_SUFFIX+="</assemblies>"

XUNIT_PREFIX="<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
XUNIT_PREFIX+="<assemblies>\n"
XUNIT_PREFIX+="<assembly name=\"PAL\" total=\"$NUMBER_OF_TESTS\" passed=\"$NUMBER_OF_PASSED_TESTS\" failed=\"$NUMBER_OF_FAILED_TESTS\" skipped=\"0\">\n"
XUNIT_PREFIX+="<collection total=\"$NUMBER_OF_TESTS\" passed=\"$NUMBER_OF_PASSED_TESTS\" failed=\"$NUMBER_OF_FAILED_TESTS\" skipped=\"0\" name=\"palsuite\">"

printf "$XUNIT_SUFFIX" >> $PAL_XUNIT_TEST_LIST_TMP
printf "$XUNIT_PREFIX" | cat - $PAL_XUNIT_TEST_LIST_TMP > $PAL_XUNIT_TEST_LIST

# If there were tests failures then print the list of failed tests
if [ $NUMBER_OF_FAILED_TESTS -gt "0" ]; then
  echo "The following test(s) failed:"
  while read FAILED_TEST_NAME
  do
    echo $FAILED_TEST_NAME
  done < $PAL_FAILED_TEST_LIST
  echo
fi

echo PAL Test Results:
echo "  Passed: $NUMBER_OF_PASSED_TESTS"
echo "  Failed: $NUMBER_OF_FAILED_TESTS"
echo

# Set exit code to be equal to the number PAL tests that have failed.
# Exit code 0 indicates success.
exit $NUMBER_OF_FAILED_TESTS
