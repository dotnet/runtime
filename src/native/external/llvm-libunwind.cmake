# IMPORTANT: do not use add_compile_options(), add_definitions() or similar functions here since it will leak to the including projects

if(NOT CLR_CMAKE_TARGET_ARCH_WASM)
  include_directories(BEFORE "${CMAKE_CURRENT_LIST_DIR}/llvm-libunwind/include")
endif()

set (LLVM_LIBUNWIND_SOURCES_BASE
    src/Unwind-EHABI.cpp
    src/libunwind.cpp
)

set(LLVM_LIBUNWIND_ASM_SOURCES_BASE
    src/UnwindRegistersRestore.S
    src/UnwindRegistersSave.S
)

addprefix(LLVM_LIBUNWIND_SOURCES "${CMAKE_CURRENT_LIST_DIR}/llvm-libunwind" "${LLVM_LIBUNWIND_SOURCES_BASE}")
addprefix(LLVM_LIBUNWIND_ASM_SOURCES "${CMAKE_CURRENT_LIST_DIR}/llvm-libunwind" "${LLVM_LIBUNWIND_ASM_SOURCES_BASE}")
