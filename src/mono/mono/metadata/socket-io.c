/*
 * socket-io.c: Socket IO internal calls
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>

#include <glib.h>
#include <string.h>
#include <stdlib.h>
#include <unistd.h>
#include <errno.h>

#ifndef PLATFORM_WIN32
#ifdef HAVE_AIO_H
#include <aio.h>
#define USE_AIO	1
#elif defined(HAVE_SYS_AIO_H)
#include <sys/aio.h>
#define USE_AIO 1
#else
#undef USE_AIO
#endif
#endif

#include <mono/metadata/object.h>
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/socket-io.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/threads.h>
/* FIXME change this code to not mess so much with the internals */
#include <mono/metadata/class-internals.h>

#include <sys/time.h> 

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

#include "mono/io-layer/socket-wrappers.h"

#ifdef PLATFORM_WIN32
/* This is a kludge to make this file build under cygwin:
 * w32api/ws2tcpip.h has definitions for some AF_INET6 values and
 * prototypes for some but not all required functions (notably
 * inet_ntop() is missing), but the libws2_32 library is missing the
 * actual implementations of these functions.
 */
#undef AF_INET6
#endif

#undef DEBUG

static gint32 convert_family(MonoAddressFamily mono_family)
{
	gint32 family=-1;
	
	switch(mono_family) {
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
		g_warning("System.Net.Sockets.AddressFamily has unsupported value 0x%x", mono_family);
		break;
		
	case AddressFamily_Unspecified:
		family=AF_UNSPEC;
		break;
		
	case AddressFamily_Unix:
		family=AF_UNIX;
		break;
		
	case AddressFamily_InterNetwork:
		family=AF_INET;
		break;
		
	case AddressFamily_Ipx:
#ifdef AF_IPX
		family=AF_IPX;
#endif
		break;
		
	case AddressFamily_Sna:
		family=AF_SNA;
		break;
		
	case AddressFamily_DecNet:
		family=AF_DECnet;
		break;
		
	case AddressFamily_AppleTalk:
		family=AF_APPLETALK;
		break;
		
	case AddressFamily_InterNetworkV6:
#ifdef AF_INET6
		family=AF_INET6;
#endif
		break;
	case AddressFamily_Irda:
#ifdef AF_IRDA	
		family=AF_IRDA;
#endif
		break;
	default:
		g_warning("System.Net.Sockets.AddressFamily has unknown value 0x%x", mono_family);
	}

	return(family);
}

static MonoAddressFamily convert_to_mono_family(guint16 af_family)
{
	MonoAddressFamily family=AddressFamily_Unknown;
	
	switch(af_family) {
	case AF_UNSPEC:
		family=AddressFamily_Unspecified;
		break;
		
	case AF_UNIX:
		family=AddressFamily_Unix;
		break;
		
	case AF_INET:
		family=AddressFamily_InterNetwork;
		break;
		
#ifdef AF_IPX
	case AF_IPX:
		family=AddressFamily_Ipx;
		break;
#endif
		
	case AF_SNA:
		family=AddressFamily_Sna;
		break;
		
	case AF_DECnet:
		family=AddressFamily_DecNet;
		break;
		
	case AF_APPLETALK:
		family=AddressFamily_AppleTalk;
		break;
		
#ifdef AF_INET6
	case AF_INET6:
		family=AddressFamily_InterNetworkV6;
		break;
#endif
		
#ifdef AF_IRDA	
	case AF_IRDA:
		family=AddressFamily_Irda;
		break;
#endif
	default:
		g_warning("unknown address family 0x%x", af_family);
	}

	return(family);
}

static gint32 convert_type(MonoSocketType mono_type)
{
	gint32 type=-1;
	
	switch(mono_type) {
	case SocketType_Stream:
		type=SOCK_STREAM;
		break;

	case SocketType_Dgram:
		type=SOCK_DGRAM;
		break;
		
	case SocketType_Raw:
		type=SOCK_RAW;
		break;

	case SocketType_Rdm:
		type=SOCK_RDM;
		break;

	case SocketType_Seqpacket:
		type=SOCK_SEQPACKET;
		break;

	case SocketType_Unknown:
		g_warning("System.Net.Sockets.SocketType has unsupported value 0x%x", mono_type);
		break;

	default:
		g_warning("System.Net.Sockets.SocketType has unknown value 0x%x", mono_type);
	}

	return(type);
}

static gint32 convert_proto(MonoProtocolType mono_proto)
{
	gint32 proto=-1;
	
	switch(mono_proto) {
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
		proto=mono_proto;
		break;
		
	case ProtocolType_ND:
	case ProtocolType_Raw:
	case ProtocolType_Ipx:
	case ProtocolType_Spx:
	case ProtocolType_SpxII:
	case ProtocolType_Unknown:
		/* These protocols arent */
		g_warning("System.Net.Sockets.ProtocolType has unsupported value 0x%x", mono_proto);
		break;
		
	default:
		break;
	}

	return(proto);
}

static gint32 convert_sockopt_level_and_name(MonoSocketOptionLevel mono_level,
					     MonoSocketOptionName mono_name,
					     int *system_level,
					     int *system_name)
{
	switch (mono_level) {
	case SocketOptionLevel_Socket:
		*system_level = SOL_SOCKET;
		
		switch(mono_name) {
		case SocketOptionName_DontLinger:
			/* This is SO_LINGER, because the setsockopt
			 * internal call maps DontLinger to SO_LINGER
			 * with l_onoff=0
			 */
			*system_name = SO_LINGER;
			break;
		case SocketOptionName_Debug:
			*system_name = SO_DEBUG;
			break;
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
		case SocketOptionName_DontRoute:
			*system_name = SO_DONTROUTE;
			break;
		case SocketOptionName_Broadcast:
			*system_name = SO_BROADCAST;
			break;
		case SocketOptionName_Linger:
			*system_name = SO_LINGER;
			break;
		case SocketOptionName_OutOfBandInline:
			*system_name = SO_OOBINLINE;
			break;
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
		case SocketOptionName_UseLoopback:
		case SocketOptionName_MaxConnections:
			/* Can't figure out how to map these, so fall
			 * through
			 */
		default:
			g_warning("System.Net.Sockets.SocketOptionName 0x%x is not supported at Socket level", mono_name);
			return(-1);
		}
		break;
		
	case SocketOptionLevel_IP:
#ifdef HAVE_SOL_IP
		*system_level = SOL_IP;
#else
		if (1) {
			static int cached = 0;
			static int proto;
			
			if (!cached) {
				struct protoent *pent;
				
				pent = getprotobyname ("IP");
				proto = pent ? pent->p_proto : 0 /* 0 a good default value?? */;
				cached = 1;
			}
			
			*system_level = proto;
		}
#endif /* HAVE_SOL_IP */
		
		switch(mono_name) {
		case SocketOptionName_IPOptions:
			*system_name = IP_OPTIONS;
			break;
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
		case SocketOptionName_AddSourceMembership:
		case SocketOptionName_DropSourceMembership:
		case SocketOptionName_BlockSource:
		case SocketOptionName_UnblockSource:
			/* Can't figure out how to map these, so fall
			 * through
			 */
		default:
			g_warning("System.Net.Sockets.SocketOptionName 0x%x is not supported at IP level", mono_name);
			return(-1);
		}
		break;

#ifdef AF_INET6
	case SocketOptionLevel_IPv6:
#ifdef HAVE_SOL_IPV6
		*system_level = SOL_IPV6;
#else
		if (1) {
			static int cached = 0;
			static int proto;

			if (!cached) {
				struct protoent *pent;

				pent = getprotobyname ("IPV6");
				proto = pent ? pent->p_proto : 41 /* 41 a good default value?? */;
				cached = 1;
			}

			*system_level = proto;
		}
#endif /* HAVE_SOL_IPV6 */

		switch(mono_name) {
		case SocketOptionName_IpTimeToLive:
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
		case SocketOptionName_PacketInformation:
			*system_name = IPV6_PKTINFO;
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
			g_warning("System.Net.Sockets.SocketOptionName 0x%x is not supported at IPv6 level", mono_name);
			return(-1);
		}

		break;	/* SocketOptionLevel_IPv6 */
#endif
		
	case SocketOptionLevel_Tcp:
#ifdef HAVE_SOL_TCP
		*system_level = SOL_TCP;
#else
		if (1) {
			static int cached = 0;
			static int proto;
			
			if (!cached) {
				struct protoent *pent;
				
				pent = getprotobyname ("TCP");
				proto = pent ? pent->p_proto : 6 /* is 6 a good default value?? */;
				cached = 1;
			}
			
			*system_level = proto;
		}
#endif /* HAVE_SOL_TCP */
		
		switch(mono_name) {
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
			g_warning("System.Net.Sockets.SocketOptionName 0x%x is not supported at TCP level", mono_name);
			return(-1);
		}
		break;
		
	case SocketOptionLevel_Udp:
		g_warning("System.Net.Sockets.SocketOptionLevel has unsupported value 0x%x", mono_level);

		switch(mono_name) {
		case SocketOptionName_NoChecksum:
		case SocketOptionName_ChecksumCoverage:
		default:
			g_warning("System.Net.Sockets.SocketOptionName 0x%x is not supported at UDP level", mono_name);
			return(-1);
		}
		return(-1);
		break;

	default:
		g_warning("System.Net.Sockets.SocketOptionLevel has unknown value 0x%x", mono_level);
		return(-1);
	}

	return(0);
}

