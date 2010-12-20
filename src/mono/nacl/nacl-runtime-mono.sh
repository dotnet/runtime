#!/bin/bash
# Copyright (c) 2009 The Native Client Authors. All rights reserved.
# Use of this source code is governed by a BSD-style license that be
# found in the LICENSE file.
#

# nacl-runtime-mono.sh
#
# usage:  nacl-runtime-mono.sh
#
# this script builds mono runtime for Native Client 
#

readonly MONO_TRUNK_NACL=$(pwd)

source common.sh
source nacl-common.sh

readonly PACKAGE_NAME=runtime${TARGET_BIT_PREFIX}-build
readonly INSTALL_PATH=${MONO_TRUNK_NACL}/runtime${TARGET_BIT_PREFIX}


CustomConfigureStep() {
  Banner "Configuring ${PACKAGE_NAME}"
  # export the nacl tools
  set +e
  if [ -f ${PACKAGE_NAME}/Makefile ]
  then
    cd ${PACKAGE_NAME}
  fi
  make distclean
  cd ${MONO_TRUNK_NACL}
  set -e
  cp config-nacl-runtime${TARGET_BIT_PREFIX}.cache config-nacl-runtime${TARGET_BIT_PREFIX}.cache.temp
  Remove ${PACKAGE_NAME}
  MakeDir ${PACKAGE_NAME}
  cd ${PACKAGE_NAME}
  # TODO: remove this once libintl.h becomes available to nacl
  CC=${NACLCC} CXX=${NACLCXX} AR=${NACLAR} RANLIB=${NACLRANLIB} PKG_CONFIG_PATH=${NACL_SDK_USR_LIB}/pkgconfig \
  PKG_CONFIG_LIBDIR=${NACL_SDK_USR_LIB} PATH=${NACL_BIN_PATH}:${PATH} LIBS="-lnosys -lg" \
  CFLAGS="-g -D_POSIX_PATH_MAX=256 -DPATH_MAX=256" ../../configure \
    --host=nacl${TARGET_BIT_PREFIX} \
    --exec-prefix=${INSTALL_PATH} \
    --libdir=${INSTALL_PATH}/lib \
    --prefix=${INSTALL_PATH} \
    --oldincludedir=${MONO_TRUNK_NACL}/runtime/include \
    --disable-shared \
    --disable-mcs-build \
    --with-glib=embedded \
    --with-tls=pthread \
    --enable-threads=posix \
    --without-sigaltstack \
    --without-mmap \
    --with-gc=included \
    --enable-nacl-gc \
    --enable-nacl-codegen \
    --cache-file=../config-nacl-runtime${TARGET_BIT_PREFIX}.cache.temp
  echo "// --- Native Client runtime below" >> config.h
  echo "#define pthread_cleanup_push(x, y)" >> config.h
  echo "#define pthread_cleanup_pop(x)" >> config.h
  echo "#undef HAVE_EPOLL" >> config.h
  echo "#undef HAVE_WORKING_SIGALTSTACK" >> config.h
  echo "extern long int timezone;" >> config.h
  echo "extern int daylight;" >> config.h
  echo "#define sem_trywait(x) sem_wait(x)" >> config.h
  echo "#define sem_timedwait(x,y) sem_wait(x)" >> config.h
  echo "#define getdtablesize() (32768)" >> config.h
  echo "// --- Native Client runtime below" >> eglib/src/eglib-config.h
  echo "#undef G_BREAKPOINT" >> eglib/src/eglib-config.h
  echo "#define G_BREAKPOINT() G_STMT_START { __asm__ (\"hlt\"); } G_STMT_END" >> eglib/src/eglib-config.h
  rm ../config-nacl-runtime${TARGET_BIT_PREFIX}.cache.temp
}

CustomInstallStep() {
  make install
  CopyNormalMonoLibs
}

CustomPackageInstall() {
  CustomConfigureStep
  DefaultBuildStep
  CustomInstallStep
}


CustomPackageInstall
exit 0
