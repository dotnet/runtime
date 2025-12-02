#!/usr/bin/env bash

# Exit code constants
readonly EXIT_CODE_SUCCESS=0       # Script ran normally.
readonly EXIT_CODE_EXCEPTION=1     # Script exited because something exceptional happened (e.g. bad arguments, Ctrl-C interrupt).
readonly EXIT_CODE_TEST_FAILURE=2  # Script completed successfully, but one or more tests failed.

scriptPath="$(cd "$(dirname "$BASH_SOURCE[0]")"; pwd -P)"
repoRootDir="$(cd "$scriptPath"/../..; pwd -P)"

# Default to python3 if it is installed
__Python=python
if command -v python3 &>/dev/null; then
    __Python=python3
fi

# Run the tests using cross platform run.py
# All argument parsing and processing is now done in run.py
$__Python "$repoRootDir/src/tests/run.py" "$@"
exit "$?"