#define STASH_SYS_ASS(this) \
	if(system_assembly == NULL) { \
		system_assembly=mono_image_loaded ("System"); \
	}

static MonoImage *system_assembly=NULL;


#ifdef AF_INET6
static gint32 get_family_hint(void)
{
	MonoClass *socket_class;
	MonoClassField *ipv6_field, *ipv4_field;
	gint32 ipv6_enabled = -1, ipv4_enabled = -1;
	MonoVTable *vtable;

	socket_class = mono_class_from_name (system_assembly,
					     "System.Net.Sockets", "Socket");
	ipv4_field = mono_class_get_field_from_name (socket_class,
						     "ipv4Supported");
	ipv6_field = mono_class_get_field_from_name (socket_class,
						     "ipv6Supported");
	vtable = mono_class_vtable (mono_domain_get (), socket_class);

	mono_field_static_get_value(vtable, ipv4_field, &ipv4_enabled);
	mono_field_static_get_value(vtable, ipv6_field, &ipv6_enabled);

	if(ipv4_enabled == 1 && ipv6_enabled == 1) {
		return(PF_UNSPEC);
	} else if(ipv4_enabled == 1) {
		return(PF_INET);
	} else {
		return(PF_INET6);
	}
}
#endif

gpointer ves_icall_System_Net_Sockets_Socket_Socket_internal(MonoObject *this, gint32 family, gint32 type, gint32 proto, gint32 *error)
{
	SOCKET sock;
	gint32 sock_family;
	gint32 sock_proto;
	gint32 sock_type;
	
	MONO_ARCH_SAVE_REGS;

	STASH_SYS_ASS(this);
	
	*error = 0;
	
	sock_family=convert_family(family);
	if(sock_family==-1) {
		*error = WSAEAFNOSUPPORT;
		return(NULL);
	}

	sock_proto=convert_proto(proto);
	if(sock_proto==-1) {
		*error = WSAEPROTONOSUPPORT;
		return(NULL);
	}
	
	sock_type=convert_type(type);
	if(sock_type==-1) {
		*error = WSAESOCKTNOSUPPORT;
		return(NULL);
	}
	
	sock = _wapi_socket (sock_family, sock_type, sock_proto,
			     NULL, 0, WSA_FLAG_OVERLAPPED);

	if(sock==INVALID_SOCKET) {
		*error = WSAGetLastError ();
		return(NULL);
	}

	if (sock_family == AF_INET && sock_type == SOCK_DGRAM) {
		return (GUINT_TO_POINTER (sock));
	}

#ifdef AF_INET6
	if (sock_family == AF_INET6 && sock_type == SOCK_DGRAM) {
		return (GUINT_TO_POINTER (sock));
	}
#endif

#ifndef PLATFORM_WIN32
	/* .net seems to set this by default for SOCK_STREAM,
	 * not for SOCK_DGRAM (see bug #36322)
	 *
	 * It seems winsock has a rather different idea of what
	 * SO_REUSEADDR means.  If it's set, then a new socket can be
	 * bound over an existing listening socket.  There's a new
	 * windows-specific option called SO_EXCLUSIVEADDRUSE but
	 * using that means the socket MUST be closed properly, or a
	 * denial of service can occur.  Luckily for us, winsock
	 * behaves as though any other system would when SO_REUSEADDR
	 * is true, so we don't need to do anything else here.  See
	 * bug 53992.
	 */
	{
	int ret, true = 1;
	
	ret = _wapi_setsockopt (sock, SOL_SOCKET, SO_REUSEADDR, &true, sizeof (true));
	if(ret==SOCKET_ERROR) {
		*error = WSAGetLastError ();
		
		closesocket(sock);
		return(NULL);
	}
	}
#endif
	
	return(GUINT_TO_POINTER (sock));
}

/* FIXME: the SOCKET parameter (here and in other functions in this
 * file) is really an IntPtr which needs to be converted to a guint32.
 */
void ves_icall_System_Net_Sockets_Socket_Close_internal(SOCKET sock,
							gint32 *error)
{
	MONO_ARCH_SAVE_REGS;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": closing 0x%x", sock);
#endif

	*error = 0;
	
	closesocket(sock);
}

gint32 ves_icall_System_Net_Sockets_SocketException_WSAGetLastError_internal(void)
{
	MONO_ARCH_SAVE_REGS;

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": returning %d", WSAGetLastError());
#endif

	return(WSAGetLastError());
}

gint32 ves_icall_System_Net_Sockets_Socket_Available_internal(SOCKET sock,
							      gint32 *error)
{
	int ret;
	gulong amount;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	ret=ioctlsocket(sock, FIONREAD, &amount);
	if(ret==SOCKET_ERROR) {
		*error = WSAGetLastError ();
		return(0);
	}
	
	return(amount);
}

void ves_icall_System_Net_Sockets_Socket_Blocking_internal(SOCKET sock,
							   gboolean block,
							   gint32 *error)
{
	int ret;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	ret = ioctlsocket (sock, FIONBIO, (gulong *) &block);
	if(ret==SOCKET_ERROR) {
		*error = WSAGetLastError ();
	}
}

gpointer ves_icall_System_Net_Sockets_Socket_Accept_internal(SOCKET sock,
							     gint32 *error)
{
	SOCKET newsock;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	newsock = _wapi_accept (sock, NULL, 0);
	if(newsock==INVALID_SOCKET) {
		*error = WSAGetLastError ();
		return(NULL);
	}
	
	return(GUINT_TO_POINTER (newsock));
}

void ves_icall_System_Net_Sockets_Socket_Listen_internal(SOCKET sock,
							 guint32 backlog,
							 gint32 *error)
{
	int ret;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	ret = _wapi_listen (sock, backlog);
	if(ret==SOCKET_ERROR) {
		*error = WSAGetLastError ();
	}
}

