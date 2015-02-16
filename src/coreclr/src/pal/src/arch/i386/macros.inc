#if defined(__APPLE__)
#define C_FUNC(name) _##name
#else
#define C_FUNC(name) name
#endif

