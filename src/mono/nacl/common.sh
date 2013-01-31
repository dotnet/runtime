# Copyright (c) 2011 The Native Client Authors. All rights reserved.
# Use of this source code is governed by a BSD-style license that be
# found in the LICENSE file.
#

set -o nounset
set -o errexit

# scripts that source this file must be run from within packages tree
readonly SAVE_PWD=$(pwd)

# Pick platform directory for compiler.
readonly OS_NAME=$(uname -s)
if [ $OS_NAME = "Darwin" ]; then
  readonly OS_SUBDIR="mac"
  readonly OS_SUBDIR_SHORT="mac"
elif [ $OS_NAME = "Linux" ]; then
  readonly OS_SUBDIR="linux"
  readonly OS_SUBDIR_SHORT="linux"
else
  readonly OS_SUBDIR="windows"
  readonly OS_SUBDIR_SHORT="win"
fi

readonly MACHINE=$(uname -m)
if [ $MACHINE = "x86_64" ]; then
  readonly TARGET_BITSIZE=${TARGET_BITSIZE:-"64"}
  readonly HOST_BITSIZE=${HOST_BITSIZE:-"64"}
else
  # uname -m reports i686 on Linux and i386 on Mac
  readonly TARGET_BITSIZE=${TARGET_BITSIZE:-"32"}
  readonly HOST_BITSIZE=${HOST_BITSIZE:-"32"}
fi

if [ $TARGET_BITSIZE == "64" ]; then
  readonly TARGET_BIT_PREFIX="64"
  readonly CROSS_ID=x86_64
else
  readonly TARGET_BIT_PREFIX=""
  readonly CROSS_ID=i686
fi
# we might want to override the detected host platform (e.g. on OSX 10.6)
if [ $HOST_BITSIZE == "64" ]; then
  readonly HOST_BIT_PREFIX="64"
else
  readonly HOST_BIT_PREFIX=""
fi

export NACL_CROSS_PREFIX=${CROSS_ID}-nacl
export NACL_CROSS_PREFIX_DASH=${NACL_CROSS_PREFIX}-

readonly NACL_NEWLIB=${NACL_NEWLIB:-"0"}

if [ $NACL_NEWLIB = "1" ]; then
  readonly NACL_SDK_BASE=${NACL_SDK_ROOT}/toolchain/${OS_SUBDIR_SHORT}_x86_newlib
else
case "${NACL_SDK_ROOT}" in
*pepper_15* | *pepper_16* | *pepper_17*)
  readonly NACL_SDK_BASE=${NACL_SDK_ROOT}/toolchain/${OS_SUBDIR_SHORT}_x86
  ;;
*)
  readonly NACL_SDK_BASE=${NACL_SDK_ROOT}/toolchain/${OS_SUBDIR_SHORT}_x86_glibc
  ;;
esac
fi

readonly NACL_BIN_PATH=${NACL_SDK_BASE}/bin
export NACLCC=${NACL_BIN_PATH}/${NACL_CROSS_PREFIX_DASH}gcc
export NACLCXX=${NACL_BIN_PATH}/${NACL_CROSS_PREFIX_DASH}g++
export NACLAR=${NACL_BIN_PATH}/${NACL_CROSS_PREFIX_DASH}ar
export NACLRANLIB=${NACL_BIN_PATH}/${NACL_CROSS_PREFIX_DASH}ranlib
export NACLLD=${NACL_BIN_PATH}/${NACL_CROSS_PREFIX_DASH}ld
export NACLAS=${NACL_BIN_PATH}/${NACL_CROSS_PREFIX_DASH}as

# NACL_SDK_GCC_SPECS_PATH is where nacl-gcc 'specs' file will be installed
readonly NACL_SDK_GCC_SPECS_PATH=${NACL_SDK_BASE}/lib/gcc/x86_64-nacl/4.4.3

