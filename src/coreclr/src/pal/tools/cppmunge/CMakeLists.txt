# Require at least version 2.8.12 of CMake
cmake_minimum_required(VERSION 2.8.12)

# Set the project name
project(cppmunge)

# Includes

# Compile options

add_definitions(-D_BSD_SOURCE) # Needed to get realpath part of CRT for c99 compat.

# Sources

set(SOURCES cppmunge.c)

# Function to add definitions for a set of configurations. The configurations parameter can 
# contain multiple configurations separated by semicolon (e.g. Debug;Release)

add_executable(cppmunge ${SOURCES})