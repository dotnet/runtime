#!/usr/bin/env bash

function print_usage {
    echo ''
    echo 'CoreCLR test runner script.'
    echo ''
    echo 'Typical command line:'
    echo ''
    echo 'coreclr/tests/runtest.sh'
    echo '    --testRootDir="temp/Windows_NT.x64.Debug"'
    echo '    --testNativeBinDir="coreclr/bin/obj/Linux.x64.Debug/tests"'
    echo '    --coreClrBinDir="coreclr/bin/Product/Linux.x64.Debug"'
    echo '    --mscorlibDir="windows/coreclr/bin/Product/Linux.x64.Debug"'
    echo '    --coreFxBinDir="corefx/bin/Linux.AnyCPU.Debug"'
    echo '    --coreFxNativeBinDir="corefx/bin/Linux.x64.Debug"'
    echo ''
    echo 'Required arguments:'
    echo '  --testRootDir=<path>             : Root directory of the test build (e.g. coreclr/bin/tests/Windows_NT.x64.Debug).'
    echo '  --testNativeBinDir=<path>        : Directory of the native CoreCLR test build (e.g. coreclr/bin/obj/Linux.x64.Debug/tests).'
    echo '  (Also required: Either --coreOverlayDir, or all of the switches --coreOverlayDir overrides)'
    echo ''
    echo 'Optional arguments:'
    echo '  --coreOverlayDir=<path>          : Directory containing core binaries and test dependencies. If not specified, the'
    echo '                                     default is testRootDir/Tests/coreoverlay. This switch overrides --coreClrBinDir,'
    echo '                                     --mscorlibDir, --coreFxBinDir, and --coreFxNativeBinDir.'
    echo '  --coreClrBinDir=<path>           : Directory of the CoreCLR build (e.g. coreclr/bin/Product/Linux.x64.Debug).'
    echo '  --mscorlibDir=<path>             : Directory containing the built mscorlib.dll. If not specified, it is expected to be'
    echo '                                       in the directory specified by --coreClrBinDir.'
    echo '  --coreFxBinDir="<path>[;<path>]" : List of one or more directories with CoreFX build outputs (semicolon-delimited)'
    echo '                                     (e.g. "corefx/bin/Linux.AnyCPU.Debug;corefx/bin/Unix.AnyCPU.Debug;corefx/bin/AnyOS.AnyCPU.Debug").'
    echo '                                     If files with the same name are present in multiple directories, the first one wins.'
    echo '  --coreFxNativeBinDir=<path>      : Directory of the CoreFX native build (e.g. corefx/bin/Linux.x64.Debug).'
    echo '  --testDir=<path>                 : Run tests only in the specified directory. The path is relative to the directory'
    echo '                                     specified by --testRootDir. Multiple of this switch may be specified.'
    echo '  --testDirFile=<path>             : Run tests only in the directories specified by the file at <path>. Paths are listed'
    echo '                                     one line, relative to the directory specified by --testRootDir.'
    echo '  --runFailingTestsOnly            : Run only the tests that are disabled on this platform due to unexpected failures.'
    echo '                                     Failing tests are listed in coreclr/tests/failingTestsOutsideWindows.txt, one per'
    echo '                                     line, as paths to .sh files relative to the directory specified by --testRootDir.'
    echo '  --disableEventLogging            : Disable the events logged by both VM and Managed Code'
    echo '  --sequential                     : Run tests sequentially (default is to run in parallel).'
    echo '  --playlist=<path>                : Run only the tests that are specified in the file at <path>, in the same format as'
    echo '                                     runFailingTestsOnly'
    echo '  -v, --verbose                    : Show output from each test.'
    echo '  -h|--help                        : Show usage information.'
    echo '  --useServerGC                    : Enable server GC for this test run'
    echo '  --test-env                       : Script to set environment variables for tests'
    echo '  --runcrossgentests               : Runs the ready to run tests' 
    echo '  --jitstress=<n>                  : Runs the tests with COMPlus_JitStress=n'
    echo '  --jitstressregs=<n>              : Runs the tests with COMPlus_JitStressRegs=n'
    echo '  --jitminopts                     : Runs the tests with COMPlus_JITMinOpts=1'
    echo '  --jitforcerelocs                 : Runs the tests with COMPlus_ForceRelocs=1'
    echo '  --gcstresslevel n                : Runs the tests with COMPlus_GCStress=n'
    echo '    0: None                                1: GC on all allocs and '"'easy'"' places'
    echo '    2: GC on transitions to preemptive GC  4: GC on every allowable JITed instr'
    echo '    8: GC on every allowable NGEN instr   16: GC only on a unique stack trace'
    echo '  --long-gc                        : Runs the long GC tests'
    echo '  --gcsimulator                    : Runs the GCSimulator tests'
    echo '  --show-time                      : Print execution sequence and running time for each test'
    echo '  --no-lf-conversion               : Do not execute LF conversion before running test script'
    echo '  --limitedDumpGeneration          : Enables the generation of a limited number of core dumps if test(s) crash, even if ulimit'
    echo '                                     is zero when launching this script. This option is intended for use in CI.'
    echo ''
    echo 'Runtime Code Coverage options:'
    echo '  --coreclr-coverage               : Optional argument to get coreclr code coverage reports'
    echo '  --coreclr-objs=<path>            : Location of root of the object directory'
    echo '                                     containing the linux/mac coreclr build'
    echo '  --coreclr-src=<path>             : Location of root of the directory'
    echo '                                     containing the coreclr source files'
    echo '  --coverage-output-dir=<path>     : Directory where coverage output will be written to'
    echo ''
}

