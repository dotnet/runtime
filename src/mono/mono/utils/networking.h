/**
 * \file
 * Portable networking functions
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2015 Xamarin
 */


#ifndef __MONO_NETWORKING_H__
#define __MONO_NETWORKING_H__

#include <config.h>
#include <glib.h>

#ifdef HAVE_ARPA_INET_H
#include <arpa/inet.h>
#endif
#include <sys/types.h>
#ifdef HAVE_SYS_SOCKET_H
#include <sys/socket.h>
#endif
#ifdef HAVE_SYS_SOCKIO_H
#include <sys/sockio.h>
#endif

#ifdef HAVE_NETINET_IN_H
#include <netinet/in.h>
#endif

#ifdef HOST_WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
#endif

#include <mono/utils/mono-compiler.h>

typedef enum {
	MONO_HINT_UNSPECIFIED		= 0,
	MONO_HINT_IPV4				= 1,
	MONO_HINT_IPV6				= 2,
	MONO_HINT_CANONICAL_NAME	= 4,
	MONO_HINT_CONFIGURED_ONLY	= 8,
	MONO_HINT_NUMERIC_HOST      = 16,
} MonoGetAddressHints;

typedef struct _MonoAddressEntry MonoAddressEntry;

struct _MonoAddressEntry {
	int family;
	int socktype;
	int protocol;
	int address_len;
	union {
		struct in_addr v4;
#ifdef HAVE_STRUCT_SOCKADDR_IN6
		struct in6_addr v6;
#endif
	} address;
	const char *canonical_name;
	MonoAddressEntry *next;
};

typedef struct {
	MonoAddressEntry *entries;
	char **aliases;
} MonoAddressInfo;

typedef union {
	struct sockaddr_in v4;
#ifdef HAVE_STRUCT_SOCKADDR_IN6
	struct sockaddr_in6 v6;
#endif
	struct sockaddr addr;
} MonoSocketAddress;

typedef struct {
	int family;
	union {
		struct in_addr v4;
#ifdef HAVE_STRUCT_SOCKADDR_IN6
		struct in6_addr v6;
#endif
	} addr;
} MonoAddress;

/* This only supports IPV4 / IPV6 and tcp */
int mono_get_address_info (const char *hostname, int port, int flags, MonoAddressInfo **res);

void mono_free_address_info (MonoAddressInfo *ai);

void mono_socket_address_init (MonoSocketAddress *sa, socklen_t *len, int family, const void *address, int port);

void *mono_get_local_interfaces (int family, int *interface_count);

#ifndef HAVE_INET_PTON
int inet_pton (int family, const char *address, void *inaddrp);
#endif

void mono_address_init (MonoAddress *out_addr, int family, void *in_addr);
int mono_address_size_for_family (int family);
gboolean mono_networking_addr_to_str (MonoAddress *address, char *buffer, socklen_t buflen);

int mono_networking_get_tcp_protocol (void);
int mono_networking_get_ip_protocol (void);
int mono_networking_get_ipv6_protocol (void);

void mono_networking_init (void);
void mono_networking_shutdown (void);


#endif
