#pragma once

#include <stdio.h>
#pragma message("Defining MH_LOG")

static int MH_LOG_indent_level = 0;

#define MH_LOG(msg, ...) { \
  printf("MH_NATIVE_LOG: "); \
  for (int i = 0; i < MH_LOG_indent_level; i++) { \
    printf("  "); \
  } \
  printf("%s : %s | %d :: ", __func__, __FILE__, __LINE__); \
  printf(msg, ##__VA_ARGS__); \
  printf("\n"); \
  fflush(stdout); \
}

#define MH_LOG_INDENT() { \
  MH_LOG_indent_level++; \
} 

#define MH_LOG_UNINDENT() { \
  MH_LOG_indent_level--; \
} 
