#pragma once

#ifdef STRING_DLL
# define API __declspec(dllexport)
#else
# define API
#endif

#ifdef __cplusplus
extern "C" {
#endif
    API void printString(char* someString);
#ifdef __cplusplus
}
#endif
