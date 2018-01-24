// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#ifndef KNOWN_EVENT
 #define KNOWN_EVENT(name, provider, level, keyword)
#endif // KNOWN_EVENT

#ifndef DYNAMIC_EVENT
 #define DYNAMIC_EVENT(name, provider, level, keyword, ...)
#endif // DYNAMIC_EVENT

#undef KNOWN_EVENT
#undef DYNAMIC_EVENT
