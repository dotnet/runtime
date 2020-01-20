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

#include <mono/metadata/w32socket.h>

#if !defined(DISABLE_SOCKETS) && !defined(ENABLE_NETCORE)

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
#ifdef HAVE_NETINET_TCP_H
#include <arpa/inet.h>
#endif
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <errno.h>
#include <mono/utils/mono-errno.h>

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
#include <mono/metadata/image-internals.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/networking.h>
#include <mono/metadata/w32handle.h>
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
#elif defined(HAVE_QP2GETIFADDRS)
/* Bizarrely, IBM i implements this, but AIX doesn't, so on i, it has a different name... */
#include <as400_types.h>
#include <as400_protos.h>
/* Defines to just reuse ifaddrs code */
#define ifaddrs ifaddrs_pase
#define freeifaddrs Qp2freeifaddrs
#define getifaddrs Qp2getifaddrs
#endif

#if defined(_MSC_VER) && G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT)
#include <MSWSock.h>
#endif
#include "icall-decl.h"

#define LOGDEBUG(...)  
/* define LOGDEBUG(...) g_message(__VA_ARGS__)  */

static gboolean
addrinfo_to_IPHostEntry_handles (MonoAddressInfo *info, MonoStringHandleOut h_name, MonoArrayHandleOut h_aliases, MonoArrayHandleOut h_addr_list, gboolean add_local_ips, MonoError *error);

static MonoObjectHandle
create_object_handle_from_sockaddr (struct sockaddr *saddr, int sa_size, gint32 *werror, MonoError *error);

static struct sockaddr*
create_sockaddr_from_handle (MonoObjectHandle saddr_obj, socklen_t *sa_size, gint32 *werror, MonoError *error);

#ifdef HOST_WIN32

static SOCKET
mono_w32socket_socket (int domain, int type, int protocol)
{
	SOCKET ret;
	MONO_ENTER_GC_SAFE;
	ret = WSASocket (domain, type, protocol, NULL, 0, WSA_FLAG_OVERLAPPED);
	MONO_EXIT_GC_SAFE;
	return ret;
}

static gint
mono_w32socket_bind (SOCKET sock, struct sockaddr *addr, socklen_t addrlen)
{
	gint ret;
	MONO_ENTER_GC_SAFE;
	ret = bind (sock, addr, addrlen);
	MONO_EXIT_GC_SAFE;
	return ret;
}

static gint
mono_w32socket_getpeername (SOCKET sock, struct sockaddr *name, socklen_t *namelen)
{
	gint ret;
	MONO_ENTER_GC_SAFE;
	ret = getpeername (sock, name, namelen);
	MONO_EXIT_GC_SAFE;
	return ret;
}

static gint
mono_w32socket_getsockname (SOCKET sock, struct sockaddr *name, socklen_t *namelen)
{
	gint ret;
	MONO_ENTER_GC_SAFE;
	ret = getsockname (sock, name, namelen);
	MONO_EXIT_GC_SAFE;
	return ret;
}

static gint
mono_w32socket_getsockopt (SOCKET sock, gint level, gint optname, gpointer optval, socklen_t *optlen)
{
	gint ret;
	MONO_ENTER_GC_SAFE;
	ret = getsockopt (sock, level, optname, (char*)optval, optlen);
	MONO_EXIT_GC_SAFE;
	return ret;
}

static gint
mono_w32socket_setsockopt (SOCKET sock, gint level, gint optname, gconstpointer optval, socklen_t optlen)
{
	gint ret;
	MONO_ENTER_GC_SAFE;
	ret = setsockopt (sock, level, optname, (const char*)optval, optlen);
	MONO_EXIT_GC_SAFE;
	return ret;
}

static gint
mono_w32socket_listen (SOCKET sock, gint backlog)
{
	gint ret;
	MONO_ENTER_GC_SAFE;
	ret = listen (sock, backlog);
	MONO_EXIT_GC_SAFE;
	return ret;
}

static gint
mono_w32socket_shutdown (SOCKET sock, gint how)
{
	gint ret;
	MONO_ENTER_GC_SAFE;
	ret = shutdown (sock, how);
	MONO_EXIT_GC_SAFE;
	return ret;
}

static gint
mono_w32socket_ioctl (SOCKET sock, gint32 command, gchar *input, gint inputlen, gchar *output, gint outputlen, DWORD *written)
{
	gint ret;
	MONO_ENTER_GC_SAFE;
	ret = WSAIoctl (sock, command, input, inputlen, output, outputlen, written, NULL, NULL);
	MONO_EXIT_GC_SAFE;
	return ret;
}

static gboolean
mono_w32socket_close (SOCKET sock)
{
	gboolean ret;
	MONO_ENTER_GC_SAFE;
	ret = closesocket (sock);
	MONO_EXIT_GC_SAFE;
	return ret;
}

#endif /* HOST_WIN32 */

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
		MonoAssemblyLoadContext *alc = mono_domain_default_alc (domain);

		socket_assembly = mono_image_loaded_internal (alc, "System", FALSE);
		if (!socket_assembly) {
			MonoAssemblyOpenRequest req;
			mono_assembly_request_prepare_open (&req, MONO_ASMCTX_DEFAULT, alc);
			MonoAssembly *sa = mono_assembly_request_open ("System.dll", &req, NULL);
		
			if (!sa) {
				g_assert_not_reached ();
			} else {
				socket_assembly = mono_assembly_get_image_internal (sa);
			}
		}
		mono_atomic_store_release (&domain->socket_assembly, socket_assembly);
	}
	
	return domain->socket_assembly;
}

