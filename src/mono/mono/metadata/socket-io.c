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

#include <mono/metadata/object.h>
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/socket-io.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/appdomain.h>

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
		family=AF_IPX;
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
		family=AF_INET6;
		break;
#ifdef AF_IRDA	
	case AddressFamily_Irda:
		family=AF_IRDA;
		break;
#endif
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
		
	case AF_IPX:
		family=AddressFamily_Ipx;
		break;
		
	case AF_SNA:
		family=AddressFamily_Sna;
		break;
		
	case AF_DECnet:
		family=AddressFamily_DecNet;
		break;
		
	case AF_APPLETALK:
		family=AddressFamily_AppleTalk;
		break;
		
	case AF_INET6:
		family=AddressFamily_InterNetworkV6;
		break;
		
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
		system_assembly=this->vtable->klass->image; \
	}

static MonoImage *system_assembly=NULL;

static MonoException *get_socket_exception(guint32 error_code)
{
	/* Don't cache this exception, because we need the object
	 * constructor to set up the message from the sockets error code.
	 */
	MonoException *ex;
	
	/* This is a bit of a kludge.  The SocketException 0-arg
	 * constructor calls WSAGetLastError() to find the error code
	 * to use.  Until we can init objects with parameters, this
	 * will have to do.
	 */
	WSASetLastError(error_code);
	
	ex=(MonoException *)mono_exception_from_name(system_assembly,
						     "System.Net.Sockets",
						     "SocketException");
	return(ex);
}

gpointer ves_icall_System_Net_Sockets_Socket_Socket_internal(MonoObject *this, gint32 family, gint32 type, gint32 proto)
{
	SOCKET sock;
	gint32 sock_family;
	gint32 sock_proto;
	gint32 sock_type;
	int ret;
	int true=1;
	
	STASH_SYS_ASS(this);
	
	sock_family=convert_family(family);
	if(sock_family==-1) {
		mono_raise_exception(get_socket_exception(WSAEAFNOSUPPORT));
		return(NULL);
	}

	sock_proto=convert_proto(proto);
	if(sock_proto==-1) {
		mono_raise_exception(get_socket_exception(WSAEPROTONOSUPPORT));
		return(NULL);
	}
	
	sock_type=convert_type(type);
	if(sock_type==-1) {
		mono_raise_exception(get_socket_exception(WSAESOCKTNOSUPPORT));
		return(NULL);
	}
	
	sock=socket(sock_family, sock_type, sock_proto);
	if(sock==INVALID_SOCKET) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
		return(NULL);
	}

	/* .net seems to set this by default */
	ret=setsockopt(sock, SOL_SOCKET, SO_REUSEADDR, &true, sizeof(true));
	if(ret==SOCKET_ERROR) {
		closesocket(sock);
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
		return(NULL);
	}
	
	return(GUINT_TO_POINTER (sock));
}

/* FIXME: the SOCKET parameter (here and in other functions in this
 * file) is really an IntPtr which needs to be converted to a guint32.
 */
void ves_icall_System_Net_Sockets_Socket_Close_internal(SOCKET sock)
{
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": closing 0x%x", sock);
#endif

	closesocket(sock);
}

gint32 ves_icall_System_Net_Sockets_SocketException_WSAGetLastError_internal(void)
{
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": returning %d", WSAGetLastError());
#endif

	return(WSAGetLastError());
}

gint32 ves_icall_System_Net_Sockets_Socket_Available_internal(SOCKET sock)
{
	int ret, amount;
	
	ret=ioctlsocket(sock, FIONREAD, &amount);
	if(ret==SOCKET_ERROR) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
		return(0);
	}
	
	return(amount);
}

void ves_icall_System_Net_Sockets_Socket_Blocking_internal(SOCKET sock,
							   gboolean block)
{
	int ret;
	
	ret=ioctlsocket(sock, FIONBIO, &block);
	if(ret==SOCKET_ERROR) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
	}
}

