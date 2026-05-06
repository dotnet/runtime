// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Stub implementations of networking functions for browser target.

#include "pal_config.h"
#include "pal_networking.h"
#include "pal_utilities.h"
#include "pal_errno.h"

#include <errno.h>
#include <string.h>
#include <stdlib.h>
#include <unistd.h>

int32_t SystemNative_GetHostEntryForName(const uint8_t* address, int32_t addressFamily, HostEntry* entry)
{
    (void)address;
    (void)addressFamily;
    (void)entry;
    return Error_ENOTSUP;
}

void SystemNative_FreeHostEntry(HostEntry* entry)
{
    (void)entry;
}

int32_t SystemNative_GetNameInfo(const uint8_t* address,
                               int32_t addressLength,
                               int8_t isIPv6,
                               uint8_t* host,
                               int32_t hostLength,
                               uint8_t* service,
                               int32_t serviceLength,
                               int32_t flags)
{
    (void)address;
    (void)addressLength;
    (void)isIPv6;
    (void)host;
    (void)hostLength;
    (void)service;
    (void)serviceLength;
    (void)flags;
    return Error_ENOTSUP;
}

int32_t SystemNative_GetDomainName(uint8_t* name, int32_t nameLength)
{
    (void)name;
    (void)nameLength;
    errno = ENOTSUP;
    return -1;
}

int32_t SystemNative_GetHostName(uint8_t* name, int32_t nameLength)
{
    (void)name;
    (void)nameLength;
    errno = ENOTSUP;
    return -1;
}

int32_t SystemNative_GetSocketAddressSizes(int32_t* ipv4SocketAddressSize, int32_t* ipv6SocketAddressSize, int32_t* udsSocketAddressSize, int32_t* maxSocketAddressSize)
{
    (void)ipv4SocketAddressSize;
    (void)ipv6SocketAddressSize;
    (void)udsSocketAddressSize;
    (void)maxSocketAddressSize;
    return Error_ENOTSUP;
}

int32_t SystemNative_GetAddressFamily(const uint8_t* socketAddress, int32_t socketAddressLen, int32_t* addressFamily)
{
    (void)socketAddress;
    (void)socketAddressLen;
    (void)addressFamily;
    return Error_ENOTSUP;
}

int32_t SystemNative_SetAddressFamily(uint8_t* socketAddress, int32_t socketAddressLen, int32_t addressFamily)
{
    (void)socketAddress;
    (void)socketAddressLen;
    (void)addressFamily;
    return Error_ENOTSUP;
}

int32_t SystemNative_GetPort(const uint8_t* socketAddress, int32_t socketAddressLen, uint16_t* port)
{
    (void)socketAddress;
    (void)socketAddressLen;
    (void)port;
    return Error_ENOTSUP;
}

int32_t SystemNative_SetPort(uint8_t* socketAddress, int32_t socketAddressLen, uint16_t port)
{
    (void)socketAddress;
    (void)socketAddressLen;
    (void)port;
    return Error_ENOTSUP;
}

int32_t SystemNative_GetIPv4Address(const uint8_t* socketAddress, int32_t socketAddressLen, uint32_t* address)
{
    (void)socketAddress;
    (void)socketAddressLen;
    (void)address;
    return Error_ENOTSUP;
}

int32_t SystemNative_SetIPv4Address(uint8_t* socketAddress, int32_t socketAddressLen, uint32_t address)
{
    (void)socketAddress;
    (void)socketAddressLen;
    (void)address;
    return Error_ENOTSUP;
}

int32_t SystemNative_GetIPv6Address(
    const uint8_t* socketAddress, int32_t socketAddressLen, uint8_t* address, int32_t addressLen, uint32_t* scopeId)
{
    (void)socketAddress;
    (void)socketAddressLen;
    (void)address;
    (void)addressLen;
    (void)scopeId;
    return Error_ENOTSUP;
}

