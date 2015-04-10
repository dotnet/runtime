include_directories("../../pal/prebuilt/corerror")
if(WIN32)
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /NOENTRY")
else()

    set (RC_TO_PO ${CMAKE_CURRENT_SOURCE_DIR}/rctopo.awk)
    set (PROCESS_RC ${CMAKE_CURRENT_SOURCE_DIR}/processrc.awk)
    
    # Create a command to build gettext resources binary file from windows .rc file
    # The target binary file path is returned in the variable specified by
    # the TARGET_FILE parameter.
    function(build_resources SOURCE TARGET_NAME TARGET_FILE)
    
        # Ensure that the necessary tools are present
        find_program(AWK awk)
        if (AWK STREQUAL "AWK-NOTFOUND")
            message(FATAL_ERROR "AWK not found")
        endif()

        find_program(MSGFMT msgfmt)
        if (MSGFMT STREQUAL "MSGFMT-NOTFOUND")
            message(FATAL_ERROR "msgfmt not found. Please install the gettext package.")
        endif()

        get_compile_definitions(PREPROCESS_DEFINITIONS)
        get_include_directories(INCLUDE_DIRECTORIES)

        set(PREPROCESSED_SOURCE ${CMAKE_CURRENT_BINARY_DIR}/${TARGET_NAME}.rc.i)
        set(GETTEXT_SOURCE ${CMAKE_CURRENT_BINARY_DIR}/${TARGET_NAME}.po)
        set(GETTEXT_TARGET ${CMAKE_CURRENT_BINARY_DIR}/${TARGET_NAME}.mo)

        add_custom_command(
            OUTPUT  ${GETTEXT_TARGET}
            # Preprocess the windows .rc file
            COMMAND ${CMAKE_CXX_COMPILER} -E -P ${PREPROCESS_DEFINITIONS} ${INCLUDE_DIRECTORIES} -o ${PREPROCESSED_SOURCE} -x c ${SOURCE}
            # Convert the preprocessed .rc file to the .po file which is a source for the gettext toolchain
            COMMAND ${AWK} -f ${RC_TO_PO} -f ${PROCESS_RC} ${PREPROCESSED_SOURCE} >${GETTEXT_SOURCE}
            # Compile the .po file into the target binary .mo file
            COMMAND ${MSGFMT} ${GETTEXT_SOURCE} -o ${GETTEXT_TARGET}
            DEPENDS ${SOURCE}
            COMMENT "Building ${TARGET_NAME}.mo"
        )
        
        set(${TARGET_FILE} ${GETTEXT_TARGET} PARENT_SCOPE)
        
    endfunction()

endif(WIN32)    

add_subdirectory(full)
add_subdirectory(small)