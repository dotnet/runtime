/**
 * \file
 * Socket IO internal calls
 *
 * Authors:
 *	Dick Porter (dick@ximian.com)
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 *
 * This file has been re-licensed under the MIT License:
 * http://opensource.org/licenses/MIT
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#ifndef DISABLE_SOCKETS

#if defined(__APPLE__) || defined(__FreeBSD__)
#define __APPLE_USE_RFC_3542
#endif

#include <glib.h>
#include <string.h>
#include <stdlib.h>
#ifdef HOST_WIN32
#include <ws2tcpip.h>
#else
#include <sys/socket.h>
#ifdef HAVE_SYS_IOCTL_H
#include <sys/ioctl.h>
#endif
#include <netinet/in.h>
#include <netinet/tcp.h>
#ifdef HAVE_NETDB_H
#include <netdb.h>
#endif
#include <arpa/inet.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <errno.h>

#include <sys/types.h>

#include <mono/metadata/object.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/w32file.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/threadpool-io.h>
#include <mono/utils/mono-poll.h>
/* FIXME change this code to not mess so much with the internals */
#include <mono/metadata/class-internals.h>
#include <mono/metadata/domain-internals.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/networking.h>
#include <mono/metadata/w32handle.h>
#include <mono/metadata/w32socket.h>
#include <mono/metadata/w32socket-internals.h>
#include <mono/metadata/w32error.h>

#include <time.h>
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#ifdef HAVE_SYS_IOCTL_H
#include <sys/ioctl.h>
#endif
#ifdef HAVE_NET_IF_H
#include <net/if.h>
#endif

#ifdef HAVE_NETDB_H
#include <netdb.h>
#endif
#ifdef HAVE_SYS_FILIO_H
#include <sys/filio.h>     /* defines FIONBIO and FIONREAD */
#endif
#ifdef HAVE_SYS_SOCKIO_H
#include <sys/sockio.h>    /* defines SIOCATMARK */
#endif
#ifdef HAVE_SYS_UN_H
#include <sys/un.h>
#endif

#ifdef HAVE_GETIFADDRS
// <net/if.h> must be included before <ifaddrs.h>
#include <ifaddrs.h>
#endif

#if defined(_MSC_VER) && G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT)
#include <MSWSock.h>
#endif

#define LOGDEBUG(...)  
/* define LOGDEBUG(...) g_message(__VA_ARGS__)  */

static gboolean
addrinfo_to_IPHostEntry_handles (MonoAddressInfo *info, MonoStringHandleOut h_name, MonoArrayHandleOut h_aliases, MonoArrayHandleOut h_addr_list, gboolean add_local_ips, MonoError *error);

#ifdef HOST_WIN32

static SOCKET
mono_w32socket_socket (int domain, int type, int protocol)
{
	return WSASocket (domain, type, protocol, NULL, 0, WSA_FLAG_OVERLAPPED);
}

static gint
mono_w32socket_bind (SOCKET sock, struct sockaddr *addr, socklen_t addrlen)
{
	return bind (sock, addr, addrlen);
}

static gint
mono_w32socket_getpeername (SOCKET sock, struct sockaddr *name, socklen_t *namelen)
{
	return getpeername (sock, name, namelen);
}

static gint
mono_w32socket_getsockname (SOCKET sock, struct sockaddr *name, socklen_t *namelen)
{
	return getsockname (sock, name, namelen);
}

static gint
mono_w32socket_getsockopt (SOCKET sock, gint level, gint optname, gpointer optval, socklen_t *optlen)
{
	return getsockopt (sock, level, optname, optval, optlen);
}

static gint
mono_w32socket_setsockopt (SOCKET sock, gint level, gint optname, const gpointer optval, socklen_t optlen)
{
	return setsockopt (sock, level, optname, optval, optlen);
}

static gint
mono_w32socket_listen (SOCKET sock, gint backlog)
{
	return listen (sock, backlog);
}

static gint
mono_w32socket_shutdown (SOCKET sock, gint how)
{
	return shutdown (sock, how);
}

static gint
mono_w32socket_ioctl (SOCKET sock, gint32 command, gchar *input, gint inputlen, gchar *output, gint outputlen, glong *written)
{
	return WSAIoctl (sock, command, input, inputlen, output, outputlen, written, NULL, NULL);
}

static gboolean
mono_w32socket_close (SOCKET sock)
{
	return CloseHandle (sock);
}

#endif /* HOST_WIN32 */

static void
abort_syscall (gpointer data)
{
	mono_thread_info_abort_socket_syscall_for_close ((MonoNativeThreadId) (gsize) data);
}

static gint32
convert_family (MonoAddressFamily mono_family)
{
	switch (mono_family) {
	case AddressFamily_Unknown:
	case AddressFamily_ImpLink:
	case AddressFamily_Pup:
	case AddressFamily_Chaos:
	case AddressFamily_Iso:
	case AddressFamily_Ecma:
	case AddressFamily_DataKit:
	case AddressFamily_Ccitt:
	case AddressFamily_DataLink:
	case AddressFamily_Lat:
	case AddressFamily_HyperChannel:
	case AddressFamily_NetBios:
	case AddressFamily_VoiceView:
	case AddressFamily_FireFox:
	case AddressFamily_Banyan:
	case AddressFamily_Atm:
	case AddressFamily_Cluster:
	case AddressFamily_Ieee12844:
	case AddressFamily_NetworkDesigners:
		g_warning ("System.Net.Sockets.AddressFamily has unsupported value 0x%x", mono_family);
		return -1;
	case AddressFamily_Unspecified:
		return AF_UNSPEC;
	case AddressFamily_Unix:
		return AF_UNIX;
	case AddressFamily_InterNetwork:
		return AF_INET;
	case AddressFamily_AppleTalk:
#ifdef AF_APPLETALK
		return AF_APPLETALK;
#else
		return -1;
#endif
	case AddressFamily_InterNetworkV6:
#ifdef HAVE_STRUCT_SOCKADDR_IN6
		return AF_INET6;
#else
		return -1;
#endif
	case AddressFamily_DecNet:
#ifdef AF_DECnet
		return AF_DECnet;
#else
		return -1;
#endif
	case AddressFamily_Ipx:
#ifdef AF_IPX
		return AF_IPX;
#else
		return -1;
#endif
	case AddressFamily_Sna:
#ifdef AF_SNA
		return AF_SNA;
#else
		return -1;
#endif
	case AddressFamily_Irda:
#ifdef AF_IRDA
		return AF_IRDA;
#else
		return -1;
#endif
	default:
		g_warning ("System.Net.Sockets.AddressFamily has unknown value 0x%x", mono_family);
		return -1;
	}
}

static MonoAddressFamily
convert_to_mono_family (guint16 af_family)
{
	switch (af_family) {
	case AF_UNSPEC:
		return AddressFamily_Unspecified;
	case AF_UNIX:
		return AddressFamily_Unix;
	case AF_INET:
		return AddressFamily_InterNetwork;
#ifdef AF_IPX
	case AF_IPX:
		return AddressFamily_Ipx;
#endif
#ifdef AF_SNA
	case AF_SNA:
		return AddressFamily_Sna;
#endif
#ifdef AF_DECnet
	case AF_DECnet:
		return AddressFamily_DecNet;
#endif
#ifdef AF_APPLETALK
	case AF_APPLETALK:
		return AddressFamily_AppleTalk;
#endif
#ifdef HAVE_STRUCT_SOCKADDR_IN6
	case AF_INET6:
		return AddressFamily_InterNetworkV6;
#endif
#ifdef AF_IRDA
	case AF_IRDA:
		return AddressFamily_Irda;
#endif
	default:
		g_warning ("unknown address family 0x%x", af_family);
		return AddressFamily_Unknown;
	}
}

static gint32
convert_type (MonoSocketType mono_type)
{
	switch (mono_type) {
	case SocketType_Stream:
		return SOCK_STREAM;
	case SocketType_Dgram:
		return SOCK_DGRAM;
	case SocketType_Raw:
		return SOCK_RAW;
	case SocketType_Rdm:
#ifdef SOCK_RDM
		return SOCK_RDM;
#else
		return -1;
#endif
	case SocketType_Seqpacket:
#ifdef SOCK_SEQPACKET
		return SOCK_SEQPACKET;
#else
		return -1;
#endif
	case SocketType_Unknown:
		g_warning ("System.Net.Sockets.SocketType has unsupported value 0x%x", mono_type);
		return -1;
	default:
		g_warning ("System.Net.Sockets.SocketType has unknown value 0x%x", mono_type);
		return -1;
	}
}

static gint32
convert_proto (MonoProtocolType mono_proto)
{
	switch (mono_proto) {
	case ProtocolType_IP:
	case ProtocolType_IPv6:
	case ProtocolType_Icmp:
	case ProtocolType_Igmp:
	case ProtocolType_Ggp:
	case ProtocolType_Tcp:
	case ProtocolType_Pup:
	case ProtocolType_Udp:
	case ProtocolType_Idp:
		/* These protocols are known (on my system at least) */
		return mono_proto;
	case ProtocolType_ND:
	case ProtocolType_Raw:
	case ProtocolType_Ipx:
	case ProtocolType_Spx:
	case ProtocolType_SpxII:
	case ProtocolType_Unknown:
		/* These protocols arent */
		g_warning ("System.Net.Sockets.ProtocolType has unsupported value 0x%x", mono_proto);
		return -1;
	default:
		return -1;
	}
}

/* Convert MonoSocketFlags */
static gint32
convert_socketflags (gint32 sflags)
{
	gint32 flags = 0;

	if (!sflags)
		/* SocketFlags.None */
		return 0;

	if (sflags & ~(SocketFlags_OutOfBand | SocketFlags_MaxIOVectorLength | SocketFlags_Peek | 
			SocketFlags_DontRoute | SocketFlags_Partial))
		/* Contains invalid flag values */
		return -1;

#ifdef MSG_OOB
	if (sflags & SocketFlags_OutOfBand)
		flags |= MSG_OOB;
#endif
	if (sflags & SocketFlags_Peek)
		flags |= MSG_PEEK;
	if (sflags & SocketFlags_DontRoute)
		flags |= MSG_DONTROUTE;

	/* Ignore Partial - see bug 349688.  Don't return -1, because
	 * according to the comment in that bug ms runtime doesn't for
	 * UDP sockets (this means we will silently ignore it for TCP
	 * too)
	 */
#ifdef MSG_MORE
	if (sflags & SocketFlags_Partial)
		flags |= MSG_MORE;
#endif
#if 0
	/* Don't do anything for MaxIOVectorLength */
	if (sflags & SocketFlags_MaxIOVectorLength)
		return -1;	
#endif
	return flags;
}

/*
 * Returns:
 *    0 on success (mapped mono_level and mono_name to system_level and system_name
 *   -1 on error
 *   -2 on non-fatal error (ie, must ignore)
 */