function print_results {
    echo ""
    echo "======================="
    echo "     Test Results"
    echo "======================="
    echo "# CoreCLR Bin Dir  : $coreClrBinDir"
    echo "# Tests Discovered : $countTotalTests"
    echo "# Passed           : $countPassedTests"
    echo "# Failed           : $countFailedTests"
    echo "# Skipped          : $countSkippedTests"
    echo "======================="
}

# Initialize counters for bookkeeping.
countTotalTests=0
countPassedTests=0
countFailedTests=0
countSkippedTests=0

# Variables for xUnit-style XML output. XML format: https://xunit.github.io/docs/format-xml-v2.html
xunitOutputPath=
xunitTestOutputPath=

# libExtension determines extension for dynamic library files
OSName=$(uname -s)
libExtension=
case $OSName in
    Darwin)
        libExtension="dylib"
        ;;

    Linux)
        libExtension="so"
        ;;

    NetBSD)
        libExtension="so"
        ;;

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        libExtension="so"
        ;;
esac

# clean up any existing dumpling remnants from previous runs.
dumplingsListPath="$PWD/dumplings.txt"
if [ -f "$dumplingsListPath" ]; then
    rm "$dumplingsListPath"
fi

find . -type f -name "local_dumplings.txt" -exec rm {} \;

function xunit_output_begin {
    xunitOutputPath=$testRootDir/coreclrtests.xml
    xunitTestOutputPath=${xunitOutputPath}.test
    if [ -e "$xunitOutputPath" ]; then
        rm -f -r "$xunitOutputPath"
    fi
    if [ -e "$xunitTestOutputPath" ]; then
        rm -f -r "$xunitTestOutputPath"
    fi
}

function xunit_output_add_test {
    # <assemblies>
    #   <assembly>
    #     <collection>
    #       <test .../> <!-- Write this element here -->

    local scriptFilePath=$1
    local outputFilePath=$2
    local testResult=$3 # Pass, Fail, or Skip
    local testScriptExitCode=$4

    local testPath=${scriptFilePath%.sh} # Remove trailing ".sh"
    local testDir=$(dirname "$testPath")
    local testName=$(basename "$testPath")

    # Replace '/' with '.'
    testPath=$(echo "$testPath" | tr / .)
    testDir=$(echo "$testDir" | tr / .)

    local line

    line="      "
    line="${line}<test"
    line="${line} name=\"${testPath}\""
    line="${line} type=\"${testDir}\""
    line="${line} method=\"${testName}\""
    line="${line} result=\"${testResult}\""

    if [ "$testResult" == "Pass" ]; then
        line="${line}/>"
        echo "$line" >>"$xunitTestOutputPath"
        return
    fi

    line="${line}>"
    echo "$line" >>"$xunitTestOutputPath"

    line="        "
    if [ "$testResult" == "Skip" ]; then
        line="${line}<reason><![CDATA[$(cat "$outputFilePath")]]></reason>"
        echo "$line" >>"$xunitTestOutputPath"
    else
        line="${line}<failure exception-type=\"Exit code: ${testScriptExitCode}\">"
        echo "$line" >>"$xunitTestOutputPath"

        line="          "
        line="${line}<message>"
        echo "$line" >>"$xunitTestOutputPath"
        line="            "
        line="${line}<![CDATA["
        echo "$line" >>"$xunitTestOutputPath"
        cat "$outputFilePath" >>"$xunitTestOutputPath"
        line="            "
        line="${line}]]>"
        echo "$line" >>"$xunitTestOutputPath"
        line="          "
        line="${line}</message>"
        echo "$line" >>"$xunitTestOutputPath"

        line="        "
        line="${line}</failure>"
        echo "$line" >>"$xunitTestOutputPath"
    fi

    line="      "
    line="${line}</test>"
    echo "$line" >>"$xunitTestOutputPath"
}

