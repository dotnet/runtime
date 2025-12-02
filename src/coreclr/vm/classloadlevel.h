// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// classloadlevel.h


#ifndef _H_CLASSLOADLEVEL
#define _H_CLASSLOADLEVEL

// Class loading is split into phases in order to handle recursion
// through field types and generic instantiations in the presence of
// multiple threads and the possibility of load failures.
//
// This enum represents the level to which a class has been loaded.
// (See GetLoadLevel() on TypeHandle, MethodTable and TypeDesc).
//
// CLASS_LOAD_BEGIN
//
//   Placeholder level used before type has been created or located in ngen image
//
//
// CLASS_LOAD_APPROXPARENTS
//
//   Type has been created, but some fields have been filled in with only
//   "approximate" information for generic type arguments. In
//   particular, the parent class is approximate, and interfaces are
//   generic (instantiation at formal type parameters). Other
//   information (vtable and dictionary) may be based on these
//   approximate type arguments.
//
//
// CLASS_LOAD_EXACTPARENTS
//
//   The generic arguments to parent class and interfaces are exact
//   types, and the whole hierarchy (parent and interfaces) is loaded
//   to this level. However, other dependent types (such as generic arguments)
//   may still be loaded at a lower level.
//
//
// CLASS_DEPENDENCIES_LOADED
//
//   The type is fully loaded, as are all dependents (hierarchy, generic args,
//   canonical MT, etc). For generic instantiations, the constraints
//   have not yet been verified.
//
//
// CLASS_LOADED
//
//   This is a "read-only" verification phase that changes no state other than
//   to flip the IsFullyLoaded() bit. We use this phase to do conformity
//   checks (which can't be done in an earlier phase) on the class in a
//   recursion-proof manner.
//   For eg, we check constraints on generic types, and access checks for
//   the type of (valuetype) fields.
//

enum ClassLoadLevel
{
    CLASS_LOAD_BEGIN,
    CLASS_LOAD_APPROXPARENTS,
    CLASS_LOAD_EXACTPARENTS,
    CLASS_DEPENDENCIES_LOADED,
    CLASS_LOADED,

    CLASS_LOAD_LEVEL_FINAL = CLASS_LOADED,
};


extern const char * const classLoadLevelName[];

#endif // _H_CLASSLOADLEVEL