static MonoObject *create_object_from_sockaddr(struct sockaddr *saddr,
					       int sa_size, gint32 *error)
{
	MonoDomain *domain = mono_domain_get ();
	MonoObject *sockaddr_obj;
	MonoClass *sockaddr_class;
	MonoClassField *field;
	MonoArray *data;
	MonoAddressFamily family;

	/* Build a System.Net.SocketAddress object instance */
	sockaddr_class=mono_class_from_name(system_assembly, "System.Net", "SocketAddress");
	sockaddr_obj=mono_object_new(domain, sockaddr_class);
	
	/* Locate the SocketAddress data buffer in the object */
	field=mono_class_get_field_from_name(sockaddr_class, "data");

	/* Make sure there is space for the family and size bytes */
	data=mono_array_new(domain, mono_get_byte_class (), sa_size+2);

	/* The data buffer is laid out as follows:
	 * byte 0 is the address family
	 * byte 1 is the buffer length
	 * bytes 2 and 3 are the port info
	 * the rest is the address info
	 */
		
	family=convert_to_mono_family(saddr->sa_family);
	if(family==AddressFamily_Unknown) {
		*error = WSAEAFNOSUPPORT;
		return(NULL);
	}

	mono_array_set(data, guint8, 0, family & 0x0FF);
	mono_array_set(data, guint8, 1, ((family << 8) & 0x0FFFF));
	
	if(saddr->sa_family==AF_INET) {
		struct sockaddr_in *sa_in=(struct sockaddr_in *)saddr;
		guint16 port=ntohs(sa_in->sin_port);
		guint32 address=ntohl(sa_in->sin_addr.s_addr);
		
		if(sa_size<8) {
			mono_raise_exception((MonoException *)mono_exception_from_name(mono_get_corlib (), "System", "SystemException"));
		}
		
		mono_array_set(data, guint8, 2, (port>>8) & 0xff);
		mono_array_set(data, guint8, 3, (port) & 0xff);
		mono_array_set(data, guint8, 4, (address>>24) & 0xff);
		mono_array_set(data, guint8, 5, (address>>16) & 0xff);
		mono_array_set(data, guint8, 6, (address>>8) & 0xff);
		mono_array_set(data, guint8, 7, (address) & 0xff);
	
		mono_field_set_value (sockaddr_obj, field, data);

		return(sockaddr_obj);
#ifdef AF_INET6
	} else if (saddr->sa_family == AF_INET6) {
		struct sockaddr_in6 *sa_in=(struct sockaddr_in6 *)saddr;
		int i;

		guint16 port=ntohs(sa_in->sin6_port);

		if(sa_size<28) {
			mono_raise_exception((MonoException *)mono_exception_from_name(mono_get_corlib (), "System", "SystemException"));
		}

		mono_array_set(data, guint8, 2, (port>>8) & 0xff);
		mono_array_set(data, guint8, 3, (port) & 0xff);

		for(i=0; i<16; i++) {
			mono_array_set(data, guint8, 8+i,
				       sa_in->sin6_addr.s6_addr[i]);
		}

		mono_array_set(data, guint8, 24, sa_in->sin6_scope_id & 0xff);
		mono_array_set(data, guint8, 25,
			       (sa_in->sin6_scope_id >> 8) & 0xff);
		mono_array_set(data, guint8, 26,
			       (sa_in->sin6_scope_id >> 16) & 0xff);
		mono_array_set(data, guint8, 27,
			       (sa_in->sin6_scope_id >> 24) & 0xff);

		mono_field_set_value (sockaddr_obj, field, data);

		return(sockaddr_obj);
#endif
#ifdef HAVE_SYS_UN_H
	} else if (saddr->sa_family == AF_UNIX) {
		int i;

		for (i = 0; i < sa_size; i++) {
			mono_array_set (data, guint8, i+2, saddr->sa_data[i]);
		}
		
		mono_field_set_value (sockaddr_obj, field, data);

		return sockaddr_obj;
#endif
	} else {
		*error = WSAEAFNOSUPPORT;
		return(NULL);
	}
}

extern MonoObject *ves_icall_System_Net_Sockets_Socket_LocalEndPoint_internal(SOCKET sock, gint32 *error)
{
	gchar sa[32];	/* sockaddr in not big enough for sockaddr_in6 */
	int salen;
	int ret;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	salen=sizeof(sa);
	ret = _wapi_getsockname (sock, (struct sockaddr *)sa, &salen);
	
	if(ret==SOCKET_ERROR) {
		*error = WSAGetLastError ();
		return(NULL);
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": bound to %s port %d", inet_ntoa(((struct sockaddr_in *)&sa)->sin_addr), ntohs(((struct sockaddr_in *)&sa)->sin_port));
#endif

	return(create_object_from_sockaddr((struct sockaddr *)sa, salen,
					   error));
}

extern MonoObject *ves_icall_System_Net_Sockets_Socket_RemoteEndPoint_internal(SOCKET sock, gint32 *error)
{
	gchar sa[32];	/* sockaddr in not big enough for sockaddr_in6 */
	int salen;
	int ret;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	salen=sizeof(sa);
	ret = _wapi_getpeername (sock, (struct sockaddr *)sa, &salen);
	
	if(ret==SOCKET_ERROR) {
		*error = WSAGetLastError ();
		return(NULL);
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": connected to %s port %d", inet_ntoa(((struct sockaddr_in *)&sa)->sin_addr), ntohs(((struct sockaddr_in *)&sa)->sin_port));
#endif

	return(create_object_from_sockaddr((struct sockaddr *)sa, salen,
					   error));
}

static struct sockaddr *create_sockaddr_from_object(MonoObject *saddr_obj,
						    int *sa_size,
						    gint32 *error)
{
	MonoClassField *field;
	MonoArray *data;
	gint32 family;
	int len;

	/* Dig the SocketAddress data buffer out of the object */
	field=mono_class_get_field_from_name(saddr_obj->vtable->klass, "data");
	data=*(MonoArray **)(((char *)saddr_obj) + field->offset);

	/* The data buffer is laid out as follows:
	 * byte 0 is the address family low byte
	 * byte 1 is the address family high byte
	 * INET:
	 * 	bytes 2 and 3 are the port info
	 * 	the rest is the address info
	 * UNIX:
	 * 	the rest is the file name
	 */
	len = mono_array_length (data);
	if (len < 2) {
		mono_raise_exception (mono_exception_from_name(mono_get_corlib (), "System", "SystemException"));
	}
	
	family = convert_family (mono_array_get (data, guint8, 0) + (mono_array_get (data, guint8, 1) << 8));
	if(family==AF_INET) {
		struct sockaddr_in *sa=g_new0(struct sockaddr_in, 1);
		guint16 port=(mono_array_get(data, guint8, 2) << 8) +
			mono_array_get(data, guint8, 3);
		guint32 address=(mono_array_get(data, guint8, 4) << 24) +
			(mono_array_get(data, guint8, 5) << 16 ) +
			(mono_array_get(data, guint8, 6) << 8) +
			mono_array_get(data, guint8, 7);
		
		sa->sin_family=family;
		sa->sin_addr.s_addr=htonl(address);
		sa->sin_port=htons(port);

		*sa_size=sizeof(struct sockaddr_in);
		return((struct sockaddr *)sa);

#ifdef AF_INET6
	} else if (family == AF_INET6) {
		struct sockaddr_in6 *sa=g_new0(struct sockaddr_in6, 1);
		int i;

		guint16 port = mono_array_get(data, guint8, 3) + (mono_array_get(data, guint8, 2) << 8);
		guint32 scopeid = mono_array_get(data, guint8, 24) + 
			(mono_array_get(data, guint8, 25)<<8) + 
			(mono_array_get(data, guint8, 26)<<16) + 
			(mono_array_get(data, guint8, 27)<<24);

		sa->sin6_family=family;
		sa->sin6_port=htons(port);
		sa->sin6_scope_id = scopeid;

		for(i=0; i<16; i++)
			sa->sin6_addr.s6_addr[i] = mono_array_get(data, guint8, 8+i);

		*sa_size=sizeof(struct sockaddr_in6);
		return((struct sockaddr *)sa);
#endif
#ifdef HAVE_SYS_UN_H
	} else if (family == AF_UNIX) {
		struct sockaddr_un *sock_un = g_new0 (struct sockaddr_un, 1);
		int i;

		if (len - 2 > MONO_SIZEOF_SUNPATH)
			mono_raise_exception (mono_get_exception_index_out_of_range ());

		sock_un->sun_family = family;
		for (i = 0; i < len - 2; i++)
			sock_un->sun_path [i] = mono_array_get (data, guint8,
								i + 2);
		sock_un->sun_path [len - 2] = '\0';
		*sa_size = sizeof (struct sockaddr_un);

		return (struct sockaddr *)sock_un;
#endif
	} else {
		*error = WSAEAFNOSUPPORT;
		return(0);
	}
}

extern void ves_icall_System_Net_Sockets_Socket_Bind_internal(SOCKET sock, MonoObject *sockaddr, gint32 *error)
{
	struct sockaddr *sa;
	int sa_size;
	int ret;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	sa=create_sockaddr_from_object(sockaddr, &sa_size, error);
	if (*error != 0) {
		return;
	}

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": binding to %s port %d", inet_ntoa(((struct sockaddr_in *)sa)->sin_addr), ntohs (((struct sockaddr_in *)sa)->sin_port));
#endif

	ret = _wapi_bind (sock, sa, sa_size);
	if(ret==SOCKET_ERROR) {
		*error = WSAGetLastError ();
	}

	g_free(sa);
}

enum {
	SelectModeRead,
	SelectModeWrite,
	SelectModeError
};

