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

if [ "$RUNTIME_PATH" == "" ]; then
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
exitcode_list[135]="IGBUS  Unaligned memory access. Core dumped."
exitcode_list[136]="SIGFPE  Bad floating point arguments. Core dumped."
exitcode_list[137]="SIGKILL Killed eg by kill"
exitcode_list[139]="SIGSEGV Illegal memory access. Deref invalid pointer, overrunning buffer, stack overflow etc. Core dumped."
exitcode_list[143]="SIGTERM Terminated. Usually before SIGKILL."
exitcode_list[159]="SIGSYS  Bad System Call."

function print_info_from_core_file_using_lldb {
  local core_file_name=$1
  local executable_name=$2
  local plugin_path_name="$RUNTIME_PATH/shared/Microsoft.NETCore.App/9.9.9/libsosplugin.so"

  # check for existence of lldb on the path
  hash lldb 2>/dev/null || { echo >&2 "lldb was not found. Unable to print core file."; return; }

  # pe, clrstack, and dumpasync are defined in libsosplugin.so
  if [ ! -f $plugin_path_name ]; then
    echo $plugin_path_name cannot be found.
    return
  fi

  echo ----- start ===============  lldb Output =====================================================
  echo Printing managed exceptions, managed call stacks, and async state machines.
  lldb -O "settings set target.exec-search-paths $RUNTIME_PATH" -o "plugin load $plugin_path_name" -o "clrthreads -managedexception" -o "pe -nested" -o "clrstack -all -a -f" -o "dumpasync -fields -stacks -roots" -o "quit"  --core $core_file_name $executable_name
  echo ----- end ===============  lldb Output =======================================================
}

function print_info_from_core_file_using_gdb {
  local core_file_name=$1
  local executable_name=$2

  # Check for the existence of GDB on the path
  hash gdb 2>/dev/null || { echo >&2 "GDB was not found. Unable to print core file."; return; }

  echo ----- start ===============  GDB Output =====================================================
  # Open the dump in GDB and print the stack from each thread. We can add more
  # commands here if desired.
  echo printing native stack.
  gdb --batch -ex "thread apply all bt full" -ex "quit" $executable_name $core_file_name
  echo ----- end ===============  GDB Output =======================================================
}

function print_info_from_core_file {
  local core_file_name=$1
  local executable_name=$RUNTIME_PATH/$2

  if ! [ -e $executable_name ]; then
    echo "Unable to find executable $executable_name"
    return
  elif ! [ -e $core_file_name ]; then
    echo "Unable to find core file $core_file_name"
    return
  fi
  echo "Printing info from core file $core_file_name"
  print_info_from_core_file_using_gdb $core_file_name $executable_name
  print_info_from_core_file_using_lldb $core_file_name $executable_name
}

function copy_core_file_to_temp_location {
  local core_file_name=$1

  local storage_location="/tmp/coredumps"

  # Create the directory (this shouldn't fail even if it already exists).
  mkdir -p $storage_location

  local new_location=$storage_location/core.$RANDOM

  echo "Copying core file $core_file_name to $new_location in case you need it."
  cp $core_file_name $new_location
}

# ========================= BEGIN Core File Setup ============================
if [ "$(uname -s)" == "Darwin" ]; then
  # On OS X, we will enable core dump generation only if there are no core
  # files already in /cores/ at this point. This is being done to prevent
  # inadvertently flooding the CI machines with dumps.
  if [[ ! -d "/cores" || ! "$(ls -A /cores)" ]]; then
    ulimit -c unlimited
  fi

elif [ "$(uname -s)" == "Linux" ]; then
  # On Linux, we'll enable core file generation unconditionally, and if a dump
  # is generated, we will print some useful information from it and delete the
  # dump immediately.

  if [ -e /proc/self/coredump_filter ]; then
      # Include memory in private and shared file-backed mappings in the dump.
      # This ensures that we can see disassembly from our shared libraries when
      # inspecting the contents of the dump. See 'man core' for details.
      echo -n 0x3F > /proc/self/coredump_filter
  fi

  ulimit -c unlimited
fi
# ========================= END Core File Setup ==============================

# ========================= BEGIN Test Execution =============================
echo ----- start $(date) ===============  To repro directly: =====================================================
echo pushd $EXECUTION_DIR
[[RunCommandsEcho]]
echo popd
echo ===========================================================================================================
pushd $EXECUTION_DIR
[[RunCommands]]
test_exitcode=$?
popd
echo ----- end $(date) ----- exit code $test_exitcode ----------------------------------------------------------

if [ "${exitcode_list[$test_exitcode]}" != "" ]; then
  echo exit code $test_exitcode means ${exitcode_list[$test_exitcode]}
fi
# ========================= END Test Execution ===============================

# ======================= BEGIN Core File Inspection =========================
pushd $EXECUTION_DIR >/dev/null

if [[ $test_exitcode -ne 0 ]]; then
  echo ulimit -c value: $(ulimit -c)
fi

if [[ "$(uname -s)" == "Linux" && $test_exitcode -ne 0 ]]; then
  if [ -n "$HELIX_WORKITEM_PAYLOAD" ]; then

    # For abrupt failures, in Helix, dump some of the kernel log, in case there is a hint
    if [[ $test_exitcode -ne 1 ]]; then
      dmesg | tail -50
    fi

    have_sleep=$(which sleep)
    if [ -x "$have_sleep" ]; then
      echo Waiting a few seconds for any dump to be written..
      sleep 10s
    fi
  fi

  echo cat /proc/sys/kernel/core_pattern: $(cat /proc/sys/kernel/core_pattern)
  echo cat /proc/sys/kernel/core_uses_pid: $(cat /proc/sys/kernel/core_uses_pid)
  echo cat /proc/sys/kernel/coredump_filter: $(cat /proc/sys/kernel/coredump_filter)

  echo Looking around for any Linux dump..

  # Depending on distro/configuration, the core files may either be named "core"
  # or "core.<PID>" by default. We read /proc/sys/kernel/core_uses_pid to
  # determine which it is.
  core_name_uses_pid=0
  if [ -e /proc/sys/kernel/core_uses_pid ] && [ "1" == $(cat /proc/sys/kernel/core_uses_pid) ]; then
    core_name_uses_pid=1
  fi

  if [ $core_name_uses_pid == "1" ]; then
    # We don't know what the PID of the process was, so let's look at all core
    # files whose name matches core.NUMBER
    echo Looking for files matching core.* ...
    for f in core.*; do
      [[ $f =~ core.[0-9]+ ]] && print_info_from_core_file "$f" "dotnet" && copy_core_file_to_temp_location "$f" && rm "$f"
    done
  elif [ -f core ]; then
    echo found a dump named core in $EXECUTION_DIR !
    print_info_from_core_file "core" "dotnet"
    copy_core_file_to_temp_location "core"
    rm "core"
  else
    echo ... found no dump in $PWD
  fi
fi
popd >/dev/null
# ======================== END Core File Inspection ==========================
# The helix work item should not exit with non-zero if tests ran and produced results
# The special console runner for runtime returns 1 when tests fail
if [ "$test_exitcode" == "1" ]; then
  if [ -n "$HELIX_WORKITEM_PAYLOAD" ]; then
    exit 0
  fi
fi

exit $test_exitcode
fi

