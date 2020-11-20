set (CLR_ENG_NATIVE_DIR ${CLR_REPO_ROOT_DIR}/eng/native)

# TO_NATIVE_PATH so it uses backslashes in Windows to avoid getting error,
# as CLR_SRC_NATIVE_DIR is used to specify source files.
file (TO_NATIVE_PATH ${CLR_REPO_ROOT_DIR}/src/native CLR_SRC_NATIVE_DIR)