MonoBoolean
ves_icall_System_Net_Sockets_Socket_Poll_internal (SOCKET sock, gint mode,
						   gint timeout, gint32 *error)
{
	fd_set fds;
	int ret = 0;
	struct timeval tv;
	struct timeval *tvptr;
	div_t divvy;

	MONO_ARCH_SAVE_REGS;

	do {
		/* FIXME: in case of extra iteration (WSAEINTR), substract time
		 * from the initial timeout */
		*error = 0;
		FD_ZERO (&fds);
		_wapi_FD_SET (sock, &fds);
		if (timeout >= 0) {
			divvy = div (timeout, 1000000);
			tv.tv_sec = divvy.quot;
			tv.tv_usec = divvy.rem;
			tvptr = &tv;
		} else {
			tvptr = NULL;
		}

		if (mode == SelectModeRead) {
			ret = _wapi_select (0, &fds, NULL, NULL, tvptr);
		} else if (mode == SelectModeWrite) {
			ret = _wapi_select (0, NULL, &fds, NULL, tvptr);
		} else if (mode == SelectModeError) {
			ret = _wapi_select (0, NULL, NULL, &fds, tvptr);
		} else {
			g_assert_not_reached ();
		}
	} while ((ret == SOCKET_ERROR) && (*error == WSAGetLastError ()) == WSAEINTR);

	return (ret != SOCKET_ERROR && _wapi_FD_ISSET (sock, &fds));
}

extern void ves_icall_System_Net_Sockets_Socket_Connect_internal(SOCKET sock, MonoObject *sockaddr, gint32 *error)
{
	struct sockaddr *sa;
	int sa_size;
	int ret;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	sa=create_sockaddr_from_object(sockaddr, &sa_size, error);
	if (*error != 0) {
		return;
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": connecting to %s port %d", inet_ntoa(((struct sockaddr_in *)sa)->sin_addr), ntohs (((struct sockaddr_in *)sa)->sin_port));
#endif

	ret = _wapi_connect (sock, sa, sa_size);
	if(ret==SOCKET_ERROR) {
		*error = WSAGetLastError ();
	}

	g_free(sa);
}

gint32 ves_icall_System_Net_Sockets_Socket_Receive_internal(SOCKET sock, MonoArray *buffer, gint32 offset, gint32 count, gint32 flags, gint32 *error)
{
	int ret;
	guchar *buf;
	gint32 alen;
	int recvflags=0;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	alen=mono_array_length(buffer);
	if(offset+count>alen) {
		return(0);
	}
	
	buf=mono_array_addr(buffer, guchar, offset);
	
	ret = _wapi_recv (sock, buf, count, recvflags);
	if(ret==SOCKET_ERROR) {
		*error = WSAGetLastError ();
		return(0);
	}

	return(ret);
}

gint32 ves_icall_System_Net_Sockets_Socket_RecvFrom_internal(SOCKET sock, MonoArray *buffer, gint32 offset, gint32 count, gint32 flags, MonoObject **sockaddr, gint32 *error)
{
	int ret;
	guchar *buf;
	gint32 alen;
	int recvflags=0;
	struct sockaddr *sa;
	int sa_size;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	alen=mono_array_length(buffer);
	if(offset+count>alen) {
		return(0);
	}

	sa=create_sockaddr_from_object(*sockaddr, &sa_size, error);
	if (*error != 0) {
		return(0);
	}
	
	buf=mono_array_addr(buffer, guchar, offset);
	
	ret = _wapi_recvfrom (sock, buf, count, recvflags, sa, &sa_size);
	if(ret==SOCKET_ERROR) {
		g_free(sa);
		*error = WSAGetLastError ();
		return(0);
	}

	/* If we didn't get a socket size, then we're probably a
	 * connected connection-oriented socket and the stack hasn't
	 * returned the remote address. All we can do is return null.
	 */
	if ( sa_size != 0 )
		*sockaddr=create_object_from_sockaddr(sa, sa_size, error);
	else
		*sockaddr=NULL;

	g_free(sa);
	
	return(ret);
}

gint32 ves_icall_System_Net_Sockets_Socket_Send_internal(SOCKET sock, MonoArray *buffer, gint32 offset, gint32 count, gint32 flags, gint32 *error)
{
	int ret;
	guchar *buf;
	gint32 alen;
	int sendflags=0;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	alen=mono_array_length(buffer);
	if(offset+count>alen) {
		return(0);
	}

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": alen: %d", alen);
#endif
	
	buf=mono_array_addr(buffer, guchar, offset);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Sending %d bytes", count);
#endif

	ret = _wapi_send (sock, buf, count, sendflags);
	if(ret==SOCKET_ERROR) {
		*error = WSAGetLastError ();
		return(0);
	}

	return(ret);
}

gint32 ves_icall_System_Net_Sockets_Socket_SendTo_internal(SOCKET sock, MonoArray *buffer, gint32 offset, gint32 count, gint32 flags, MonoObject *sockaddr, gint32 *error)
{
	int ret;
	guchar *buf;
	gint32 alen;
	int sendflags=0;
	struct sockaddr *sa;
	int sa_size;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	alen=mono_array_length(buffer);
	if(offset+count>alen) {
		return(0);
	}

	sa=create_sockaddr_from_object(sockaddr, &sa_size, error);
	if(*error != 0) {
		return(0);
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": alen: %d", alen);
#endif
	
	buf=mono_array_addr(buffer, guchar, offset);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Sending %d bytes", count);
#endif

	ret = _wapi_sendto (sock, buf, count, sendflags, sa, sa_size);
	if(ret==SOCKET_ERROR) {
		*error = WSAGetLastError ();
	}

	g_free(sa);
	
	return(ret);
}

static SOCKET Socket_to_SOCKET(MonoObject *sockobj)
{
	SOCKET sock;
	MonoClassField *field;
	
	field=mono_class_get_field_from_name(sockobj->vtable->klass, "socket");
	sock=*(SOCKET *)(((char *)sockobj)+field->offset);

	return(sock);
}

