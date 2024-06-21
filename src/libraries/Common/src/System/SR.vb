' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
'
' SR.vb
'
'   This is a standin for the SR class used throughout FX.
'

Imports System.Resources

Namespace System

    Friend NotInheritable Class SR
        Private Shared ReadOnly s_usingResourceKeys As Boolean = GetUsingResourceKeysSwitchValue()

        Private Shared Function GetUsingResourceKeysSwitchValue() As Boolean
            Dim usingResourceKeys As Boolean
            If (AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", usingResourceKeys)) Then
                Return usingResourceKeys
            End If

            Return False
        End Function

        ' This method Is used to decide if we need to append the exception message parameters to the message when calling SR.Format.
        ' by default it returns the value of System.Resources.UseSystemResourceKeys AppContext switch Or false if Not specified.
        ' Native code generators can replace the value this returns based on user input at the time of native code generation.
        ' The trimming tools are also capable of replacing the value of this method when the application Is being trimmed.
        Public Shared Function UsingResourceKeys() As Boolean
            Return s_usingResourceKeys
        End Function

        Friend Shared Function GetResourceString(ByVal resourceKey As String, Optional ByVal defaultString As String = Nothing) As String
            If (UsingResourceKeys()) Then
                Return If(defaultString, resourceKey)
            End If

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
