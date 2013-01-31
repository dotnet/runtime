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

readonly PACKAGE_NAME=runtime${TARGET_BIT_PREFIX}-build
readonly INSTALL_PATH=${MONO_TRUNK_NACL}/naclmono-${CROSS_ID}


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
  if [ $TARGET_BITSIZE == "32" ]; then
    CONFIG_OPTS="--host=i686-pc-linux-gnu --build=i686-pc-linux-gnu --target=i686-pc-linux-gnu"
  else
    CONFIG_OPTS="--host=x86_64-pc-linux-gnu --build=x86_64-pc-linux-gnu --target=x86_64-pc-linux-gnu"
  fi
  # UGLY hack to allow dynamic linking
  sed -i -e s/elf_i386/elf_nacl/ -e s/elf_x86_64/elf64_nacl/ ../configure
  sed -i -e s/elf_i386/elf_nacl/ -e s/elf_x86_64/elf64_nacl/ ../libgc/configure
  sed -i -e s/elf_i386/elf_nacl/ -e s/elf_x86_64/elf64_nacl/ ../eglib/configure
  Remove ${PACKAGE_NAME}
  MakeDir ${PACKAGE_NAME}
  cd ${PACKAGE_NAME}
  CC=${NACLCC} CXX=${NACLCXX} AR=${NACLAR} RANLIB=${NACLRANLIB} PKG_CONFIG_PATH=${NACL_SDK_USR_LIB}/pkgconfig LD="${NACLLD}" \
  PKG_CONFIG_LIBDIR=${NACL_SDK_USR_LIB} PATH=${NACL_BIN_PATH}:${PATH} LIBS="-lnacl_dyncode -lc -lg -lnosys -lnacl" \
  CFLAGS="-g -O2 -D_POSIX_PATH_MAX=256 -DPATH_MAX=256" ../../configure \
    ${CONFIG_OPTS} \
    --exec-prefix=${INSTALL_PATH} \
    --libdir=${INSTALL_PATH}/lib \
    --prefix=${INSTALL_PATH} \
    --program-prefix="" \
    --oldincludedir=${INSTALL_PATH}/include \
    --with-glib=embedded \
    --with-tls=pthread \
    --enable-threads=posix \
    --without-sigaltstack \
    --without-mmap \
    --with-gc=included \
    --enable-nacl-gc \
    --with-sgen=no \
    --enable-nls=no \
    --enable-nacl-codegen \
    --disable-system-aot \
    --enable-shared \
    --disable-parallel-mark \
    --with-static-mono=no

}

CustomInstallStep() {
  make install
}

CustomPackageInstall() {
  CustomConfigureStep
  DefaultBuildStep
  CustomInstallStep
}

CustomPackageInstall
exit 0
