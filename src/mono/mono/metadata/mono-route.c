/**
 * \file
 * Read the network routing tables using sysctl(3) calls
 * Required for Unix-like systems that don't have Linux's /proc/net/route
 *
 * Author:
 *   Ben Woods (woodsb02@gmail.com)
 */

#include <config.h>

#if defined(HOST_DARWIN) || defined(HOST_BSD)
#include <sys/socket.h>
#include <net/if.h>
#include <net/if_dl.h>
#include <netinet/in.h>
#include <sys/param.h>
#include <sys/sysctl.h>
#include <stdlib.h>
#include <string.h>
#include <mono/metadata/object.h>
#include <mono/metadata/mono-route.h>

extern MonoBoolean ves_icall_System_Net_NetworkInformation_MacOsIPInterfaceProperties_ParseRouteInfo_internal(MonoString *iface, MonoArray **gw_addr_list)
{
	MonoError error;
	size_t needed;
	in_addr_t in;
	int mib[6];
	int num_gws=0, gwnum=0;
	unsigned int ifindex = 0;
	char *buf, *next, *lim, *ifacename;
	struct rt_msghdr *rtm;

	MonoDomain *domain = mono_domain_get ();

	ifacename = mono_string_to_utf8_checked(iface, &error);
	if (mono_error_set_pending_exception (&error))
		return FALSE;

	if ((ifindex = if_nametoindex(ifacename)) == 0)
		return FALSE;
	g_free(ifacename);

	// MIB array defining data to read from sysctl
	mib[0] = CTL_NET;	// Networking
	mib[1] = PF_ROUTE;	// Routing messages
	mib[2] = 0;		// Protocol number (always zero)
	mib[3] = AF_INET;	// Address family (IPv4)
	mib[4] = NET_RT_DUMP;	// Dump routing table
	mib[5] = 0;		//

	// First sysctl call with oldp set to NULL to determine size of available data
	if (sysctl(mib, G_N_ELEMENTS(mib), NULL, &needed, NULL, 0) < 0)
		return FALSE;

	// Allocate suffcient memory for available data based on the previous sysctl call
	if ((buf = g_malloc (needed)) == NULL)
		return FALSE;

	// Second sysctl call to retrieve data into appropriately sized buffer
	if (sysctl(mib, G_N_ELEMENTS(mib), buf, &needed, NULL, 0) < 0) {
		g_free (buf);
		return FALSE;
	}

	lim = buf + needed;
	for (next = buf; next < lim; next += rtm->rtm_msglen) {
		rtm = (struct rt_msghdr *)next;
		if (rtm->rtm_version != RTM_VERSION)
			continue;
		if (rtm->rtm_index != ifindex)
			continue;
		if((in = gateway_from_rtm(rtm)) == 0)
			continue;
		num_gws++;
	}

	*gw_addr_list = mono_array_new_checked (domain, mono_get_string_class (), num_gws, &error);
	goto_if_nok (&error, leave);

	for (next = buf; next < lim; next += rtm->rtm_msglen) {
		rtm = (struct rt_msghdr *)next;
		if (rtm->rtm_version != RTM_VERSION)
			continue;
		if (rtm->rtm_index != ifindex)
			continue;
		if ((in = gateway_from_rtm(rtm)) == 0)
			continue;

		MonoString *addr_string;
		char addr [16], *ptr;
		int len;

		ptr = (char *) &in;
		len = snprintf(addr, sizeof(addr), "%u.%u.%u.%u",
			(unsigned char) ptr [0],
			(unsigned char) ptr [1],
			(unsigned char) ptr [2],
			(unsigned char) ptr [3]);

		if ((len >= sizeof(addr)) || (len < 0))
			// snprintf output truncated
			continue;

		addr_string = mono_string_new_checked (domain, addr, &error);
		goto_if_nok (&error, leave);
		mono_array_setref (*gw_addr_list, gwnum, addr_string);
		gwnum++;
	}
leave:
	g_free (buf);
	return is_ok (&error);
}

in_addr_t gateway_from_rtm(struct rt_msghdr *rtm)
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

#endif /* #if defined(HOST_DARWIN) || defined(HOST_BSD) */
