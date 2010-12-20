
CopyNormalMonoLibs() {
  NORMAL_MSCORLIB_DLL=$MONO_TRUNK_NACL/normal-mono/lib/mono/2.0/mscorlib.dll
  if [ ! -f ${NORMAL_MSCORLIB_DLL} ]
  then
    Banner "Normal mscorlib.dll not found, building normal mono"
    cd ${MONO_TRUNK_NACL}
    ./normal-mono.sh
  fi
  if [ ! -f ${NORMAL_MSCORLIB_DLL} ]
  then
    Banner "Normal mscorlib.dll not found after normal mono build, exiting..."
    exit -1
  fi
  Banner "Copying normal-mono libs to install dir"
  mkdir -p ${INSTALL_PATH}/lib/mono
  cp -R ${MONO_TRUNK_NACL}/normal-mono/lib/mono/* ${INSTALL_PATH}/lib/mono/
}

