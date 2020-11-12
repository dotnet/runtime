// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>

// Include System.Native headers
#include "pal_console.h"
#include "pal_datetime.h"
#include "pal_errno.h"
#include "pal_interfaceaddresses.h"
#include "pal_io.h"
#include "pal_log.h"
#include "pal_memory.h"
#include "pal_mount.h"
#include "pal_networkchange.h"
#include "pal_networking.h"
#include "pal_networkstatistics.h"
#include "pal_process.h"
#include "pal_random.h"
#include "pal_runtimeextensions.h"
#include "pal_runtimeinformation.h"
#include "pal_searchpath.h"
#include "pal_signal.h"
#include "pal_string.h"
#include "pal_sysctl.h"
#include "pal_tcpstate.h"
#include "pal_threading.h"
#include "pal_time.h"
#include "pal_uid.h"

#define FCFuncStart(name) EXTERN_C const void* name[]; const void* name[] = {
#define FCFuncEnd() (void*)0x01 /* FCFuncFlag_EndOfArray */ };

#define QCFuncElement(name,impl) \
    (void*)0x8 /* FCFuncFlag_QCall */, (void*)(impl), (void*)name,

FCFuncStart(gEmbedded_Fcntl)
    QCFuncElement("FcntlCanGetSetPipeSz", SystemNative_FcntlCanGetSetPipeSz)
    QCFuncElement("GetFD", SystemNative_FcntlGetFD)
    QCFuncElement("GetIsNonBlocking", SystemNative_FcntlGetIsNonBlocking)
    QCFuncElement("GetPipeSz", SystemNative_FcntlGetPipeSz)
    QCFuncElement("SetFD", SystemNative_FcntlSetFD)
    QCFuncElement("DangerousSetIsNonBlocking", SystemNative_FcntlSetIsNonBlocking)
    QCFuncElement("SetIsNonBlocking", SystemNative_FcntlSetIsNonBlocking)
    QCFuncElement("SetPipeSz", SystemNative_FcntlSetPipeSz)
FCFuncEnd()

FCFuncStart(gEmbedded_Sys)
    QCFuncElement("Accept", SystemNative_Accept)
    QCFuncElement("Access", SystemNative_Access)
    QCFuncElement("Bind", SystemNative_Bind)
    QCFuncElement("ChDir", SystemNative_ChDir)
    QCFuncElement("ChMod", SystemNative_ChMod)
    QCFuncElement("Close", SystemNative_Close)
    QCFuncElement("CloseDir", SystemNative_CloseDir)

#if defined(__FreeBSD__) || defined(__linux__)
    QCFuncElement("CloseNetworkChangeListenerSocket", SystemNative_CloseNetworkChangeListenerSocket)
#endif

    QCFuncElement("CloseSocketEventPort", SystemNative_CloseSocketEventPort)
    QCFuncElement("ConfigureTerminalForChildProcess", SystemNative_ConfigureTerminalForChildProcess)
    QCFuncElement("Connect", SystemNative_Connect)
    QCFuncElement("ConvertErrorPalToPlatform", SystemNative_ConvertErrorPalToPlatform)
    QCFuncElement("ConvertErrorPlatformToPal", SystemNative_ConvertErrorPlatformToPal)
    QCFuncElement("CopyFile", SystemNative_CopyFile)

#if defined(__FreeBSD__) || defined(__linux__)
    QCFuncElement("CreateNetworkChangeListenerSocket", SystemNative_CreateNetworkChangeListenerSocket)