gpointer ves_icall_System_Net_Sockets_Socket_Accept_internal(SOCKET sock)
{
	SOCKET newsock;
	
	newsock=accept(sock, NULL, 0);
	if(newsock==INVALID_SOCKET) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
		return(NULL);
	}
	
	return(GUINT_TO_POINTER (newsock));
}

void ves_icall_System_Net_Sockets_Socket_Listen_internal(SOCKET sock,
							 guint32 backlog)
{
	int ret;
	
	ret=listen(sock, backlog);
	if(ret==SOCKET_ERROR) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
	}
}

static MonoObject *create_object_from_sockaddr(struct sockaddr *saddr,
					       int sa_size)
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
	data=mono_array_new(domain, mono_defaults.byte_class, sa_size+2);

	/* The data buffer is laid out as follows:
	 * byte 0 is the address family
	 * byte 1 is the buffer length
	 * bytes 2 and 3 are the port info
	 * the rest is the address info
	 */
		
	family=convert_to_mono_family(saddr->sa_family);
	if(family==AddressFamily_Unknown) {
		mono_raise_exception(get_socket_exception(WSAEAFNOSUPPORT));
		return(NULL);
	}

	mono_array_set(data, guint8, 0, family);
	mono_array_set(data, guint8, 1, sa_size+2);
	
	if(saddr->sa_family==AF_INET) {
		struct sockaddr_in *sa_in=(struct sockaddr_in *)saddr;
		guint16 port=ntohs(sa_in->sin_port);
		guint32 address=ntohl(sa_in->sin_addr.s_addr);
		
		if(sa_size<8) {
			mono_raise_exception((MonoException *)mono_exception_from_name(mono_defaults.corlib, "System", "SystemException"));
		}
		
		mono_array_set(data, guint8, 2, (port>>8) & 0xff);
		mono_array_set(data, guint8, 3, (port) & 0xff);
		mono_array_set(data, guint8, 4, (address>>24) & 0xff);
		mono_array_set(data, guint8, 5, (address>>16) & 0xff);
		mono_array_set(data, guint8, 6, (address>>8) & 0xff);
		mono_array_set(data, guint8, 7, (address) & 0xff);
		
		*(MonoArray **)(((char *)sockaddr_obj) + field->offset)=data;

		return(sockaddr_obj);
	} else {
		mono_raise_exception(get_socket_exception(WSAEAFNOSUPPORT));
		return(NULL);
	}
}

extern MonoObject *ves_icall_System_Net_Sockets_Socket_LocalEndPoint_internal(SOCKET sock)
{
	struct sockaddr sa;
	int salen;
	int ret;
	
	salen=sizeof(struct sockaddr);
	ret=getsockname(sock, &sa, &salen);
	
	if(ret==SOCKET_ERROR) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": bound to %s port %d", inet_ntoa(((struct sockaddr_in *)&sa)->sin_addr), ntohs(((struct sockaddr_in *)&sa)->sin_port));
#endif

	return(create_object_from_sockaddr(&sa, salen));
}

extern MonoObject *ves_icall_System_Net_Sockets_Socket_RemoteEndPoint_internal(SOCKET sock)
{
	struct sockaddr sa;
	int salen;
	int ret;
	
	salen=sizeof(struct sockaddr);
	ret=getpeername(sock, &sa, &salen);
	
	if(ret==SOCKET_ERROR) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": connected to %s port %d", inet_ntoa(((struct sockaddr_in *)&sa)->sin_addr), ntohs(((struct sockaddr_in *)&sa)->sin_port));
#endif

	return(create_object_from_sockaddr(&sa, salen));
}

