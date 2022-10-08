
To enable profiling set the following environment variables:
- `CORECLR_ENABLE_PROFILING=1`
- `CORECLR_PROFILER={_CLSID of profiler_}`

# Finding the profiler library
Once profiling is enabled there are two ways we load your profiler, with environment variables (cross-plat) or through the registry (Windows only)

## Environment Variable (cross-plat)
Set one of the following (if all are set, the bitness-specific variables take precedence). The 32/64 ones specify which bitness of profiler is loaded
- `CORECLR_PROFILER_PATH=full path to your profiler's DLL`
- `CORECLR_PROFILER_PATH_32=full path to your profiler's DLL`
- `CORECLR_PROFILER_PATH_64=full path to your profiler's DLL`

If any of these environment variable are present, we skip the registry look up altogether, and just use the path from `CORECLR_PROFILER_PATH` to load your DLL.

A couple things to note about this:
- If you specify `CORECLR_PROFILER_PATH` _and_ register your profiler, then `CORECLR_PROFILER_PATH` always wins.  Even if `CORECLR_PROFILER_PATH` points to an invalid path, we will still use `CORECLR_PROFILER_PATH`, and just fail to load your profiler.
- `CORECLR_PROFILER` is _always required_.  If you specify `CORECLR_PROFILER_PATH`, we skip the registry look up. We still need to know your profiler's CLSID, so we can pass it to your class factory's CreateInstance call.


## Through the registry (Windows Only)
If the `CORECLR_PROFILER_PATH*` environment variables above are not set (and you're running on Windows) then coreclr will look up the CLSID from `CORECLR_PROFILER` in the registry to find the full path to your profiler's DLL. Just like with any COM server DLL, we look for your profiler's CLSID under HKEY_CLASSES_ROOT, which merges the classes from HKLM and HKCU.
