' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Runtime.Versioning
Imports Microsoft.Win32

Imports Microsoft.VisualBasic.CompilerServices
Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports System.Diagnostics.CodeAnalysis

Namespace Microsoft.VisualBasic

    Public Module Interaction
        Private m_SortedEnvList As System.Collections.SortedList

        '============================================================================
        ' Application/system interaction functions.
        '============================================================================

        Public Function Shell(ByVal PathName As String, Optional ByVal Style As AppWinStyle = AppWinStyle.MinimizedFocus, Optional ByVal Wait As Boolean = False, Optional ByVal Timeout As Integer = -1) As Integer
            Return DirectCast(InvokeMethod("Shell", PathName, Style, Wait, Timeout), Integer)
        End Function

        Public Sub AppActivate(ByVal ProcessId As Integer)
            InvokeMethod("AppActivateByProcessId", ProcessId)
        End Sub

        Public Sub AppActivate(ByVal Title As String)
            InvokeMethod("AppActivateByTitle", Title)
        End Sub

        Private m_CommandLine As String

        Public Function Command() As String

            If m_CommandLine Is Nothing Then
                Dim s As String = Environment.CommandLine

                'The first element of the array is the .exe name
                '  we must remove this when building the return value
                If (s Is Nothing) OrElse (s.Length = 0) Then
                    Return ""
                End If

                'The following code must remove the application name from the command line
                ' without disturbing the arguments (trailing and embedded spaces)
                '
                'We also need to handle embedded spaces in the application name
                ' as well as skipping over quotations used around embedded spaces within
                ' the application name
                '  examples:
                '       f:\"Program Files"\Microsoft\foo.exe  a b  d   e  f 
                '       "f:\"Program Files"\Microsoft\foo.exe" a b  d   e  f 
                '       f:\Program Files\Microsoft\foo.exe                  a b  d   e  f 
                Dim LengthOfAppName, j As Integer

                'Remove the app name from the arguments
                LengthOfAppName = Environment.GetCommandLineArgs(0).Length

                Do
                    j = s.IndexOf(ChrW(34), j)
                    If j >= 0 AndAlso j <= LengthOfAppName Then
                        s = s.Remove(j, 1)
                    End If
                Loop While (j >= 0 AndAlso j <= LengthOfAppName)

                If j = 0 OrElse j > s.Length Then
                    m_CommandLine = ""
                Else
                    m_CommandLine = LTrim(s.Substring(LengthOfAppName))
                End If
            End If
            Return m_CommandLine
        End Function

        Public Function Environ(ByVal Expression As Integer) As String

            'Validate index - Note that unlike the fx, this is a legacy VB function and the index is 1 based.
            If Expression <= 0 OrElse Expression > 255 Then
                Throw New ArgumentException(SR.Format(SR.Argument_Range1toFF1, "Expression"))
            End If

            If m_SortedEnvList Is Nothing Then
                SyncLock m_EnvironSyncObject
                    If m_SortedEnvList Is Nothing Then
                        'Constructing the sorted environment list is extremely slow, so we keep a copy around. This list must be alphabetized to match vb5/vb6 behavior
                        m_SortedEnvList = New System.Collections.SortedList(Environment.GetEnvironmentVariables())
                    End If
                End SyncLock
            End If

            If Expression > m_SortedEnvList.Count Then
                Return ""
            End If

            Dim EnvVarName As String = m_SortedEnvList.GetKey(Expression - 1).ToString()
            Dim EnvVarValue As String = m_SortedEnvList.GetByIndex(Expression - 1).ToString()
            Return (EnvVarName & "=" & EnvVarValue)
        End Function

        Private m_EnvironSyncObject As New Object

        Public Function Environ(ByVal Expression As String) As String
            Expression = Trim(Expression)

            If Expression.Length = 0 Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Expression"))
            End If

            Return Environment.GetEnvironmentVariable(Expression)
        End Function

        '============================================================================
        ' User interaction functions.
        '============================================================================

        <SupportedOSPlatform("windows")>
        Public Sub Beep()
#If TARGET_WINDOWS Then
            UnsafeNativeMethods.MessageBeep(0)
#Else
            Throw New PlatformNotSupportedException()
