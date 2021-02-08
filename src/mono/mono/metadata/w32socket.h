/**
 * \file
 * System.Net.Sockets.Socket support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#ifndef _MONO_METADATA_W32SOCKET_H_
#define _MONO_METADATA_W32SOCKET_H_

#include <config.h>
#include <glib.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/icalls.h>

#ifndef HOST_WIN32
#define INVALID_SOCKET ((SOCKET)(guint32)(~0))
#define SOCKET_ERROR (-1)

typedef gint SOCKET;

typedef struct {
	guint32 len;
	gpointer buf;
} WSABUF, *LPWSABUF;
#endif

/* This is a copy of System.Net.Sockets.SocketType */
typedef enum {
	SocketType_Stream=1,
	SocketType_Dgram=2,
	SocketType_Raw=3,
	SocketType_Rdm=4,
	SocketType_Seqpacket=5,
	SocketType_Unknown=-1
} MonoSocketType;

/* This is a copy of System.Net.Sockets.AddressFamily */
typedef enum {
	AddressFamily_Unknown=-1,
	AddressFamily_Unspecified=0,
	AddressFamily_Unix=1,
	AddressFamily_InterNetwork=2,
	AddressFamily_ImpLink=3,
	AddressFamily_Pup=4,
	AddressFamily_Chaos=5,
	AddressFamily_NS=6,
	AddressFamily_Ipx=6,
	AddressFamily_Iso=7,
	AddressFamily_Osi=7,
	AddressFamily_Ecma=8,
	AddressFamily_DataKit=9,
	AddressFamily_Ccitt=10,
	AddressFamily_Sna=11,
	AddressFamily_DecNet=12,
	AddressFamily_DataLink=13,
	AddressFamily_Lat=14,
	AddressFamily_HyperChannel=15,
	AddressFamily_AppleTalk=16,
	AddressFamily_NetBios=17,
	AddressFamily_VoiceView=18,
	AddressFamily_FireFox=19,
	AddressFamily_Banyan=21,
	AddressFamily_Atm=22,
	AddressFamily_InterNetworkV6=23,
	AddressFamily_Cluster=24,
	AddressFamily_Ieee12844=25,
	AddressFamily_Irda=26,
	AddressFamily_NetworkDesigners=28
} MonoAddressFamily;

/* This is a copy of System.Net.Sockets.ProtocolType */
typedef enum {
	ProtocolType_IP=0,
	ProtocolType_Icmp=1,
	ProtocolType_Igmp=2,
	ProtocolType_Ggp=3,
	ProtocolType_Tcp=6,
	ProtocolType_Pup=12,
	ProtocolType_Udp=17,
	ProtocolType_Idp=22,
	ProtocolType_IPv6=41,
	ProtocolType_ND=77,
	ProtocolType_Raw=255,
	ProtocolType_Unspecified=0,
	ProtocolType_Ipx=1000,
	ProtocolType_Spx=1256,
	ProtocolType_SpxII=1257,
	ProtocolType_Unknown=-1
} MonoProtocolType;

/* This is a copy of System.Net.Sockets.SocketOptionLevel */
typedef enum {
	SocketOptionLevel_Socket=65535,
	SocketOptionLevel_IP=0,
	SocketOptionLevel_IPv6=41,
	SocketOptionLevel_Tcp=6,
	SocketOptionLevel_Udp=17
} MonoSocketOptionLevel;

/* This is a copy of System.Net.Sockets.SocketOptionName */
typedef enum {
	SocketOptionName_Debug=1,
	SocketOptionName_AcceptConnection=2,
	SocketOptionName_ReuseAddress=4,
	SocketOptionName_KeepAlive=8,
	SocketOptionName_DontRoute=16,
	SocketOptionName_IPProtectionLevel = 23,
	SocketOptionName_IPv6Only = 27,
	SocketOptionName_Broadcast=32,
	SocketOptionName_UseLoopback=64,
	SocketOptionName_Linger=128,
	SocketOptionName_OutOfBandInline=256,
	SocketOptionName_DontLinger= -129,
	SocketOptionName_ExclusiveAddressUse= -5,
	SocketOptionName_SendBuffer= 4097,
	SocketOptionName_ReceiveBuffer=4098,
	SocketOptionName_SendLowWater=4099,
	SocketOptionName_ReceiveLowWater=4100,
	SocketOptionName_SendTimeout=4101,
	SocketOptionName_ReceiveTimeout=4102,
	SocketOptionName_Error=4103,
	SocketOptionName_Type=4104,
	SocketOptionName_MaxConnections=2147483647,
	SocketOptionName_IPOptions=1,
	SocketOptionName_HeaderIncluded=2,
	SocketOptionName_TypeOfService=3,
	SocketOptionName_IpTimeToLive=4,
	SocketOptionName_MulticastInterface=9,
	SocketOptionName_MulticastTimeToLive=10,
	SocketOptionName_MulticastLoopback=11,
	SocketOptionName_AddMembership=12,
	SocketOptionName_DropMembership=13,
	SocketOptionName_DontFragment=14,
	SocketOptionName_AddSourceMembership=15,
	SocketOptionName_DropSourceMembership=16,
	SocketOptionName_BlockSource=17,
	SocketOptionName_UnblockSource=18,
	SocketOptionName_PacketInformation=19,
	SocketOptionName_NoDelay=1,
	SocketOptionName_BsdUrgent=2,
	SocketOptionName_Expedited=2,
	SocketOptionName_NoChecksum=1,
	SocketOptionName_ChecksumCoverage=20,
	SocketOptionName_HopLimit=21,

	/* This is Mono-specific, keep it in sync with
	 * Mono.Posix/PeerCred.cs
	 */
	SocketOptionName_PeerCred=10001
} MonoSocketOptionName;

/* This is a copy of System.Net.Sockets.SocketFlags */
typedef enum {
	SocketFlags_None = 0x0000,
	SocketFlags_OutOfBand = 0x0001,
	SocketFlags_MaxIOVectorLength = 0x0010,
	SocketFlags_Peek = 0x0002,
	SocketFlags_DontRoute = 0x0004,
	SocketFlags_Partial = 0x8000
} MonoSocketFlags;

typedef struct
{
	MonoObject obj;
	gint pid;
	gint uid;
	gint gid;
} MonoPeerCredData;

/* Safely access Mono.Posix.PeerCredData from native code */
TYPED_HANDLE_DECL (MonoPeerCredData);

ICALL_EXPORT
gint32
ves_icall_System_Net_Sockets_SocketException_WSAGetLastError_icall (void);

#endif /* _MONO_METADATA_W32SOCKET_H_ */
