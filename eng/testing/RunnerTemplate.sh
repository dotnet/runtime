#!/usr/bin/env bash

usage()
{
  echo "Usage: RunTests.sh {-r|--runtime-path} <runtime-path> [{--rsp-file} <rsp-file>]"
  echo ""
  echo "Parameters:"
  echo "--runtime-path           (Mandatory) Testhost containing the test runtime used during test execution (short: -r)"
  echo "--rsp-file               RSP file to pass in additional arguments"
  echo "--help                   Print help and exit (short: -h)"
}

EXECUTION_DIR=$(dirname "$0")
RUNTIME_PATH=''
RSP_FILE=''

while [[ $# > 0 ]]; do
  opt="$(echo "${1}" | tr "[:upper:]" "[:lower:]")"
  case "$opt" in
    --help|-h)
      usage
      exit 0
      ;;
    --runtime-path|-r)
      RUNTIME_PATH=$2
      shift
      ;;
    --rsp-file)
      RSP_FILE=\@$2
      shift
      ;;
    *)
      echo "Invalid argument: $1"
      usage
      exit -1
      ;;
  esac

  shift
done

if [[ -z "$RUNTIME_PATH" ]]; then
  echo "error: -r|--runtime-path argument is required."
  usage
  exit -1
fi

# Don't use a globally installed SDK.
export DOTNET_MULTILEVEL_LOOKUP=0

exitcode_list[0]="Exited Successfully"
exitcode_list[130]="SIGINT  Ctrl-C occurred. Likely tests timed out."
exitcode_list[131]="SIGQUIT Ctrl-\ occurred. Core dumped."
exitcode_list[132]="SIGILL  Illegal Instruction. Core dumped. Likely codegen issue."
exitcode_list[133]="SIGTRAP Breakpoint hit. Core dumped."
exitcode_list[134]="SIGABRT Abort. Managed or native assert, or runtime check such as heap corruption, caused call to abort(). Core dumped."
exitcode_list[135]="IGBUS   Unaligned memory access. Core dumped."
exitcode_list[136]="SIGFPE  Bad floating point arguments. Core dumped."
exitcode_list[137]="SIGKILL Killed either due to out of memory/resources (see /var/log/messages) or by explicit kill."
exitcode_list[139]="SIGSEGV Illegal memory access. Deref invalid pointer, overrunning buffer, stack overflow etc. Core dumped."
exitcode_list[143]="SIGTERM Terminated. Usually before SIGKILL."
exitcode_list[159]="SIGSYS  Bad System Call."

function move_core_file_to_temp_location {
  local core_file_name=$1

  # Append the dmp extension to ensure XUnitLogChecker finds it
  local new_location=$HELIX_DUMP_FOLDER/$core_file_name.dmp

  echo "Copying dump file '$core_file_name' to '$new_location'"
  cp $core_file_name $new_location

  # Delete the old one
  rm $core_file_name
}

xunitlogchecker_exit_code=0
function invoke_xunitlogchecker {
  local dump_folder=$1

  total_dumps=$(find $dump_folder -name "*.dmp" | wc -l)
  
  if [[ $total_dumps > 0 ]]; then
    echo "Total dumps found in $dump_folder: $total_dumps"
    xunitlogchecker_file_name="$HELIX_CORRELATION_PAYLOAD/XUnitLogChecker.dll"
    dotnet_file_name="$RUNTIME_PATH/dotnet"

    if [[ ! -f $dotnet_file_name ]]; then
      echo "'$dotnet_file_name' was not found. Unable to run XUnitLogChecker."
      xunitlogchecker_exit_code=1
    elif [[ ! -f $xunitlogchecker_file_name ]]; then 
      echo "'$xunitlogchecker_file_name' was not found. Unable to print dump file contents."
      xunitlogchecker_exit_code=2
    elif [[ ! -d $dump_folder ]]; then
      echo "The dump directory '$dump_folder' does not exist."
    else
      echo "Executing XUnitLogChecker in $dump_folder..."
      cmd="$dotnet_file_name --roll-forward Major $xunitlogchecker_file_name --dumps-path $dump_folder"
      echo "$cmd"
      $cmd
      xunitlogchecker_exit_code=$?
    fi
  else
    echo "No dumps found in $dump_folder."
  fi
}

# ========================= BEGIN Core File Setup ============================
system_name="$(uname -s)"
if [[ $system_name == "Darwin" ]]; then
  # On OS X, we will enable core dump generation only if there are no core
  # files already in /cores/ at this point. This is being done to prevent
  # inadvertently flooding the CI machines with dumps.
  if [[ ! -d "/cores" || ! "$(ls -A /cores)" ]]; then
    # Disabling core dumps on macOS. System dumps are large (even for very small
    # programs) and not configurable. As a result, if a single PR build causes a
    # lot of tests to crash, we can take out the entire queue.
    # See discussions in:
    #   https://github.com/dotnet/core-eng/issues/15333
    #   https://github.com/dotnet/core-eng/issues/15597
    ulimit -c 0
  fi
fi

export DOTNET_DbgEnableMiniDump=1
export DOTNET_EnableCrashReport=1
export DOTNET_DbgMiniDumpName=$HELIX_DUMP_FOLDER/coredump.%d.dmp
# ========================= END Core File Setup ==============================

