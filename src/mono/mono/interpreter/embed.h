#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>

int
mono_main (int argc, char* argv[]);

MonoDomain *
mono_interp_init(const char *file);

int
mono_interp_exec(MonoDomain *domain, MonoAssembly *assembly, int argc, char *argv[]);

void        
mono_interp_cleanup(MonoDomain *domain);

