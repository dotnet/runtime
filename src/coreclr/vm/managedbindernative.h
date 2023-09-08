#include "object.h"

// managed Internal.Runtime.Binder.Assembly
class BinderAssemblyObject : public Object
{
public:
    PEImage* m_PEImage;
};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<BinderAssemblyObject> BINDERASSEMBLYREF;
#else // USE_CHECKED_OBJECTREFS
typedef DPTR(BinderAssemblyObject) PTR_BinderAssemblyObject;
typedef PTR_BinderAssemblyObject BINDERASSEMBLYREF;
#endif // USE_CHECKED_OBJECTREFS


// managed Internal.Runtime.Binder.AssemblyBinder
class AssemblyBinderObject : public Object
{

};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<AssemblyBinderObject> ASSEMBLYBINDERREF;
#else // USE_CHECKED_OBJECTREFS
typedef DPTR(AssemblyBinderObject) PTR_AssemblyBinderObject;
typedef AssemblyBinderObject ASSEMBLYBINDERREF;
#endif // USE_CHECKED_OBJECTREFS
