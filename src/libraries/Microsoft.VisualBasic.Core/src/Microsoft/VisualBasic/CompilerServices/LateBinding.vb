' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Diagnostics
Imports System.Globalization
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Runtime.Remoting

Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public NotInheritable Class LateBinding
        ' Prevent creation.
        Private Sub New()
        End Sub

        Private Const DefaultCallType As CallType = CType(0, CallType)

        Private Shared Function GetMostDerivedMemberInfo(ByVal objIReflect As IReflect, ByVal name As String, ByVal flags As BindingFlags) As MemberInfo
            Dim mi() As MemberInfo
            Dim i As Integer
            Dim Selected As MemberInfo

            ' Filter out generic methods for compatibility with the Whidbey framework
            mi = GetNonGenericMembers(objIReflect.GetMember(name, flags))

            If mi Is Nothing OrElse mi.Length = 0 Then
                Return Nothing
            End If

            'Only the outermost definition can be called
            Selected = mi(0)
            For i = 1 To mi.GetUpperBound(0)
                If mi(i).DeclaringType.IsSubclassOf(Selected.DeclaringType) Then
                    Selected = mi(i)
                End If
            Next i

            Return Selected

        End Function

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Function LateGet(ByVal o As Object,
                                ByVal objType As Type,
                                ByVal name As String,
                                ByVal args() As Object,
                                ByVal paramnames() As String,
                                ByVal CopyBack() As Boolean) As Object

            Dim flags As BindingFlags

            flags = BindingFlags.IgnoreCase Or
                    BindingFlags.GetProperty Or
                    BindingFlags.InvokeMethod Or
                    BindingFlags.FlattenHierarchy Or
                    BindingFlags.OptionalParamBinding Or
                    BindingFlags.Static Or
                    BindingFlags.Instance Or
                    BindingFlags.Public


            If objType Is Nothing Then
                If o Is Nothing Then
                    Throw VbMakeException(vbErrors.ObjNotSet)
                End If

                objType = o.GetType()
            End If

            Dim objIReflect As IReflect = GetCorrectIReflect(o, objType)

            If name Is Nothing Then
                name = ""
            End If


            If objType.IsCOMObject Then
                CheckForClassExtendingCOMClass(objType)

            Else
                'Fields must be checked for and returned here
                '  shadowing causes behavior that must be
                Dim mi As MemberInfo = GetMostDerivedMemberInfo(objIReflect, name, flags Or BindingFlags.GetField)
                If (Not mi Is Nothing) AndAlso (mi.MemberType = MemberTypes.Field) Then
                    'SECURITY CHECK
                    VBBinder.SecurityCheckForLateboundCalls(mi, objType, objIReflect)
                    'SECURITY CHECK

                    ' If we use the System.Type's IReflect Implementation
                    Dim ValueOfField As Object
                    If objType Is objIReflect OrElse CType(mi, FieldInfo).IsStatic OrElse
                       DoesTargetObjectMatch(o, mi) Then

                        VerifyObjRefPresentForInstanceCall(o, mi)

                        ValueOfField = CType(mi, FieldInfo).GetValue(o)
                    Else
                        ValueOfField = InvokeMemberOnIReflect(objIReflect, mi, BindingFlags.GetField, o, Nothing)
                    End If

                    If (args Is Nothing OrElse args.Length = 0) Then
                        Return ValueOfField
                    Else
                        Return LateIndexGet(ValueOfField, args, paramnames)
                    End If
                End If
            End If

            Dim binder As VBBinder

            binder = New VBBinder(CopyBack)

            Try
                Return binder.InvokeMember(name, flags, objType, objIReflect, o, args, paramnames)

                '
                '
                ' There may be a property or field that returns an array or object with a default member
                ' We get the field or property then try using a LateIndexGet
            Catch ex As Exception When IsMissingMemberException(ex)

                If objType.IsCOMObject() OrElse ((Not args Is Nothing) AndAlso (args.Length > 0)) Then
                    Dim oTmp As Object

                    flags = BindingFlags.IgnoreCase Or
                            BindingFlags.GetProperty Or
                            BindingFlags.InvokeMethod Or
                            BindingFlags.FlattenHierarchy Or
                            BindingFlags.OptionalParamBinding Or
                            BindingFlags.Static Or
                            BindingFlags.Instance Or
                            BindingFlags.Public

                    If Not objType.IsCOMObject() Then
                        flags = flags Or BindingFlags.GetField
                    End If

                    Try
                        oTmp = binder.InvokeMember(name, flags, objType, objIReflect, o, Nothing, Nothing)
                    Catch exInner As AccessViolationException
                        Throw exInner
                    Catch exInner As StackOverflowException
                        Throw exInner
                    Catch exInner As OutOfMemoryException
                        Throw exInner
                    Catch exInner As System.Threading.ThreadAbortException
                        Throw exInner
                    Catch
                        oTmp = Nothing
                    End Try
                    If oTmp Is Nothing Then
                        Throw New MissingMemberException(GetResourceString(SR.MissingMember_MemberNotFoundOnType2, name, VBFriendlyName(objType, o)))
                    Else
                        Try
                            Return LateIndexGet(oTmp, args, paramnames)
                        Catch exInner As Exception When IsMissingMemberException(exInner) AndAlso (TypeOf ex Is MissingMemberException)
                            Throw ex
                        End Try
                    End If
                Else
                    Throw New MissingMemberException(GetResourceString(SR.MissingMember_MemberNotFoundOnType2, name, VBFriendlyName(objType, o)))
                End If

            Catch ex As TargetInvocationException
                Throw ex.InnerException

            End Try

        End Function

        Private Shared Function IsMissingMemberException(ByVal ex As Exception) As Boolean

            If TypeOf ex Is MissingMemberException Then
                Return True

            ElseIf TypeOf ex Is MemberAccessException Then
                Return True

            Else
                Dim cex As COMException = TryCast(ex, COMException)

                If cex IsNot Nothing Then
                    If cex.ErrorCode = DISP_E_UNKNOWNNAME Then
                        Return True

                    ElseIf cex.ErrorCode = (SEVERITY_ERROR Or FACILITY_CONTROL Or 438) Then
                        Return True
                    End If

                ElseIf TypeOf ex Is TargetInvocationException AndAlso
                        (TypeOf ex.InnerException Is COMException AndAlso
                        CType(ex.InnerException, COMException).ErrorCode = DISP_E_NOTACOLLECTION) Then
                    Return True

                End If
            End If

            Return False

        End Function

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Sub LateSetComplex(ByVal o As Object, ByVal objType As Type, ByVal name As String,
                                  ByVal args() As Object, ByVal paramnames() As String,
                                  ByVal OptimisticSet As Boolean, ByVal RValueBase As Boolean)

            Try
                InternalLateSet(o, objType, name, args, paramnames, OptimisticSet, DefaultCallType)

                If RValueBase AndAlso objType.IsValueType Then
                    'note that objType is passed byref to InternalLateSet and that it 
                    'should be valid by the time we get to this point
                    Throw New Exception(GetResourceString(SR.RValueBaseForValueType, VBFriendlyName(objType, o), VBFriendlyName(objType, o)))
                End If
            Catch ex As System.MissingMemberException When OptimisticSet = True
                'A missing member exception means it has no Set member.  Silently handle the exception.
            End Try

        End Sub

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Sub LateSet(ByVal o As Object, ByVal objType As Type, ByVal name As String,
                           ByVal args() As Object, ByVal paramnames() As String)

            InternalLateSet(o, objType, name, args, paramnames, False, DefaultCallType)

        End Sub

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Friend Shared Sub InternalLateSet(ByVal o As Object,
                                   ByRef objType As Type,
                                   ByVal name As String,
                                   ByVal args() As Object,
                                   ByVal paramnames() As String,
                                   ByVal OptimisticSet As Boolean,
                                   ByVal UseCallType As CallType)

            Dim flags As BindingFlags
            Dim binder As VBBinder

            flags = BindingFlags.IgnoreCase Or
                    BindingFlags.FlattenHierarchy Or
                    BindingFlags.OptionalParamBinding Or
                    BindingFlags.Static Or
                    BindingFlags.Instance Or
                    BindingFlags.Public

            If objType Is Nothing Then
                If o Is Nothing Then
                    Throw VbMakeException(vbErrors.ObjNotSet)
                End If

                objType = o.GetType()
            End If

            Dim objIReflect As IReflect = GetCorrectIReflect(o, objType)

            If name Is Nothing Then
                name = ""
            End If

            If objType.IsCOMObject() Then
                CheckForClassExtendingCOMClass(objType)
                If UseCallType = CallType.Set Then
                    flags = flags Or BindingFlags.PutRefDispProperty
                    If args(args.GetUpperBound(0)) Is Nothing Then
