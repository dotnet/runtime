' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Text
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

        '============================================================================
        ' String functions.
        '============================================================================
        Public Function Choose(ByVal Index As Double, ByVal ParamArray Choice() As Object) As Object

            Dim FixedIndex As Integer = CInt(Fix(Index) - 1) 'ParamArray is 0 based, but Choose assumes 1 based 

            If Choice.Rank <> 1 Then
                Throw New ArgumentException(GetResourceString(SR.Argument_RankEQOne1, "Choice"))
            ElseIf FixedIndex < 0 OrElse FixedIndex > Choice.GetUpperBound(0) Then
                Return Nothing
            End If

            Return Choice(FixedIndex)
        End Function

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

        Public Function Partition(ByVal Number As Long, ByVal Start As Long, ByVal [Stop] As Long, ByVal Interval As Long) As String
            'CONSIDER: Change to use StringBuilder
            Dim Lower As Long
            Dim Upper As Long
            Dim NoUpper As Boolean
            Dim NoLower As Boolean
            Dim Buffer As String = Nothing
            Dim Buffer1 As String
            Dim Buffer2 As String
            Dim Spaces As Long

            'Validate arguments
            If Start < 0 Then
                Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue1, "Start"))
            End If

            If [Stop] <= Start Then
                Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue1, "Stop"))
            End If

            If Interval < 1 Then
                Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue1, "Interval"))
            End If

            'Check for before-first and after-last ranges
            If Number < Start Then
                Upper = Start - 1
                NoLower = True
            ElseIf Number > [Stop] Then
                Lower = [Stop] + 1
                NoUpper = True
            ElseIf Interval = 1 Then 'This is a special case
                Lower = Number
                Upper = Number
            Else
                'Calculate the upper and lower ranges
                'Note the use of Integer division "\" which truncates to whole number
                Lower = ((Number - Start) \ Interval) * Interval + Start
                Upper = Lower + Interval - 1

                'Adjust for first and last ranges
                If Upper > [Stop] Then
                    Upper = [Stop]
                End If

                If Lower < Start Then
                    Lower = Start
                End If
            End If

            'Build-up the string.  Calculate number of spaces needed: VB3 uses Stop + 1.
            'This may seem bogus but it has to be this way for VB3 compatibilty.
            Buffer1 = CStr([Stop] + 1)
            Buffer2 = CStr(Start - 1)

            If Len(Buffer1) > Len(Buffer2) Then
                Spaces = Len(Buffer1)
            Else
                Spaces = Len(Buffer2)
            End If

            'Handle case where Upper is -1 and Stop < 9
            If NoLower Then
                Buffer1 = CStr(Upper)
                If Spaces < Len(Buffer1) Then
                    Spaces = Len(Buffer1)
                End If
            End If

            'Insert lower-end of partition range.
            If NoLower Then
                InsertSpaces(Buffer, Spaces)
            Else
                InsertNumber(Buffer, Lower, Spaces)
            End If

            'Insert the partition 
            Buffer = Buffer & ":"

            'Insert upper-end of partition range
            If NoUpper Then
                InsertSpaces(Buffer, Spaces)
            Else
                InsertNumber(Buffer, Upper, Spaces)
            End If

            Return Buffer
        End Function

        Private Sub InsertSpaces(ByRef Buffer As String, ByVal Spaces As Long)
            Do While Spaces > 0 'consider:  - use stringbuilder
                Buffer = Buffer & " "
                Spaces = Spaces - 1
            Loop
        End Sub

        Private Sub InsertNumber(ByRef Buffer As String, ByVal Num As Long, ByVal Spaces As Long)
            Dim Buffer1 As String 'consider:  - use stringbuilder

            'Convert number to a string
            Buffer1 = CStr(Num)

            'Insert leading spaces
            InsertSpaces(Buffer, Spaces - Len(Buffer1))

            'Append string
            Buffer = Buffer & Buffer1
        End Sub

        Public Function Switch(ByVal ParamArray VarExpr() As Object) As Object
            Dim Elements As Integer
            Dim Index As Integer

            If VarExpr Is Nothing Then
                Return Nothing
            End If

            Elements = VarExpr.Length
            Index = 0

            'Ensure we have an even number of arguments (0 based)
            If (Elements Mod 2) <> 0 Then
                Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue1, "VarExpr"))
            End If

            Do While Elements > 0
                If CBool(VarExpr(Index)) Then
                    Return VarExpr(Index + 1)
                End If

                Index += 2
                Elements -= 2
            Loop

            Return Nothing 'If nothing matched above
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

