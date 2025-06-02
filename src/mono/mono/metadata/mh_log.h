#ifndef MH_LOG_HEADER_
#define MH_LOG_HEADER_


#include <stdio.h>

#define MH_LOG(msg, ...) { \
  printf("MH_NATIVE_LOG: File %s | Line %d :: ", __FILE__, __LINE__); \
  printf((msg), ##__VA_ARGS__); \
  printf("\n"); \
  fflush(stdout); \
}

#endif