void ves_icall_System_Net_Sockets_Socket_Select_internal(MonoArray **read_socks, MonoArray **write_socks, MonoArray **err_socks, gint32 timeout, gint32 *error)
{
	fd_set readfds, writefds, errfds;
	fd_set *readptr = NULL, *writeptr = NULL, *errptr = NULL;
	struct timeval tv;
	div_t divvy;
	int ret;
	int readarrsize = 0, writearrsize = 0, errarrsize = 0;
	MonoDomain *domain=mono_domain_get();
	MonoClass *sock_arr_class;
	MonoArray *socks;
	int count;
	int i;
	SOCKET handle;
	
	MONO_ARCH_SAVE_REGS;

	if (*read_socks)
		readarrsize=mono_array_length(*read_socks);

	*error = 0;
	
	if(readarrsize>FD_SETSIZE) {
		*error = WSAEFAULT;
		return;
	}
	
	if (readarrsize) {
		readptr = &readfds;
		FD_ZERO(&readfds);
		for(i=0; i<readarrsize; i++) {
			handle = Socket_to_SOCKET(mono_array_get(*read_socks, MonoObject *, i));
			_wapi_FD_SET(handle, &readfds);
		}
	}
	
	if (*write_socks)
		writearrsize=mono_array_length(*write_socks);

	if(writearrsize>FD_SETSIZE) {
		*error = WSAEFAULT;
		return;
	}
	
	if (writearrsize) {
		writeptr = &writefds;
		FD_ZERO(&writefds);
		for(i=0; i<writearrsize; i++) {
			handle = Socket_to_SOCKET(mono_array_get(*write_socks, MonoObject *, i));
			_wapi_FD_SET(handle, &writefds);
		}
	}
	
	if (*err_socks)
		errarrsize=mono_array_length(*err_socks);

	if(errarrsize>FD_SETSIZE) {
		*error = WSAEFAULT;
		return;
	}
	
	if (errarrsize) {
		errptr = &errfds;
		FD_ZERO(&errfds);
		for(i=0; i<errarrsize; i++) {
			handle = Socket_to_SOCKET(mono_array_get(*err_socks, MonoObject *, i));
			_wapi_FD_SET(handle, &errfds);
		}
	}

	/* Negative timeout meaning block until ready is only
	 * specified in Poll, not Select
	 */

	divvy = div (timeout, 1000000);
	
	do {
		if(timeout>=0) {
			tv.tv_sec=divvy.quot;
			tv.tv_usec=divvy.rem;

			ret = _wapi_select (0, readptr, writeptr, errptr, &tv);
		} else {
			ret = _wapi_select (0, readptr, writeptr, errptr, NULL);
		}
	} while ((ret==SOCKET_ERROR) && (WSAGetLastError() == WSAEINTR));
	
	if(ret==SOCKET_ERROR) {
		*error = WSAGetLastError ();
		return;
	}

	if (readarrsize) {
		sock_arr_class=((MonoObject *)*read_socks)->vtable->klass;
		
		count=0;
		for(i=0; i<readarrsize; i++) {
			if(_wapi_FD_ISSET(Socket_to_SOCKET(mono_array_get(*read_socks, MonoObject *, i)), &readfds)) {
				count++;
			}
		}
		socks=mono_array_new_full(domain, sock_arr_class, &count, NULL);
		count=0;
		for(i=0; i<readarrsize; i++) {
			MonoObject *sock=mono_array_get(*read_socks, MonoObject *, i);
			
			if(_wapi_FD_ISSET(Socket_to_SOCKET(sock), &readfds)) {
				mono_array_set(socks, MonoObject *, count, sock);
				count++;
			}
		}
		*read_socks=socks;
	} else {
		*read_socks = NULL;
	}

	if (writearrsize) {
		sock_arr_class=((MonoObject *)*write_socks)->vtable->klass;
		count=0;
		for(i=0; i<writearrsize; i++) {
			if(_wapi_FD_ISSET(Socket_to_SOCKET(mono_array_get(*write_socks, MonoObject *, i)), &writefds)) {
				count++;
			}
		}
		socks=mono_array_new_full(domain, sock_arr_class, &count, NULL);
		count=0;
		for(i=0; i<writearrsize; i++) {
			MonoObject *sock=mono_array_get(*write_socks, MonoObject *, i);
			
			if(_wapi_FD_ISSET(Socket_to_SOCKET(sock), &writefds)) {
				mono_array_set(socks, MonoObject *, count, sock);
				count++;
			}
		}
		*write_socks=socks;
	} else {
		*write_socks = NULL;
	}

	if (errarrsize) {
		sock_arr_class=((MonoObject *)*err_socks)->vtable->klass;
		count=0;
		for(i=0; i<errarrsize; i++) {
			if(_wapi_FD_ISSET(Socket_to_SOCKET(mono_array_get(*err_socks, MonoObject *, i)), &errfds)) {
				count++;
			}
		}
		socks=mono_array_new_full(domain, sock_arr_class, &count, NULL);
		count=0;
		for(i=0; i<errarrsize; i++) {
			MonoObject *sock=mono_array_get(*err_socks, MonoObject *, i);
			
			if(_wapi_FD_ISSET(Socket_to_SOCKET(sock), &errfds)) {
				mono_array_set(socks, MonoObject *, count, sock);
				count++;
			}
		}
		*err_socks=socks;
	}
}

static MonoObject* int_to_object (MonoDomain *domain, int val)
{
	return mono_value_box (domain, mono_get_int32_class (), &val);
}


void ves_icall_System_Net_Sockets_Socket_GetSocketOption_obj_internal(SOCKET sock, gint32 level, gint32 name, MonoObject **obj_val, gint32 *error)
{
	int system_level;
	int system_name;
	int ret;
	int val;
	int valsize=sizeof(val);
	struct linger linger;
	int lingersize=sizeof(linger);
	struct timeval tv;
	int tvsize=sizeof(tv);
#ifdef SO_PEERCRED
	struct ucred cred;
	int credsize = sizeof(cred);
#endif
	MonoDomain *domain=mono_domain_get();
	MonoObject *obj;
	MonoClass *obj_class;
	MonoClassField *field;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	ret=convert_sockopt_level_and_name(level, name, &system_level,
					   &system_name);
	if(ret==-1) {
		*error = WSAENOPROTOOPT;
		return;
	}
	
	/* No need to deal with MulticastOption names here, because
	 * you cant getsockopt AddMembership or DropMembership (the
	 * int getsockopt will error, causing an exception)
	 */
	switch(name) {
	case SocketOptionName_Linger:
	case SocketOptionName_DontLinger:
		ret = _wapi_getsockopt(sock, system_level, system_name, &linger,
			       &lingersize);
		break;
		
	case SocketOptionName_SendTimeout:
	case SocketOptionName_ReceiveTimeout:
		ret = _wapi_getsockopt (sock, system_level, system_name, &tv,
		           &tvsize);
		break;

#ifdef SO_PEERCRED
	case SocketOptionName_PeerCred: 
		ret = _wapi_getsockopt (sock, system_level, system_name, &cred,
					&credsize);
		break;
#endif

	default:
		ret = _wapi_getsockopt (sock, system_level, system_name, &val,
			       &valsize);
	}
	
	if(ret==SOCKET_ERROR) {
		*error = WSAGetLastError ();
		return;
	}
	
	switch(name) {
	case SocketOptionName_Linger:
		/* build a System.Net.Sockets.LingerOption */
		obj_class=mono_class_from_name(system_assembly,
					       "System.Net.Sockets",
					       "LingerOption");
		obj=mono_object_new(domain, obj_class);
		
		/* Locate and set the fields "bool enabled" and "int
		 * seconds"
		 */
		field=mono_class_get_field_from_name(obj_class, "enabled");
		*(guint8 *)(((char *)obj)+field->offset)=linger.l_onoff;

		field=mono_class_get_field_from_name(obj_class, "seconds");
		*(guint32 *)(((char *)obj)+field->offset)=linger.l_linger;
		
		break;
		
	case SocketOptionName_DontLinger:
		/* construct a bool int in val - true if linger is off */
		obj = int_to_object (domain, !linger.l_onoff);
		break;
		
	case SocketOptionName_SendTimeout:
	case SocketOptionName_ReceiveTimeout:
		obj = int_to_object (domain, (tv.tv_sec * 1000) + (tv.tv_usec / 1000));
		break;

#ifdef SO_PEERCRED
	case SocketOptionName_PeerCred: 
	{
		/* build a Mono.Posix.PeerCred+PeerCredData if
		 * possible
		 */
		MonoImage *mono_posix_image = mono_image_loaded ("Mono.Posix");
		MonoPeerCredData *cred_data;
		
		if (mono_posix_image == NULL) {
			*error = WSAENOPROTOOPT;
			return;
		}
		
		obj_class = mono_class_from_name(mono_posix_image,
						 "Mono.Posix",
						 "PeerCred/PeerCredData");
		obj = mono_object_new(domain, obj_class);
		cred_data = (MonoPeerCredData *)obj;
		cred_data->pid = cred.pid;
		cred_data->uid = cred.uid;
		cred_data->gid = cred.gid;
		break;
	}
#endif

	default:
		obj = int_to_object (domain, val);
	}

	*obj_val=obj;
}

void ves_icall_System_Net_Sockets_Socket_GetSocketOption_arr_internal(SOCKET sock, gint32 level, gint32 name, MonoArray **byte_val, gint32 *error)
{
	int system_level;
	int system_name;
	int ret;
	guchar *buf;
	int valsize;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	ret=convert_sockopt_level_and_name(level, name, &system_level,
					   &system_name);
	if(ret==-1) {
		*error = WSAENOPROTOOPT;
		return;
	}

	valsize=mono_array_length(*byte_val);
	buf=mono_array_addr(*byte_val, guchar, 0);
	
	ret = _wapi_getsockopt (sock, system_level, system_name, buf, &valsize);
	if(ret==SOCKET_ERROR) {
		*error = WSAGetLastError ();
	}
}

#if defined(HAVE_STRUCT_IP_MREQN) || defined(HAVE_STRUCT_IP_MREQ)
static struct in_addr ipaddress_to_struct_in_addr(MonoObject *ipaddr)
{
	struct in_addr inaddr;
	MonoClassField *field;
	
	field=mono_class_get_field_from_name(ipaddr->vtable->klass, "address");

	/* No idea why .net uses a 64bit type to hold a 32bit value...
	 *
	 * Internal value of IPAddess is in Network Order, there is no need
	 * to call htonl here.
	 */
	inaddr.s_addr=(guint32)*(guint64 *)(((char *)ipaddr)+field->offset);
	
	return(inaddr);
}
#endif

#ifdef AF_INET6
static struct in6_addr ipaddress_to_struct_in6_addr(MonoObject *ipaddr)
{
	struct in6_addr in6addr;
	MonoClassField *field;
	MonoArray *data;
	int i;

