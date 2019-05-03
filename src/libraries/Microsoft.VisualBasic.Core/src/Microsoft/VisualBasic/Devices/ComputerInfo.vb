' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Diagnostics
Imports Microsoft.VisualBasic.CompilerServices

Namespace Microsoft.VisualBasic.Devices

    '''*************************************************************************
    ''' ;ComputerInfo
    ''' <summary>
    ''' Provides configuration information about the current computer and the current process.
    ''' </summary>
    <DebuggerTypeProxy(GetType(ComputerInfo.ComputerInfoDebugView))>
    Public Class ComputerInfo

        ' Keep the debugger proxy current as you change this class - see the nested ComputerInfoDebugView below.

        '= PUBLIC =============================================================

        '''******************************************************************************
        ''' ;New
        ''' <summary>
        ''' Default ctor
        ''' </summary>
        Sub New()
        End Sub

        '''******************************************************************************
        ''' ;TotalPhysicalMemory
        ''' <summary>
        ''' Gets the total size of physical memory on the machine.
        ''' </summary>
        ''' <value>A 64-bit unsigned integer containing the size of total physical memory on the machine, in bytes.</value>
        ''' <exception cref="System.ComponentModel.Win32Exception">If we are unable to obtain the memory status.</exception>
        <CLSCompliant(False)>
        Public ReadOnly Property TotalPhysicalMemory() As UInt64
            Get
                Return MemoryStatus.TotalPhysicalMemory
            End Get
        End Property

        '''******************************************************************************
        ''' ;AvailablePhysicalMemory
        ''' <summary>
        ''' Gets the total size of free physical memory on the machine.
        ''' </summary>
        ''' <value>A 64-bit unsigned integer containing the size of free physical memory on the machine, in bytes.</value>
        ''' <exception cref="System.ComponentModel.Win32Exception">If we are unable to obtain the memory status.</exception>
        <CLSCompliant(False)>
        Public ReadOnly Property AvailablePhysicalMemory() As UInt64
            Get
                Return MemoryStatus.AvailablePhysicalMemory
            End Get
        End Property

        '''******************************************************************************
        ''' ;TotalVirtualMemory
        ''' <summary>
        ''' Gets the total size of user potion of virtual address space for calling process.
        ''' </summary>
        ''' <value>A 64-bit unsigned integer containing the size of user potion of virtual address space for calling process, 
        '''          in bytes.</value>
        ''' <exception cref="System.ComponentModel.Win32Exception">If we are unable to obtain the memory status.</exception>
        <CLSCompliant(False)>
        Public ReadOnly Property TotalVirtualMemory() As UInt64
            Get
                Return MemoryStatus.TotalVirtualMemory
            End Get
        End Property

        '''******************************************************************************
        ''' ;AvailableVirtualMemory
        ''' <summary>
        ''' Gets the total size of free user potion of virtual address space for calling process.
        ''' </summary>
        ''' <value>A 64-bit unsigned integer containing the size of free user potion of virtual address space for calling process, 
        '''          in bytes.</value>
        ''' <exception cref="System.ComponentModel.Win32Exception">If we are unable to obtain the memory status.</exception>
        <CLSCompliant(False)>
        Public ReadOnly Property AvailableVirtualMemory() As UInt64
            Get
                Return MemoryStatus.AvailableVirtualMemory
            End Get
        End Property

        '''******************************************************************************
        ''' ;InstalledUICulture
        ''' <summary>
        ''' Gets the current UICulture installed on the machine.
        ''' </summary>
        ''' <value>A CultureInfo object represents the UI culture installed on the machine.</value>
        Public ReadOnly Property InstalledUICulture() As Globalization.CultureInfo
            Get
                Return Globalization.CultureInfo.InstalledUICulture
            End Get
        End Property

        '''**************************************************************************
        ''' ;OSPlatform
        ''' <summary>
        ''' Gets the platform OS name.
        ''' </summary>
        ''' <value>A string containing a Platform ID like "Win32NT", "Win32S", "Win32Windows". See PlatformID enum.</value>
        ''' <exception cref="System.ExecutionEngineException">If cannot obtain the OS Version information.</exception>
        Public ReadOnly Property OSPlatform() As String
            Get
                Return Environment.OSVersion.Platform.ToString
            End Get
        End Property

        '''******************************************************************************
        ''' ;OSVersion
        ''' <summary>
        ''' Get the current version number of the operating system.
        ''' </summary>
        ''' <value>A string contains the current version number of the operating system.</value>
        ''' <exception cref="System.ExecutionEngineException">If cannot obtain the OS Version information.</exception>
        Public ReadOnly Property OSVersion() As String
            Get
                Return Environment.OSVersion.Version.ToString
            End Get
        End Property

        '= FRIEND =============================================================

        '''******************************************************************************
        ''' ;ComputerInfoDebugView
        ''' <summary>
        ''' Debugger proxy for the ComputerInfo class.  The problem is that OSFullName can time out the debugger
        ''' so we offer a view that doesn't have that field. 
        ''' </summary>
        ''' <remarks></remarks>
        Friend NotInheritable Class ComputerInfoDebugView
            Public Sub New(ByVal RealClass As ComputerInfo)
                m_InstanceBeingWatched = RealClass
            End Sub

            <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
            Public ReadOnly Property TotalPhysicalMemory() As UInt64
                Get
                    Return m_InstanceBeingWatched.TotalPhysicalMemory
                End Get
            End Property

            <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
            Public ReadOnly Property AvailablePhysicalMemory() As UInt64
                Get
                    Return m_InstanceBeingWatched.AvailablePhysicalMemory
                End Get
            End Property

            <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
            Public ReadOnly Property TotalVirtualMemory() As UInt64
                Get
                    Return m_InstanceBeingWatched.TotalVirtualMemory
                End Get
            End Property

            <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
            Public ReadOnly Property AvailableVirtualMemory() As UInt64
                Get
                    Return m_InstanceBeingWatched.AvailableVirtualMemory
                End Get
            End Property

            <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
            Public ReadOnly Property InstalledUICulture() As Globalization.CultureInfo
                Get
                    Return m_InstanceBeingWatched.InstalledUICulture
                End Get
            End Property

            <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
            Public ReadOnly Property OSPlatform() As String
                Get
                    Return m_InstanceBeingWatched.OSPlatform
                End Get
            End Property

            <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
            Public ReadOnly Property OSVersion() As String
                Get
                    Return m_InstanceBeingWatched.OSVersion
                End Get
            End Property

            <DebuggerBrowsable(DebuggerBrowsableState.Never)> Private m_InstanceBeingWatched As ComputerInfo
        End Class

        '= PRIVATE ============================================================

        '''******************************************************************************
        ''' ;MemoryStatus
        ''' <summary>
        ''' Get the whole memory information details.
        ''' </summary>
        ''' <value>An InternalMemoryStatus class.</value>
        Private ReadOnly Property MemoryStatus() As InternalMemoryStatus
            Get
                If m_InternalMemoryStatus Is Nothing Then
                    m_InternalMemoryStatus = New InternalMemoryStatus
                End If
                Return m_InternalMemoryStatus
            End Get
        End Property

        Private m_InternalMemoryStatus As InternalMemoryStatus = Nothing ' Cache our InternalMemoryStatus

        '''******************************************************************************
        ''' ;InternalMemoryStatus
        ''' <summary>
        ''' Calls GlobalMemoryStatusEx and returns the correct value.
        ''' </summary>
        Private Class InternalMemoryStatus
            Friend Sub New()
            End Sub

            Friend ReadOnly Property TotalPhysicalMemory() As UInt64
                Get
