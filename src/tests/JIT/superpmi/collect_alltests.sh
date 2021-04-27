#!/bin/bash

CORECLRROOT=~/src/coreclr
TESTROOT=~/test
MCHFILE=${TESTROOT}/alltests_linux.mch
FLAVOR=windows.x64.Debug

pushd ${TESTROOT}/${FLAVOR}/superpmi/superpmicollect
${TESTROOT}/${FLAVOR}/Tests/coreoverlay/corerun ${TESTROOT}/${FLAVOR}/JIT/superpmi/superpmicollect/superpmicollect.exe -mch ${MCHFILE} -run ${CORECLRROOT}/tests/src/JIT/superpmi/runtests.sh
popd