#Disable Warning BC40000 ' DispatchWrapper is marked obsolete.
                        args(args.GetUpperBound(0)) = New DispatchWrapper(Nothing)
#Enable Warning BC40000
                    End If
                ElseIf UseCallType = CallType.Let Then
                    flags = flags Or BindingFlags.PutDispProperty
                Else
                    flags = flags Or GetPropertyPutFlags(args(args.GetUpperBound(0)))
                End If
            Else
                flags = flags Or BindingFlags.SetProperty
                'Fields must be checked for and returned here
                '  shadowing causes behavior that must be 
                Dim mi As MemberInfo = GetMostDerivedMemberInfo(objIReflect, name, flags Or BindingFlags.SetField)
                If (Not mi Is Nothing) AndAlso (mi.MemberType = MemberTypes.Field) Then
                    Dim fi As FieldInfo = CType(mi, FieldInfo)
                    Dim NewValue As Object

                    If fi.IsInitOnly Then
                        Throw New MissingMemberException(GetResourceString(SR.MissingMember_ReadOnlyField2, name, VBFriendlyName(objType, o)))
                    End If

                    If (args Is Nothing OrElse args.Length = 0) Then
                        'Everything must be shadowed
                        Throw New MissingMemberException(GetResourceString(SR.MissingMember_MemberNotFoundOnType2, name, VBFriendlyName(objType, o)))

                    ElseIf args.Length = 1 Then
                        NewValue = args(0)
                        'SECURITY CHECK
                        VBBinder.SecurityCheckForLateboundCalls(fi, objType, objIReflect)
                        'SECURITY CHECK

                        Dim FieldValue As Object
                        If NewValue Is Nothing Then
                            FieldValue = Nothing
                        Else
                            FieldValue = ObjectType.CTypeHelper(args(0), fi.FieldType)
                        End If

                        If objType Is objIReflect OrElse fi.IsStatic OrElse
                           DoesTargetObjectMatch(o, fi) Then

                            VerifyObjRefPresentForInstanceCall(o, fi)

                            fi.SetValue(o, FieldValue)
                        Else
                            InvokeMemberOnIReflect(objIReflect, fi, BindingFlags.SetField, o, New Object() {FieldValue})
                        End If

                        Return

                    ElseIf args.Length > 1 Then
                        'SECURITY CHECK
                        VBBinder.SecurityCheckForLateboundCalls(mi, objType, objIReflect)
                        'SECURITY CHECK

                        Dim FieldValue As Object = Nothing
                        If objType Is objIReflect OrElse CType(mi, FieldInfo).IsStatic OrElse
                           DoesTargetObjectMatch(o, mi) Then

                            VerifyObjRefPresentForInstanceCall(o, mi)

                            FieldValue = CType(mi, FieldInfo).GetValue(o)

                        Else
                            FieldValue = InvokeMemberOnIReflect(objIReflect, mi, BindingFlags.GetField, o, New Object() {FieldValue})
                        End If

                        LateIndexSet(FieldValue, args, paramnames)
                        Return

                    End If

                End If
            End If

            binder = New VBBinder(Nothing)

            If (OptimisticSet AndAlso args.GetUpperBound(0) > 0) Then
                'Check for an overloaded property.  
                '  We need to see what property needs to be set
                '  overloaded properties can cause problems because
                '  of ReadOnly/WriteOnly
                Dim pi As PropertyInfo
                Dim propflags As BindingFlags =
                                BindingFlags.GetProperty Or
                                BindingFlags.IgnoreCase Or
                                BindingFlags.FlattenHierarchy Or
                                BindingFlags.OptionalParamBinding Or
                                BindingFlags.Static Or
                                BindingFlags.Instance Or
                                BindingFlags.Public

                Dim indexTypes As System.Type()
                Dim i As Integer
                Dim oArg As Object

                indexTypes = New System.Type(args.GetUpperBound(0) - 1) {}

                For i = 0 To indexTypes.GetUpperBound(0)
                    oArg = args(i)
                    If oArg Is Nothing Then
                        indexTypes(i) = Nothing
                    Else
                        indexTypes(i) = oArg.GetType()
                    End If
                Next i
                Try
                    pi = objIReflect.GetProperty(name, propflags, binder, GetType(Integer), indexTypes, Nothing)
                    If pi Is Nothing OrElse (Not pi.CanWrite) Then
                        'Property has no setter, so bail
                        Return
                    End If
                Catch ex As MissingMemberException
                    'No set for this
                    Return
                End Try
            End If

            Try
                binder.InvokeMember(name, flags, objType, objIReflect, o, args, paramnames)
            Catch ex As Exception When IsMissingMemberException(ex)
                ' There may be a property or field that returns an array or object with a default member
                ' We get the field or property then try using a LateIndexGet
                If (Not args Is Nothing) AndAlso (args.Length > 1) Then
                    Dim oTmp As Object

                    flags = BindingFlags.IgnoreCase Or
                            BindingFlags.GetProperty Or
                            BindingFlags.FlattenHierarchy Or
                            BindingFlags.OptionalParamBinding Or
                            BindingFlags.Static Or
                            BindingFlags.Instance Or
                            BindingFlags.Public

                    If Not objType.IsCOMObject() Then
                        flags = flags Or BindingFlags.GetField
                    End If


                    Try
                        oTmp = binder.InvokeMember(name, flags, objType, objIReflect, o, Nothing, Nothing)
                    Catch exInner As Exception When IsMissingMemberException(exInner) AndAlso (TypeOf ex Is MissingMemberException)
                        'Throw the exception thrown by VBBinder.InvokeMember
                        Throw ex
                    Catch exInner As AccessViolationException
                        Throw exInner
                    Catch exInner As StackOverflowException
                        Throw exInner
                    Catch exInner As OutOfMemoryException
                        Throw exInner
                    Catch exInner As System.Threading.ThreadAbortException
                        Throw exInner
                    Catch exInner As Exception
                        oTmp = Nothing
                    End Try

                    If oTmp Is Nothing Then
                        Throw New MissingMemberException(GetResourceString(SR.MissingMember_MemberNotFoundOnType2, name, VBFriendlyName(objType, o)))
                    Else
                        Try
                            LateIndexSet(oTmp, args, paramnames)
                        Catch exInner As Exception When IsMissingMemberException(exInner) AndAlso (TypeOf ex Is MissingMemberException)
                            Throw ex
                        End Try
                    End If
                Else
                    Throw New MissingMemberException(GetResourceString(SR.MissingMember_MemberNotFoundOnType2, name, VBFriendlyName(objType, o)))
                End If

            Catch ex As TargetInvocationException
                If ex.InnerException Is Nothing Then
                    Throw ex
                ElseIf TypeOf ex.InnerException Is TargetParameterCountException Then
                    If (flags And BindingFlags.PutRefDispProperty) <> 0 Then
                        'Set was being called
                        Throw New MissingMemberException(GetResourceString(SR.MissingMember_MemberSetNotFoundOnType2, name, VBFriendlyName(objType, o)))
                    Else
                        'Let was being called
                        Throw New MissingMemberException(GetResourceString(SR.MissingMember_MemberLetNotFoundOnType2, name, VBFriendlyName(objType, o)))
                    End If
                Else
                    Throw ex.InnerException
                End If
            End Try

        End Sub

        Private Shared Sub CheckForClassExtendingCOMClass(ByVal objType As Type)

            If Not objType.IsCOMObject OrElse objType.FullName = "System.__ComObject" Then
                Return
            End If

            If objType.BaseType.FullName = "System.__ComObject" Then
                Return
            End If
            Throw New InvalidOperationException(GetResourceString(SR.LateboundCallToInheritedComClass))

        End Sub

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Function LateIndexGet(ByVal o As Object, ByVal args() As Object, ByVal paramnames() As String) As Object

            Dim objType As Type
            Dim binder As VBBinder
            Dim DefaultName As String = Nothing

            If o Is Nothing Then
                Throw VbMakeException(vbErrors.ObjNotSet)
            End If

            objType = o.GetType()

            Dim objIReflect As IReflect = GetCorrectIReflect(o, objType)

            If objType.IsArray() Then

                'Named arguments are not allowed as indexers        
                If paramnames IsNot Nothing AndAlso paramnames.Length <> 0 Then
                    Throw New ArgumentException(GetResourceString(SR.Argument_InvalidNamedArgs))
                End If

                Dim ArgCount As Integer
                Dim ary As Array

                ary = CType(o, Array)

                'Optimized cases
                ArgCount = args.Length

                'Check for valid dimensions
                If ArgCount <> ary.Rank Then
                    Throw New RankException
                End If

                If ArgCount = 1 Then
                    Return ary.GetValue(CInt(args(0)))
                ElseIf ArgCount = 2 Then
                    Return ary.GetValue(CInt(args(0)), CInt(args(1)))
                Else
                    Dim IndexArray() As Integer
                    Dim ArgIndex As Integer

                    ReDim IndexArray(ArgCount - 1)

                    For ArgIndex = 0 To ArgCount - 1
                        IndexArray(ArgIndex) = CInt(args(ArgIndex))
                    Next ArgIndex

                    Return ary.GetValue(IndexArray)
                End If
            Else
                Dim flags As BindingFlags
                Dim member As MemberInfo
                Dim members As MemberInfo()
                Dim match As MethodBase() = Nothing

                flags = BindingFlags.IgnoreCase Or
                        BindingFlags.GetProperty Or
                        BindingFlags.InvokeMethod Or
                        BindingFlags.FlattenHierarchy Or
                        BindingFlags.OptionalParamBinding Or
                        BindingFlags.Instance Or
                        BindingFlags.Static Or
                        BindingFlags.Public

                If Not objType.IsCOMObject() Then
                    If (args Is Nothing OrElse args.Length = 0) Then
                        'how can this be?  how can we have an indexed late get with no arguments?
                        flags = flags Or BindingFlags.GetField
                    End If

                    Dim i, iNext As Integer

                    members = GetDefaultMembers(objType, objIReflect, DefaultName)

                    If Not members Is Nothing Then
                        For i = 0 To members.GetUpperBound(0)
                            member = members(i)
                            If member.MemberType = MemberTypes.Property Then
                                member = CType(member, PropertyInfo).GetGetMethod()
                            End If

                            If Not member Is Nothing AndAlso member.MemberType <> MemberTypes.Field Then
                                members(iNext) = member
                                iNext += 1
                            End If
                        Next i
                    End If

                    'Catch the missing method here, Invoke below will throw an ArgumentException
                    If members Is Nothing Or iNext = 0 Then
                        Throw New MissingMemberException(GetResourceString(SR.MissingMember_NoDefaultMemberFound1, VBFriendlyName(objType, o)))
                    End If
                    match = New MethodBase(iNext - 1) {}
                    For i = 0 To iNext - 1
                        Try
                            match(i) = CType(members(i), MethodBase)
                        Catch ex As StackOverflowException
                            Throw ex
                        Catch ex As OutOfMemoryException
                            Throw ex
                        Catch ex As System.Threading.ThreadAbortException
                            Throw ex
                        Catch ex As Exception
                            ' If this assert is triggered due to an AccessViolationException,
                            ' report a bug to the CLR.
                            Debug.Assert(False, ex.Message)
                        End Try
                    Next i

                Else
                    CheckForClassExtendingCOMClass(objType)
                End If

                binder = New VBBinder(Nothing)

                Try
                    If objType.IsCOMObject() Then
                        Return binder.InvokeMember("", flags, objType, objIReflect, o, args, paramnames)

                    Else
                        Dim state As Object = Nothing
                        Dim retValue As Object
                        Dim method As MethodBase

                        'Give binder necessary information for creating error text
                        binder.m_BindToName = DefaultName
                        binder.m_objType = objType
                        method = binder.BindToMethod(flags, match, args, Nothing, Nothing, paramnames, state)

                        '
                        'BEGIN: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
                        '
                        VBBinder.SecurityCheckForLateboundCalls(method, objType, objIReflect)

                        If objType Is objIReflect OrElse method.IsStatic OrElse
                           DoesTargetObjectMatch(o, method) Then

                            VerifyObjRefPresentForInstanceCall(o, method)

                            retValue = method.Invoke(o, args)

                        Else
                            retValue = InvokeMemberOnIReflect(objIReflect, method, BindingFlags.InvokeMethod, o, args)
                        End If

                        '
                        'END: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
                        '
                        binder.ReorderArgumentArray(args, state)
                        Return retValue

                    End If

                Catch ex As Exception When IsMissingMemberException(ex)
                    Throw New MissingMemberException(GetResourceString(SR.MissingMember_NoDefaultMemberFound1, VBFriendlyName(objType, o)))

                Catch ex As TargetInvocationException
                    Throw ex.InnerException
                End Try
            End If
        End Function

        Private Shared Function GetDefaultMembers(ByVal typ As Type, ByVal objIReflect As IReflect, ByRef DefaultName As String) As MemberInfo()

            Dim attributeList As Object()
            Dim members As MemberInfo()

            If typ Is objIReflect Then
                Do
                    attributeList = typ.GetCustomAttributes(GetType(DefaultMemberAttribute), False)
                    If (Not attributeList Is Nothing) AndAlso (attributeList.Length <> 0) Then
                        DefaultName = CType(attributeList(0), DefaultMemberAttribute).MemberName
                        members = typ.GetMember(DefaultName, BindingFlags.IgnoreCase Or
                                                            BindingFlags.FlattenHierarchy Or
                                                            BindingFlags.Instance Or
                                                            BindingFlags.Static Or
                                                            BindingFlags.Public)

                        ' Filter out generic methods for compatibility with the Whidbey framework
                        members = GetNonGenericMembers(members)

                        If members Is Nothing OrElse members.Length = 0 Then
                            DefaultName = ""
                            Return Nothing
                        End If
                        Return members
                    End If
                    typ = typ.BaseType
                Loop While (Not typ Is Nothing)

                DefaultName = ""

                Return Nothing
            Else

                members = objIReflect.GetMember("", BindingFlags.IgnoreCase Or
                                          BindingFlags.FlattenHierarchy Or
                                          BindingFlags.Instance Or
                                          BindingFlags.Static Or
                                          BindingFlags.Public)

                ' Filter out generic methods for compatibility with the Whidbey framework
                members = GetNonGenericMembers(members)

                If members Is Nothing OrElse members.Length = 0 Then
                    DefaultName = ""
                    Return Nothing
                End If

                DefaultName = members(0).Name
                Return members
            End If

        End Function

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Sub LateIndexSetComplex(ByVal o As Object, ByVal args() As Object, ByVal paramnames() As String,
                                       ByVal OptimisticSet As Boolean, ByVal RValueBase As Boolean)

            Try
                LateIndexSet(o, args, paramnames)

                If RValueBase AndAlso o.GetType().IsValueType Then
                    Throw New Exception(GetResourceString(SR.RValueBaseForValueType, o.GetType().Name, o.GetType().Name))
                End If
            Catch ex As System.MissingMemberException When OptimisticSet = True
                'A missing member exception means it has no Set member.  Silently handle the exception.
            End Try
        End Sub

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Sub LateIndexSet(ByVal o As Object, ByVal args() As Object, ByVal paramnames() As String)

            Dim objType As Type
            Dim DefaultName As String = Nothing

            If o Is Nothing Then
                Throw VbMakeException(vbErrors.ObjNotSet)
            End If

            objType = o.GetType()

            Dim objIReflect As IReflect = GetCorrectIReflect(o, objType)

            If objType.IsArray Then

                'Named arguments are not allowed as indexers        
                If paramnames IsNot Nothing AndAlso paramnames.Length <> 0 Then
                    Throw New ArgumentException(GetResourceString(SR.Argument_InvalidNamedArgs))
                End If

                Dim ArgCount As Integer
                Dim ary As Array
                Dim NewValue As Object

                ary = CType(o, Array)

                'Optimized cases
                ArgCount = args.Length - 1

                NewValue = args(ArgCount)
                If Not NewValue Is Nothing Then
                    Dim elemType As Type
                    elemType = objType.GetElementType()

                    'Check that the type is valid for the assignment
                    If Not NewValue.GetType() Is elemType Then
                        NewValue = ObjectType.CTypeHelper(NewValue, elemType)
                    End If
                End If

                'Check for valid dimensions
                If ArgCount <> ary.Rank Then
                    Throw New RankException
                End If

                If ArgCount = 1 Then
                    ary.SetValue(NewValue, CInt(args(0)))

                ElseIf ArgCount = 2 Then
                    ary.SetValue(NewValue, CInt(args(0)), CInt(args(1)))

                Else
                    Dim IndexArray() As Integer
                    Dim ArgIndex As Integer
                    ReDim IndexArray(ArgCount - 1)

                    For ArgIndex = 0 To ArgCount - 1
                        IndexArray(ArgIndex) = CInt(args(ArgIndex))
                    Next ArgIndex

                    ary.SetValue(NewValue, IndexArray)
                End If
            Else
                Dim flags As BindingFlags
                Dim member As MemberInfo
                Dim members As MemberInfo()
                Dim match As MethodBase() = Nothing

                flags = BindingFlags.IgnoreCase Or
                        BindingFlags.FlattenHierarchy Or
                        BindingFlags.OptionalParamBinding Or
                        BindingFlags.Instance Or
                        BindingFlags.Static Or
                        BindingFlags.Public

                If objType.IsCOMObject() Then
                    CheckForClassExtendingCOMClass(objType)
                    flags = flags Or GetPropertyPutFlags(args(args.GetUpperBound(0)))

                Else
                    flags = flags Or BindingFlags.SetProperty
                    If (args.Length = 1) Then
                        flags = flags Or BindingFlags.SetField
                    End If

                    Dim i, iNext As Integer

                    members = GetDefaultMembers(objType, objIReflect, DefaultName)

                    If Not members Is Nothing Then
                        For i = 0 To members.GetUpperBound(0)
                            member = members(i)
                            If member.MemberType = MemberTypes.Property Then
                                member = CType(member, PropertyInfo).GetSetMethod()
                            End If

                            If Not member Is Nothing AndAlso member.MemberType <> MemberTypes.Field Then
                                members(iNext) = member
                                iNext += 1
                            End If
                        Next i
                    End If

                    'Catch the missing method here, Invoke below will throw an ArgumentException
                    If members Is Nothing Or iNext = 0 Then
                        Throw New MissingMemberException(GetResourceString(SR.MissingMember_NoDefaultMemberFound1, VBFriendlyName(objType, o)))
                    End If

                    match = New MethodBase(iNext - 1) {}
                    For i = 0 To iNext - 1
                        Try
                            match(i) = CType(members(i), MethodBase)
                        Catch ex As StackOverflowException
                            Throw ex
                        Catch ex As OutOfMemoryException
                            Throw ex
                        Catch ex As System.Threading.ThreadAbortException
                            Throw ex
                        Catch
                            ' If this assert is triggered because of an AccessViolation exception,
                            ' report a bug to the CLR.
                            Debug.Assert(False)
                        End Try
                    Next i
                End If

                Dim binder As VBBinder
                binder = New VBBinder(Nothing)
                Try
                    If objType.IsCOMObject Then
                        binder.InvokeMember("", flags, objType, objIReflect, o, args, paramnames)
                    Else
                        Dim state As Object = Nothing
                        Dim method As MethodBase

                        binder.m_BindToName = DefaultName
                        binder.m_objType = objType
                        method = binder.BindToMethod(flags, match, args, Nothing, Nothing, paramnames, state)

                        '
                        'BEGIN: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
                        '
                        VBBinder.SecurityCheckForLateboundCalls(method, objType, objIReflect)

                        If objType Is objIReflect OrElse method.IsStatic OrElse
                           DoesTargetObjectMatch(o, method) Then

                            VerifyObjRefPresentForInstanceCall(o, method)

                            method.Invoke(o, args)

                        Else
                            InvokeMemberOnIReflect(objIReflect, method, BindingFlags.InvokeMethod, o, args)
                        End If

                        '
                        'END: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
                        '

                        binder.ReorderArgumentArray(args, state)

                    End If

                Catch ex As Exception When IsMissingMemberException(ex)
                    'Override the message so that we're consistent with above Throw
                    Throw New MissingMemberException(GetResourceString(SR.MissingMember_NoDefaultMemberFound1, VBFriendlyName(objType, o)))

                Catch ex As TargetInvocationException
                    Throw ex.InnerException
                End Try
            End If
        End Sub

        Private Shared Function GetPropertyPutFlags(ByVal NewValue As Object) As BindingFlags
            If (NewValue Is Nothing) Then
                Return BindingFlags.SetProperty
