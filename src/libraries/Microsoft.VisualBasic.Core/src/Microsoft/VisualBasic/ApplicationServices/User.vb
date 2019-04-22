' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Explicit On
Option Strict On

Imports System.ComponentModel
Imports System.Security.Principal

Namespace Microsoft.VisualBasic.ApplicationServices

    '''************************************************************************
    ''';User
    ''' <summary>
    ''' Class abstracting the computer user
    ''' </summary>
    Public Class User

        '==PUBLIC**************************************************************

        '''********************************************************************
        ''';New
        ''' <summary>
        ''' Creates an instance of User
        ''' </summary>
        Public Sub New()
        End Sub

        '''********************************************************************
        ''';Name
        ''' <summary>
        ''' The name of the current user
        ''' </summary>
        Public ReadOnly Property Name() As String
            Get
                Return InternalPrincipal.Identity.Name
            End Get
        End Property

        '''********************************************************************
        ''';CurrentPrincipal
        ''' <summary>
        ''' The current IPrincipal which represents the current user
        ''' </summary>
        ''' <value>An IPrincipal representing the current user</value>
        <EditorBrowsable(EditorBrowsableState.Advanced)> Public Property CurrentPrincipal() As IPrincipal
            Get
                Return InternalPrincipal
            End Get
            Set(ByVal value As IPrincipal)
                InternalPrincipal = value
            End Set
        End Property

        '''********************************************************************
        ''';IsAuthenticated
        ''' <summary>
        ''' Indicates whether or not the current user has been authenticated.
        ''' </summary>
        Public ReadOnly Property IsAuthenticated() As Boolean
            Get
                Return InternalPrincipal.Identity.IsAuthenticated
            End Get
        End Property

        '''********************************************************************
        ''';IsInRole
        ''' <summary>
        ''' Indicates whether or not the current user is a member of the passed in role
        ''' </summary>
        ''' <param name="role">The name of the role</param>
        ''' <returns>True if the user is a member of the role otherwise False</returns>
        Public Function IsInRole(ByVal role As String) As Boolean
            Return InternalPrincipal.IsInRole(role)
        End Function

        '==PROTECTED***********************************************************

        ''';InternalPrincipal
        ''' <summary>
        ''' The principal representing the current user.
        ''' </summary>
        ''' <value>An IPrincipal representing the current user</value>
        ''' <remarks> 
        ''' This should be overriden by derived classes that don't get the current
        ''' user from the current thread
        ''' </remarks>
        Protected Overridable Property InternalPrincipal() As IPrincipal
            Get
                Return System.Threading.Thread.CurrentPrincipal
            End Get
            Set(ByVal value As IPrincipal)
                System.Threading.Thread.CurrentPrincipal = value
            End Set
        End Property

    End Class 'User

End Namespace