	field=mono_class_get_field_from_name(ipaddr->vtable->klass, "_numbers");
	data=*(MonoArray **)(((char *)ipaddr) + field->offset);

/* Solaris has only the 8 bit version. */
#ifndef s6_addr16
	for(i=0; i<8; i++) {
		guint16 s = mono_array_get (data, guint16, i);
		in6addr.s6_addr[2 * i] = (s >> 8) & 0xff;
		in6addr.s6_addr[2 * i + 1] = s & 0xff;
	}
#else
	for(i=0; i<8; i++)
		in6addr.s6_addr16[i] = mono_array_get (data, guint16, i);
#endif
	return(in6addr);
}
#endif /* AF_INET6 */

void ves_icall_System_Net_Sockets_Socket_SetSocketOption_internal(SOCKET sock, gint32 level, gint32 name, MonoObject *obj_val, MonoArray *byte_val, gint32 int_val, gint32 *error)
{
	int system_level;
	int system_name;
	int ret;
#ifdef AF_INET6
	int sol_ip;
	int sol_ipv6;

	*error = 0;
	
#ifdef HAVE_SOL_IPV6
	sol_ipv6 = SOL_IPV6;
#else
	{
		struct protoent *pent;
		pent = getprotobyname ("ipv6");
		sol_ipv6 = (pent != NULL) ? pent->p_proto : 41;
	}
#endif

#ifdef HAVE_SOL_IP
	sol_ip = SOL_IP;
#else
	{
		struct protoent *pent;
		pent = getprotobyname ("ip");
		sol_ip = (pent != NULL) ? pent->p_proto : 0;
	}
#endif
#endif /* AF_INET6 */

	MONO_ARCH_SAVE_REGS;

	ret=convert_sockopt_level_and_name(level, name, &system_level,
					   &system_name);
	if(ret==-1) {
		*error = WSAENOPROTOOPT;
		return;
	}

	/* Only one of obj_val, byte_val or int_val has data */
	if(obj_val!=NULL) {
		MonoClassField *field;
		struct linger linger;
		int valsize;
		
		switch(name) {
		case SocketOptionName_DontLinger:
			linger.l_onoff=0;
			linger.l_linger=0;
			valsize=sizeof(linger);
			ret = _wapi_setsockopt (sock, system_level,
						system_name, &linger, valsize);
			break;
			
		case SocketOptionName_Linger:
			/* Dig out "bool enabled" and "int seconds"
			 * fields
			 */
			field=mono_class_get_field_from_name(obj_val->vtable->klass, "enabled");
			linger.l_onoff=*(guint8 *)(((char *)obj_val)+field->offset);
			field=mono_class_get_field_from_name(obj_val->vtable->klass, "seconds");
			linger.l_linger=*(guint32 *)(((char *)obj_val)+field->offset);
			
			valsize=sizeof(linger);
			ret = _wapi_setsockopt (sock, system_level,
						system_name, &linger, valsize);
			break;
		case SocketOptionName_AddMembership:
		case SocketOptionName_DropMembership:
#if defined(HAVE_STRUCT_IP_MREQN) || defined(HAVE_STRUCT_IP_MREQ)
		{
			MonoObject *address = NULL;

#ifdef AF_INET6
			if(system_level == sol_ipv6) {
				struct ipv6_mreq mreq6;

				/*
				 *	Get group address
				 */
				field = mono_class_get_field_from_name (obj_val->vtable->klass, "group");
				address = *(gpointer *)(((char *)obj_val) + field->offset);
				
				if(address) {
					mreq6.ipv6mr_multiaddr = ipaddress_to_struct_in6_addr (address);
				}

				field=mono_class_get_field_from_name(obj_val->vtable->klass, "ifIndex");
				mreq6.ipv6mr_interface =*(guint64 *)(((char *)obj_val)+field->offset);

				ret = _wapi_setsockopt (sock, system_level,
							system_name, &mreq6,
							sizeof (mreq6));
			} else if(system_level == sol_ip)
#endif /* AF_INET6 */
			{
#ifdef HAVE_STRUCT_IP_MREQN
				struct ip_mreqn mreq = {{0}};
#else
				struct ip_mreq mreq = {{0}};
#endif /* HAVE_STRUCT_IP_MREQN */
			
				/* pain! MulticastOption holds two IPAddress
				 * members, so I have to dig the value out of
				 * those :-(
				 */
				field = mono_class_get_field_from_name (obj_val->vtable->klass, "group");
				address = *(gpointer *)(((char *)obj_val) + field->offset);

				/* address might not be defined and if so, set the address to ADDR_ANY.
				 */
				if(address) {
					mreq.imr_multiaddr = ipaddress_to_struct_in_addr (address);
				}

				field = mono_class_get_field_from_name (obj_val->vtable->klass, "local");
				address = *(gpointer *)(((char *)obj_val) + field->offset);

#ifdef HAVE_STRUCT_IP_MREQN
				if(address) {
					mreq.imr_address = ipaddress_to_struct_in_addr (address);
				}
#else
				if(address) {
					mreq.imr_interface = ipaddress_to_struct_in_addr (address);
				}
#endif /* HAVE_STRUCT_IP_MREQN */
			
				ret = _wapi_setsockopt (sock, system_level,
							system_name, &mreq,
							sizeof (mreq));
			}
			break;
		}
#endif /* HAVE_STRUCT_IP_MREQN || HAVE_STRUCT_IP_MREQ */
		default:
			/* Cause an exception to be thrown */
			*error = WSAEINVAL;
			return;
		}
	} else if (byte_val!=NULL) {
		int valsize=mono_array_length(byte_val);
		guchar *buf=mono_array_addr(byte_val, guchar, 0);
	
		ret = _wapi_setsockopt (sock, system_level, system_name, buf, valsize);
		if(ret==SOCKET_ERROR) {
			*error = WSAGetLastError ();
			return;
		}
	} else {
		switch(name) {
			case SocketOptionName_SendTimeout:
			case SocketOptionName_ReceiveTimeout: {
				struct timeval tv;
				tv.tv_sec = int_val / 1000;
				tv.tv_usec = (int_val % 1000) * 1000;
				ret = _wapi_setsockopt (sock, system_level, system_name, &tv, sizeof (tv));
				break;
			}
			default:
				ret = _wapi_setsockopt (sock, system_level, system_name, &int_val,
			       sizeof(int_val));
		}
	}

	if(ret==SOCKET_ERROR) {
		*error = WSAGetLastError ();
	}
}

void ves_icall_System_Net_Sockets_Socket_Shutdown_internal(SOCKET sock,
							   gint32 how,
							   gint32 *error)
{
	int ret;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	/* Currently, the values for how (recv=0, send=1, both=2) match
	 * the BSD API
	 */
	ret = _wapi_shutdown (sock, how);
	if(ret==SOCKET_ERROR) {
		*error = WSAGetLastError ();
	}
}

gint
ves_icall_System_Net_Sockets_Socket_WSAIoctl (SOCKET sock, gint32 code,
					      MonoArray *input,
					      MonoArray *output, gint32 *error)
{
	gulong output_bytes = 0;
	gchar *i_buffer, *o_buffer;
	gint i_len, o_len;
	gint ret;

	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	if (code == FIONBIO) {
		/* Invalid command. Must use Socket.Blocking */
		return -1;
	}

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

	ret = WSAIoctl (sock, code, i_buffer, i_len, o_buffer, o_len, &output_bytes, NULL, NULL);
	if (ret == SOCKET_ERROR) {
		*error = WSAGetLastError ();
		return(-1);
	}

	return (gint) output_bytes;
}

