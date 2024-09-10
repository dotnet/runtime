set(CMAKE_CROSSCOMPILING TRUE)
set(CMAKE_SYSTEM_NAME "Linux")
set(CMAKE_SYSTEM_PROCESSOR "riscv64")

# Avoid to use system path for cross-compile
set(CMAKE_FIND_USE_CMAKE_SYSTEM_PATH FALSE)

set(TOOLCHAIN_PATH "" CACHE STRING "The toolchain path.")
if(NOT TOOLCHAIN_PATH)
  set(TOOLCHAIN_PATH ${CMAKE_SOURCE_DIR}/prebuilt-riscv-toolchain-qemu/riscv-clang)
endif()

set(TOOLCHAIN_PREFIX "riscv64-unknown-linux-gnu-" CACHE STRING "The toolchain prefix.")
set(QEMU_PATH "" CACHE STRING "The qemu path.")
if(NOT QEMU_PATH)
  set(QEMU_PATH ${CMAKE_SOURCE_DIR}/prebuilt-riscv-toolchain-qemu/riscv-qemu/bin/qemu-riscv64)
endif()

# toolchain setting
set(CMAKE_C_COMPILER "${TOOLCHAIN_PATH}/bin/${TOOLCHAIN_PREFIX}clang")
set(CMAKE_CXX_COMPILER "${TOOLCHAIN_PATH}/bin/${TOOLCHAIN_PREFIX}clang++")

# disable auto-vectorizer
add_compile_options(-fno-vectorize -fno-slp-vectorize)

# emulator setting
set(QEMU_CPU_OPTION "rv64,zba=true,zbb=true,zbc=true,zbs=true,v=true,vlen=512,elen=64,vext_spec=v1.0")
set(CMAKE_CROSSCOMPILING_EMULATOR ${QEMU_PATH} -cpu ${QEMU_CPU_OPTION} -L ${TOOLCHAIN_PATH}/sysroot/)
