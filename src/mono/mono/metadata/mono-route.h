#ifndef __MONO_ROUTE_H__
#define __MONO_ROUTE_H__

#if defined(PLATFORM_MACOSX) || defined(PLATFORM_BSD)

#include <sys/socket.h>
#include <net/route.h>
#include <mono/metadata/object-internals.h>

in_addr_t gateway_from_rtm (struct rt_msghdr *rtm) MONO_INTERNAL;

/* Category icalls */
extern MonoBoolean ves_icall_System_Net_NetworkInformation_MacOsIPInterfaceProperties_ParseRouteInfo_internal (MonoString *iface, MonoArray **gw_addr_list) MONO_INTERNAL;

#endif /* #if defined(PLATFORM_MACOSX) || defined(PLATFORM_BSD) */
#endif /* __MONO_ROUTE_H__ */
