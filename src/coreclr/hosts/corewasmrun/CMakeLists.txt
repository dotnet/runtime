project(corewasmrun)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_executable_clr(corewasmrun
  corewasmrun.cpp
  ../corerun/corerun.wasm.cpp
)

set(_WASM_PRELOAD_DIR "${CMAKE_INSTALL_PREFIX}/IL")
if (EXISTS "${_WASM_PRELOAD_DIR}")
  set(_WASM_PRELOAD_FILE --preload-file ${_WASM_PRELOAD_DIR}@/)
endif (EXISTS "${_WASM_PRELOAD_DIR}")

target_include_directories(corewasmrun PRIVATE
  ../corerun
)

target_compile_options(corewasmrun PRIVATE -fwasm-exceptions)
target_link_options(corewasmrun PRIVATE
  -fwasm-exceptions
  -sEXIT_RUNTIME=1
  -sINITIAL_MEMORY=134217728
  -sSTACK_SIZE=5MB
  -sFORCE_FILESYSTEM=1
  ${_WASM_PRELOAD_FILE}
  -Wl,-error-limit=0)
target_link_libraries(corewasmrun PRIVATE
  coreclr_static
  System.Native-Static
  System.Native.TimeZoneData)

# The corerun version for wasm is being installed in its own directory
# because it is an executable that comes with an index.html and we
# want to avoid clashes.
install_clr(TARGETS corewasmrun DESTINATIONS corewasmrun COMPONENT hosts)
install(FILES index.html DESTINATION corewasmrun COMPONENT hosts)
