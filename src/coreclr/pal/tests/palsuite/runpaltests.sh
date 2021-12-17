#!/usr/bin/env bash
#
# This script executes PAL tests from the specified build location.
#

if [ $# -lt 1 -o $# -gt 3 ]
then
  echo "Usage..."
  echo "runpaltests.sh <path to root build directory of the pal tests>  [<path to test output folder>] [<path to temp folder for PAL tests>]"
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
if [ ! -e "$1" ]; then
  echo "Core_Root not found at $1"
  exit 1
fi
BUILD_ROOT_DIR="$(cd "$1"; pwd -P)"

# Create path to the compiled PAL tets in the build directory
PAL_TEST_BUILD=$BUILD_ROOT_DIR
echo Running PAL tests from $PAL_TEST_BUILD

export LD_LIBRARY_PATH=$BUILD_ROOT_DIR:$LD_LIBRARY_PATH

# Environment variable PWD contains absolute path to the current folder
# so use it to create absolute path to the file with a list of tests.
PAL_TEST_LIST=$BUILD_ROOT_DIR/paltestlist.txt
# Change current directory back to the original location
echo The list of PAL tests to run will be read from $PAL_TEST_LIST

# Create the test output root directory
if [ $# -gt 2 ]
then
  PAL_TEST_OUTPUT_DIR=$3
  mkdir -p $PAL_TEST_OUTPUT_DIR
else
  mkdir -p /tmp/PalTestOutput
  if [ ! -d /tmp/PalTestOutput ]; then
    rm -f -r /tmp/PalTestOutput
    mkdir -p /tmp/PalTestOutput
  fi
  PAL_TEST_OUTPUT_DIR=/tmp/PalTestOutput/default
fi

# Determine the folder to use for PAL test output during the run, and the folder where output files were requested to be copied.
# First check if the output folder was passed as a parameter to the script. It is supposed be the second parameter so check if
# we have more than 1 argument.
if [ $# -gt 1 ]
then
  COPY_TO_TEST_OUTPUT_DIR=$2
else
  COPY_TO_TEST_OUTPUT_DIR=$PAL_TEST_OUTPUT_DIR
fi

# Determine the folder to use for PAL test output during the run
if [ ! $# -gt 2 ]
then
  if [ "$COPY_TO_TEST_OUTPUT_DIR" != "$PAL_TEST_OUTPUT_DIR" ]; then
    # Output files were requested to be copied to a specific folder. In this mode, we need to support parallel runs of PAL tests
    # on the same machine. Make a unique temp folder for working output inside $PAL_TEST_RESULTS_DIR.
    PAL_TEST_OUTPUT_DIR=$(mktemp -d /tmp/PalTestOutput/tmp.XXXXXXXX)
  fi
fi

echo PAL tests will store their temporary files and output in $PAL_TEST_OUTPUT_DIR.
if [ "$COPY_TO_TEST_OUTPUT_DIR" != "$PAL_TEST_OUTPUT_DIR" ]; then
  echo Output files will be copied to $COPY_TO_TEST_OUTPUT_DIR at the end.
fi

# Path to a file that will contains a list PAL tests that failed during the test run.
PAL_FAILED_TEST_LIST=$PAL_TEST_OUTPUT_DIR/palfailedtests.txt

# Path to a file that will contain the XUnit style test result for Jenkins
# We use a temp file as at the end we have to prepend with the number of tests
# and failures
PAL_XUNIT_TEST_LIST_TMP=$PAL_TEST_OUTPUT_DIR/pal_tests.xml.tmp
PAL_XUNIT_TEST_LIST=$PAL_TEST_OUTPUT_DIR/pal_tests.xml

# Capturing stdout and stderr
PAL_OUT_FILE=$PAL_TEST_OUTPUT_DIR/pal_test_out

# Remove and recreate the temporary test output directory, and the directory where output files were requested to be copied.
if [[ "$COPY_TO_TEST_OUTPUT_DIR" == "$PAL_TEST_OUTPUT_DIR" ]]; then
  if [ -e $PAL_TEST_OUTPUT_DIR ]; then
    rm -f -r $PAL_TEST_OUTPUT_DIR
  fi
  mkdir -p $PAL_TEST_OUTPUT_DIR
else
  # No need to recreate the temp output directory, as mktemp would have created a unique empty directory
  if [ -e $COPY_TO_TEST_OUTPUT_DIR ]; then
    rm -f -r $COPY_TO_TEST_OUTPUT_DIR
  fi
  mkdir -p $COPY_TO_TEST_OUTPUT_DIR
  if [ ! -d $COPY_TO_TEST_OUTPUT_DIR ]; then
    echo Failed to create $COPY_TO_TEST_OUTPUT_DIR.
    COPY_TO_TEST_OUTPUT_DIR=$PAL_TEST_OUTPUT_DIR
  fi
fi

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

  # Create a folder with the test name, and use that as the working directory for the test. Many PAL tests don't clean up after
  # themselves and may leave files/directories around, but even to handle test failures that result in a dirty state, run each
  # test in its own folder.
  TEST_WORKING_DIR=$PAL_TEST_OUTPUT_DIR/$(basename $TEST_NAME)
  if [ -e $TEST_WORKING_DIR ]; then
    rm -f -r $TEST_WORKING_DIR
  fi
  mkdir $TEST_WORKING_DIR
  cd $TEST_WORKING_DIR

  # Create path to a test executable to run
  TEST_COMMAND="$PAL_TEST_BUILD/$TEST_NAME"
  if [ ! -f $TEST_COMMAND ]; then
    TEST_COMMAND="$PAL_TEST_BUILD/paltests $TEST_NAME"
  fi

  echo -n .
  STARTTIME=$(date +%s)
  # Redirect to temp file
  $TEST_COMMAND 2>&1 | tee ${PAL_OUT_FILE} ; ( exit ${PIPESTATUS[0]} )
  # Get exit code of the test process.
  TEST_EXIT_CODE=$?

  ENDTIME=$(date +%s)

  # Change back to the output directory, and remove the test's working directory if it's empty
  cd $PAL_TEST_OUTPUT_DIR
  rmdir $TEST_WORKING_DIR 2>/dev/null

  TEST_XUNIT_NAME=$(dirname $TEST_NAME)
  TEST_XUNIT_CLASSNAME=$(dirname $TEST_XUNIT_NAME)
  TEST_XUNIT_NAME=${TEST_XUNIT_NAME#*/}
  TEST_XUNIT_NAME=${TEST_XUNIT_NAME#*/}

  TEST_XUNIT_NAME=$(echo $TEST_XUNIT_NAME | tr / .)
  TEST_XUNIT_CLASSNAME=$(echo $TEST_XUNIT_CLASSNAME | tr / .)

  echo -n "<test name=\"$TEST_XUNIT_CLASSNAME.$TEST_XUNIT_NAME\" type=\"$TEST_XUNIT_CLASSNAME\" method=\"$TEST_XUNIT_NAME\" time=\"$(($ENDTIME - $STARTTIME))\" result=\"" >> $PAL_XUNIT_TEST_LIST_TMP

  # If the exit code is 0 then the test passed, otherwise record a failure.
  if [ "$TEST_EXIT_CODE" -eq "0" ]; then
    NUMBER_OF_PASSED_TESTS=$(($NUMBER_OF_PASSED_TESTS + 1))
    echo "Pass\" />" >> $PAL_XUNIT_TEST_LIST_TMP
  else
    echo "Fail\" >" >> $PAL_XUNIT_TEST_LIST_TMP
    echo "<failure exception-type=\"Exit code: $TEST_EXIT_CODE\">" >> $PAL_XUNIT_TEST_LIST_TMP
    echo "<message><![CDATA[$(cat $PAL_OUT_FILE)]]></message>" >> $PAL_XUNIT_TEST_LIST_TMP
    echo "<output><![CDATA[$(cat $PAL_OUT_FILE)]]></output>" >> $PAL_XUNIT_TEST_LIST_TMP
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

if [ "$COPY_TO_TEST_OUTPUT_DIR" != "$PAL_TEST_OUTPUT_DIR" ]; then
  mv -f $PAL_TEST_OUTPUT_DIR/* $COPY_TO_TEST_OUTPUT_DIR/
  rm -f -r $PAL_TEST_OUTPUT_DIR
  echo Copied PAL test output files to $COPY_TO_TEST_OUTPUT_DIR.
fi

# Set exit code to be equal to the number PAL tests that have failed.
# Exit code 0 indicates success.
exit $NUMBER_OF_FAILED_TESTS
