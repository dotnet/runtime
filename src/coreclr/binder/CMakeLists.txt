set(CMAKE_INCLUDE_CURRENT_DIR ON)
include_directories(BEFORE "../vm/${ARCH_SOURCES_DIR}")
include_directories(BEFORE "../vm")
include_directories(BEFORE "inc")

set(BINDER_COMMON_SOURCES
    applicationcontext.cpp
    assembly.cpp
    assemblybindercommon.cpp
    assemblyname.cpp
    bindertracing.cpp
    defaultassemblybinder.cpp
    failurecache.cpp
    textualidentityparser.cpp
    utils.cpp
)

set(BINDER_COMMON_HEADERS
    inc/applicationcontext.hpp
    inc/applicationcontext.inl
    inc/assembly.hpp
    inc/assembly.inl
    inc/assemblybindercommon.hpp
    inc/assemblyentry.hpp
    inc/assemblyhashtraits.hpp
    inc/assemblyidentity.hpp
    inc/assemblyname.hpp
    inc/assemblyname.inl
    inc/assemblyversion.hpp
    inc/assemblyversion.inl
    inc/bindertypes.hpp
    inc/bindertracing.h
    inc/bindresult.hpp
    inc/bindresult.inl
    inc/defaultassemblybinder.h
    inc/failurecache.hpp
    inc/failurecachehashtraits.hpp
    inc/loadcontext.hpp
    inc/loadcontext.inl
    inc/textualidentityparser.hpp
    inc/utils.hpp
)

set(BINDER_SOURCES
    ${BINDER_COMMON_SOURCES}
    activitytracker.cpp
    customassemblybinder.cpp
)

set(BINDER_HEADERS
    ${BINDER_COMMON_HEADERS}
    inc/activitytracker.h
    inc/customassemblybinder.h
    inc/contextentry.hpp
)

if (CLR_CMAKE_TARGET_WIN32)
    list(APPEND BINDER_SOURCES ${BINDER_HEADERS})
endif(CLR_CMAKE_TARGET_WIN32)

convert_to_absolute_path(BINDER_SOURCES ${BINDER_SOURCES})

add_library_clr(v3binder_obj
    OBJECT
    ${BINDER_SOURCES}
)
add_dependencies(v3binder_obj eventing_headers)
add_library(v3binder INTERFACE)
target_sources(v3binder INTERFACE $<TARGET_OBJECTS:v3binder_obj>)
