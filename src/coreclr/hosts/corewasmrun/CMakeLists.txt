project(corewasmrun)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

if (DEFINED CLR_CMAKE_ICU_DIR)
  link_directories(${CLR_CMAKE_ICU_DIR}/lib)
endif(DEFINED CLR_CMAKE_ICU_DIR)

add_executable_clr(corewasmrun
  corewasmrun.cpp
)

set(_WASM_PRELOAD_DIR "${CMAKE_INSTALL_PREFIX}/IL")
if (EXISTS "${_WASM_PRELOAD_DIR}")
  set(_WASM_PRELOAD_FILE --preload-file ${_WASM_PRELOAD_DIR}@/)
endif (EXISTS "${_WASM_PRELOAD_DIR}")

target_compile_options(corewasmrun PRIVATE -fwasm-exceptions)
target_link_options(corewasmrun PRIVATE -fwasm-exceptions -sEXIT_RUNTIME=1 -sINITIAL_MEMORY=134217728 -sFORCE_FILESYSTEM=1 ${_WASM_PRELOAD_FILE} -Wl,-error-limit=0)

target_link_libraries(corewasmrun PRIVATE coreclr_static)
target_link_libraries(corewasmrun PRIVATE clrinterpreter)

target_link_libraries(corewasmrun PRIVATE icuuc)
target_link_libraries(corewasmrun PRIVATE icui18n)
target_link_libraries(corewasmrun PRIVATE icudata)

install_clr(TARGETS corewasmrun DESTINATIONS . COMPONENT hosts)
