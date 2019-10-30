' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Diagnostics
Imports System.Reflection
Imports System.Globalization
Imports System.Security

Imports Microsoft.VisualBasic.CompilerServices.LateBinding
Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    Friend NotInheritable Class VBBinder

        Inherits Binder

        Const PARAMARRAY_NONE As Integer = -1
        Const ARG_MISSING As Integer = -1

        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
        Friend NotInheritable Class VBBinderState
            Friend m_OriginalArgs() As Object
            Friend m_ByRefFlags() As Boolean
            Friend m_OriginalByRefFlags() As Boolean
            Friend m_OriginalParamOrder() As Integer

            Friend Sub New()
            End Sub
        End Class

        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
        Enum BindScore
            Exact = 0
            Widening0 = 1
            Widening1 = 2
            [Narrowing] = 3
            Unknown = 4
        End Enum

        Friend m_BindToName As String
        Friend m_objType As System.Type
        Private m_state As VBBinderState
        Private m_CachedMember As MemberInfo

        Private Sub ThrowInvalidCast(ByVal ArgType As System.Type, ByVal ParmType As System.Type, ByVal ParmIndex As Integer)
            Throw New InvalidCastException(GetResourceString(SR.InvalidCast_FromToArg4, CalledMethodName(), CStr(ParmIndex + 1), VBFriendlyName(ArgType), VBFriendlyName(ParmType)))
        End Sub

        'Flags to indicate if parameter was passed
        Private m_ByRefFlags As Boolean()

        Sub New(ByVal CopyBack As Boolean())
            m_ByRefFlags = CopyBack
        End Sub

        '
        ' * This method allows us to reorder the arguments back to the original caller order
        ' * if necessary
        '
        Public Overrides Sub ReorderArgumentArray(ByRef args() As Object, ByVal objState As Object)

            Dim i, j As Integer
            Dim state As VBBinderState = CType(objState, VBBinderState)
            Dim IsByRef As Boolean

            If (args Is Nothing) OrElse (state Is Nothing) Then
                GoTo CleanupAndExit
            End If

            If Not state.m_OriginalParamOrder Is Nothing Then

                'The arguments have been reordered, lets put them back
                If (Not m_ByRefFlags Is Nothing) Then

                    If state.m_ByRefFlags Is Nothing Then
                        'Clear all flags, nothing was byref
                        For i = 0 To m_ByRefFlags.GetUpperBound(0)
                            m_ByRefFlags(i) = False
                        Next i

                    Else
                        'Make temporary array to reorder arguments
                        For i = 0 To state.m_OriginalParamOrder.GetUpperBound(0)

                            j = state.m_OriginalParamOrder(i)
                            If j >= 0 AndAlso j <= args.GetUpperBound(0) Then
                                m_ByRefFlags(j) = state.m_ByRefFlags(j)
                                state.m_OriginalArgs(j) = args(i)
                            End If

                        Next i

                    End If

                End If

            Else

                If Not m_ByRefFlags Is Nothing Then

                    If state.m_ByRefFlags Is Nothing Then
                        'Clear all flags, nothing was byref
                        For i = 0 To m_ByRefFlags.GetUpperBound(0)
                            m_ByRefFlags(i) = False
                        Next i
                    Else
                        For i = 0 To m_ByRefFlags.GetUpperBound(0)
                            If m_ByRefFlags(i) Then
                                'Argument was passed to us as a byref candidate
                                'so we need to reflect it's true "ByRef"ness
                                IsByRef = state.m_ByRefFlags(i)
                                m_ByRefFlags(i) = IsByRef
                                If IsByRef Then
                                    state.m_OriginalArgs(i) = args(i)
                                End If
                            End If
                        Next i
                    End If

                End If

            End If

