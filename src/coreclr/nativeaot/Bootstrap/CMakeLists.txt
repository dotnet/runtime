
function(install_bootstrapper_object targetName destination)
  add_dependencies(nativeaot ${targetName})
  if (MSVC)
    set_target_properties(${targetName} PROPERTIES
      COMPILE_PDB_NAME ${targetName}
      COMPILE_PDB_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>"
    )
  endif()
  if (MSVC)
    install (FILES $<TARGET_OBJECTS:${targetName}> DESTINATION ${destination} COMPONENT nativeaot RENAME ${targetName}.obj)
    install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/${targetName}.pdb DESTINATION ${destination} COMPONENT nativeaot)
  else()
    install (FILES $<TARGET_OBJECTS:${targetName}> DESTINATION ${destination} COMPONENT nativeaot RENAME lib${targetName}.o)
  endif()
endfunction()

add_subdirectory(base)
add_subdirectory(dll)

if (CLR_CMAKE_TARGET_WIN32)
  add_subdirectory(dllmain)
endif()
