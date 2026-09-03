# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

# Mirrors src/coreclr/hosts/corerun/configure.cmake: the shared corerun pal header (corerun.hpp)
# consumes HAVE_DIRENT_D_TYPE / HAVE_GETAUXVAL from this generated config.h. The corehost build
# does not include the CMake check modules globally (unlike the coreclr build), so include them.
include(CheckSymbolExists)
include(CheckStructHasMember)

check_symbol_exists(getauxval sys/auxv.h HAVE_GETAUXVAL)
check_struct_has_member ("struct dirent" d_type dirent.h HAVE_DIRENT_D_TYPE)

configure_file(
	${CMAKE_CURRENT_SOURCE_DIR}/config.h.in
	${CMAKE_CURRENT_BINARY_DIR}/config.h)