gpointer
ves_icall_System_Net_Sockets_Socket_Socket_icall (gint32 family, gint32 type, gint32 proto, gint32 *werror, MonoError *error)
{
	SOCKET sock;
	gint32 sock_family;
	gint32 sock_proto;
	gint32 sock_type;
	
	error_init (error);
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
ves_icall_System_Net_Sockets_Socket_Close_icall (gsize sock, gint32 *werror)
{
	LOGDEBUG (g_message ("%s: closing 0x%x", __func__, sock));

	*werror = 0;

	/* Clear any pending work item from this socket if the underlying
	 * polling system does not notify when the socket is closed */
	mono_threadpool_io_remove_socket (GPOINTER_TO_INT (sock));

	mono_w32socket_close ((SOCKET) sock);
}

gint32
ves_icall_System_Net_Sockets_SocketException_WSAGetLastError_icall (void)
{
	LOGDEBUG (g_message("%s: returning %d", __func__, mono_w32socket_get_last_error ()));

	return mono_w32socket_get_last_error ();
}

gint32
ves_icall_System_Net_Sockets_Socket_Available_icall (gsize sock, gint32 *werror)
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
ves_icall_System_Net_Sockets_Socket_Blocking_icall (gsize sock, MonoBoolean block, gint32 *werror)
{
	int ret;
	
	*werror = 0;

	ret = mono_w32socket_set_blocking (sock, block);
	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();
}

gpointer
ves_icall_System_Net_Sockets_Socket_Accept_icall (gsize sock, gint32 *werror, MonoBoolean blocking)
{
	SOCKET newsock;

	*werror = 0;

	newsock = mono_w32socket_accept (sock, NULL, 0, blocking);
	if (newsock == INVALID_SOCKET) {
		*werror = mono_w32socket_get_last_error ();
		return NULL;
	}
	
	return GUINT_TO_POINTER (newsock);
}

void
ves_icall_System_Net_Sockets_Socket_Listen_icall (gsize sock, guint32 backlog, gint32 *werror)
{
	int ret;
	
	*werror = 0;

	ret = mono_w32socket_listen (sock, backlog);
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

static MonoObjectHandle
create_object_handle_from_sockaddr (struct sockaddr *saddr, int sa_size, gint32 *werror, MonoError *error)
{
	MonoDomain *domain = mono_domain_get ();
	MonoAddressFamily family;

	error_init (error);

	/* Build a System.Net.SocketAddress object instance */
	if (!domain->sockaddr_class)
		domain->sockaddr_class = mono_class_load_from_name (get_socket_assembly (), "System.Net", "SocketAddress");
	MonoObjectHandle sockaddr_obj = mono_object_new_handle (domain, domain->sockaddr_class, error);
	return_val_if_nok (error, MONO_HANDLE_NEW (MonoObject, NULL));
	
	/* Locate the SocketAddress data buffer in the object */
	if (!domain->sockaddr_data_field) {
		domain->sockaddr_data_field = mono_class_get_field_from_name_full (domain->sockaddr_class, "m_Buffer", NULL);
		g_assert (domain->sockaddr_data_field);
	}

	/* Locate the SocketAddress data buffer length in the object */
	if (!domain->sockaddr_data_length_field) {
		domain->sockaddr_data_length_field = mono_class_get_field_from_name_full (domain->sockaddr_class, "m_Size", NULL);
		g_assert (domain->sockaddr_data_length_field);
	}

	/* May be the +2 here is too conservative, as sa_len returns
	 * the length of the entire sockaddr_in/in6, including
	 * sizeof (unsigned short) of the family */
	/* We can't really avoid the +2 as all code below depends on this size - INCLUDING unix domain sockets.*/
	MonoArrayHandle data = mono_array_new_handle (domain, mono_get_byte_class (), sa_size + 2, error);
	return_val_if_nok (error, MONO_HANDLE_NEW (MonoObject, NULL));

	/* The data buffer is laid out as follows:
	 * bytes 0 and 1 are the address family
	 * bytes 2 and 3 are the port info
	 * the rest is the address info
	 */
		
	family = convert_to_mono_family (saddr->sa_family);
	if (family == AddressFamily_Unknown) {
		*werror = WSAEAFNOSUPPORT;
		return MONO_HANDLE_NEW (MonoObject, NULL);
	}

	MONO_HANDLE_ARRAY_SETVAL (data, guint8, 0, family & 0x0FF);
	MONO_HANDLE_ARRAY_SETVAL (data, guint8, 1, (family >> 8) & 0x0FF);
	
	if (saddr->sa_family == AF_INET) {
		struct sockaddr_in *sa_in = (struct sockaddr_in *)saddr;
		guint16 port = ntohs (sa_in->sin_port);
		guint32 address = ntohl (sa_in->sin_addr.s_addr);
		int buffer_size = 8;
		
		if (sa_size < buffer_size) {
			mono_error_set_generic_error (error, "System", "SystemException", "");
			return MONO_HANDLE_NEW (MonoObject, NULL);
		}
		
		MONO_HANDLE_ARRAY_SETVAL (data, guint8, 2, (port>>8) & 0xff);
		MONO_HANDLE_ARRAY_SETVAL (data, guint8, 3, (port) & 0xff);
		MONO_HANDLE_ARRAY_SETVAL (data, guint8, 4, (address>>24) & 0xff);
		MONO_HANDLE_ARRAY_SETVAL (data, guint8, 5, (address>>16) & 0xff);
		MONO_HANDLE_ARRAY_SETVAL (data, guint8, 6, (address>>8) & 0xff);
		MONO_HANDLE_ARRAY_SETVAL (data, guint8, 7, (address) & 0xff);
	
		mono_field_set_value_internal (MONO_HANDLE_RAW (sockaddr_obj), domain->sockaddr_data_field, MONO_HANDLE_RAW (data)); /* FIXME: use handles for mono_field_set_value */
		mono_field_set_value_internal (MONO_HANDLE_RAW (sockaddr_obj), domain->sockaddr_data_length_field, &buffer_size); /* FIXME: use handles for mono_field_set_value */

		return sockaddr_obj;
	}
#ifdef HAVE_STRUCT_SOCKADDR_IN6
	else if (saddr->sa_family == AF_INET6) {
		struct sockaddr_in6 *sa_in = (struct sockaddr_in6 *)saddr;
		int i;
		int buffer_size = 28;

		guint16 port = ntohs (sa_in->sin6_port);

		if (sa_size < buffer_size) {
			mono_error_set_generic_error (error, "System", "SystemException", "");
			return MONO_HANDLE_NEW (MonoObject, NULL);
		}

		MONO_HANDLE_ARRAY_SETVAL (data, guint8, 2, (port>>8) & 0xff);
		MONO_HANDLE_ARRAY_SETVAL (data, guint8, 3, (port) & 0xff);
		
		if (is_ipv4_mapped_any (&sa_in->sin6_addr)) {
			// Map ::ffff:0:0 to :: (bug #5502)
			for (i = 0; i < 16; i++)
				MONO_HANDLE_ARRAY_SETVAL (data, guint8, 8 + i, 0);
		} else {
			for (i = 0; i < 16; i++) {
				MONO_HANDLE_ARRAY_SETVAL (data, guint8, 8 + i,
							  sa_in->sin6_addr.s6_addr [i]);
			}
		}

		MONO_HANDLE_ARRAY_SETVAL (data, guint8, 24, sa_in->sin6_scope_id & 0xff);
		MONO_HANDLE_ARRAY_SETVAL (data, guint8, 25,
					  (sa_in->sin6_scope_id >> 8) & 0xff);
		MONO_HANDLE_ARRAY_SETVAL (data, guint8, 26,
					  (sa_in->sin6_scope_id >> 16) & 0xff);
		MONO_HANDLE_ARRAY_SETVAL (data, guint8, 27,
					  (sa_in->sin6_scope_id >> 24) & 0xff);

		mono_field_set_value_internal (MONO_HANDLE_RAW (sockaddr_obj), domain->sockaddr_data_field, MONO_HANDLE_RAW (data)); /* FIXME: use handles for mono_field_set_value */
		mono_field_set_value_internal (MONO_HANDLE_RAW (sockaddr_obj), domain->sockaddr_data_length_field, &buffer_size); /* FIXME: use handles for mono_field_set_value */

		return sockaddr_obj;
	}
#endif
#ifdef HAVE_SYS_UN_H
	else if (saddr->sa_family == AF_UNIX) {
		int i;
		int buffer_size = sa_size + 2;

		for (i = 0; i < sa_size; i++)
			MONO_HANDLE_ARRAY_SETVAL (data, guint8, i + 2, saddr->sa_data [i]);
		
		mono_field_set_value_internal (MONO_HANDLE_RAW (sockaddr_obj), domain->sockaddr_data_field, MONO_HANDLE_RAW (data)); /* FIXME: use handles for mono_field_set_value */
		mono_field_set_value_internal (MONO_HANDLE_RAW (sockaddr_obj), domain->sockaddr_data_length_field, &buffer_size); /* FIXME: use handles for mono_field_set_value */

		return sockaddr_obj;
	}
#endif
	else {
		*werror = WSAEAFNOSUPPORT;
		return MONO_HANDLE_NEW (MonoObject, NULL);
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

static MonoObjectHandle
mono_w32socket_getname (gsize sock, gint32 af, gboolean local, gint32 *werror, MonoError *error)
{
	gpointer sa = NULL;
	socklen_t salen = 0;
	int ret;
	MonoObjectHandle result = NULL_HANDLE;

	*werror = 0;
	
	salen = get_sockaddr_size (convert_family ((MonoAddressFamily)af));
	if (salen == 0) {
		*werror = WSAEAFNOSUPPORT;
		goto exit;
	}
	if (salen <= 128) {
		sa = g_alloca (salen);
		memset (sa, 0, salen);
	} else {
		sa = g_malloc0 (salen);
	}

	/* Note: linux returns just 2 for AF_UNIX. Always. */
	ret = (local ? mono_w32socket_getsockname : mono_w32socket_getpeername) (sock, (struct sockaddr *)sa, &salen);
	if (ret == SOCKET_ERROR) {
		*werror = mono_w32socket_get_last_error ();
		goto exit;
	}
	
	LOGDEBUG (g_message("%s: %s to %s port %d", __func__, local ? "bound" : "connected", inet_ntoa (((struct sockaddr_in *)&sa)->sin_addr), ntohs (((struct sockaddr_in *)&sa)->sin_port)));

	result = create_object_handle_from_sockaddr ((struct sockaddr *)sa, salen, werror, error);
exit:
	if (salen > 128)
		g_free (sa);
	return result;
}

MonoObjectHandle
ves_icall_System_Net_Sockets_Socket_LocalEndPoint_icall (gsize sock, gint32 af, gint32 *werror, MonoError *error)
{
	return mono_w32socket_getname (sock, af, TRUE, werror, error);
}

MonoObjectHandle
ves_icall_System_Net_Sockets_Socket_RemoteEndPoint_icall (gsize sock, gint32 af, gint32 *werror, MonoError *error)
{
	return mono_w32socket_getname (sock, af, FALSE, werror, error);
}

static struct sockaddr*
create_sockaddr_from_handle (MonoObjectHandle saddr_obj, socklen_t *sa_size, gint32 *werror, MonoError *error)
{
	MonoDomain *domain = mono_domain_get ();
	gint32 family;
	int len;

	error_init (error);

	if (!domain->sockaddr_class)
		domain->sockaddr_class = mono_class_load_from_name (get_socket_assembly (), "System.Net", "SocketAddress");

	/* Locate the SocketAddress data buffer in the object */
	if (!domain->sockaddr_data_field) {
		domain->sockaddr_data_field = mono_class_get_field_from_name_full (domain->sockaddr_class, "m_Buffer", NULL);
		g_assert (domain->sockaddr_data_field);
	}

	/* Locate the SocketAddress data buffer length in the object */
	if (!domain->sockaddr_data_length_field) {
		domain->sockaddr_data_length_field = mono_class_get_field_from_name_full (domain->sockaddr_class, "m_Size", NULL);
		g_assert (domain->sockaddr_data_length_field);
	}

	MonoArrayHandle data = MONO_HANDLE_NEW_GET_FIELD (saddr_obj, MonoArray, domain->sockaddr_data_field);

	/* The data buffer is laid out as follows:
	 * byte 0 is the address family low byte
	 * byte 1 is the address family high byte
	 * INET:
	 * 	bytes 2 and 3 are the port info
	 * 	the rest is the address info
	 * UNIX:
	 * 	the rest is the file name
	 */
	len = MONO_HANDLE_GET_FIELD_VAL (saddr_obj, int, domain->sockaddr_data_length_field);
	g_assert (len >= 2);

	uint32_t gchandle;
	guint8 *buf = MONO_ARRAY_HANDLE_PIN (data, guint8, 0, &gchandle);
	family = convert_family ((MonoAddressFamily)(buf[0] + (buf[1] << 8)));
	if (family == AF_INET) {
		struct sockaddr_in *sa;
		guint16 port;
		guint32 address;
		
		if (len < 8) {
			mono_error_set_generic_error (error, "System", "SystemException", "");
			mono_gchandle_free_internal (gchandle);
			return NULL;
		}

		sa = g_new0 (struct sockaddr_in, 1);
		port = (buf[2] << 8) + buf[3];
		address = (buf[4] << 24) + (buf[5] << 16) + (buf[6] << 8) + buf[7];

		sa->sin_family = family;
		sa->sin_addr.s_addr = htonl (address);
		sa->sin_port = htons (port);

		*sa_size = sizeof (struct sockaddr_in);
		mono_gchandle_free_internal (gchandle);
		return (struct sockaddr *)sa;
	}
#ifdef HAVE_STRUCT_SOCKADDR_IN6
	else if (family == AF_INET6) {
		struct sockaddr_in6 *sa;
		int i;
		guint16 port;
		guint32 scopeid;
		
		if (len < 28) {
			mono_error_set_generic_error (error, "System", "SystemException", "");
			mono_gchandle_free_internal (gchandle);
			return NULL;
		}

		sa = g_new0 (struct sockaddr_in6, 1);
		port = buf[3] + (buf[2] << 8);
		scopeid = buf[24] + (buf[25] << 8) + (buf[26] << 16) + (buf[27] << 24);

		sa->sin6_family = family;
		sa->sin6_port = htons (port);
		sa->sin6_scope_id = scopeid;

		for (i = 0; i < 16; i++)
			sa->sin6_addr.s6_addr [i] = buf[8 + i];

		*sa_size = sizeof (struct sockaddr_in6);
		mono_gchandle_free_internal (gchandle);
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
			mono_error_set_argument_out_of_range (error, "SocketAddress.Size", "MonoArgumentException:SocketAddress.Size");
			mono_gchandle_free_internal (gchandle);
			return NULL;
		}
		
		sock_un = g_new0 (struct sockaddr_un, 1);

		sock_un->sun_family = family;
		for (i = 0; i < len - 2; i++)
			sock_un->sun_path [i] = buf[i + 2];
		
		*sa_size = len;
		mono_gchandle_free_internal (gchandle);
		return (struct sockaddr *)sock_un;
	}
#endif
	else {
		*werror = WSAEAFNOSUPPORT;
		mono_gchandle_free_internal (gchandle);
		return 0;
	}
}

void
ves_icall_System_Net_Sockets_Socket_Bind_icall (gsize sock, MonoObjectHandle sockaddr, gint32 *werror, MonoError *error)
{
	struct sockaddr *sa;
	socklen_t sa_size;
	int ret;
	
	error_init (error);
	*werror = 0;
	
	sa = create_sockaddr_from_handle (sockaddr, &sa_size, werror, error);
	if (*werror != 0)
		return;
	return_if_nok (error);

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
ves_icall_System_Net_Sockets_Socket_Poll_icall (gsize sock, gint mode, gint timeout, gint32 *werror)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	mono_pollfd *pfds;
	int ret;
	time_t start;
	gint rtimeout;

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
	rtimeout = timeout;
	start = time (NULL);

	do {
		MONO_ENTER_GC_SAFE;

		ret = mono_poll (pfds, 1, timeout);

		MONO_EXIT_GC_SAFE;

		if (timeout > 0 && ret < 0) {
			int err = errno;
			int sec = time (NULL) - start;
			
			timeout = rtimeout - sec * 1000;
			if (timeout < 0) {
				timeout = 0;
			}
			
			mono_set_errno (err);
		}

		if (ret == -1 && errno == EINTR) {
			if (mono_thread_test_state (thread, ThreadState_AbortRequested)) {
				g_free (pfds);
				return FALSE;
			}

			/* Suspend requested? */
			mono_thread_interruption_checkpoint_void ();

			mono_set_errno (EINTR);
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
ves_icall_System_Net_Sockets_Socket_Connect_icall (gsize sock, MonoObjectHandle sockaddr, gint32 *werror, MonoBoolean blocking, MonoError *error)
{
	struct sockaddr *sa;
	socklen_t sa_size;
	int ret;

	error_init  (error);
	*werror = 0;

	sa = create_sockaddr_from_handle (sockaddr, &sa_size, werror, error);
	if (*werror != 0)
		return;
	return_if_nok (error);

	LOGDEBUG (g_message("%s: connecting to %s port %d", __func__, inet_ntoa (((struct sockaddr_in *)sa)->sin_addr), ntohs (((struct sockaddr_in *)sa)->sin_port)));

	ret = mono_w32socket_connect (sock, sa, sa_size, blocking);
	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();

	g_free (sa);
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT)

void
ves_icall_System_Net_Sockets_Socket_Disconnect_icall (gsize sock, MonoBoolean reuse, gint32 *werror)
{
	LOGDEBUG (g_message("%s: disconnecting from socket %p (reuse %d)", __func__, sock, reuse));

	*werror = mono_w32socket_disconnect (sock, reuse);
}

#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT) */

MonoBoolean
ves_icall_System_Net_Sockets_Socket_Duplicate_icall (gpointer handle, gint32 targetProcessId, gpointer *duplicate_handle, gint32 *werror)
{
	*werror = 0;
	if (!mono_w32socket_duplicate (handle, targetProcessId, duplicate_handle)) {
		*werror = mono_w32error_get_last ();
		return FALSE;
	}

	return TRUE;
}

gint32
ves_icall_System_Net_Sockets_Socket_Receive_icall (gsize sock, gchar *buffer, gint32 count, gint32 flags, gint32 *werror, MonoBoolean blocking)
{
	int ret;
	int recvflags = 0;
	
	*werror = 0;
	
	recvflags = convert_socketflags (flags);
	if (recvflags == -1) {
		*werror = WSAEOPNOTSUPP;
		return 0;
	}

	ret = mono_w32socket_recv (sock, buffer, count, recvflags, blocking);
	if (ret == SOCKET_ERROR) {
		*werror = mono_w32socket_get_last_error ();
		return 0;
	}

	return ret;
}

gint32
ves_icall_System_Net_Sockets_Socket_Receive_array_icall (gsize sock, WSABUF *buffers, gint32 count, gint32 flags, gint32 *werror, MonoBoolean blocking)
{
	int ret;
	guint32 recv;
	guint32 recvflags = 0;
	
	*werror = 0;
	
	recvflags = convert_socketflags (flags);
	if (recvflags == -1) {
		*werror = WSAEOPNOTSUPP;
		return 0;
	}

	ret = mono_w32socket_recvbuffers (sock, buffers, count, &recv, &recvflags, NULL, NULL, blocking);
	if (ret == SOCKET_ERROR) {
		*werror = mono_w32socket_get_last_error ();
		return 0;
	}

	return recv;
}

gint32
ves_icall_System_Net_Sockets_Socket_ReceiveFrom_icall (gsize sock, gchar *buffer, gint32 count, gint32 flags, MonoObjectHandleInOut sockaddr, gint32 *werror, MonoBoolean blocking, MonoError *error)
{
	int ret;
	int recvflags = 0;
	struct sockaddr *sa;
	socklen_t sa_size;
	
	error_init (error);
	*werror = 0;

	sa = create_sockaddr_from_handle (sockaddr, &sa_size, werror, error);
	if (*werror != 0)
		return 0;
	if (!is_ok (error))
		return 0;
	
	recvflags = convert_socketflags (flags);
	if (recvflags == -1) {
		*werror = WSAEOPNOTSUPP;
		return 0;
	}

	ret = mono_w32socket_recvfrom (sock, buffer, count, recvflags, sa, &sa_size, blocking);
	if (ret == SOCKET_ERROR) {
		*werror = mono_w32socket_get_last_error ();
		g_free(sa);
		return 0;
	}

	/* If we didn't get a socket size, then we're probably a
	 * connected connection-oriented socket and the stack hasn't
	 * returned the remote address. All we can do is return null.
	 */
	if (sa_size) {
		MONO_HANDLE_ASSIGN (sockaddr, create_object_handle_from_sockaddr (sa, sa_size, werror, error));
		if (!is_ok (error)) {
			g_free (sa);
			return 0;
		}
	} else {
		MONO_HANDLE_ASSIGN (sockaddr, MONO_HANDLE_NEW (MonoObject, NULL));
	}

	g_free (sa);
	
	return ret;
}

gint32
ves_icall_System_Net_Sockets_Socket_Send_icall (gsize sock, gchar *buffer, gint32 count, gint32 flags, gint32 *werror, MonoBoolean blocking)
{
	int ret;
	int sendflags = 0;
	
	*werror = 0;
	
	LOGDEBUG (g_message("%s: Sending %d bytes", __func__, count));

	sendflags = convert_socketflags (flags);
	if (sendflags == -1) {
		*werror = WSAEOPNOTSUPP;
		return 0;
	}

	ret = mono_w32socket_send (sock, buffer, count, sendflags, blocking);
	if (ret == SOCKET_ERROR) {
		*werror = mono_w32socket_get_last_error ();
		return 0;
	}

	return ret;
}

gint32
ves_icall_System_Net_Sockets_Socket_Send_array_icall (gsize sock, WSABUF *buffers, gint32 count, gint32 flags, gint32 *werror, MonoBoolean blocking)
{
	int ret;
	guint32 sent;
	guint32 sendflags = 0;
	
	*werror = 0;
	
	sendflags = convert_socketflags (flags);
	if (sendflags == -1) {
		*werror = WSAEOPNOTSUPP;
		return 0;
	}

	ret = mono_w32socket_sendbuffers (sock, buffers, count, &sent, sendflags, NULL, NULL, blocking);
	if (ret == SOCKET_ERROR) {
		*werror = mono_w32socket_get_last_error ();
		return 0;
	}

	return sent;
}

gint32
ves_icall_System_Net_Sockets_Socket_SendTo_icall (gsize sock, gchar *buffer, gint32 count, gint32 flags, MonoObjectHandle sockaddr, gint32 *werror, MonoBoolean blocking, MonoError *error)
{
	int ret;
	int sendflags = 0;
	struct sockaddr *sa;
	socklen_t sa_size;
	
	*werror = 0;

	sa = create_sockaddr_from_handle (sockaddr, &sa_size, werror, error);
	if (*werror != 0 || !is_ok (error))
		return 0;
	
	LOGDEBUG (g_message("%s: Sending %d bytes", __func__, count));

	sendflags = convert_socketflags (flags);
	if (sendflags == -1) {
		*werror = WSAEOPNOTSUPP;
		g_free (sa);
		return 0;
	}

	ret = mono_w32socket_sendto (sock, buffer, count, sendflags, sa, sa_size, blocking);
	if (ret == SOCKET_ERROR) {
		*werror = mono_w32socket_get_last_error ();
		g_free(sa);
		return 0;
	}

	g_free(sa);
	return ret;
}

static SOCKET
Socket_to_SOCKET (MonoObjectHandle sockobj)
{
	MonoClassField *field;
	
	field = mono_class_get_field_from_name_full (mono_handle_class (sockobj), "m_Handle", NULL);
	MonoSafeHandleHandle safe_handle = MONO_HANDLE_NEW_GET_FIELD(sockobj, MonoSafeHandle, field);

	if (MONO_HANDLE_IS_NULL (safe_handle))
		return -1;

	return (SOCKET)(gsize)MONO_HANDLE_GETVAL (safe_handle, handle);
}

#define POLL_ERRORS (MONO_POLLERR | MONO_POLLHUP | MONO_POLLNVAL)

static gboolean
collect_pollfds_from_array (MonoArrayHandle sockets, int i, int nfds, mono_pollfd *pfds, int *idx, int *mode)
{
	HANDLE_FUNCTION_ENTER ();
	gboolean result = TRUE;
	MonoObjectHandle obj = MONO_HANDLE_NEW (MonoObject, NULL);
	MONO_HANDLE_ARRAY_GETREF (obj, sockets, i);
	if (MONO_HANDLE_IS_NULL (obj)) {
		(*mode)++;
		goto leave;
	}

	if (*idx >= nfds) {
		result = FALSE;
		goto leave;
	}

	pfds [*idx].fd = Socket_to_SOCKET (obj);
	pfds [*idx].events = (*mode == 0) ? MONO_POLLIN : (*mode == 1) ? MONO_POLLOUT : POLL_ERRORS;
	(*idx)++;
leave:
	HANDLE_FUNCTION_RETURN_VAL (result);
}

static void
set_socks_array_from_pollfds (MonoArrayHandle sockets, int i, mono_pollfd *pfds, int *ret, int *mode, MonoArrayHandle socks, int *idx)
{
	HANDLE_FUNCTION_ENTER ();
	mono_pollfd *pfd;

	MonoObjectHandle obj = MONO_HANDLE_NEW (MonoObject, NULL);
	MONO_HANDLE_ARRAY_GETREF (obj, sockets, i);
	if (MONO_HANDLE_IS_NULL (obj)) {
		(*mode)++;
		(*idx)++;
		goto leave;
	}

	pfd = &pfds [i - *mode];
	if (pfd->revents == 0)
		goto leave;

	(*ret)--;
	if (((*mode == 0 && (pfd->revents & (MONO_POLLIN | POLL_ERRORS)) != 0)) ||
	    ((*mode == 1 && (pfd->revents & (MONO_POLLOUT | POLL_ERRORS)) != 0)) ||
	    ((pfd->revents & POLL_ERRORS) != 0)) {
		MONO_HANDLE_ARRAY_SETREF (socks, *idx, obj);
		(*idx)++;
	}
leave:
	HANDLE_FUNCTION_RETURN ();
}

void
ves_icall_System_Net_Sockets_Socket_Select_icall (MonoArrayHandleOut sockets, gint32 timeout, gint32 *werror, MonoError *error)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	mono_pollfd *pfds;
	int nfds, idx;
	int ret;
	int i, count;
	int mode;
	MonoClass *sock_arr_class;
	time_t start;
	uintptr_t socks_size;
	gint32 rtimeout;

	error_init (error);
	*werror = 0;

	/* *sockets -> READ, null, WRITE, null, ERROR, null */
	count = mono_array_handle_length (sockets);
	nfds = count - 3; /* NULL separators */
	pfds = g_new0 (mono_pollfd, nfds);
	mode = idx = 0;
	for (i = 0; i < count; i++) {
		if (!collect_pollfds_from_array (sockets, i, nfds, pfds, &idx, &mode)) {
			/* The socket array was bogus */
			g_free (pfds);
			*werror = WSAEFAULT;
			return;
		}
	}

	timeout = (timeout >= 0) ? (timeout / 1000) : -1;
	rtimeout = timeout;
	start = time (NULL);
	do {
		MONO_ENTER_GC_SAFE;

		ret = mono_poll (pfds, nfds, timeout);

		MONO_EXIT_GC_SAFE;

		if (timeout > 0 && ret < 0) {
			int err = errno;
			int sec = time (NULL) - start;

			timeout = rtimeout - sec * 1000;
			if (timeout < 0)
				timeout = 0;
			mono_set_errno (err);
		}

		if (ret == -1 && errno == EINTR) {
			if (mono_thread_test_state (thread, ThreadState_AbortRequested)) {
				g_free (pfds);
				MONO_HANDLE_ASSIGN (sockets, MONO_HANDLE_NEW (MonoObject, NULL));
				return;
			}

			/* Suspend requested? */
			mono_thread_interruption_checkpoint_void ();

			mono_set_errno (EINTR);
		}
	} while (ret == -1 && errno == EINTR);
	
	if (ret == -1) {
		*werror = mono_w32socket_convert_error (errno);
		g_free (pfds);
		return;
	}

	if (ret == 0) {
		g_free (pfds);
		MONO_HANDLE_ASSIGN (sockets, MONO_HANDLE_NEW (MonoObject, NULL));
		return;
	}

	sock_arr_class = mono_handle_class (sockets);
	socks_size = ((uintptr_t)ret) + 3; /* space for the NULL delimiters */
	MonoArrayHandle socks = MONO_HANDLE_NEW (MonoArray, mono_array_new_full_checked (mono_domain_get (), sock_arr_class, &socks_size, NULL, error));
	if (!is_ok (error)) {
		g_free (pfds);
		return;
	}

	mode = idx = 0;
	for (i = 0; i < count && ret > 0; i++) {
		set_socks_array_from_pollfds (sockets, i, pfds, &ret, &mode, socks, &idx);
	}

	MONO_HANDLE_ASSIGN (sockets, socks);
	g_free (pfds);
}

static MonoObjectHandle
int_to_object_handle (MonoDomain *domain, int val, MonoError *error)
{
	return MONO_HANDLE_NEW (MonoObject, mono_value_box_checked (domain, mono_get_int32_class (), &val, error));
}

void
ves_icall_System_Net_Sockets_Socket_GetSocketOption_obj_icall (gsize sock, gint32 level, gint32 name, MonoObjectHandleOut obj_val, gint32 *werror, MonoError *error)
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
	MonoDomain *domain = mono_domain_get ();
	MonoClass *obj_class;
	MonoClassField *field;
	
	error_init (error);
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
		MONO_HANDLE_ASSIGN (obj_val, int_to_object_handle (domain, 0, error));
		return;
	}

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
		ret = mono_w32socket_getsockopt (sock, system_level, system_name, &time_ms, &time_ms_size);
		break;

#ifdef SO_PEERCRED
	case SocketOptionName_PeerCred: 
		ret = mono_w32socket_getsockopt (sock, system_level, system_name, &cred, &credsize);
		break;
#endif

	default:
		ret = mono_w32socket_getsockopt (sock, system_level, system_name, &val, &valsize);
	}

	if (ret == SOCKET_ERROR) {
		*werror = mono_w32socket_get_last_error ();
		return;
	}
	
	switch (name) {
	case SocketOptionName_Linger: {
		/* build a System.Net.Sockets.LingerOption */
		obj_class = mono_class_load_from_name (get_socket_assembly (),
											   "System.Net.Sockets",
											   "LingerOption");
		MonoObjectHandle obj = mono_object_new_handle (domain, obj_class, error);
		return_if_nok (error);

		/* Locate and set the fields "bool enabled" and "int
		 * lingerTime"
		 */
		field = mono_class_get_field_from_name_full (obj_class, "enabled", NULL);
		MONO_HANDLE_SET_FIELD_VAL (obj, guint8, field, linger.l_onoff);

		field = mono_class_get_field_from_name_full (obj_class, "lingerTime", NULL);
		MONO_HANDLE_SET_FIELD_VAL (obj, guint32, field, linger.l_linger);

		MONO_HANDLE_ASSIGN (obj_val, obj);
		break;
	}
	case SocketOptionName_DontLinger: {
		/* construct a bool int in val - true if linger is off */
		MonoObjectHandle obj = int_to_object_handle (domain, !linger.l_onoff, error);
		return_if_nok (error);

		MONO_HANDLE_ASSIGN (obj_val, obj);
		break;
	}
	case SocketOptionName_SendTimeout:
	case SocketOptionName_ReceiveTimeout: {
		MonoObjectHandle obj = int_to_object_handle (domain, time_ms, error);
		return_if_nok (error);

		MONO_HANDLE_ASSIGN (obj_val, obj);
		break;
	}

#ifdef SO_PEERCRED
	case SocketOptionName_PeerCred:  {
		/* 
		 * build a Mono.Posix.PeerCred+PeerCredData if
		 * possible
		 */
		static MonoImage *mono_posix_image = NULL;
		
		if (mono_posix_image == NULL) {
			MonoAssemblyLoadContext *alc = mono_domain_default_alc (domain);
			mono_posix_image = mono_image_loaded_internal (alc, "Mono.Posix", FALSE);
			if (!mono_posix_image) {
				MonoAssemblyOpenRequest req;
				mono_assembly_request_prepare_open (&req, MONO_ASMCTX_DEFAULT, alc);
				MonoAssembly *sa = mono_assembly_request_open ("Mono.Posix.dll", &req, NULL);
				if (!sa) {
					*werror = WSAENOPROTOOPT;
					return;
				} else {
					mono_posix_image = mono_assembly_get_image_internal (sa);
				}
			}
		}
		
		obj_class = mono_class_load_from_name (mono_posix_image,
						 "Mono.Posix",
						 "PeerCredData");
		MonoPeerCredDataHandle cred_data = MONO_HANDLE_CAST (MonoPeerCredData, mono_object_new_handle (domain, obj_class, error));
		return_if_nok (error);

		MONO_HANDLE_SETVAL (cred_data, pid, gint, cred.pid);
		MONO_HANDLE_SETVAL (cred_data, uid, gint, cred.uid);
		MONO_HANDLE_SETVAL (cred_data, gid, gint, cred.gid);

		MONO_HANDLE_ASSIGN (obj_val, cred_data);
		break;
	}
#endif

	default: {
#if !defined(SO_EXCLUSIVEADDRUSE) && defined(SO_REUSEADDR)
		if (level == SocketOptionLevel_Socket && name == SocketOptionName_ExclusiveAddressUse)
			val = val ? 0 : 1;
#endif
		MonoObjectHandle obj = int_to_object_handle (domain, val, error);
		return_if_nok (error);

		MONO_HANDLE_ASSIGN (obj_val, obj);
	}
	}
}
void
ves_icall_System_Net_Sockets_Socket_GetSocketOption_arr_icall (gsize sock, gint32 level, gint32 name, MonoArrayHandle byte_val, gint32 *werror, MonoError *error)
{
	int system_level = 0;
	int system_name = 0;
	int ret;
	socklen_t valsize;
	
	error_init (error);
	*werror = 0;
	
	ret = convert_sockopt_level_and_name((MonoSocketOptionLevel)level, (MonoSocketOptionName)name, &system_level,
										 &system_name);
	if (ret == -1) {
		*werror = WSAENOPROTOOPT;
		return;
	}
	if (ret == -2)
		return;

	valsize = mono_array_handle_length (byte_val);

	uint32_t gchandle;
	guchar *buf = MONO_ARRAY_HANDLE_PIN (byte_val, guchar, 0, &gchandle);

	ret = mono_w32socket_getsockopt (sock, system_level, system_name, buf, &valsize);

	mono_gchandle_free_internal (gchandle);

	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();
}

#if defined(HAVE_STRUCT_IP_MREQN) || defined(HAVE_STRUCT_IP_MREQ)
static struct in_addr
ipaddress_handle_to_struct_in_addr (MonoObjectHandle ipaddr)
{
	struct in_addr inaddr;
	MonoClassField *field;
	
	field = mono_class_get_field_from_name_full (mono_handle_class (ipaddr), "_addressOrScopeId", NULL);
	g_assert (field);

	/* No idea why .net uses a 64bit type to hold a 32bit value...
	 *
	 * Internal value of IPAddess is in little-endian order
	 */
	inaddr.s_addr = GUINT_FROM_LE ((guint32)MONO_HANDLE_GET_FIELD_VAL (ipaddr, guint32, field));
	
	return inaddr;
}

#ifdef HAVE_STRUCT_SOCKADDR_IN6
static struct in6_addr
ipaddress_handle_to_struct_in6_addr (MonoObjectHandle ipaddr)
{
	struct in6_addr in6addr;
	MonoClassField *field;
	int i;

	field = mono_class_get_field_from_name_full (mono_handle_class (ipaddr), "_numbers", NULL);
	g_assert (field);
	MonoArrayHandle data = MONO_HANDLE_NEW_GET_FIELD (ipaddr, MonoArray, field);

	for (i = 0; i < 8; i++) {
		guint16 v;
		MONO_HANDLE_ARRAY_GETVAL (v, data, guint16, i);
		const guint16 s = GUINT16_TO_BE (v);

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

#ifdef HAVE_STRUCT_SOCKADDR_IN6
#if defined(__APPLE__) || defined(__FreeBSD__)
static int
get_local_interface_id (int family)
{
#if !(defined(HAVE_GETIFADDRS) || defined(HAVE_QP2GETIFADDRS)) || !defined(HAVE_IF_NAMETOINDEX)
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
#endif /* HAVE_STRUCT_SOCKADDR_IN6 */

void
ves_icall_System_Net_Sockets_Socket_SetSocketOption_icall (gsize sock, gint32 level, gint32 name, MonoObjectHandle obj_val, MonoArrayHandle byte_val, gint32 int_val, gint32 *werror, MonoError *error)
{
	struct linger linger;
	int system_level = 0;
	int system_name = 0;
	int ret;
	int sol_ip;
	int sol_ipv6;

	error_init (error);
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
	if (!MONO_HANDLE_IS_NULL (obj_val)) {
		MonoClass *obj_class = mono_handle_class (obj_val);
		MonoClassField *field;
		int valsize;
		
		switch (name) {
		case SocketOptionName_Linger:
			/* Dig out "bool enabled" and "int lingerTime"
			 * fields
			 */
			field = mono_class_get_field_from_name_full (obj_class, "enabled", NULL);
			linger.l_onoff = MONO_HANDLE_GET_FIELD_VAL (obj_val, guint8, field);
			field = mono_class_get_field_from_name_full (obj_class, "lingerTime", NULL);
			linger.l_linger = MONO_HANDLE_GET_FIELD_VAL (obj_val, guint32, field);
			
			valsize = sizeof (linger);
			ret = mono_w32socket_setsockopt (sock, system_level, system_name, &linger, valsize);
			break;
		case SocketOptionName_AddMembership:
		case SocketOptionName_DropMembership:
#if defined(HAVE_STRUCT_IP_MREQN) || defined(HAVE_STRUCT_IP_MREQ)
		{
			MonoObjectHandle address = MONO_HANDLE_NEW (MonoObject, NULL);
#ifdef HAVE_STRUCT_SOCKADDR_IN6
			if (system_level == sol_ipv6) {
				struct ipv6_mreq mreq6;

				/*
				 *	Get group address
				 */
				field = mono_class_get_field_from_name_full (obj_class, "m_Group", NULL);
				g_assert (field);
				MONO_HANDLE_ASSIGN (address, MONO_HANDLE_NEW_GET_FIELD (obj_val, MonoObject, field));
				
				if (!MONO_HANDLE_IS_NULL (address))
					mreq6.ipv6mr_multiaddr = ipaddress_handle_to_struct_in6_addr (address);

				field = mono_class_get_field_from_name_full (obj_class, "m_Interface", NULL);
				mreq6.ipv6mr_interface = MONO_HANDLE_GET_FIELD_VAL (obj_val, guint64, field);
				
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
				field = mono_class_get_field_from_name_full (obj_class, "group", NULL);
				MONO_HANDLE_ASSIGN (address, MONO_HANDLE_NEW_GET_FIELD (obj_val, MonoObject, field));

				/* address might not be defined and if so, set the address to ADDR_ANY.
				 */
				if (!MONO_HANDLE_IS_NULL (address))
					mreq.imr_multiaddr = ipaddress_handle_to_struct_in_addr (address);

				field = mono_class_get_field_from_name_full (obj_class, "localAddress", NULL);
				MONO_HANDLE_ASSIGN (address, MONO_HANDLE_NEW_GET_FIELD (obj_val, MonoObject, field));

#ifdef HAVE_STRUCT_IP_MREQN
				if (!MONO_HANDLE_IS_NULL (address))
					mreq.imr_address = ipaddress_handle_to_struct_in_addr (address);

				field = mono_class_get_field_from_name_full (obj_class, "ifIndex", NULL);
				mreq.imr_ifindex = MONO_HANDLE_GET_FIELD_VAL (obj_val, gint32, field);
#else
				if (!MONO_HANDLE_IS_NULL (address))
					mreq.imr_interface = ipaddress_handle_to_struct_in_addr (address);
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
	} else if (!MONO_HANDLE_IS_NULL (byte_val)) {
		int valsize = mono_array_handle_length (byte_val);
		uint32_t gchandle;
		guchar *buf = MONO_ARRAY_HANDLE_PIN (byte_val, guchar, 0, &gchandle);
		
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
		mono_gchandle_free_internal (gchandle);
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
				ret = mono_w32socket_setsockopt (sock, system_level, system_name, &mreq, sizeof (mreq));
				break;
			}
			int_val = GUINT32_TO_BE (int_val);
#endif /* HAVE_STRUCT_IP_MREQN */
#endif /* HOST_WIN32 */
			/* int_val is in_addr */
			ret = mono_w32socket_setsockopt (sock, system_level, system_name, &int_val, sizeof (int_val));
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
			ret = mono_w32socket_setsockopt (sock, system_level, system_name, &int_val, sizeof (int_val));
		}
	}

	if (ret == SOCKET_ERROR) {
		*werror = mono_w32socket_get_last_error ();

#ifdef HAVE_IP_MTU_DISCOVER
		if (system_name == IP_MTU_DISCOVER) {
			switch (system_level) {
			case IP_PMTUDISC_DONT:
			case IP_PMTUDISC_WANT:
			case IP_PMTUDISC_DO:
#ifdef IP_PMTUDISC_PROBE
			case IP_PMTUDISC_PROBE:
#endif
#ifdef IP_PMTUDISC_INTERFACE
			case IP_PMTUDISC_INTERFACE:
#endif
#ifdef IP_PMTUDISC_OMIT
			case IP_PMTUDISC_OMIT:
#endif
				/*
				 * This happens if HAVE_IP_MTU_DISCOVER is set but the OS
				 * doesn't actually understand it. The only OS that this is
				 * known to happen on currently is Windows Subsystem for Linux
				 * (newer versions have been fixed to recognize it). Just
				 * pretend everything is fine.
				 */
				ret = 0;
				*werror = 0;
				break;
			default:
				break;
			}
		}
#endif
	}
}

void
ves_icall_System_Net_Sockets_Socket_Shutdown_icall (gsize sock, gint32 how, gint32 *werror)
{
	int ret;

	*werror = 0;

	/* Currently, the values for how (recv=0, send=1, both=2) match the BSD API */
	ret = mono_w32socket_shutdown (sock, how);
	if (ret == SOCKET_ERROR)
		*werror = mono_w32socket_get_last_error ();
}

gint
ves_icall_System_Net_Sockets_Socket_IOControl_icall (gsize sock, gint32 code, MonoArrayHandle input, MonoArrayHandle output, gint32 *werror, MonoError *error)
{
#ifdef HOST_WIN32
	DWORD output_bytes = 0;
#else
	glong output_bytes = 0;
#endif
	gchar *i_buffer, *o_buffer;
	gint i_len, o_len;
	uint32_t i_gchandle = 0;
	uint32_t o_gchandle = 0;
	gint ret;

	error_init (error);
	*werror = 0;
	
	if ((guint32)code == FIONBIO)
		/* Invalid command. Must use Socket.Blocking */
		return -1;

	if (MONO_HANDLE_IS_NULL (input)) {
		i_buffer = NULL;
		i_len = 0;
		i_gchandle = 0;
	} else {
		i_len = mono_array_handle_length (input);
		i_buffer = MONO_ARRAY_HANDLE_PIN (input, gchar, 0, &i_gchandle);
	}

	if (MONO_HANDLE_IS_NULL (output)) {
		o_buffer = NULL;
		o_len = 0;
		o_gchandle = 0;
	} else {
		o_len = mono_array_handle_length (output);
		o_buffer = MONO_ARRAY_HANDLE_PIN (output, gchar, 0, &o_gchandle);
	}

	ret = mono_w32socket_ioctl (sock, code, i_buffer, i_len, o_buffer, o_len, &output_bytes);

	mono_gchandle_free_internal (i_gchandle);
	mono_gchandle_free_internal (o_gchandle);

	if (ret == SOCKET_ERROR) {
		*werror = mono_w32socket_get_last_error ();
		return -1;
	}

	return (gint)output_bytes;
}

static gboolean
addrinfo_add_string (MonoDomain *domain, const char *s, MonoArrayHandle arr, int index, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoStringHandle str = mono_string_new_handle (domain, s, error);
	goto_if_nok (error, leave);
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
		goto_if_nok (error, leave);
			
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
	goto_if_nok (error, leave);
	if (add_local_ips) {
		int addr_index = addrinfo_add_local_ips (domain, h_addr_list, error);
		goto_if_nok (error, leave);
		if (addr_index > 0)
			goto leave;
	}

	gint32 count;
	count = 0;
	for (ai = info->entries; ai != NULL; ai = ai->next) {
		if (ai->family != AF_INET && ai->family != AF_INET6)
			continue;
		count++;
	}

	int addr_index;
	addr_index = 0;
	MONO_HANDLE_ASSIGN (h_addr_list, mono_array_new_handle (domain, mono_get_string_class (), count, error));
	goto_if_nok (error, leave);

	gboolean name_assigned;
	name_assigned = FALSE;
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
			goto_if_nok (error, leave);
		}

		addr_index++;
	}

leave:
	if (info)
		mono_free_address_info (info);

	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

MonoBoolean
ves_icall_System_Net_Dns_GetHostByName (MonoStringHandle host, MonoStringHandleOut h_name, MonoArrayHandleOut h_aliases, MonoArrayHandleOut h_addr_list, gint32 hint, MonoError *error)
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
ves_icall_System_Net_Dns_GetHostByAddr (MonoStringHandle addr, MonoStringHandleOut h_name, MonoArrayHandleOut h_aliases, MonoArrayHandleOut h_addr_list, gint32 hint, MonoError *error)
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

	switch (family) {
	case AF_INET: {
#if HAVE_SOCKADDR_IN_SIN_LEN
		saddr.sin_len = sizeof (saddr);
#endif
		MONO_ENTER_GC_SAFE;
		ret = getnameinfo ((struct sockaddr*)&saddr, sizeof (saddr), hostname, sizeof (hostname), NULL, 0, 0) == 0;
		MONO_EXIT_GC_SAFE;
		break;
	}
#ifdef HAVE_STRUCT_SOCKADDR_IN6
	case AF_INET6: {
#if HAVE_SOCKADDR_IN6_SIN_LEN
		saddr6.sin6_len = sizeof (saddr6);
#endif
		MONO_ENTER_GC_SAFE;
		ret = getnameinfo ((struct sockaddr*)&saddr6, sizeof (saddr6), hostname, sizeof (hostname), NULL, 0, 0) == 0;
		MONO_EXIT_GC_SAFE;
		break;
	}
#endif
	default:
		g_assert_not_reached ();
	}

	if (!ret)
		return FALSE;

	if (mono_get_address_info (hostname, 0, hint | MONO_HINT_CANONICAL_NAME | MONO_HINT_CONFIGURED_ONLY, &info) != 0)
		return FALSE;

	MonoBoolean result = addrinfo_to_IPHostEntry_handles (info, h_name, h_aliases, h_addr_list, FALSE, error);
	return result;
}

MonoBoolean
ves_icall_System_Net_Dns_GetHostName (MonoStringHandleOut h_name, MonoError *error)
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

MonoBoolean
ves_icall_System_Net_Sockets_Socket_SendFile_icall (gsize sock, MonoStringHandle filename, MonoArrayHandle pre_buffer, MonoArrayHandle post_buffer, gint flags, gint32 *werror, MonoBoolean blocking, MonoError *error)
{
	HANDLE file;
	gboolean ret;
	TRANSMIT_FILE_BUFFERS buffers;
	uint32_t pre_buffer_gchandle = 0;
	uint32_t post_buffer_gchandle = 0;

	error_init (error);
	*werror = 0;

	if (MONO_HANDLE_IS_NULL (filename))
		return FALSE;

	/* FIXME: replace file by a proper fd that we can call open and close on, as they are interruptible */

	uint32_t filename_gchandle;
	gunichar2 *filename_chars = mono_string_handle_pin_chars (filename, &filename_gchandle);
	file = mono_w32file_create (filename_chars, GENERIC_READ, FILE_SHARE_READ, OPEN_EXISTING, 0);
	mono_gchandle_free_internal (filename_gchandle);
	if (file == INVALID_HANDLE_VALUE) {
		*werror = mono_w32error_get_last ();
		return FALSE;
	}

	memset (&buffers, 0, sizeof (buffers));
	if (!MONO_HANDLE_IS_NULL (pre_buffer)) {
		buffers.Head = MONO_ARRAY_HANDLE_PIN (pre_buffer, guchar, 0, &pre_buffer_gchandle);
		buffers.HeadLength = mono_array_handle_length (pre_buffer);
	}
	if (!MONO_HANDLE_IS_NULL (post_buffer)) {
		buffers.Tail = MONO_ARRAY_HANDLE_PIN (post_buffer, guchar, 0, &post_buffer_gchandle);
		buffers.TailLength = mono_array_handle_length (post_buffer);
	}

	ret = mono_w32socket_transmit_file (sock, file, &buffers, flags, blocking);

	if (pre_buffer_gchandle)
		mono_gchandle_free_internal (pre_buffer_gchandle);
	if (post_buffer_gchandle)
		mono_gchandle_free_internal (post_buffer_gchandle);

	if (!ret)
		*werror = mono_w32socket_get_last_error ();

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
ves_icall_cancel_blocking_socket_operation (MonoThreadObjectHandle thread, MonoError *error)
{
	error_init (error);
	MonoInternalThreadHandle internal = MONO_HANDLE_NEW_GET (MonoInternalThread, thread, internal_thread);
	g_assert (!MONO_HANDLE_IS_NULL (internal));

	guint64 tid = mono_internal_thread_handle_ptr (internal)->tid;
	mono_thread_info_abort_socket_syscall_for_close (MONO_UINT_TO_NATIVE_THREAD_ID (tid));
}

#else

void
mono_network_init (void)
{
}

void
mono_network_cleanup (void)
{
}

#endif // !defined(DISABLE_SOCKETS) && !defined(ENABLE_NETCORE)
