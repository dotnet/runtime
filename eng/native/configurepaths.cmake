get_filename_component(CLR_REPO_ROOT_DIR ${CMAKE_CURRENT_LIST_DIR}/../.. ABSOLUTE)
set(CLR_ENG_NATIVE_DIR ${CMAKE_CURRENT_LIST_DIR})
get_filename_component(CLR_SRC_NATIVE_DIR ${CMAKE_CURRENT_LIST_DIR}/../../src/native ABSOLUTE)

# TO_NATIVE_PATH so it uses backslashes in Windows to avoid getting error,
# as CLR_SRC_NATIVE_DIR is used to specify source files.
file (TO_NATIVE_PATH ${CLR_SRC_NATIVE_DIR} CLR_SRC_NATIVE_DIR)
