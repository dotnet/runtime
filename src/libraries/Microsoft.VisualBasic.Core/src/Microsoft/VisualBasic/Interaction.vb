' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic

    Public Module Interaction

        Friend Function IIf(Of T)(ByVal condition As Boolean, ByVal truePart As T, ByVal falsePart As T) As T
            If condition Then
                Return truePart
            End If

            Return falsePart
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

