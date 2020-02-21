project (DynamicTestServer)


# Get the current list of definitions to pass to midl
#get_compile_definitions(MIDL_DEFINITIONS)
#get_include_directories(MIDL_INCLUDE_DIRECTORIES)

# Compile IDL file using MIDL
set(IDL_SOURCE ${CMAKE_CURRENT_SOURCE_DIR}/Contract.idl)
get_filename_component(IDL_NAME ${IDL_SOURCE} NAME_WE)

FIND_PROGRAM( MIDL midl.exe )
set(IDL_OUTPUT_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}/Contract)
add_custom_command(
    OUTPUT ${IDL_OUTPUT_DIRECTORY}/${IDL_NAME}_i.c ${IDL_OUTPUT_DIRECTORY}/${IDL_NAME}.h
    COMMAND ${MIDL} ${MIDL_INCLUDE_DIRECTORIES}
        /h ${IDL_OUTPUT_DIRECTORY}/${IDL_NAME}.h ${MIDL_DEFINITIONS}
        /out ${IDL_OUTPUT_DIRECTORY}
        /tlb ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/DynamicTestServer.tlb
        ${IDL_SOURCE}
    DEPENDS ${IDL_SOURCE}
    COMMENT "Compiling ${IDL_SOURCE}")


# Compile *_i.c as C files
#add_compile_options(/TC)

include_directories(${IDL_OUTPUT_DIRECTORY})
set(SOURCES
    CollectionTest.cpp
    DispatchImpl.cpp
    DynamicTestServer.cpp
    Exports.def
    ${IDL_OUTPUT_DIRECTORY}/${IDL_NAME}_i.c)

if (CLR_CMAKE_HOST_WIN32)
    # 4365 - signed/unsigned mismatch
    add_compile_options(-wd4365)
endif()

# add the shared library
add_library (${PROJECT_NAME} SHARED ${SOURCES})
target_link_libraries(${PROJECT_NAME} ${LINK_LIBRARIES_ADDITIONAL})

# Copy manifest file to project output
file(GENERATE OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/DynamicTestServer.X.manifest INPUT ${CMAKE_CURRENT_SOURCE_DIR}/DynamicTestServer.X.manifest)
#file(GENERATE OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/DynamicTestServer.tlb INPUT ${IDL_OUTPUT_DIRECTORY}/DynamicTestServer.tlb)

# add the install targets
install (TARGETS ${PROJECT_NAME} DESTINATION bin)
