#pragma once
#include <mono/metadata/metadata.h>
#include <stdio.h>

static int MH_LOG_indent_level = 0;
#if(0)
#define MH_LOG(msg, ...) do { } while (0)
#define MH_LOG_INDENT() do { } while (0)
#define MH_LOG_UNINDENT() do { } while (0)
#else
#define MH_LOG(msg, ...) { \
  printf("MH_NATIVE_LOG: "); \
  for (int mh_idx = 0; mh_idx < MH_LOG_indent_level; mh_idx++) { \
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
#endif 
extern void log_mono_type(MonoType* type) ;
extern void log_mono_type_enum(MonoTypeEnum type_enum);
extern void log_mint_type(int value);
