# user_events support in the runtime

Historically only kernelspace code was allowed to emit events in the Linux kernel, meaning that programs like Perf could only collect system events. Over the years various libraries such as LTTng have been created to allow userspace applications to write to the same trace as the kernel code. The runtime has supported LTTng since very early on, but LTTng has some limitations that are problematic. LTTng requires that all providers and events are known at compile time rather than runtime, and has also recently broken their ABI in a way that is difficult to recover from.

Starting with kernel version 6.4 the user_events feature is available. user_events allows userspace applications to write events to the same traces as kernel events but does not have the limitations of LTTng. It allows dynamic event creation at runtime and has a stable ABI. For this reason we are adding support for user_events to the runtime in .net 9.

# Limitations

Currently the support for user_events is experimental and does not support managed EventSources, it only supports native runtime events such as JIT, GC, class loads, etc.

# How to enable

The support for user_events is off by default and can be enabled in one of two ways.

1. Setting the `DOTNET_EnableUserEvents` environment variable to the value `1`.
2. Setting the `System.Diagnostics.Tracing.UserEvents` configuration value to `true` in either your project file or in your `runtimeconfig.json` file.

# Format

The events are written with the EventHeader format specified at https://github.com/microsoft/LinuxTracepoints/blob/main/libeventheader-tracepoint/include/eventheader/eventheader.h