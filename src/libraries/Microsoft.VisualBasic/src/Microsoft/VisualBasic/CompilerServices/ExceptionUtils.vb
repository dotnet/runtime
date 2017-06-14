' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    Friend Enum vbErrors
        ObjNotSet = 91
        IllegalFor = 92
    End Enum

    ' Implements error utilities for Basic
    Friend NotInheritable Class ExceptionUtils

        ' Prevent creation.
        Private Sub New()
        End Sub

        Friend Shared Function VbMakeIllegalForException() As System.Exception
            Return VbMakeExceptionEx(vbErrors.IllegalFor, GetResourceString(SR.ID92)) ' 92 - IllegaFor
        End Function

        Friend Shared Function VbMakeObjNotSetException() As System.Exception
            Return VbMakeExceptionEx(vbErrors.ObjNotSet, GetResourceString(SR.ID91)) ' 91 - ObjNotSet
        End Function

        Private Shared Function VbMakeExceptionEx(ByVal number As Integer, ByVal sMsg As String) As System.Exception
            Dim vBDefinedError As Boolean

            VbMakeExceptionEx = BuildException(number, sMsg, vBDefinedError)

            If vBDefinedError Then
            End If

        End Function


        Private Shared Function BuildException(ByVal number As Integer, ByVal description As String, ByRef vBDefinedError As Boolean) As System.Exception

            vBDefinedError = True

            Select Case number

                Case vbErrors.ObjNotSet
                    Return New NullReferenceException(description)

                Case Else
                    vBDefinedError = False
                    Return New Exception(description)
            End Select

            vBDefinedError = False
            Return New Exception(description)
        End Function

    End Class

    Public NotInheritable Class InternalErrorException
        Inherits System.Exception

        Public Sub New(ByVal message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(ByVal message As String, ByVal innerException As System.Exception)
            MyBase.New(message, innerException)
        End Sub

        ' default constructor
        Public Sub New()
            MyBase.New(GetResourceString(SR.InternalError))
        End Sub

    End Class

End Namespace
