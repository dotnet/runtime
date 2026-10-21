#!/bin/bash
# Run the System.GC library tests (default GC) with the runtime of this build tree.
# usage: gc-library-tests.sh <repo-root>
# Needs: ./build.sh -s clr+libs -c Release, then ./build.sh -s host -c Release and ./build.sh -s libs.pretest -c Release,
# so that artifacts/bin/testhost has the dotnet host and the runtime of the build. In a tree without a .git folder add
# /p:RepositoryCommit=<any commit id> to every build.sh call.
set -u
R=$1
cd "$R"
./dotnet.sh build src/libraries/System.Runtime/tests/System.Runtime.Tests/System.Runtime.Tests.csproj -c Release -t:Test \
  "/p:XUnitOptions=-class System.Tests.GCTests -class System.Tests.GCExtendedTests"
