' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict On
Option Explicit On

Imports System
Imports Microsoft.VisualBasic.MyServices

Namespace Microsoft.VisualBasic.Devices

    '''**************************************************************************
    ''' ;Computer 
    ''' <summary>
    '''   A RAD object representing the 'computer' that serves as a discovery 
    '''   mechanism for finding principle abstractions in the system that you can
    '''   code against such as the file system, the clipboard, performance 
    '''   counters, etc. It also provides functionality you would expect to see 
    '''   associated with the computer such as playing sound, timers, access to 
    '''   environment variables, etc. This class represent a general computer
    '''   available from a Windows Application, Web app, Dll library, etc.
    ''' </summary>
    Public Class Computer : Inherits ServerComputer

        '= PUBLIC =============================================================

        'NOTE: The .Net design guidelines state that access to Instance members does not have to be thread-safe.  Access to Shared members does have to be thread-safe. 
        'Since My.Computer creates the instance of Computer in a thread-safe way, access to the Computer will necessarily be thread-safe.
        'There is nothing to prevent a user from passing our computer object across threads or creating their own instance and then getting into trouble.
        ' But that is completely consistent with the rest of the FX design.  It is MY.* that is thread safe and leads to best practice access to these objects.
        '  If you dim them up yourself, you are responsible for managing the threading.

        '''**************************************************************************
        ''' ;Audio
        ''' <summary>
        ''' Get an Audio object which can play sound files or resources.
        ''' </summary>
        ''' <value>A sound object.</value>
        Public ReadOnly Property Audio() As Audio
            Get
                If m_Audio IsNot Nothing Then Return m_Audio
                m_Audio = New Audio()
                Return m_Audio
            End Get
        End Property

        '''**************************************************************************
        ''' ;Clipboard
        ''' <summary>
        ''' A thin wrapper for System.Windows.Forms.Clipboard
        ''' </summary>
        ''' <value>An object representing the clipboard</value>
        Public ReadOnly Property Clipboard() As ClipboardProxy
            Get
                If m_Clipboard Is Nothing Then
                    m_Clipboard = New ClipboardProxy()
                End If

                Return m_Clipboard
            End Get
        End Property

        '''**************************************************************************
        ''' ;Ports
        ''' <summary>
        ''' Gets a port object which gives access to the ports on the local machine
        ''' </summary>
        ''' <value>A collection of serial ports on the machine.</value>
        Public ReadOnly Property Ports() As Ports
            Get
                If m_Ports Is Nothing Then
                    m_Ports = New Ports()
                End If

                Return m_Ports
            End Get
        End Property

        '''**************************************************************************
        ''' ;Mouse
        ''' <summary>
        ''' This property returns the Mouse object containing information about
        ''' the physical mouse installed to the machine.
        ''' </summary>
        ''' <value>An instance of the Mouse class.</value>
        Public ReadOnly Property Mouse() As Mouse
            Get
                If m_Mouse IsNot Nothing Then Return m_Mouse
                m_Mouse = New Mouse
                Return m_Mouse
            End Get
        End Property

        '''**************************************************************************
        ''' ;Keyboard
        ''' <summary>
        ''' This property returns the Keyboard object representing some
        ''' keyboard properties and a send keys method
        ''' </summary>
        ''' <value>An instance of the Keyboard class.</value>
        Public ReadOnly Property Keyboard() As Keyboard
            Get
                If m_KeyboardInstance IsNot Nothing Then Return m_KeyboardInstance
                m_KeyboardInstance = New Keyboard
                Return m_KeyboardInstance
            End Get
        End Property

        '= FRIENDS ============================================================

        '= PROTECTED ==========================================================

        '= PRIVATE ============================================================

        Private m_Audio As Audio 'Lazy initialized cache for the Audio class.
        Private m_Ports As Ports 'Lazy initialized cache for the Ports class
        Private Shared m_Clipboard As ClipboardProxy 'Lazy initialized cacche for the clipboard class. (proxies can be shared - they have no state)
        Private Shared m_Mouse As Mouse 'Lazy initialized cache for the Mouse class. SHARED because Mouse behaves as a readonly singleton class
        Private Shared m_KeyboardInstance As Keyboard 'Lazy initialized cache for the Keyboard class.  SHARED because Keyboard behaves as a readonly singleton class

    End Class 'Computer
End Namespace