# NACL_SDK_USR is where the headers, libraries, etc. will be installed
readonly NACL_SDK_USR=${NACL_SDK_BASE}/${NACL_CROSS_PREFIX}/usr
readonly NACL_SDK_USR_INCLUDE=${NACL_SDK_USR}/include
readonly NACL_SDK_USR_LIB=${NACL_SDK_USR}/lib


######################################################################
# Helper functions
######################################################################

Banner() {
  echo "######################################################################"
  echo $*
  echo "######################################################################"
}


VerifyPath() {
  # make sure path isn't all slashes (possibly from an unset variable)
  local PATH=$1
  local TRIM=${PATH##/}
  if [ ${#TRIM} -ne 0 ]; then
    return 0
  else
    return 1
  fi
}


ChangeDir() {
  local NAME=$1
  if VerifyPath ${NAME}; then
    cd ${NAME}
  else
    echo "ChangeDir called with bad path."
    exit -1
  fi
}


Remove() {
  local NAME=$1
  if VerifyPath ${NAME}; then
    rm -rf ${NAME}
  else
    echo "Remove called with bad path."
    exit -1
  fi
}


MakeDir() {
  local NAME=$1
  if VerifyPath ${NAME}; then
    mkdir -p ${NAME}
  else
    echo "MakeDir called with bad path."
    exit -1
  fi
}


PatchSpecFile() {
  # fix up spaces so gcc sees entire path
  local SED_SAFE_SPACES_USR_INCLUDE=${NACL_SDK_USR_INCLUDE/ /\ /}
  local SED_SAFE_SPACES_USR_LIB=${NACL_SDK_USR_LIB/ /\ /}
  # have nacl-gcc dump specs file & add include & lib search paths
  ${NACL_SDK_BASE}/bin/x86_64-nacl-gcc -dumpspecs |\
    sed "/*cpp:/{
      N
      s|$| -I${SED_SAFE_SPACES_USR_INCLUDE}|
    }" |\
    sed "/*link_libgcc:/{
      N
      s|$| -L${SED_SAFE_SPACES_USR_LIB}|
    }" >${NACL_SDK_GCC_SPECS_PATH}/specs
}


DefaultConfigureStep() {
  Banner "Configuring ${PACKAGE_NAME}"
  # export the nacl tools
  export CC=${NACLCC}
  export CXX=${NACLCXX}
  export AR=${NACLAR}
  export RANLIB=${NACLRANLIB}
  export PKG_CONFIG_PATH=${NACL_SDK_USR_LIB}/pkgconfig
  export PKG_CONFIG_LIBDIR=${NACL_SDK_USR_LIB}
  export PATH=${NACL_BIN_PATH}:${PATH};
  ChangeDir ${NACL_PACKAGES_REPOSITORY}/${PACKAGE_NAME}
  Remove ${PACKAGE_NAME}-build
  MakeDir ${PACKAGE_NAME}-build
  cd ${PACKAGE_NAME}-build
  ../configure \
    --host=nacl \
    --disable-shared \
    --prefix=${NACL_SDK_USR} \
    --exec-prefix=${NACL_SDK_USR} \
    --libdir=${NACL_SDK_USR_LIB} \
    --oldincludedir=${NACL_SDK_USR_INCLUDE} \
    --with-http=off \
    --with-html=off \
    --with-ftp=off \
    --with-x=no
}


DefaultBuildStep() {
  # assumes pwd has makefile
  make clean
if [ $TARGET_BITSIZE == "64" ]; then
  make -j8
else
  make
fi
}


DefaultInstallStep() {
  # assumes pwd has makefile
  make install
}


DefaultCleanUpStep() {
  PatchSpecFile
  ChangeDir ${SAVE_PWD}
}


DefaultPackageInstall() {
  DefaultPreInstallStep
  DefaultDownloadStep
  DefaultExtractStep
  DefaultPatchStep
  DefaultConfigureStep
  DefaultBuildStep
  DefaultInstallStep
  DefaultCleanUpStep
}
