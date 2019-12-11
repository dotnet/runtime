/**
 * \file
 * Read the network routing tables using sysctl(3) calls
 * Required for Unix-like systems that don't have Linux's /proc/net/route
 *
 * Author:
 *   Ben Woods (woodsb02@gmail.com)
 */

#include "config.h"

#ifndef ENABLE_NETCORE
#if HOST_DARWIN || HOST_BSD

#include <sys/types.h>
#include <sys/socket.h>
#include <net/if.h>
#include <net/if_dl.h>
#include <netinet/in.h>
#include <sys/param.h>
#include <sys/sysctl.h>
#include <stdlib.h>
#include <string.h>

#if HOST_IOS || HOST_WATCHOS || HOST_TVOS
// The iOS SDK does not provide the net/route.h header but using the Darwin version works fine.
#include "../../support/ios/net/route.h"
#else
#include <net/route.h>
#endif

static in_addr_t
gateway_from_rtm (struct rt_msghdr *rtm);

#include "object.h"
#include "icall-decl.h"

MonoBoolean
ves_icall_System_Net_NetworkInformation_MacOsIPInterfaceProperties_ParseRouteInfo (MonoStringHandle iface_handle, MonoArrayHandleOut gw_addr_list_handle, MonoError *error)
{
	MonoString *iface = MONO_HANDLE_RAW (iface_handle);
	MONO_HANDLE_ASSIGN_RAW (gw_addr_list_handle, NULL);

	size_t needed;
	in_addr_t in;
	int mib [6];
	int num_gws = 0, gwnum = 0;
	unsigned int ifindex = 0;
	char *buf = NULL;
	char *next, *lim;
	char *ifacename = NULL;
	struct rt_msghdr *rtm;
	MonoArray *gw_addr_list = NULL;
	MonoStringHandle addr_string_handle = NULL_HANDLE_INIT; // FIXME probably overkill
	MonoDomain *domain = mono_domain_get ();
	MonoBoolean result = FALSE;

	ifacename = mono_string_to_utf8_checked_internal (iface, error);
	goto_if_nok (error, fail);

	if ((ifindex = if_nametoindex (ifacename)) == 0)
		goto fail;

	// MIB array defining data to read from sysctl
	mib [0] = CTL_NET;	// Networking
	mib [1] = PF_ROUTE;	// Routing messages
	mib [2] = 0;		// Protocol number (always zero)
	mib [3] = AF_INET;	// Address family (IPv4)
	mib [4] = NET_RT_DUMP;	// Dump routing table
	mib [5] = 0;		//

	// First sysctl call with oldp set to NULL to determine size of available data
	if (sysctl(mib, G_N_ELEMENTS(mib), NULL, &needed, NULL, 0) < 0)
		goto fail;

	// Allocate suffcient memory for available data based on the previous sysctl call
	if ((buf = g_malloc (needed)) == NULL)
		goto fail;

	// Second sysctl call to retrieve data into appropriately sized buffer
	if (sysctl (mib, G_N_ELEMENTS (mib), buf, &needed, NULL, 0) < 0)
		goto fail;

	lim = buf + needed;
	for (next = buf; next < lim; next += rtm->rtm_msglen) {
		rtm = (struct rt_msghdr *)next;
		if (rtm->rtm_version != RTM_VERSION
			|| rtm->rtm_index != ifindex
			|| (in = gateway_from_rtm (rtm)) == 0)
			continue;
		num_gws++;
	}

	gw_addr_list = mono_array_new_checked (domain, mono_get_string_class (), num_gws, error);
	goto_if_nok (error, leave);

	MONO_HANDLE_ASSIGN_RAW (gw_addr_list_handle, gw_addr_list);

	addr_string_handle = MONO_HANDLE_NEW (MonoString, NULL); // FIXME probably overkill

	for (next = buf; next < lim; next += rtm->rtm_msglen) {
		rtm = (struct rt_msghdr *)next;
		if (rtm->rtm_version != RTM_VERSION
			|| rtm->rtm_index != ifindex
			|| (in = gateway_from_rtm (rtm)) == 0)
			continue;

		MonoString *addr_string;
		char addr [16], *ptr;
		int len;

		ptr = (char *) &in;
		len = snprintf(addr, sizeof (addr), "%u.%u.%u.%u",
			(unsigned char) ptr [0],
			(unsigned char) ptr [1],
			(unsigned char) ptr [2],
			(unsigned char) ptr [3]);

		if (len >= sizeof (addr) || len < 0)
			// snprintf output truncated
			continue;

		addr_string = mono_string_new_checked (domain, addr, error);
		goto_if_nok (error, leave);
		MONO_HANDLE_ASSIGN_RAW (addr_string_handle, addr_string); // FIXME probably overkill
		mono_array_setref_internal (gw_addr_list, gwnum, addr_string);
		gwnum++;
	}
leave:
	result = is_ok (error);
fail:
	g_free (ifacename);
	g_free (buf);
	return result;
}

static in_addr_t
gateway_from_rtm(struct rt_msghdr *rtm)
{
	struct sockaddr *gw;
	unsigned int l;

	struct sockaddr *addr = (struct sockaddr *)(rtm + 1);
	l = roundup(addr->sa_len, sizeof(long)); \
	gw = (struct sockaddr *)((char *) addr + l); \

	if (rtm->rtm_addrs & RTA_GATEWAY) {
		if(gw->sa_family == AF_INET) {
			struct sockaddr_in *sockin = (struct sockaddr_in *)gw;
			return(sockin->sin_addr.s_addr);
		}
	}

	return 0;
}

#else

#include "object.h"
#include "icall-decl.h"

MonoBoolean
ves_icall_System_Net_NetworkInformation_MacOsIPInterfaceProperties_ParseRouteInfo (MonoStringHandle iface_handle, MonoArrayHandleOut gw_addr_list_handle, MonoError *error)
{
	mono_error_set_not_implemented (error, "");
	return FALSE;
}

#endif

#endif

extern const char mono_route_empty_file_no_warning;
const char mono_route_empty_file_no_warning = 0;
