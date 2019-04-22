' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Diagnostics

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

    End Class

End Namespace