int32_t SystemNative_SetIPv6Address(
    uint8_t* socketAddress, int32_t socketAddressLen, uint8_t* address, int32_t addressLen, uint32_t scopeId)
{
    (void)socketAddress;
    (void)socketAddressLen;
    (void)address;
    (void)addressLen;
    (void)scopeId;
    return Error_ENOTSUP;
}

int32_t SystemNative_GetControlMessageBufferSize(int32_t isIPv4, int32_t isIPv6)
{
    (void)isIPv4;
    (void)isIPv6;
    return 0;
}

int32_t SystemNative_TryGetIPPacketInformation(MessageHeader* messageHeader, int32_t isIPv4, IPPacketInformation* packetInfo)
{
    (void)messageHeader;
    (void)isIPv4;
    (void)packetInfo;
    return 0; // false
}

int32_t SystemNative_GetIPv4MulticastOption(intptr_t socket, int32_t multicastOption, IPv4MulticastOption* option)
{
    (void)socket;
    (void)multicastOption;
    (void)option;
    return Error_ENOTSUP;
}

int32_t SystemNative_SetIPv4MulticastOption(intptr_t socket, int32_t multicastOption, IPv4MulticastOption* option)
{
    (void)socket;
    (void)multicastOption;
    (void)option;
    return Error_ENOTSUP;
}

int32_t SystemNative_GetIPv6MulticastOption(intptr_t socket, int32_t multicastOption, IPv6MulticastOption* option)
{
    (void)socket;
    (void)multicastOption;
    (void)option;
    return Error_ENOTSUP;
}

int32_t SystemNative_SetIPv6MulticastOption(intptr_t socket, int32_t multicastOption, IPv6MulticastOption* option)
{
    (void)socket;
    (void)multicastOption;
    (void)option;
    return Error_ENOTSUP;
}

int32_t SystemNative_GetLingerOption(intptr_t socket, LingerOption* option)
{
    (void)socket;
    (void)option;
    return Error_ENOTSUP;
}

int32_t SystemNative_SetLingerOption(intptr_t socket, LingerOption* option)
{
    (void)socket;
    (void)option;
    return Error_ENOTSUP;
}

int32_t SystemNative_SetReceiveTimeout(intptr_t socket, int32_t millisecondsTimeout)
{
    (void)socket;
    (void)millisecondsTimeout;
    return Error_ENOTSUP;
}

int32_t SystemNative_SetSendTimeout(intptr_t socket, int32_t millisecondsTimeout)
{
    (void)socket;
    (void)millisecondsTimeout;
    return Error_ENOTSUP;
}

int32_t SystemNative_Receive(intptr_t socket, void* buffer, int32_t bufferLen, int32_t flags, int32_t* received)
{
    (void)socket;
    (void)buffer;
    (void)bufferLen;
    (void)flags;
    (void)received;
    return Error_ENOTSUP;
}

int32_t SystemNative_ReceiveMessage(intptr_t socket, MessageHeader* messageHeader, int32_t flags, int64_t* received)
{
    (void)socket;
    (void)messageHeader;
    (void)flags;
    (void)received;
    return Error_ENOTSUP;
}

int32_t SystemNative_ReceiveSocketError(intptr_t socket, MessageHeader* messageHeader)
{
    (void)socket;
    (void)messageHeader;
    return Error_ENOTSUP;
}

int32_t SystemNative_Send(intptr_t socket, void* buffer, int32_t bufferLen, int32_t flags, int32_t* sent)
{
    (void)socket;
    (void)buffer;
    (void)bufferLen;
    (void)flags;
    (void)sent;
    return Error_ENOTSUP;
}

int32_t SystemNative_SendMessage(intptr_t socket, MessageHeader* messageHeader, int32_t flags, int64_t* sent)
{
    (void)socket;
    (void)messageHeader;
    (void)flags;
    (void)sent;
    return Error_ENOTSUP;
}

