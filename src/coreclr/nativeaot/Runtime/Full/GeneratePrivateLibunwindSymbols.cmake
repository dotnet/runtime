if(NOT DEFINED NM_TOOL)
  message(FATAL_ERROR "NM_TOOL is required")
endif()

if(NOT DEFINED OUTPUT_FILE)
  message(FATAL_ERROR "OUTPUT_FILE is required")
endif()

if(NOT DEFINED OBJECT_FILES)
  message(FATAL_ERROR "OBJECT_FILES is required")
endif()

string(REPLACE "|" ";" OBJECT_FILE_LIST "${OBJECT_FILES}")
set(SYMBOLS)

foreach(OBJECT_FILE IN LISTS OBJECT_FILE_LIST)
  if(OBJECT_FILE MATCHES "UnixNativeCodeManager\\.cpp\\.o$")
    continue()
  endif()

  execute_process(
    COMMAND "${NM_TOOL}" -g --defined-only "${OBJECT_FILE}"
    RESULT_VARIABLE NM_RESULT
    OUTPUT_VARIABLE NM_OUTPUT
    ERROR_VARIABLE NM_ERROR)

  if(NOT NM_RESULT EQUAL 0)
    message(FATAL_ERROR "Failed to list symbols from ${OBJECT_FILE}: ${NM_ERROR}")
  endif()

  string(REPLACE "\n" ";" NM_LINES "${NM_OUTPUT}")
  foreach(NM_LINE IN LISTS NM_LINES)
    string(STRIP "${NM_LINE}" NM_LINE)
    if(NM_LINE STREQUAL "")
      continue()
    endif()

    string(REGEX REPLACE "[ \t]+" ";" NM_FIELDS "${NM_LINE}")
    list(LENGTH NM_FIELDS NM_FIELD_COUNT)
    if(NM_FIELD_COUNT LESS 3)
      continue()
    endif()

    list(GET NM_FIELDS 2 SYMBOL_NAME)

    # UnwindHelpers.cpp is linked into the private libunwind object because it
    # instantiates libunwind templates and references libunwind assembly
    # helpers. Its public runtime entrypoints must remain externally visible to
    # UnixNativeCodeManager.cpp.
    if(SYMBOL_NAME MATCHES "^_ZN13UnwindHelpers")
      continue()
    endif()

    list(APPEND SYMBOLS "${SYMBOL_NAME}")
  endforeach()
endforeach()

list(REMOVE_DUPLICATES SYMBOLS)
list(SORT SYMBOLS)

file(WRITE "${OUTPUT_FILE}" "")
foreach(SYMBOL IN LISTS SYMBOLS)
  file(APPEND "${OUTPUT_FILE}" "${SYMBOL}\n")
endforeach()