#Disable Warning BC40000 ' CurrencyWrapper is marked obsolete.
            ElseIf (TypeOf NewValue Is System.ValueType) OrElse
                (TypeOf NewValue Is String) OrElse
                (TypeOf NewValue Is DBNull) OrElse
                (TypeOf NewValue Is Missing) OrElse
                (TypeOf NewValue Is System.Array) OrElse
                (TypeOf NewValue Is System.Runtime.InteropServices.CurrencyWrapper) Then
#Enable Warning BC40000
                Return BindingFlags.PutDispProperty
            End If
            Return BindingFlags.PutRefDispProperty
        End Function

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Sub LateCall(ByVal o As Object, ByVal objType As Type, ByVal name As String,
                            ByVal args() As Object, ByVal paramnames() As String, ByVal CopyBack() As Boolean)

            InternalLateCall(o, objType, name, args, paramnames, CopyBack, True)
        End Sub

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Friend Shared Function InternalLateCall(ByVal o As Object, ByVal objType As Type, ByVal name As String,
                                         ByVal args() As Object, ByVal paramnames() As String, ByVal CopyBack() As Boolean, ByVal IgnoreReturn As Boolean) As Object
            Dim flags As BindingFlags

            flags = BindingFlags.IgnoreCase Or
                    BindingFlags.InvokeMethod Or
                    BindingFlags.FlattenHierarchy Or
                    BindingFlags.OptionalParamBinding Or
                    BindingFlags.Static Or
                    BindingFlags.Instance Or
                    BindingFlags.Public

            If IgnoreReturn Then
                flags = flags Or BindingFlags.IgnoreReturn
            End If

            If objType Is Nothing Then
                If o Is Nothing Then
                    Throw VbMakeException(vbErrors.ObjNotSet)
                End If

                objType = o.GetType()
            End If

            Dim objIReflect As IReflect = GetCorrectIReflect(o, objType)

            If objType.IsCOMObject Then
                CheckForClassExtendingCOMClass(objType)
            End If

            If name Is Nothing Then
                name = ""
            End If

            Dim binder As VBBinder = New VBBinder(CopyBack)

            If Not objType.IsCOMObject Then
                Dim mi() As MemberInfo
                mi = GetMembersByName(objIReflect, name, flags)

                If (mi Is Nothing) OrElse (mi.Length = 0) Then
                    Throw New MissingMemberException(GetResourceString(SR.MissingMember_MemberNotFoundOnType2, name, VBFriendlyName(objType, o)))
                End If
                If MemberIsField(mi) Then
                    'This expression is not a procedure, but occurs as the target of a procedure call.
                    Throw New ArgumentException(GetResourceString(SR.ExpressionNotProcedure, name, VBFriendlyName(objType, o)))
                End If

                'Try a FastCall
                If (mi.Length = 1 AndAlso (paramnames Is Nothing OrElse paramnames.Length = 0)) Then
                    Dim Parameters As ParameterInfo()
                    Dim member As MemberInfo
                    Dim method As MethodBase
                    Dim ArgsLength, ParametersLength As Integer

                    member = mi(0)
                    If member.MemberType = MemberTypes.Property Then
                        member = CType(member, PropertyInfo).GetGetMethod()
                        If member Is Nothing Then
                            Throw New MissingMemberException(GetResourceString(SR.MissingMember_MemberNotFoundOnType2, name, VBFriendlyName(objType, o)))
                        End If
                    End If

                    method = CType(member, MethodBase)
                    Parameters = method.GetParameters()

                    ArgsLength = args.Length
                    ParametersLength = Parameters.Length

                    If ParametersLength = ArgsLength Then
                        If ParametersLength = 0 Then
                            Return FastCall(o, method, Parameters, args, objType, objIReflect)
                        ElseIf (CopyBack Is Nothing) AndAlso NoByrefs(Parameters) Then
                            'Check that we don't have a param array here
                            Dim LastParam As ParameterInfo
                            LastParam = Parameters(ParametersLength - 1)
                            If LastParam.ParameterType.IsArray() Then
                                'Check for ParamArray attribute
                                Dim ca() As Object
                                ca = LastParam.GetCustomAttributes(GetType(ParamArrayAttribute), False)
                                If (ca Is Nothing) OrElse (ca.Length = 0) Then
                                    Return FastCall(o, method, Parameters, args, objType, objIReflect)
                                End If
                            Else
                                Return FastCall(o, method, Parameters, args, objType, objIReflect)
                            End If
                        End If
                    End If

                End If

            End If

            Try
                Return binder.InvokeMember(name, flags, objType, objIReflect, o, args, paramnames)

            Catch ex As MissingMemberException
                'Keep existing exception text
                Throw

                'Some exceptions can occur that need to be mapped to MissingMemberException
            Catch ex As Exception When IsMissingMemberException(ex)
                Throw New MissingMemberException(GetResourceString(SR.MissingMember_MemberNotFoundOnType2, name, VBFriendlyName(objType, o)))

            Catch ex As TargetInvocationException
                Throw ex.InnerException

            End Try

        End Function

        Private Shared Function NoByrefs(ByVal parameters As ParameterInfo()) As Boolean
            Dim i As Integer
            For i = 0 To parameters.Length - 1
                If parameters(i).ParameterType.IsByRef Then
                    Return False
                End If
            Next i
            Return True
        End Function

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Private Shared Function FastCall(ByVal o As Object, ByVal method As MethodBase, ByVal Parameters As ParameterInfo(), ByVal args As Object(), ByVal objType As Type, ByVal objIReflect As IReflect) As Object

            Dim oArg As Object
            Dim Parameter As ParameterInfo
            Dim i As Integer

            For i = 0 To args.GetUpperBound(0)
                Parameter = Parameters(i)
                oArg = args(i)
                If TypeOf oArg Is Missing AndAlso Parameter.IsOptional Then
                    oArg = Parameter.DefaultValue
                End If
                args(i) = ObjectType.CTypeHelper(oArg, Parameter.ParameterType)
            Next i
            '
            'BEGIN: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
            '
            VBBinder.SecurityCheckForLateboundCalls(method, objType, objIReflect)

            If objType Is objIReflect OrElse method.IsStatic OrElse
                DoesTargetObjectMatch(o, method) Then

                VerifyObjRefPresentForInstanceCall(o, method)

                Return method.Invoke(o, args)

            Else
                Return InvokeMemberOnIReflect(objIReflect, method, BindingFlags.InvokeMethod, o, args)
            End If

            '
            'END: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
            '
        End Function

        Private Shared Function GetMembersByName(ByVal objIReflect As IReflect, ByVal name As String, ByVal flags As BindingFlags) As MemberInfo()

            ' Filter out generic methods for compatibility with the WHidbey framework
            GetMembersByName = GetNonGenericMembers(objIReflect.GetMember(name, flags))

            If (Not GetMembersByName Is Nothing) AndAlso (GetMembersByName.Length = 0) Then
                Return Nothing
            End If

        End Function

        Private Shared Function MemberIsField(ByVal mi As MemberInfo()) As Boolean

            Dim MemberIndex As Integer
            Dim ThisMember As MemberInfo

            For MemberIndex = 0 To mi.GetUpperBound(0)

                ThisMember = mi(MemberIndex)

                If ThisMember Is Nothing Then
                    'Skip this one

                ElseIf ThisMember.MemberType = MemberTypes.Field Then

                    '
                    'Run through the list and remove all the inherited members of this type
                    '
                    Dim j As Integer
                    For j = 0 To mi.GetUpperBound(0)

                        If MemberIndex <> j AndAlso (Not mi(j) Is Nothing) AndAlso
                            ThisMember.DeclaringType.IsSubclassOf(mi(j).DeclaringType) Then
                            ' ThisMember Shadows the baseclass and ThatMethod should not be accessible
                            ' to the caller
                            mi(j) = Nothing
                        End If

                    Next j

                End If

            Next

            For Each ThisMember In mi
                If Not ThisMember Is Nothing Then
                    If ThisMember.MemberType <> MemberTypes.Field Then
                        Return False
                    End If
                End If
            Next
            Return True
        End Function

        Friend Shared Function DoesTargetObjectMatch(ByVal Value As Object, ByVal Member As MemberInfo) As Boolean
            If (Value Is Nothing) OrElse Member.DeclaringType.IsAssignableFrom(Value.GetType) Then
                Return True
            End If

            Return False
        End Function


        ' Used for invoking the InvokeMember of IReflect. We don't want to use the flags already because
        ' we don't want clashes like between InvokeMethod and SetProperty.
        Const VBLateBindingFlags As BindingFlags = BindingFlags.IgnoreCase Or
                                                       BindingFlags.FlattenHierarchy Or
                                                       BindingFlags.OptionalParamBinding Or
                                                       BindingFlags.Static Or
                                                       BindingFlags.Instance Or
                                                       BindingFlags.Public

        Friend Shared Function InvokeMemberOnIReflect(ByVal objIReflect As IReflect, ByVal member As MemberInfo, ByVal flags As BindingFlags, ByVal target As Object, ByVal args As Object()) As Object

            Dim binder As New VBBinder(Nothing)
            binder.CacheMember(member)

            Return objIReflect.InvokeMember(member.Name, VBLateBindingFlags Or flags, binder, target, args, Nothing, Nothing, Nothing)

        End Function

        Private Shared Function GetCorrectIReflect(ByVal o As Object, ByVal objType As Type) As IReflect

            ' For a System.Type Object, we always use the underlying System.Type's IReflect implementation, because a System.Type's Implementation
            ' returns information about the Type it represents and not its own information. If we did not do this, latebound calls to a System.Type
            ' Object would fail.

            Return DirectCast(objType, IReflect)
        End Function

        Friend Shared Sub VerifyObjRefPresentForInstanceCall(ByVal Value As Object, ByVal Member As MemberInfo)
            If Value Is Nothing Then
                Dim IsStatic As Boolean = True

                Debug.Assert(Not Member Is Nothing, "How can this be Nothing ?")

                Select Case Member.MemberType
                    Case MemberTypes.Method
                        IsStatic = DirectCast(Member, MethodInfo).IsStatic

                    Case MemberTypes.Field
                        IsStatic = DirectCast(Member, FieldInfo).IsStatic

                    Case MemberTypes.Constructor
                        IsStatic = DirectCast(Member, ConstructorInfo).IsStatic

                    Case MemberTypes.Property
                        ' We always decide based on the context and use the get or the set accessor
                        ' appropriately. So by the time this method is invoked, we should just see 
                        ' the MethodInfos of the Getter or the Setter

                        Debug.Assert(False, "How can a property get here ?")

                    Case Else
                        'this should never happen

                        Debug.Assert(False, "How did we get here ?")
                End Select


                If Not IsStatic Then
                    'Reference to non-shared member '|1' requires an object reference.
                    Throw New NullReferenceException(
                        GetResourceString(SR.NullReference_InstanceReqToAccessMember1, MemberToString(Member)))
                End If
            End If
        End Sub

        Friend Shared Function GetNonGenericMembers(ByVal Members As MemberInfo()) As MemberInfo()
            If Members IsNot Nothing AndAlso Members.Length > 0 Then
                Dim NonGenericMemberCount As Integer = 0

                For MemberIndex As Integer = 0 To Members.GetUpperBound(0)
                    If LegacyIsGeneric(Members(MemberIndex)) Then
                        Members(MemberIndex) = Nothing
                    Else
                        NonGenericMemberCount += 1
                    End If
                Next

                If NonGenericMemberCount = Members.GetUpperBound(0) + 1 Then
                    ' There weren't any generic members. Return the original array.
                    Return Members
                ElseIf NonGenericMemberCount > 0 Then
                    Dim NonGenericMembers(NonGenericMemberCount - 1) As MemberInfo

                    Dim NonGenericIndex As Integer = 0

                    ' Collect the non-generic methods
                    For MemberIndex As Integer = 0 To Members.GetUpperBound(0)
                        If Members(MemberIndex) IsNot Nothing Then
                            NonGenericMembers(NonGenericIndex) = Members(MemberIndex)
                            NonGenericIndex += 1
                        End If
                    Next

                    Return NonGenericMembers
                End If
            End If

            Return Nothing
        End Function

        Friend Shared Function LegacyIsGeneric(ByVal Member As MemberInfo) As Boolean
            'Returns True whether Method is an instantiated or uninstantiated generic method.
            Dim Method As MethodBase = TryCast(Member, MethodBase)
            If Method Is Nothing Then Return False
            Return Method.IsGenericMethod
        End Function

    End Class

End Namespace
