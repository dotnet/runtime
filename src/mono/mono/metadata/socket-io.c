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

#include <mono/metadata/object.h>
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/socket-io.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/appdomain.h>

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
	}

	return(proto);
}

#define STASH_SYS_ASS(this) \
	if(system_assembly==NULL) { \
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

SOCKET ves_icall_System_Net_Sockets_Socket_Socket_internal(MonoObject *this, gint32 family, gint32 type, gint32 proto)
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
	
	return(sock);
}

void ves_icall_System_Net_Sockets_Socket_Close_internal(SOCKET sock)
{
	closesocket(sock);
}

gint32 ves_icall_System_Net_Sockets_SocketException_WSAGetLastError_internal(void)
{
	g_message(G_GNUC_PRETTY_FUNCTION ": returning %d", WSAGetLastError());
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

SOCKET ves_icall_System_Net_Sockets_Socket_Accept_internal(SOCKET sock)
{
	SOCKET newsock;
	
	newsock=accept(sock, NULL, 0);
	if(newsock==INVALID_SOCKET) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
		return(NULL);
	}
	
	return(newsock);
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
			mono_raise_exception(mono_exception_from_name(mono_defaults.corlib, "System", "SystemException"));
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
	g_message(G_GNUC_PRETTY_FUNCTION ": bound to %s port %d", inet_ntoa((struct sockaddr_in)sa.sin_addr), ntohs((struct sockaddr_in)sa.sin_port));
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
	g_message(G_GNUC_PRETTY_FUNCTION ": connected to %s port %d", inet_ntoa((struct sockaddr_in)sa.sin_addr), ntohs((struct sockaddr_in)sa.sin_port));
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
		mono_raise_exception(mono_exception_from_name(mono_defaults.corlib, "System", "SystemException"));
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
	g_message(G_GNUC_PRETTY_FUNCTION ": binding to %s port %d", inet_ntoa(sa.sin_addr), port);
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
	g_message(G_GNUC_PRETTY_FUNCTION ": connecting to %s port %d", inet_ntoa(sa.sin_addr), port);
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
	g_free(sa);
	
	if(ret==SOCKET_ERROR) {
		mono_raise_exception(get_socket_exception(WSAGetLastError()));
	}

	*sockaddr=create_object_from_sockaddr(sa, sa_size);
	
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
		
		snprintf(addr, 16, "%u.%u.%u.%u",
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

extern gboolean ves_icall_System_Net_Dns_GetHostByName_internal(MonoString *host, MonoString **h_name, MonoArray **h_aliases, MonoArray **h_addr_list)
{
	char *hostname;
	struct hostent *he;
	
	hostname=mono_string_to_utf8(host);
	he=gethostbyname(hostname);
	free(hostname);
	
	if(he==NULL) {
		return(FALSE);
	}

	return(hostent_to_IPHostEntry(he, h_name, h_aliases, h_addr_list));
}

extern gboolean ves_icall_System_Net_Dns_GetHostByAddr_internal(MonoString *addr, MonoString **h_name, MonoArray **h_aliases, MonoArray **h_addr_list)
{
	char *address;
	guint32 inaddr;
	struct hostent *he;
	
	address=mono_string_to_utf8(addr);
	inaddr=inet_addr(address);
	free(address);
	if(inaddr==INADDR_NONE) {
		return(FALSE);
	}
	
	he=gethostbyaddr(&inaddr, sizeof(inaddr), AF_INET);
	if(he==NULL) {
		return(FALSE);
	}

	return(hostent_to_IPHostEntry(he, h_name, h_aliases, h_addr_list));
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