#ifndef AF_INET6
static gboolean hostent_to_IPHostEntry(struct hostent *he, MonoString **h_name,
				       MonoArray **h_aliases,
				       MonoArray **h_addr_list)
{
	MonoDomain *domain = mono_domain_get ();
	int i;

	if(he->h_length!=4 || he->h_addrtype!=AF_INET) {
		return(FALSE);
	}

	*h_name=mono_string_new(domain, he->h_name);

	i=0;
	while(he->h_aliases[i]!=NULL) {
		i++;
	}
	
	*h_aliases=mono_array_new(domain, mono_get_string_class (), i);
	i=0;
	while(he->h_aliases[i]!=NULL) {
		MonoString *alias;
		
		alias=mono_string_new(domain, he->h_aliases[i]);
		mono_array_set(*h_aliases, MonoString *, i, alias);
		i++;
	}

	i=0;
	while(he->h_addr_list[i]!=NULL) {
		i++;
	}
	
	*h_addr_list=mono_array_new(domain, mono_get_string_class (), i);
	i=0;
	while(he->h_addr_list[i]!=NULL) {
		MonoString *addr_string;
		char addr[16];
		
		g_snprintf(addr, 16, "%u.%u.%u.%u",
			 (unsigned char)he->h_addr_list[i][0],
			 (unsigned char)he->h_addr_list[i][1],
			 (unsigned char)he->h_addr_list[i][2],
			 (unsigned char)he->h_addr_list[i][3]);
		
		addr_string=mono_string_new(domain, addr);
		mono_array_set(*h_addr_list, MonoString *, i, addr_string);
		i++;
	}

	return(TRUE);
}
#endif

#if defined(AF_INET6) && defined(HAVE_GETHOSTBYNAME2_R)
static gboolean hostent_to_IPHostEntry2(struct hostent *he1,struct hostent *he2, MonoString **h_name, MonoArray **h_aliases, MonoArray **h_addr_list)
{
	MonoDomain *domain = mono_domain_get ();
	int i, host_count, host_index, family_hint;

	family_hint = get_family_hint ();

	if(he1 == NULL && he2 == NULL) {
		return(FALSE);
	}

	/*
	 * Check if address length and family are correct
	 */
	if (he1 != NULL && (he1->h_length!=4 || he1->h_addrtype!=AF_INET)) {
		return(FALSE);
	}

	if (he2 != NULL && (he2->h_length!=16 || he2->h_addrtype!=AF_INET6)) {
		return(FALSE);
	}

	/*
	 * Get the aliases and host name from he1 or he2 whichever is
	 * not null, if he1 is not null then take aliases from he1
	 */
	if (he1 != NULL && (family_hint == PF_UNSPEC ||
			    family_hint == PF_INET)) {
		*h_name=mono_string_new (domain, he1->h_name);

		i=0;
		while(he1->h_aliases[i]!=NULL) {
			i++;
		}

		*h_aliases=mono_array_new (domain, mono_get_string_class (),
					   i);
		i=0;
		while(he1->h_aliases[i]!=NULL) {
			MonoString *alias;

			alias=mono_string_new (domain, he1->h_aliases[i]);
			mono_array_set (*h_aliases, MonoString *, i, alias);
			i++;
		}
	} else if (family_hint == PF_UNSPEC || family_hint == PF_INET6) {
		*h_name=mono_string_new (domain, he2->h_name);

		i=0;
		while(he2->h_aliases [i] != NULL) {
			i++;
		}

		*h_aliases=mono_array_new (domain, mono_get_string_class (),
					   i);
		i=0;
		while(he2->h_aliases[i]!=NULL) {
			MonoString *alias;

			alias=mono_string_new (domain, he2->h_aliases[i]);
			mono_array_set (*h_aliases, MonoString *, i, alias);
			i++;
		}
	}

	/*
	 * Count the number of addresses in he1 + he2
	 */
	host_count = 0;
	if (he1 != NULL && (family_hint == PF_UNSPEC ||
			    family_hint == PF_INET)) {
		i=0;
		while(he1->h_addr_list[i]!=NULL) {
			i++;
			host_count++;
		}
	}

	if (he2 != NULL && (family_hint == PF_UNSPEC ||
			    family_hint == PF_INET6)) {
		i=0;
		while(he2->h_addr_list[i]!=NULL) {
			i++;
			host_count++;
		}
	}

	/*
	 * Fills the array
	 */
	*h_addr_list=mono_array_new (domain, mono_get_string_class (),
				     host_count);

	host_index = 0;

	if (he2 != NULL && (family_hint == PF_UNSPEC ||
			    family_hint == PF_INET6)) {
		i = 0;
		while(he2->h_addr_list[i] != NULL) {
			MonoString *addr_string;
			char addr[40];

			inet_ntop (AF_INET6, he2->h_addr_list[i], addr,
				   sizeof(addr));

			addr_string = mono_string_new (domain, addr);
			mono_array_set (*h_addr_list, MonoString *, host_index,
					addr_string);
			i++;
			host_index++;
		}
	}

	if (he1 != NULL && (family_hint == PF_UNSPEC ||
			    family_hint == PF_INET)) {
		i=0;
		while(he1->h_addr_list[i] != NULL) {
			MonoString *addr_string;
			char addr[17];

			inet_ntop (AF_INET, he1->h_addr_list[i], addr,
				   sizeof(addr));

			addr_string=mono_string_new (domain, addr);
			mono_array_set (*h_addr_list, MonoString *, host_index,
					addr_string);
			i++;
			host_index++;
		}
	}

	return(TRUE);
}
#endif

#if defined(AF_INET6)
static gboolean 
addrinfo_to_IPHostEntry(struct addrinfo *info, MonoString **h_name,
						MonoArray **h_aliases,
						MonoArray **h_addr_list)
{
	gint32 count, i;
	struct addrinfo *ai = NULL;

	MonoDomain *domain = mono_domain_get ();

	for (count=0, ai=info; ai!=NULL; ai=ai->ai_next) {
		if((ai->ai_family != PF_INET) && (ai->ai_family != PF_INET6)) {
			continue;
		}

		count++;
	}

	*h_aliases=mono_array_new(domain, mono_get_string_class (), 0);
	*h_addr_list=mono_array_new(domain, mono_get_string_class (), count);

	for (ai=info, i=0; ai!=NULL; ai=ai->ai_next) {
		MonoString *addr_string;
		const char *ret;
		char *buffer;
		gint32 buffer_size = 0;

		if((ai->ai_family != PF_INET) && (ai->ai_family != PF_INET6)) {
			continue;
		}

		buffer_size = 256;
		do {
			buffer = g_malloc0(buffer_size);

			if(ai->ai_family == PF_INET) {
				ret = inet_ntop(ai->ai_family, (void*)&(((struct sockaddr_in*)ai->ai_addr)->sin_addr), buffer, buffer_size);
			} else {
				ret = inet_ntop(ai->ai_family, (void*)&(((struct sockaddr_in6*)ai->ai_addr)->sin6_addr), buffer, buffer_size);
			}

			if(ret == 0) {
				g_free(buffer);
				buffer_size += 256;
			}
		} while(ret == 0 && errno == ENOSPC);

		if(ret) {
			addr_string=mono_string_new(domain, buffer);
			g_free(buffer);
		} else {
			addr_string=mono_string_new(domain, "");
		}

		mono_array_set(*h_addr_list, MonoString *, i, addr_string);

		if(!i && ai->ai_canonname != NULL) {
			*h_name=mono_string_new(domain, ai->ai_canonname);
		}

		i++;
	}

	if(info) {
		freeaddrinfo(info);
	}

	return(TRUE);
}
#endif

#ifdef AF_INET6
MonoBoolean ves_icall_System_Net_Dns_GetHostByName_internal(MonoString *host, MonoString **h_name, MonoArray **h_aliases, MonoArray **h_addr_list)
{
#if !defined(HAVE_GETHOSTBYNAME2_R)
	struct addrinfo *info = NULL, hints;
	char *hostname;
	
	MONO_ARCH_SAVE_REGS;
	
	hostname=mono_string_to_utf8 (host);
	
	memset(&hints, 0, sizeof(hints));
	hints.ai_family = get_family_hint ();
	hints.ai_socktype = SOCK_STREAM;
	hints.ai_flags = AI_CANONNAME;

	if (getaddrinfo(hostname, NULL, &hints, &info) == -1) {
		return(FALSE);
	}
	
	g_free(hostname);

	return(addrinfo_to_IPHostEntry(info, h_name, h_aliases, h_addr_list));
#else
	struct hostent he1,*hp1, he2, *hp2;
	int buffer_size1, buffer_size2;
	char *buffer1, *buffer2;
	int herr;
	gboolean return_value;
	char *hostname;
	
	MONO_ARCH_SAVE_REGS;
	
	hostname=mono_string_to_utf8 (host);

	buffer_size1 = 512;
	buffer_size2 = 512;
	buffer1 = g_malloc0(buffer_size1);
	buffer2 = g_malloc0(buffer_size2);

	while (gethostbyname2_r(hostname, AF_INET, &he1, buffer1, buffer_size1,
				&hp1, &herr) == ERANGE) {
		buffer_size1 *= 2;
		buffer1 = g_realloc(buffer1, buffer_size1);
	}

	if (hp1 == NULL)
	{
		while (gethostbyname2_r(hostname, AF_INET6, &he2, buffer2,
					buffer_size2, &hp2, &herr) == ERANGE) {
			buffer_size2 *= 2;
			buffer2 = g_realloc(buffer2, buffer_size2);
		}
	}
	else
		hp2 = NULL;

	return_value = hostent_to_IPHostEntry2(hp1, hp2, h_name, h_aliases,
					       h_addr_list);

	g_free(buffer1);
	g_free(buffer2);
	g_free(hostname);

	return(return_value);
#endif /* HAVE_GETHOSTBYNAME2_R */
}
#else /* AF_INET6 */
MonoBoolean ves_icall_System_Net_Dns_GetHostByName_internal(MonoString *host, MonoString **h_name, MonoArray **h_aliases, MonoArray **h_addr_list)
{
	struct hostent *he;
	char *hostname;
	
	MONO_ARCH_SAVE_REGS;

	hostname=mono_string_to_utf8(host);

	he = _wapi_gethostbyname (hostname);
	g_free(hostname);

	if(he==NULL) {
		return(FALSE);
	}

	return(hostent_to_IPHostEntry(he, h_name, h_aliases, h_addr_list));
}
#endif /* AF_INET6 */