CleanupAndExit:
            If Not state Is Nothing Then
                state.m_OriginalParamOrder = Nothing
                state.m_ByRefFlags = Nothing
            End If

        End Sub

        '/**
        ' * This method is passed a of methods and must choose the best fit.  
        ' * 
        ' * @exception MissingMethodException
        ' * @exception ArgumentException
        ' * @exception AmbiguousMatchException
        ' * be invoked.
        '*/

        Public Overrides Function BindToMethod(ByVal bindingAttr As BindingFlags, ByVal match() As MethodBase, ByRef args() As Object, ByVal modifiers() As ParameterModifier, ByVal culture As CultureInfo, ByVal names() As String, ByRef ObjState As Object) As MethodBase

            Dim HasByRefArgs As Boolean
            Dim ThisMatchHasByRefs As Boolean
            Dim SelectedCount, SelectedIndex As Integer
            Dim SelectedMatch As MethodBase
            Dim ThisMethod As MethodBase
            Dim LastParam As ParameterInfo
            Dim state As VBBinderState
            Dim MethodIndex, NameIndex, ParmIndex, ArgIndex As Integer
            Dim Parameters() As ParameterInfo
            Dim ArgTypes() As Type
            Dim ArgType As Type = Nothing
            Dim ParmType As Type = Nothing
            Dim ParamArrayElementType As Type = Nothing
            Dim ParamArrayIndex As Integer
            Dim LastArgIndexToCheck, LastParmIndexToCheck As Integer
            Dim IsPropertySet As Boolean
            Dim InitialMemberCount As Integer ' Member count minus shadowed members
            Dim MostSpecific As Integer

            If (match Is Nothing) OrElse (match.Length = 0) Then
                Throw VbMakeException(vbErrors.OLENoPropOrMethod)
            End If

            If (Not m_CachedMember Is Nothing) AndAlso
               (m_CachedMember.MemberType = MemberTypes.Method) AndAlso
               (Not match(0) Is Nothing) AndAlso
               (match(0).Name = m_CachedMember.Name) Then

                Return DirectCast(m_CachedMember, MethodBase)
            End If

            IsPropertySet = ((bindingAttr And BindingFlags.SetProperty) <> 0)

            If Not names Is Nothing AndAlso names.Length = 0 Then
                ' simplify the checkin down below
                names = Nothing
            End If

            ' There is a minor difference in behavior of early vs. late binding
            ' with regard to Shadows
            '  Shadows can apply to protected methods overriding a public
            '  method of the same name on a base class
            '  but we do not know about these methods, because only asked for
            '  public members.  To get early bound binding semantics, we need
            '  to query for all methods and then remove all private/protected members
            '  from the match list after we remove the methods that are shadowed 
            '  on the base class, since they should not be visible in the derived class

            'STEP 1 - Remove Shadowed members
            'Iterate through the methods and see if any Shadowed members
            'need to be removed from the list
            'MethodIndex = 0

            SelectedCount = match.Length

            If SelectedCount > 1 Then
                For MethodIndex = 0 To match.GetUpperBound(0)

                    ThisMethod = match(MethodIndex)

                    If ThisMethod Is Nothing Then
                        'Skip this one

                    ElseIf ThisMethod.IsHideBySig Then
                        'Hide-by-sig - shadows exact name and sig on base types
                        '
                        'Don't bother filtering here, this will get done below for this case

                    ElseIf ThisMethod.IsVirtual Then
                        'Virtual methods only shadow if NewSlot set
                        If (ThisMethod.Attributes And MethodAttributes.NewSlot) <> 0 Then
                            '
                            'Run through the list and remove all the inherited members of this type
                            '
                            Dim j As Integer
                            For j = 0 To match.GetUpperBound(0)

                                If MethodIndex <> j AndAlso (Not match(j) Is Nothing) AndAlso
                                    ThisMethod.DeclaringType.IsSubclassOf(match(j).DeclaringType) Then
                                    ' ThisMethod Shadows the baseclass and ThatMethod should not be accessible
                                    ' to the caller
                                    match(j) = Nothing
                                    SelectedCount -= 1
                                End If

                            Next j
                        End If

                    Else
                        '
                        'Run through the list and remove all the inherited members of this type
                        '
                        Dim j As Integer
                        For j = 0 To match.GetUpperBound(0)

                            If MethodIndex <> j AndAlso (Not match(j) Is Nothing) AndAlso
                                ThisMethod.DeclaringType.IsSubclassOf(match(j).DeclaringType) Then
                                ' ThisMethod Shadows the baseclass and ThatMethod should not be accessible
                                ' to the caller
                                match(j) = Nothing
                                SelectedCount -= 1
                            End If

                        Next j

                    End If

                Next
            End If

            InitialMemberCount = SelectedCount

            'STEP 2 - Remove all Private and Protected members
            ' 
            ' TBD IF NEEDED : see note above on shadows

            'STEP 3 - Remove functions that don't have matching argument names
            '
            If Not names Is Nothing Then

                For MethodIndex = 0 To match.GetUpperBound(0)

                    ThisMethod = match(MethodIndex)

                    If Not ThisMethod Is Nothing Then

                        Parameters = ThisMethod.GetParameters()

                        'Check for the last argument being a ParamArray
                        LastParmIndexToCheck = Parameters.GetUpperBound(0)
                        If IsPropertySet Then
                            LastParmIndexToCheck -= 1
                        End If

                        If LastParmIndexToCheck >= 0 Then
                            LastParam = Parameters(LastParmIndexToCheck)

                            ParamArrayIndex = PARAMARRAY_NONE

                            If LastParam.ParameterType.IsArray() Then
                                'Check for ParamArray attribute
                                Dim ca() As Object
                                ca = LastParam.GetCustomAttributes(GetType(ParamArrayAttribute), False)
                                If (Not ca Is Nothing) AndAlso (ca.Length > 0) Then
                                    ParamArrayIndex = LastParmIndexToCheck
                                Else
                                    ParamArrayIndex = PARAMARRAY_NONE
                                End If
                            End If
                        End If

                        For NameIndex = 0 To names.GetUpperBound(0)

                            For ParmIndex = 0 To LastParmIndexToCheck

                                If StrComp(names(NameIndex), Parameters(ParmIndex).Name, CompareMethod.Text) = 0 Then
                                    If ParmIndex = ParamArrayIndex AndAlso SelectedCount = 1 Then
                                        Throw VbMakeExceptionEx(vbErrors.NamedArgsNotSupported, GetResourceString(SR.NamedArgumentOnParamArray))
                                    Else
                                        If ParmIndex = ParamArrayIndex Then
                                            'Matched against a paramarray, force into the removal code below
                                            ParmIndex = LastParmIndexToCheck + 1
                                        Else
                                            'Found it, so look at the next name
                                        End If
                                        Exit For
                                    End If
                                End If

                            Next ParmIndex

                            ' This is an error condition.  The name was not found.  This
                            ' method must not match what we sent.
                            If (ParmIndex > LastParmIndexToCheck) Then

                                If SelectedCount = 1 Then
                                    '   i.e.  MissingMethod, MissingField, MissingMember
                                    'This is the last possible matching member
                                    ' so throw an exception that the name doesn't match
                                    Throw New MissingMemberException(GetResourceString(SR.Argument_InvalidNamedArg2, names(NameIndex), CalledMethodName()))
                                End If

                                match(MethodIndex) = Nothing
                                SelectedCount -= 1
                                Exit For

                            End If

                        Next NameIndex

                    End If

                Next MethodIndex

            End If

            'STEP 4 - Rearrange the arguments
            '         Named arguments and ParamArrays affect the ordering,
            '         so we have to move them around a bit, 
            '         but also keep track of what order they
            '         were in so we can reorder them on the way out

            ' We are creating a paramOrder array to act as a mapping
            ' between the order of the args and the actual order of the
            ' parameters in the method.  This order may differ because
            ' named parameters (names) may change the order.  If names
            ' is not provided, then we assume the default mapping (0,1,...)

            Dim ParamArrayIndexList() As Integer

            'Create a list of flags marking those having a ParamArray
            ParamArrayIndexList = New Integer(match.Length - 1) {}

            For MethodIndex = 0 To match.GetUpperBound(0)

                ThisMethod = match(MethodIndex)

                If Not ThisMethod Is Nothing Then

                    ParamArrayIndex = PARAMARRAY_NONE

                    Parameters = ThisMethod.GetParameters()
                    LastParmIndexToCheck = Parameters.GetUpperBound(0)
                    If IsPropertySet Then
                        LastParmIndexToCheck -= 1
                    End If

                    'Check for the last argument being a ParamArray
                    If LastParmIndexToCheck >= 0 Then

                        LastParam = Parameters(LastParmIndexToCheck)

                        If LastParam.ParameterType.IsArray() Then
                            'Check for ParamArray attribute
                            Dim ca() As Object
                            ca = LastParam.GetCustomAttributes(GetType(ParamArrayAttribute), False)
                            If (Not ca Is Nothing) AndAlso (ca.Length > 0) Then
                                ParamArrayIndex = LastParmIndexToCheck
                            End If
                        End If

                    End If

                    ParamArrayIndexList(MethodIndex) = ParamArrayIndex

                    If (ParamArrayIndex = PARAMARRAY_NONE) AndAlso (args.Length > Parameters.Length) Then
                        'If we have too many arguments, we don't match any function
                        If SelectedCount = 1 Then
                            Throw New MissingMemberException(GetResourceString(SR.NoMethodTakingXArguments2, CalledMethodName(), CStr(GetPropArgCount(args, IsPropertySet))))
                        End If

                        'Clear this entry
                        match(MethodIndex) = Nothing
                        SelectedCount -= 1

                    End If

                    Dim LengthOfNonParamArrayArguments As Integer = LastParmIndexToCheck
                    If ParamArrayIndex <> PARAMARRAY_NONE Then
                        LengthOfNonParamArrayArguments -= 1
                    End If

                    If (args.Length < LengthOfNonParamArrayArguments) Then

                        ' If the number of parameters is greater than the number
                        ' of args then we are in the situation were we must
                        ' be using default values.
                        Dim j As Integer
                        For j = args.Length To LengthOfNonParamArrayArguments - 1
                            'DBNull indicates no default value
                            If (Parameters(j).DefaultValue Is System.DBNull.Value) Then
                                Exit For
                            End If
                        Next j

                        If (j <> LengthOfNonParamArrayArguments) Then
                            'Not enough arguments to call this method, so remove it
                            If SelectedCount = 1 Then
                                Throw New MissingMemberException(GetResourceString(SR.NoMethodTakingXArguments2, CalledMethodName(), CStr(GetPropArgCount(args, IsPropertySet))))
                            End If
                            match(MethodIndex) = Nothing
                            SelectedCount -= 1

                        End If

                    End If

                End If

            Next MethodIndex


            'STEP 5 - Create mapping table for argument reordering
            '
            Dim paramOrder As Object() = New Object(match.Length - 1) {}
            Dim ArgIndexes() As Integer

            For MethodIndex = 0 To match.GetUpperBound(0)

                ThisMethod = match(MethodIndex)

                If Not ThisMethod Is Nothing Then

                    Parameters = ThisMethod.GetParameters()

                    If args.Length > Parameters.Length Then
                        ArgIndexes = New Integer(args.Length - 1) {}
                    Else
                        ArgIndexes = New Integer(Parameters.Length - 1) {}
                    End If

                    paramOrder(MethodIndex) = ArgIndexes

                    If (names Is Nothing) Then
                        ' Default mapping

                        Dim TmpLastIndex As Integer
                        ' Mark which parameters have not been found in the names list
                        TmpLastIndex = args.GetUpperBound(0)
                        If IsPropertySet Then
                            TmpLastIndex -= 1
                        End If
                        For ArgIndex = 0 To TmpLastIndex
                            If TypeOf args(ArgIndex) Is System.Reflection.Missing AndAlso (ArgIndex > Parameters.GetUpperBound(0) OrElse Parameters(ArgIndex).IsOptional) Then
                                ArgIndexes(ArgIndex) = ARG_MISSING
                            Else
                                ArgIndexes(ArgIndex) = ArgIndex
                            End If
                        Next ArgIndex


                        TmpLastIndex = ArgIndexes.GetUpperBound(0)
                        ' Any extra arguments must be optional
                        For ArgIndex = ArgIndex To TmpLastIndex
                            ArgIndexes(ArgIndex) = ARG_MISSING
                        Next ArgIndex

                        If IsPropertySet Then
                            'Last index or args array is the Set value
                            'we might have optional arguments before the new value
                            ArgIndexes(TmpLastIndex) = args.GetUpperBound(0)
                        End If
                    Else
                        ' Named parameters, reorder the mapping.  If 
                        '  CreateParamOrder fails, it means that the method
                        '  doesn't have a name that matchs one of the named
                        '  parameters so we don't consider it any further.

                        Dim ex As Exception

                        ex = CreateParamOrder(IsPropertySet,
                                            ArgIndexes,
                                            ThisMethod.GetParameters(),
                                            args,
                                            names)
                        If (Not ex Is Nothing) Then
                            If SelectedCount = 1 Then
                                'Just throw the exception
                                Throw ex
                            Else
                                match(MethodIndex) = Nothing
                                SelectedCount -= 1
                            End If
                        End If
                    End If

                End If

            Next MethodIndex


            'STEP 6 - Save the types of the arguments passed in

            ' objects that contain a null are treated as
            ' if they were typeless (but match either object references
            ' or value classes).  We mark this condition by
            ' placing a null in the argTypes array.
            ArgTypes = New Type(args.Length - 1) {}

            For ArgIndex = 0 To args.GetUpperBound(0)
                If Not args(ArgIndex) Is Nothing Then
                    ArgTypes(ArgIndex) = args(ArgIndex).GetType()
                End If
            Next


            'STEP 7 - Eliminate methods that have types that cannot be called 
            '         (i.e. no widening or narrowing posibilities)
            '

            For MethodIndex = 0 To match.GetUpperBound(0)

                ThisMethod = match(MethodIndex)

                If Not ThisMethod Is Nothing Then

                    Parameters = ThisMethod.GetParameters()
                    ArgIndexes = CType(paramOrder(MethodIndex), Integer())
                    LastParmIndexToCheck = ArgIndexes.GetUpperBound(0)
                    If IsPropertySet Then
                        LastParmIndexToCheck -= 1
                    End If

                    ParamArrayIndex = ParamArrayIndexList(MethodIndex)
                    If ParamArrayIndex <> PARAMARRAY_NONE Then
                        ParamArrayElementType = Parameters(ParamArrayIndex).ParameterType.GetElementType()
                    Else
                        'No ParamArray involved, eleminate methods with insufficient arguments
                        If ArgIndexes.Length > Parameters.Length Then
                            GoTo ClearMethod7
                        End If
                    End If

                    For ParmIndex = 0 To LastParmIndexToCheck

                        ArgIndex = ArgIndexes(ParmIndex)

                        'Do we need this check now? Will it already have been removed?
                        If (ArgIndex = ARG_MISSING) Then
                            If Parameters(ParmIndex).IsOptional OrElse (ParmIndex = ParamArrayIndexList(MethodIndex)) Then
                                'Argument not supplied for this argument
                                GoTo NextParm7
                            Else
                                If SelectedCount = 1 Then
                                    Throw New MissingMemberException(GetResourceString(SR.NoMethodTakingXArguments2, CalledMethodName(), CStr(GetPropArgCount(args, IsPropertySet))))
                                End If
                                GoTo ClearMethod7
                            End If
                        End If

                        ArgType = ArgTypes(ArgIndex)
                        'If ArgType Is Nothing Then
                        '    ArgType = GetType(Object)
                        'End If
                        If (ArgType Is Nothing) Then
                            'Nothing matches anything
                            GoTo NextParm7
                        End If

                        If (ParamArrayIndex <> PARAMARRAY_NONE) AndAlso (ParmIndex > ParamArrayIndex) Then
                            ParmType = Parameters(ParamArrayIndex).ParameterType.GetElementType()
                        Else
                            ParmType = Parameters(ParmIndex).ParameterType

                            If ParmType.IsByRef Then
                                ParmType = ParmType.GetElementType()
                            End If

                            If (ParmIndex = ParamArrayIndex) Then
                                If (ParmType.IsInstanceOfType(args(ArgIndex)) AndAlso ParmIndex = LastParmIndexToCheck) Then
                                    'Arg can be cast to the Param type
                                    GoTo NextParm7
                                End If
                                ParmType = ParamArrayElementType
                            End If

                        End If

                        If ParmType Is ArgType Then
                            GoTo NextParm7
                        End If

                        If (ArgType Is Type.Missing) AndAlso Parameters(ParmIndex).IsOptional Then
                            'Missing matches any optional
                            GoTo NextParm7
                        End If

                        If args(ArgIndex) Is Missing.Value Then
                            GoTo ClearMethod7
                        End If

                        If (ParmType Is GetType(Object)) Then
                            'Param type is Object, so anything goes
                            GoTo NextParm7
                        End If

                        If ParmType.IsInstanceOfType(args(ArgIndex)) Then
                            'Arg can be cast to the Param type
                            GoTo NextParm7
                        End If

                        'Check if this can be converted 
                        Dim ParmTypeCode As TypeCode
                        Dim ArgTypeCode As TypeCode

                        ParmTypeCode = System.Type.GetTypeCode(ParmType)

                        If ArgType Is Nothing Then
                            ArgTypeCode = TypeCode.Empty
                        Else
                            ArgTypeCode = System.Type.GetTypeCode(ArgType)
                        End If

                        Select Case ParmTypeCode

                            Case TypeCode.Boolean,
                                    TypeCode.Byte,
                                    TypeCode.Int16,
                                    TypeCode.Int32,
                                    TypeCode.Int64,
                                    TypeCode.Decimal,
                                    TypeCode.Single,
                                    TypeCode.Double
                                'Allowed coercions

                                Select Case ArgTypeCode

                                    Case TypeCode.Boolean,
                                            TypeCode.Byte,
                                            TypeCode.Int16,
                                            TypeCode.Int32,
                                            TypeCode.Int64,
                                            TypeCode.Decimal,
                                            TypeCode.Single,
                                            TypeCode.Double,
                                            TypeCode.String
                                        'Allowed coercions

                                    Case Else
                                        'Case TypeCode.DateTime, TypeCode.Char, TypeCode.Object
                                        GoTo ClearMethod7

                                End Select

                            Case TypeCode.Char

                                Select Case ArgTypeCode

                                    Case TypeCode.String
                                        'This can be converted

                                    Case Else
                                        GoTo ClearMethod7

                                End Select

                            Case TypeCode.String
                                Select Case ArgTypeCode
                                    Case TypeCode.Boolean,
                                            TypeCode.Byte,
                                            TypeCode.Int16,
                                            TypeCode.Int32,
                                            TypeCode.Int64,
                                            TypeCode.Decimal,
                                            TypeCode.Double,
                                            TypeCode.Single,
                                            TypeCode.Char,
                                            TypeCode.String,
                                            TypeCode.Empty
                                        'Accept These

                                        'Case TypeCode.Object, TypeCode.UInt16, TypeCode.UInt32, TypeCode.UInt64
                                    Case Else
                                        If ArgType Is GetType(Char()) Then
                                            'Accept as if type String
                                        Else
                                            GoTo ClearMethod7
                                        End If
                                End Select

                            Case TypeCode.DateTime
                                Select Case ArgTypeCode
                                    Case TypeCode.String
                                        'Accept String-to-Date

                                    Case Else
                                        GoTo ClearMethod7
                                End Select

                                'Case TypeCode.Object, TypeCode.UInt16, TypeCode.UInt32, TypeCode.UInt64
                            Case Else
                                If ParmType Is GetType(Char()) Then
                                    Select Case ArgTypeCode
                                        Case TypeCode.String

                                        Case TypeCode.Object
                                            If Not ArgType Is GetType(Char()) Then
                                                GoTo ClearMethod7
                                            End If

                                        Case Else
                                            GoTo ClearMethod7
                                    End Select

                                Else
                                    'If we're here, then the Param
                                    ' is a reference type, but NOT System.Object
                                    ' and cannot be cast to the expected type
                                    '
                                    GoTo ClearMethod7
                                End If

                        End Select