#endif

    QCFuncElement("CreateSocketEventBuffer", SystemNative_CreateSocketEventBuffer)
    QCFuncElement("CreateSocketEventPort", SystemNative_CreateSocketEventPort)
    QCFuncElement("Disconnect", SystemNative_Disconnect)
    QCFuncElement("Dup", SystemNative_Dup)
    QCFuncElement("EnumerateInterfaceAddresses", SystemNative_EnumerateInterfaceAddresses)
    QCFuncElement("FChMod", SystemNative_FChMod)
    QCFuncElement("FLock", SystemNative_FLock)
    QCFuncElement("ForkAndExecProcess", SystemNative_ForkAndExecProcess)
    QCFuncElement("FreeHostEntry", SystemNative_FreeHostEntry)
    QCFuncElement("FreeSocketEventBuffer", SystemNative_FreeSocketEventBuffer)
    QCFuncElement("FStat", SystemNative_FStat)
    QCFuncElement("FSync", SystemNative_FSync)
    QCFuncElement("FTruncate", SystemNative_FTruncate)
    QCFuncElement("GetAddressFamily", SystemNative_GetAddressFamily)
    QCFuncElement("GetAllMountPoints", SystemNative_GetAllMountPoints)
    QCFuncElement("GetAtOutOfBandMark", SystemNative_GetAtOutOfBandMark)
    QCFuncElement("GetBytesAvailable", SystemNative_GetBytesAvailable)
    QCFuncElement("GetControlCharacters", SystemNative_GetControlCharacters)
    QCFuncElement("GetControlMessageBufferSize", SystemNative_GetControlMessageBufferSize)
    QCFuncElement("GetCpuUtilization", SystemNative_GetCpuUtilization)
    QCFuncElement("GetCwd", SystemNative_GetCwd)
    QCFuncElement("GetDomainName", SystemNative_GetDomainName)
    QCFuncElement("GetDomainSocketSizes", SystemNative_GetDomainSocketSizes)
    QCFuncElement("GetEGid", SystemNative_GetEGid)
    QCFuncElement("GetEUid", SystemNative_GetEUid)
    QCFuncElement("GetFormatInfoForMountPoint", SystemNative_GetFormatInfoForMountPoint)
    QCFuncElement("GetGroupList", SystemNative_GetGroupList)
    QCFuncElement("GetHostEntryForName", SystemNative_GetHostEntryForName)
    QCFuncElement("GetHostName", SystemNative_GetHostName)
    QCFuncElement("GetIPSocketAddressSizes", SystemNative_GetIPSocketAddressSizes)
    QCFuncElement("GetIPv4Address", SystemNative_GetIPv4Address)
    QCFuncElement("GetIPv4MulticastOption", SystemNative_GetIPv4MulticastOption)
    QCFuncElement("GetIPv6Address", SystemNative_GetIPv6Address)
    QCFuncElement("GetIPv6MulticastOption", SystemNative_GetIPv6MulticastOption)
    QCFuncElement("GetLingerOption", SystemNative_GetLingerOption)
    QCFuncElement("GetMaximumAddressSize", SystemNative_GetMaximumAddressSize)
    QCFuncElement("GetNameInfo", SystemNative_GetNameInfo)
    QCFuncElement("GetNetworkInterfaces", SystemNative_GetNetworkInterfaces)
    // trimmed
    QCFuncElement("GetCryptographicallySecureRandomBytes", SystemNative_GetCryptographicallySecureRandomBytes)
    QCFuncElement("GetNonCryptographicallySecureRandomBytes", SystemNative_GetNonCryptographicallySecureRandomBytes)
    QCFuncElement("GetOSArchitecture", SystemNative_GetOSArchitecture)
    QCFuncElement("GetPeerID", SystemNative_GetPeerID)
    QCFuncElement("GetPeerName", SystemNative_GetPeerName)
    QCFuncElement("GetPeerUserName", SystemNative_GetPeerUserName)
    QCFuncElement("GetPid", SystemNative_GetPid)
    QCFuncElement("GetPort", SystemNative_GetPort)
    QCFuncElement("GetPriority", SystemNative_GetPriority)
    QCFuncElement("GetProcessArchitecture", SystemNative_GetProcessArchitecture)
    QCFuncElement("GetPwNamR", SystemNative_GetPwNamR)
    QCFuncElement("GetPwUidR", SystemNative_GetPwUidR)
    QCFuncElement("GetRawSockOpt", SystemNative_GetRawSockOpt)
    QCFuncElement("GetReadDirRBufferSize", SystemNative_GetReadDirRBufferSize)
    QCFuncElement("GetSignalForBreak", SystemNative_GetSignalForBreak)
    QCFuncElement("GetSocketErrorOption", SystemNative_GetSocketErrorOption)
    QCFuncElement("GetSocketType", SystemNative_GetSocketType)
    QCFuncElement("GetSockName", SystemNative_GetSockName)
    QCFuncElement("GetSockOpt", SystemNative_GetSockOpt)
    QCFuncElement("GetSpaceInfoForMountPoint", SystemNative_GetSpaceInfoForMountPoint)
    QCFuncElement("GetTimestamp", SystemNative_GetTimestamp)
    QCFuncElement("GetUnixNamePrivate", SystemNative_GetUnixName)
    QCFuncElement("GetUnixRelease", SystemNative_GetUnixRelease)
    QCFuncElement("GetUnixVersion", SystemNative_GetUnixVersion)
    QCFuncElement("GetWindowSize", SystemNative_GetWindowSize)
    QCFuncElement("InitializeConsoleBeforeRead", SystemNative_InitializeConsoleBeforeRead)
    QCFuncElement("InitializeTerminalAndSignalHandling", SystemNative_InitializeTerminalAndSignalHandling)
    QCFuncElement("INotifyAddWatch", SystemNative_INotifyAddWatch)
    QCFuncElement("INotifyInit", SystemNative_INotifyInit)
    QCFuncElement("INotifyRemoveWatch_private", SystemNative_INotifyRemoveWatch)
    QCFuncElement("InterfaceNameToIndex", SystemNative_InterfaceNameToIndex)
    QCFuncElement("IsATty", SystemNative_IsATty)
    QCFuncElement("Kill", SystemNative_Kill)
    QCFuncElement("LChflags", SystemNative_LChflags)
    QCFuncElement("LChflagsCanSetHiddenFlag", SystemNative_LChflagsCanSetHiddenFlag)
    QCFuncElement("Link", SystemNative_Link)
    QCFuncElement("Listen", SystemNative_Listen)
    QCFuncElement("LockFileRegion", SystemNative_LockFileRegion)
    QCFuncElement("LSeek", SystemNative_LSeek)
    QCFuncElement("LStat", SystemNative_LStat)
    QCFuncElement("MAdvise", SystemNative_MAdvise)
    QCFuncElement("MapTcpState", SystemNative_MapTcpState)
    QCFuncElement("MemAlloc", SystemNative_MemAlloc)
    QCFuncElement("MemFree", SystemNative_MemFree)
    QCFuncElement("MemReAlloc", SystemNative_MemReAlloc)
    QCFuncElement("MkDir", SystemNative_MkDir)
    QCFuncElement("MksTemps", SystemNative_MksTemps)
    QCFuncElement("MMap", SystemNative_MMap)
    QCFuncElement("MSync", SystemNative_MSync)
    QCFuncElement("MUnmap", SystemNative_MUnmap)
    QCFuncElement("Open", SystemNative_Open)
    QCFuncElement("OpenDir", SystemNative_OpenDir)
    //trimmed 
    QCFuncElement("PathConf", SystemNative_PathConf)
    QCFuncElement("Pipe", SystemNative_Pipe)
    QCFuncElement("PlatformSupportsDualModeIPv4PacketInfo", SystemNative_PlatformSupportsDualModeIPv4PacketInfo)
    QCFuncElement("Poll", SystemNative_Poll)
    QCFuncElement("PosixFAdvise", SystemNative_PosixFAdvise)
    QCFuncElement("Read", SystemNative_Read)
    QCFuncElement("ReadDirR", SystemNative_ReadDirR)

