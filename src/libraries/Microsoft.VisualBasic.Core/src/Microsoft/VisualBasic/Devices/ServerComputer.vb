' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict On
Option Explicit On

Imports System
Imports Microsoft.VisualBasic.MyServices

Namespace Microsoft.VisualBasic.Devices

    '''**************************************************************************
    ''' ;ServerComputer
    ''' <summary>
    ''' A RAD object representing the server 'computer' for the web/Windows Services
    ''' that serves as a discovery mechanism for finding principle abstractions in
    ''' the system that you can code against
    ''' </summary>
    Public Class ServerComputer

        '= PUBLIC =============================================================

        'NOTE: The .Net design guidelines state that access to Instance members does not have to be thread-safe.  Access to Shared members does have to be thread-safe. 
        'Since My.Computer creates the instance of Computer in a thread-safe way, access to the Computer will necessarily be thread-safe.
        'There is nothing to prevent a user from passing our computer object across threads or creating their own instance and then getting into trouble.
        'But that is completely consistent with the rest of the FX design.  It is MY.* that is thread safe and leads to best practice access to these objects.
        'If you dim them up yourself, you are responsible for managing the threading.

        '''**************************************************************************
        ''' ;Clock
        ''' <summary>
        ''' Returns the Clock object which contains the LocalTime and GMTTime.
        ''' </summary>
        Public ReadOnly Property Clock() As Clock
            Get
                If m_Clock IsNot Nothing Then Return m_Clock
                m_Clock = New Clock
                Return m_Clock
            End Get
        End Property

        '''**************************************************************************
        ''' ;FileSystem
        ''' <summary>
        ''' Gets the object representing the file system of the computer.
        ''' </summary>
        ''' <value>A System.IO.FileSystem object.</value>
        ''' <remarks>The instance returned by this property is lazy initialized and cached.</remarks>
        Public ReadOnly Property FileSystem() As FileSystemProxy
            Get
                If m_FileIO Is Nothing Then
                    m_FileIO = New FileSystemProxy
                End If
                Return m_FileIO
            End Get
        End Property

        '''**************************************************************************
        ''' ;Info
        ''' <summary>
        ''' Gets the object representing information about the computer's state
        ''' </summary>
        ''' <value>A Microsoft.VisualBasic.MyServices.ComputerInfo object.</value>
        ''' <remarks>The instance returned by this property is lazy initialized and cached.</remarks>
        Public ReadOnly Property Info() As ComputerInfo
            Get
                If m_ComputerInfo Is Nothing Then
                    m_ComputerInfo = New ComputerInfo
                End If
                Return m_ComputerInfo
            End Get
        End Property

        '''**************************************************************************
        ''' ;Network
        ''' <summary>
        ''' This property returns the Network object containing information about 
        ''' the network the machine is part of.
        ''' </summary>
        ''' <value>An instance of the Network.Network class.</value>
        Public ReadOnly Property Network() As Network
            Get
                If m_Network IsNot Nothing Then Return m_Network
                m_Network = New Network
                Return m_Network
            End Get
        End Property

        '''**************************************************************************
        ''' ;Name
        ''' <summary>
        ''' This property wraps the System.Environment.MachineName property 
        ''' in the .NET framework to return the name of the computer.
        ''' </summary>
        ''' <value>A string containing the name of the computer.</value>
        Public ReadOnly Property Name() As String
            Get
                Return System.Environment.MachineName
            End Get
        End Property

        '''**************************************************************************
        ''' ;Registry
        ''' <summary>
        ''' Get the Registry object, which can be used to read, set and 
        ''' enumerate keys and values in the system registry.
        ''' </summary>
        ''' <value>An instance of the RegistryProxy object</value>
        ''' <remarks></remarks>
        Public ReadOnly Property Registry() As RegistryProxy
            Get
                If m_RegistryInstance IsNot Nothing Then Return m_RegistryInstance
                m_RegistryInstance = New RegistryProxy
                Return m_RegistryInstance
            End Get
        End Property

        '= FRIENDS ============================================================

        '= PROTECTED ==========================================================

        '= PRIVATE ============================================================

        Private m_ComputerInfo As ComputerInfo 'Lazy initialized cache for ComputerInfo
        Private m_FileIO As FileSystemProxy 'Lazy initialized cache for the FileSystem.
        Private m_Network As Network 'Lazy initialized cache for the Network class.
        Private m_RegistryInstance As RegistryProxy 'Lazy initialized cache for the Registry class
        Private Shared m_Clock As Clock 'Lazy initialized cache for the Clock class.  SHARED because Clock behaves as a readonly singleton class

    End Class 'MyServerComputer
End Namespace