static gint32
convert_sockopt_level_and_name (MonoSocketOptionLevel mono_level, MonoSocketOptionName mono_name, int *system_level, int *system_name)
{
	switch (mono_level) {
	case SocketOptionLevel_Socket:
		*system_level = SOL_SOCKET;
		
		switch (mono_name) {
		case SocketOptionName_DontLinger:
			/* This is SO_LINGER, because the setsockopt
			 * internal call maps DontLinger to SO_LINGER
			 * with l_onoff=0
			 */
			*system_name = SO_LINGER;
			break;
#ifdef SO_DEBUG
		case SocketOptionName_Debug:
			*system_name = SO_DEBUG;
			break;
#endif
#ifdef SO_ACCEPTCONN
		case SocketOptionName_AcceptConnection:
			*system_name = SO_ACCEPTCONN;
			break;
#endif
		case SocketOptionName_ReuseAddress:
			*system_name = SO_REUSEADDR;
			break;
		case SocketOptionName_KeepAlive:
			*system_name = SO_KEEPALIVE;
			break;
#ifdef SO_DONTROUTE
		case SocketOptionName_DontRoute:
			*system_name = SO_DONTROUTE;
			break;
#endif
		case SocketOptionName_Broadcast:
			*system_name = SO_BROADCAST;
			break;
		case SocketOptionName_Linger:
			*system_name = SO_LINGER;
			break;
#ifdef SO_OOBINLINE
		case SocketOptionName_OutOfBandInline:
			*system_name = SO_OOBINLINE;
			break;
#endif
		case SocketOptionName_SendBuffer:
			*system_name = SO_SNDBUF;
			break;
		case SocketOptionName_ReceiveBuffer:
			*system_name = SO_RCVBUF;
			break;
		case SocketOptionName_SendLowWater:
			*system_name = SO_SNDLOWAT;
			break;
		case SocketOptionName_ReceiveLowWater:
			*system_name = SO_RCVLOWAT;
			break;
		case SocketOptionName_SendTimeout:
			*system_name = SO_SNDTIMEO;
			break;
		case SocketOptionName_ReceiveTimeout:
			*system_name = SO_RCVTIMEO;
			break;
		case SocketOptionName_Error:
			*system_name = SO_ERROR;
			break;
		case SocketOptionName_Type:
			*system_name = SO_TYPE;
			break;
#ifdef SO_PEERCRED
		case SocketOptionName_PeerCred:
			*system_name = SO_PEERCRED;
			break;
#endif
		case SocketOptionName_ExclusiveAddressUse:
#ifdef SO_EXCLUSIVEADDRUSE
			*system_name = SO_EXCLUSIVEADDRUSE;
			break;
#endif
		case SocketOptionName_UseLoopback:
#ifdef SO_USELOOPBACK
			*system_name = SO_USELOOPBACK;
			break;
#endif
		case SocketOptionName_MaxConnections:
#ifdef SO_MAXCONN
			*system_name = SO_MAXCONN;
			break;
#elif defined(SOMAXCONN)
			*system_name = SOMAXCONN;
			break;
#endif
		default:
			g_warning ("System.Net.Sockets.SocketOptionName 0x%x is not supported at Socket level", mono_name);
			return -1;
		}
		break;
		
	case SocketOptionLevel_IP:
		*system_level = mono_networking_get_ip_protocol ();
		
		switch (mono_name) {
#ifdef IP_OPTIONS
		case SocketOptionName_IPOptions:
			*system_name = IP_OPTIONS;
			break;
#endif
#ifdef IP_HDRINCL
		case SocketOptionName_HeaderIncluded:
			*system_name = IP_HDRINCL;
			break;
#endif
#ifdef IP_TOS
		case SocketOptionName_TypeOfService:
			*system_name = IP_TOS;
			break;
#endif
#ifdef IP_TTL
		case SocketOptionName_IpTimeToLive:
			*system_name = IP_TTL;
			break;
#endif
		case SocketOptionName_MulticastInterface:
			*system_name = IP_MULTICAST_IF;
			break;
		case SocketOptionName_MulticastTimeToLive:
			*system_name = IP_MULTICAST_TTL;
			break;
		case SocketOptionName_MulticastLoopback:
			*system_name = IP_MULTICAST_LOOP;
			break;
		case SocketOptionName_AddMembership:
			*system_name = IP_ADD_MEMBERSHIP;
			break;
		case SocketOptionName_DropMembership:
			*system_name = IP_DROP_MEMBERSHIP;
			break;
#ifdef HAVE_IP_PKTINFO
		case SocketOptionName_PacketInformation:
			*system_name = IP_PKTINFO;
			break;
#endif /* HAVE_IP_PKTINFO */

		case SocketOptionName_DontFragment:
#ifdef HAVE_IP_DONTFRAGMENT
			*system_name = IP_DONTFRAGMENT;
			break;
#elif defined HAVE_IP_MTU_DISCOVER
			/* Not quite the same */
			*system_name = IP_MTU_DISCOVER;
			break;
#else
			/* If the flag is not available on this system, we can ignore this error */
			return -2;
#endif /* HAVE_IP_DONTFRAGMENT */
		case SocketOptionName_AddSourceMembership:
		case SocketOptionName_DropSourceMembership:
		case SocketOptionName_BlockSource:
		case SocketOptionName_UnblockSource:
			/* Can't figure out how to map these, so fall
			 * through
			 */
		default:
			g_warning ("System.Net.Sockets.SocketOptionName 0x%x is not supported at IP level", mono_name);
			return -1;
		}
		break;

	case SocketOptionLevel_IPv6:
		*system_level = mono_networking_get_ipv6_protocol ();

		switch (mono_name) {
		case SocketOptionName_IpTimeToLive:
		case SocketOptionName_HopLimit:
			*system_name = IPV6_UNICAST_HOPS;
			break;
		case SocketOptionName_MulticastInterface:
			*system_name = IPV6_MULTICAST_IF;
			break;
		case SocketOptionName_MulticastTimeToLive:
			*system_name = IPV6_MULTICAST_HOPS;
			break;
		case SocketOptionName_MulticastLoopback:
			*system_name = IPV6_MULTICAST_LOOP;
			break;
		case SocketOptionName_AddMembership:
			*system_name = IPV6_JOIN_GROUP;
			break;
		case SocketOptionName_DropMembership:
			*system_name = IPV6_LEAVE_GROUP;
			break;
		case SocketOptionName_IPv6Only:
#ifdef IPV6_V6ONLY
			*system_name = IPV6_V6ONLY;
#else
			return -1;
#endif
			break;
		case SocketOptionName_PacketInformation:
#ifdef HAVE_IPV6_PKTINFO
			*system_name = IPV6_PKTINFO;
#endif
			break;
		case SocketOptionName_HeaderIncluded:
		case SocketOptionName_IPOptions:
		case SocketOptionName_TypeOfService:
		case SocketOptionName_DontFragment:
		case SocketOptionName_AddSourceMembership:
		case SocketOptionName_DropSourceMembership:
		case SocketOptionName_BlockSource:
		case SocketOptionName_UnblockSource:
			/* Can't figure out how to map these, so fall
			 * through
			 */
		default:
			g_warning ("System.Net.Sockets.SocketOptionName 0x%x is not supported at IPv6 level", mono_name);
			return -1;
		}
		break;	/* SocketOptionLevel_IPv6 */
		
	case SocketOptionLevel_Tcp:
		*system_level = mono_networking_get_tcp_protocol ();
		
		switch (mono_name) {
		case SocketOptionName_NoDelay:
			*system_name = TCP_NODELAY;
			break;
#if 0
			/* The documentation is talking complete
			 * bollocks here: rfc-1222 is titled
			 * 'Advancing the NSFNET Routing Architecture'
			 * and doesn't mention either of the words
			 * "expedite" or "urgent".
			 */
		case SocketOptionName_BsdUrgent:
		case SocketOptionName_Expedited:
#endif
		default:
			g_warning ("System.Net.Sockets.SocketOptionName 0x%x is not supported at TCP level", mono_name);
			return -1;
		}
		break;
		
	case SocketOptionLevel_Udp:
		g_warning ("System.Net.Sockets.SocketOptionLevel has unsupported value 0x%x", mono_level);

		switch(mono_name) {
		case SocketOptionName_NoChecksum:
		case SocketOptionName_ChecksumCoverage:
		default:
			g_warning ("System.Net.Sockets.SocketOptionName 0x%x is not supported at UDP level", mono_name);
			return -1;
		}
		return -1;
		break;

	default:
		g_warning ("System.Net.Sockets.SocketOptionLevel has unknown value 0x%x", mono_level);
		return -1;
	}

	return 0;
}

static MonoImage*
get_socket_assembly (void)
{
	MonoDomain *domain = mono_domain_get ();
	
	if (domain->socket_assembly == NULL) {
		MonoImage *socket_assembly;

		socket_assembly = mono_image_loaded ("System");
		if (!socket_assembly) {
			MonoAssembly *sa = mono_assembly_open_predicate ("System.dll", FALSE, FALSE, NULL, NULL, NULL);
		
			if (!sa) {
				g_assert_not_reached ();
			} else {
				socket_assembly = mono_assembly_get_image (sa);
			}
		}
		mono_atomic_store_release (&domain->socket_assembly, socket_assembly);
	}
	
	return domain->socket_assembly;
}

gpointer
ves_icall_System_Net_Sockets_Socket_Socket_internal (MonoObject *this_obj, gint32 family, gint32 type, gint32 proto, gint32 *werror)
{
	SOCKET sock;
	gint32 sock_family;
	gint32 sock_proto;
	gint32 sock_type;
	
	*werror = 0;
	
	sock_family = convert_family ((MonoAddressFamily)family);
	if (sock_family == -1) {
		*werror = WSAEAFNOSUPPORT;
		return NULL;
	}

	sock_proto = convert_proto ((MonoProtocolType)proto);
	if (sock_proto == -1) {
		*werror = WSAEPROTONOSUPPORT;
		return NULL;
	}
	
	sock_type = convert_type ((MonoSocketType)type);
	if (sock_type == -1) {
		*werror = WSAESOCKTNOSUPPORT;
		return NULL;
	}
	
	sock = mono_w32socket_socket (sock_family, sock_type, sock_proto);

	if (sock == INVALID_SOCKET) {
		*werror = mono_w32socket_get_last_error ();
		return NULL;
	}

	return GUINT_TO_POINTER (sock);
}

/* FIXME: the SOCKET parameter (here and in other functions in this
 * file) is really an IntPtr which needs to be converted to a guint32.
 */
void
ves_icall_System_Net_Sockets_Socket_Close_internal (gsize sock, gint32 *werror)
{
	LOGDEBUG (g_message ("%s: closing 0x%x", __func__, sock));

	*werror = 0;

	/* Clear any pending work item from this socket if the underlying
	 * polling system does not notify when the socket is closed */
	mono_threadpool_io_remove_socket (GPOINTER_TO_INT (sock));

	MONO_ENTER_GC_SAFE;
	mono_w32socket_close ((SOCKET) sock);
	MONO_EXIT_GC_SAFE;
}

gint32
ves_icall_System_Net_Sockets_SocketException_WSAGetLastError_internal (void)
{
	LOGDEBUG (g_message("%s: returning %d", __func__, mono_w32socket_get_last_error ()));

	return mono_w32socket_get_last_error ();
}

