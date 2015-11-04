#ifndef TRACE_H
#define TRACE_H

#include "pal.h"

namespace trace
{
    enum class level_t
    {
        Error = 0,
        Warning = 1,
        Info = 2,
        Verbose = 3
    };

    void set_level(level_t level);
    bool is_enabled(level_t level);
    void verbose(const pal::char_t* format, ...);
    void info(const pal::char_t* format, ...);
    void warning(const pal::char_t* format, ...);
    void error(const pal::char_t* format, ...);
};

#endif // TRACE_H
