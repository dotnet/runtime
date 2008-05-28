/*
 * socket-io.c: Socket IO internal calls
 *
 * Authors:
 *	Dick Porter (dick@ximian.com)
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 * Copyright (c) 2005-2006 Novell, Inc. (http://www.novell.com)
 */

#include <config.h>

#include <glib.h>
#include <string.h>
#include <stdlib.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <errno.h>

#include <mono/metadata/object.h>
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/socket-io.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threads-types.h>
#include <mono/utils/mono-poll.h>
/* FIXME change this code to not mess so much with the internals */
#include <mono/metadata/class-internals.h>
#include <mono/metadata/threadpool-internals.h>
#include <mono/metadata/domain-internals.h>

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

/* Convert MonoSocketFlags */
static gint32 convert_socketflags (gint32 sflags)
{
	gint32 flags = 0;

	if (!sflags)
		/* SocketFlags.None */
		return 0;

	if (sflags & ~(SocketFlags_OutOfBand | SocketFlags_MaxIOVectorLength | SocketFlags_Peek | 
			SocketFlags_DontRoute | SocketFlags_Partial))
		/* Contains invalid flag values */
		return -1;

	if (sflags & SocketFlags_OutOfBand)
		flags |= MSG_OOB;
	if (sflags & SocketFlags_Peek)
		flags |= MSG_PEEK;
	if (sflags & SocketFlags_DontRoute)
		flags |= MSG_DONTROUTE;
	if (sflags & SocketFlags_Partial)
#ifdef MSG_MORE
		flags |= MSG_MORE;
#else
		return -1;	
#endif
	if (sflags & SocketFlags_MaxIOVectorLength)
		/* FIXME: Don't know what to do for MaxIOVectorLength query */
		return -1;	
	
	return (flags ? flags : -1);
}

/*
 * Returns:
 *    0 on success (mapped mono_level and mono_name to system_level and system_name
 *   -1 on error
 *   -2 on non-fatal error (ie, must ignore)
 */
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
#ifdef HAVE_IP_DONTFRAGMENT
			*system_name = IP_DONTFRAGMENT;
			break;
#elif defined HAVE_IP_MTU_DISCOVER
			/* Not quite the same */
			*system_name = IP_MTU_DISCOVER;
			break;
#else
			/* If the flag is not available on this system, we can ignore this error */
			return (-2);
#endif /* HAVE_IP_DONTFRAGMENT */
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
		if (!system_assembly) {	\
			MonoAssembly *sa = mono_assembly_open ("System.dll", NULL);	\
			if (!sa) g_assert_not_reached ();	\
			else {system_assembly = mono_assembly_get_image (sa);}	\
		}	\
	}

static MonoImage *system_assembly=NULL;


