set(MDCOMPILER_SOURCES
  assemblymd.cpp
  assemblymd_emit.cpp
  classfactory.cpp
  custattr_import.cpp
  custattr_emit.cpp
  disp.cpp
  emit.cpp
  filtermanager.cpp
  helper.cpp
  import.cpp
  importhelper.cpp
  mdutil.cpp
  regmeta.cpp
  regmeta_compilersupport.cpp
  regmeta_emit.cpp
  regmeta_import.cpp
  regmeta_imetadatatables.cpp
  regmeta_vm.cpp
  verifylayouts.cpp
)

convert_to_absolute_path(MDCOMPILER_SOURCES ${MDCOMPILER_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(dac)
add_subdirectory(wks)
add_subdirectory(dbi)
add_subdirectory(crossgen)
