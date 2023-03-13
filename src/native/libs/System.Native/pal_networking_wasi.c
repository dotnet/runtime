// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_networking.h"
#include "pal_safecrt.h"
#include "pal_utilities.h"
#include <pal_networking_common.h>
#include <fcntl.h>

#include <stdlib.h>
#include <limits.h>
int32_t SystemNative_GetHostEntryForName(const uint8_t* address, int32_t addressFamily, HostEntry* entry)
{
    return -1;
}

void SystemNative_FreeHostEntry(HostEntry* entry)
{
    if (entry != NULL)
    {
        free(entry->CanonicalName);
        free(entry->IPAddressList);

        entry->CanonicalName = NULL;
        entry->IPAddressList = NULL;
        entry->IPAddressCount = 0;
    }
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
    assert(address != NULL);
    assert(addressLength > 0);
    assert((host != NULL) || (service != NULL));
    assert((hostLength > 0) || (serviceLength > 0));

    return Error_EINVAL;
}

int32_t SystemNative_GetDomainName(uint8_t* name, int32_t nameLength)
{
    assert(name != NULL);
    assert(nameLength > 0);
    return Error_EFAULT;
}

int32_t SystemNative_GetHostName(uint8_t* name, int32_t nameLength)
{
    assert(name != NULL);
    assert(nameLength > 0);

    size_t unsignedSize = (uint32_t)nameLength;
    return gethostname((char*)name, unsignedSize);
}

int32_t SystemNative_GetIPSocketAddressSizes(int32_t* ipv4SocketAddressSize, int32_t* ipv6SocketAddressSize)
{
    return Error_EFAULT;
}

int32_t SystemNative_GetAddressFamily(const uint8_t* socketAddress, int32_t socketAddressLen, int32_t* addressFamily)
{
    return Error_EFAULT;
}

int32_t SystemNative_SetAddressFamily(uint8_t* socketAddress, int32_t socketAddressLen, int32_t addressFamily)
{
    return Error_EFAULT;
}

int32_t SystemNative_GetPort(const uint8_t* socketAddress, int32_t socketAddressLen, uint16_t* port)
{
    return Error_EFAULT;
}

int32_t SystemNative_SetPort(uint8_t* socketAddress, int32_t socketAddressLen, uint16_t port)
{
    return Error_EFAULT;
}

int32_t SystemNative_GetIPv4Address(const uint8_t* socketAddress, int32_t socketAddressLen, uint32_t* address)
{
    return Error_EFAULT;
}

int32_t SystemNative_SetIPv4Address(uint8_t* socketAddress, int32_t socketAddressLen, uint32_t address)
{
    return Error_EFAULT;
}

int32_t SystemNative_GetIPv6Address(
    const uint8_t* socketAddress, int32_t socketAddressLen, uint8_t* address, int32_t addressLen, uint32_t* scopeId)
{
    return Error_EFAULT;
}

int32_t
SystemNative_SetIPv6Address(uint8_t* socketAddress, int32_t socketAddressLen, uint8_t* address, int32_t addressLen, uint32_t scopeId)
{
    return Error_EFAULT;
}

int32_t SystemNative_GetControlMessageBufferSize(int32_t isIPv4, int32_t isIPv6)
{
    return Error_EFAULT;
}


int32_t
SystemNative_TryGetIPPacketInformation(MessageHeader* messageHeader, int32_t isIPv4, IPPacketInformation* packetInfo)
{
    if (messageHeader == NULL || packetInfo == NULL)
    {
        return 0;
    }

    return Error_EINVAL;
}

int32_t SystemNative_GetIPv4MulticastOption(intptr_t socket, int32_t multicastOption, IPv4MulticastOption* option)
{
    return Error_EINVAL;
}

