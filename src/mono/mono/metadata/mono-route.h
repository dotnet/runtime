/**
 * \file
 */

#ifndef __MONO_ROUTE_H__
#define __MONO_ROUTE_H__

#if defined(HOST_DARWIN) || defined(HOST_BSD)

#include <sys/socket.h>

#if defined (HOST_IOS) || defined (HOST_WATCHOS) || defined (HOST_APPLETVOS)
// The iOS SDK does not provide the net/route.h header but using the Darwin version works fine.
#include "../../support/ios/net/route.h"
#else
#include <net/route.h>
#endif

#include <mono/metadata/object-internals.h>

in_addr_t gateway_from_rtm (struct rt_msghdr *rtm);

/* Category icalls */
extern MonoBoolean ves_icall_System_Net_NetworkInformation_MacOsIPInterfaceProperties_ParseRouteInfo_internal (MonoString *iface, MonoArray **gw_addr_list);

#endif /* #if defined(HOST_DARWIN) || defined(HOST_BSD) */
#endif /* __MONO_ROUTE_H__ */