function xunit_output_end {
    local errorSource=$1
    local errorMessage=$2

    local errorCount
    if [ -z "$errorSource" ]; then
        ((errorCount = 0))
    else
        ((errorCount = 1))
    fi

    echo '<?xml version="1.0" encoding="utf-8"?>' >>"$xunitOutputPath"
    echo '<assemblies>' >>"$xunitOutputPath"

    local line

    # <assembly ...>
    line="  "
    line="${line}<assembly"
    line="${line} name=\"CoreClrTestAssembly\""
    line="${line} total=\"${countTotalTests}\""
    line="${line} passed=\"${countPassedTests}\""
    line="${line} failed=\"${countFailedTests}\""
    line="${line} skipped=\"${countSkippedTests}\""
    line="${line} errors=\"${errorCount}\""
    line="${line}>"
    echo "$line" >>"$xunitOutputPath"

    # <collection ...>
    line="    "
    line="${line}<collection"
    line="${line} name=\"CoreClrTestCollection\""
    line="${line} total=\"${countTotalTests}\""
    line="${line} passed=\"${countPassedTests}\""
    line="${line} failed=\"${countFailedTests}\""
    line="${line} skipped=\"${countSkippedTests}\""
    line="${line}>"
    echo "$line" >>"$xunitOutputPath"

    # <test .../> <test .../> ...
    if [ -f "$xunitTestOutputPath" ]; then
        cat "$xunitTestOutputPath" >>"$xunitOutputPath"
        rm -f "$xunitTestOutputPath"
    fi

    # </collection>
    line="    "
    line="${line}</collection>"
    echo "$line" >>"$xunitOutputPath"

    if [ -n "$errorSource" ]; then
        # <errors>
        line="    "
        line="${line}<errors>"
        echo "$line" >>"$xunitOutputPath"

        # <error ...>
        line="      "
        line="${line}<error"
        line="${line} type=\"TestHarnessError\""
        line="${line} name=\"${errorSource}\""
        line="${line}>"
        echo "$line" >>"$xunitOutputPath"

        # <failure .../>
        line="        "
        line="${line}<failure>${errorMessage}</failure>"
        echo "$line" >>"$xunitOutputPath"

        # </error>
        line="      "
        line="${line}</error>"
        echo "$line" >>"$xunitOutputPath"

        # </errors>
        line="    "
        line="${line}</errors>"
        echo "$line" >>"$xunitOutputPath"
    fi

    # </assembly>
    line="  "
    line="${line}</assembly>"
    echo "$line" >>"$xunitOutputPath"

    # </assemblies>
    echo '</assemblies>' >>"$xunitOutputPath"
}

function exit_with_error {
    local errorSource=$1
    local errorMessage=$2
    local printUsage=$3

    if [ -z "$printUsage" ]; then
        ((printUsage = 0))
    fi

    echo "$errorMessage"
    xunit_output_end "$errorSource" "$errorMessage"
    if ((printUsage != 0)); then
        print_usage
    fi
    exit $EXIT_CODE_EXCEPTION
}

# Handle Ctrl-C. We will stop execution and print the results that
# we gathered so far.
function handle_ctrl_c {
    local errorSource='handle_ctrl_c'

    echo ""
    echo "*** Stopping... ***"
    print_results
    exit_with_error "$errorSource" "Test run aborted by Ctrl+C."
}

# Register the Ctrl-C handler
trap handle_ctrl_c INT