int32_t SystemNative_Accept(intptr_t socket, uint8_t* socketAddress, int32_t* socketAddressLen, intptr_t* acceptedSocket)
{
    (void)socket;
    (void)socketAddress;
    (void)socketAddressLen;
    (void)acceptedSocket;
    return Error_ENOTSUP;
}

int32_t SystemNative_Bind(intptr_t socket, int32_t protocolType, uint8_t* socketAddress, int32_t socketAddressLen)
{
    (void)socket;
    (void)protocolType;
    (void)socketAddress;
    (void)socketAddressLen;
    return Error_ENOTSUP;
}

int32_t SystemNative_Connect(intptr_t socket, uint8_t* socketAddress, int32_t socketAddressLen)
{
    (void)socket;
    (void)socketAddress;
    (void)socketAddressLen;
    return Error_ENOTSUP;
}

int32_t SystemNative_Connectx(intptr_t socket, uint8_t* socketAddress, int32_t socketAddressLen, uint8_t* data, int32_t dataLen, int32_t tfo, int* sent)
{
    (void)socket;
    (void)socketAddress;
    (void)socketAddressLen;
    (void)data;
    (void)dataLen;
    (void)tfo;
    if (sent != NULL)
    {
        *sent = 0;
    }
    return Error_ENOTSUP;
}

int32_t SystemNative_GetPeerName(intptr_t socket, uint8_t* socketAddress, int32_t* socketAddressLen)
{
    (void)socket;
    (void)socketAddress;
    (void)socketAddressLen;
    return Error_ENOTSUP;
}

int32_t SystemNative_GetSockName(intptr_t socket, uint8_t* socketAddress, int32_t* socketAddressLen)
{
    (void)socket;
    (void)socketAddress;
    (void)socketAddressLen;
    return Error_ENOTSUP;
}

int32_t SystemNative_Listen(intptr_t socket, int32_t backlog)
{
    (void)socket;
    (void)backlog;
    return Error_ENOTSUP;
}

int32_t SystemNative_Shutdown(intptr_t socket, int32_t socketShutdown)
{
    (void)socket;
    (void)socketShutdown;
    return Error_ENOTSUP;
}

int32_t SystemNative_GetSocketErrorOption(intptr_t socket, int32_t* error)
{
    (void)socket;
    (void)error;
    return Error_ENOTSUP;
}

int32_t SystemNative_GetSockOpt(
    intptr_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t* optionLen)
{
    (void)socket;
    (void)socketOptionLevel;
    (void)socketOptionName;
    (void)optionValue;
    (void)optionLen;
    return Error_ENOTSUP;
}

int32_t SystemNative_GetRawSockOpt(
    intptr_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t* optionLen)
{
    (void)socket;
    (void)socketOptionLevel;
    (void)socketOptionName;
    (void)optionValue;
    (void)optionLen;
    return Error_ENOTSUP;
}

int32_t SystemNative_SetSockOpt(
    intptr_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t optionLen)
{
    (void)socket;
    (void)socketOptionLevel;
    (void)socketOptionName;
    (void)optionValue;
    (void)optionLen;
    return Error_ENOTSUP;
}

int32_t SystemNative_SetRawSockOpt(
    intptr_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t optionLen)
{
    (void)socket;
    (void)socketOptionLevel;
    (void)socketOptionName;
    (void)optionValue;
    (void)optionLen;
    return Error_ENOTSUP;
}

int32_t SystemNative_Socket(int32_t addressFamily, int32_t socketType, int32_t protocolType, intptr_t* createdSocket)
{
    (void)addressFamily;
    (void)socketType;
    (void)protocolType;
    (void)createdSocket;
    return Error_ENOTSUP;
}

int32_t SystemNative_GetSocketType(intptr_t socket, int32_t* addressFamily, int32_t* socketType, int32_t* protocolType, int32_t* isListening)
{
    (void)socket;
    (void)addressFamily;
    (void)socketType;
    (void)protocolType;
    (void)isListening;
    return Error_ENOTSUP;
}

