# ***************************************************************************
#    RunBenchmarks.sh
#    
#    This is a sample script for how to run benchmarks on Unix-like system.
#    
#    It requires the user to set CORECLR_ROOT to the root directory
#    of the enlistment(repo).  It also requires that CoreCLR has been built, 
#    and that all CoreCLR tests have been built.
#    
#    The preformance harness "RunBenchmarks.exe" is built as a test case
#    as are all the performance tests it runs.
#    
#    For the ByteMark tests, it must copy the command scripts to the 
#    binary directory for the tests.
#
#    By default, the performance harness is run on top of CoreCLR.  There
#    is a commented out section that can be used to run on top of DesktopCLR.
#    
#    A standard benchmark run is done with one warmup run, and five iterations
#    of the benchmark.
#
# ***************************************************************************

ARCH=${1:-x64}
BUILD=${2:-Release}

CORERUN=${CORERUN:-corerun}

# *** set this appropriately for enlistment you are running benchmarks in

if [ -z "$CORECLR_ROOT" ]; then
  echo "You must set CORECLR_ROOT to be the root of your coreclr repo (e.g. /git/repos/coreclr)"
  exit 1
fi

# *** Currently we can build test cases only on Windows, so "Windows_NT" is hard-coded in the variables.
BENCHMARK_ROOT_DIR="$CORECLR_ROOT/bin/tests/Windows_NT.$ARCH.$BUILD/JIT/Performance/CodeQuality"
BENCHMARK_SRC_DIR="$CORECLR_ROOT/tests/src/JIT/Performance/RunBenchmarks"
BENCHMARK_HOST="$CORERUN $CORECLR_ROOT/bin/tests/Windows_NT.$ARCH.$BUILD/JIT/Performance/RunBenchmarks/RunBenchmarks/RunBenchmarks.exe"
BENCHMARK_RUNNER="-runner $CORERUN"

# *** need to copy command files for Bytemark
mkdir -p ${BENCHMARK_ROOT_DIR}/Bytemark/Bytemark
cp -rf $CORECLR_ROOT/tests/src/JIT/Performance/CodeQuality/Bytemark/commands ${BENCHMARK_ROOT_DIR}/Bytemark/Bytemark/commands

BENCHMARK_CONTROLS="-run -v -w -n 5"
BENCHMARK_SET="-f $BENCHMARK_SRC_DIR/coreclr_benchmarks.xml -notags broken"
BENCHMARK_OUTPUT="-csvfile $BENCHMARK_SRC_DIR/coreclr_benchmarks.csv"
BENCHMARK_SWITCHES="$BENCHMARK_CONTROLS -r $BENCHMARK_ROOT_DIR"

echo "$BENCHMARK_HOST $BENCHMARK_RUNNER $BENCHMARK_SET $BENCHMARK_OUTPUT $BENCHMARK_SWITCHES"
$BENCHMARK_HOST $BENCHMARK_RUNNER $BENCHMARK_SET $BENCHMARK_OUTPUT $BENCHMARK_SWITCHES

