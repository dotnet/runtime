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
    compatibility.cpp
    textualidentityparser.cpp
    assemblyidentitycache.cpp
    coreclrbindercommon.cpp
    fusionassemblyname.cpp
    fusionhelpers.cpp
)

set(BINDER_SOURCES
    ${BINDER_COMMON_SOURCES}
    clrprivbinderassemblyloadcontext.cpp
)

set(BINDER_CROSSGEN_SOURCES
    ${BINDER_COMMON_SOURCES}
)

convert_to_absolute_path(BINDER_SOURCES ${BINDER_SOURCES})
convert_to_absolute_path(BINDER_CROSSGEN_SOURCES ${BINDER_CROSSGEN_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(v3binder)
add_subdirectory(v3binder_crossgen)
