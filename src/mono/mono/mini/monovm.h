#ifndef _MONO_MINI_MONOVM_H_
#define _MONO_MINI_MONOVM_H_

#include <mono/utils/mono-publib.h>

// MonoVM equivalents of the CoreCLR hosting API and helpers
// Only functional on netcore builds

MONO_API int
monovm_initialize (int propertyCount, const char **propertyKeys, const char **propertyValues);

MONO_API int
monovm_execute_assembly (int argc, const char **argv, const char *managedAssemblyPath, unsigned int *exitCode);

MONO_API int
monovm_shutdown (int *latchedExitCode);

#endif // _MONO_MINI_MONOVM_H_