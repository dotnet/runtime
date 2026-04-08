# Build two static libraries for the two exception handling models.
# Build integration will choose which one to use at link time.
add_library(CppExceptionHandling STATIC ExceptionHandling.Cpp.cpp)
add_library(WasmExceptionHandling STATIC ExceptionHandling.Wasm.cpp)
add_library(EmulatedExceptionHandling STATIC ExceptionHandling.Emulated.cpp)

target_compile_options(CppExceptionHandling PRIVATE -fexceptions)
target_compile_options(WasmExceptionHandling PRIVATE -fwasm-exceptions)

install_static_library(CppExceptionHandling aotsdk nativeaot)
install_static_library(WasmExceptionHandling aotsdk nativeaot)
install_static_library(EmulatedExceptionHandling aotsdk nativeaot)
