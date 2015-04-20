set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories(BEFORE "../vm/amd64")
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
if(WIN32)
  add_subdirectory(v3binder_crossgen)
endif()