gint32
ves_icall_System_Net_Sockets_Socket_Available_internal (gsize sock, gint32 *werror)
{
	int ret;
	guint64 amount;
	
	*werror = 0;

	/* FIXME: this might require amount to be unsigned long. */
	ret = mono_w32socket_get_available (sock, &amount);
	if (ret == SOCKET_ERROR) {
		*werror = mono_w32socket_get_last_error ();
		return 0;
	}
	
	return amount;
}

void
ves_icall_System_Net_Sockets_Socket_Blocking_internal (gsize sock, gboolean block, gint32 *werror)
{
	int ret;
	
	*werror = 0;

	ret = mono_w32socket_set_blocking (sock, block);
	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();
}

gpointer
ves_icall_System_Net_Sockets_Socket_Accept_internal (gsize sock, gint32 *werror, gboolean blocking)
{
	gboolean interrupted;
	SOCKET newsock;

	*werror = 0;

	mono_thread_info_install_interrupt (abort_syscall, (gpointer) (gsize) mono_native_thread_id_get (), &interrupted);
	if (interrupted) {
		*werror = WSAEINTR;
		return NULL;
	}

	MONO_ENTER_GC_SAFE;
	newsock = mono_w32socket_accept (sock, NULL, 0, blocking);
	MONO_EXIT_GC_SAFE;

	if (newsock == INVALID_SOCKET)
		*werror = mono_w32socket_get_last_error ();

	mono_thread_info_uninstall_interrupt (&interrupted);
	if (interrupted)
		*werror = WSAEINTR;

	if (*werror)
		return NULL;
	
	return GUINT_TO_POINTER (newsock);
}

void
ves_icall_System_Net_Sockets_Socket_Listen_internal(gsize sock, guint32 backlog, gint32 *werror)
{
	int ret;
	
	*werror = 0;

	MONO_ENTER_GC_SAFE;

	ret = mono_w32socket_listen (sock, backlog);

	MONO_EXIT_GC_SAFE;

	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();
}

#ifdef HAVE_STRUCT_SOCKADDR_IN6
// Check whether it's ::ffff::0:0.
static gboolean
is_ipv4_mapped_any (const struct in6_addr *addr)
{
	int i;
	
	for (i = 0; i < 10; i++) {
		if (addr->s6_addr [i])
			return FALSE;
	}
	if ((addr->s6_addr [10] != 0xff) || (addr->s6_addr [11] != 0xff))
		return FALSE;
	for (i = 12; i < 16; i++) {
		if (addr->s6_addr [i])
			return FALSE;
	}
	return TRUE;
}
#endif

static MonoObject*
create_object_from_sockaddr (struct sockaddr *saddr, int sa_size, gint32 *werror, MonoError *error)
{
	MonoDomain *domain = mono_domain_get ();
	MonoObject *sockaddr_obj;
	MonoArray *data;
	MonoAddressFamily family;

	error_init (error);

	/* Build a System.Net.SocketAddress object instance */
	if (!domain->sockaddr_class)
		domain->sockaddr_class = mono_class_load_from_name (get_socket_assembly (), "System.Net", "SocketAddress");
	sockaddr_obj = mono_object_new_checked (domain, domain->sockaddr_class, error);
	return_val_if_nok (error, NULL);
	
	/* Locate the SocketAddress data buffer in the object */
	if (!domain->sockaddr_data_field) {
		domain->sockaddr_data_field = mono_class_get_field_from_name (domain->sockaddr_class, "m_Buffer");
		g_assert (domain->sockaddr_data_field);
	}

	/* Locate the SocketAddress data buffer length in the object */
	if (!domain->sockaddr_data_length_field) {
		domain->sockaddr_data_length_field = mono_class_get_field_from_name (domain->sockaddr_class, "m_Size");
		g_assert (domain->sockaddr_data_length_field);
	}

	/* May be the +2 here is too conservative, as sa_len returns
	 * the length of the entire sockaddr_in/in6, including
	 * sizeof (unsigned short) of the family */
	/* We can't really avoid the +2 as all code below depends on this size - INCLUDING unix domain sockets.*/
	data = mono_array_new_cached (domain, mono_get_byte_class (), sa_size + 2, error);
	return_val_if_nok (error, NULL);

	/* The data buffer is laid out as follows:
	 * bytes 0 and 1 are the address family
	 * bytes 2 and 3 are the port info
	 * the rest is the address info
	 */
		
	family = convert_to_mono_family (saddr->sa_family);
	if (family == AddressFamily_Unknown) {
		*werror = WSAEAFNOSUPPORT;
		return NULL;
	}

	mono_array_set (data, guint8, 0, family & 0x0FF);
	mono_array_set (data, guint8, 1, (family >> 8) & 0x0FF);
	
	if (saddr->sa_family == AF_INET) {
		struct sockaddr_in *sa_in = (struct sockaddr_in *)saddr;
		guint16 port = ntohs (sa_in->sin_port);
		guint32 address = ntohl (sa_in->sin_addr.s_addr);
		int buffer_size = 8;
		
		if (sa_size < buffer_size) {
			mono_error_set_exception_instance (error, mono_exception_from_name (mono_get_corlib (), "System", "SystemException"));
			return NULL;
		}
		
		mono_array_set (data, guint8, 2, (port>>8) & 0xff);
		mono_array_set (data, guint8, 3, (port) & 0xff);
		mono_array_set (data, guint8, 4, (address>>24) & 0xff);
		mono_array_set (data, guint8, 5, (address>>16) & 0xff);
		mono_array_set (data, guint8, 6, (address>>8) & 0xff);
		mono_array_set (data, guint8, 7, (address) & 0xff);
	
		mono_field_set_value (sockaddr_obj, domain->sockaddr_data_field, data);
		mono_field_set_value (sockaddr_obj, domain->sockaddr_data_length_field, &buffer_size);

		return sockaddr_obj;
	}
#ifdef HAVE_STRUCT_SOCKADDR_IN6
	else if (saddr->sa_family == AF_INET6) {
		struct sockaddr_in6 *sa_in = (struct sockaddr_in6 *)saddr;
		int i;
		int buffer_size = 28;

		guint16 port = ntohs (sa_in->sin6_port);

		if (sa_size < buffer_size) {
			mono_error_set_exception_instance (error, mono_exception_from_name (mono_get_corlib (), "System", "SystemException"));
			return NULL;
		}

		mono_array_set (data, guint8, 2, (port>>8) & 0xff);
		mono_array_set (data, guint8, 3, (port) & 0xff);
		
		if (is_ipv4_mapped_any (&sa_in->sin6_addr)) {
			// Map ::ffff:0:0 to :: (bug #5502)
			for (i = 0; i < 16; i++)
				mono_array_set (data, guint8, 8 + i, 0);
		} else {
			for (i = 0; i < 16; i++) {
				mono_array_set (data, guint8, 8 + i,
								sa_in->sin6_addr.s6_addr [i]);
			}
		}

		mono_array_set (data, guint8, 24, sa_in->sin6_scope_id & 0xff);
		mono_array_set (data, guint8, 25,
						(sa_in->sin6_scope_id >> 8) & 0xff);
		mono_array_set (data, guint8, 26,
						(sa_in->sin6_scope_id >> 16) & 0xff);
		mono_array_set (data, guint8, 27,
						(sa_in->sin6_scope_id >> 24) & 0xff);

		mono_field_set_value (sockaddr_obj, domain->sockaddr_data_field, data);
		mono_field_set_value (sockaddr_obj, domain->sockaddr_data_length_field, &buffer_size);

		return sockaddr_obj;
	}
#endif
#ifdef HAVE_SYS_UN_H
	else if (saddr->sa_family == AF_UNIX) {
		int i;
		int buffer_size = sa_size + 2;

		for (i = 0; i < sa_size; i++)
			mono_array_set (data, guint8, i + 2, saddr->sa_data [i]);
		
		mono_field_set_value (sockaddr_obj, domain->sockaddr_data_field, data);
		mono_field_set_value (sockaddr_obj, domain->sockaddr_data_length_field, &buffer_size);

		return sockaddr_obj;
	}
#endif
	else {
		*werror = WSAEAFNOSUPPORT;
		return NULL;
	}
}

static int
get_sockaddr_size (int family)
{
	int size;

	size = 0;
	if (family == AF_INET) {
		size = sizeof (struct sockaddr_in);
	}
#ifdef HAVE_STRUCT_SOCKADDR_IN6
	else if (family == AF_INET6) {
		size = sizeof (struct sockaddr_in6);
	}
#endif
#ifdef HAVE_SYS_UN_H
	else if (family == AF_UNIX) {
		size = sizeof (struct sockaddr_un);
	}
#endif
	return size;
}

MonoObject*
ves_icall_System_Net_Sockets_Socket_LocalEndPoint_internal (gsize sock, gint32 af, gint32 *werror)
{
	gchar *sa;
	socklen_t salen;
	int ret;
	MonoObject *result;
	MonoError error;
	
	*werror = 0;
	
	salen = get_sockaddr_size (convert_family ((MonoAddressFamily)af));
	if (salen == 0) {
		*werror = WSAEAFNOSUPPORT;
		return NULL;
	}
	sa = (salen <= 128) ? (gchar *)alloca (salen) : (gchar *)g_malloc0 (salen);

	MONO_ENTER_GC_SAFE;

	ret = mono_w32socket_getsockname (sock, (struct sockaddr *)sa, &salen);

	MONO_EXIT_GC_SAFE;
	
	if (ret == SOCKET_ERROR) {
		*werror = mono_w32socket_get_last_error ();
		if (salen > 128)
			g_free (sa);
		return NULL;
	}
	
	LOGDEBUG (g_message("%s: bound to %s port %d", __func__, inet_ntoa (((struct sockaddr_in *)&sa)->sin_addr), ntohs (((struct sockaddr_in *)&sa)->sin_port)));

	result = create_object_from_sockaddr ((struct sockaddr *)sa, salen, werror, &error);
	if (salen > 128)
		g_free (sa);
	if (!mono_error_ok (&error))
		mono_error_set_pending_exception (&error);
	return result;
}

MonoObject*
ves_icall_System_Net_Sockets_Socket_RemoteEndPoint_internal (gsize sock, gint32 af, gint32 *werror)
{
	gchar *sa;
	socklen_t salen;
	int ret;
	MonoObject *result;
	MonoError error;
	
	*werror = 0;
	
	salen = get_sockaddr_size (convert_family ((MonoAddressFamily)af));
	if (salen == 0) {
		*werror = WSAEAFNOSUPPORT;
		return NULL;
	}
	sa = (salen <= 128) ? (gchar *)alloca (salen) : (gchar *)g_malloc0 (salen);
	/* Note: linux returns just 2 for AF_UNIX. Always. */

	MONO_ENTER_GC_SAFE;

	ret = mono_w32socket_getpeername (sock, (struct sockaddr *)sa, &salen);

	MONO_EXIT_GC_SAFE;

	if (ret == SOCKET_ERROR) {
		*werror = mono_w32socket_get_last_error ();
		if (salen > 128)
			g_free (sa);
		return NULL;
	}
	
	LOGDEBUG (g_message("%s: connected to %s port %d", __func__, inet_ntoa (((struct sockaddr_in *)&sa)->sin_addr), ntohs (((struct sockaddr_in *)&sa)->sin_port)));

	result = create_object_from_sockaddr ((struct sockaddr *)sa, salen, werror, &error);
	if (salen > 128)
		g_free (sa);
	if (!mono_error_ok (&error))
		mono_error_set_pending_exception (&error);
	return result;
}

