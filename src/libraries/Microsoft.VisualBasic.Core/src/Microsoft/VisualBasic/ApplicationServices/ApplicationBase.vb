' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict On
Option Explicit On

Imports System
Imports ExUtils = Microsoft.VisualBasic.CompilerServices.ExceptionUtils

Namespace Microsoft.VisualBasic.ApplicationServices

    '''**************************************************************************
    ''' ;ApplicationBase
    ''' <summary>
    ''' Abstract class that defines the application Startup/Shutdown model for VB 
    ''' Windows Applications such as console, winforms, dll, service.
    ''' </summary>
    Public Class ApplicationBase

        '= PUBLIC =============================================================

        Public Sub New()
        End Sub

        '''**************************************************************************
        ''' ;GetEnvironmentVariable
        ''' <summary>
        ''' Return the value of the specified environment variable.
        ''' </summary>
        ''' <param name="Name">A String containing the name of the environment variable.</param>
        ''' <returns>A string containing the value of the environment variable.</returns>
        ''' <exception cref="System.ArgumentNullException">if Name is Nothing. (Framework)</exception>
        ''' <exception cref="System.Security.SecurityException">if caller does not have EnvironmentPermission.Read. (Framework)</exception>
        ''' <exception cref="System.ArgumentException">if the specified environment variable does not exist. (Ours)</exception>
        Public Function GetEnvironmentVariable(ByVal name As String) As String

            ' Framework returns Null if not found.
            Dim VariableValue As String = System.Environment.GetEnvironmentVariable(name)

            ' Since the explicity requested a specific environment variable and we couldn't find it, throw
            If VariableValue Is Nothing Then
                Throw ExUtils.GetArgumentExceptionWithArgName("name", SR.EnvVarNotFound_Name, name)
            End If

            Return VariableValue
        End Function

        '**********************************************************************
        ';Culture
        '
        'Summary:
        '   Get the information about the current culture used by the current thread.
        'Returns:
        '   The CultureInfo object that represents the culture used by the current thread.
        '**********************************************************************
        Public ReadOnly Property Culture() As System.Globalization.CultureInfo
            Get
                Return System.Threading.Thread.CurrentThread.CurrentCulture
            End Get
        End Property

        '**********************************************************************
        ';UICulture
        '
        'Summary:
        '   Get the information about the current culture used by the Resource
        '   Manager to look up culture-specific resource at run time.
        'Returns:
        '   The CultureInfo object that represents the culture used by the 
        '   Resource Manager to look up culture-specific resources at run time.
        '**********************************************************************
        Public ReadOnly Property UICulture() As System.Globalization.CultureInfo
            Get
                Return System.Threading.Thread.CurrentThread.CurrentUICulture
            End Get
        End Property

        '**********************************************************************
        ';ChangeCulture
        '
        'Summary:
        '   Change the culture currently in used by the current thread.
        'Params:
        '   CultureName: name of the culture as a String. For a list of possible 
        '   names, see http://msdn.microsoft.com/library/en-us/cpref/html/frlrfSystemGlobalizationCultureInfoClassTopic.asp
        'Remarks:
        '   CultureInfo constructor will throw exceptions if CultureName is Nothing 
        '   or an invalid CultureInfo ID. We are not catching those exceptions.
        '**********************************************************************
        Public Sub ChangeCulture(ByVal cultureName As String)
            System.Threading.Thread.CurrentThread.CurrentCulture = New System.Globalization.CultureInfo(cultureName)
        End Sub

        '**********************************************************************
        ';ChangeUICulture
        '
        'Summary:
        '   Change the culture currently used by the Resource Manager to look
        '   up culture-specific resource at runtime.
        'Params:
        '   CultureName: name of the culture as a String. For a list of possible 
        '   names, see http://msdn.microsoft.com/library/en-us/cpref/html/frlrfSystemGlobalizationCultureInfoClassTopic.asp
        'Remarks:
        '   CultureInfo constructor will throw exceptions if CultureName is Nothing 
        '   or an invalid CultureInfo ID. We are not catching those exceptions.
        '**********************************************************************
        Public Sub ChangeUICulture(ByVal cultureName As String)
            System.Threading.Thread.CurrentThread.CurrentUICulture = New System.Globalization.CultureInfo(cultureName)
        End Sub

        '= FRIEND =============================================================

        '= PROTECTED ==========================================================

        '= PRIVATE ==========================================================

    End Class 'ApplicationBase
End Namespace