static struct sockaddr *create_sockaddr_from_object(MonoObject *saddr_obj,
						    int *sa_size)
{
	MonoClassField *field;
	MonoArray *data;
	gint32 family;
	int len;

	/* Dig the SocketAddress data buffer out of the object */
	field=mono_class_get_field_from_name(saddr_obj->vtable->klass, "data");
	data=*(MonoArray **)(((char *)saddr_obj) + field->offset);

	/* The data buffer is laid out as follows:
	 * byte 0 is the address family
	 * byte 1 is the buffer length
	 * bytes 2 and 3 are the port info
	 * the rest is the address info
	 */
	len=mono_array_get(data, guint8, 1);
	if((len<2) || (mono_array_length(data)!=len)) {
		mono_raise_exception((MonoException *)mono_exception_from_name(mono_defaults.corlib, "System", "SystemException"));
	}
	
	family=convert_family(mono_array_get(data, guint8, 0));
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
	} else {
		mono_raise_exception(get_socket_exception(WSAEAFNOSUPPORT));
		return(0);
	}
}

extern void ves_icall_System_Net_Sockets_Socket_Bind_internal(SOCKET sock, MonoObject *sockaddr)
{
	struct sockaddr *sa;
	int sa_size;
	int ret;
	
	sa=create_sockaddr_from_object(sockaddr, &sa_size);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": binding to %s port %d", inet_ntoa(((struct sockaddr_in *)&sa)->sin_addr), ntohs (((struct sockaddr_in *)&sa)->sin_port));
#endif

	ret=bind(sock, sa, sa_size);
	g_free(sa);
	
	if(ret==SOCKET_ERROR) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
	}
}

extern void ves_icall_System_Net_Sockets_Socket_Connect_internal(SOCKET sock, MonoObject *sockaddr)
{
	struct sockaddr *sa;
	int sa_size;
	int ret;
	
	sa=create_sockaddr_from_object(sockaddr, &sa_size);
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": connecting to %s port %d", inet_ntoa(((struct sockaddr_in *)&sa)->sin_addr), ntohs (((struct sockaddr_in *)&sa)->sin_port));
#endif

	ret=connect(sock, sa, sa_size);
	g_free(sa);
	
	if(ret==SOCKET_ERROR) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
	}
}

gint32 ves_icall_System_Net_Sockets_Socket_Receive_internal(SOCKET sock, MonoArray *buffer, gint32 offset, gint32 count, gint32 flags)
{
	int ret;
	guchar *buf;
	gint32 alen;
	int recvflags=0;
	
	alen=mono_array_length(buffer);
	if(offset+count>alen) {
		return(0);
	}
	
	buf=mono_array_addr(buffer, guchar, offset);
	
	ret=recv(sock, buf, count, recvflags);
	if(ret==SOCKET_ERROR) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
	}
	
	return(ret);
}

gint32 ves_icall_System_Net_Sockets_Socket_RecvFrom_internal(SOCKET sock, MonoArray *buffer, gint32 offset, gint32 count, gint32 flags, MonoObject **sockaddr)
{
	int ret;
	guchar *buf;
	gint32 alen;
	int recvflags=0;
	struct sockaddr *sa;
	int sa_size;
	
	alen=mono_array_length(buffer);
	if(offset+count>alen) {
		return(0);
	}

	sa=create_sockaddr_from_object(*sockaddr, &sa_size);
	
	buf=mono_array_addr(buffer, guchar, offset);
	
	ret=recvfrom(sock, buf, count, recvflags, sa, &sa_size);
	
	if(ret==SOCKET_ERROR) {
		g_free(sa);
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
	}

	*sockaddr=create_object_from_sockaddr(sa, sa_size);
	g_free(sa);
	
	return(ret);
}

gint32 ves_icall_System_Net_Sockets_Socket_Send_internal(SOCKET sock, MonoArray *buffer, gint32 offset, gint32 count, gint32 flags)
{
	int ret;
	guchar *buf;
	gint32 alen;
	int sendflags=0;
	
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

	ret=send(sock, buf, count, sendflags);
	if(ret==SOCKET_ERROR) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
	}
	
	return(ret);
}

gint32 ves_icall_System_Net_Sockets_Socket_SendTo_internal(SOCKET sock, MonoArray *buffer, gint32 offset, gint32 count, gint32 flags, MonoObject *sockaddr)
{
	int ret;
	guchar *buf;
	gint32 alen;
	int sendflags=0;
	struct sockaddr *sa;
	int sa_size;
	
	alen=mono_array_length(buffer);
	if(offset+count>alen) {
		return(0);
	}

	sa=create_sockaddr_from_object(sockaddr, &sa_size);
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": alen: %d", alen);
#endif
	
	buf=mono_array_addr(buffer, guchar, offset);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Sending %d bytes", count);