static struct sockaddr*
create_sockaddr_from_object (MonoObject *saddr_obj, socklen_t *sa_size, gint32 *werror, MonoError *error)
{
	MonoDomain *domain = mono_domain_get ();
	MonoArray *data;
	gint32 family;
	int len;

	error_init (error);

	if (!domain->sockaddr_class)
		domain->sockaddr_class = mono_class_load_from_name (get_socket_assembly (), "System.Net", "SocketAddress");

	/* Locate the SocketAddress data buffer in the object */
	if (!domain->sockaddr_data_field) {
		domain->sockaddr_data_field = mono_class_get_field_from_name (domain->sockaddr_class, "m_Buffer");
		g_assert (domain->sockaddr_data_field);
	}

	/* Locate the SocketAddress data buffer length in the object */
	if (!domain->sockaddr_data_length_field) {
		domain->sockaddr_data_length_field = mono_class_get_field_from_name (domain->sockaddr_class, "m_Size");
		g_assert (domain->sockaddr_data_length_field);
	}

	data = *(MonoArray **)(((char *)saddr_obj) + domain->sockaddr_data_field->offset);

	/* The data buffer is laid out as follows:
	 * byte 0 is the address family low byte
	 * byte 1 is the address family high byte
	 * INET:
	 * 	bytes 2 and 3 are the port info
	 * 	the rest is the address info
	 * UNIX:
	 * 	the rest is the file name
	 */
	len = *(int *)(((char *)saddr_obj) + domain->sockaddr_data_length_field->offset);
	g_assert (len >= 2);

	family = convert_family ((MonoAddressFamily)(mono_array_get (data, guint8, 0) + (mono_array_get (data, guint8, 1) << 8)));
	if (family == AF_INET) {
		struct sockaddr_in *sa;
		guint16 port;
		guint32 address;
		
		if (len < 8) {
			mono_error_set_exception_instance (error, mono_exception_from_name (mono_get_corlib (), "System", "SystemException"));
			return NULL;
		}

		sa = g_new0 (struct sockaddr_in, 1);
		port = (mono_array_get (data, guint8, 2) << 8) +
			mono_array_get (data, guint8, 3);
		address = (mono_array_get (data, guint8, 4) << 24) +
			(mono_array_get (data, guint8, 5) << 16 ) +
			(mono_array_get (data, guint8, 6) << 8) +
			mono_array_get (data, guint8, 7);

		sa->sin_family = family;
		sa->sin_addr.s_addr = htonl (address);
		sa->sin_port = htons (port);

		*sa_size = sizeof (struct sockaddr_in);
		return (struct sockaddr *)sa;
	}
#ifdef HAVE_STRUCT_SOCKADDR_IN6
	else if (family == AF_INET6) {
		struct sockaddr_in6 *sa;
		int i;
		guint16 port;
		guint32 scopeid;
		
		if (len < 28) {
			mono_error_set_exception_instance (error, mono_exception_from_name (mono_get_corlib (), "System", "SystemException"));
			return NULL;
		}

		sa = g_new0 (struct sockaddr_in6, 1);
		port = mono_array_get (data, guint8, 3) +
			(mono_array_get (data, guint8, 2) << 8);
		scopeid = mono_array_get (data, guint8, 24) + 
			(mono_array_get (data, guint8, 25) << 8) + 
			(mono_array_get (data, guint8, 26) << 16) + 
			(mono_array_get (data, guint8, 27) << 24);

		sa->sin6_family = family;
		sa->sin6_port = htons (port);
		sa->sin6_scope_id = scopeid;

		for (i = 0; i < 16; i++)
			sa->sin6_addr.s6_addr [i] = mono_array_get (data, guint8, 8 + i);

		*sa_size = sizeof (struct sockaddr_in6);
		return (struct sockaddr *)sa;
	}
#endif
#ifdef HAVE_SYS_UN_H
	else if (family == AF_UNIX) {
		struct sockaddr_un *sock_un;
		int i;

		/* Need a byte for the '\0' terminator/prefix, and the first
		 * two bytes hold the SocketAddress family
		 */
		if (len - 2 >= sizeof (sock_un->sun_path)) {
			mono_error_set_exception_instance (error, mono_get_exception_index_out_of_range ());
			return NULL;
		}
		
		sock_un = g_new0 (struct sockaddr_un, 1);

		sock_un->sun_family = family;
		for (i = 0; i < len - 2; i++)
			sock_un->sun_path [i] = mono_array_get (data, guint8, i + 2);
		
		*sa_size = len;
		return (struct sockaddr *)sock_un;
	}
#endif
	else {
		*werror = WSAEAFNOSUPPORT;
		return 0;
	}
}

void
ves_icall_System_Net_Sockets_Socket_Bind_internal (gsize sock, MonoObject *sockaddr, gint32 *werror)
{
	MonoError error;
	struct sockaddr *sa;
	socklen_t sa_size;
	int ret;
	
	*werror = 0;
	
	sa = create_sockaddr_from_object (sockaddr, &sa_size, werror, &error);
	if (*werror != 0)
		return;
	if (!mono_error_ok (&error)) {
		mono_error_set_pending_exception (&error);
		return;
	}

	LOGDEBUG (g_message("%s: binding to %s port %d", __func__, inet_ntoa (((struct sockaddr_in *)sa)->sin_addr), ntohs (((struct sockaddr_in *)sa)->sin_port)));

	ret = mono_w32socket_bind (sock, sa, sa_size);

	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();

	g_free (sa);
}

enum {
	SelectModeRead,
	SelectModeWrite,
	SelectModeError
};

MonoBoolean
ves_icall_System_Net_Sockets_Socket_Poll_internal (gsize sock, gint mode,
												   gint timeout, gint32 *werror)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	mono_pollfd *pfds;
	int ret;
	gboolean interrupted;
	time_t start;

	*werror = 0;

	pfds = g_new0 (mono_pollfd, 1);
	pfds->fd = GPOINTER_TO_INT (sock);

	switch (mode) {
	case SelectModeRead:
		pfds->events = MONO_POLLIN;
		break;
	case SelectModeWrite:
		pfds->events = MONO_POLLOUT;
		break;
	default:
		pfds->events = MONO_POLLERR | MONO_POLLHUP | MONO_POLLNVAL;
		break;
	}

	timeout = (timeout >= 0) ? (timeout / 1000) : -1;
	start = time (NULL);

	do {
		mono_thread_info_install_interrupt (abort_syscall, (gpointer) (gsize) mono_native_thread_id_get (), &interrupted);
		if (interrupted) {
			g_free (pfds);
			*werror = WSAEINTR;
			return FALSE;
		}

		MONO_ENTER_GC_SAFE;

		ret = mono_poll (pfds, 1, timeout);

		MONO_EXIT_GC_SAFE;

		mono_thread_info_uninstall_interrupt (&interrupted);
		if (interrupted) {
			g_free (pfds);
			*werror = WSAEINTR;
			return FALSE;
		}

		if (timeout > 0 && ret < 0) {
			int err = errno;
			int sec = time (NULL) - start;
			
			timeout -= sec * 1000;
			if (timeout < 0) {
				timeout = 0;
			}
			
			errno = err;
		}

		if (ret == -1 && errno == EINTR) {
			if (mono_thread_test_state (thread, ThreadState_AbortRequested)) {
				g_free (pfds);
				return FALSE;
			}

			/* Suspend requested? */
			mono_thread_interruption_checkpoint ();

			errno = EINTR;
		}
	} while (ret == -1 && errno == EINTR);

	if (ret == -1) {
		*werror = mono_w32socket_convert_error (errno);
		g_free (pfds);
		return FALSE;
	}

	g_free (pfds);
	return ret != 0;
}

void
ves_icall_System_Net_Sockets_Socket_Connect_internal (gsize sock, MonoObject *sockaddr, gint32 *werror, gboolean blocking)
{
	MonoError error;
	struct sockaddr *sa;
	socklen_t sa_size;
	int ret;
	gboolean interrupted;

	*werror = 0;

	sa = create_sockaddr_from_object (sockaddr, &sa_size, werror, &error);
	if (*werror != 0)
		return;
	if (!mono_error_ok (&error)) {
		mono_error_set_pending_exception (&error);
		return;
	}

	LOGDEBUG (g_message("%s: connecting to %s port %d", __func__, inet_ntoa (((struct sockaddr_in *)sa)->sin_addr), ntohs (((struct sockaddr_in *)sa)->sin_port)));

	mono_thread_info_install_interrupt (abort_syscall, (gpointer) (gsize) mono_native_thread_id_get (), &interrupted);
	if (interrupted) {
		*werror = WSAEINTR;
		return;
	}

	MONO_ENTER_GC_SAFE;
	ret = mono_w32socket_connect (sock, sa, sa_size, blocking);
	MONO_EXIT_GC_SAFE;

	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();

	mono_thread_info_uninstall_interrupt (&interrupted);
	if (interrupted)
		*werror = WSAEINTR;

	g_free (sa);
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT)

void
ves_icall_System_Net_Sockets_Socket_Disconnect_internal (gsize sock, MonoBoolean reuse, gint32 *werror)
{
	gboolean interrupted;

	LOGDEBUG (g_message("%s: disconnecting from socket %p (reuse %d)", __func__, sock, reuse));

	mono_thread_info_install_interrupt (abort_syscall, (gpointer) (gsize) mono_native_thread_id_get (), &interrupted);
	if (interrupted) {
		*werror = WSAEINTR;
		return;
	}

	MONO_ENTER_GC_SAFE;
	*werror = mono_w32socket_disconnect (sock, reuse);
	MONO_EXIT_GC_SAFE;

	mono_thread_info_uninstall_interrupt (&interrupted);
	if (interrupted)
		*werror = WSAEINTR;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT) */

gint32
ves_icall_System_Net_Sockets_Socket_Receive_internal (gsize sock, MonoArray *buffer, gint32 offset, gint32 count, gint32 flags, gint32 *werror, gboolean blocking)
{
	int ret;
	gchar *buf;
	gint32 alen;
	int recvflags = 0;
	gboolean interrupted;
	MonoInternalThread* curthread G_GNUC_UNUSED = mono_thread_internal_current ();
	
	*werror = 0;
	
	alen = mono_array_length (buffer);
	if (offset > alen - count)
		return 0;
	
	buf = mono_array_addr (buffer, gchar, offset);
	
	recvflags = convert_socketflags (flags);
	if (recvflags == -1) {
		*werror = WSAEOPNOTSUPP;
		return 0;
	}

	mono_thread_info_install_interrupt (abort_syscall, (gpointer) (gsize) mono_native_thread_id_get (), &interrupted);
	if (interrupted)
		return 0;

	MONO_ENTER_GC_SAFE;
	ret = mono_w32socket_recv (sock, buf, count, recvflags, blocking);
	MONO_EXIT_GC_SAFE;

	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();

	mono_thread_info_uninstall_interrupt (&interrupted);
	if (interrupted)
		*werror = WSAEINTR;

	if (*werror)
		return 0;

	return ret;
}