#End If
        End Sub

        Public Function InputBox(ByVal Prompt As String, Optional ByVal Title As String = "", Optional ByVal DefaultResponse As String = "", Optional ByVal XPos As Integer = -1, Optional ByVal YPos As Integer = -1) As String
            Return DirectCast(InvokeMethod("InputBox", Prompt, Title, DefaultResponse, XPos, YPos), String)
        End Function

        Public Function MsgBox(ByVal Prompt As Object, Optional ByVal Buttons As MsgBoxStyle = MsgBoxStyle.OkOnly, Optional ByVal Title As Object = Nothing) As MsgBoxResult
            Return DirectCast(InvokeMethod("MsgBox", Prompt, Buttons, Title), MsgBoxResult)
        End Function

        <UnconditionalSuppressMessage("ReflectionAnalsys", "IL2075:UnrecognizedReflectionPattern",
                Justification:="Trimmer warns because it can't see Microsoft.VisualBasic.Forms. If the assembly is there, the trimmer will be able to tell to preserve the method specified.")>
        Private Function InvokeMethod(methodName As String, ParamArray args As Object()) As Object
            Dim type As Type = Type.GetType("Microsoft.VisualBasic._Interaction, Microsoft.VisualBasic.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError:=False)
            Dim method As MethodInfo = type?.GetMethod(methodName)
            If method Is Nothing Then
                Throw New PlatformNotSupportedException(SR.MethodRequiresSystemWindowsForms)
            End If
            Return method.Invoke(Nothing, BindingFlags.DoNotWrapExceptions, Nothing, args, Nothing)
        End Function

        '============================================================================
        ' String functions.
        '============================================================================
        Public Function Choose(ByVal Index As Double, ByVal ParamArray Choice() As Object) As Object

            Dim FixedIndex As Integer = CInt(Fix(Index) - 1) 'ParamArray is 0 based, but Choose assumes 1 based 

            If Choice.Rank <> 1 Then
                Throw New ArgumentException(SR.Format(SR.Argument_RankEQOne1, "Choice"))
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
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Start"))
            End If

            If [Stop] <= Start Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Stop"))
            End If

            If Interval < 1 Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Interval"))
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
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "VarExpr"))
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

        '============================================================================
        ' Registry functions.
        '============================================================================

        <SupportedOSPlatform("windows")>
        Public Sub DeleteSetting(ByVal AppName As String, Optional ByVal Section As String = Nothing, Optional ByVal Key As String = Nothing)
            Dim AppSection As String
            Dim UserKey As RegistryKey
            Dim AppSectionKey As RegistryKey = Nothing

            CheckPathComponent(AppName)
            AppSection = FormRegKey(AppName, Section)

            Try
                UserKey = Registry.CurrentUser

                If IsNothing(Key) OrElse (Key.Length = 0) Then
                    UserKey.DeleteSubKeyTree(AppSection)
                Else
                    AppSectionKey = UserKey.OpenSubKey(AppSection, True)
                    If AppSectionKey Is Nothing Then
                        Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Section"))
                    End If

                    AppSectionKey.DeleteValue(Key)
                End If

            Catch ex As Exception
                Throw ex
            Finally
                If AppSectionKey IsNot Nothing Then
                    AppSectionKey.Close()
                End If
            End Try
        End Sub

        <SupportedOSPlatform("windows")>
        Public Function GetAllSettings(ByVal AppName As String, ByVal Section As String) As String(,)
            Dim rk As RegistryKey
            Dim sAppSect As String
            Dim i As Integer
            Dim lUpperBound As Integer
            Dim sValueNames() As String
            Dim sValues(,) As String
            Dim o As Object
            Dim sName As String

            ' Check for empty string in path
            CheckPathComponent(AppName)
            CheckPathComponent(Section)
            sAppSect = FormRegKey(AppName, Section)
            rk = Registry.CurrentUser.OpenSubKey(sAppSect)


            If rk Is Nothing Then
                Return Nothing
            End If

            GetAllSettings = Nothing
            Try
                If rk.ValueCount <> 0 Then
                    sValueNames = rk.GetValueNames()
                    lUpperBound = sValueNames.GetUpperBound(0)
                    ReDim sValues(lUpperBound, 1)

                    For i = 0 To lUpperBound
                        sName = sValueNames(i)

                        'Assign name
                        sValues(i, 0) = sName

                        'Assign value
                        o = rk.GetValue(sName)

                        If (Not o Is Nothing) AndAlso (TypeOf o Is String) Then
                            sValues(i, 1) = o.ToString()
                        End If
                    Next i

                    GetAllSettings = sValues
                End If

            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex

            Catch ex As Exception
                'Consume the exception

            Finally
                rk.Close()
            End Try
        End Function

        <SupportedOSPlatform("windows")>
        Public Function GetSetting(ByVal AppName As String, ByVal Section As String, ByVal Key As String, Optional ByVal [Default] As String = "") As String
            Dim rk As RegistryKey = Nothing
            Dim sAppSect As String
            Dim o As Object

            'Check for empty strings
            CheckPathComponent(AppName)
            CheckPathComponent(Section)
            CheckPathComponent(Key)
            If [Default] Is Nothing Then
                [Default] = ""
            End If

            'Open the sub key
            sAppSect = FormRegKey(AppName, Section)
            Try
                rk = Registry.CurrentUser.OpenSubKey(sAppSect)    'By default, does not request write permission

                'Get the key's value
                If rk Is Nothing Then
                    Return [Default]
                End If

                o = rk.GetValue(Key, [Default])
            Finally
                If rk IsNot Nothing Then
                    rk.Close()
                End If
            End Try

            If o Is Nothing Then
                Return Nothing
            ElseIf TypeOf o Is String Then ' - odd that this is required to be a string when it isn't in GetAllSettings() above...
                Return DirectCast(o, String)
            Else
                Throw New ArgumentException(SR.Argument_InvalidValue)
            End If
        End Function

        <SupportedOSPlatform("windows")>
        Public Sub SaveSetting(ByVal AppName As String, ByVal Section As String, ByVal Key As String, ByVal Setting As String)
            Dim rk As RegistryKey
            Dim sIniSect As String

            ' Check for empty string in path
            CheckPathComponent(AppName)
            CheckPathComponent(Section)
            CheckPathComponent(Key)

            sIniSect = FormRegKey(AppName, Section)
            rk = Registry.CurrentUser.CreateSubKey(sIniSect)

            If rk Is Nothing Then
                'Subkey could not be created
                Throw New ArgumentException(SR.Format(SR.Interaction_ResKeyNotCreated1, sIniSect))
            End If

            Try
                rk.SetValue(Key, Setting)
            Catch ex As Exception
                Throw ex
            Finally
                rk.Close()
            End Try
        End Sub

        '============================================================================
        ' Private functions.
        '============================================================================
        Private Function FormRegKey(ByVal sApp As String, ByVal sSect As String) As String
            Const REGISTRY_INI_ROOT As String = "Software\VB and VBA Program Settings"
            'Forms the string for the key value
            If IsNothing(sApp) OrElse (sApp.Length = 0) Then
                FormRegKey = REGISTRY_INI_ROOT
            ElseIf IsNothing(sSect) OrElse (sSect.Length = 0) Then
                FormRegKey = REGISTRY_INI_ROOT & "\" & sApp
            Else
                FormRegKey = REGISTRY_INI_ROOT & "\" & sApp & "\" & sSect
            End If
        End Function

        Private Sub CheckPathComponent(ByVal s As String)
            If (s Is Nothing) OrElse (s.Length = 0) Then
                Throw New ArgumentException(SR.Argument_PathNullOrEmpty)
            End If
        End Sub

        <SupportedOSPlatform("windows")>
        <RequiresUnreferencedCode("The COM object to be created cannot be statically analyzed and may be trimmed")>
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
                If String.Equals(Environment.MachineName, ServerName, StringComparison.OrdinalIgnoreCase) Then
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
            Catch e As Exception
                Throw VbMakeException(vbErrors.CantCreateObject)
            End Try
        End Function

        <SupportedOSPlatform("windows")>
        <RequiresUnreferencedCode("The COM component to be returned cannot be statically analyzed and may be trimmed")>
        Public Function GetObject(Optional ByVal PathName As String = Nothing, Optional ByVal [Class] As String = Nothing) As Object
            'Only works for Com2 objects, not for COM+ objects.

            If Len([Class]) = 0 Then
                Try
                    Return Marshal.BindToMoniker([PathName])
                Catch ex As StackOverflowException
                    Throw ex
                Catch ex As OutOfMemoryException
                    Throw ex
                Catch
                    Throw VbMakeException(vbErrors.CantCreateObject)
                End Try
            Else
                If PathName Is Nothing Then
                    Return Nothing
                ElseIf Len(PathName) = 0 Then
                    Try
                        Dim t As Type = Type.GetTypeFromProgID([Class])
                        Return System.Activator.CreateInstance(t)
                    Catch ex As StackOverflowException
                        Throw ex
                    Catch ex As OutOfMemoryException
                        Throw ex
                    Catch
                        Throw VbMakeException(vbErrors.CantCreateObject)
                    End Try
                Else
                    Return Nothing
                End If
            End If
        End Function

        '============================================================================
        ' Object/latebound functions.
        '============================================================================
        <RequiresUnreferencedCode("The type of ObjectRef cannot be statically analyzed and its members may be trimmed.")>
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
                    Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "CallType"))
            End Select
        End Function

    End Module

End Namespace

