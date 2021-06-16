// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "gcenv.ee.h"
#include "threadsuspend.h"
#include "nativeoverlapped.h"
#include "interoplibinterface.h"

#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#include "comcallablewrapper.h"
#endif // FEATURE_COMINTEROP

#include "gctoclreventsink.h"
#include "configuration.h"
#include "genanalysis.h"
#include "eventpipeadapter.h"

// the method table for the WeakReference class
extern MethodTable* pWeakReferenceMT;

// The canonical method table for WeakReference<T>
extern MethodTable* pWeakReferenceOfTCanonMT;

// Finalizes a weak reference directly.
extern void FinalizeWeakReference(Object* obj);

namespace standalone
{

#include "gcenv.ee.cpp"

} // namespace standalone
