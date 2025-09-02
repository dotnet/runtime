set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(INTERPRETER_SOURCES
  compiler.cpp
  compileropt.cpp
  intops.cpp
  interpconfig.cpp
  eeinterp.cpp
  stackmap.cpp
  naming.cpp
  methodset.cpp
  intrinsics.cpp)

set(INTERPRETER_LINK_LIBRARIES
  gcinfo
)

if(CLR_CMAKE_HOST_WIN32)
    list(APPEND INTERPRETER_LINK_LIBRARIES
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
    )
endif(CLR_CMAKE_HOST_WIN32)

if(CLR_CMAKE_HOST_WIN32)
    set(CLRINTERPRETER_EXPORTS ${CMAKE_CURRENT_LIST_DIR}/clrinterpreter.exports)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/clrinterpreter.def)
    preprocess_file(${CLRINTERPRETER_EXPORTS} ${EXPORTS_FILE})
else()
    set(CLRINTERPRETER_EXPORTS ${CMAKE_CURRENT_LIST_DIR}/clrinterpreter_unixexports.src)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/clrinterpreter.exports)
    generate_exports_file(${CLRINTERPRETER_EXPORTS} ${EXPORTS_FILE})
endif()

add_library_clr(clrinterpreter_objects OBJECT ${INTERPRETER_SOURCES})

if (NOT CMAKE_GENERATOR MATCHES "Visual Studio")
  set_target_properties(clrinterpreter_objects PROPERTIES EXCLUDE_FROM_ALL $<NOT:${FEATURE_INTERPRETER}>)
endif()

if(CLR_CMAKE_HOST_WIN32)
    add_library_clr(clrinterpreter SHARED $<TARGET_OBJECTS:clrinterpreter_objects> $<TARGET_OBJECTS:dn-containers> ${EXPORTS_FILE})
    set_property(TARGET clrinterpreter APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
else()
    add_library_clr(clrinterpreter SHARED $<TARGET_OBJECTS:clrinterpreter_objects> $<TARGET_OBJECTS:dn-containers>)
    set_exports_linker_option(${EXPORTS_FILE})
    set_property(TARGET clrinterpreter APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET clrinterpreter APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
endif()
add_dependencies(clrinterpreter dn-containers)

target_link_libraries(clrinterpreter
        PRIVATE
        ${INTERPRETER_LINK_LIBRARIES}
    )

install_clr(TARGETS clrinterpreter DESTINATIONS . COMPONENT runtime OPTIONAL)
