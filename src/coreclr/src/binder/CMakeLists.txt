set(CMAKE_INCLUDE_CURRENT_DIR ON)
include_directories(BEFORE "../vm/${ARCH_SOURCES_DIR}")
include_directories(BEFORE "../vm")
include_directories(BEFORE "inc")

set(BINDER_COMMON_SOURCES
    variables.cpp
    utils.cpp
    assemblyname.cpp
    propertymap.cpp
    applicationcontext.cpp
    assembly.cpp
    failurecache.cpp
    assemblybinder.cpp
    stringlexer.cpp
    clrprivbindercoreclr.cpp
    binderinterface.cpp
    debuglog.cpp
    bindinglog.cpp
    cdebuglog.cpp
    textualidentityparser.cpp
    assemblyidentitycache.cpp
    coreclrbindercommon.cpp
    fusionassemblyname.cpp
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
    inc/binderinterface.hpp
    inc/bindertypes.hpp
    inc/bindinglog.hpp
    inc/bindinglog.inl
    inc/bindresult.hpp
    inc/bindresult.inl
    inc/cdebuglog.hpp
    inc/clrprivbindercoreclr.h
    inc/coreclrbindercommon.h
    inc/debuglog.hpp
    inc/failurecache.hpp
    inc/failurecachehashtraits.hpp
    inc/fusionassemblyname.hpp
    inc/fusionhelpers.hpp
    inc/list.hpp
    inc/loadcontext.hpp
    inc/loadcontext.inl
    inc/propertyhashtraits.hpp
    inc/propertymap.hpp
    inc/stringlexer.hpp
    inc/stringlexer.inl
    inc/textualidentityparser.hpp
    inc/utils.hpp
    inc/variables.hpp
)

set(BINDER_SOURCES
    ${BINDER_COMMON_SOURCES}
    clrprivbinderassemblyloadcontext.cpp
)

set(BINDER_HEADERS
    ${BINDER_COMMON_HEADERS}
    inc/clrprivbinderassemblyloadcontext.h
    inc/contextentry.hpp
)

set(BINDER_CROSSGEN_SOURCES
    ${BINDER_COMMON_SOURCES}
)

set(BINDER_CROSSGEN_HEADERS
    ${BINDER_COMMON_HEADERS}
)

if (WIN32)
    list(APPEND BINDER_SOURCES ${BINDER_HEADERS})
    list(APPEND BINDER_CROSSGEN_SOURCES ${BINDER_CROSSGEN_HEADERS})
endif(WIN32)

convert_to_absolute_path(BINDER_SOURCES ${BINDER_SOURCES})
convert_to_absolute_path(BINDER_CROSSGEN_SOURCES ${BINDER_CROSSGEN_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(v3binder)
add_subdirectory(v3binder_crossgen)
