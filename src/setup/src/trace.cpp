#include "trace.h"

static trace::level_t g_level = trace::level_t::Error;

void trace::set_level(trace::level_t new_level)
{
    g_level = new_level;
}

bool trace::is_enabled(trace::level_t level)
{
    return level <= g_level;
}

void trace::verbose(const pal::char_t* format, ...)
{
    if (trace::is_enabled(trace::level_t::Verbose))
    {
        va_list args;
        va_start(args, format);
        pal::err_vprintf(format, args);
        va_end(args);
    }
}

void trace::info(const pal::char_t* format, ...)
{
    if (trace::is_enabled(trace::level_t::Info))
    {
        va_list args;
        va_start(args, format);
        pal::err_vprintf(format, args);
        va_end(args);
    }
}

void trace::error(const pal::char_t* format, ...)
{
    if (trace::is_enabled(trace::level_t::Error))
    {
        va_list args;
        va_start(args, format);
        pal::err_vprintf(format, args);
        va_end(args);
    }
}

void trace::warning(const pal::char_t* format, ...)
{
    if (trace::is_enabled(trace::level_t::Warning))
    {
        va_list args;
        va_start(args, format);
        pal::err_vprintf(format, args);
        va_end(args);
    }
}
