' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'
' SR.vb
'
'   This is a standin for the SR class used throughout FX.
'

Imports System.Resources
Imports System.Text

Namespace System

    Friend Class SR

        Private Shared s_resourceManager As ResourceManager
        Private Sub New()
        End Sub
        Private Shared ReadOnly Property ResourceManager As ResourceManager
            Get

                If SR.s_resourceManager Is Nothing Then

                    ' The following constructor ResourceManager(Type) is going to be replaced by the private constructor ResourceManager(String)
                    ' we'll pass s_resourcesName to this constructor
                    SR.s_resourceManager = New ResourceManager(SR.ResourceType)
                End If
                Return SR.s_resourceManager
            End Get
        End Property

        ' This method is used to decide if we need to append the exception message parameters to the message when calling SR.Format. 
        ' by default it returns false. We overwrite the implementation of this method to return true through IL transformer 
        ' when compiling ProjectN app as retail build. 
        <Global.System.Runtime.CompilerServices.MethodImpl(Global.System.Runtime.CompilerServices.MethodImplOptions.NoInlining)>
        Public Shared Function UsingResourceKeys() As Boolean
            Return False
        End Function

        Friend Shared Function GetResourceString(ByVal resourceKey As String, ByVal defaultString As String) As String

            Dim resourceString As String = Nothing
            Try
                resourceString = ResourceManager.GetString(resourceKey)
            Catch ex As MissingManifestResourceException
            End Try

            ' if we are running on desktop, ResourceManager.GetString will just return resourceKey. so
            ' in this case we'll return defaultString (if it is not null) 
            If defaultString IsNot Nothing AndAlso resourceKey.Equals(resourceString, StringComparison.Ordinal) Then
                Return defaultString
            End If

            Return resourceString
        End Function

        Friend Shared Function Format(ByVal resourceFormat As String, ParamArray args() As Object) As String
            If args IsNot Nothing Then
                If (UsingResourceKeys()) Then
                    Return resourceFormat + String.Join(", ", args)
                End If
                Return String.Format(resourceFormat, args)
            End If
            Return resourceFormat
        End Function

        <Global.System.Runtime.CompilerServices.MethodImpl(Global.System.Runtime.CompilerServices.MethodImplOptions.NoInlining)>
        Friend Shared Function Format(ByVal resourceFormat As String, p1 As Object) As String
            If (UsingResourceKeys()) Then
                Return String.Join(", ", resourceFormat, p1)
            End If

            Return String.Format(resourceFormat, p1)
        End Function

        <Global.System.Runtime.CompilerServices.MethodImpl(Global.System.Runtime.CompilerServices.MethodImplOptions.NoInlining)>
        Friend Shared Function Format(ByVal resourceFormat As String, p1 As Object, p2 As Object) As String
            If (UsingResourceKeys()) Then
                Return String.Join(", ", resourceFormat, p1, p2)
            End If

            Return String.Format(resourceFormat, p1, p2)
        End Function

        <Global.System.Runtime.CompilerServices.MethodImpl(Global.System.Runtime.CompilerServices.MethodImplOptions.NoInlining)>
        Friend Shared Function Format(ByVal resourceFormat As String, p1 As Object, p2 As Object, p3 As Object) As String
            If (UsingResourceKeys()) Then
                Return String.Join(", ", resourceFormat, p1, p2, p3)
            End If
            Return String.Format(resourceFormat, p1, p2, p3)
        End Function
    End Class
End Namespace