gint32
ves_icall_System_Net_Sockets_Socket_Receive_array_internal (gsize sock, MonoArray *buffers, gint32 flags, gint32 *werror, gboolean blocking)
{
	int ret, count;
	gboolean interrupted;
	guint32 recv;
	WSABUF *wsabufs;
	guint32 recvflags = 0;
	
	*werror = 0;
	
	wsabufs = mono_array_addr (buffers, WSABUF, 0);
	count = mono_array_length (buffers);
	
	recvflags = convert_socketflags (flags);
	if (recvflags == -1) {
		*werror = WSAEOPNOTSUPP;
		return 0;
	}

	mono_thread_info_install_interrupt (abort_syscall, (gpointer) (gsize) mono_native_thread_id_get (), &interrupted);
	if (interrupted) {
		*werror = WSAEINTR;
		return 0;
	}

	MONO_ENTER_GC_SAFE;
	ret = mono_w32socket_recvbuffers (sock, wsabufs, count, &recv, &recvflags, NULL, NULL, blocking);
	MONO_EXIT_GC_SAFE;

	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();

	mono_thread_info_uninstall_interrupt (&interrupted);
	if (interrupted)
		*werror = WSAEINTR;

	if (*werror)
		return 0;

	return recv;
}

gint32
ves_icall_System_Net_Sockets_Socket_ReceiveFrom_internal (gsize sock, MonoArray *buffer, gint32 offset, gint32 count, gint32 flags, MonoObject **sockaddr, gint32 *werror, gboolean blocking)
{
	MonoError error;
	int ret;
	gchar *buf;
	gint32 alen;
	int recvflags = 0;
	struct sockaddr *sa;
	socklen_t sa_size;
	gboolean interrupted;
	
	*werror = 0;
	
	alen = mono_array_length (buffer);
	if (offset > alen - count)
		return 0;

	sa = create_sockaddr_from_object (*sockaddr, &sa_size, werror, &error);
	if (*werror != 0)
		return 0;
	if (!mono_error_ok (&error)) {
		mono_error_set_pending_exception (&error);
		return 0;
	}
	
	buf = mono_array_addr (buffer, gchar, offset);
	
	recvflags = convert_socketflags (flags);
	if (recvflags == -1) {
		*werror = WSAEOPNOTSUPP;
		return 0;
	}

	mono_thread_info_install_interrupt (abort_syscall, (gpointer) (gsize) mono_native_thread_id_get (), &interrupted);
	if (interrupted) {
		g_free (sa);
		*werror = WSAEINTR;
		return 0;
	}

	MONO_ENTER_GC_SAFE;
	ret = mono_w32socket_recvfrom (sock, buf, count, recvflags, sa, &sa_size, blocking);
	MONO_EXIT_GC_SAFE;

	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();

	mono_thread_info_uninstall_interrupt (&interrupted);

	if (interrupted)
		*werror = WSAEINTR;

	if (*werror) {
		g_free(sa);
		return 0;
	}

	/* If we didn't get a socket size, then we're probably a
	 * connected connection-oriented socket and the stack hasn't
	 * returned the remote address. All we can do is return null.
	 */
	if (sa_size) {
		*sockaddr = create_object_from_sockaddr (sa, sa_size, werror, &error);
		if (!mono_error_ok (&error)) {
			mono_error_set_pending_exception (&error);
			g_free (sa);
			return 0;
		}
	} else {
		*sockaddr = NULL;
	}

	g_free (sa);
	
	return ret;
}

gint32
ves_icall_System_Net_Sockets_Socket_Send_internal (gsize sock, MonoArray *buffer, gint32 offset, gint32 count, gint32 flags, gint32 *werror, gboolean blocking)
{
	int ret;
	gchar *buf;
	gint32 alen;
	int sendflags = 0;
	gboolean interrupted;
	
	*werror = 0;
	
	alen = mono_array_length (buffer);
	if (offset > alen - count)
		return 0;

	LOGDEBUG (g_message("%s: alen: %d", __func__, alen));
	
	buf = mono_array_addr (buffer, gchar, offset);

	LOGDEBUG (g_message("%s: Sending %d bytes", __func__, count));

	sendflags = convert_socketflags (flags);
	if (sendflags == -1) {
		*werror = WSAEOPNOTSUPP;
		return 0;
	}

	mono_thread_info_install_interrupt (abort_syscall, (gpointer) (gsize) mono_native_thread_id_get (), &interrupted);
	if (interrupted) {
		*werror = WSAEINTR;
		return 0;
	}

	MONO_ENTER_GC_SAFE;
	ret = mono_w32socket_send (sock, buf, count, sendflags, blocking);
	MONO_EXIT_GC_SAFE;

	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();

	mono_thread_info_uninstall_interrupt (&interrupted);
	if (interrupted)
		*werror = WSAEINTR;

	if (*werror)
		return 0;

	return ret;
}

gint32
ves_icall_System_Net_Sockets_Socket_Send_array_internal (gsize sock, MonoArray *buffers, gint32 flags, gint32 *werror, gboolean blocking)
{
	int ret, count;
	guint32 sent;
	WSABUF *wsabufs;
	guint32 sendflags = 0;
	gboolean interrupted;
	
	*werror = 0;
	
	wsabufs = mono_array_addr (buffers, WSABUF, 0);
	count = mono_array_length (buffers);
	
	sendflags = convert_socketflags (flags);
	if (sendflags == -1) {
		*werror = WSAEOPNOTSUPP;
		return 0;
	}

	mono_thread_info_install_interrupt (abort_syscall, (gpointer) (gsize) mono_native_thread_id_get (), &interrupted);
	if (interrupted) {
		*werror = WSAEINTR;
		return 0;
	}

	MONO_ENTER_GC_SAFE;
	ret = mono_w32socket_sendbuffers (sock, wsabufs, count, &sent, sendflags, NULL, NULL, blocking);
	MONO_EXIT_GC_SAFE;

	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();

	mono_thread_info_uninstall_interrupt (&interrupted);
	if (interrupted)
		*werror = WSAEINTR;

	if (*werror)
		return 0;

	return sent;
}

gint32
ves_icall_System_Net_Sockets_Socket_SendTo_internal (gsize sock, MonoArray *buffer, gint32 offset, gint32 count, gint32 flags, MonoObject *sockaddr, gint32 *werror, gboolean blocking)
{
	MonoError error;
	int ret;
	gchar *buf;
	gint32 alen;
	int sendflags = 0;
	struct sockaddr *sa;
	socklen_t sa_size;
	gboolean interrupted;
	
	*werror = 0;
	
	alen = mono_array_length (buffer);
	if (offset > alen - count) {
		return 0;
	}

	sa = create_sockaddr_from_object(sockaddr, &sa_size, werror, &error);
	if (*werror != 0)
		return 0;
	if (!mono_error_ok (&error)) {
		mono_error_set_pending_exception (&error);
		return 0;
	}
	
	LOGDEBUG (g_message ("%s: alen: %d", __func__, alen));
	
	buf = mono_array_addr (buffer, gchar, offset);

	LOGDEBUG (g_message("%s: Sending %d bytes", __func__, count));

	sendflags = convert_socketflags (flags);
	if (sendflags == -1) {
		g_free (sa);
		*werror = WSAEOPNOTSUPP;
		return 0;
	}

	mono_thread_info_install_interrupt (abort_syscall, (gpointer) (gsize) mono_native_thread_id_get (), &interrupted);
	if (interrupted) {
		g_free (sa);
		*werror = WSAEINTR;
		return 0;
	}

	MONO_ENTER_GC_SAFE;
	ret = mono_w32socket_sendto (sock, buf, count, sendflags, sa, sa_size, blocking);
	MONO_EXIT_GC_SAFE;

	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();

	mono_thread_info_uninstall_interrupt (&interrupted);
	if (interrupted)
		*werror = WSAEINTR;

	g_free(sa);

	if (*werror)
		return 0;

	return ret;
}

static SOCKET
Socket_to_SOCKET (MonoObject *sockobj)
{
	MonoSafeHandle *safe_handle;
	MonoClassField *field;
	
	field = mono_class_get_field_from_name (sockobj->vtable->klass, "m_Handle");
	safe_handle = ((MonoSafeHandle *)(*(gpointer *)(((char *)sockobj) + field->offset)));

	if (safe_handle == NULL)
		return -1;

	return (SOCKET)safe_handle->handle;
}

#define POLL_ERRORS (MONO_POLLERR | MONO_POLLHUP | MONO_POLLNVAL)

void
ves_icall_System_Net_Sockets_Socket_Select_internal (MonoArray **sockets, gint32 timeout, gint32 *werror)
{
	MonoError error;
	MonoInternalThread *thread = mono_thread_internal_current ();
	MonoObject *obj;
	mono_pollfd *pfds;
	int nfds, idx;
	int ret;
	int i, count;
	int mode;
	MonoClass *sock_arr_class;
	MonoArray *socks;
	time_t start;
	uintptr_t socks_size;
	gboolean interrupted;

	*werror = 0;

	/* *sockets -> READ, null, WRITE, null, ERROR, null */
	count = mono_array_length (*sockets);
	nfds = count - 3; /* NULL separators */
	pfds = g_new0 (mono_pollfd, nfds);
	mode = idx = 0;
	for (i = 0; i < count; i++) {
		obj = mono_array_get (*sockets, MonoObject *, i);
		if (obj == NULL) {
			mode++;
			continue;
		}

		if (idx >= nfds) {
			/* The socket array was bogus */
			g_free (pfds);
			*werror = WSAEFAULT;
			return;
		}

		pfds [idx].fd = Socket_to_SOCKET (obj);
		pfds [idx].events = (mode == 0) ? MONO_POLLIN : (mode == 1) ? MONO_POLLOUT : POLL_ERRORS;
		idx++;
	}

	timeout = (timeout >= 0) ? (timeout / 1000) : -1;
	start = time (NULL);
	do {
		mono_thread_info_install_interrupt (abort_syscall, (gpointer) (gsize) mono_native_thread_id_get (), &interrupted);
		if (interrupted) {
			g_free (pfds);
			*werror = WSAEINTR;
			return;
		}

		MONO_ENTER_GC_SAFE;

		ret = mono_poll (pfds, nfds, timeout);

		MONO_EXIT_GC_SAFE;

		mono_thread_info_uninstall_interrupt (&interrupted);
		if (interrupted) {
			g_free (pfds);
			*werror = WSAEINTR;
			return;
		}

		if (timeout > 0 && ret < 0) {
			int err = errno;
			int sec = time (NULL) - start;

			timeout -= sec * 1000;
			if (timeout < 0)
				timeout = 0;
			errno = err;
		}

		if (ret == -1 && errno == EINTR) {
			if (mono_thread_test_state (thread, ThreadState_AbortRequested)) {
				g_free (pfds);
				*sockets = NULL;
				return;
			}

			/* Suspend requested? */
			mono_thread_interruption_checkpoint ();

			errno = EINTR;
		}
	} while (ret == -1 && errno == EINTR);
	
	if (ret == -1) {
		*werror = mono_w32socket_convert_error (errno);
		g_free (pfds);
		return;
	}

	if (ret == 0) {
		g_free (pfds);
		*sockets = NULL;
		return;
	}

	sock_arr_class = ((MonoObject *)*sockets)->vtable->klass;
	socks_size = ((uintptr_t)ret) + 3; /* space for the NULL delimiters */
	socks = mono_array_new_full_checked (mono_domain_get (), sock_arr_class, &socks_size, NULL, &error);
	if (!mono_error_ok (&error)) {
		mono_error_set_pending_exception (&error);
		g_free (pfds);
		return;
	}

	mode = idx = 0;
	for (i = 0; i < count && ret > 0; i++) {
		mono_pollfd *pfd;

		obj = mono_array_get (*sockets, MonoObject *, i);
		if (obj == NULL) {
			mode++;
			idx++;
			continue;
		}

		pfd = &pfds [i - mode];
		if (pfd->revents == 0)
			continue;

		ret--;
		if (mode == 0 && (pfd->revents & (MONO_POLLIN | POLL_ERRORS)) != 0) {
			mono_array_setref (socks, idx++, obj);
		} else if (mode == 1 && (pfd->revents & (MONO_POLLOUT | POLL_ERRORS)) != 0) {
			mono_array_setref (socks, idx++, obj);
		} else if ((pfd->revents & POLL_ERRORS) != 0) {
			mono_array_setref (socks, idx++, obj);
		}
	}

	*sockets = socks;
	g_free (pfds);
}