#endif

	ret=sendto(sock, buf, count, sendflags, sa, sa_size);
	g_free(sa);
	
	if(ret==SOCKET_ERROR) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
	}
	
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

void ves_icall_System_Net_Sockets_Socket_Select_internal(MonoArray **read_socks, MonoArray **write_socks, MonoArray **err_socks, gint32 timeout)
{
	fd_set readfds, writefds, errfds;
	struct timeval tv;
	div_t divvy;
	int ret;
	int readarrsize, writearrsize, errarrsize;
	MonoDomain *domain=mono_domain_get();
	MonoClass *sock_arr_class;
	MonoArray *socks;
	int count;
	int i;
	
	readarrsize=mono_array_length(*read_socks);
	if(readarrsize>FD_SETSIZE) {
		mono_raise_exception(get_socket_exception(WSAEFAULT));
		return;
	}
	
	FD_ZERO(&readfds);
	for(i=0; i<readarrsize; i++) {
		FD_SET(Socket_to_SOCKET(mono_array_get(*read_socks, MonoObject *, i)), &readfds);
	}
	
	writearrsize=mono_array_length(*write_socks);
	if(writearrsize>FD_SETSIZE) {
		mono_raise_exception(get_socket_exception(WSAEFAULT));
		return;
	}
	
	FD_ZERO(&writefds);
	for(i=0; i<writearrsize; i++) {
		FD_SET(Socket_to_SOCKET(mono_array_get(*write_socks, MonoObject *, i)), &writefds);
	}
	
	errarrsize=mono_array_length(*err_socks);
	if(errarrsize>FD_SETSIZE) {
		mono_raise_exception(get_socket_exception(WSAEFAULT));
		return;
	}
	
	FD_ZERO(&errfds);
	for(i=0; i<errarrsize; i++) {
		FD_SET(Socket_to_SOCKET(mono_array_get(*err_socks, MonoObject *, i)), &errfds);
	}

	/* Negative timeout meaning block until ready is only
	 * specified in Poll, not Select
	 */
	if(timeout>=0) {
		divvy=div(timeout, 1000000);
		tv.tv_sec=divvy.quot;
		tv.tv_usec=divvy.rem*1000000;
	
		ret=select(0, &readfds, &writefds, &errfds, &tv);
	} else {
		ret=select(0, &readfds, &writefds, &errfds, NULL);
	}
	
	if(ret==SOCKET_ERROR) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
		return;
	}

	sock_arr_class=((MonoObject *)*read_socks)->vtable->klass;
	
	count=0;
	for(i=0; i<readarrsize; i++) {
		if(FD_ISSET(Socket_to_SOCKET(mono_array_get(*read_socks, MonoObject *, i)), &readfds)) {
			count++;
		}
	}
	socks=mono_array_new_full(domain, sock_arr_class, &count, NULL);
	count=0;
	for(i=0; i<readarrsize; i++) {
		MonoObject *sock=mono_array_get(*read_socks, MonoObject *, i);
		
		if(FD_ISSET(Socket_to_SOCKET(sock), &readfds)) {
			mono_array_set(socks, MonoObject *, count, sock);
			count++;
		}
	}
	*read_socks=socks;

	count=0;
	for(i=0; i<writearrsize; i++) {
		if(FD_ISSET(Socket_to_SOCKET(mono_array_get(*write_socks, MonoObject *, i)), &writefds)) {
			count++;
		}
	}
	socks=mono_array_new_full(domain, sock_arr_class, &count, NULL);
	count=0;
	for(i=0; i<writearrsize; i++) {
		MonoObject *sock=mono_array_get(*write_socks, MonoObject *, i);
		
		if(FD_ISSET(Socket_to_SOCKET(sock), &writefds)) {
			mono_array_set(socks, MonoObject *, count, sock);
			count++;
		}
	}
	*write_socks=socks;

	count=0;
	for(i=0; i<errarrsize; i++) {
		if(FD_ISSET(Socket_to_SOCKET(mono_array_get(*err_socks, MonoObject *, i)), &errfds)) {
			count++;
		}
	}
	socks=mono_array_new_full(domain, sock_arr_class, &count, NULL);
	count=0;
	for(i=0; i<errarrsize; i++) {
		MonoObject *sock=mono_array_get(*err_socks, MonoObject *, i);
		
		if(FD_ISSET(Socket_to_SOCKET(sock), &errfds)) {
			mono_array_set(socks, MonoObject *, count, sock);
			count++;
		}
	}
	*err_socks=socks;
}