#if defined(__FreeBSD__) || defined(__linux__)
    QCFuncElement("ReadEvents", SystemNative_ReadEvents)
#endif

    QCFuncElement("ReadLink", SystemNative_ReadLink)
    QCFuncElement("ReadStdin", SystemNative_ReadStdin)
    QCFuncElement("Receive", SystemNative_Receive)
    QCFuncElement("ReceiveMessage", SystemNative_ReceiveMessage)
    QCFuncElement("RegisterForCtrl", SystemNative_RegisterForCtrl)
    QCFuncElement("RegisterForSigChld", SystemNative_RegisterForSigChld)
    QCFuncElement("Rename", SystemNative_Rename)
    QCFuncElement("RestoreAndHandleCtrl", SystemNative_RestoreAndHandleCtrl)
    QCFuncElement("RmDir", SystemNative_RmDir)

#if HAVE_SCHED_GETAFFINITY
    QCFuncElement("SchedGetAffinity", SystemNative_SchedGetAffinity)
#endif

#if HAVE_SCHED_SETAFFINITY
    QCFuncElement("SchedSetAffinity", SystemNative_SchedSetAffinity)
#endif

    QCFuncElement("Send", SystemNative_Send)
    QCFuncElement("SendFile", SystemNative_SendFile)
    QCFuncElement("SendMessage", SystemNative_SendMessage)
    QCFuncElement("SetAddressFamily", SystemNative_SetAddressFamily)
    QCFuncElement("SetEUid", SystemNative_SetEUid)
    QCFuncElement("SetIPv4Address", SystemNative_SetIPv4Address)
    QCFuncElement("SetIPv4MulticastOption", SystemNative_SetIPv4MulticastOption)
    QCFuncElement("SetIPv6Address", SystemNative_SetIPv6Address)
    QCFuncElement("SetIPv6MulticastOption", SystemNative_SetIPv6MulticastOption)
    QCFuncElement("SetKeypadXmit", SystemNative_SetKeypadXmit)
    QCFuncElement("SetLingerOption", SystemNative_SetLingerOption)
    QCFuncElement("SetPort", SystemNative_SetPort)
    QCFuncElement("SetPriority", SystemNative_SetPriority)
    QCFuncElement("SetRawSockOpt", SystemNative_SetRawSockOpt)
    QCFuncElement("SetReceiveTimeout", SystemNative_SetReceiveTimeout)
    QCFuncElement("SetSendTimeout", SystemNative_SetSendTimeout)
    QCFuncElement("SetSignalForBreak", SystemNative_SetSignalForBreak)
    QCFuncElement("SetSockOpt", SystemNative_SetSockOpt)
    QCFuncElement("SetTerminalInvalidationHandler", SystemNative_SetTerminalInvalidationHandler)
    QCFuncElement("ShmOpen", SystemNative_ShmOpen)
    QCFuncElement("ShmUnlink", SystemNative_ShmUnlink)
    QCFuncElement("Shutdown", SystemNative_Shutdown)
    QCFuncElement("SNPrintF", SystemNative_SNPrintF)
    QCFuncElement("Socket", SystemNative_Socket)
    QCFuncElement("Stat", SystemNative_Stat)
    QCFuncElement("StdinReady", SystemNative_StdinReady)
    QCFuncElement("StrErrorR", SystemNative_StrErrorR)
    QCFuncElement("SysConf", SystemNative_SysConf)
    QCFuncElement("SysLog", SystemNative_SysLog)
    QCFuncElement("TryChangeSocketEventRegistration", SystemNative_TryChangeSocketEventRegistration)
    QCFuncElement("TryGetIPPacketInformation", SystemNative_TryGetIPPacketInformation)
    QCFuncElement("UninitializeConsoleAfterRead", SystemNative_UninitializeConsoleAfterRead)
    QCFuncElement("Unlink", SystemNative_Unlink)
    QCFuncElement("UnregisterForCtrl", SystemNative_UnregisterForCtrl)
    QCFuncElement("UTimensat", SystemNative_UTimensat)
    QCFuncElement("WaitForSocketEvents", SystemNative_WaitForSocketEvents)
    QCFuncElement("WaitIdAnyExitedNoHangNoWait", SystemNative_WaitIdAnyExitedNoHangNoWait)
    QCFuncElement("WaitPidExitedNoHang", SystemNative_WaitPidExitedNoHang)
    QCFuncElement("Write", SystemNative_Write)

    // trimmed
    QCFuncElement("GetNodeName", SystemNative_GetNodeName)
    // trimmed
    QCFuncElement("RealPath", SystemNative_RealPath)
    // trimmed
    QCFuncElement("GetSid", SystemNative_GetSid)

    // OSX (bsd?) only