static MonoObject*
int_to_object (MonoDomain *domain, int val, MonoError *error)
{
	return mono_value_box_checked (domain, mono_get_int32_class (), &val, error);
}

void
ves_icall_System_Net_Sockets_Socket_GetSocketOption_obj_internal (gsize sock, gint32 level, gint32 name, MonoObject **obj_val, gint32 *werror)
{
	int system_level = 0;
	int system_name = 0;
	int ret;
	int val = 0;
	socklen_t valsize = sizeof (val);
	struct linger linger;
	socklen_t lingersize = sizeof (linger);
	int time_ms = 0;
	socklen_t time_ms_size = sizeof (time_ms);
#ifdef SO_PEERCRED
#  if defined(__OpenBSD__)
	struct sockpeercred cred;
#  else
	struct ucred cred;
#  endif
	socklen_t credsize = sizeof (cred);
#endif
	MonoError error;
	MonoDomain *domain = mono_domain_get ();
	MonoObject *obj;
	MonoClass *obj_class;
	MonoClassField *field;
	
	*werror = 0;
	
#if !defined(SO_EXCLUSIVEADDRUSE) && defined(SO_REUSEADDR)
	if (level == SocketOptionLevel_Socket && name == SocketOptionName_ExclusiveAddressUse) {
		system_level = SOL_SOCKET;
		system_name = SO_REUSEADDR;
		ret = 0;
	} else
#endif
	{
		ret = convert_sockopt_level_and_name ((MonoSocketOptionLevel)level, (MonoSocketOptionName)name, &system_level, &system_name);
	}

	if (ret == -1) {
		*werror = WSAENOPROTOOPT;
		return;
	}
	if (ret == -2) {
		*obj_val = int_to_object (domain, 0, &error);
		mono_error_set_pending_exception (&error);
		return;
	}

	MONO_ENTER_GC_SAFE;

	/* No need to deal with MulticastOption names here, because
	 * you cant getsockopt AddMembership or DropMembership (the
	 * int getsockopt will error, causing an exception)
	 */
	switch (name) {
	case SocketOptionName_Linger:
	case SocketOptionName_DontLinger:
		ret = mono_w32socket_getsockopt (sock, system_level, system_name, &linger, &lingersize);
		break;
		
	case SocketOptionName_SendTimeout:
	case SocketOptionName_ReceiveTimeout:
		ret = mono_w32socket_getsockopt (sock, system_level, system_name, (char *)&time_ms, &time_ms_size);
		break;

#ifdef SO_PEERCRED
	case SocketOptionName_PeerCred: 
		ret = mono_w32socket_getsockopt (sock, system_level, system_name, &cred, &credsize);
		break;
#endif

	default:
		ret = mono_w32socket_getsockopt (sock, system_level, system_name, &val, &valsize);
	}

	MONO_EXIT_GC_SAFE;

	if (ret == SOCKET_ERROR) {
		*werror = mono_w32socket_get_last_error ();
		return;
	}
	
	switch (name) {
	case SocketOptionName_Linger:
		/* build a System.Net.Sockets.LingerOption */
		obj_class = mono_class_load_from_name (get_socket_assembly (),
											   "System.Net.Sockets",
											   "LingerOption");
		obj = mono_object_new_checked (domain, obj_class, &error);
		if (!mono_error_ok (&error)) {
			mono_error_set_pending_exception (&error);
			return;
		}

		/* Locate and set the fields "bool enabled" and "int
		 * lingerTime"
		 */
		field = mono_class_get_field_from_name(obj_class, "enabled");
		*(guint8 *)(((char *)obj)+field->offset) = linger.l_onoff;

		field = mono_class_get_field_from_name(obj_class, "lingerTime");
		*(guint32 *)(((char *)obj)+field->offset)=linger.l_linger;
		break;
	case SocketOptionName_DontLinger:
		/* construct a bool int in val - true if linger is off */
		obj = int_to_object (domain, !linger.l_onoff, &error);
		mono_error_set_pending_exception (&error);
		break;
	case SocketOptionName_SendTimeout:
	case SocketOptionName_ReceiveTimeout:
		obj = int_to_object (domain, time_ms, &error);
		mono_error_set_pending_exception (&error);
		break;

#ifdef SO_PEERCRED
	case SocketOptionName_PeerCred:  {
		/* 
		 * build a Mono.Posix.PeerCred+PeerCredData if
		 * possible
		 */
		static MonoImage *mono_posix_image = NULL;
		MonoPeerCredData *cred_data;
		
		if (mono_posix_image == NULL) {
			mono_posix_image = mono_image_loaded ("Mono.Posix");
			if (!mono_posix_image) {
				MonoAssembly *sa = mono_assembly_open_predicate ("Mono.Posix.dll", FALSE, FALSE, NULL, NULL, NULL);
				if (!sa) {
					*werror = WSAENOPROTOOPT;
					return;
				} else {
					mono_posix_image = mono_assembly_get_image (sa);
				}
			}
		}
		
		obj_class = mono_class_load_from_name (mono_posix_image,
						 "Mono.Posix",
						 "PeerCredData");
		obj = mono_object_new_checked (domain, obj_class, &error);
		if (!mono_error_ok (&error)) {
			mono_error_set_pending_exception (&error);
			return;
		}
		cred_data = (MonoPeerCredData *)obj;
		cred_data->pid = cred.pid;
		cred_data->uid = cred.uid;
		cred_data->gid = cred.gid;
		break;
	}
#endif

	default:
#if !defined(SO_EXCLUSIVEADDRUSE) && defined(SO_REUSEADDR)
		if (level == SocketOptionLevel_Socket && name == SocketOptionName_ExclusiveAddressUse)
			val = val ? 0 : 1;
#endif
		obj = int_to_object (domain, val, &error);
		mono_error_set_pending_exception (&error);
	}

	*obj_val = obj;
}

void
ves_icall_System_Net_Sockets_Socket_GetSocketOption_arr_internal (gsize sock, gint32 level, gint32 name, MonoArray **byte_val, gint32 *werror)
{
	int system_level = 0;
	int system_name = 0;
	int ret;
	guchar *buf;
	socklen_t valsize;
	
	*werror = 0;
	
	ret = convert_sockopt_level_and_name((MonoSocketOptionLevel)level, (MonoSocketOptionName)name, &system_level,
										 &system_name);
	if (ret == -1) {
		*werror = WSAENOPROTOOPT;
		return;
	}
	if (ret == -2)
		return;

	valsize = mono_array_length (*byte_val);
	buf = mono_array_addr (*byte_val, guchar, 0);

	MONO_ENTER_GC_SAFE;

	ret = mono_w32socket_getsockopt (sock, system_level, system_name, buf, &valsize);

	MONO_EXIT_GC_SAFE;

	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();
}

#if defined(HAVE_STRUCT_IP_MREQN) || defined(HAVE_STRUCT_IP_MREQ)
static struct in_addr
ipaddress_to_struct_in_addr (MonoObject *ipaddr)
{
	struct in_addr inaddr;
	MonoClassField *field;
	
	field = mono_class_get_field_from_name (ipaddr->vtable->klass, "m_Address");

	/* No idea why .net uses a 64bit type to hold a 32bit value...
	 *
	 * Internal value of IPAddess is in little-endian order
	 */
	inaddr.s_addr = GUINT_FROM_LE ((guint32)*(guint64 *)(((char *)ipaddr) + field->offset));
	
	return inaddr;
}

#ifdef HAVE_STRUCT_SOCKADDR_IN6
static struct in6_addr
ipaddress_to_struct_in6_addr (MonoObject *ipaddr)
{
	struct in6_addr in6addr;
	MonoClassField *field;
	MonoArray *data;
	int i;

	field = mono_class_get_field_from_name (ipaddr->vtable->klass, "m_Numbers");
	g_assert (field);
	data = *(MonoArray **)(((char *)ipaddr) + field->offset);

	for (i = 0; i < 8; i++) {
		const guint16 s = GUINT16_TO_BE (mono_array_get (data, guint16, i));

/* Solaris/MacOS have only the 8 bit version. */
#ifndef s6_addr16
		in6addr.s6_addr[2 * i + 1] = (s >> 8) & 0xff;
		in6addr.s6_addr[2 * i] = s & 0xff;
#else
		in6addr.s6_addr16[i] = s;
#endif
	}
	return in6addr;
}
#endif
#endif

#if defined(__APPLE__) || defined(__FreeBSD__)

static int
get_local_interface_id (int family)
{
#if !defined(HAVE_GETIFADDRS) || !defined(HAVE_IF_NAMETOINDEX)
	return 0;
#else
	struct ifaddrs *ifap = NULL, *ptr;
	int idx = 0;
	
	if (getifaddrs (&ifap))
		return 0;
	
	for (ptr = ifap; ptr; ptr = ptr->ifa_next) {
		if (!ptr->ifa_addr || !ptr->ifa_name)
			continue;
		if (ptr->ifa_addr->sa_family != family)
			continue;
		if ((ptr->ifa_flags & IFF_LOOPBACK) != 0)
			continue;
		if ((ptr->ifa_flags & IFF_MULTICAST) == 0)
			continue;
			
		idx = if_nametoindex (ptr->ifa_name);
		break;
	}
	
	freeifaddrs (ifap);
	return idx;
#endif
}

#endif /* defined(__APPLE__) || defined(__FreeBSD__) */

