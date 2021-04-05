set(CMAKE_INCLUDE_CURRENT_DIR ON)
include_directories(BEFORE "../vm/${ARCH_SOURCES_DIR}")
include_directories(BEFORE "../vm")
include_directories(BEFORE "inc")

set(BINDER_COMMON_SOURCES
    applicationcontext.cpp
    assembly.cpp
    assemblybinder.cpp
    assemblyidentitycache.cpp
    assemblyname.cpp
    bindertracing.cpp
    clrprivbindercoreclr.cpp
    coreclrbindercommon.cpp
    failurecache.cpp
    stringlexer.cpp
    textualidentityparser.cpp
    utils.cpp
)

set(BINDER_COMMON_HEADERS
    inc/applicationcontext.hpp
    inc/applicationcontext.inl
    inc/assembly.hpp
    inc/assembly.inl
    inc/assemblybinder.hpp
    inc/assemblyentry.hpp
    inc/assemblyhashtraits.hpp
    inc/assemblyidentity.hpp
    inc/assemblyidentitycache.hpp
    inc/assemblyname.hpp
    inc/assemblyname.inl
    inc/assemblyversion.hpp
    inc/assemblyversion.inl
    inc/bindertypes.hpp
    inc/bindertracing.h
    inc/bindresult.hpp
    inc/bindresult.inl
    inc/clrprivbindercoreclr.h
    inc/coreclrbindercommon.h
    inc/failurecache.hpp
    inc/failurecachehashtraits.hpp
    inc/loadcontext.hpp
    inc/loadcontext.inl
    inc/stringlexer.hpp
    inc/stringlexer.inl
    inc/textualidentityparser.hpp
    inc/utils.hpp
)

set(BINDER_SOURCES
    ${BINDER_COMMON_SOURCES}
    activitytracker.cpp
    clrprivbinderassemblyloadcontext.cpp
)

set(BINDER_HEADERS
    ${BINDER_COMMON_HEADERS}
    inc/activitytracker.h
    inc/clrprivbinderassemblyloadcontext.h
    inc/contextentry.hpp
)

set(BINDER_CROSSGEN_SOURCES
    ${BINDER_COMMON_SOURCES}
)

set(BINDER_CROSSGEN_HEADERS
    ${BINDER_COMMON_HEADERS}
)

if (CLR_CMAKE_TARGET_WIN32)
    list(APPEND BINDER_SOURCES ${BINDER_HEADERS})
    list(APPEND BINDER_CROSSGEN_SOURCES ${BINDER_CROSSGEN_HEADERS})
endif(CLR_CMAKE_TARGET_WIN32)

convert_to_absolute_path(BINDER_SOURCES ${BINDER_SOURCES})
convert_to_absolute_path(BINDER_CROSSGEN_SOURCES ${BINDER_CROSSGEN_SOURCES})

add_library_clr(v3binder_obj
    OBJECT
    ${BINDER_SOURCES}
)
add_dependencies(v3binder_obj eventing_headers)
add_library(v3binder INTERFACE)
target_sources(v3binder INTERFACE $<TARGET_OBJECTS:v3binder_obj>)

add_library_clr(v3binder_crossgen
    STATIC
    ${BINDER_CROSSGEN_SOURCES}
)
add_dependencies(v3binder_crossgen eventing_headers)
set_target_properties(v3binder_crossgen PROPERTIES CROSSGEN_COMPONENT TRUE)