void ves_icall_System_Net_Sockets_Socket_GetSocketOption_obj_internal(SOCKET sock, gint32 level, gint32 name, MonoObject **obj_val)
{
	int system_level;
	int system_name;
	int ret;
	int val;
	int valsize=sizeof(val);
	struct linger linger;
	int lingersize=sizeof(linger);
	MonoDomain *domain=mono_domain_get();
	MonoObject *obj;
	MonoClass *obj_class;
	MonoClassField *field;
	
	ret=convert_sockopt_level_and_name(level, name, &system_level,
					   &system_name);
	if(ret==-1) {
		mono_raise_exception(get_socket_exception(WSAENOPROTOOPT));
		return;
	}
	
	/* No need to deal with MulticastOption names here, because
	 * you cant getsockopt AddMembership or DropMembership (the
	 * int getsockopt will error, causing an exception)
	 */
	switch(name) {
	case SocketOptionName_Linger:
	case SocketOptionName_DontLinger:
		ret=getsockopt(sock, system_level, system_name, &linger,
			       &lingersize);
		break;
		
	default:
		ret=getsockopt(sock, system_level, system_name, &val,
			       &valsize);
	}
	
	if(ret==SOCKET_ERROR) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
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
		val=!linger.l_onoff;
		
		/* fall through */
		
	default:
		/* construct an Int32 object to hold val */
		obj=mono_object_new(domain, mono_defaults.int32_class);
		
		/* Locate and set the "value" field */
		field=mono_class_get_field_from_name(mono_defaults.int32_class,
						     "value");
		*(gint32 *)(((char *)obj)+field->offset)=val;
	}

	*obj_val=obj;
}

void ves_icall_System_Net_Sockets_Socket_GetSocketOption_arr_internal(SOCKET sock, gint32 level, gint32 name, MonoArray **byte_val)
{
	int system_level;
	int system_name;
	int ret;
	guchar *buf;
	int valsize;
	
	ret=convert_sockopt_level_and_name(level, name, &system_level,
					   &system_name);
	if(ret==-1) {
		mono_raise_exception(get_socket_exception(WSAENOPROTOOPT));
		return;
	}

	valsize=mono_array_length(*byte_val);
	buf=mono_array_addr(*byte_val, guchar, 0);
	
	ret=getsockopt(sock, system_level, system_name, buf, &valsize);
	if(ret==SOCKET_ERROR) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
	}
}

static struct in_addr ipaddress_to_struct_in_addr(MonoObject *ipaddr)
{
	struct in_addr inaddr;
	guint64 addr;
	MonoClassField *field;
	
	field=mono_class_get_field_from_name(ipaddr->vtable->klass, "address");
	addr=*(guint64 *)(((char *)ipaddr)+field->offset);

	/* No idea why .net uses a 64bit type to hold a 32bit value */
	inaddr.s_addr=htonl((guint32)addr);
	
	return(inaddr);
}