void
ves_icall_System_Net_Sockets_Socket_SetSocketOption_internal (gsize sock, gint32 level, gint32 name, MonoObject *obj_val, MonoArray *byte_val, gint32 int_val, gint32 *werror)
{
	struct linger linger;
	int system_level = 0;
	int system_name = 0;
	int ret;
	int sol_ip;
	int sol_ipv6;

	*werror = 0;

	sol_ipv6 = mono_networking_get_ipv6_protocol ();
	sol_ip = mono_networking_get_ip_protocol ();

	ret = convert_sockopt_level_and_name ((MonoSocketOptionLevel)level, (MonoSocketOptionName)name, &system_level,
										  &system_name);

#if !defined(SO_EXCLUSIVEADDRUSE) && defined(SO_REUSEADDR)
	if (level == SocketOptionLevel_Socket && name == SocketOptionName_ExclusiveAddressUse) {
		system_name = SO_REUSEADDR;
		int_val = int_val ? 0 : 1;
		ret = 0;
	}
#endif

	if (ret == -1) {
		*werror = WSAENOPROTOOPT;
		return;
	}
	if (ret == -2)
		return;

	/* Only one of obj_val, byte_val or int_val has data */
	if (obj_val) {
		MonoClassField *field;
		int valsize;
		
		switch (name) {
		case SocketOptionName_Linger:
			/* Dig out "bool enabled" and "int lingerTime"
			 * fields
			 */
			field = mono_class_get_field_from_name (obj_val->vtable->klass, "enabled");
			linger.l_onoff = *(guint8 *)(((char *)obj_val) + field->offset);
			field = mono_class_get_field_from_name (obj_val->vtable->klass, "lingerTime");
			linger.l_linger = *(guint32 *)(((char *)obj_val) + field->offset);
			
			valsize = sizeof (linger);
			ret = mono_w32socket_setsockopt (sock, system_level, system_name, &linger, valsize);
			break;
		case SocketOptionName_AddMembership:
		case SocketOptionName_DropMembership:
#if defined(HAVE_STRUCT_IP_MREQN) || defined(HAVE_STRUCT_IP_MREQ)
		{
			MonoObject *address = NULL;
#ifdef HAVE_STRUCT_SOCKADDR_IN6
			if (system_level == sol_ipv6) {
				struct ipv6_mreq mreq6;

				/*
				 *	Get group address
				 */
				field = mono_class_get_field_from_name (obj_val->vtable->klass, "m_Group");
				g_assert (field);
				address = *(MonoObject **)(((char *)obj_val) + field->offset);
				
				if (address)
					mreq6.ipv6mr_multiaddr = ipaddress_to_struct_in6_addr (address);

				field = mono_class_get_field_from_name (obj_val->vtable->klass, "m_Interface");
				mreq6.ipv6mr_interface = *(guint64 *)(((char *)obj_val) + field->offset);
				
#if defined(__APPLE__) || defined(__FreeBSD__)
				/*
				* Bug #5504:
				*
				* Mac OS Lion doesn't allow ipv6mr_interface = 0.
				*
				* Tests on Windows and Linux show that the multicast group is only
				* joined on one NIC when interface = 0, so we simply use the interface
				* id from the first non-loopback interface (this is also what
				* Dns.GetHostName (string.Empty) would return).
				*/
				if (!mreq6.ipv6mr_interface)
					mreq6.ipv6mr_interface = get_local_interface_id (AF_INET6);
#endif
					
				ret = mono_w32socket_setsockopt (sock, system_level, system_name, &mreq6, sizeof (mreq6));

				break; // Don't check sol_ip
			}
#endif
			if (system_level == sol_ip) {
#ifdef HAVE_STRUCT_IP_MREQN
				struct ip_mreqn mreq = {{0}};
#else
				struct ip_mreq mreq = {{0}};
#endif /* HAVE_STRUCT_IP_MREQN */
			
				/*
				 * pain! MulticastOption holds two IPAddress
				 * members, so I have to dig the value out of
				 * those :-(
				 */
				field = mono_class_get_field_from_name (obj_val->vtable->klass, "group");
				address = *(MonoObject **)(((char *)obj_val) + field->offset);

				/* address might not be defined and if so, set the address to ADDR_ANY.
				 */
				if (address)
					mreq.imr_multiaddr = ipaddress_to_struct_in_addr (address);

				field = mono_class_get_field_from_name (obj_val->vtable->klass, "localAddress");
				address = *(MonoObject **)(((char *)obj_val) + field->offset);

#ifdef HAVE_STRUCT_IP_MREQN
				if (address)
					mreq.imr_address = ipaddress_to_struct_in_addr (address);

				field = mono_class_get_field_from_name (obj_val->vtable->klass, "ifIndex");
				mreq.imr_ifindex = *(gint32 *)(((char *)obj_val) + field->offset);
#else
				if (address)
					mreq.imr_interface = ipaddress_to_struct_in_addr (address);
#endif /* HAVE_STRUCT_IP_MREQN */

				ret = mono_w32socket_setsockopt (sock, system_level, system_name, &mreq, sizeof (mreq));
			}
			break;
		}
#endif /* HAVE_STRUCT_IP_MREQN || HAVE_STRUCT_IP_MREQ */
		default:
			/* Cause an exception to be thrown */
			*werror = WSAEINVAL;
			return;
		}
	} else if (byte_val!=NULL) {
		int valsize = mono_array_length (byte_val);
		guchar *buf = mono_array_addr (byte_val, guchar, 0);
		
		switch(name) {
		case SocketOptionName_DontLinger:
			if (valsize == 1) {
				linger.l_onoff = (*buf) ? 0 : 1;
				linger.l_linger = 0;
				ret = mono_w32socket_setsockopt (sock, system_level, system_name, &linger, sizeof (linger));
			} else {
				*werror = WSAEINVAL;
			}
			break;
		default:
			ret = mono_w32socket_setsockopt (sock, system_level, system_name, buf, valsize);
			break;
		}
	} else {
		/* ReceiveTimeout/SendTimeout get here */
		switch (name) {
		case SocketOptionName_DontLinger:
			linger.l_onoff = !int_val;
			linger.l_linger = 0;
			ret = mono_w32socket_setsockopt (sock, system_level, system_name, &linger, sizeof (linger));
			break;
		case SocketOptionName_MulticastInterface:
#ifndef HOST_WIN32
#ifdef HAVE_STRUCT_IP_MREQN
			int_val = GUINT32_FROM_BE (int_val);
			if ((int_val & 0xff000000) == 0) {
				/* int_val is interface index */
				struct ip_mreqn mreq = {{0}};
				mreq.imr_ifindex = int_val;
				ret = mono_w32socket_setsockopt (sock, system_level, system_name, (char *) &mreq, sizeof (mreq));
				break;
			}
			int_val = GUINT32_TO_BE (int_val);
#endif /* HAVE_STRUCT_IP_MREQN */
#endif /* HOST_WIN32 */
			/* int_val is in_addr */
			ret = mono_w32socket_setsockopt (sock, system_level, system_name, (char *) &int_val, sizeof (int_val));
			break;
		case SocketOptionName_DontFragment:
#ifdef HAVE_IP_MTU_DISCOVER
			/* Fiddle with the value slightly if we're
			 * turning DF on
			 */
			if (int_val == 1)
				int_val = IP_PMTUDISC_DO;
			/* Fall through */
#endif
			
		default:
			ret = mono_w32socket_setsockopt (sock, system_level, system_name, (char *) &int_val, sizeof (int_val));
		}
	}

	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();
}

void
ves_icall_System_Net_Sockets_Socket_Shutdown_internal (gsize sock, gint32 how, gint32 *werror)
{
	int ret;
	gboolean interrupted;

	*werror = 0;

	mono_thread_info_install_interrupt (abort_syscall, (gpointer) (gsize) mono_native_thread_id_get (), &interrupted);
	if (interrupted) {
		*werror = WSAEINTR;
		return;
	}

	MONO_ENTER_GC_SAFE;

	/* Currently, the values for how (recv=0, send=1, both=2) match the BSD API */
	ret = mono_w32socket_shutdown (sock, how);

	MONO_EXIT_GC_SAFE;

	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();

	mono_thread_info_uninstall_interrupt (&interrupted);
	if (interrupted) {
		*werror = WSAEINTR;
	}

}

gint
ves_icall_System_Net_Sockets_Socket_IOControl_internal (gsize sock, gint32 code, MonoArray *input, MonoArray *output, gint32 *werror)
{
	glong output_bytes = 0;
	gchar *i_buffer, *o_buffer;
	gint i_len, o_len;
	gint ret;

	*werror = 0;
	
	if ((guint32)code == FIONBIO)
		/* Invalid command. Must use Socket.Blocking */
		return -1;

	if (input == NULL) {
		i_buffer = NULL;
		i_len = 0;
	} else {
		i_buffer = mono_array_addr (input, gchar, 0);
		i_len = mono_array_length (input);
	}

	if (output == NULL) {
		o_buffer = NULL;
		o_len = 0;
	} else {
		o_buffer = mono_array_addr (output, gchar, 0);
		o_len = mono_array_length (output);
	}

	MONO_ENTER_GC_SAFE;

	ret = mono_w32socket_ioctl (sock, code, i_buffer, i_len, o_buffer, o_len, &output_bytes);

	MONO_EXIT_GC_SAFE;

	if (ret == SOCKET_ERROR) {
		*werror = mono_w32socket_get_last_error ();
		return -1;
	}

	return (gint)output_bytes;
}

static gboolean 
addrinfo_to_IPHostEntry (MonoAddressInfo *info, MonoString **h_name_raw, MonoArray **h_aliases_raw, MonoArray **h_addr_list_raw, gboolean add_local_ips, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoStringHandle h_name = MONO_HANDLE_NEW (MonoString, NULL);
	MonoArrayHandle h_aliases = MONO_HANDLE_NEW (MonoArray, NULL);
	MonoArrayHandle h_addr_list = MONO_HANDLE_NEW (MonoArray, NULL);
	gboolean ret = addrinfo_to_IPHostEntry_handles (info, h_name, h_aliases, h_addr_list, add_local_ips, error);
	*h_name_raw = MONO_HANDLE_RAW (h_name);
	*h_aliases_raw = MONO_HANDLE_RAW (h_aliases);
	*h_addr_list_raw = MONO_HANDLE_RAW (h_addr_list);
	HANDLE_FUNCTION_RETURN_VAL (ret);
}

static gboolean
addrinfo_add_string (MonoDomain *domain, const char *s, MonoArrayHandle arr, int index, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoStringHandle str = mono_string_new_handle (domain, s, error);
	if (!is_ok (error))
		goto leave;
	MONO_HANDLE_ARRAY_SETREF (arr, index, str);
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));

}