NextParm7:
                    Next ParmIndex

                End If
                GoTo NextMethod7
ClearMethod7:
                If SelectedCount = 1 Then
                    'Removing the only remaining member
                    If InitialMemberCount = 1 Then
                        ThrowInvalidCast(ArgType, ParmType, ParmIndex)
                    Else
                        Throw New AmbiguousMatchException(GetResourceString(SR.AmbiguousMatch_NarrowingConversion1, CalledMethodName()))
                    End If
                End If
                match(MethodIndex) = Nothing
                SelectedCount -= 1

NextMethod7:

            Next MethodIndex


            SelectedCount = 0

            'STEP 8
            '
            ' Find the methods that match...
            For MethodIndex = 0 To match.GetUpperBound(0)

                ThisMethod = match(MethodIndex)

                ' If we have named parameters then we may
                ' have hole in the match array.
                If (ThisMethod Is Nothing) Then
                    GoTo NextMethod8
                End If

                ArgIndexes = CType(paramOrder(MethodIndex), Integer())

                ' Validate the parameters.
                Parameters = ThisMethod.GetParameters()

                ThisMatchHasByRefs = False

                LastParmIndexToCheck = Parameters.GetUpperBound(0)
                If IsPropertySet Then
                    LastParmIndexToCheck -= 1
                End If

                LastArgIndexToCheck = args.GetUpperBound(0)
                If IsPropertySet Then
                    LastArgIndexToCheck -= 1
                End If
                ParamArrayIndex = ParamArrayIndexList(MethodIndex)
                If ParamArrayIndex <> PARAMARRAY_NONE Then
                    ParamArrayElementType = Parameters(LastParmIndexToCheck).ParameterType.GetElementType()
                End If

                For ParmIndex = 0 To LastParmIndexToCheck

                    If ParmIndex = ParamArrayIndex Then
                        ParmType = ParamArrayElementType
                    Else
                        ParmType = Parameters(ParmIndex).ParameterType
                    End If

                    If ParmType.IsByRef Then
                        ThisMatchHasByRefs = True
                        ParmType = ParmType.GetElementType()
                    End If

                    ArgIndex = ArgIndexes(ParmIndex)

                    If (ArgIndex = ARG_MISSING) AndAlso Parameters(ParmIndex).IsOptional _
                                OrElse (ParmIndex = ParamArrayIndexList(MethodIndex)) Then
                        'Argument not supplied for this argument
                        GoTo NextParm8
                    End If

                    ArgType = ArgTypes(ArgIndex)

                    If (ArgType Is Nothing) Then
                        'Nothing passed, will cast to anything
                        GoTo NextParm8
                    End If

                    If (ArgType Is Type.Missing) AndAlso Parameters(ParmIndex).IsOptional Then
                        GoTo NextParm8
                    End If

                    If ParmType Is ArgType Then
                        GoTo NextParm8
                    End If

                    If (ParmType Is GetType(Object)) Then
                        GoTo NextParm8
                    End If

                    'Check if this can be converted 
                    Dim ParmTypeCode As TypeCode
                    Dim ArgTypeCode As TypeCode

                    ParmTypeCode = System.Type.GetTypeCode(ParmType)

                    If ArgType Is Nothing Then
                        ArgTypeCode = TypeCode.Empty
                    Else
                        ArgTypeCode = System.Type.GetTypeCode(ArgType)
                    End If

                    Select Case ParmTypeCode

                        Case TypeCode.Boolean,
                                TypeCode.Byte,
                                TypeCode.Int16,
                                TypeCode.Int32,
                                TypeCode.Int64,
                                TypeCode.Decimal,
                                TypeCode.Single,
                                TypeCode.Double
                            'Allowed coercions

                            Select Case ArgTypeCode

                                Case TypeCode.Boolean,
                                        TypeCode.Byte,
                                        TypeCode.Int16,
                                        TypeCode.Int32,
                                        TypeCode.Int64,
                                        TypeCode.Decimal,
                                        TypeCode.Single,
                                        TypeCode.Double,
                                        TypeCode.String
                                    'Allowed coercions

                                Case Else 'Case TypeCode.DateTime, TypeCode.Char
                                    If SelectedCount = 0 Then
                                        ThrowInvalidCast(ArgType, ParmType, ParmIndex)
                                    End If

                            End Select

                        Case TypeCode.Char
                        Case TypeCode.String
                        Case TypeCode.DateTime

                        Case Else

                    End Select

NextParm8:
                Next


                'If we went through all the args, and they matched
                ' then j will be > the args upper bound
                If (ParmIndex > LastParmIndexToCheck) Then
                    'WE FOUND ONE!
                    If MethodIndex <> SelectedCount Then
                        match(SelectedCount) = match(MethodIndex)
                        paramOrder(SelectedCount) = paramOrder(MethodIndex)
                        ParamArrayIndexList(SelectedCount) = ParamArrayIndexList(MethodIndex)
                        match(MethodIndex) = Nothing
                    End If
                    SelectedCount += 1
                    If ThisMatchHasByRefs Then
                        HasByRefArgs = True
                    End If
                Else
                    match(MethodIndex) = Nothing
                End If
