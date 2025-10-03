# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

include_directories(${INC_PLATFORM_DIR})

add_library(usetls ${TEST_LIB_TYPE} testtls.cpp)

install (TARGETS usetls DESTINATION bin)