static int
addrinfo_add_local_ips (MonoDomain *domain, MonoArrayHandleOut h_addr_list, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	struct in_addr *local_in = NULL;
	int nlocal_in = 0;
	struct in6_addr *local_in6 = NULL;
	int nlocal_in6 = 0;
	int addr_index = 0;

	error_init (error);
	local_in = (struct in_addr *) mono_get_local_interfaces (AF_INET, &nlocal_in);
	local_in6 = (struct in6_addr *) mono_get_local_interfaces (AF_INET6, &nlocal_in6);
	if (nlocal_in || nlocal_in6) {
		char addr [INET6_ADDRSTRLEN];
		MONO_HANDLE_ASSIGN (h_addr_list,  mono_array_new_handle (domain, mono_get_string_class (), nlocal_in + nlocal_in6, error));
		if (!is_ok (error))
			goto leave;
			
		if (nlocal_in) {
			int i;

			for (i = 0; i < nlocal_in; i++) {
				MonoAddress maddr;
				mono_address_init (&maddr, AF_INET, &local_in [i]);
				if (mono_networking_addr_to_str (&maddr, addr, sizeof (addr))) {
					if (!addrinfo_add_string (domain, addr, h_addr_list, addr_index, error))
						goto leave;
					addr_index++;
				}
			}
		}
#ifdef HAVE_STRUCT_SOCKADDR_IN6
		if (nlocal_in6) {
			int i;

			for (i = 0; i < nlocal_in6; i++) {
				MonoAddress maddr;
				mono_address_init (&maddr, AF_INET6, &local_in6 [i]);
				if (mono_networking_addr_to_str (&maddr, addr, sizeof (addr))) {
					if (!addrinfo_add_string (domain, addr, h_addr_list, addr_index, error))
						goto leave;
					addr_index++;
				}
			}
		}
#endif
	}

leave:
	g_free (local_in);
	g_free (local_in6);
	HANDLE_FUNCTION_RETURN_VAL (addr_index);
}

static gboolean 
addrinfo_to_IPHostEntry_handles (MonoAddressInfo *info, MonoStringHandleOut h_name, MonoArrayHandleOut h_aliases, MonoArrayHandleOut h_addr_list, gboolean add_local_ips, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	MonoAddressEntry *ai = NULL;
	MonoDomain *domain = mono_domain_get ();

	error_init (error);
	MONO_HANDLE_ASSIGN (h_aliases, mono_array_new_handle (domain, mono_get_string_class (), 0, error));
	if (!is_ok (error))
		goto leave;
	if (add_local_ips) {
		int addr_index = addrinfo_add_local_ips (domain, h_addr_list, error);
		if (!is_ok (error))
			goto leave;
		if (addr_index > 0)
			goto leave;
	}

	gint32 count = 0;
	for (ai = info->entries; ai != NULL; ai = ai->next) {
		if (ai->family != AF_INET && ai->family != AF_INET6)
			continue;
		count++;
	}

	int addr_index = 0;
	MONO_HANDLE_ASSIGN (h_addr_list, mono_array_new_handle (domain, mono_get_string_class (), count, error));
	if (!is_ok (error))
		goto leave;

	gboolean name_assigned = FALSE;
	for (ai = info->entries; ai != NULL; ai = ai->next) {
		MonoAddress maddr;
		char buffer [INET6_ADDRSTRLEN]; /* Max. size for IPv6 */

		if ((ai->family != PF_INET) && (ai->family != PF_INET6))
			continue;

		mono_address_init (&maddr, ai->family, &ai->address);
		const char *addr = NULL;
		if (mono_networking_addr_to_str (&maddr, buffer, sizeof (buffer)))
			addr = buffer;
		else
			addr = "";
		if (!addrinfo_add_string (domain, addr, h_addr_list, addr_index, error))
			goto leave;

		if (!name_assigned) {
			name_assigned = TRUE;
			const char *name = ai->canonical_name != NULL ? ai->canonical_name : buffer;
			MONO_HANDLE_ASSIGN (h_name, mono_string_new_handle (domain, name, error));
			if (!is_ok (error))
				goto leave;
		}

		addr_index++;
	}

leave:
	if (info)
		mono_free_address_info (info);

	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

MonoBoolean
ves_icall_System_Net_Dns_GetHostByName_internal (MonoStringHandle host, MonoStringHandleOut h_name, MonoArrayHandleOut h_aliases, MonoArrayHandleOut h_addr_list, gint32 hint, MonoError *error)
{
	gboolean add_local_ips = FALSE, add_info_ok = TRUE;
	gchar this_hostname [256];
	MonoAddressInfo *info = NULL;

	error_init (error);

	char *hostname = mono_string_handle_to_utf8 (host, error);
	return_val_if_nok (error, FALSE);

	if (*hostname == '\0') {
		add_local_ips = TRUE;
		MONO_HANDLE_ASSIGN (h_name, host);
	}

	if (!add_local_ips && gethostname (this_hostname, sizeof (this_hostname)) != -1) {
		if (!strcmp (hostname, this_hostname)) {
			add_local_ips = TRUE;
			MONO_HANDLE_ASSIGN (h_name, host);
		}
	}

#ifdef HOST_WIN32
	// Win32 APIs already returns local interface addresses for empty hostname ("")
	// so we never want to add them manually.
	add_local_ips = FALSE;
	if (mono_get_address_info(hostname, 0, MONO_HINT_CANONICAL_NAME | hint, &info))
		add_info_ok = FALSE;
#else
	if (*hostname && mono_get_address_info (hostname, 0, MONO_HINT_CANONICAL_NAME | hint, &info))
		add_info_ok = FALSE;
#endif

	g_free(hostname);

	if (add_info_ok) {
		MonoBoolean result = addrinfo_to_IPHostEntry_handles (info, h_name, h_aliases, h_addr_list, add_local_ips, error);
		return result;
	}
	return FALSE;
}

MonoBoolean
ves_icall_System_Net_Dns_GetHostByAddr_internal (MonoStringHandle addr, MonoStringHandleOut h_name, MonoArrayHandleOut h_aliases, MonoArrayHandleOut h_addr_list, gint32 hint, MonoError *error)
{
	char *address;
	struct sockaddr_in saddr;
#ifdef HAVE_STRUCT_SOCKADDR_IN6
	struct sockaddr_in6 saddr6;
#endif
	MonoAddressInfo *info = NULL;
	gint32 family;
	gchar hostname [NI_MAXHOST] = { 0 };
	gboolean ret;

	error_init (error);

	address = mono_string_handle_to_utf8 (addr, error);
	return_val_if_nok (error, FALSE);

	if (inet_pton (AF_INET, address, &saddr.sin_addr ) == 1) {
		family = AF_INET;
		saddr.sin_family = AF_INET;
	}
#ifdef HAVE_STRUCT_SOCKADDR_IN6
	else if (inet_pton (AF_INET6, address, &saddr6.sin6_addr) == 1) {
		family = AF_INET6;
		saddr6.sin6_family = AF_INET6;
	}
#endif
	else {
		g_free (address);
		return FALSE;
	}

	g_free (address);

	MONO_ENTER_GC_SAFE;

	switch (family) {
	case AF_INET: {
#if HAVE_SOCKADDR_IN_SIN_LEN
		saddr.sin_len = sizeof (saddr);
#endif
		ret = getnameinfo ((struct sockaddr*)&saddr, sizeof (saddr), hostname, sizeof (hostname), NULL, 0, 0) == 0;
		break;
	}
#ifdef HAVE_STRUCT_SOCKADDR_IN6
	case AF_INET6: {
#if HAVE_SOCKADDR_IN6_SIN_LEN
		saddr6.sin6_len = sizeof (saddr6);
#endif
		ret = getnameinfo ((struct sockaddr*)&saddr6, sizeof (saddr6), hostname, sizeof (hostname), NULL, 0, 0) == 0;
		break;
	}
#endif
	default:
		g_assert_not_reached ();
	}

	MONO_EXIT_GC_SAFE;

	if (!ret)
		return FALSE;

	if (mono_get_address_info (hostname, 0, hint | MONO_HINT_CANONICAL_NAME | MONO_HINT_CONFIGURED_ONLY, &info) != 0)
		return FALSE;

	MonoBoolean result = addrinfo_to_IPHostEntry_handles (info, h_name, h_aliases, h_addr_list, FALSE, error);
	return result;
}

MonoBoolean
ves_icall_System_Net_Dns_GetHostName_internal (MonoStringHandleOut h_name, MonoError *error)
{
	gchar hostname [NI_MAXHOST] = { 0 };
	int ret;

	error_init (error);
	MONO_ENTER_GC_SAFE;
	ret = gethostname (hostname, sizeof (hostname));
	MONO_EXIT_GC_SAFE;
	if (ret == -1)
		return FALSE;

	MONO_HANDLE_ASSIGN (h_name, mono_string_new_handle (mono_domain_get (), hostname, error));
	return TRUE;
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT)
gboolean
ves_icall_System_Net_Sockets_Socket_SendFile_internal (gsize sock, MonoString *filename, MonoArray *pre_buffer, MonoArray *post_buffer, gint flags, gint32 *werror, gboolean blocking)
{
	HANDLE file;
	gboolean ret;
	gboolean interrupted;
	TRANSMIT_FILE_BUFFERS buffers;

	*werror = 0;

	if (filename == NULL)
		return FALSE;

	/* FIXME: replace file by a proper fd that we can call open and close on, as they are interruptible */

	file = mono_w32file_create (mono_string_chars (filename), GENERIC_READ, FILE_SHARE_READ, OPEN_EXISTING, 0);
	if (file == INVALID_HANDLE_VALUE) {
		*werror = mono_w32error_get_last ();
		return FALSE;
	}

	memset (&buffers, 0, sizeof (buffers));
	if (pre_buffer != NULL) {
		buffers.Head = mono_array_addr (pre_buffer, guchar, 0);
		buffers.HeadLength = mono_array_length (pre_buffer);
	}
	if (post_buffer != NULL) {
		buffers.Tail = mono_array_addr (post_buffer, guchar, 0);
		buffers.TailLength = mono_array_length (post_buffer);
	}

	mono_thread_info_install_interrupt (abort_syscall, (gpointer) (gsize) mono_native_thread_id_get (), &interrupted);
	if (interrupted) {
		mono_w32file_close (file);
		mono_w32error_set_last (WSAEINTR);
		return FALSE;
	}

	MONO_ENTER_GC_SAFE;
	ret = mono_w32socket_transmit_file (sock, file, &buffers, flags, blocking);
	MONO_EXIT_GC_SAFE;

	if (!ret)
		*werror = mono_w32socket_get_last_error ();

	mono_thread_info_uninstall_interrupt (&interrupted);
	if (interrupted) {
		mono_w32file_close (file);
		*werror = WSAEINTR;
		return FALSE;
	}

	mono_w32file_close (file);

	if (*werror)
		return FALSE;

	return ret;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT) */

void
mono_network_init (void)
{
	mono_networking_init ();
	mono_w32socket_initialize ();
}

void
mono_network_cleanup (void)
{
	mono_w32socket_cleanup ();
	mono_networking_shutdown ();
}

void
icall_cancel_blocking_socket_operation (MonoThread *thread)
{
	MonoInternalThread *internal;

	internal = thread->internal_thread;
	g_assert (internal);

	mono_thread_info_abort_socket_syscall_for_close (MONO_UINT_TO_NATIVE_THREAD_ID (internal->tid));
}

#endif /* #ifndef DISABLE_SOCKETS */
