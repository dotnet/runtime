set(CMAKE_INCLUDE_CURRENT_DIR ON)

if(CLR_CMAKE_PLATFORM_ARCH_AMD64)
    include_directories(BEFORE "../vm/amd64")
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
    include_directories(BEFORE "../vm/arm")
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
    include_directories(BEFORE "../vm/arm64")
else()
    message(FATAL_ERROR "Only ARM, ARM64 and AMD64 is supported")
endif()
include_directories(BEFORE "../vm")
include_directories(BEFORE "inc")
      
set(BINDER_SOURCES
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
    clrprivbinderassemblyloadcontext.cpp
    coreclrbindercommon.cpp
    fusionassemblyname.cpp
    fusionhelpers.cpp
)

convert_to_absolute_path(BINDER_SOURCES ${BINDER_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(v3binder)
add_subdirectory(v3binder_crossgen)

