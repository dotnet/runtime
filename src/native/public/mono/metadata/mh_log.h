#pragma once
#include <mono/metadata/metadata.h>
#include <stdio.h>
#include <stdlib.h>

#define MH_LVL_TRACE 5
#define MH_LVL_VERBOSE 4
#define MH_LVL_DEBUG 3
#define MH_LVL_INFO 2

static int MH_LOG_indent_level = 0;
static int MH_LOG_verbosity_level = MH_LVL_INFO;
static int get_mh_log_verbosity() {
    const char* env = getenv("MH_LOG_VERBOSITY");
    if (env) {        
        MH_LOG_verbosity_level = atoi(env);
    } else {        
        MH_LOG_verbosity_level = MH_LVL_INFO; // Default verbosity
    }    
    return MH_LOG_verbosity_level;
}

// make default verbosity MH_LVL_DEBUG (3). Set MH_LOG_VERBOSITY to this or higher for more verbose logging 
#define MH_LOG(msg, ...) MH_LOGV(MH_LVL_TRACE, msg, ##__VA_ARGS__)

#define MH_LOGV(verbosity, msg, ...) { \
  if ((verbosity) <= get_mh_log_verbosity()) { \
      printf("MH_NATIVE_LOG: "); \
      for (int mh_idx = 0; mh_idx < MH_LOG_indent_level; mh_idx++) { \
        printf("  "); \
      } \
      printf("%s : %s | %d :: ", __func__, __FILE__, __LINE__); \
      printf(msg, ##__VA_ARGS__); \
      printf("\n"); \
      fflush(stdout); \
  } \
}

#define MH_LOG_INDENT() { \
  MH_LOG_indent_level++; \
} 

#define MH_LOG_UNINDENT() { \
  MH_LOG_indent_level--; \
} 
 
extern void log_mono_type(MonoType* type) ;
extern void log_mono_type_enum(MonoTypeEnum type_enum);
extern void log_mint_type(int value);
