# PrepTests.sh
# Initializes our test running environment by setting environment variables appropriately. Already set variables
# are not overwritten.  This is intended for use on unix.
#
# Created 10/28/2014 - mmitche

ExecPrepTestsCore()
{
    unset -f ExecPrepTestsCore

    echo
    echo Preparing this window to run tests
    echo Setting up Exernal Environment Variables
    echo _TGTCPU - Target CPU
    echo BVT_ROOT - Root of tests to run
    echo CORE_RUN - Test host
    echo CORE_ROOT - Root of CLR drop.
    echo
    
    # Set $_TGTCPU based on the processor arch.  In bash on an x64 linux machine, that will show up as x86_x64.
    # If so, then set to AMD64.  Otherwise I386.
    if ! test $_TGTCPU
    then
      export _TGTCPU=`arch`
      if test $_TGTCPU = "x86_x64"
      then
        export _TGTCPU="AMD64"
      else
        export _TGTCPU="i386"
      fi
    fi
    
    # Set $BVT_ROOT based on current directory
    if ! test $BVT_ROOT
    then
      export BVT_ROOT=$PWD
    fi
    
    # Set $CORE_RUN based on current directory
    if ! test $CORE_RUN
    then
      if test -e $BVT_ROOT/Hosting/CoreClr/Activation/Host/TestHost.exe
      then  
        export CORE_RUN=$BVT_ROOT/Hosting/CoreClr/Activation/Host/TestHost.exe
      else
        echo !!!ERROR: $BVT_ROOT/Hosting/CoreClr/Activation/Host/TestHost.exe does not exist. CORE_RUN not set.
        return 1
      fi
    fi
    
    # Set $CORE_ROOT base to $PWD unless otherwise set.
    if ! test "$CORE_ROOT"
    then
      echo Warning. CORE_ROOT is not set at the moment.  Setting it to $PWD
      export CORE_ROOT=$PWD
    fi
    
    # Report the current state of the environment
    echo _TGCPU is set to: $_TGTCPU
    echo BVT_ROOT is set to: $BVT_ROOT
    echo CORE_ROOT is set to: $CORE_ROOT
    echo CORE_RUN is set to $CORE_RUN
    echo

    return 0
}

# This is explicitly so that we can RETURN and not exit this script.
ExecPrepTestsCore $*