NextMethod8:
            Next

            If (SelectedCount = 0) Then
                Throw New MissingMemberException(GetResourceString(SR.NoMethodTakingXArguments2, CalledMethodName(), CStr(GetPropArgCount(args, IsPropertySet))))
            End If

            state = New VBBinderState
            m_state = state

            'Store in OUT param for caller to pass back to us
            ObjState = state

            state.m_OriginalArgs = args

            If (SelectedCount = 1) Then

                'All matches are pushed to the front of the list
                SelectedIndex = 0

            Else

                ' Walk all of the methods looking the most specific method to invoke
                Dim AmbiguousCount As Integer
                Dim Score, LowestScore As BindScore

                SelectedIndex = 0
                LowestScore = BindScore.Unknown
                AmbiguousCount = 0

                ' Score each method 
                '    0 ==> Exact match
                '    1 ==> Casting down conversion required
                '    2 ==> Intrinsic widening conversion required
                '    3 ==> Requires narrowing conversion

                For MethodIndex = 0 To SelectedCount - 1 'match.GetUpperBound(0)

                    ThisMethod = match(MethodIndex)

                    If ThisMethod Is Nothing Then
                        'Skip it
                    Else

                        ArgIndexes = CType(paramOrder(MethodIndex), Integer())
                        Score = BindingScore(ThisMethod.GetParameters(), ArgIndexes, ArgTypes, IsPropertySet, ParamArrayIndexList(MethodIndex))

                        If Score < LowestScore Then
                            If MethodIndex <> 0 Then
                                match(0) = match(MethodIndex)
                                paramOrder(0) = paramOrder(MethodIndex)
                                ParamArrayIndexList(0) = ParamArrayIndexList(MethodIndex)
                                match(MethodIndex) = Nothing
                            End If
                            AmbiguousCount = 1
                            LowestScore = Score

                        ElseIf Score = LowestScore Then

                            If Score = BindScore.Exact OrElse Score = BindScore.Widening1 Then

                                MostSpecific = GetMostSpecific(match(0), ThisMethod, ArgIndexes, paramOrder, IsPropertySet, ParamArrayIndexList(0), ParamArrayIndexList(MethodIndex), args)

                                If MostSpecific = -1 Then
                                    If AmbiguousCount <> MethodIndex Then
                                        match(AmbiguousCount) = match(MethodIndex)
                                        paramOrder(AmbiguousCount) = paramOrder(MethodIndex)
                                        ParamArrayIndexList(AmbiguousCount) = ParamArrayIndexList(MethodIndex)
                                        match(MethodIndex) = Nothing
                                    End If
                                    AmbiguousCount += 1

                                ElseIf MostSpecific = 0 Then
                                    'AmbiguousCount remains unchanged
                                    'because we could have already had multiple matches

                                Else 'If MostSpecific = 1 Then

                                    ' VSW 370803: For MethodIndex to be the new best match, it
                                    ' needs to be most specific than all the current ambiguous
                                    ' matches.
                                    Dim MoreSpecificThanAllMatches As Boolean = True

                                    For AmbiguousIndex As Integer = 1 To AmbiguousCount - 1
                                        If GetMostSpecific(match(AmbiguousIndex), ThisMethod, ArgIndexes, paramOrder, IsPropertySet, ParamArrayIndexList(AmbiguousIndex), ParamArrayIndexList(MethodIndex), args) <> 1 Then
                                            MoreSpecificThanAllMatches = False
                                            Exit For
                                        End If
                                    Next

                                    If MoreSpecificThanAllMatches Then
                                        AmbiguousCount = 0
                                    End If

                                    If MethodIndex <> AmbiguousCount Then
                                        match(AmbiguousCount) = match(MethodIndex)
                                        paramOrder(AmbiguousCount) = paramOrder(MethodIndex)
                                        ParamArrayIndexList(AmbiguousCount) = ParamArrayIndexList(MethodIndex)
                                        match(MethodIndex) = Nothing
                                    End If
                                    AmbiguousCount += 1
                                End If

                            Else
                                If AmbiguousCount <> MethodIndex Then
                                    match(AmbiguousCount) = match(MethodIndex)
                                    paramOrder(AmbiguousCount) = paramOrder(MethodIndex)
                                    ParamArrayIndexList(AmbiguousCount) = ParamArrayIndexList(MethodIndex)
                                    match(MethodIndex) = Nothing
                                End If
                                AmbiguousCount += 1
                            End If

                        Else
                            '   We don't care it is less specific
                            match(MethodIndex) = Nothing
                        End If

                    End If

                Next MethodIndex

                If (AmbiguousCount > 1) Then
                    'We have an ambiguous match, run through and remove
                    'shadowed members
                    For MethodIndex = 0 To match.GetUpperBound(0)

                        ThisMethod = match(MethodIndex)
                        If Not ThisMethod Is Nothing Then
                            '
                            'Run through the list and remove all the inherited members of this type
                            '
                            Dim j As Integer
                            For j = 0 To match.GetUpperBound(0)

                                If MethodIndex <> j AndAlso (Not match(j) Is Nothing) AndAlso
                                    (ThisMethod Is match(j) OrElse
                                        (ThisMethod.DeclaringType.IsSubclassOf(match(j).DeclaringType) AndAlso
                                         MethodsDifferOnlyByReturnType(ThisMethod, match(j)))) Then
                                    'If (Not ThisMethod.IsHideBySig) OrElse MethodsDifferOnlyByReturnType(ThisMethod, match(j)) Then
                                    ' ThisMethod Shadows the baseclass and match(j) should not be accessible
                                    ' to the caller
                                    match(j) = Nothing
                                    AmbiguousCount -= 1
                                End If

                            Next j
                        End If
                    Next
                    Diagnostics.Debug.Assert(AmbiguousCount > 0)
                    'Iterate through to force them to the top of the list
                    For MethodIndex = 0 To match.GetUpperBound(0)
                        If match(MethodIndex) Is Nothing Then
                            Dim j As Integer
                            Dim TmpMatch As MethodBase
                            For j = MethodIndex + 1 To match.GetUpperBound(0)
                                TmpMatch = match(j)
                                If Not TmpMatch Is Nothing Then
                                    match(MethodIndex) = TmpMatch
                                    paramOrder(MethodIndex) = paramOrder(j)
                                    ParamArrayIndexList(MethodIndex) = ParamArrayIndexList(j)
                                    match(j) = Nothing
                                End If
                            Next j
                        End If
                    Next MethodIndex
                End If

                If (AmbiguousCount > 1) Then
                    Dim Msg As String = ControlChars.CrLf & "    " & MethodToString(match(0))
                    For MethodIndex = 1 To AmbiguousCount - 1
                        Msg = Msg & ControlChars.CrLf & "    " & MethodToString(match(MethodIndex))
                    Next MethodIndex

                    Select Case LowestScore
                        Case BindScore.Exact
                            Throw New AmbiguousMatchException(GetResourceString(SR.AmbiguousCall_ExactMatch2, CalledMethodName(), Msg))

                        Case BindScore.Widening0, BindScore.Widening1
                            Throw New AmbiguousMatchException(GetResourceString(SR.AmbiguousCall_WideningConversion2, CalledMethodName(), Msg))

                        Case Else
                            'BindScore.Narrowing
                            'BindScore.Unknown
                            Throw New AmbiguousMatchException(GetResourceString(SR.AmbiguousCall2, CalledMethodName(), Msg))
                    End Select
                End If

            End If

            SelectedMatch = match(SelectedIndex)

            ArgIndexes = CType(paramOrder(SelectedIndex), Integer())

            If (Not names Is Nothing) Then
                ReorderParams(ArgIndexes, args, state)
            End If

            Dim parms() As ParameterInfo = SelectedMatch.GetParameters()

            If args.Length > 0 Then
                state.m_ByRefFlags = New Boolean(args.GetUpperBound(0)) {}

                'Could have been multiple matches, so check the 
                'selected match
                HasByRefArgs = False

                For ParmIndex = 0 To parms.GetUpperBound(0)
                    If parms(ParmIndex).ParameterType.IsByRef Then
                        If state.m_OriginalParamOrder Is Nothing Then
                            If ParmIndex < state.m_ByRefFlags.Length Then
                                state.m_ByRefFlags(ParmIndex) = True
                            End If
                        Else
                            If ParmIndex < state.m_OriginalParamOrder.Length Then
                                Dim OriginalParmIndex As Integer = state.m_OriginalParamOrder(ParmIndex)
                                If OriginalParmIndex >= 0 Then
                                    state.m_ByRefFlags(OriginalParmIndex) = True
                                End If
                            End If
                        End If
                        HasByRefArgs = True
                    End If
                Next

                If Not HasByRefArgs Then
                    state.m_ByRefFlags = Nothing
                End If

            Else
                state.m_ByRefFlags = Nothing
            End If

            ' If the parameters and the args are not the same length
            ' then we need to create an argument array.

            ParamArrayIndex = ParamArrayIndexList(SelectedIndex)
            If ParamArrayIndex <> PARAMARRAY_NONE Then

                LastParmIndexToCheck = parms.GetUpperBound(0)
                If IsPropertySet Then
                    LastParmIndexToCheck -= 1
                End If

                LastArgIndexToCheck = args.GetUpperBound(0)
                If IsPropertySet Then
                    LastArgIndexToCheck -= 1
                End If

                'Fill in the non-paramarray arguments
                Dim objs() As Object = New Object(parms.Length - 1) {}

                'Assign arguments before the paramarray
                For ParmIndex = 0 To Math.Min(LastArgIndexToCheck, ParamArrayIndex) - 1
                    objs(ParmIndex) = ObjectType.CTypeHelper(args(ParmIndex), parms(ParmIndex).ParameterType)
                Next ParmIndex

                'Assign default values of missing arguments
                If LastArgIndexToCheck < ParamArrayIndex Then
                    For ParmIndex = LastArgIndexToCheck + 1 To ParamArrayIndex - 1
                        objs(ParmIndex) = ObjectType.CTypeHelper(parms(ParmIndex).DefaultValue, parms(ParmIndex).ParameterType)
                    Next ParmIndex
                End If

                'Fill in the Set value if we are doing a property or field set
                If IsPropertySet Then
                    Dim SetValueIndex As Integer = objs.GetUpperBound(0)
                    objs(SetValueIndex) = ObjectType.CTypeHelper(args(args.GetUpperBound(0)), parms(SetValueIndex).ParameterType)
                End If

                If LastArgIndexToCheck = -1 Then
                    'No arguments, just pack the empty paramarray

                    'Stuff the Object array into the last (non-setvalue) argument
                    objs(ParamArrayIndex) = System.Array.CreateInstance(ParamArrayElementType, 0)
                Else
                    ParamArrayElementType = parms(LastParmIndexToCheck).ParameterType.GetElementType()

                    Dim ParamArrayLength As Integer = args.Length - parms.Length + 1

                    ParmType = parms(LastParmIndexToCheck).ParameterType
                    If ParamArrayLength = 1 AndAlso ParmType.IsArray AndAlso (args(ParamArrayIndex) Is Nothing OrElse ParmType.IsInstanceOfType(args(ParamArrayIndex))) Then
                        objs(ParamArrayIndex) = args(ParamArrayIndex)

                    Else

                        If ParamArrayElementType Is GetType(Object) Then
                            'Special handling for Object() paramarray
                            Dim ObjArray() As Object = New Object(ParamArrayLength - 1) {}
                            For ArgIndex = 0 To ParamArrayLength - 1
                                ObjArray(ArgIndex) = ObjectType.CTypeHelper(args(ArgIndex + ParamArrayIndex), ParamArrayElementType)
                            Next ArgIndex
                            'Stuff the Object array into the last argument
                            objs(ParamArrayIndex) = ObjArray
                        Else
                            'Special handling for non-Object() paramarray
                            Dim TypeArray As System.Array = System.Array.CreateInstance(ParamArrayElementType, ParamArrayLength)
                            For ArgIndex = 0 To ParamArrayLength - 1
                                TypeArray.SetValue(ObjectType.CTypeHelper(args(ArgIndex + ParamArrayIndex), ParamArrayElementType), ArgIndex)
                            Next ArgIndex
                            'Stuff the Object array into the last argument
                            objs(ParamArrayIndex) = TypeArray
                        End If

                    End If

                End If
                args = objs
            Else
                Dim objs() As Object = New Object(parms.Length - 1) {}
                Dim MappedArgIndex As Integer

                For ArgIndex = 0 To objs.GetUpperBound(0)
                    MappedArgIndex = ArgIndexes(ArgIndex)
                    If MappedArgIndex >= 0 AndAlso MappedArgIndex <= args.GetUpperBound(0) Then
                        objs(ArgIndex) = ObjectType.CTypeHelper(args(MappedArgIndex), parms(ArgIndex).ParameterType)
                    Else
                        objs(ArgIndex) = ObjectType.CTypeHelper(parms(ArgIndex).DefaultValue, parms(ArgIndex).ParameterType)
                    End If
                Next ArgIndex

                For ParmIndex = ArgIndex To parms.GetUpperBound(0)
                    objs(ParmIndex) = ObjectType.CTypeHelper(parms(ParmIndex).DefaultValue, parms(ParmIndex).ParameterType)
                Next
                args = objs
            End If

            'Step XX - Change arguments to correct type for calling method
            '
            Debug.Assert(Not SelectedMatch Is Nothing, "Should have already thrown an exception")
            If SelectedMatch Is Nothing Then
                Throw New MissingMemberException(GetResourceString(SR.NoMethodTakingXArguments2, CalledMethodName(), CStr(GetPropArgCount(args, IsPropertySet))))
            End If
            Return SelectedMatch

        End Function

        Private Function GetPropArgCount(ByVal args As Object(), ByVal IsPropertySet As Boolean) As Integer
            If IsPropertySet Then
                Return args.Length - 1
            Else
                Return args.Length
            End If
        End Function

        Private Function GetMostSpecific(ByVal match0 As MethodBase, ByVal ThisMethod As MethodBase, ByVal ArgIndexes() As Integer, ByVal ParamOrder As Object(), ByVal IsPropertySet As Boolean, ByVal ParamArrayIndex0 As Integer, ByVal ParamArrayIndex1 As Integer, ByVal args As Object()) As Integer

            Dim AmbigParams() As ParameterInfo
            Dim Parameters() As ParameterInfo
            Dim Type0, Type1 As Type
            Dim MostSpecific As Integer = -1
            Dim AmbigArgIndexes() As Integer
            Dim ParmIndex As Integer
            Dim Index0, Index1 As Integer
            Dim ParamArrayElementType0 As Type = Nothing
            Dim ParamArrayElementType1 As Type = Nothing
            Dim LastNonSetValueIndex0, LastNonSetValueIndex1, LastNonSetValueIndexArgs As Integer
            Dim ParamArrayExpanded0, ParamArrayExpanded1 As Boolean
            Dim ArgCountUpperBound As Integer = args.GetUpperBound(0)

            Parameters = ThisMethod.GetParameters()

            AmbigParams = match0.GetParameters()
            AmbigArgIndexes = CType(ParamOrder(0), Integer())
            MostSpecific = -1

            LastNonSetValueIndexArgs = args.GetUpperBound(0)
            LastNonSetValueIndex0 = AmbigParams.GetUpperBound(0)
            LastNonSetValueIndex1 = Parameters.GetUpperBound(0)
            If IsPropertySet Then
                LastNonSetValueIndex0 -= 1
                LastNonSetValueIndex1 -= 1
                LastNonSetValueIndexArgs -= 1
                ArgCountUpperBound -= 1
            End If

            If ParamArrayIndex0 = PARAMARRAY_NONE Then
                ParamArrayExpanded0 = False
            Else
                ParamArrayElementType0 = AmbigParams(ParamArrayIndex0).ParameterType.GetElementType()
                ParamArrayExpanded0 = True
                If (LastNonSetValueIndexArgs <> PARAMARRAY_NONE) AndAlso (LastNonSetValueIndexArgs = LastNonSetValueIndex0) Then
                    Dim objTmp As Object = args(LastNonSetValueIndexArgs)
                    If (objTmp Is Nothing) OrElse (AmbigParams(LastNonSetValueIndex0).ParameterType.IsInstanceOfType(objTmp)) Then
                        ParamArrayExpanded0 = False
                    End If
                End If
            End If

            If ParamArrayIndex1 = PARAMARRAY_NONE Then
                ParamArrayExpanded1 = False
            Else
                ParamArrayElementType1 = Parameters(ParamArrayIndex1).ParameterType.GetElementType()
                ParamArrayExpanded1 = True
                If (LastNonSetValueIndexArgs <> PARAMARRAY_NONE) AndAlso (LastNonSetValueIndexArgs = LastNonSetValueIndex1) Then
                    Dim objTmp As Object = args(LastNonSetValueIndexArgs)
                    If (objTmp Is Nothing) OrElse (Parameters(LastNonSetValueIndex1).ParameterType.IsInstanceOfType(objTmp)) Then
                        ParamArrayExpanded1 = False
                    End If
                End If
            End If

            For ParmIndex = 0 To Math.Min(ArgCountUpperBound, Math.Max(LastNonSetValueIndex0, LastNonSetValueIndex1))

                If ParmIndex <= LastNonSetValueIndex0 Then
                    Index0 = AmbigArgIndexes(ParmIndex)
                Else
                    Index0 = -1
                End If

                If ParmIndex <= LastNonSetValueIndex1 Then
                    Index1 = ArgIndexes(ParmIndex)
                Else
                    Index1 = -1
                End If

                If Index0 = -1 AndAlso Index1 = -1 Then
                    'Both are optional and thus equal

                ElseIf (ParamArrayExpanded1 AndAlso ParamArrayIndex1 <> PARAMARRAY_NONE AndAlso ParmIndex >= ParamArrayIndex1) Then

                    'Paramarray, so everything else must be equal or could be basetype diffs
                    If ParamArrayExpanded0 AndAlso ParamArrayIndex0 <> PARAMARRAY_NONE AndAlso ParmIndex >= ParamArrayIndex0 Then
                        Type0 = ParamArrayElementType0
                    Else
                        Type0 = AmbigParams(Index0).ParameterType
                        If Type0.IsByRef Then
                            Type0 = Type0.GetElementType()
                        End If
                    End If

                    If ParamArrayElementType1 Is Type0 Then
                        'Identical
                        If MostSpecific = -1 AndAlso ParamArrayIndex0 = PARAMARRAY_NONE AndAlso ParmIndex = LastNonSetValueIndex0 AndAlso
                            (Not args(LastNonSetValueIndex0) Is Nothing) Then
                            MostSpecific = 0
                        End If

                    ElseIf ObjectType.IsWideningConversion(Type0, ParamArrayElementType1) Then
                        'match(0) is a less widening conversion
                        'up to this argument
                        If MostSpecific <> 1 Then
                            MostSpecific = 0
                        Else
                            MostSpecific = -1
                            Exit For
                        End If

                    ElseIf ObjectType.IsWideningConversion(ParamArrayElementType1, Type0) Then
                        'match(MethodIndex) is a less widening conversion
                        'up to this argument
                        If MostSpecific <> 0 Then
                            MostSpecific = 1
                        Else
                            MostSpecific = -1
                            Exit For
                        End If
                    End If

                ElseIf (ParamArrayExpanded0 AndAlso ParamArrayIndex0 <> PARAMARRAY_NONE AndAlso ParmIndex >= ParamArrayIndex0) Then

                    'Paramarray, so everything else must be equal or could be basetype diffs
                    If ParamArrayExpanded1 AndAlso ParamArrayIndex1 <> PARAMARRAY_NONE AndAlso ParmIndex >= ParamArrayIndex1 Then
                        Type1 = ParamArrayElementType1
                    Else
                        Type1 = Parameters(Index1).ParameterType
                        If Type1.IsByRef Then
                            Type1 = Type1.GetElementType()
                        End If
                    End If

                    If ParamArrayElementType0 Is Type1 Then
                        'Identical
                        If MostSpecific = -1 AndAlso ParamArrayIndex1 = PARAMARRAY_NONE AndAlso ParmIndex = LastNonSetValueIndex1 AndAlso
                            (Not args(LastNonSetValueIndex1) Is Nothing) Then
                            MostSpecific = 1
                        End If

                    ElseIf ObjectType.IsWideningConversion(ParamArrayElementType0, Type1) Then
                        'match(0) is a less widening conversion
                        'up to this argument
                        If MostSpecific <> 1 Then
                            MostSpecific = 0
                        Else
                            MostSpecific = -1
                            Exit For
                        End If

                    ElseIf ObjectType.IsWideningConversion(Type1, ParamArrayElementType0) Then
                        'match(MethodIndex) is a less widening conversion
                        'up to this argument
                        If MostSpecific <> 0 Then
                            MostSpecific = 1
                        Else
                            MostSpecific = -1
                            Exit For
                        End If
                    End If

                Else
                    Type0 = AmbigParams(AmbigArgIndexes(ParmIndex)).ParameterType
                    Type1 = Parameters(ArgIndexes(ParmIndex)).ParameterType

                    If Type0 Is Type1 Then
                        'Neither is more specific

                    ElseIf ObjectType.IsWideningConversion(Type0, Type1) Then
                        'match(0) is a more derived class than match(MethodIndex)
                        'up to this argument
                        If MostSpecific <> 1 Then
                            MostSpecific = 0
                        Else
                            MostSpecific = -1
                            Exit For
                        End If
                    ElseIf ObjectType.IsWideningConversion(Type1, Type0) Then
                        'match(MethodIndex) is a more derived class than match(0)
                        'up to this argument
                        If MostSpecific <> 0 Then
                            MostSpecific = 1
                        Else
                            MostSpecific = -1
                            Exit For
                        End If
                    ElseIf ObjectType.IsWiderNumeric(Type0, Type1) Then
                        'match(MethodIndex) is a more derived class than match(0)
                        'up to this argument
                        If MostSpecific <> 0 Then
                            MostSpecific = 1
                        Else
                            MostSpecific = -1
                            Exit For
                        End If
                    ElseIf ObjectType.IsWiderNumeric(Type1, Type0) Then
                        'match(0) is a more derived class than match(MethodIndex)
                        'up to this argument
                        If MostSpecific <> 1 Then
                            MostSpecific = 0
                        Else
                            MostSpecific = -1
                            Exit For
                        End If
                    Else
                        MostSpecific = -1
                    End If
                End If
            Next ParmIndex

            If MostSpecific = -1 Then
                If (ParamArrayIndex0 = PARAMARRAY_NONE OrElse Not ParamArrayExpanded0) AndAlso ParamArrayIndex1 <> PARAMARRAY_NONE Then
                    If ParamArrayExpanded1 AndAlso MatchesParamArraySignature(AmbigParams, Parameters, ParamArrayIndex1, IsPropertySet, ArgCountUpperBound) Then
                        MostSpecific = 0
                    End If

                ElseIf (ParamArrayIndex1 = PARAMARRAY_NONE OrElse Not ParamArrayExpanded1) AndAlso ParamArrayIndex0 <> PARAMARRAY_NONE Then
                    If ParamArrayExpanded0 AndAlso MatchesParamArraySignature(Parameters, AmbigParams, ParamArrayIndex0, IsPropertySet, ArgCountUpperBound) Then
                        MostSpecific = 1
                    End If

                End If

            End If

            Return MostSpecific
        End Function

        Private Function MatchesParamArraySignature(ByVal param0 As ParameterInfo(), ByVal param1 As ParameterInfo(), ByVal ParamArrayIndex1 As Integer, ByVal IsPropertySet As Boolean, ByVal ArgCountUpperBound As Integer) As Boolean

            Dim i As Integer
            Dim paramType0, paramType1 As Type
            Dim UpperBound As Integer

            UpperBound = param0.GetUpperBound(0)
            If IsPropertySet Then
                UpperBound -= 1
            End If
            UpperBound = System.Math.Min(UpperBound, ArgCountUpperBound)

            For i = 0 To UpperBound

                paramType0 = param0(i).ParameterType
                If paramType0.IsByRef Then
                    paramType0 = paramType0.GetElementType()
                End If

                If i >= ParamArrayIndex1 Then
                    paramType1 = param1(ParamArrayIndex1).ParameterType
                    paramType1 = paramType1.GetElementType()
                Else
                    paramType1 = param1(i).ParameterType
                    If paramType1.IsByRef Then
                        paramType1 = paramType1.GetElementType()
                    End If
                End If

                If Not paramType0 Is paramType1 Then
                    Return False
                End If
            Next i

            Return True
        End Function

        Private Function MethodsDifferOnlyByReturnType(ByVal match1 As MethodBase, ByVal match2 As MethodBase) As Boolean
            Dim p1(), p2() As ParameterInfo

            If match1 Is match2 Then
                'Both are nothing

            End If
            p1 = match1.GetParameters()
            p2 = match2.GetParameters()


            Dim i As Integer
            Dim paramType1, paramType2 As Type
            Dim UpperBound As Integer

            UpperBound = System.Math.Min(p1.GetUpperBound(0), p2.GetUpperBound(0))

            For i = 0 To UpperBound
                paramType1 = p1(i).ParameterType
                If paramType1.IsByRef Then
                    paramType1 = paramType1.GetElementType()
                End If
                paramType2 = p2(i).ParameterType
                If paramType2.IsByRef Then
                    paramType2 = paramType2.GetElementType()
                End If
                If Not paramType1 Is paramType2 Then
                    Return False
                End If
            Next i

            If p1.Length > p2.Length Then
                'Optional arguments could also cause a match
                For i = UpperBound + 1 To p2.GetUpperBound(0)
                    If Not p1(i).IsOptional Then
                        Return False
                    End If
                Next i

            ElseIf p2.Length > p1.Length Then
                'Optional arguments could also cause a match
                For i = UpperBound + 1 To p1.GetUpperBound(0)
                    If Not p2(i).IsOptional Then
                        Return False
                    End If
                Next i

            End If

            Return True

        End Function

        ' *
        ' * Fields can have no arguments, so just choose the method on the outermost type
        ' *
        Public Overrides Function BindToField(ByVal bindingAttr As BindingFlags, ByVal match() As FieldInfo, ByVal value As Object, ByVal culture As CultureInfo) As FieldInfo
            'Only the outermost definition can be called
            Dim i As Integer

            If (Not m_CachedMember Is Nothing) AndAlso (m_CachedMember.MemberType = MemberTypes.Field) AndAlso (Not match(0) Is Nothing) AndAlso
                (match(0).Name = m_CachedMember.Name) Then
                Return DirectCast(m_CachedMember, FieldInfo)
            End If

            BindToField = match(0)
            For i = 1 To match.GetUpperBound(0)
                If match(i).DeclaringType.IsSubclassOf(BindToField.DeclaringType) Then
                    BindToField = match(i)
                End If
            Next i

        End Function

        ' *
        ' * Given a of methods that match the base criteria, select a method based
        ' * upon an array of types.  This method should return null If no method matchs
        ' * the criteria.
        ' *
        Public Overrides Function SelectMethod(ByVal bindingAttr As BindingFlags, ByVal match() As MethodBase, ByVal types() As Type, ByVal modifiers() As ParameterModifier) As MethodBase
            Throw New NotSupportedException
        End Function

        '/**
        ' * Given a of properties that match the base criteria, select one.
        ' */
        Public Overrides Function SelectProperty(ByVal bindingAttr As BindingFlags, ByVal match() As PropertyInfo, ByVal returnType As Type, ByVal indexes() As Type, ByVal modifiers() As ParameterModifier) As PropertyInfo

            ' Walk all of the methods looking the most specific method based on the index types
            Dim AmbiguousCount As Integer
            Dim Score, LowestScore As BindScore
            Dim ThisProperty As PropertyInfo
            Dim PropertyIndex, ParmIndex As Integer
            Dim Parameters, AmbigParams As ParameterInfo()
            Dim Type0, Type1 As Type

            LowestScore = BindScore.Unknown
            AmbiguousCount = 0

            ' Score each method 
            '    0 ==> Exact match
            '    1 ==> Casting down conversion required
            '    2 ==> Intrinsic widening conversion required
            '    3 ==> Requires narrowing conversion

            For PropertyIndex = 0 To match.GetUpperBound(0)

                ThisProperty = match(PropertyIndex)

                If ThisProperty Is Nothing Then
                    'Skip it
                Else

                    Score = BindingScore(ThisProperty.GetIndexParameters(), Nothing, indexes, False, PARAMARRAY_NONE)

                    If Score < LowestScore Then
                        If PropertyIndex <> 0 Then
                            match(0) = match(PropertyIndex)
                            match(PropertyIndex) = Nothing
                        End If
                        AmbiguousCount = 1
                        LowestScore = Score

                    ElseIf Score = LowestScore Then

                        If Score = BindScore.Widening1 Then

                            'It is possible that one is more 
                            ' precise than another
                            Dim MostSpecific As Integer = -1
                            Dim Index0, Index1 As Integer

                            Parameters = ThisProperty.GetIndexParameters()

                            AmbigParams = match(0).GetIndexParameters()

                            MostSpecific = -1

                            For ParmIndex = 0 To Parameters.GetUpperBound(0)
                                Index0 = ParmIndex
                                Index1 = ParmIndex
                                If Index0 = -1 OrElse Index1 = -1 Then
                                    'Both are optional and thus equal
                                Else
                                    Type0 = AmbigParams(Index0).ParameterType
                                    Type1 = Parameters(Index1).ParameterType

                                    If ObjectType.IsWideningConversion(Type0, Type1) Then
                                        'match(0) is a less widening conversion
                                        'up to this argument
                                        If MostSpecific <> 1 Then
                                            MostSpecific = 0
                                        Else
                                            MostSpecific = -1
                                            Exit For
                                        End If
                                    ElseIf ObjectType.IsWideningConversion(Type1, Type0) Then
                                        'match(PropertyIndex) is a less widening conversion
                                        'up to this argument
                                        If MostSpecific <> 0 Then
                                            MostSpecific = 1
                                        Else
                                            MostSpecific = -1
                                            Exit For
                                        End If
                                    End If
                                End If

                            Next ParmIndex

                            If MostSpecific = -1 Then
                                If AmbiguousCount <> PropertyIndex Then
                                    match(AmbiguousCount) = match(PropertyIndex)
                                    match(PropertyIndex) = Nothing
                                End If
                                AmbiguousCount += 1

                            ElseIf MostSpecific = 0 Then
                                AmbiguousCount = 1

                            Else 'If MostSpecific = 1 Then
                                If PropertyIndex <> 0 Then
                                    match(0) = match(PropertyIndex)
                                    'paramOrder(0) = paramOrder(PropertyIndex)
                                    'ParamArrayIndexList(0) = ParamArrayIndexList(PropertyIndex)
                                    match(PropertyIndex) = Nothing
                                End If
                                AmbiguousCount = 1
                            End If

                        ElseIf Score = BindScore.Exact Then

                            'Must be a shadowed member
                            If ThisProperty.DeclaringType.IsSubclassOf(match(0).DeclaringType) Then
                                If PropertyIndex <> 0 Then
                                    match(0) = match(PropertyIndex)
                                    match(PropertyIndex) = Nothing
                                End If
                                AmbiguousCount = 1

                            ElseIf match(0).DeclaringType.IsSubclassOf(ThisProperty.DeclaringType) Then
                                'Keep the first match

                            Else
                                If AmbiguousCount <> PropertyIndex Then
                                    match(AmbiguousCount) = match(PropertyIndex)
                                    match(PropertyIndex) = Nothing
                                End If
                                AmbiguousCount += 1

                            End If

                        Else
                            If AmbiguousCount <> PropertyIndex Then
                                match(AmbiguousCount) = match(PropertyIndex)
                                match(PropertyIndex) = Nothing
                            End If
                            AmbiguousCount += 1
                        End If

                    Else
                        '   We don't care it is less specific
                        match(PropertyIndex) = Nothing
                    End If

                End If

            Next PropertyIndex

            If AmbiguousCount = 1 Then
                Return match(0)
            Else 'If AmbiguousCount = 0 Then
                Return Nothing
            End If

        End Function

        '/**
        ' * ChangeType
        ' * The default binder doesn't support any change type functionality.
        ' * This is because the default is built into the low level invoke code.
        ' */
        Public Overrides Function ChangeType(ByVal value As Object, ByVal typ As Type, ByVal culture As CultureInfo) As Object

            Try
                If (typ Is GetType(System.Object)) OrElse (typ.IsByRef AndAlso typ.GetElementType() Is GetType(System.Object)) Then
                    Return value
                Else
                    Return ObjectType.CTypeHelper(value, typ)
                End If
            Catch ex As Exception
                Throw New InvalidCastException(GetResourceString(SR.InvalidCast_FromTo, VBFriendlyName(value), VBFriendlyName(typ)))
            End Try
        End Function

        Private Function BindingScore(ByVal Parameters() As ParameterInfo, ByVal paramOrder() As Integer, ByVal ArgTypes() As Type, ByVal IsPropertySet As Boolean, ByVal ParamArrayIndex As Integer) As BindScore

            Dim Score As BindScore
            Dim ArgType, ParmType As Type
            Dim ArgIndex, ParmIndex As Integer
            Dim LastArgNonSetValueIndex, LastParamNonSetValueIndex As Integer

            Score = BindScore.Exact

            LastArgNonSetValueIndex = ArgTypes.GetUpperBound(0)
            LastParamNonSetValueIndex = Parameters.GetUpperBound(0)
            If IsPropertySet Then
                LastParamNonSetValueIndex -= 1
                LastArgNonSetValueIndex -= 1
            End If

            For ParmIndex = 0 To Math.Max(LastArgNonSetValueIndex, LastParamNonSetValueIndex)

                If paramOrder Is Nothing Then
                    ArgIndex = ParmIndex
                Else
                    ArgIndex = paramOrder(ParmIndex)
                End If

                If ArgIndex = -1 Then
                    ArgType = Nothing
                Else
                    ArgType = ArgTypes(ArgIndex)
                End If

                If ArgType Is Nothing Then
                    'Treat as zero

                Else
                    If ParmIndex > LastParamNonSetValueIndex Then
                        ParmType = Parameters(ParamArrayIndex).ParameterType
                    Else
                        ParmType = Parameters(ParmIndex).ParameterType
                    End If

                    If ParmIndex = ParamArrayIndex AndAlso ArgType.IsArray() AndAlso ParmType Is ArgType Then
                        'BindScore.Exact - default, don't overwrite current value
                    ElseIf ParmIndex = ParamArrayIndex AndAlso ArgType.IsArray() AndAlso
                        (m_state.m_OriginalArgs Is Nothing OrElse m_state.m_OriginalArgs(ArgIndex) Is Nothing OrElse ParmType.IsInstanceOfType(m_state.m_OriginalArgs(ArgIndex))) Then
                        If Score < BindScore.Widening1 Then
                            Score = BindScore.Widening1
                        End If
                    Else
                        If ParamArrayIndex <> ARG_MISSING AndAlso ParmIndex >= ParamArrayIndex OrElse ParmType.IsByRef Then
                            ParmType = ParmType.GetElementType()
                        End If

                        ' If the two types are exact move on...
                        If (ArgType Is ParmType) Then
                            'BindScore.Exact - default, don't overwrite current value

                        ElseIf ObjectType.IsWideningConversion(ArgType, ParmType) Then
                            If Score < BindScore.Widening1 Then
                                Score = BindScore.Widening1
                            End If

                            'This ElseIf is most likely covered by the above IsWidening call
                        ElseIf ArgType.IsArray() AndAlso
                            (m_state.m_OriginalArgs Is Nothing OrElse m_state.m_OriginalArgs(ArgIndex) Is Nothing OrElse ParmType.IsInstanceOfType(m_state.m_OriginalArgs(ArgIndex))) Then
                            If Score < BindScore.Widening1 Then
                                Score = BindScore.Widening1
                            End If

                        Else
                            Score = BindScore.Narrowing

                        End If
                    End If


                End If

            Next ParmIndex

            Return Score

        End Function

        ' This method will sort the vars array into the mapping order stored
        ' in the paramOrder array.
        Private Sub ReorderParams(ByVal paramOrder() As Integer, ByVal vars() As Object, ByVal state As VBBinderState)

            Dim i As Integer
            'paramOrder.GetUpperBound(0) should always be the MAX
            Dim ArrayUBound As Integer = Math.Max(vars.GetUpperBound(0), paramOrder.GetUpperBound(0))

            state.m_OriginalParamOrder = New Integer(ArrayUBound) {}

            For i = 0 To ArrayUBound

                state.m_OriginalParamOrder(i) = paramOrder(i)

            Next i

        End Sub

        ' This method will create the mapping between the Parameters and the underlying
        ' data based upon the names array.  The names array is stored in the same order
        ' as the values and maps to the parameters of the method.  We store the mapping
        ' from the parameters to the names in the paramOrder array.  All parameters that
        ' don't have matching names are then stored in the array in order.
        Private Function CreateParamOrder(ByVal SetProp As Boolean, ByVal paramOrder() As Integer, ByVal pars() As ParameterInfo, ByVal args() As Object, ByVal names() As String) As Exception

            Dim used() As Boolean = New Boolean(pars.Length - 1) {}
            Dim i, j As Integer
            Dim LastUnnamedIndex As Integer = (args.Length - names.Length - 1)
            Dim LastNonSetIndex As Integer = pars.GetUpperBound(0)

            ' Mark which parameters have not been found in the names list
            For i = 0 To pars.GetUpperBound(0)
                paramOrder(i) = ARG_MISSING
            Next i

            If SetProp Then
                'The last unnamed argument is the Set value
                ' and cannot be moved from that spot
                paramOrder(pars.GetUpperBound(0)) = args.GetUpperBound(0)
                LastUnnamedIndex -= 1
                LastNonSetIndex -= 1
            End If

            'Unnamed parameters must be used as the first arguments
            For i = 0 To LastUnnamedIndex
                paramOrder(i) = names.Length + i
            Next i

            ' Find the parameters with names. 
            For i = 0 To names.GetUpperBound(0)

                For j = 0 To LastNonSetIndex

                    If StrComp(names(i), pars(j).Name, CompareMethod.Text) = 0 Then

                        If paramOrder(j) <> -1 Then
                            Return New ArgumentException(GetResourceString(SR.NamedArgumentAlreadyUsed1, pars(j).Name))
                        End If

                        paramOrder(j) = i
                        used(i) = True
                        Exit For

                    End If

                Next j

                ' This is an error condition.  The name was not found.  This
                ' method must not match what we sent.
                If (j > LastNonSetIndex) Then
                    'Should no longer hit this, since we removed all these cases in a previous step
                    Return New MissingMemberException(GetResourceString(SR.Argument_InvalidNamedArg2, names(i), CalledMethodName()))
                End If

            Next i

            Return Nothing

        End Function

        <SecuritySafeCritical()>
        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Friend Function InvokeMember(ByVal name As String,
            ByVal invokeAttr As BindingFlags,
            ByVal objType As System.Type,
            ByVal objIReflect As IReflect,
            ByVal target As Object,
            ByVal args As Object(),
            ByVal namedParameters As String()) As Object

            If objType.IsCOMObject() Then
                Dim modifiers As ParameterModifier() = Nothing

                Try
                    '
                    'BEGIN: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
                    '
                    Return objIReflect.InvokeMember(name, invokeAttr, Nothing, target, args, modifiers, Nothing, namedParameters)

                    '
                    'END: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
                    '
                Catch ex As MissingMemberException
                    Throw New MissingMemberException(GetResourceString(SR.MissingMember_MemberNotFoundOnType2, name, VBFriendlyName(objType)))
                End Try
            End If

            m_BindToName = name
            m_objType = objType