function create_core_overlay {
    local errorSource='create_core_overlay'
    local printUsage=1

    if [ -n "$coreOverlayDir" ]; then
        export CORE_ROOT="$coreOverlayDir"
        return
    fi

    # Check inputs to make sure we have enough information to create the core layout. $testRootDir/Tests/Core_Root should
    # already exist and contain test dependencies that are not built.
    local testDependenciesDir=$testRootDir/Tests/Core_Root
    if [ ! -d "$testDependenciesDir" ]; then
        exit_with_error "$errorSource" "Did not find the test dependencies directory: $testDependenciesDir"
    fi
    if [ -z "$coreClrBinDir" ]; then
        exit_with_error "$errorSource" "One of --coreOverlayDir or --coreClrBinDir must be specified." "$printUsage"
    fi
    if [ ! -d "$coreClrBinDir" ]; then
        exit_with_error "$errorSource" "Directory specified by --coreClrBinDir does not exist: $coreClrBinDir"
    fi
    if [ ! -f "$mscorlibDir/mscorlib.dll" ]; then
        exit_with_error "$errorSource" "mscorlib.dll was not found in: $mscorlibDir"
    fi
    if [ -z "$coreFxBinDir" ]; then
        exit_with_error "$errorSource" "One of --coreOverlayDir or --coreFxBinDir must be specified." "$printUsage"
    fi
    if [ -z "$coreFxNativeBinDir" ]; then
        exit_with_error "$errorSource" "One of --coreOverlayDir or --coreFxBinDir must be specified." "$printUsage"
    fi
    if [ ! -d "$coreFxNativeBinDir/Native" ]; then
        exit_with_error "$errorSource" "Directory specified by --coreNativeFxBinDir does not exist: $coreFxNativeBinDir/Native"
    fi

    # Create the overlay
    coreOverlayDir=$testRootDir/Tests/coreoverlay
    export CORE_ROOT="$coreOverlayDir"
    if [ -e "$coreOverlayDir" ]; then
        rm -f -r "$coreOverlayDir"
    fi
    mkdir "$coreOverlayDir"

    while IFS=';' read -ra coreFxBinDirectories; do
        for currDir in "${coreFxBinDirectories[@]}"; do
            if [ ! -d "$currDir" ]; then
                exit_with_error "$errorSource" "Directory specified in --coreFxBinDir does not exist: $currDir"
            fi
            pushd $currDir > /dev/null
            for dirName in $(find . -iname '*.dll' \! -iwholename '*test*' \! -iwholename '*/ToolRuntime/*' \! -iwholename '*/RemoteExecutorConsoleApp/*' \! -iwholename '*/net*' \! -iwholename '*aot*' -exec dirname {} \; | uniq | sed 's/\.\/\(.*\)/\1/g'); do
                cp -n -v "$currDir/$dirName/$dirName.dll" "$coreOverlayDir/"
            done
            popd $currDur > /dev/null
        done
    done <<< $coreFxBinDir

    cp -f -v "$coreFxNativeBinDir/Native/"*."$libExtension" "$coreOverlayDir/" 2>/dev/null

    cp -f -v "$coreClrBinDir/"* "$coreOverlayDir/" 2>/dev/null
    cp -f -v "$mscorlibDir/mscorlib.dll" "$coreOverlayDir/"
    cp -n -v "$testDependenciesDir"/* "$coreOverlayDir/" 2>/dev/null
    if [ -f "$coreOverlayDir/mscorlib.ni.dll" ]; then
        # Test dependencies come from a Windows build, and mscorlib.ni.dll would be the one from Windows
        rm -f "$coreOverlayDir/mscorlib.ni.dll"
    fi
}

function precompile_overlay_assemblies {

    if [ $doCrossgen == 1 ]; then

        local overlayDir=$CORE_ROOT

        filesToPrecompile=$(ls -trh $overlayDir/*.dll)
        for fileToPrecompile in ${filesToPrecompile}
        do
            local filename=${fileToPrecompile}
            # Precompile any assembly except mscorlib since we already have its NI image available.
            if [[ "$filename" != *"mscorlib.dll"* ]]; then
                if [[ "$filename" != *"mscorlib.ni.dll"* ]]; then
                    echo Precompiling $filename
                    $overlayDir/crossgen /Platform_Assemblies_Paths $overlayDir $filename 2>/dev/null
                    local exitCode=$?
                    if [ $exitCode == -2146230517 ]; then
                        echo $filename is not a managed assembly.
                    elif [ $exitCode != 0 ]; then
                        echo Unable to precompile $filename.
                    else
                        echo Successfully precompiled $filename
                    fi
                fi
            fi
        done
    else
        echo Skipping crossgen of FX assemblies.
    fi
}

function copy_test_native_bin_to_test_root {
    local errorSource='copy_test_native_bin_to_test_root'

    if [ -z "$testNativeBinDir" ]; then
        exit_with_error "$errorSource" "--testNativeBinDir is required."
    fi
    testNativeBinDir=$testNativeBinDir/src
    if [ ! -d "$testNativeBinDir" ]; then
        exit_with_error "$errorSource" "Directory specified by --testNativeBinDir does not exist: $testNativeBinDir"
    fi

    # Copy native test components from the native test build into the respective test directory in the test root directory
    find "$testNativeBinDir" -type f -iname '*.$libExtension' |
        while IFS='' read -r filePath || [ -n "$filePath" ]; do
            local dirPath=$(dirname "$filePath")
            local destinationDirPath=${testRootDir}${dirPath:${#testNativeBinDir}}
            if [ ! -d "$destinationDirPath" ]; then
                exit_with_error "$errorSource" "Cannot copy native test bin '$filePath' to '$destinationDirPath/', as the destination directory does not exist."
            fi
            cp -f "$filePath" "$destinationDirPath/"
        done
}

# Variables for unsupported and failing tests
declare -a unsupportedTests
declare -a failingTests
declare -a playlistTests
((runFailingTestsOnly = 0))

# Get an array of items by reading the specified file line by line.
function read_array {
    local theArray=()

    # bash in Mac OS X doesn't support 'readarray', so using alternate way instead.
    # readarray -t theArray < "$1"
    while IFS='' read -r line || [ -n "$line" ]; do
        theArray[${#theArray[@]}]=$line
    done < "$1"
    echo ${theArray[@]}
}

function load_unsupported_tests {
    # Load the list of tests that are not supported on this platform. These tests are disabled (skipped) permanently.
    unsupportedTests=($(read_array "$(dirname "$0")/testsUnsupportedOutsideWindows.txt"))
}

function load_failing_tests {
    # Load the list of tests that fail on this platform. These tests are disabled (skipped) temporarily, pending investigation.
    failingTests=($(read_array "$(dirname "$0")/testsFailingOutsideWindows.txt"))
}

function load_playlist_tests {
    # Load the list of tests that are enabled as a part of this test playlist.
    playlistTests=($(read_array "${playlistFile}"))
}

function is_unsupported_test {
    for unsupportedTest in "${unsupportedTests[@]}"; do
        if [ "$1" == "$unsupportedTest" ]; then
            return 0
        fi
    done
    return 1
}

function is_failing_test {
    for failingTest in "${failingTests[@]}"; do
        if [ "$1" == "$failingTest" ]; then
            return 0
        fi
    done
    return 1
}

function is_playlist_test {
    for playlistTest in "${playlistTests[@]}"; do
        if [ "$1" == "$playlistTest" ]; then
            return 0
        fi
    done
    return 1
}

function skip_unsupported_test {
    # This function runs in a background process. It should not echo anything, and should not use global variables. This
    # function is analogous to run_test, and causes the test to be skipped with the message below.

    local scriptFilePath=$1
    local outputFilePath=$2

    echo "Not supported on this platform." >"$outputFilePath"
    return 2 # skip the test
}

function skip_failing_test {
    # This function runs in a background process. It should not echo anything, and should not use global variables. This
    # function is analogous to run_test, and causes the test to be skipped with the message below.

    local scriptFilePath=$1
    local outputFilePath=$2

    echo "Temporarily disabled on this platform due to unexpected failures." >"$outputFilePath"
    return 2 # skip the test
}

function skip_non_playlist_test {
    # This function runs in a background process. It should not echo anything, and should not use global variables. This
    # function is analogous to run_test, and causes the test to be skipped with the message below.

    local scriptFilePath=$1
    local outputFilePath=$2

    echo "Test is not included in the running playlist." >"$outputFilePath"
    return 2 # skip the test
}

function set_up_core_dump_generation {
    # We will only enable dump generation here if we're on Mac or Linux
    if [[ ! ( "$(uname -s)" == "Darwin" || "$(uname -s)" == "Linux" ) ]]; then
        return
    fi

    # We won't enable dump generation on OS X/macOS if the machine hasn't been
    # configured with the kern.corefile pattern we expect.
    if [[ ( "$(uname -s)" == "Darwin" && "$(sysctl -n kern.corefile)" != "core.%P" ) ]]; then
        echo "WARNING: Core dump generation not being enabled due to unexpected kern.corefile value."
        return
    fi

    # Allow dump generation
    ulimit -c unlimited

    if [ "$(uname -s)" == "Linux" ]; then
        if [ -e /proc/self/coredump_filter ]; then
            # Include memory in private and shared file-backed mappings in the dump.
            # This ensures that we can see disassembly from our shared libraries when
            # inspecting the contents of the dump. See 'man core' for details.
            echo 0x3F > /proc/self/coredump_filter
        fi
    fi
}

function print_info_from_core_file {
    local core_file_name=$1
    local executable_name=$2

    if ! [ -e $executable_name ]; then
        echo "Unable to find executable $executable_name"
        return
    elif ! [ -e $core_file_name ]; then
        echo "Unable to find core file $core_file_name"
        return
    fi

    # Use LLDB to inspect the core dump on Mac, and GDB everywhere else.
    if [[ "$OSName" == "Darwin" ]]; then
        hash lldb 2>/dev/null || { echo >&2 "LLDB was not found. Unable to print core file."; return; }

        echo "Printing info from core file $core_file_name"
        lldb -c $core_file_name -b -o 'bt'
    else
        # Use GDB to print the backtrace from the core file.
        hash gdb 2>/dev/null || { echo >&2 "GDB was not found. Unable to print core file."; return; }

        echo "Printing info from core file $core_file_name"
        gdb --batch -ex "thread apply all bt full" -ex "quit" $executable_name $core_file_name
    fi
}

function download_dumpling_script {
    echo "Downloading latest version of dumpling script."
    wget "https://raw.githubusercontent.com/Microsoft/dotnet-reliability/master/src/triage.python/dumpling.py"

    local dumpling_script="dumpling.py"
    chmod +x $dumpling_script
}

function upload_core_file_to_dumpling {
    local core_file_name=$1
    local dumpling_script="dumpling.py"
    local dumpling_file="local_dumplings.txt"

    # dumpling requires that the file exist before appending.
    touch ./$dumpling_file

    if [ ! -x $dumpling_script ]; then
        download_dumpling_script
    fi

    if [ ! -x $dumpling_script ]; then
        echo "Failed to download dumpling script. Dump cannot be uploaded."
        return
    fi

    echo "Uploading $core_file_name to dumpling service."

    local paths_to_add=""
    if [ -d "$coreClrBinDir" ]; then
        echo "Uploading CoreCLR binaries with dump."
        paths_to_add=$coreClrBinDir
    fi

    # The output from this will include a unique ID for this dump.
    ./$dumpling_script "--corefile" "$core_file_name" "upload" "--addpaths" $paths_to_add "--squelch" | tee -a $dumpling_file
}

function preserve_core_file {
    local core_file_name=$1
    local storage_location="/tmp/coredumps_coreclr"

    # Create the directory (this shouldn't fail even if it already exists).
    mkdir -p $storage_location

    # Only preserve the dump if the directory is empty. Otherwise, do nothing.
    # This is a way to prevent us from storing/uploading too many dumps.
    if [ ! "$(ls -A $storage_location)" ]; then
        echo "Copying core file $core_file_name to $storage_location"
        cp $core_file_name $storage_location

        upload_core_file_to_dumpling $core_file_name
    fi
}

function inspect_and_delete_core_files {
    # This function prints some basic information from core files in the current
    # directory and deletes them immediately. Based on the state of the system, it may
    # also upload a core file to the dumpling service.
    # (see preserve_core_file).
    
    # Depending on distro/configuration, the core files may either be named "core"
    # or "core.<PID>" by default. We will read /proc/sys/kernel/core_uses_pid to 
    # determine which one it is.
    # On OS X/macOS, we checked the kern.corefile value before enabling core dump
    # generation, so we know it always includes the PID.
    local core_name_uses_pid=0
    if [[ (( -e /proc/sys/kernel/core_uses_pid ) && ( "1" == $(cat /proc/sys/kernel/core_uses_pid) )) 
          || ( "$(uname -s)" == "Darwin" ) ]]; then
        core_name_uses_pid=1
    fi

    if [ $core_name_uses_pid == "1" ]; then
        # We don't know what the PID of the process was, so let's look at all core
        # files whose name matches core.NUMBER
        for f in core.*; do
            [[ $f =~ core.[0-9]+ ]] && print_info_from_core_file "$f" $CORE_ROOT/"corerun" && preserve_core_file "$f" && rm "$f"
        done
    elif [ -f core ]; then
        print_info_from_core_file "core" $CORE_ROOT/"corerun"
        preserve_core_file "core"
        rm "core"
    fi
}

function run_test {
    # This function runs in a background process. It should not echo anything, and should not use global variables.

    local scriptFilePath=$1
    local outputFilePath=$2

    # Switch to directory where the script is
    cd "$(dirname "$scriptFilePath")"

    local scriptFileName=$(basename "$scriptFilePath")
    local outputFileName=$(basename "$outputFilePath")

    if [ "$limitedCoreDumps" == "ON" ]; then
        set_up_core_dump_generation
    fi

    "./$scriptFileName" >"$outputFileName" 2>&1
    local testScriptExitCode=$?

    # We will try to print some information from generated core dumps if a debugger
    # is available, and possibly store a dump in a non-transient location.
    if [ "$limitedCoreDumps" == "ON" ]; then
        inspect_and_delete_core_files
    fi

    return $testScriptExitCode
}

# Variables for running tests in the background
if [ `uname` = "NetBSD" ]; then
    NumProc=$(getconf NPROCESSORS_ONLN)
else
    NumProc=$(getconf _NPROCESSORS_ONLN)
fi
((maxProcesses = $NumProc * 3 / 2)) # long tests delay process creation, use a few more processors

((nextProcessIndex = 0))
((processCount = 0))
declare -a scriptFilePaths
declare -a outputFilePaths
declare -a processIds
declare -a testStartTimes

function finish_test {
    wait ${processIds[$nextProcessIndex]}
    local testScriptExitCode=$?
    ((--processCount))

    local scriptFilePath=${scriptFilePaths[$nextProcessIndex]}
    local outputFilePath=${outputFilePaths[$nextProcessIndex]}
    local scriptFileName=$(basename "$scriptFilePath")

    local testEndTime=
    local testRunningTime=
    local header=

    if [ "$showTime" == "ON" ]; then
        testEndTime=$(date +%s)
        testRunningTime=$(echo "$testEndTime - ${testStartTimes[$nextProcessIndex]}" | bc)
        header=$(printf "[%03d:%4.0fs] " "$countTotalTests" "$testRunningTime")
    fi

    local xunitTestResult
    case $testScriptExitCode in
        0)
            let countPassedTests++
            xunitTestResult='Pass'
            if ((verbose == 1 || runFailingTestsOnly == 1)); then
                echo "PASSED   - ${header}${scriptFilePath}"
            else
                echo "         - ${header}${scriptFilePath}"
            fi
            ;;
        2)
            let countSkippedTests++
            xunitTestResult='Skip'
            echo "SKIPPED  - ${header}${scriptFilePath}"
            ;;
        *)
            let countFailedTests++
            xunitTestResult='Fail'
            echo "FAILED   - ${header}${scriptFilePath}"
            ;;
    esac
    let countTotalTests++

    if ((verbose == 1 || testScriptExitCode != 0)); then
        while IFS='' read -r line || [ -n "$line" ]; do
            echo "               $line"
        done <"$outputFilePath"
    fi

    xunit_output_add_test "$scriptFilePath" "$outputFilePath" "$xunitTestResult" "$testScriptExitCode"
}

function finish_remaining_tests {
    # Finish the remaining tests in the order in which they were started
    if ((nextProcessIndex >= processCount)); then
        ((nextProcessIndex = 0))
    fi
    while ((processCount > 0)); do
        finish_test
        ((nextProcessIndex = (nextProcessIndex + 1) % maxProcesses))
    done
    ((nextProcessIndex = 0))
}

function prep_test {
    local scriptFilePath=$1

    test "$verbose" == 1 && echo "Preparing $scriptFilePath"

    if [ ! "$noLFConversion" == "ON" ]; then
        # Convert DOS line endings to Unix if needed
        perl -pi -e 's/\r\n|\n|\r/\n/g' "$scriptFilePath"
    fi
        
    # Add executable file mode bit if needed
    chmod +x "$scriptFilePath"

    #remove any NI and Locks
    rm -f *.ni.*
    rm -rf lock
}

function start_test {
    local scriptFilePath=$1
    if ((runFailingTestsOnly == 1)) && ! is_failing_test "$scriptFilePath"; then
        return
    fi

    # Skip any test that's not in the current playlist, if a playlist was
    # given to us.
    if [ -n "$playlistFile" ] && ! is_playlist_test "$scriptFilePath"; then
        return
    fi

    if ((nextProcessIndex < processCount)); then
        finish_test
    fi

    scriptFilePaths[$nextProcessIndex]=$scriptFilePath
    local scriptFileName=$(basename "$scriptFilePath")
    local outputFilePath=$(dirname "$scriptFilePath")/${scriptFileName}.out
    outputFilePaths[$nextProcessIndex]=$outputFilePath

    if [ "$showTime" == "ON" ]; then
        testStartTimes[$nextProcessIndex]=$(date +%s)
    fi

    test "$verbose" == 1 && echo "Starting $scriptFilePath"
    if is_unsupported_test "$scriptFilePath"; then
        skip_unsupported_test "$scriptFilePath" "$outputFilePath" &
    elif ((runFailingTestsOnly == 0)) && is_failing_test "$scriptFilePath"; then
        skip_failing_test "$scriptFilePath" "$outputFilePath" &
    else
        run_test "$scriptFilePath" "$outputFilePath" &
    fi
    processIds[$nextProcessIndex]=$!

    ((nextProcessIndex = (nextProcessIndex + 1) % maxProcesses))
    ((++processCount))
}

# Get a list of directories in which to scan for tests by reading the
# specified file line by line.
function set_test_directories {
    local errorSource='set_test_directories'

    local listFileName=$1

    if [ ! -f "$listFileName" ]
    then
        exit_with_error "$errorSource" "Test directories file not found at $listFileName"
    fi
    testDirectories=($(read_array "$listFileName"))
}

function run_tests_in_directory {
    local testDir=$1

    # Recursively search through directories for .sh files to prepare them.
    # Note: This needs to occur before any test runs as some of the .sh files
    # depend on other .sh files
    for scriptFilePath in $(find "$testDir" -type f -iname '*.sh' | sort)
    do
        prep_test "${scriptFilePath:2}"
    done
    echo "The tests have been prepared"
    # Recursively search through directories for .sh files to run.
    for scriptFilePath in $(find "$testDir" -type f -iname '*.sh' | sort)
    do
        start_test "${scriptFilePath:2}"
    done
}

function coreclr_code_coverage()
{

  local coverageDir="$coverageOutputDir/Coverage"
  local toolsDir="$coverageOutputDir/Coverage/tools"
  local reportsDir="$coverageOutputDir/Coverage/reports"
  local packageName="unix-code-coverage-tools.1.0.0.nupkg"
  rm -rf $coverageDir
  mkdir -p $coverageDir
  mkdir -p $toolsDir
  mkdir -p $reportsDir
  pushd $toolsDir > /dev/null

  echo "Pulling down code coverage tools"
  wget -q https://www.myget.org/F/dotnet-buildtools/api/v2/package/unix-code-coverage-tools/1.0.0 -O $packageName
  echo "Unzipping to $toolsDir"
  unzip -q -o $packageName

  # Invoke gcovr
  chmod a+rwx ./gcovr
  chmod a+rwx ./$OSName/llvm-cov

  echo
  echo "Generating coreclr code coverage reports at $reportsDir/coreclr.html"
  echo "./gcovr $coreClrObjs --gcov-executable=$toolsDir/$OS/llvm-cov -r $coreClrSrc --html --html-details -o $reportsDir/coreclr.html"
  echo
  ./gcovr $coreClrObjs --gcov-executable=$toolsDir/$OSName/llvm-cov -r $coreClrSrc --html --html-details -o $reportsDir/coreclr.html
  exitCode=$?
  popd > /dev/null
  exit $exitCode
}

# Exit code constants
readonly EXIT_CODE_SUCCESS=0       # Script ran normally.
readonly EXIT_CODE_EXCEPTION=1     # Script exited because something exceptional happened (e.g. bad arguments, Ctrl-C interrupt).
readonly EXIT_CODE_TEST_FAILURE=2  # Script completed successfully, but one or more tests failed.

# Argument variables
testRootDir=
testNativeBinDir=
coreOverlayDir=
coreClrBinDir=
mscorlibDir=
coreFxBinDir=
coreFxNativeBinDir=
coreClrObjs=
coreClrSrc=
coverageOutputDir=
testEnv=
playlistFile=
showTime=
noLFConversion=
gcsimulator=
longgc=
limitedCoreDumps=

((disableEventLogging = 0))
((serverGC = 0))

# Handle arguments
verbose=0
doCrossgen=0

for i in "$@"
do
    case $i in
        -h|--help)
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
        -v|--verbose)
            verbose=1
            ;;
        --crossgen)
            doCrossgen=1
            ;;
        --jitstress=*)
            export COMPlus_JitStress=${i#*=}
            ;;
        --jitstressregs=*)
            export COMPlus_JitStressRegs=${i#*=}
            ;;
        --jitminopts)
            export COMPlus_JITMinOpts=1
            ;;
        --jitforcerelocs)
            export COMPlus_ForceRelocs=1
            ;;
        --testRootDir=*)
            testRootDir=${i#*=}
            ;;
        --testNativeBinDir=*)
            testNativeBinDir=${i#*=}
            ;;
        --coreOverlayDir=*)
            coreOverlayDir=${i#*=}
            ;;
        --coreClrBinDir=*)
            coreClrBinDir=${i#*=}
            ;;
        --mscorlibDir=*)
            mscorlibDir=${i#*=}
            ;;
        --coreFxBinDir=*)
            coreFxBinDir=${i#*=}
            ;;
        --coreFxNativeBinDir=*)
            coreFxNativeBinDir=${i#*=}
            ;;
        --testDir=*)
            testDirectories[${#testDirectories[@]}]=${i#*=}
            ;;
        --testDirFile=*)
            set_test_directories "${i#*=}"
            ;;
        --runFailingTestsOnly)
            ((runFailingTestsOnly = 1))
            ;;
        --disableEventLogging)
            ((disableEventLogging = 1))
            ;;
        --runcrossgentests)
            export RunCrossGen=1
            ;;
        --sequential)
            ((maxProcesses = 1))
            ;;
        --useServerGC)
            ((serverGC = 1))
            ;;
        --long-gc)
            ((longgc = 1))
            ;;
        --gcsimulator)
            ((gcsimulator = 1))
            ;;
        --playlist=*)
            playlistFile=${i#*=}
            ;;
        --coreclr-coverage)
            CoreClrCoverage=ON
            ;;
        --coreclr-objs=*)
            coreClrObjs=${i#*=}
            ;;
        --coreclr-src=*)
            coreClrSrc=${i#*=}
            ;;
        --coverage-output-dir=*)
            coverageOutputDir=${i#*=}
            ;;
        --test-env=*)
            testEnv=${i#*=}
            ;;            
        --gcstresslevel=*)
            export COMPlus_GCStress=${i#*=}
            ;;            
        --show-time)
            showTime=ON
            ;;
        --no-lf-conversion)
            noLFConversion=ON
            ;;
        --limitedDumpGeneration)
            limitedCoreDumps=ON
            ;;
        *)
            echo "Unknown switch: $i"
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
    esac
done

if ((disableEventLogging == 0)); then
    export COMPlus_EnableEventLog=1
fi

export CORECLR_SERVER_GC="$serverGC"

if [ -z "$testRootDir" ]; then
    echo "--testRootDir is required."
    print_usage
    exit $EXIT_CODE_EXCEPTION
fi
if [ ! -d "$testRootDir" ]; then
    echo "Directory specified by --testRootDir does not exist: $testRootDir"
    exit $EXIT_CODE_EXCEPTION
fi

# Copy native interop test libraries over to the mscorlib path in
# order for interop tests to run on linux.
if [ -z "$mscorlibDir" ]; then
    mscorlibDir=$coreClrBinDir
fi
if [ -d "$mscorlibDir" ] && [ -d "$mscorlibDir/bin" ]; then
    cp $mscorlibDir/bin/* $mscorlibDir
fi

if [ ! -z "$longgc" ]; then
    echo "Running Long GC tests"
    export RunningLongGCTests=1
fi

if [ ! -z "$gcsimulator" ]; then
    echo "Running GC simulator tests"
    export RunningGCSimulatorTests=1
fi

# If this is a coverage run, make sure the appropriate args have been passed
if [ "$CoreClrCoverage" == "ON" ]
then
    echo "Code coverage is enabled for this run"
    echo ""
    if [ ! "$OSName" == "Darwin" ] && [ ! "$OSName" == "Linux" ]
    then
        echo "Code Coverage not supported on $OS"
        exit 1
    fi

    if [ -z "$coreClrObjs" ]
    then
        echo "Coreclr obj files are required to generate code coverage reports"
        echo "Coreclr obj files root path can be passed using '--coreclr-obj' argument"
        exit 1
    fi

    if [ -z "$coreClrSrc" ]
    then
        echo "Coreclr src files are required to generate code coverage reports"
        echo "Coreclr src files root path can be passed using '--coreclr-src' argument"
        exit 1
    fi

    if [ -z "$coverageOutputDir" ]
    then
        echo "Output directory for coverage results must be specified"
        echo "Output path can be specified '--coverage-output-dir' argument"
        exit 1
    fi
fi

xunit_output_begin
create_core_overlay
precompile_overlay_assemblies
copy_test_native_bin_to_test_root

if [ -n "$playlistFile" ]
then
    # Use a playlist file exclusively, if it was provided
    echo "Executing playlist $playlistFile"
    load_playlist_tests
else
    load_unsupported_tests
    load_failing_tests
fi

scriptPath=$(dirname $0)
${scriptPath}/setup-runtime-dependencies.sh --outputDir=$coreOverlayDir

export __TestEnv=$testEnv

cd "$testRootDir"
time_start=$(date +"%s")
if [ -z "$testDirectories" ]
then
    # No test directories were specified, so run everything in the current
    # directory and its subdirectories.
    run_tests_in_directory "."
else
    # Otherwise, run all the tests in each specified test directory.
    for testDir in "${testDirectories[@]}"
    do
        if [ ! -d "$testDir" ]; then
            echo "Test directory does not exist: $testDir"
        else
            run_tests_in_directory "./$testDir"
        fi
    done
fi
finish_remaining_tests

print_results

echo "constructing $dumplingsListPath"
find . -type f -name "local_dumplings.txt" -exec cat {} \; > $dumplingsListPath

if [ -s $dumplingsListPath ]; then
    cat $dumplingsListPath
else
    rm $dumplingsListPath
fi

time_end=$(date +"%s")
time_diff=$(($time_end-$time_start))
echo "$(($time_diff / 60)) minutes and $(($time_diff % 60)) seconds taken to run CoreCLR tests."

xunit_output_end

if [ "$CoreClrCoverage" == "ON" ]
then
    coreclr_code_coverage
fi

if ((countFailedTests > 0)); then
    exit $EXIT_CODE_TEST_FAILURE
fi

exit $EXIT_CODE_SUCCESS
