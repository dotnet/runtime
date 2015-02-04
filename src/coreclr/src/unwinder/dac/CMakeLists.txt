include(${CLR_DIR}/dac.cmake)

add_definitions(-DFEATURE_NO_HOST)
add_definitions(-D_TARGET_AMD64_=1)
add_definitions(-DDBG_TARGET_64BIT=1)
add_definitions(-DDBG_TARGET_AMD64=1)
add_definitions(-DDBG_TARGET_WIN64=1)
add_definitions(-D_WIN64=1)

add_library(unwinder_dac ${UNWINDER_SOURCES})