int32_t SystemNative_SetIPv4MulticastOption(intptr_t socket, int32_t multicastOption, IPv4MulticastOption* option)
{
    if (option == NULL)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_GetIPv6MulticastOption(intptr_t socket, int32_t multicastOption, IPv6MulticastOption* option)
{
    if (option == NULL)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_SetIPv6MulticastOption(intptr_t socket, int32_t multicastOption, IPv6MulticastOption* option)
{
    if (option == NULL)
    {
        return Error_EFAULT;
    }
    return Error_EINVAL;
}


int32_t SystemNative_GetLingerOption(intptr_t socket, LingerOption* option)
{
    if (option == NULL)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_SetLingerOption(intptr_t socket, LingerOption* option)
{
    if (option == NULL)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_SetReceiveTimeout(intptr_t socket, int32_t millisecondsTimeout)
{
    return Error_EINVAL;
}

int32_t SystemNative_SetSendTimeout(intptr_t socket, int32_t millisecondsTimeout)
{
    return Error_EINVAL;
}
int32_t SystemNative_Receive(intptr_t socket, void* buffer, int32_t bufferLen, int32_t flags, int32_t* received)
{
    if (buffer == NULL || bufferLen < 0 || received == NULL)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_ReceiveMessage(intptr_t socket, MessageHeader* messageHeader, int32_t flags, int64_t* received)
{
    if (messageHeader == NULL || received == NULL || messageHeader->SocketAddressLen < 0 ||
        messageHeader->ControlBufferLen < 0 || messageHeader->IOVectorCount < 0)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_Send(intptr_t socket, void* buffer, int32_t bufferLen, int32_t flags, int32_t* sent)
{
    return Error_EINVAL;
}

int32_t SystemNative_SendMessage(intptr_t socket, MessageHeader* messageHeader, int32_t flags, int64_t* sent)
{
    return Error_EINVAL;
}

int32_t SystemNative_Accept(intptr_t socket, uint8_t* socketAddress, int32_t* socketAddressLen, intptr_t* acceptedSocket)
{
    if (socketAddress == NULL || socketAddressLen == NULL || acceptedSocket == NULL || *socketAddressLen < 0)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_Bind(intptr_t socket, int32_t protocolType, uint8_t* socketAddress, int32_t socketAddressLen)
{
    if (socketAddress == NULL || socketAddressLen < 0)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_Connect(intptr_t socket, uint8_t* socketAddress, int32_t socketAddressLen)
{
    return Error_EINVAL;
}

int32_t SystemNative_GetPeerName(intptr_t socket, uint8_t* socketAddress, int32_t* socketAddressLen)
{
    if (socketAddress == NULL || socketAddressLen == NULL || *socketAddressLen < 0)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_GetSockName(intptr_t socket, uint8_t* socketAddress, int32_t* socketAddressLen)
{
    if (socketAddress == NULL || socketAddressLen == NULL || *socketAddressLen < 0)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_Listen(intptr_t socket, int32_t backlog)
{
    return Error_EINVAL;
}

int32_t SystemNative_Shutdown(intptr_t socket, int32_t socketShutdown)
{
    return Error_EINVAL;
}

int32_t SystemNative_GetSocketErrorOption(intptr_t socket, int32_t* error)
{
    if (error == NULL)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_GetSockOpt(
    intptr_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t* optionLen)
{
    if (optionLen == NULL || *optionLen < 0)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_GetRawSockOpt(
    intptr_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t* optionLen)
{
    if (optionLen == NULL || *optionLen < 0)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t
SystemNative_SetSockOpt(intptr_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t optionLen)
{
    return Error_EINVAL;
}

int32_t SystemNative_SetRawSockOpt(
    intptr_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t optionLen)
{
    if (optionLen < 0 || optionValue == NULL)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_Socket(int32_t addressFamily, int32_t socketType, int32_t protocolType, intptr_t* createdSocket)
{
    if (createdSocket == NULL)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_GetSocketType(intptr_t socket, int32_t* addressFamily, int32_t* socketType, int32_t* protocolType, int32_t* isListening)
{
    if (addressFamily == NULL || socketType == NULL || protocolType == NULL || isListening == NULL)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_GetAtOutOfBandMark(intptr_t socket, int32_t* atMark)
{
    if (atMark == NULL)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_GetBytesAvailable(intptr_t socket, int32_t* available)
{
    if (available == NULL)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_CreateSocketEventPort(intptr_t* port)
{
    if (port == NULL)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_CloseSocketEventPort(intptr_t port)
{
    return Error_EINVAL;
}

int32_t SystemNative_CreateSocketEventBuffer(int32_t count, SocketEvent** buffer)
{
    if (buffer == NULL || count < 0)
    {
        return Error_EFAULT;
    }

    return Error_EINVAL;
}

int32_t SystemNative_FreeSocketEventBuffer(SocketEvent* buffer)
{
    free(buffer);
    return Error_SUCCESS;
}

int32_t
SystemNative_TryChangeSocketEventRegistration(intptr_t port, intptr_t socket, int32_t currentEvents, int32_t newEvents, uintptr_t data)
{
    return Error_EINVAL;
}

int32_t SystemNative_WaitForSocketEvents(intptr_t port, SocketEvent* buffer, int32_t* count)
{
    return Error_EINVAL;
}

int32_t SystemNative_PlatformSupportsDualModeIPv4PacketInfo(void)
{
    return 0;
}


char* SystemNative_GetPeerUserName(intptr_t socket)
{
    return NULL;
}

void SystemNative_GetDomainSocketSizes(int32_t* pathOffset, int32_t* pathSize, int32_t* addressSize)
{
    *pathOffset = -1;
    *pathSize = -1;
    *addressSize = -1;
}

int32_t SystemNative_GetMaximumAddressSize(void)
{
    return sizeof(struct sockaddr_storage);
}

int32_t SystemNative_Disconnect(intptr_t socket)
{
    return Error_EINVAL;
}

int32_t SystemNative_SendFile(intptr_t out_fd, intptr_t in_fd, int64_t offset, int64_t count, int64_t* sent)
{
    assert(sent != NULL);

    return Error_EINVAL;
}

uint32_t SystemNative_InterfaceNameToIndex(char* interfaceName)
{
    assert(interfaceName != NULL);
    return Error_EINVAL;
}

