# Updating idl files

This directory has a variety of .idl files (such as corprof.idl) that need a little special handling when you make changes. Originally when we built on Windows only
the build rules would automatically convert the idls into corresponding .h/.c files and include them in compilations. On non-windows platforms we don't have an equivalent
for midl.exe which did that conversion so we work around the issue by doing:

- Build on Windows as normal, which will generate files in `artifacts\obj\windows.x64.Debug\inc\idls_out\`
- Copy any updated headers into `src\coreclr\pal\prebuilt\inc\`
- If needed, adjust any of the .cpp files in `src\coreclr\pal\prebuilt\idl\` by hand, using the corresponding `artifacts\obj\windows.x64.Debug\inc\idls_out\*_i.c` as a guide.
  - Typically
this is just adding `MIDL_DEFINE_GUID(...)` for any new classes/interfaces that have been added to the idl file.

Include these src changes with the remainder of your work when you submit a PR.