#ifndef HAVE_INET_PTON
static int
inet_pton (int family, const char *address, void *inaddrp)
{
	if (family == AF_INET) {
#ifdef HAVE_INET_ATON
		struct in_addr inaddr;
		
		if (!inet_aton (address, &inaddr))
			return 0;
		
		memcpy (inaddrp, &inaddr, sizeof (struct in_addr));
		return 1;
#else
		/* assume the system has inet_addr(), if it doesn't
		   have that we're pretty much screwed... */
		guint32 inaddr;
		
		if (!strcmp (address, "255.255.255.255")) {
			/* special-case hack */
			inaddr = 0xffffffff;
		} else {
			inaddr = inet_addr (address);
#ifndef INADDR_NONE
#define INADDR_NONE ((in_addr_t) -1)
#endif
			if (inaddr == INADDR_NONE)
				return 0;
		}
		
		memcpy (inaddrp, &inaddr, sizeof (guint32));
		return 1;
#endif /* HAVE_INET_ATON */
	}
	
	return -1;
}
#endif /* !HAVE_INET_PTON */

extern MonoBoolean ves_icall_System_Net_Dns_GetHostByAddr_internal(MonoString *addr, MonoString **h_name, MonoArray **h_aliases, MonoArray **h_addr_list)
{
	char *address;

#ifdef AF_INET6
	struct sockaddr_in saddr;
	struct sockaddr_in6 saddr6;
	struct addrinfo *info = NULL, hints;
	gint32 family;
	char hostname[1024] = {0};
#else
	struct in_addr inaddr;
	struct hostent *he;
#endif

	MONO_ARCH_SAVE_REGS;

	address = mono_string_to_utf8 (addr);

#ifdef AF_INET6
	if (inet_pton (AF_INET, address, &saddr.sin_addr ) <= 0) {
		/* Maybe an ipv6 address */
		if (inet_pton (AF_INET6, address, &saddr6.sin6_addr) <= 0) {
			g_free (address);
			return FALSE;
		}
		else {
			family = AF_INET6;
			saddr6.sin6_family = AF_INET6;
		}
	}
	else {
		family = AF_INET;
		saddr.sin_family = AF_INET;
	}
	g_free(address);

	if(family == AF_INET) {
		if(getnameinfo ((struct sockaddr*)&saddr, sizeof(saddr),
				hostname, sizeof(hostname), NULL, 0,
				NI_NAMEREQD) != 0) {
			return(FALSE);
		}
	} else if(family == AF_INET6) {
		if(getnameinfo ((struct sockaddr*)&saddr6, sizeof(saddr6),
				hostname, sizeof(hostname), NULL, 0,
				NI_NAMEREQD) != 0) {
			return(FALSE);
		}
	}

	memset (&hints, 0, sizeof(hints));
	hints.ai_family = get_family_hint ();
	hints.ai_socktype = SOCK_STREAM;
	hints.ai_flags = AI_CANONNAME;

	if( getaddrinfo (hostname, NULL, &hints, &info) == -1 ) {
		return(FALSE);
	}

	return(addrinfo_to_IPHostEntry (info, h_name, h_aliases, h_addr_list));
#else
	if (inet_pton (AF_INET, address, &inaddr) <= 0) {
		g_free (address);
		return(FALSE);
	}
	g_free (address);

	if ((he = gethostbyaddr ((char *) &inaddr, sizeof (inaddr), AF_INET)) == NULL) {
		return(FALSE);
	}

	return(hostent_to_IPHostEntry (he, h_name, h_aliases, h_addr_list));
#endif
}

extern MonoBoolean ves_icall_System_Net_Dns_GetHostName_internal(MonoString **h_name)
{
	guchar hostname[256];
	int ret;
	
	MONO_ARCH_SAVE_REGS;

	ret = gethostname (hostname, sizeof (hostname));
	if(ret==-1) {
		return(FALSE);
	}
	
	*h_name=mono_string_new(mono_domain_get (), hostname);

	return(TRUE);
}


/* Async interface */
#ifndef USE_AIO
void
ves_icall_System_Net_Sockets_Socket_AsyncReceive (MonoSocketAsyncResult *ares, gint *error)
{
	MONO_ARCH_SAVE_REGS;

	*error = ERROR_NOT_SUPPORTED;
}

void
ves_icall_System_Net_Sockets_Socket_AsyncSend (MonoSocketAsyncResult *ares, gint *error)
{
	MONO_ARCH_SAVE_REGS;

	*error = ERROR_NOT_SUPPORTED;
}
#else
static void
wsa_overlapped_callback (guint32 error, guint32 numbytes, gpointer result)
{
	MonoSocketAsyncResult *ares = (MonoSocketAsyncResult *) result;
	MonoThread *thread;
 
	ares->completed = TRUE;
	ares->error = error;
	ares->total = numbytes;

	if (ares->callback != NULL) {
		gpointer p [1];

		*p = ares;
		thread = mono_thread_attach (mono_object_domain (ares));
		mono_runtime_invoke (ares->callback->method_info->method, NULL, p, NULL);

		mono_thread_detach (thread);
	}

	if (ares->wait_handle != NULL)
		SetEvent (ares->wait_handle->handle);
}

void
ves_icall_System_Net_Sockets_Socket_AsyncReceive (MonoSocketAsyncResult *ares, gint *error)
{
	gint32 bytesread;

	MONO_ARCH_SAVE_REGS;

	if (_wapi_socket_async_read (ares->handle,
					mono_array_addr (ares->buffer, gchar, ares->offset),
					ares->size,
					&bytesread,
					ares,
					wsa_overlapped_callback) == FALSE) {
		*error = WSAGetLastError ();
	} else {
		*error = 0;
		ares->completed_synch = TRUE;
		wsa_overlapped_callback (0, bytesread, ares);
	}
}

void
ves_icall_System_Net_Sockets_Socket_AsyncSend (MonoSocketAsyncResult *ares, gint *error)
{
	gint32 byteswritten;

	MONO_ARCH_SAVE_REGS;

	if (_wapi_socket_async_write (ares->handle,
					mono_array_addr (ares->buffer, gchar, ares->offset),
					ares->size,
					&byteswritten,
					ares,
					wsa_overlapped_callback) == FALSE) {
		*error = WSAGetLastError ();
	} else {
		*error = 0;
		ares->completed_synch = TRUE;
		wsa_overlapped_callback (0, byteswritten, ares);
	}
}
#endif /* USE_AIO */

void mono_network_init(void)
{
	WSADATA wsadata;
	int err;
	
	err=WSAStartup(MAKEWORD(2,0), &wsadata);
	if(err!=0) {
		g_error(G_GNUC_PRETTY_FUNCTION ": Couldn't initialise networking");
		exit(-1);
	}

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Using socket library: %s", wsadata.szDescription);
	g_message(G_GNUC_PRETTY_FUNCTION ": Socket system status: %s", wsadata.szSystemStatus);
#endif
}

void mono_network_cleanup(void)
{
	WSACleanup();
}

