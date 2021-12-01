
# Get the current list of definitions to pass to midl
get_compile_definitions(MIDL_DEFINITIONS)
get_include_directories(MIDL_INCLUDE_DIRECTORIES)
find_program(MIDL midl.exe)

function(compile_idl idl_file tlb_out)
    # Compile IDL file using MIDL
    set(IDL_SOURCE ${idl_file})
    get_filename_component(IDL_NAME ${IDL_SOURCE} NAME_WE)
    set(tlb_out_local "${CMAKE_CURRENT_BINARY_DIR}/${IDL_NAME}.tlb")
    set("${tlb_out}" "${tlb_out_local}" PARENT_SCOPE)

    add_custom_command(
        OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/${IDL_NAME}_i.c ${CMAKE_CURRENT_BINARY_DIR}/${IDL_NAME}.h ${tlb_out_local}
        COMMAND ${MIDL} ${MIDL_INCLUDE_DIRECTORIES}
            /h ${CMAKE_CURRENT_BINARY_DIR}/${IDL_NAME}.h ${MIDL_DEFINITIONS}
            /out ${CMAKE_CURRENT_BINARY_DIR}
            /tlb ${tlb_out_local}
            ${IDL_SOURCE}
        DEPENDS ${IDL_SOURCE}
        COMMENT "Compiling ${IDL_SOURCE}")
endfunction()

compile_idl(${CMAKE_CURRENT_SOURCE_DIR}/Server.idl Server_tlb)
compile_idl(${CMAKE_CURRENT_SOURCE_DIR}/Nested.idl Nested_tlb)

add_custom_target(typelibs ALL DEPENDS "${Server_tlb}" "${Nested_tlb}")

install(FILES "${Server_tlb}" "${Nested_tlb}" DESTINATION corehost_test)