#ifdef AF_INET6
static gint32 get_family_hint(void)
{
	MonoDomain *domain = mono_domain_get ();

	if (!domain->inet_family_hint) {
		MonoClass *socket_class;
		MonoClassField *ipv6_field, *ipv4_field;
		gint32 ipv6_enabled = -1, ipv4_enabled = -1;
		MonoVTable *vtable;

		socket_class = mono_class_from_name (system_assembly, "System.Net.Sockets", "Socket");
		ipv4_field = mono_class_get_field_from_name (socket_class, "ipv4Supported");
		ipv6_field = mono_class_get_field_from_name (socket_class, "ipv6Supported");
		vtable = mono_class_vtable (mono_domain_get (), socket_class);
		mono_runtime_class_init (vtable);

		mono_field_static_get_value (vtable, ipv4_field, &ipv4_enabled);
		mono_field_static_get_value (vtable, ipv6_field, &ipv6_enabled);

		mono_domain_lock (domain);
		if (ipv4_enabled == 1 && ipv6_enabled == 1) {
			domain->inet_family_hint = 1;
		} else if (ipv4_enabled == 1) {
			domain->inet_family_hint = 2;
		} else {
			domain->inet_family_hint = 3;
		}
		mono_domain_unlock (domain);
	}
	switch (domain->inet_family_hint) {
	case 1: return PF_UNSPEC;
	case 2: return PF_INET;
	case 3: return PF_INET6;
	default:
		return PF_UNSPEC;
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

	/* Clear any pending work item from this socket if the underlying
	 * polling system does not notify when the socket is closed */
	mono_thread_pool_remove_socket (GPOINTER_TO_INT (sock));
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

	/*
	 * block == TRUE/FALSE means we will block/not block.
	 * But the ioctlsocket call takes TRUE/FALSE for non-block/block
	 */
	block = !block;
	
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
#ifdef HAVE_SYS_UN_H
	if (saddr->sa_family == AF_UNIX) {
		/* sa_len includes the entire sockaddr size, so we don't need the
		 * N bytes (sizeof (unsigned short)) of the family. */
		data=mono_array_new(domain, mono_get_byte_class (), sa_size);
	} else
#endif
	{
		/* May be the +2 here is too conservative, as sa_len returns
		 * the length of the entire sockaddr_in/in6, including
		 * sizeof (unsigned short) of the family */
		data=mono_array_new(domain, mono_get_byte_class (), sa_size+2);
	}

	/* The data buffer is laid out as follows:
	 * bytes 0 and 1 are the address family
	 * bytes 2 and 3 are the port info
	 * the rest is the address info
	 */
		
	family=convert_to_mono_family(saddr->sa_family);
	if(family==AddressFamily_Unknown) {
		*error = WSAEAFNOSUPPORT;
		return(NULL);
	}

	mono_array_set(data, guint8, 0, family & 0x0FF);
	mono_array_set(data, guint8, 1, (family >> 8) & 0x0FF);
	
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
	socklen_t salen;
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
	socklen_t salen;
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
						    socklen_t *sa_size,
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
	if (family == AF_INET) {
		struct sockaddr_in *sa;
		guint16 port;
		guint32 address;
		
		if (len < 8) {
			mono_raise_exception (mono_exception_from_name (mono_get_corlib (), "System", "SystemException"));
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

		*sa_size = sizeof(struct sockaddr_in);
		return((struct sockaddr *)sa);

#ifdef AF_INET6
	} else if (family == AF_INET6) {
		struct sockaddr_in6 *sa;
		int i;
		guint16 port;
		guint32 scopeid;
		
		if (len < 28) {
			mono_raise_exception (mono_exception_from_name (mono_get_corlib (), "System", "SystemException"));
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

		for(i=0; i<16; i++) {
			sa->sin6_addr.s6_addr[i] = mono_array_get (data, guint8, 8+i);
		}

		*sa_size = sizeof(struct sockaddr_in6);
		return((struct sockaddr *)sa);
#endif
#ifdef HAVE_SYS_UN_H
	} else if (family == AF_UNIX) {
		struct sockaddr_un *sock_un;
		int i;

		/* Need a byte for the '\0' terminator/prefix, and the first
		 * two bytes hold the SocketAddress family
		 */
		if (len - 2 >= MONO_SIZEOF_SUNPATH) {
			mono_raise_exception (mono_get_exception_index_out_of_range ());
		}
		
		sock_un = g_new0 (struct sockaddr_un, 1);

		sock_un->sun_family = family;
		for (i = 0; i < len - 2; i++) {
			sock_un->sun_path [i] = mono_array_get (data, guint8,
								i + 2);
		}
		
		*sa_size = len;

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
	socklen_t sa_size;
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
	MonoThread *thread = NULL;
	mono_pollfd *pfds;
	int ret;
	time_t start;
	

	MONO_ARCH_SAVE_REGS;
	
	pfds = g_new0 (mono_pollfd, 1);
	pfds[0].fd = GPOINTER_TO_INT (sock);
	pfds[0].events = (mode == SelectModeRead) ? MONO_POLLIN :
		(mode == SelectModeWrite) ? MONO_POLLOUT :
		(MONO_POLLERR | MONO_POLLHUP | MONO_POLLNVAL);

	timeout = (timeout >= 0) ? (timeout / 1000) : -1;
	start = time (NULL);
	do {
		*error = 0;
		
		ret = mono_poll (pfds, 1, timeout);
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
			int leave = 0;

			if (thread == NULL) {
				thread = mono_thread_current ();
			}
			
			leave = mono_thread_test_state (thread, ThreadState_AbortRequested | ThreadState_StopRequested);
			
			if (leave != 0) {
				g_free (pfds);
				return(FALSE);
			} else {
				/* Suspend requested? */
				mono_thread_interruption_checkpoint ();
			}
			errno = EINTR;
		}
	} while (ret == -1 && errno == EINTR);

	if (ret == -1) {
#ifdef PLATFORM_WIN32
		*error = WSAGetLastError ();
#else
		*error = errno_to_WSA (errno, __func__);
#endif
		g_free (pfds);
		return(FALSE);
	}
	
	g_free (pfds);

	if (ret == 0) {
		return(FALSE);
	} else {
		return (TRUE);
	}
}

extern void ves_icall_System_Net_Sockets_Socket_Connect_internal(SOCKET sock, MonoObject *sockaddr, gint32 *error)
{
	struct sockaddr *sa;
	socklen_t sa_size;
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

/* These #defines from mswsock.h from wine.  Defining them here allows
 * us to build this file on a mingw box that doesn't know the magic
 * numbers, but still run on a newer windows box that does.
 */
#ifndef WSAID_DISCONNECTEX
#define WSAID_DISCONNECTEX {0x7fda2e11,0x8630,0x436f,{0xa0, 0x31, 0xf5, 0x36, 0xa6, 0xee, 0xc1, 0x57}}
typedef BOOL (WINAPI *LPFN_DISCONNECTEX)(SOCKET, LPOVERLAPPED, DWORD, DWORD);
#endif

#ifndef WSAID_TRANSMITFILE
#define WSAID_TRANSMITFILE {0xb5367df0,0xcbac,0x11cf,{0x95,0xca,0x00,0x80,0x5f,0x48,0xa1,0x92}}
typedef BOOL (WINAPI *LPFN_TRANSMITFILE)(SOCKET, HANDLE, DWORD, DWORD, LPOVERLAPPED, LPTRANSMIT_FILE_BUFFERS, DWORD);
#endif

extern void ves_icall_System_Net_Sockets_Socket_Disconnect_internal(SOCKET sock, MonoBoolean reuse, gint32 *error)
{
	int ret;
	glong output_bytes = 0;
	GUID disco_guid = WSAID_DISCONNECTEX;
	GUID trans_guid = WSAID_TRANSMITFILE;
	LPFN_DISCONNECTEX _wapi_disconnectex = NULL;
	LPFN_TRANSMITFILE _wapi_transmitfile = NULL;
	gboolean bret;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
#ifdef DEBUG
	g_message("%s: disconnecting from socket %p (reuse %d)", __func__,
		  sock, reuse);
#endif

	/* I _think_ the extension function pointers need to be looked
	 * up for each socket.  FIXME: check the best way to store
	 * pointers to functions in managed objects that still works
	 * on 64bit platforms.
	 */
	ret = WSAIoctl (sock, SIO_GET_EXTENSION_FUNCTION_POINTER,
			(void *)&disco_guid, sizeof(GUID),
			(void *)&_wapi_disconnectex, sizeof(void *),
			&output_bytes, NULL, NULL);
	if (ret != 0) {
		/* make sure that WSAIoctl didn't put crap in the
		 * output pointer
		 */
		_wapi_disconnectex = NULL;

		/* Look up the TransmitFile extension function pointer
		 * instead of calling TransmitFile() directly, because
		 * apparently "Several of the extension functions have
		 * been available since WinSock 1.1 and are exported
		 * from MSWsock.dll, however it's not advisable to
		 * link directly to this dll as this ties you to the
		 * Microsoft WinSock provider. A provider neutral way
		 * of accessing these extension functions is to load
		 * them dynamically via WSAIoctl using the
		 * SIO_GET_EXTENSION_FUNCTION_POINTER op code. This
		 * should, theoretically, allow you to access these
		 * functions from any provider that supports them..." 
		 * (http://www.codeproject.com/internet/jbsocketserver3.asp)
		 */
		ret = WSAIoctl (sock, SIO_GET_EXTENSION_FUNCTION_POINTER,
				(void *)&trans_guid, sizeof(GUID),
				(void *)&_wapi_transmitfile, sizeof(void *),
				&output_bytes, NULL, NULL);
		if (ret != 0) {
			_wapi_transmitfile = NULL;
		}
	}

	if (_wapi_disconnectex != NULL) {
		bret = _wapi_disconnectex (sock, NULL, TF_REUSE_SOCKET, 0);
	} else if (_wapi_transmitfile != NULL) {
		bret = _wapi_transmitfile (sock, NULL, 0, 0, NULL, NULL,
					   TF_DISCONNECT | TF_REUSE_SOCKET);
	} else {
		*error = ERROR_NOT_SUPPORTED;
		return;
	}

	if (bret == FALSE) {
		*error = WSAGetLastError ();
	}
}

gint32 ves_icall_System_Net_Sockets_Socket_Receive_internal(SOCKET sock, MonoArray *buffer, gint32 offset, gint32 count, gint32 flags, gint32 *error)
{
	int ret;
	guchar *buf;
	gint32 alen;
	int recvflags=0;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	alen = mono_array_length (buffer);
	if (offset > alen - count) {
		return(0);
	}
	
	buf=mono_array_addr(buffer, guchar, offset);
	
	recvflags = convert_socketflags (flags);
	if (recvflags == -1) {
		*error = WSAEOPNOTSUPP;
		return (0);
	}
		
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
	socklen_t sa_size;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	alen = mono_array_length (buffer);
	if (offset > alen - count) {
		return(0);
	}

	sa=create_sockaddr_from_object(*sockaddr, &sa_size, error);
	if (*error != 0) {
		return(0);
	}
	
	buf=mono_array_addr(buffer, guchar, offset);
	
	recvflags = convert_socketflags (flags);
	if (recvflags == -1) {
		*error = WSAEOPNOTSUPP;
		return (0);
	}

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
	
	alen = mono_array_length (buffer);
	if (offset > alen - count) {
		return(0);
	}

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": alen: %d", alen);
#endif
	
	buf=mono_array_addr(buffer, guchar, offset);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Sending %d bytes", count);
#endif

	sendflags = convert_socketflags (flags);
	if (sendflags == -1) {
		*error = WSAEOPNOTSUPP;
		return (0);
	}

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
	socklen_t sa_size;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	alen = mono_array_length (buffer);
	if (offset > alen - count) {
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

	sendflags = convert_socketflags (flags);
	if (sendflags == -1) {
		*error = WSAEOPNOTSUPP;
		return (0);
	}

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

#define POLL_ERRORS (MONO_POLLERR | MONO_POLLHUP | MONO_POLLNVAL)
void ves_icall_System_Net_Sockets_Socket_Select_internal(MonoArray **sockets, gint32 timeout, gint32 *error)
{
	MonoThread *thread = NULL;
	MonoObject *obj;
	mono_pollfd *pfds;
	int nfds, idx;
	int ret;
	int i, count;
	int mode;
	MonoClass *sock_arr_class;
	MonoArray *socks;
	time_t start;
	mono_array_size_t socks_size;
	
	MONO_ARCH_SAVE_REGS;

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
			*error = WSAEFAULT;
			return;
		}

		pfds [idx].fd = GPOINTER_TO_INT (Socket_to_SOCKET (obj));
		pfds [idx].events = (mode == 0) ? MONO_POLLIN : (mode == 1) ? MONO_POLLOUT : POLL_ERRORS;
		idx++;
	}

	timeout = (timeout >= 0) ? (timeout / 1000) : -1;
	start = time (NULL);
	do {
		*error = 0;
		ret = mono_poll (pfds, nfds, timeout);
		if (timeout > 0 && ret < 0) {
			int err = errno;
			int sec = time (NULL) - start;

			timeout -= sec * 1000;
			if (timeout < 0)
				timeout = 0;
			errno = err;
		}

		if (ret == -1 && errno == EINTR) {
			int leave = 0;
			if (thread == NULL)
				thread = mono_thread_current ();

			leave = mono_thread_test_state (thread, ThreadState_AbortRequested | ThreadState_StopRequested);
			
			if (leave != 0) {
				g_free (pfds);
				*sockets = NULL;
				return;
			} else {
				/* Suspend requested? */
				mono_thread_interruption_checkpoint ();
			}
			errno = EINTR;
		}
	} while (ret == -1 && errno == EINTR);
	
	if (ret == -1) {
#ifdef PLATFORM_WIN32
		*error = WSAGetLastError ();
#else
		*error = errno_to_WSA (errno, __func__);
#endif
		g_free (pfds);
		return;
	}

	if (ret == 0) {
		g_free (pfds);
		*sockets = NULL;
		return;
	}

	sock_arr_class= ((MonoObject *)*sockets)->vtable->klass;
	socks_size = ((mono_array_size_t)ret) + 3; /* space for the NULL delimiters */
	socks = mono_array_new_full (mono_domain_get (), sock_arr_class, &socks_size, NULL);

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
	socklen_t valsize=sizeof(val);
	struct linger linger;
	socklen_t lingersize=sizeof(linger);
	int time_ms = 0;
	socklen_t time_ms_size = sizeof (time_ms);
#ifdef SO_PEERCRED
	struct ucred cred;
	socklen_t credsize = sizeof(cred);
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
	if (ret == -2) {
		*obj_val = int_to_object (domain, 0);
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
		ret = _wapi_getsockopt (sock, system_level, system_name, (char *) &time_ms, &time_ms_size);
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
		obj = int_to_object (domain, time_ms);
		break;

#ifdef SO_PEERCRED
	case SocketOptionName_PeerCred: 
	{
		/* build a Mono.Posix.PeerCred+PeerCredData if
		 * possible
		 */
		static MonoImage *mono_posix_image = NULL;
		MonoPeerCredData *cred_data;
		
		if (mono_posix_image == NULL) {
			mono_posix_image=mono_image_loaded ("Mono.Posix");
			if (!mono_posix_image) {
				MonoAssembly *sa = mono_assembly_open ("Mono.Posix.dll", NULL);
				if (!sa) {
					*error = WSAENOPROTOOPT;
					return;
				} else {
					mono_posix_image = mono_assembly_get_image (sa);
				}
			}
		}
		
		obj_class = mono_class_from_name(mono_posix_image,
						 "Mono.Posix",
						 "PeerCredData");
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
	socklen_t valsize;
	
	MONO_ARCH_SAVE_REGS;

	*error = 0;
	
	ret=convert_sockopt_level_and_name(level, name, &system_level,
					   &system_name);
	if(ret==-1) {
		*error = WSAENOPROTOOPT;
		return;
	}
	if(ret==-2)
		return;

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
	
	field=mono_class_get_field_from_name(ipaddr->vtable->klass, "m_Address");

	/* No idea why .net uses a 64bit type to hold a 32bit value...
	 *
	 * Internal value of IPAddess is in little-endian order
	 */
	inaddr.s_addr=GUINT_FROM_LE ((guint32)*(guint64 *)(((char *)ipaddr)+field->offset));
	
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

	field=mono_class_get_field_from_name(ipaddr->vtable->klass, "m_Numbers");
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
	if(ret==-2){
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
		/* ReceiveTimeout/SendTimeout get here */
		switch(name) {
		case SocketOptionName_DontFragment:
#ifdef HAVE_IP_MTU_DISCOVER
			/* Fiddle with the value slightly if we're
			 * turning DF on
			 */
			if (int_val == 1) {
				int_val = IP_PMTUDISC_DO;
			}
			/* Fall through */
#endif
			
		default:
			ret = _wapi_setsockopt (sock, system_level, system_name, (char *) &int_val, sizeof (int_val));
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
	glong output_bytes = 0;
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

#ifdef HAVE_SIOCGIFCONF
static gboolean
is_loopback (int family, void *ad)
{
	char *ptr = (char *) ad;

	if (family == AF_INET) {
		return (ptr [0] == 127);
	}
#ifdef AF_INET6
	else {
		return (IN6_IS_ADDR_LOOPBACK ((struct in6_addr *) ptr));
	}
#endif
	return FALSE;
}

static void *
get_local_ips (int family, int *nips)
{
	int addr_size, offset, fd, i, count;
	int max_ifaces = 50; /* 50 interfaces should be enough... */
	struct ifconf ifc;
	struct ifreq *ifr;
	struct ifreq iflags;
	char *result, *tmp_ptr;
	gboolean ignore_loopback = FALSE;

	*nips = 0;
	if (family == AF_INET) {
		addr_size = sizeof (struct in_addr);
		offset = G_STRUCT_OFFSET (struct sockaddr_in, sin_addr);
#ifdef AF_INET6
	} else if (family == AF_INET6) {
		addr_size = sizeof (struct in6_addr);
		offset = G_STRUCT_OFFSET (struct sockaddr_in6, sin6_addr);
#endif
	} else {
		return NULL;
	}

	fd = socket (family, SOCK_STREAM, 0);

	ifc.ifc_len = max_ifaces * sizeof (struct ifreq);
	ifc.ifc_buf = g_malloc (ifc.ifc_len);
	if (ioctl (fd, SIOCGIFCONF, &ifc) < 0) {
		close (fd);
		g_free (ifc.ifc_buf);
		return NULL;
	}

	count = ifc.ifc_len / sizeof (struct ifreq);
	*nips = count;
	if (count == 0) {
		g_free (ifc.ifc_buf);
		close (fd);
		return NULL;
	}

	for (i = 0, ifr = ifc.ifc_req; i < *nips; i++, ifr++) {
		strcpy (iflags.ifr_name, ifr->ifr_name);
		if (ioctl (fd, SIOCGIFFLAGS, &iflags) < 0) {
			continue;
		}

		if ((iflags.ifr_flags & IFF_UP) == 0) {
			ifr->ifr_name [0] = '\0';
			continue;
		}

		if ((iflags.ifr_flags & IFF_LOOPBACK) == 0) {
			ignore_loopback = TRUE;
		}
	}

	close (fd);
	result = g_malloc (addr_size * count);
	tmp_ptr = result;
	for (i = 0, ifr = ifc.ifc_req; i < count; i++, ifr++) {
		if (ifr->ifr_name [0] == '\0') {
			(*nips)--;
			continue;
		}

		if (ignore_loopback && is_loopback (family, ((char *) &ifr->ifr_addr) + offset)) {
			(*nips)--;
			continue;
		}

		memcpy (tmp_ptr, ((char *) &ifr->ifr_addr) + offset, addr_size);
		tmp_ptr += addr_size;
	}

	g_free (ifc.ifc_buf);
	return result;
}
#else
static void *
get_local_ips (int family, int *nips)
{
	*nips = 0;
	return NULL;
}

#endif /* HAVE_SIOCGIFCONF */

#ifndef AF_INET6
static gboolean hostent_to_IPHostEntry(struct hostent *he, MonoString **h_name,
				       MonoArray **h_aliases,
				       MonoArray **h_addr_list,
				       gboolean add_local_ips)
{
	MonoDomain *domain = mono_domain_get ();
	int i;
	struct in_addr *local_in = NULL;
	int nlocal_in = 0;

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
		mono_array_setref (*h_aliases, i, alias);
		i++;
	}

	if (add_local_ips) {
		local_in = (struct in_addr *) get_local_ips (AF_INET, &nlocal_in);
		if (nlocal_in) {
			*h_addr_list = mono_array_new(domain, mono_get_string_class (), nlocal_in);
			for (i = 0; i < nlocal_in; i++) {
				MonoString *addr_string;
				char addr [16], *ptr;
				
				ptr = (char *) &local_in [i];
				g_snprintf(addr, 16, "%u.%u.%u.%u",
					 (unsigned char) ptr [0],
					 (unsigned char) ptr [1],
					 (unsigned char) ptr [2],
					 (unsigned char) ptr [3]);
				
				addr_string = mono_string_new (domain, addr);
				mono_array_setref (*h_addr_list, i, addr_string);
				i++;
			}

			g_free (local_in);
		}
	}
	
	if (nlocal_in == 0) {
		i = 0;
		while (he->h_addr_list[i]!=NULL) {
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
			mono_array_setref (*h_addr_list, i, addr_string);
			i++;
		}
	}

	return(TRUE);
}

static gboolean ipaddr_to_IPHostEntry(const char *addr, MonoString **h_name,
				      MonoArray **h_aliases,
				      MonoArray **h_addr_list)
{
	MonoDomain *domain = mono_domain_get ();

	*h_name=mono_string_new(domain, addr);
	*h_aliases=mono_array_new(domain, mono_get_string_class (), 0);
	*h_addr_list=mono_array_new(domain, mono_get_string_class (), 1);
	mono_array_setref (*h_addr_list, 0, *h_name);

	return(TRUE);
}
#endif

#if defined(AF_INET6) && defined(HAVE_GETHOSTBYNAME2_R)
static gboolean hostent_to_IPHostEntry2(struct hostent *he1,struct hostent *he2, MonoString **h_name,
				MonoArray **h_aliases, MonoArray **h_addr_list, gboolean add_local_ips)
{
	MonoDomain *domain = mono_domain_get ();
	int i, host_count, host_index, family_hint;
	struct in_addr *local_in = NULL;
	int nlocal_in = 0;
	struct in6_addr *local_in6 = NULL;
	int nlocal_in6 = 0;
	gboolean from_local = FALSE;

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
			mono_array_setref (*h_aliases, i, alias);
			i++;
		}
	} else if (he2 != NULL && (family_hint == PF_UNSPEC ||
				   family_hint == PF_INET6)) {
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
			mono_array_setref (*h_aliases, i, alias);
			i++;
		}
	} else {
		return(FALSE);
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
	host_index = 0;
	if (add_local_ips) {
		if (family_hint == PF_UNSPEC || family_hint == PF_INET)
			local_in = (struct in_addr *) get_local_ips (AF_INET, &nlocal_in);

		if (family_hint == PF_UNSPEC || family_hint == PF_INET6)
			local_in6 = (struct in6_addr *) get_local_ips (AF_INET6, &nlocal_in6);

		if (nlocal_in || nlocal_in6) {
			from_local = TRUE;
			*h_addr_list = mono_array_new (domain, mono_get_string_class (),
							     nlocal_in + nlocal_in6);

			if (nlocal_in6) {
				int n;
				for (n = 0; n < nlocal_in6; n++) {
					MonoString *addr_string;
					const char *ret;
					char addr[48]; /* INET6_ADDRSTRLEN == 46, but IPv6 addresses can be 48 bytes with the trailing NULL */

					ret = inet_ntop (AF_INET6, &local_in6 [n], addr, sizeof(addr));

					if (ret != NULL) {
						addr_string = mono_string_new (domain, addr);
						mono_array_setref (*h_addr_list, host_index, addr_string);
						host_index++;
					}
				}
			}

			if (nlocal_in) {
				int n;
				for (n = 0; n < nlocal_in; n++) {
					MonoString *addr_string;
					const char *ret;
					char addr[16]; /* INET_ADDRSTRLEN == 16 */

					ret = inet_ntop (AF_INET, &local_in [n], addr, sizeof(addr));

					if (ret != NULL) {
						addr_string = mono_string_new (domain, addr);
						mono_array_setref (*h_addr_list, host_index, addr_string);
						host_index++;
					}
				}
			}
			g_free (local_in);
			g_free (local_in6);
			return TRUE;
		}

		g_free (local_in);
		g_free (local_in6);
	}

	*h_addr_list=mono_array_new (domain, mono_get_string_class (), host_count);

	if (he2 != NULL && (family_hint == PF_UNSPEC ||
			    family_hint == PF_INET6)) {
		i = 0;
		while(he2->h_addr_list[i] != NULL) {
			MonoString *addr_string;
			const char *ret;
			char addr[48]; /* INET6_ADDRSTRLEN == 46, but IPv6 addresses can be 48 bytes long with the trailing NULL */

			ret = inet_ntop (AF_INET6, he2->h_addr_list[i], addr,
					 sizeof(addr));

			if (ret != NULL) {
				addr_string = mono_string_new (domain, addr);
				mono_array_setref (*h_addr_list, host_index, addr_string);
				i++;
				host_index++;
			}
		}
	}

	if (he1 != NULL && (family_hint == PF_UNSPEC ||
			    family_hint == PF_INET)) {
		i=0;
		while(he1->h_addr_list[i] != NULL) {
			MonoString *addr_string;
			const char *ret;
			char addr[16]; /* INET_ADDRSTRLEN == 16 */

			ret = inet_ntop (AF_INET, he1->h_addr_list[i], addr,
					 sizeof(addr));

			if (ret != NULL) {
				addr_string=mono_string_new (domain, addr);
				mono_array_setref (*h_addr_list, host_index, addr_string);
				i++;
				host_index++;
			}
		}
	}

	return(TRUE);
}
#endif

#if defined(AF_INET6)
static gboolean 
addrinfo_to_IPHostEntry(struct addrinfo *info, MonoString **h_name,
						MonoArray **h_aliases,
						MonoArray **h_addr_list,
						gboolean add_local_ips)
{
	gint32 count, i;
	struct addrinfo *ai = NULL;
	struct in_addr *local_in = NULL;
	int nlocal_in = 0;
	struct in6_addr *local_in6 = NULL;
	int nlocal_in6 = 0;
	int addr_index;

	MonoDomain *domain = mono_domain_get ();

	addr_index = 0;
	*h_aliases=mono_array_new(domain, mono_get_string_class (), 0);
	if (add_local_ips) {
		local_in = (struct in_addr *) get_local_ips (AF_INET, &nlocal_in);
		local_in6 = (struct in6_addr *) get_local_ips (AF_INET6, &nlocal_in6);
		if (nlocal_in || nlocal_in6) {
			*h_addr_list=mono_array_new(domain, mono_get_string_class (), nlocal_in + nlocal_in6);
			if (nlocal_in) {
				MonoString *addr_string;
				char addr [16];
				int i;

				for (i = 0; i < nlocal_in; i++) {
					inet_ntop (AF_INET, &local_in [i], addr, sizeof (addr));
					addr_string = mono_string_new (domain, addr);
					mono_array_setref (*h_addr_list, addr_index, addr_string);
					addr_index++;
				}
			}

			if (nlocal_in6) {
				MonoString *addr_string;
				const char *ret;
				char addr [48];
				int i;

				for (i = 0; i < nlocal_in6; i++) {
					ret = inet_ntop (AF_INET6, &local_in6 [i], addr, sizeof (addr));
					if (ret != NULL) {
						addr_string = mono_string_new (domain, addr);
						mono_array_setref (*h_addr_list, addr_index, addr_string);
						addr_index++;
					}
				}
			}

			g_free (local_in);
			g_free (local_in6);
			if (info) {
				freeaddrinfo (info);
			}
			return TRUE;
		}

		g_free (local_in);
		g_free (local_in6);
	}

	for (count=0, ai=info; ai!=NULL; ai=ai->ai_next) {
		if (ai->ai_family != AF_INET && ai->ai_family != AF_INET6)
			continue;

		count++;
	}

	*h_addr_list=mono_array_new(domain, mono_get_string_class (), count);

	for (ai=info, i=0; ai!=NULL; ai=ai->ai_next) {
		MonoString *addr_string;
		const char *ret;
		char buffer [48]; /* Max. size for IPv6 */

		if((ai->ai_family != PF_INET) && (ai->ai_family != PF_INET6)) {
			continue;
		}

		if(ai->ai_family == PF_INET) {
			ret = inet_ntop(ai->ai_family, (void*)&(((struct sockaddr_in*)ai->ai_addr)->sin_addr), buffer, 16);
		} else {
			ret = inet_ntop(ai->ai_family, (void*)&(((struct sockaddr_in6*)ai->ai_addr)->sin6_addr), buffer, 48);
		}

		if(ret) {
			addr_string=mono_string_new(domain, buffer);
		} else {
			addr_string=mono_string_new(domain, "");
		}

		mono_array_setref (*h_addr_list, addr_index, addr_string);

		if(!i && ai->ai_canonname != NULL) {
			*h_name=mono_string_new(domain, ai->ai_canonname);
		}

		addr_index++;
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
	gboolean add_local_ips = FALSE;
#ifdef HAVE_SIOCGIFCONF
	gchar this_hostname [256];
#endif
#if !defined(HAVE_GETHOSTBYNAME2_R)
	struct addrinfo *info = NULL, hints;
	char *hostname;
	
	MONO_ARCH_SAVE_REGS;
	
	hostname=mono_string_to_utf8 (host);
#ifdef HAVE_SIOCGIFCONF
	if (gethostname (this_hostname, sizeof (this_hostname)) != -1) {
		if (!strcmp (hostname, this_hostname))
			add_local_ips = TRUE;
	}
#endif

	memset(&hints, 0, sizeof(hints));
	hints.ai_family = get_family_hint ();
	hints.ai_socktype = SOCK_STREAM;
	hints.ai_flags = AI_CANONNAME;

	if (getaddrinfo(hostname, NULL, &hints, &info) == -1) {
		return(FALSE);
	}
	
	g_free(hostname);

	return(addrinfo_to_IPHostEntry(info, h_name, h_aliases, h_addr_list, add_local_ips));
#else
	struct hostent he1,*hp1, he2, *hp2;
	int buffer_size1, buffer_size2;
	char *buffer1, *buffer2;
	int herr;
	gboolean return_value;
	char *hostname;
	
	MONO_ARCH_SAVE_REGS;
	
	hostname=mono_string_to_utf8 (host);

#ifdef HAVE_SIOCGIFCONF
	if (gethostname (this_hostname, sizeof (this_hostname)) != -1) {
		if (!strcmp (hostname, this_hostname))
			add_local_ips = TRUE;
	}
#endif

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
					       h_addr_list, add_local_ips);

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
	gboolean add_local_ips = FALSE;
#ifdef HAVE_SIOCGIFCONF
	gchar this_hostname [256];
#endif
	
	MONO_ARCH_SAVE_REGS;

	hostname=mono_string_to_utf8(host);
#ifdef HAVE_SIOCGIFCONF
	if (gethostname (this_hostname, sizeof (this_hostname)) != -1) {
		if (!strcmp (hostname, this_hostname))
			add_local_ips = TRUE;
	}
#endif

	he = _wapi_gethostbyname (hostname);
	g_free(hostname);

	if(he==NULL) {
		return(FALSE);
	}

	return(hostent_to_IPHostEntry(he, h_name, h_aliases, h_addr_list, add_local_ips));
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
	const char *version;
	gboolean v1;
	
#ifdef AF_INET6
	struct sockaddr_in saddr;
	struct sockaddr_in6 saddr6;
	struct addrinfo *info = NULL, hints;
	gint32 family;
	char hostname[1024] = {0};
	int flags = 0;
#else
	struct in_addr inaddr;
	struct hostent *he;
	gboolean ret;
#endif

	MONO_ARCH_SAVE_REGS;

	version = mono_get_runtime_info ()->framework_version;
	v1 = (version[0] == '1');

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

	if (v1) {
		flags = NI_NAMEREQD;
	}
	
	if(family == AF_INET) {
#if HAVE_SOCKADDR_IN_SIN_LEN
		saddr.sin_len = sizeof (saddr);
#endif
		if(getnameinfo ((struct sockaddr*)&saddr, sizeof(saddr),
				hostname, sizeof(hostname), NULL, 0,
				flags) != 0) {
			return(FALSE);
		}
	} else if(family == AF_INET6) {
#if HAVE_SOCKADDR_IN6_SIN_LEN
		saddr6.sin6_len = sizeof (saddr6);
#endif
		if(getnameinfo ((struct sockaddr*)&saddr6, sizeof(saddr6),
				hostname, sizeof(hostname), NULL, 0,
				flags) != 0) {
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

	return(addrinfo_to_IPHostEntry (info, h_name, h_aliases, h_addr_list, FALSE));
#else
	if (inet_pton (AF_INET, address, &inaddr) <= 0) {
		g_free (address);
		return(FALSE);
	}

	if ((he = gethostbyaddr ((char *) &inaddr, sizeof (inaddr), AF_INET)) == NULL) {
		if (v1) {
			ret = FALSE;
		} else {
			ret = ipaddr_to_IPHostEntry (address, h_name,
						     h_aliases, h_addr_list);
		}
	} else {
		ret = hostent_to_IPHostEntry (he, h_name, h_aliases,
					      h_addr_list, FALSE);
	}

	g_free (address);
	return(ret);
#endif
}

extern MonoBoolean ves_icall_System_Net_Dns_GetHostName_internal(MonoString **h_name)
{
	gchar hostname[256];
	int ret;
	
	MONO_ARCH_SAVE_REGS;

	ret = gethostname (hostname, sizeof (hostname));
	if(ret==-1) {
		return(FALSE);
	}
	
	*h_name=mono_string_new(mono_domain_get (), hostname);

	return(TRUE);
}

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

