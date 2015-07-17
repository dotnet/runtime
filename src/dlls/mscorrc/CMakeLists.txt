include_directories("../../pal/prebuilt/corerror")
if(WIN32)
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /NOENTRY")
    # remove /ltcg from resource-only libraries
    string(REPLACE "/LTCG" "" CMAKE_SHARED_LINKER_FLAGS_RELEASE ${CMAKE_SHARED_LINKER_FLAGS_RELEASE})
    string(REPLACE "/LTCG" "" CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO ${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO})
else()

    set (RC_TO_CPP ${CMAKE_CURRENT_SOURCE_DIR}/rctocpp.awk)
    set (PROCESS_RC ${CMAKE_CURRENT_SOURCE_DIR}/processrc.awk)
    set (RESOURCE_STRING_HEADER_DIR ${CMAKE_CURRENT_SOURCE_DIR})

    # Create a command to create a C++ source file containing an array of 
    # NativeStringResource structs which represent the information from a 
    # given Windows .rc file. The target C++ file path is returned in the
    # variable specified by the TARGET_FILE parameter.
    function(build_resources SOURCE TARGET_NAME TARGET_FILE)

        get_compile_definitions(PREPROCESS_DEFINITIONS)
        get_include_directories(INCLUDE_DIRECTORIES)

        set(PREPROCESSED_SOURCE ${CMAKE_CURRENT_BINARY_DIR}/${TARGET_NAME}.rc.i)
        set(RESOURCE_ENTRY_ARRAY_CPP ${CMAKE_CURRENT_BINARY_DIR}/${TARGET_NAME}.cpp)

        add_custom_command(
            OUTPUT ${RESOURCE_ENTRY_ARRAY_CPP} 
            # Preprocess the windows .rc file
            COMMAND ${CMAKE_CXX_COMPILER} -E -P ${PREPROCESS_DEFINITIONS} ${INCLUDE_DIRECTORIES} -o ${PREPROCESSED_SOURCE} -x c ${SOURCE}
            # Convert the preprocessed .rc file to a C++ file which will be used to make a static lib.
            COMMAND ${AWK} -f ${RC_TO_CPP} -f ${PROCESS_RC} ${PREPROCESSED_SOURCE} >${RESOURCE_ENTRY_ARRAY_CPP}
            DEPENDS ${SOURCE} ${RC_TO_CPP} ${PROCESS_RC}
        )

        include_directories(${RESOURCE_STRING_HEADER_DIR})
        set(${TARGET_FILE} ${RESOURCE_ENTRY_ARRAY_CPP} PARENT_SCOPE)

    endfunction()

endif(WIN32)

add_subdirectory(full)

# Only add the small version of the resources if the platform is Windows.
if(WIN32)
    add_subdirectory(small)
endif(WIN32)