#If DEBUG Then
            If (Not namedParameters Is Nothing) Then
                For i As Integer = 0 To namedParameters.GetUpperBound(0)
                    If (namedParameters(i) Is Nothing) Then
                        Diagnostics.Debug.Assert(False, "Should never be reached")
                        Throw New ArgumentException
                    End If
                Next i
            End If
#End If

            Debug.Assert((invokeAttr And BindingFlags.CreateInstance) = 0, "CreateInstance not supported")
            ' For fields, methods and properties the name must be specified.
            Debug.Assert(Not name Is Nothing, "Invalid argument")

            ' if we are looking for the default member, find it...
            If (name.Length = 0) Then
                If (objType Is objIReflect) Then
                    name = GetDefaultMemberName(objType)
                    If (name Is Nothing) Then
                        Throw New MissingMemberException(GetResourceString(SR.MissingMember_NoDefaultMemberFound1, VBFriendlyName(objType)))
                    End If
                Else
                    ' IReflect case, we pass in empty string so that user implementation can return default members determined at run time
                    name = ""
                End If
            End If

            Dim p As MethodBase()
            Dim invokeMethod As MethodBase

            p = GetMethodsByName(objType, objIReflect, name, invokeAttr)
            If (args Is Nothing) Then
                args = New Object() {}
            End If

            Dim binderState As Object = Nothing
            invokeMethod = Me.BindToMethod(invokeAttr, p, args, Nothing, Nothing, namedParameters, binderState)
            If (invokeMethod Is Nothing) Then
                Throw New MissingMemberException(GetResourceString(SR.NoMethodTakingXArguments2, CalledMethodName(), CStr(GetPropArgCount(args, (invokeAttr And BindingFlags.SetProperty) <> 0))))
            End If

            '
            'BEGIN: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
            '
            SecurityCheckForLateboundCalls(invokeMethod, objType, objIReflect)

            Dim Method As MethodInfo = DirectCast(invokeMethod, MethodInfo)
            Dim res As Object
            If objType Is objIReflect OrElse Method.IsStatic OrElse
                DoesTargetObjectMatch(target, Method) Then

                VerifyObjRefPresentForInstanceCall(target, Method)

                res = Method.Invoke(target, args)

            Else
                res = InvokeMemberOnIReflect(objIReflect, Method, BindingFlags.InvokeMethod, target, args)
            End If

            '
            'END: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
            '

            If (Not binderState Is Nothing) Then
                Me.ReorderArgumentArray(args, binderState)
            End If
            Return res

        End Function

        Private Function GetDefaultMemberName(ByVal typ As Type) As String

            Dim attributeList As Object()

            Do
                attributeList = typ.GetCustomAttributes(GetType(DefaultMemberAttribute), False)
                If (Not attributeList Is Nothing) AndAlso (attributeList.Length <> 0) Then
                    Return CType(attributeList(0), DefaultMemberAttribute).MemberName
                End If
                typ = typ.BaseType
            Loop While (Not typ Is Nothing)

            Return Nothing

        End Function

        Private Function GetMethodsByName(ByVal objType As System.Type, ByVal objIReflect As IReflect, ByVal name As String, ByVal invokeAttr As BindingFlags) As MethodBase()

            Dim mi As MemberInfo()
            Dim mb As MethodBase()
            Dim ThisMember As MemberInfo
            Dim MemberIndex As Integer
            Dim ThisMethod As MethodInfo
            Dim ThisProperty As PropertyInfo
            Dim DeclaringType As System.Type
            Dim RemovedCount As Integer

            mi = objIReflect.GetMember(name, invokeAttr)

            ' Filter out generic methods for compatibility with the Whidbey framework
            mi = GetNonGenericMembers(mi)

            If mi Is Nothing Then
                Return Nothing
            End If

            For MemberIndex = 0 To mi.GetUpperBound(0)

                ThisMember = mi(MemberIndex)

                If ThisMember Is Nothing Then
                    'Skip this one

                ElseIf ThisMember.MemberType = MemberTypes.Field Then
                    'Filter all subclass members
                    '
                    'Run through the list and remove all the inherited members of this type
                    '
                    DeclaringType = ThisMember.DeclaringType

                    Dim j As Integer

                    For j = 0 To mi.GetUpperBound(0)

                        If MemberIndex <> j AndAlso (Not mi(j) Is Nothing) AndAlso
                            DeclaringType.IsSubclassOf(mi(j).DeclaringType) Then
                            ' ThisMember Shadows the baseclass and ThatMethod should not be accessible
                            ' to the caller
                            mi(j) = Nothing
                            RemovedCount += 1
                        End If

                    Next j

                ElseIf ThisMember.MemberType = MemberTypes.Method Then

                    'Filter all subclass members
                    ThisMethod = CType(ThisMember, MethodInfo)

                    If ThisMethod.IsHideBySig Then
                        'Hide-by-sig - shadows exact name and sig on base types
                        '
                        'Don't bother filtering here, this will get done below for this case

                        'Non-virtual members shadow baseclass methods
                        'Virtual members with newslot flag shadow baseclass methods                    '
                        'Virtual members whose base definition has the newslot attribute shadow baseclass methods
                    ElseIf Not ThisMethod.IsVirtual OrElse
                        (ThisMethod.IsVirtual AndAlso ((ThisMethod.Attributes And MethodAttributes.NewSlot) <> 0)) OrElse
                        (ThisMethod.IsVirtual AndAlso ((ThisMethod.GetBaseDefinition().Attributes And MethodAttributes.NewSlot) <> 0)) Then
                        '
                        'Run through the list and remove all the inherited members of this type
                        '
                        Dim j As Integer

                        DeclaringType = ThisMember.DeclaringType

                        For j = 0 To mi.GetUpperBound(0)

                            If MemberIndex <> j AndAlso (Not mi(j) Is Nothing) AndAlso
                                DeclaringType.IsSubclassOf(mi(j).DeclaringType) Then
                                ' ThisMember Shadows the baseclass and ThatMethod should not be accessible
                                ' to the caller
                                mi(j) = Nothing
                                RemovedCount += 1
                            End If

                        Next j

                    End If

                ElseIf ThisMember.MemberType = MemberTypes.Property Then

                    ThisProperty = CType(ThisMember, PropertyInfo)

                    'Filter out all shadowed members first
                    Dim i As Integer

                    For i = 1 To 2

                        If i = 1 Then
                            ThisMethod = ThisProperty.GetGetMethod()
                        Else
                            ThisMethod = ThisProperty.GetSetMethod()
                        End If

                        If ThisMethod Is Nothing Then

                        ElseIf ThisMethod.IsHideBySig Then
                            'Hide-by-sig - shadows exact name and sig on base types
                            '
                            'Don't bother filtering here, this will get done below for this case

                        ElseIf Not ThisMethod.IsVirtual OrElse
                            (ThisMethod.IsVirtual AndAlso ((ThisMethod.Attributes And MethodAttributes.NewSlot) <> 0)) Then
                            '
                            'Run through the list and remove all the inherited members of this type
                            '
                            Dim j As Integer

                            DeclaringType = ThisMember.DeclaringType

                            For j = 0 To mi.GetUpperBound(0)

                                If MemberIndex <> j AndAlso (Not mi(j) Is Nothing) AndAlso
                                    DeclaringType.IsSubclassOf(mi(j).DeclaringType) Then
                                    ' ThisMember Shadows the baseclass and ThatMethod should not be accessible
                                    ' to the caller
                                    mi(j) = Nothing
                                    RemovedCount += 1
                                End If

                            Next j

                        End If
                    Next i

                    If (invokeAttr And BindingFlags.GetProperty) <> 0 Then
                        ThisMethod = ThisProperty.GetGetMethod()

                    ElseIf (invokeAttr And BindingFlags.SetProperty) <> 0 Then
                        ThisMethod = ThisProperty.GetSetMethod()

                    Else
                        ThisMethod = Nothing
                    End If

                    If ThisMethod Is Nothing Then
                        RemovedCount += 1
                    End If
                    mi(MemberIndex) = ThisMethod

                ElseIf ThisMember.MemberType = MemberTypes.NestedType Then

                    'Remove all shadowed members base types
                    Dim j As Integer

                    DeclaringType = ThisMember.DeclaringType

                    For j = 0 To mi.GetUpperBound(0)

                        If MemberIndex <> j AndAlso (Not mi(j) Is Nothing) AndAlso
                            DeclaringType.IsSubclassOf(mi(j).DeclaringType) Then
                            ' ThisMember Shadows the baseclass and ThatMethod should not be accessible
                            ' to the caller
                            mi(j) = Nothing
                            RemovedCount += 1
                        End If

                    Next j

                    If RemovedCount = mi.Length - 1 Then
                        Throw New ArgumentException(GetResourceString(SR.Argument_IllegalNestedType2, name, VBFriendlyName(objType)))
                    End If
                    mi(MemberIndex) = Nothing 'Remove the nested class, since we cannot use it
                    RemovedCount += 1

                End If
            Next

            'Compact the list
            Dim NewSize As Integer = mi.Length - RemovedCount

            mb = New MethodBase(NewSize - 1) {}
            Dim TargetIndex As Integer = 0
            For Index As Integer = 0 To mi.Length - 1
                If Not mi(Index) Is Nothing Then
                    mb(TargetIndex) = CType(mi(Index), MethodBase)
                    TargetIndex += 1
                End If
            Next

            Return mb

        End Function

        Friend Function CalledMethodName() As String
            Debug.Assert((Not m_objType Is Nothing) AndAlso (Not m_BindToName Is Nothing))
            Return m_objType.Name & "." & m_BindToName
        End Function

        '
        'BEGIN: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
        '
        Friend Shared Sub SecurityCheckForLateboundCalls(ByVal member As MemberInfo, ByVal objType As Type, ByVal objIReflect As IReflect)

            Dim declaringType As System.Type

            ' If we are using User provided IReflect Implementation instead of System.Type's IReflect Implementation and
            ' the Member Info is not public, then throw exception - VB latebinding only supports access of public members
            If (Not objType Is objIReflect) AndAlso (Not IsMemberPublic(member)) Then
                'No message text intentional - will get rethrown with more informative message
                Throw New MissingMethodException
            End If

            declaringType = member.DeclaringType

            ' For nested types IsNotPublic doesn't return the right value so
            ' we need to use Not IsPublic. 
            '
            ' The following code will only allow calls to members of top level public types
            ' in the runtime library. Read the reflection documentation and test with
            ' nested types before changing this code.

            If Not declaringType.IsPublic Then
                'Disallow latebound calls to internal Microsoft.VisualBasic types
                If declaringType.Assembly Is Utils.VBRuntimeAssembly Then
                    'No message text intentional - will get rethrown with more informative message
                    Throw New MissingMethodException
                End If
            End If

        End Sub

        Private Shared Function IsMemberPublic(ByVal Member As MemberInfo) As Boolean
            Debug.Assert(Not Member Is Nothing, "How can this be Nothing ?")

            Select Case Member.MemberType
                Case MemberTypes.Method
                    Return DirectCast(Member, MethodInfo).IsPublic

                Case MemberTypes.Field
                    Return DirectCast(Member, FieldInfo).IsPublic

                Case MemberTypes.Constructor
                    Return DirectCast(Member, ConstructorInfo).IsPublic

                Case MemberTypes.Property
                    Debug.Assert(False, "How can a property get here ?")
                    ' We always decided based on the context and use the get or the set accessor
                    ' appropriately. So by the time this method is invoked, we should just see 
                    ' the MethodInfos of the Getter or the Setter
                    Return False

                Case Else
                    ' No Assert here because users implementation of IReflect could return some bad stuff
                    ' return False here because VB Latebinding only supports Fields, Properties and Methods
                    Return False
            End Select
        End Function

        '
        'END: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
        '

        Friend Sub CacheMember(ByVal member As MemberInfo)
            m_CachedMember = member
        End Sub
    End Class

End Namespace