#if HAVE_NETINET_TCP_VAR_H
    QCFuncElement("GetTcpGlobalStatistics", SystemNative_GetTcpGlobalStatistics)
    QCFuncElement("GetIPv4GlobalStatistics", SystemNative_GetIPv4GlobalStatistics)
    QCFuncElement("GetUdpGlobalStatistics", SystemNative_GetUdpGlobalStatistics)
    QCFuncElement("GetIcmpv4GlobalStatistics", SystemNative_GetIcmpv4GlobalStatistics)
    QCFuncElement("GetIcmpv6GlobalStatistics", SystemNative_GetIcmpv6GlobalStatistics)
    QCFuncElement("GetEstimatedTcpConnectionCount", SystemNative_GetEstimatedTcpConnectionCount)
    QCFuncElement("GetActiveTcpConnectionInfos", SystemNative_GetActiveTcpConnectionInfos)
    QCFuncElement("GetEstimatedUdpListenerCount", SystemNative_GetEstimatedUdpListenerCount)
    QCFuncElement("GetActiveUdpListeners", SystemNative_GetActiveUdpListeners)
    QCFuncElement("GetNativeIPInterfaceStatistics", SystemNative_GetNativeIPInterfaceStatistics)
    QCFuncElement("GetNumRoutes", SystemNative_GetNumRoutes)
#endif

    // trimmed
    QCFuncElement("Sync", SystemNative_Sync)

    // trimmed
    QCFuncElement("GetRLimit", SystemNative_GetRLimit)

    // trimmed
    QCFuncElement("SetRLimit", SystemNative_SetRLimit)

    // new
    QCFuncElement("GetProcessPath", SystemNative_GetProcessPath)

#if HAVE_RT_MSGHDR
    QCFuncElement("EnumerateGatewayAddressesForInterface", SystemNative_EnumerateGatewayAddressesForInterface)
#endif

FCFuncEnd()

