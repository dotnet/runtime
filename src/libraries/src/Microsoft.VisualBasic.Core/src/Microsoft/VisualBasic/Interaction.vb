' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Runtime.InteropServices

Imports Microsoft.VisualBasic.CompilerServices
Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic

    Public Module Interaction

        '============================================================================
        ' User interaction functions.
        '============================================================================

        Public Sub Beep()
#If PLATFORM_WINDOWS Then
            UnsafeNativeMethods.MessageBeep(0)
#Else
            Throw New PlatformNotSupportedException()
#End If
        End Sub

        Public Function IIf(ByVal Expression As Boolean, ByVal TruePart As Object, ByVal FalsePart As Object) As Object
            If Expression Then
                Return TruePart
            End If

            Return FalsePart
        End Function

        Friend Function IIf(Of T)(ByVal condition As Boolean, ByVal truePart As T, ByVal falsePart As T) As T
            If condition Then
                Return truePart
            End If

            Return falsePart
        End Function

        Public Function CreateObject(ByVal ProgId As String, Optional ByVal ServerName As String = "") As Object
            'Creates local or remote COM2 objects.  Should not be used to create COM+ objects.
            'Applications that need to be STA should set STA either on their Sub Main via STAThreadAttribute
            'or through Thread.CurrentThread.ApartmentState - the VB runtime will not change this.
            'DO NOT SET THREAD STATE - Thread.CurrentThread.ApartmentState = ApartmentState.STA

            Dim t As Type

            If ProgId.Length = 0 Then
                Throw VbMakeException(vbErrors.CantCreateObject)
            End If

            If ServerName Is Nothing OrElse ServerName.Length = 0 Then
                ServerName = Nothing
            Else
                'Does the ServerName match the MachineName?
                If String.Compare(Environment.MachineName, ServerName, StringComparison.OrdinalIgnoreCase) = 0 Then
                    ServerName = Nothing
                End If
            End If

            Try
                If ServerName Is Nothing Then
                    t = Type.GetTypeFromProgID(ProgId)
                Else
                    t = Type.GetTypeFromProgID(ProgId, ServerName, True)
                End If

                Return System.Activator.CreateInstance(t)
            Catch e As COMException
                If e.ErrorCode = &H800706BA Then                    '&H800706BA = The RPC Server is unavailable
                    Throw VbMakeException(vbErrors.ServerNotFound)
                Else
                    Throw VbMakeException(vbErrors.CantCreateObject)
                End If
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch ex As System.Threading.ThreadAbortException
                Throw ex
            Catch e As Exception
                Throw VbMakeException(vbErrors.CantCreateObject)
            End Try
        End Function

        '============================================================================
        ' Object/latebound functions.
        '============================================================================
        Public Function CallByName(ByVal ObjectRef As System.Object, ByVal ProcName As String, ByVal UseCallType As CallType, ByVal ParamArray Args() As Object) As Object
            Select Case UseCallType

                Case CallType.Method
                    'Need to use LateGet, because we are returning a value
                    Return CompilerServices.LateBinding.InternalLateCall(ObjectRef, Nothing, ProcName, Args, Nothing, Nothing, False)

                Case CallType.Get
                    Return CompilerServices.LateBinding.LateGet(ObjectRef, Nothing, ProcName, Args, Nothing, Nothing)

                Case CallType.Let,
                     CallType.Set
                    CompilerServices.LateBinding.InternalLateSet(ObjectRef, Nothing, ProcName, Args, Nothing, False, UseCallType)
                    Return Nothing

                Case Else
                    Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue1, "CallType"))
            End Select
        End Function

    End Module

End Namespace