# ========================= BEGIN support for SuperPMI collection ==============================
if [ ! -z $spmi_enable_collection ]; then
  echo "SuperPMI collection enabled"
  # spmi_collect_dir and spmi_core_root need to be set before this script is run, if SuperPMI collection is enabled.
  if [ -z $spmi_collect_dir ]; then
    echo "ERROR - spmi_collect_dir not defined"
    exit 1
  fi
  if [ -z $spmi_core_root ]; then
    echo "ERROR - spmi_core_root not defined"
    exit 1
  fi
  mkdir -p $spmi_collect_dir
  export spmi_file_extension=so
  if [[ $system_name == "Darwin" ]]; then
    export spmi_file_extension=dylib
  fi
  export SuperPMIShimLogPath=$spmi_collect_dir
  export SuperPMIShimPath=$spmi_core_root/libclrjit.$spmi_file_extension
  export DOTNET_EnableExtraSuperPmiQueries=1
  export DOTNET_JitPath=$spmi_core_root/libsuperpmi-shim-collector.$spmi_file_extension
  if [ ! -e $SuperPMIShimPath ]; then
    echo "ERROR - $SuperPMIShimPath not found"
    exit 1
  fi
  if [ ! -e $DOTNET_JitPath ]; then
    echo "ERROR - $DOTNET_JitPath not found"
    exit 1
  fi
  echo "SuperPMIShimLogPath=$SuperPMIShimLogPath"
  echo "SuperPMIShimPath=$SuperPMIShimPath"
  echo "DOTNET_EnableExtraSuperPmiQueries=$DOTNET_EnableExtraSuperPmiQueries"
  echo "DOTNET_JitPath=$DOTNET_JitPath"
fi
# ========================= END support for SuperPMI collection ==============================

echo ========================= Begin custom configuration settings ==============================
[[SetCommandsEcho]]
[[SetCommands]]
echo ========================== End custom configuration settings ===============================

# ========================= BEGIN Test Execution =============================
echo ----- start $(date) ===============  To repro directly: =====================================================
echo pushd $EXECUTION_DIR
[[RunCommandsEcho]]
echo popd
echo ===========================================================================================================
pushd $EXECUTION_DIR
[[RunCommands]]
test_exitcode=$?
if [[ -s testResults.xml ]]; then
  has_test_results=1;
fi;
popd
echo ----- end $(date) ----- exit code $test_exitcode ----------------------------------------------------------

if [[ -n "${exitcode_list[$test_exitcode]}" ]]; then
  echo exit code $test_exitcode means ${exitcode_list[$test_exitcode]}
fi
# ========================= END Test Execution ===============================

# ======================= BEGIN Core File Inspection =========================
pushd $EXECUTION_DIR >/dev/null

if [[ $test_exitcode -ne 0 ]]; then
  echo ulimit -c value: $(ulimit -c)
fi

if [[ $system_name == "Linux" && $test_exitcode -ne 0 ]]; then
  echo cat /proc/sys/kernel/core_pattern: $(cat /proc/sys/kernel/core_pattern)
  echo cat /proc/sys/kernel/core_uses_pid: $(cat /proc/sys/kernel/core_uses_pid)
  echo cat /proc/sys/kernel/coredump_filter: $(cat /proc/sys/kernel/coredump_filter)

  # Depending on distro/configuration, the core files may either be named "core"
  # or "core.<PID>" by default. We read /proc/sys/kernel/core_uses_pid to
  # determine which it is.
  core_name_uses_pid=0
  if [[ -e /proc/sys/kernel/core_uses_pid && "1" == $(cat /proc/sys/kernel/core_uses_pid) ]]; then
    core_name_uses_pid=1
  fi
  
  # The osx dumps are too large to egress the machine
  echo Looking around for any Linux dumps...

  if [[ "$core_name_uses_pid" == "1" ]]; then
    # We don't know what the PID of the process was, so let's look at all core
    # files whose name matches core.NUMBER
    echo "Looking for files matching core.* ..."
    for f in $(find . -name "core.*"); do
      [[ $f =~ core.[0-9]+ ]] && move_core_file_to_temp_location "$f"
    done
  fi

  if [ -f core ]; then
    move_core_file_to_temp_location "core"
  fi
fi

if [ -n "$HELIX_WORKITEM_PAYLOAD" ]; then
  # For abrupt failures, in Helix, dump some of the kernel log, in case there is a hint
  if [[ $test_exitcode -ne 1 ]]; then
    dmesg | tail -50
  fi

fi

if [[ -z "$HELIX_CORRELATION_PAYLOAD" ]]; then
  : # Skip XUnitLogChecker execution
elif [[ -z "$__IsXUnitLogCheckerSupported" ]]; then
  echo "The '__IsXUnitLogCheckerSupported' env var is not set."
elif [[ "$__IsXUnitLogCheckerSupported" != "1" ]]; then
  echo "XUnitLogChecker not supported for this test case. Skipping."
else
  echo ----- start ===============  XUnitLogChecker Output =====================================================
  
  invoke_xunitlogchecker "$HELIX_DUMP_FOLDER"

  if [[ $xunitlogchecker_exit_code -ne 0 ]]; then
    test_exitcode=$xunitlogchecker_exit_code
  fi
  echo ----- end ===============  XUnitLogChecker Output - exit code $xunitlogchecker_exit_code ===========================
fi

popd >/dev/null
# ======================== END Core File Inspection ==========================
# The helix work item should not exit with non-zero if tests ran and produced results
# The special console runner for runtime returns 1 when tests fail
if [[ "$test_exitcode" == "1" && "$has_test_results" == "1" ]]; then
  if [ -n "$HELIX_WORKITEM_PAYLOAD" ]; then
    exit 0
  fi
fi

exit $test_exitcode
fi

