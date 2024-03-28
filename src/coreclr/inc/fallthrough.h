#ifndef __FALLTHROUGH_H__
#define __FALLTHROUGH_H__

#ifndef FALLTHROUGH
#if __has_cpp_attribute(fallthrough)
#define FALLTHROUGH [[fallthrough]]
#else
#define FALLTHROUGH
#endif // __has_cpp_attribute(fallthrough)
#endif //!FALLTHROUGH

#endif //__FALLTHROUGH_H__