#If PLATFORM_WINDOWS Then
                    Refresh()
                    Return m_MemoryStatusEx.ullTotalPhys
#Else
                    Throw New PlatformNotSupportedException()
#End If
                End Get
            End Property

            Friend ReadOnly Property AvailablePhysicalMemory() As UInt64
                Get
#If PLATFORM_WINDOWS Then
                    Refresh()
                    Return m_MemoryStatusEx.ullAvailPhys
#Else
                    Throw New PlatformNotSupportedException()
#End If
                End Get
            End Property

            Friend ReadOnly Property TotalVirtualMemory() As UInt64
                Get
#If PLATFORM_WINDOWS Then
                    Refresh()
                    Return m_MemoryStatusEx.ullTotalVirtual
#Else
                    Throw New PlatformNotSupportedException()
#End If
                End Get
            End Property

            Friend ReadOnly Property AvailableVirtualMemory() As UInt64
                Get
#If PLATFORM_WINDOWS Then
                    Refresh()
                    Return m_MemoryStatusEx.ullAvailVirtual
#Else
                    Throw New PlatformNotSupportedException()
#End If
                End Get
            End Property

#If PLATFORM_WINDOWS Then
            Private Sub Refresh()
                m_MemoryStatusEx = New NativeMethods.MEMORYSTATUSEX
                m_MemoryStatusEx.Init()
                If (Not NativeMethods.GlobalMemoryStatusEx(m_MemoryStatusEx)) Then
                    Throw ExceptionUtils.GetWin32Exception(SR.DiagnosticInfo_Memory)
                End If
            End Sub

            Private m_MemoryStatusEx As NativeMethods.MEMORYSTATUSEX
#End If
        End Class
    End Class

End Namespace