int32_t SystemNative_GetAtOutOfBandMark(intptr_t socket, int32_t* available)
{
    (void)socket;
    (void)available;
    return Error_ENOTSUP;
}

int32_t SystemNative_GetBytesAvailable(intptr_t socket, int32_t* available)
{
    (void)socket;
    (void)available;
    return Error_ENOTSUP;
}

int32_t SystemNative_GetWasiSocketDescriptor(intptr_t socket, void** entry)
{
    (void)socket;
    (void)entry;
    return Error_ENOTSUP;
}

int32_t SystemNative_CreateSocketEventPort(intptr_t* port)
{
    (void)port;
    return Error_ENOTSUP;
}

int32_t SystemNative_CloseSocketEventPort(intptr_t port)
{
    (void)port;
    return Error_ENOTSUP;
}

int32_t SystemNative_CreateSocketEventBuffer(int32_t count, SocketEvent** buffer)
{
    (void)count;
    (void)buffer;
    return Error_ENOTSUP;
}

int32_t SystemNative_FreeSocketEventBuffer(SocketEvent* buffer)
{
    (void)buffer;
    return Error_ENOTSUP;
}

int32_t SystemNative_TryChangeSocketEventRegistration(
    intptr_t port, intptr_t socket, int32_t currentEvents, int32_t newEvents, uintptr_t data)
{
    (void)port;
    (void)socket;
    (void)currentEvents;
    (void)newEvents;
    (void)data;
    return Error_ENOTSUP;
}

int32_t SystemNative_WaitForSocketEvents(intptr_t port, SocketEvent* buffer, int32_t* count)
{
    (void)port;
    (void)buffer;
    (void)count;
    return Error_ENOTSUP;
}

int32_t SystemNative_PlatformSupportsDualModeIPv4PacketInfo(void)
{
    return 0; // false
}

void SystemNative_GetDomainSocketSizes(int32_t* pathOffset, int32_t* pathSize, int32_t* addressSize)
{
    if (pathOffset != NULL) *pathOffset = 0;
    if (pathSize != NULL) *pathSize = 0;
    if (addressSize != NULL) *addressSize = 0;
}

int32_t SystemNative_GetMaximumAddressSize(void)
{
    return 128;
}

int32_t SystemNative_SendFile(intptr_t out_fd, intptr_t in_fd, int64_t offset, int64_t count, int64_t* sent)
{
    (void)out_fd;
    (void)in_fd;
    (void)offset;
    (void)count;
    (void)sent;
    return Error_ENOTSUP;
}

int32_t SystemNative_Disconnect(intptr_t socket)
{
    (void)socket;
    return Error_ENOTSUP;
}

uint32_t SystemNative_InterfaceNameToIndex(char* interfaceName)
{
    (void)interfaceName;
    return 0;
}

int32_t SystemNative_Select(int* readFds, int readFdsCount, int* writeFds, int writeFdsCount, int* errorFds, int errorFdsCount, int32_t microseconds, int32_t maxFd, int* triggered)
{
    (void)readFds;
    (void)readFdsCount;
    (void)writeFds;
    (void)writeFdsCount;
    (void)errorFds;
    (void)errorFdsCount;
    (void)microseconds;
    (void)maxFd;
    (void)triggered;
    return Error_ENOTSUP;
}

// To silence linker when linking with -nostdlib and dependency from libc/syslog
int socket(int domain, int type, int protocol)
{
    (void)domain;
    (void)type;
    (void)protocol;
    errno = ENOTSUP;
    return -1;
}

int connect(int fd, const struct sockaddr *addr, socklen_t len)
{
    (void)fd;
    (void)addr;
    (void)len;
    errno = ENOTSUP;
    return -1;
}

ssize_t send(int fd, const void *buf, size_t len, int flags)
{
    (void)fd;
    (void)buf;
    (void)len;
    (void)flags;
    errno = ENOTSUP;
    return -1;
}

