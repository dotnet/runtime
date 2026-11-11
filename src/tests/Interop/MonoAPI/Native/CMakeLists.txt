# The Mono embedding API test uses setjmp and signals, which wasi-libc does not provide.
if(NOT CLR_CMAKE_TARGET_WASI)
    add_subdirectory(mono-embedding-api-test)
endif()
