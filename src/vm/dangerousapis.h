// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
////////////////////////////////////////////////////////////////////////////////
// This header file defines the list of dangerous APIs and
// is used by InvokeUtil::IsDangerousMethod.
// Dangerous APIs are the APIs that make security decisions based on the result
// of a stack walk. When these APIs are invoked through reflection or delegate
// the stack walker can be easily confused, resulting in security holes.
////////////////////////////////////////////////////////////////////////////////

#ifndef API_NAMES
#define API_NAMES(...) __VA_ARGS__ 
#endif // !API_NAMES

// ToString is never dangerous but we include it on the Runtime*Info types because of a JScript.Net compat issue.
// JScript.Net tries to invoke these ToString APIs when a Runtime*Info object is compared to another object of a different type (e.g. a string).
// This used to cause a SecurityException in partial trust (which JScript catches) because the API was considered dangerous.
// Now this causes a MethodAccessException in partial trust because the API is inaccessible. So we add them back to the "dangerous" API
// list to maintain compatibility. See Devdiv bug 419443 for details.
DEFINE_DANGEROUS_API(APP_DOMAIN,                API_NAMES("CreateInstance", "CreateComInstanceFrom", "CreateInstanceAndUnwrap", "CreateInstanceFrom", "CreateInstanceFromAndUnwrap ", "DefineDynamicAssembly", "Load"))
DEFINE_DANGEROUS_API(ASSEMBLYBASE,              API_NAMES("CreateInstance", "Load"))
DEFINE_DANGEROUS_API(ASSEMBLY,                  API_NAMES("CreateInstance", "Load"))
DEFINE_DANGEROUS_API(ASSEMBLY_BUILDER,          API_NAMES("CreateInstance", "DefineDynamicAssembly", "DefineDynamicModule"))
DEFINE_DANGEROUS_API(INTERNAL_ASSEMBLY_BUILDER, API_NAMES("CreateInstance"))
DEFINE_DANGEROUS_API(METHOD_BASE,               API_NAMES("Invoke"))
DEFINE_DANGEROUS_API(CONSTRUCTOR_INFO,          API_NAMES("Invoke", \
                                                          "System.Runtime.InteropServices._ConstructorInfo.Invoke_2", \
                                                          "System.Runtime.InteropServices._ConstructorInfo.Invoke_3", \
                                                          "System.Runtime.InteropServices._ConstructorInfo.Invoke_4", \
                                                          "System.Runtime.InteropServices._ConstructorInfo.Invoke_5"))
DEFINE_DANGEROUS_API(CONSTRUCTOR,               API_NAMES("Invoke", "ToString"))
DEFINE_DANGEROUS_API(METHOD_INFO,               API_NAMES("CreateDelegate", "Invoke"))
DEFINE_DANGEROUS_API(METHOD,                    API_NAMES("CreateDelegate", "Invoke", "ToString"))
DEFINE_DANGEROUS_API(DYNAMICMETHOD,             API_NAMES("CreateDelegate", "Invoke", ".ctor"))
DEFINE_DANGEROUS_API(TYPE,                      API_NAMES("InvokeMember"))
DEFINE_DANGEROUS_API(CLASS,                     API_NAMES("InvokeMember", "ToString"))
DEFINE_DANGEROUS_API(TYPE_DELEGATOR,            API_NAMES("InvokeMember"))
DEFINE_DANGEROUS_API(RT_FIELD_INFO,             API_NAMES("GetValue", "SetValue", "ToString"))
DEFINE_DANGEROUS_API(FIELD_INFO,                API_NAMES("GetValue", "SetValue"))
DEFINE_DANGEROUS_API(FIELD,                     API_NAMES("GetValue", "SetValue", "ToString"))
DEFINE_DANGEROUS_API(PROPERTY_INFO,             API_NAMES("GetValue", "SetValue"))
DEFINE_DANGEROUS_API(PROPERTY,                  API_NAMES("GetValue", "SetValue", "ToString"))
DEFINE_DANGEROUS_API(EVENT_INFO,                API_NAMES("AddEventHandler", "RemoveEventHandler"))
DEFINE_DANGEROUS_API(EVENT,                     API_NAMES("AddEventHandler", "RemoveEventHandler", "ToString"))
DEFINE_DANGEROUS_API(RESOURCE_MANAGER,          API_NAMES("GetResourceSet", "InternalGetResourceSet", ".ctor"))