void ves_icall_System_Net_Sockets_Socket_SetSocketOption_internal(SOCKET sock, gint32 level, gint32 name, MonoObject *obj_val, MonoArray *byte_val, gint32 int_val)
{
	int system_level;
	int system_name;
	int ret;

	ret=convert_sockopt_level_and_name(level, name, &system_level,
					   &system_name);
	if(ret==-1) {
		mono_raise_exception(get_socket_exception(WSAENOPROTOOPT));
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
			ret=setsockopt(sock, system_level, system_name,
				       &linger, valsize);
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
			ret=setsockopt(sock, system_level, system_name,
				       &linger, valsize);
			break;
		case SocketOptionName_AddMembership:
		case SocketOptionName_DropMembership:
		{
#ifdef HAVE_STRUCT_IP_MREQN
			struct ip_mreqn mreq;
#else
			struct ip_mreq mreq;
#endif /* HAVE_STRUCT_IP_MREQN */
			
			/* pain! MulticastOption holds two IPAddress
			 * members, so I have to dig the value out of
			 * those :-(
			 */
			field = mono_class_get_field_from_name (obj_val->vtable->klass, "group");
			mreq.imr_multiaddr = ipaddress_to_struct_in_addr (*(gpointer *)(((char *)obj_val) +
											field->offset));
			field = mono_class_get_field_from_name (obj_val->vtable->klass, "local");
#ifdef HAVE_STRUCT_IP_MREQN
			mreq.imr_address = ipaddress_to_struct_in_addr (*(gpointer *)(((char *)obj_val) +
										      field->offset));
#else
			mreq.imr_interface = ipaddress_to_struct_in_addr (*(gpointer *)(((char *)obj_val) +
											field->offset));
#endif /* HAVE_STRUCT_IP_MREQN */
			
			ret = setsockopt (sock, system_level, system_name,
					  &mreq, sizeof (mreq));
			break;
		}
		default:
			/* Throw an exception */
			mono_raise_exception(get_socket_exception(WSAEINVAL));
		}
	} else if (byte_val!=NULL) {
		int valsize=mono_array_length(byte_val);
		guchar *buf=mono_array_addr(byte_val, guchar, 0);
	
		ret=setsockopt(sock, system_level, system_name, buf, valsize);
		if(ret==SOCKET_ERROR) {
			mono_raise_exception(get_socket_exception(WSAGetLastError()));
		}
	} else {
		ret=setsockopt(sock, system_level, system_name, &int_val,
			       sizeof(int_val));
	}

	if(ret==SOCKET_ERROR) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
	}
}

void ves_icall_System_Net_Sockets_Socket_Shutdown_internal(SOCKET sock,
							   gint32 how)
{
	int ret;
	
	/* Currently, the values for how (recv=0, send=1, both=2) match
	 * the BSD API
	 */
	ret=shutdown(sock, how);
	if(ret==SOCKET_ERROR) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
	}
}

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
	
	*h_aliases=mono_array_new(domain, mono_defaults.string_class, i);
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
	
	*h_addr_list=mono_array_new(domain, mono_defaults.string_class, i);
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

extern MonoBoolean ves_icall_System_Net_Dns_GetHostByName_internal(MonoString *host, MonoString **h_name, MonoArray **h_aliases, MonoArray **h_addr_list)
{
	char *hostname;
	struct hostent *he;
	
	hostname=mono_string_to_utf8(host);
	he=gethostbyname(hostname);
	g_free(hostname);
	
	if(he==NULL) {
		return(FALSE);
	}

	return(hostent_to_IPHostEntry(he, h_name, h_aliases, h_addr_list));
}

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
	struct in_addr inaddr;
	struct hostent *he;
	char *address;
	
	address = mono_string_to_utf8 (addr);
	if (inet_pton (AF_INET, address, &inaddr) <= 0) {
		g_free (address);
		return FALSE;
	}
	
	g_free (address);
	if ((he = gethostbyaddr ((char *) &inaddr, sizeof (inaddr), AF_INET)) == NULL)
		return FALSE;
	
	return(hostent_to_IPHostEntry(he, h_name, h_aliases, h_addr_list));
}

extern MonoBoolean ves_icall_System_Net_Dns_GetHostName_internal(MonoString **h_name)
{
	guchar hostname[256];
	int ret;
	
	ret=gethostname (hostname, sizeof(hostname));
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
