/**
 * \file
 * Read the network routing tables using sysctl(3) calls
 * Required for Unix-like systems that don't have Linux's /proc/net/route
 *
 * Author:
 *   Ben Woods (woodsb02@gmail.com)
 */

#include "config.h"

extern const char mono_route_empty_file_no_warning;
const char mono_route_empty_file_no_warning = 0;
