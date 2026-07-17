# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

# Linker launcher that fails the build if an object compiled with LTCG (/GL) is
# pulled in. Such objects are rejected by non-MSVC linkers. MSVC doesn't treat
# this as an error. It prints "module compiled with /GL found" and succeeds.
# This launcher runs the link command and errors if that message is found.

# cmake -P <script> <linker> <linker-arg> ...
# so the command starts at CMAKE_ARGV3.
set(_command "")
math(EXPR _last "${CMAKE_ARGC}-1")
foreach(_i RANGE 3 ${_last})
    list(APPEND _command "${CMAKE_ARGV${_i}}")
endforeach()

execute_process(
    COMMAND ${_command}
    OUTPUT_VARIABLE _linker_stdout
    ERROR_VARIABLE _linker_stderr
    RESULT_VARIABLE _linker_result)

if(_linker_stdout)
    message("${_linker_stdout}")
endif()
if(_linker_stderr)
    message("${_linker_stderr}")
endif()

if("${_linker_stdout}${_linker_stderr}" MATCHES "module compiled with /GL found")
    message(FATAL_ERROR
        "An object compiled with LTCG (/GL) was pulled into this link. LTCG objects are "
        "rejected by non-MSVC linkers such as lld-link. Disable interprocedural optimization "
        "(set INTERPROCEDURAL_OPTIMIZATION OFF) on the target that introduced the /GL object.")
endif()

if(NOT _linker_result EQUAL 0)
    message(FATAL_ERROR "Linker failed: ${_linker_result}")
endif()
