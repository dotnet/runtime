#!/bin/bash
echo "*** .NET EMSDK path setup ***"

# emscripten (emconfigure, em++, etc)
if [ -z "${EMSDK_PATH}" ]; then
  echo "\$EMSDK_PATH is empty"
  exit 1
fi
TOADD_PATH_EMSCRIPTEN="$(realpath ${EMSDK_PATH}/emscripten)"
echo "Prepending to PATH: ${TOADD_PATH_EMSCRIPTEN}"
export PATH=${TOADD_PATH_EMSCRIPTEN}:$PATH

# llvm (clang, etc)
if [ -z "${DOTNET_EMSCRIPTEN_LLVM_ROOT}" ]; then
  echo "\$DOTNET_EMSCRIPTEN_LLVM_ROOT is empty"
  exit 1
fi
TOADD_PATH_LLVM="$(realpath ${DOTNET_EMSCRIPTEN_LLVM_ROOT})"
if [ "${TOADD_PATH_EMSCRIPTEN}" != "${TOADD_PATH_LLVM}" ]; then
  echo "Prepending to PATH: ${TOADD_PATH_LLVM}"
  export PATH=${TOADD_PATH_LLVM}:$PATH
fi

# nodejs (node)
if [ -z "${DOTNET_EMSCRIPTEN_NODE_JS}" ]; then
  echo "\$DOTNET_EMSCRIPTEN_NODE_JS is empty"
  exit 1
fi
TOADD_PATH_NODEJS="$(dirname ${DOTNET_EMSCRIPTEN_NODE_JS})"
if [ "${TOADD_PATH_EMSCRIPTEN}" != "${TOADD_PATH_NODEJS}" ] && [ "${TOADD_PATH_LLVM}" != "${TOADD_PATH_NODEJS}" ]; then
  echo "Prepending to PATH: ${TOADD_PATH_NODEJS}"
  export PATH=${TOADD_PATH_NODEJS}:$PATH
fi

# binaryen (wasm-opt, etc)
if [ -z "${DOTNET_EMSCRIPTEN_BINARYEN_ROOT}" ]; then
  echo "\$DOTNET_EMSCRIPTEN_BINARYEN_ROOT is empty"
  exit 1
fi
TOADD_PATH_BINARYEN="$(realpath ${DOTNET_EMSCRIPTEN_BINARYEN_ROOT}/bin)"
if [ "${TOADD_PATH_EMSCRIPTEN}" != "${TOADD_PATH_BINARYEN}" ] && [ "${TOADD_PATH_LLVM}" != "${TOADD_PATH_BINARYEN}" ] && [ "${TOADD_PATH_NODEJS}" != "${TOADD_PATH_BINARYEN}" ]; then
  echo "Prepending to PATH: ${TOADD_PATH_BINARYEN}"
  export PATH=${TOADD_PATH_BINARYEN}:$PATH
fi
