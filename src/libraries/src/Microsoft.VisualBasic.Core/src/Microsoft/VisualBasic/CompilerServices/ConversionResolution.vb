' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Reflection
Imports System.Diagnostics
Imports System.Collections.Generic

Imports Microsoft.VisualBasic.CompilerServices.Symbols
Imports Microsoft.VisualBasic.CompilerServices.ConversionResolution
Imports Microsoft.VisualBasic.CompilerServices.ConversionResolution.OperatorCaches

Namespace Microsoft.VisualBasic.CompilerServices

    ' Implements VB conversion semantics.
    Friend Class ConversionResolution
        ' Prevent creation.
        Private Sub New()
        End Sub

        Friend Enum ConversionClass As SByte
            Bad
            Identity
            [Widening]
            [Narrowing]
            None
            Ambiguous
        End Enum

        Private Shared ReadOnly s_conversionTable As ConversionClass()()
        Friend Shared ReadOnly NumericSpecificityRank As Integer()
        Friend Shared ReadOnly ForLoopWidestTypeCode As TypeCode()()

        Shared Sub New()
            Const max As Integer = TypeCode.String

            Const bad_ As ConversionClass = ConversionClass.Bad
            Const iden As ConversionClass = ConversionClass.Identity
            Const wide As ConversionClass = ConversionClass.Widening
            Const narr As ConversionClass = ConversionClass.Narrowing
            Const none As ConversionClass = ConversionClass.None

            'Columns represent Source type, Rows represent Target type.
            '                                 empty obj   dbnul bool  char  sbyte byte  short ushrt int   uint  lng   ulng  sng   dbl   dec   date        str
            s_conversionTable = New ConversionClass(max)() _
                {
                    New ConversionClass(max) {bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_},
                    New ConversionClass(max) {bad_, iden, bad_, wide, wide, wide, wide, wide, wide, wide, wide, wide, wide, wide, wide, wide, wide, bad_, wide},
                    New ConversionClass(max) {bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_},
                    New ConversionClass(max) {bad_, narr, bad_, iden, none, narr, narr, narr, narr, narr, narr, narr, narr, narr, narr, narr, none, bad_, narr},
                    New ConversionClass(max) {bad_, narr, bad_, none, iden, none, none, none, none, none, none, none, none, none, none, none, none, bad_, narr},
                    New ConversionClass(max) {bad_, narr, bad_, narr, none, iden, narr, narr, narr, narr, narr, narr, narr, narr, narr, narr, none, bad_, narr},
                    New ConversionClass(max) {bad_, narr, bad_, narr, none, narr, iden, narr, narr, narr, narr, narr, narr, narr, narr, narr, none, bad_, narr},
                    New ConversionClass(max) {bad_, narr, bad_, narr, none, wide, wide, iden, narr, narr, narr, narr, narr, narr, narr, narr, none, bad_, narr},
                    New ConversionClass(max) {bad_, narr, bad_, narr, none, narr, wide, narr, iden, narr, narr, narr, narr, narr, narr, narr, none, bad_, narr},
                    New ConversionClass(max) {bad_, narr, bad_, narr, none, wide, wide, wide, wide, iden, narr, narr, narr, narr, narr, narr, none, bad_, narr},
                    New ConversionClass(max) {bad_, narr, bad_, narr, none, narr, wide, narr, wide, narr, iden, narr, narr, narr, narr, narr, none, bad_, narr},
                    New ConversionClass(max) {bad_, narr, bad_, narr, none, wide, wide, wide, wide, wide, wide, iden, narr, narr, narr, narr, none, bad_, narr},
                    New ConversionClass(max) {bad_, narr, bad_, narr, none, narr, wide, narr, wide, narr, wide, narr, iden, narr, narr, narr, none, bad_, narr},
                    New ConversionClass(max) {bad_, narr, bad_, narr, none, wide, wide, wide, wide, wide, wide, wide, wide, iden, narr, wide, none, bad_, narr},
                    New ConversionClass(max) {bad_, narr, bad_, narr, none, wide, wide, wide, wide, wide, wide, wide, wide, wide, iden, wide, none, bad_, narr},
                    New ConversionClass(max) {bad_, narr, bad_, narr, none, wide, wide, wide, wide, wide, wide, wide, wide, narr, narr, iden, none, bad_, narr},
                    New ConversionClass(max) {bad_, narr, bad_, none, none, none, none, none, none, none, none, none, none, none, none, none, iden, bad_, narr},
                    New ConversionClass(max) {bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_, bad_},
                    New ConversionClass(max) {bad_, narr, bad_, narr, wide, narr, narr, narr, narr, narr, narr, narr, narr, narr, narr, narr, narr, bad_, iden}
                }

            'This table is the relative ordering of the specificity of types. It is used during
            'overload resolution to answer the question: 'Of numeric types a and b, which is more specific?'.
            '
            'The general rules encoded in this table are:
            '    Smaller types are more specific than larger types.
            '    Signed types are more specific than unsigned types of equal or greater widths,
            '    with the exception of Byte which is more specific than SByte (for backwards compatibility).

            NumericSpecificityRank = New Integer(max) {}
            NumericSpecificityRank(TypeCode.Byte) = 1
            NumericSpecificityRank(TypeCode.SByte) = 2
            NumericSpecificityRank(TypeCode.Int16) = 3
            NumericSpecificityRank(TypeCode.UInt16) = 4
            NumericSpecificityRank(TypeCode.Int32) = 5
            NumericSpecificityRank(TypeCode.UInt32) = 6
            NumericSpecificityRank(TypeCode.Int64) = 7
            NumericSpecificityRank(TypeCode.UInt64) = 8
            NumericSpecificityRank(TypeCode.Decimal) = 9
            NumericSpecificityRank(TypeCode.Single) = 10
            NumericSpecificityRank(TypeCode.Double) = 11

            ' This table specifies the "widest" type to be used in For Loops
            ' It should match the results of the Add Operator.

            ForLoopWidestTypeCode = New TypeCode(max)() _
                {
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Int16, TypeCode.Empty, TypeCode.SByte, TypeCode.Int16, TypeCode.Int16, TypeCode.Int32, TypeCode.Int32, TypeCode.Int64, TypeCode.Int64, TypeCode.Decimal, TypeCode.Single, TypeCode.Double, TypeCode.Decimal, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.SByte, TypeCode.Empty, TypeCode.SByte, TypeCode.Int16, TypeCode.Int16, TypeCode.Int32, TypeCode.Int32, TypeCode.Int64, TypeCode.Int64, TypeCode.Decimal, TypeCode.Single, TypeCode.Double, TypeCode.Decimal, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Int16, TypeCode.Empty, TypeCode.Int16, TypeCode.Byte, TypeCode.Int16, TypeCode.UInt16, TypeCode.Int32, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Int16, TypeCode.Empty, TypeCode.Int16, TypeCode.Int16, TypeCode.Int16, TypeCode.Int32, TypeCode.Int32, TypeCode.Int64, TypeCode.Int64, TypeCode.Decimal, TypeCode.Single, TypeCode.Double, TypeCode.Decimal, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Int32, TypeCode.Empty, TypeCode.Int32, TypeCode.UInt16, TypeCode.Int32, TypeCode.UInt16, TypeCode.Int32, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Int32, TypeCode.Empty, TypeCode.Int32, TypeCode.Int32, TypeCode.Int32, TypeCode.Int32, TypeCode.Int32, TypeCode.Int64, TypeCode.Int64, TypeCode.Decimal, TypeCode.Single, TypeCode.Double, TypeCode.Decimal, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Int64, TypeCode.Empty, TypeCode.Int64, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Int64, TypeCode.Empty, TypeCode.Int64, TypeCode.Int64, TypeCode.Int64, TypeCode.Int64, TypeCode.Int64, TypeCode.Int64, TypeCode.Int64, TypeCode.Decimal, TypeCode.Single, TypeCode.Double, TypeCode.Decimal, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Decimal, TypeCode.Empty, TypeCode.Decimal, TypeCode.UInt64, TypeCode.Decimal, TypeCode.UInt64, TypeCode.Decimal, TypeCode.UInt64, TypeCode.Decimal, TypeCode.UInt64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Single, TypeCode.Empty, TypeCode.Single, TypeCode.Single, TypeCode.Single, TypeCode.Single, TypeCode.Single, TypeCode.Single, TypeCode.Single, TypeCode.Single, TypeCode.Single, TypeCode.Double, TypeCode.Single, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Double, TypeCode.Empty, TypeCode.Double, TypeCode.Double, TypeCode.Double, TypeCode.Double, TypeCode.Double, TypeCode.Double, TypeCode.Double, TypeCode.Double, TypeCode.Double, TypeCode.Double, TypeCode.Double, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Decimal, TypeCode.Empty, TypeCode.Decimal, TypeCode.Decimal, TypeCode.Decimal, TypeCode.Decimal, TypeCode.Decimal, TypeCode.Decimal, TypeCode.Decimal, TypeCode.Decimal, TypeCode.Single, TypeCode.Double, TypeCode.Decimal, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty},
                    New TypeCode(max) {TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty, TypeCode.Empty}
                }

            VerifyTypeCodeEnum()

#If DEBUG Then
            VerifyForLoopWidestType()
#End If
        End Sub

#If DEBUG Then
        <System.Diagnostics.ConditionalAttribute("DEBUG")>
        Private Shared Sub VerifyForLoopWidestType()
            Const Max As Integer = TypeCode.String

            For Index1 As Integer = 0 To Max
                Dim tc1 As TypeCode = CType(Index1, TypeCode)

                If IsNumericType(tc1) Then
                    For Index2 As Integer = 0 To Max
                        Dim tc2 As TypeCode = CType(Index2, TypeCode)

                        If IsNumericType(tc2) Then
                            Dim tc As TypeCode = ForLoopWidestTypeCode(tc1)(tc2)

                            Dim Type1 As Type = MapTypeCodeToType(tc1)
                            Dim Type2 As Type = MapTypeCodeToType(tc2)

                            Dim o1 As Object = 0
                            Dim o2 As Object = 0

                            o1 = Convert.ChangeType(o1, Type1)
                            o2 = Convert.ChangeType(o2, Type2)

                            Dim Result As Object = Operators.AddObject(o1, o2)

                            Debug.Assert(GetTypeCode(Result.GetType()) = tc, "Widest type is invalid")
                        End If
                    Next
                End If
            Next
        End Sub
#End If

        <System.Diagnostics.ConditionalAttribute("DEBUG")>
        Private Shared Sub VerifyTypeCodeEnum()
            Debug.Assert(TypeCode.Empty = 0, "wrong value!")
            Debug.Assert(TypeCode.Object = 1, "wrong value!")
            Debug.Assert(TypeCode.Boolean = 3, "yte is wrong value!")
            Debug.Assert(TypeCode.Char = 4, "wrong value!")
            Debug.Assert(TypeCode.SByte = 5, "wrong value!")
            Debug.Assert(TypeCode.Byte = 6, "wrong value!")
            Debug.Assert(TypeCode.Int16 = 7, "wrong value!")
            Debug.Assert(TypeCode.UInt16 = 8, "wrong value!")
            Debug.Assert(TypeCode.Int32 = 9, "wrong value!")
            Debug.Assert(TypeCode.UInt32 = 10, "wrong value!")
            Debug.Assert(TypeCode.Int64 = 11, "wrong value!")
            Debug.Assert(TypeCode.UInt64 = 12, "wrong value!")
            Debug.Assert(TypeCode.Single = 13, "wrong value!")
            Debug.Assert(TypeCode.Double = 14, "wrong value!")
            Debug.Assert(TypeCode.Decimal = 15, "wrong value!")
            Debug.Assert(TypeCode.DateTime = 16, "wrong value!")
            Debug.Assert(TypeCode.String = 18, "wrong value!")
        End Sub

        Friend Shared Function ClassifyConversion(ByVal targetType As System.Type, ByVal sourceType As System.Type, ByRef operatorMethod As Method) As ConversionClass
            'This function classifies the nature of the conversion from the source type to the target
            'type. If such a conversion requires a user-defined conversion, it will be supplied as an
            'out parameter.

            Debug.Assert(Not targetType.IsByRef AndAlso Not sourceType.IsByRef, "ByRef types unexpected.")

            Dim result As ConversionClass = ClassifyPredefinedConversion(targetType, sourceType)

            If result = ConversionClass.None AndAlso
               Not IsInterface(sourceType) AndAlso
               Not IsInterface(targetType) AndAlso
               (IsClassOrValueType(sourceType) OrElse IsClassOrValueType(targetType)) AndAlso
               Not (IsIntrinsicType(sourceType) AndAlso IsIntrinsicType(targetType)) Then

                result = ClassifyUserDefinedConversion(targetType, sourceType, operatorMethod)
            End If

            Return result

        End Function

        Friend Shared Function ClassifyIntrinsicConversion(ByVal targetTypeCode As TypeCode, ByVal sourceTypeCode As TypeCode) As ConversionClass

            Debug.Assert(IsIntrinsicType(targetTypeCode) AndAlso IsIntrinsicType(sourceTypeCode), "expected intrinsics here")
            Return s_conversionTable(targetTypeCode)(sourceTypeCode)
        End Function

        Friend Shared Function ClassifyPredefinedCLRConversion(ByVal targetType As System.Type, ByVal sourceType As System.Type) As ConversionClass
            ' This function classifies all intrinsic CLR conversions, such as inheritance,
            ' implementation, and array covariance.

            Debug.Assert(Not targetType.IsByRef AndAlso Not sourceType.IsByRef, "ByRef types unexpected.")

            ' Could we use IsAssignableFrom to cut out a number of these checks (probably the widening ones)?

            ' *IDENTITY*
            If targetType Is sourceType Then Return ConversionClass.Identity

            ' *INHERITANCE*
            If IsRootObjectType(targetType) OrElse IsOrInheritsFrom(sourceType, targetType) Then
                Return ConversionClass.Widening
            End If

            If IsRootObjectType(sourceType) OrElse IsOrInheritsFrom(targetType, sourceType) Then
                Return ConversionClass.Narrowing
            End If

            ' *INTERFACE IMPLEMENTATION*
            If IsInterface(sourceType) Then

                If IsClass(targetType) OrElse IsArrayType(targetType) OrElse IsGenericParameter(targetType) Then
                    ' Even if a class is marked NotInheritable, it can still be a COM class and implement
                    ' any interface dynamically at runtime, so we must allow a narrowing conversion.

                    Return ConversionClass.Narrowing
                End If

                If IsInterface(targetType) Then
                    Return ConversionClass.Narrowing
                End If

                If IsValueType(targetType) Then
                    If [Implements](targetType, sourceType) Then
                        Return ConversionClass.Narrowing
                    Else
                        Return ConversionClass.None
                    End If
                End If
                Debug.Assert(False, "all conversions from interface should have been handled by now")
                Return ConversionClass.Narrowing
            End If

            If IsInterface(targetType) Then

                If (IsArrayType(sourceType)) Then
                    Return ClassifyCLRArrayToInterfaceConversion(targetType, sourceType)
                End If

                If IsValueType(sourceType) Then
                    If [Implements](sourceType, targetType) Then
                        Return ConversionClass.Widening
                    Else
                        Return ConversionClass.None
                    End If
                End If

                If IsClass(sourceType) Then
                    If [Implements](sourceType, targetType) Then
                        Return ConversionClass.Widening
                    Else
                        Return ConversionClass.Narrowing
                    End If
                End If

                'generic params are handled later
            End If

            ' *ENUMERATION*
            If IsEnum(sourceType) OrElse IsEnum(targetType) Then

                If GetTypeCode(sourceType) = GetTypeCode(targetType) Then
                    If IsEnum(targetType) Then
                        Return ConversionClass.Narrowing
                    Else
                        Return ConversionClass.Widening
                    End If
                End If

                Return ConversionClass.None
            End If

            ' *GENERIC PARAMETERS*
            If IsGenericParameter(sourceType) Then
                If Not IsClassOrInterface(targetType) Then
                    Return ConversionClass.None
                End If

                'Return the best conversion from any constraint type to the target type.
                For Each interfaceConstraint As Type In GetInterfaceConstraints(sourceType)
                    Dim classification As ConversionClass =
                        ClassifyPredefinedConversion(targetType, interfaceConstraint)

                    If classification = ConversionClass.Widening OrElse
                       classification = ConversionClass.Identity Then
                        'A conversion from a constraint type cannot be an identity conversion
                        '(because a conversion operation is necessary in the generated code),
                        'so don't allow it to look any better than Widening.
                        Return ConversionClass.Widening
                    End If
                Next

                Dim classConstraint As Type = GetClassConstraint(sourceType)
                If classConstraint IsNot Nothing Then
                    Dim classification As ConversionClass =
                        ClassifyPredefinedConversion(targetType, classConstraint)

                    If classification = ConversionClass.Widening OrElse
                       classification = ConversionClass.Identity Then
                        'A conversion from a constraint type cannot be an identity conversion
                        '(because a conversion operation is necessary in the generated code),
                        'so don't allow it to look any better than Widening.
                        Return ConversionClass.Widening
                    End If
                End If

                Return IIf(IsInterface(targetType), ConversionClass.Narrowing, ConversionClass.None)
            End If

            If IsGenericParameter(targetType) Then
                Debug.Assert(Not IsInterface(sourceType),
                             "conversions from interfaces should have been handled by now")

                'If one of the constraint types is a class type, a narrowing conversion exists from that class type.
                Dim classConstraint As Type = GetClassConstraint(targetType)
                If classConstraint IsNot Nothing AndAlso IsOrInheritsFrom(classConstraint, sourceType) Then
                    Return ConversionClass.Narrowing
                End If

                Return ConversionClass.None
            End If

            ' *ARRAY COVARIANCE*
            If IsArrayType(sourceType) AndAlso IsArrayType(targetType) Then
                If sourceType.GetArrayRank = targetType.GetArrayRank Then
                    ' The element types must either be the same or
                    ' the source element type must extend or implement the
                    ' target element type. (VB implements array covariance.)
                    Return ClassifyCLRConversionForArrayElementTypes(targetType.GetElementType, sourceType.GetElementType)
                End If

                Return ConversionClass.None
            End If

            Return ConversionClass.None

        End Function

        Private Shared Function ClassifyCLRArrayToInterfaceConversion(ByVal targetInterface As System.Type, ByVal sourceArrayType As System.Type) As ConversionClass

            Debug.Assert(IsInterface(targetInterface), "Non-Interface type unexpected!!!")
            Debug.Assert(IsArrayType(sourceArrayType), "Non-Array type unexpected!!!")

            ' No need to get to System.Array, [Implements] works for arrays with respect to the interfaces on System.Array
            '
            If ([Implements](sourceArrayType, targetInterface)) Then
                Return ConversionClass.Widening
            End If

            ' Multi-dimensional arrays do not support IList<T>
            '
            If (sourceArrayType.GetArrayRank > 1) Then
                Return ConversionClass.Narrowing
            End If


            ' Check for the conversion from the Array of element type T to
            ' 1. IList(Of T) - Widening
            ' 2. Some interface that IList(Of T) inherits from - Widening
            ' 3. IList(Of SomeType that T inherits from) - Widening
            '    yes, generics covariance is allowed in the array case
            ' 4. Some interface that IList(Of SomeType that T inherits from)
            '    inherits from - Widening
            ' 5. Some interface that inherits from IList(Of T) - Narrowing
            ' 6. Some interface that inherits from IList(Of SomeType that T inherits from)
            '    - Narrowing
            '
            ' 5 and 6 are not checked for explicitly since from array to interface that
            ' the array does not widen to, we anyway return narrowing.
            '

            Dim sourceElementType As Type = sourceArrayType.GetElementType
            Dim conversion As ConversionClass = ConversionClass.None

            If (targetInterface.IsGenericType AndAlso Not targetInterface.IsGenericTypeDefinition) Then

                Dim rawTargetInterface As Type = targetInterface.GetGenericTypeDefinition()

                If (rawTargetInterface Is GetType(System.Collections.Generic.IList(Of )) OrElse
                    rawTargetInterface Is GetType(System.Collections.Generic.ICollection(Of )) OrElse
                    rawTargetInterface Is GetType(System.Collections.Generic.IEnumerable(Of ))) Then

                    conversion =
                        ClassifyCLRConversionForArrayElementTypes(
                            targetInterface.GetGenericArguments()(0),
                            sourceElementType)
                End If

            Else
                conversion =
                    ClassifyPredefinedCLRConversion(
                        targetInterface,
                        GetType(System.Collections.Generic.IList(Of )).MakeGenericType(New Type() {sourceElementType}))
            End If


            If (conversion = ConversionClass.Identity OrElse
                conversion = ConversionClass.Widening) Then

                Return ConversionClass.Widening
            End If

            Return ConversionClass.Narrowing

        End Function


        Private Shared Function ClassifyCLRConversionForArrayElementTypes(ByVal targetElementType As System.Type, ByVal sourceElementType As System.Type) As ConversionClass

            ' The element types must either be the same or
            ' the source element type must extend or implement the
            ' target element type. (VB implements array covariance.)

            ' Generic params are handled correctly here.

            If IsReferenceType(sourceElementType) AndAlso
               IsReferenceType(targetElementType) Then
                Return ClassifyPredefinedCLRConversion(targetElementType, sourceElementType)
            End If

            If IsValueType(sourceElementType) AndAlso
               IsValueType(targetElementType) Then
                Return ClassifyPredefinedCLRConversion(targetElementType, sourceElementType)
            End If

            ' Bug VSWhidbey 369131.
            ' Array co-variance and back-casting special case for generic parameters.
            '
            If IsGenericParameter(sourceElementType) AndAlso
               IsGenericParameter(targetElementType) Then

                If sourceElementType Is targetElementType Then
                    Return ConversionClass.Identity
                End If

                If IsReferenceType(sourceElementType) AndAlso
                   IsOrInheritsFrom(sourceElementType, targetElementType) Then
                    Return ConversionClass.Widening
                End If

                If IsReferenceType(targetElementType) AndAlso
                   IsOrInheritsFrom(targetElementType, sourceElementType) Then
                    Return ConversionClass.Narrowing
                End If
            End If

            Return ConversionClass.None
        End Function


        Friend Shared Function ClassifyPredefinedConversion(ByVal targetType As System.Type, ByVal sourceType As System.Type) As ConversionClass
            ' This function classifies all intrinsic language conversions, such as inheritance,
            ' implementation, array covariance, and conversions between intrinsic types.

            Debug.Assert(Not targetType.IsByRef AndAlso Not sourceType.IsByRef, "ByRef types unexpected.")

            ' Make an easy reference comparison for a common case.  More complicated type comparisons will happen later.
            If targetType Is sourceType Then Return ConversionClass.Identity

            Dim sourceTypeCode As TypeCode = GetTypeCode(sourceType)
            Dim targetTypeCode As TypeCode = GetTypeCode(targetType)

            If (IsIntrinsicType(sourceTypeCode) AndAlso IsIntrinsicType(targetTypeCode)) Then

                If IsEnum(targetType) Then
                    If IsIntegralType(sourceTypeCode) AndAlso IsIntegralType(targetTypeCode) Then
                        ' Conversion from an integral type (including an Enum type)
                        ' to an Enum type (that has an integral underlying type)
                        ' is narrowing. Enums do not necessarily have integral underlying types.
                        Return ConversionClass.Narrowing
                    End If
                End If

                If sourceTypeCode = targetTypeCode AndAlso IsEnum(sourceType) Then
                    ' Conversion from an Enum to it's underlying type is widening.
                    ' If we used ClassifyIntrinsicConversion, this kind of conversion
                    ' would be classified as identity, and that would not be good.
                    ' Catch this case here.
                    Return ConversionClass.Widening
                End If

                Return ClassifyIntrinsicConversion(targetTypeCode, sourceTypeCode)

            End If

            ' Try VB specific conversions from String-->Char() or Char()-->String.

            If IsCharArrayRankOne(sourceType) AndAlso IsStringType(targetType) Then
                ' Array of Char widens to String.
                Return ConversionClass.Widening
            End If

            If IsCharArrayRankOne(targetType) AndAlso IsStringType(sourceType) Then
                ' String narrows to array of Char.
                Return ConversionClass.Narrowing
            End If

            Return ClassifyPredefinedCLRConversion(targetType, sourceType)

        End Function

        Private Shared Function CollectConversionOperators(
            ByVal targetType As System.Type,
            ByVal sourceType As System.Type,
            ByRef foundTargetTypeOperators As Boolean,
            ByRef foundSourceTypeOperators As Boolean) As List(Of Method)

            'Find all Widening and Narrowing conversion operators. Combine the lists
            'with the Widening operators grouped at the front.

            'From the perspective of VB, intrinsic types have no conversion operators.
            'Substitute in Object for these types.
            If IsIntrinsicType(targetType) Then targetType = GetType(Object)
            If IsIntrinsicType(sourceType) Then sourceType = GetType(Object)

            Dim result As List(Of Method) =
                Operators.CollectOperators(
                    UserDefinedOperator.Widen,
                    targetType,
                    sourceType,
                    foundTargetTypeOperators,
                    foundSourceTypeOperators)

            Dim narrowingOperators As List(Of Method) =
                Operators.CollectOperators(
                    UserDefinedOperator.Narrow,
                    targetType,
                    sourceType,
                    foundTargetTypeOperators,
                    foundSourceTypeOperators)

            result.AddRange(narrowingOperators)
            Return result
        End Function

        Private Shared Function Encompasses(ByVal larger As System.Type, ByVal smaller As System.Type) As Boolean
            'Definition: LARGER is said to encompass SMALLER if SMALLER widens to or is LARGER.

            'Since determining encompasses is quite commonly used,
            'and only depends on widening or identity, a special function for classifying
            'just predefined widening conversions could be a performance gain.
            Dim result As ConversionClass = ClassifyPredefinedConversion(larger, smaller)

            Return result = ConversionClass.Widening OrElse result = ConversionClass.Identity
        End Function

        Private Shared Function NotEncompasses(ByVal larger As System.Type, ByVal smaller As System.Type) As Boolean
            'Definition: LARGER is said to not encompass SMALLER if SMALLER narrows to or is LARGER.

            'Since determining encompasses is quite commonly used,
            'and only depends on widening or identity, a special function for classifying
            'just predefined narrowing conversions could be a performance gain.
            Dim result As ConversionClass = ClassifyPredefinedConversion(larger, smaller)

            Return result = ConversionClass.Narrowing OrElse result = ConversionClass.Identity
        End Function


        Private Shared Function MostEncompassing(ByVal types As List(Of System.Type)) As System.Type
            'Given a set TYPES, determine the most encompassing type. An element
            'CANDIDATE of TYPES is said to be most encompassing if no other element of
            'TYPES encompasses CANDIDATE.

            Debug.Assert(types.Count > 0, "unexpected empty set")
            Dim maxEncompassing As System.Type = types.Item(0)

            For index As Integer = 1 To types.Count - 1
                Dim candidate As Type = types.Item(index)

                If Encompasses(candidate, maxEncompassing) Then
                    Debug.Assert(candidate Is maxEncompassing OrElse Not Encompasses(maxEncompassing, candidate),
                                 "surprisingly, two types encompass each other")
                    maxEncompassing = candidate
                ElseIf Not Encompasses(maxEncompassing, candidate) Then
                    'We have detected more than one most encompassing type in the set.
                    'Return Nothing to indicate this error condition.
                    Return Nothing
                End If
            Next

            Return maxEncompassing
        End Function


        Private Shared Function MostEncompassed(ByVal types As List(Of System.Type)) As System.Type
            'Given a set TYPES, determine the most encompassed type. An element
            'CANDIDATE of TYPES is said to be most encompassed if CANDIDATE encompasses
            'no other element of TYPES.

            Debug.Assert(types.Count > 0, "unexpected empty set")

            Dim maxEncompassed As System.Type = types.Item(0)

            For index As Integer = 1 To types.Count - 1
                Dim candidate As Type = types.Item(index)

                If Encompasses(maxEncompassed, candidate) Then
                    Debug.Assert(candidate Is maxEncompassed OrElse Not Encompasses(candidate, maxEncompassed),
                                 "surprisingly, two types encompass each other")
                    maxEncompassed = candidate
                ElseIf Not Encompasses(candidate, maxEncompassed) Then
                    'We have detected more than one most encompassed type in the set.
                    'Return Nothing to indicate this error condition.
                    Return Nothing
                End If
            Next

            Return maxEncompassed
        End Function

        Private Shared Sub FindBestMatch(
            ByVal targetType As Type,
            ByVal sourceType As Type,
            ByVal searchList As List(Of Method),
            ByVal resultList As List(Of Method),
            ByRef genericMembersExistInList As Boolean)

            'Given a set of conversion operators which convert from INPUT to RESULT, return the set
            'of operators for which INPUT is SOURCE and RESULT is TARGET.

            For Each item As Method In searchList
                Dim current As MethodBase = item.AsMethod
                Dim inputType As System.Type = current.GetParameters(0).ParameterType
                Dim resultType As System.Type = DirectCast(current, MethodInfo).ReturnType

                If inputType Is sourceType AndAlso resultType Is targetType Then
                    InsertInOperatorListIfLessGenericThanExisting(item, resultList, genericMembersExistInList)
                End If
            Next
            Return

        End Sub

        Private Shared Sub InsertInOperatorListIfLessGenericThanExisting(
            ByVal operatorToInsert As Method,
            ByVal operatorList As List(Of Method),
            ByRef genericMembersExistInList As Boolean)

            If IsGeneric(operatorToInsert.DeclaringType) Then
                genericMembersExistInList = True
            End If

            If genericMembersExistInList Then

                For i As Integer = operatorList.Count - 1 To 0 Step -1

                    Dim existing As Method = operatorList.Item(i)
                    Dim leastGeneric As Method = OverloadResolution.LeastGenericProcedure(existing, operatorToInsert)

                    If leastGeneric Is existing Then
                        ' An existing one is less generic than the current operator being
                        ' considered, so skip adding the current operator to the operator
                        ' list.
                        Return

                    ElseIf leastGeneric IsNot Nothing Then
                        ' The current operator is less generic than an existing operator,
                        ' so remove the existing operator from the list and continue to
                        ' check if any other existing operator can be removed from the
                        ' result set.
                        '
                        operatorList.Remove(existing)
                    End If
                Next
            End If

            operatorList.Add(operatorToInsert)
        End Sub

        Private Shared Function ResolveConversion(
            ByVal targetType As System.Type,
            ByVal sourceType As System.Type,
            ByVal operatorSet As List(Of Method),
            ByVal wideningOnly As Boolean,
            ByRef resolutionIsAmbiguous As Boolean) As List(Of Method)


            'This function resolves which user-defined conversion operator contained in the input set
            'can be used to perform the conversion from source type S to target type T.
            '
            'The algorithm defies succinct explanation, but roughly:
            '
            'Conversions of the form S-->T use only one user-defined conversion at a time, i.e.,
            'user-defined conversions are not chained together.  It may be necessary to convert to and
            'from intermediate types using predefined conversions to match the signature of the
            'user-defined conversion exactly, so the conversion "path" is comprised of at most three
            'parts:
            '
            '    1) [ predefined conversion  S-->Sx ]
            '    2) User-defined conversion Sx-->Tx
            '    3) [ predefined conversion Tx-->T  ]
            '
            '    Where Sx is the intermediate source type
            '      and Tx is the intermediate target type
            '
            '    Steps 1 and 3 are optional given S == Sx or Tx == T.
            '
            'Much of the algorithm below concerns itself with finding Sx and Tx.  The rules are:
            '
            '    - If a conversion operator in the set converts from S, then Sx is S.
            '    - If a conversion operator in the set converts to T, then Tx is T.
            '    - Otherwise Sx and Tx are the "closest" types to S and T.  If multiple types are
            '      equally close, the conversion is ambiguous.
            '
            'Each operator presents a possibility for Sx (the parameter type of the operator).  Given
            'these choices, the "closest" type to S is the smallest (most encompassed) type that S
            'widens to.  If S widens to none of the possible types, then the "closest" type to S is
            'the largest (most encompassing) type that widens to S.  In this way, the algorithm
            'always prefers widening from S over narrowing from S.
            '
            'Similarly, each operator presents a possibility for Tx (the return type of the operator).
            'Given these choices, the "closest" type to T is the largest (most encompassing) type that
            'widens to T.  If none of the possible types widen to T, then the "closest" type to T is
            'the smallest (most encompassed) type that T widens to.  In this way, the algorithm
            'always prefers widening to T over narrowing to T.
            '
            'Upon deciding Sx and Tx, if one operator's operands exactly match Sx and Tx, then that
            'operator is chosen.  If no operators match, or if multiple operators match, the conversion
            'is impossible.
            '
            'Refer to the language specification as it covers all details of the algorithm.

            resolutionIsAmbiguous = False

            Dim mostSpecificSourceType As System.Type = Nothing
            Dim mostSpecificTargetType As System.Type = Nothing

            Dim genericOperatorChoicesFound As Boolean = False
            Dim operatorChoices As List(Of Method) = New List(Of Method)(operatorSet.Count)
            Dim candidates As List(Of Method) = New List(Of Method)(operatorSet.Count)

            Dim sourceBases As List(Of Type) = New List(Of Type)(operatorSet.Count)
            Dim targetDeriveds As List(Of Type) = New List(Of Type)(operatorSet.Count)
            Dim sourceDeriveds As List(Of Type) = Nothing
            Dim targetBases As List(Of Type) = Nothing

            If Not wideningOnly Then
                sourceDeriveds = New List(Of Type)(operatorSet.Count)
                targetBases = New List(Of Type)(operatorSet.Count)
            End If

            'To minimize the number of calls to Encompasses, we categorize conversions
            'into three flavors:
            '
            '   1) Base of Source to Derived of Target (only flavor that can be completely widening)
            '   2) Base of Source to Base of Target
            '   3) Derived of Source to Base of Target
            '
            'For each flavor, we place the input and result type into the corresponding
            'type set. Then we calculate most encompassing/encompassed using the type sets.

            For Each currentMethod As Method In operatorSet

                Dim current As MethodBase = currentMethod.AsMethod

                'Performance trick: the operators are grouped by widening and then narrowing
                'conversions. If we are iterating over just widening conversions, we are done
                'once we find a narrowing conversion.
                If wideningOnly AndAlso IsNarrowingConversionOperator(current) Then Exit For

                Dim inputType As System.Type = current.GetParameters(0).ParameterType
                Dim resultType As System.Type = DirectCast(current, MethodInfo).ReturnType

                If (IsGeneric(current) OrElse
                       IsGeneric(current.DeclaringType)) AndAlso
                   ClassifyPredefinedConversion(resultType, inputType) <> ConversionClass.None Then
                    Continue For
                End If

                If inputType Is sourceType AndAlso resultType Is targetType Then
                    InsertInOperatorListIfLessGenericThanExisting(currentMethod, operatorChoices, genericOperatorChoicesFound)

                ElseIf operatorChoices.Count = 0 Then

                    If Encompasses(inputType, sourceType) AndAlso Encompasses(targetType, resultType) Then
                        'Check SourceBase->TargetDerived flavor.

                        candidates.Add(currentMethod)
                        If inputType Is sourceType Then mostSpecificSourceType = inputType Else sourceBases.Add(inputType)
                        If resultType Is targetType Then mostSpecificTargetType = resultType Else targetDeriveds.Add(resultType)

                    ElseIf Not wideningOnly AndAlso
                           Encompasses(inputType, sourceType) AndAlso NotEncompasses(targetType, resultType) Then
                        'Check SourceBase->TargetBase flavor.

                        candidates.Add(currentMethod)
                        If inputType Is sourceType Then mostSpecificSourceType = inputType Else sourceBases.Add(inputType)
                        If resultType Is targetType Then mostSpecificTargetType = resultType Else targetBases.Add(resultType)

                    ElseIf Not wideningOnly AndAlso
                           NotEncompasses(inputType, sourceType) AndAlso NotEncompasses(targetType, resultType) Then
                        'Check SourceDerived->TargetBase flavor.

                        candidates.Add(currentMethod)
                        If inputType Is sourceType Then mostSpecificSourceType = inputType Else sourceDeriveds.Add(inputType)
                        If resultType Is targetType Then mostSpecificTargetType = resultType Else targetBases.Add(resultType)

                    End If

                End If
            Next

            'Now attempt to find the most specific types Sx and Tx by analyzing the type sets
            'we built up in the code above.

            If operatorChoices.Count = 0 AndAlso candidates.Count > 0 Then

                If mostSpecificSourceType Is Nothing Then
                    If sourceBases.Count > 0 Then
                        mostSpecificSourceType = MostEncompassed(sourceBases)
                    Else
                        Debug.Assert(Not wideningOnly AndAlso sourceDeriveds.Count > 0, "unexpected state")
                        mostSpecificSourceType = MostEncompassing(sourceDeriveds)
                    End If
                End If

                If mostSpecificTargetType Is Nothing Then
                    If targetDeriveds.Count > 0 Then
                        mostSpecificTargetType = MostEncompassing(targetDeriveds)
                    Else
                        Debug.Assert(Not wideningOnly AndAlso targetBases.Count > 0, "unexpected state")
                        mostSpecificTargetType = MostEncompassed(targetBases)
                    End If
                End If

                If mostSpecificSourceType Is Nothing OrElse mostSpecificTargetType Is Nothing Then
                    resolutionIsAmbiguous = True
                    Return New List(Of Method)
                End If

                FindBestMatch(mostSpecificTargetType, mostSpecificSourceType, candidates, operatorChoices, genericOperatorChoicesFound)

            End If

            If operatorChoices.Count > 1 Then
                resolutionIsAmbiguous = True
            End If

            Return operatorChoices

        End Function

        Friend Shared Function ClassifyUserDefinedConversion(
            ByVal targetType As System.Type,
            ByVal sourceType As System.Type,
            ByRef operatorMethod As Method) As ConversionClass

            Dim result As ConversionClass

            'Check if we have done this classification before.
            SyncLock (ConversionCache)
                'First check if both types have no user-defined conversion operators. If so, they cannot
                'convert to each other with user-defined operators.
                If UnconvertibleTypeCache.Lookup(targetType) AndAlso UnconvertibleTypeCache.Lookup(sourceType) Then
                    Return ConversionClass.None
                End If

                'Now check if we have recently resolved this conversion.
                If ConversionCache.Lookup(targetType, sourceType, result, operatorMethod) Then
                    Return result
                End If
            End SyncLock

            'Perform the expensive work to resolve the user-defined conversion.
            Dim foundTargetTypeOperators As Boolean = False
            Dim foundSourceTypeOperators As Boolean = False
            result =
                DoClassifyUserDefinedConversion(
                    targetType,
                    sourceType,
                    operatorMethod,
                    foundTargetTypeOperators,
                    foundSourceTypeOperators)

            'Save away the results.
            SyncLock (ConversionCache)
                'Remember which types have no operators so we can avoid re-doing the work next time.
                If Not foundTargetTypeOperators Then
                    UnconvertibleTypeCache.Insert(targetType)
                End If

                If Not foundSourceTypeOperators Then
                    UnconvertibleTypeCache.Insert(sourceType)
                End If

                If foundTargetTypeOperators OrElse foundSourceTypeOperators Then
                    'Cache the result of the resolution so we can avoid re-doing the work next time, but
                    'only when conversion operators were found (otherwise, the type caches will catch this
                    'the next time).
                    ConversionCache.Insert(targetType, sourceType, result, operatorMethod)
                End If
            End SyncLock

            Return result
        End Function

        Private Shared Function DoClassifyUserDefinedConversion(
            ByVal targetType As System.Type,
            ByVal sourceType As System.Type,
            ByRef operatorMethod As Method,
            ByRef foundTargetTypeOperators As Boolean,
            ByRef foundSourceTypeOperators As Boolean) As ConversionClass

            'Classifies the conversion from Source to Target using user-defined conversion operators.
            'If such a conversion exists, it will be supplied as an out parameter.
            '
            'The result is a widening conversion from Source to Target if such a conversion exists.
            'Otherwise the result is a narrowing conversion if such a conversion exists.  Otherwise
            'no conversion is possible.  We perform this two pass process because the conversion
            '"path" is not affected by the user implicitly or explicitly specifying the conversion.
            '
            'In other words, a safe (widening) conversion is always taken regardless of whether
            'Option Strict is on or off.

            Debug.Assert(ClassifyPredefinedConversion(targetType, sourceType) = ConversionClass.None,
                         "predefined conversion is possible, so why try user-defined?")

            operatorMethod = Nothing

            Dim operatorSet As List(Of Method) =
                CollectConversionOperators(
                    targetType,
                    sourceType,
                    foundTargetTypeOperators,
                    foundSourceTypeOperators)

            If operatorSet.Count = 0 Then
                'No conversion operators, so no conversion is possible.
                Return ConversionClass.None
            End If

            Dim resolutionIsAmbiguous As Boolean = False

            Dim operatorChoices As List(Of Method) =
                ResolveConversion(
                    targetType,
                    sourceType,
                    operatorSet,
                    True,
                    resolutionIsAmbiguous)

            If operatorChoices.Count = 1 Then
                operatorMethod = operatorChoices.Item(0)
                operatorMethod.ArgumentsValidated = True
                'The result from the first pass is necessarily widening.
                Return ConversionClass.Widening

            ElseIf operatorChoices.Count = 0 AndAlso Not resolutionIsAmbiguous Then

                Debug.Assert(operatorSet.Count > 0, "expected operators")

                'Second pass: if the first pass failed, attempt to find a conversion
                'considering BOTH widening and narrowing.

                operatorChoices =
                    ResolveConversion(
                        targetType,
                        sourceType,
                        operatorSet,
                        False,
                        resolutionIsAmbiguous)

                If operatorChoices.Count = 1 Then
                    operatorMethod = operatorChoices.Item(0)
                    operatorMethod.ArgumentsValidated = True
                    'The result from the second pass is necessarily narrowing.
                    Return ConversionClass.Narrowing

                ElseIf operatorChoices.Count = 0 Then
                    'No conversion possible.
                    Return ConversionClass.None

                End If

            End If

            ' If error reporting is improved for conversion resolution,
            ' create a useful error message here.

            ' Conversion is ambiguous.
            Return ConversionClass.Ambiguous

        End Function

        Friend Class OperatorCaches
            ' Prevent creation.
            Private Sub New()
            End Sub

            Friend NotInheritable Class FixedList

                Private Structure Entry
                    Friend TargetType As Type
                    Friend SourceType As Type
                    Friend Classification As ConversionClass
                    Friend OperatorMethod As Method
                    Friend [Next] As Integer
                    Friend Previous As Integer
                End Structure

                Private ReadOnly _list As Entry()
                Private ReadOnly _size As Integer
                Private _first As Integer
                Private _last As Integer
                Private _count As Integer

                Private Const s_defaultSize As Integer = 50

                Friend Sub New()
                    MyClass.New(s_defaultSize)
                End Sub

                Friend Sub New(ByVal size As Integer)
                    'Populate the cache list with the maximum number of entires.
                    'This simplifies the insertion code for a small upfront cost.
                    _size = size

                    _list = New Entry(_size - 1) {}
                    For index As Integer = 0 To _size - 2
                        _list(index).Next = index + 1
                    Next
                    For index As Integer = _size - 1 To 1 Step -1
                        _list(index).Previous = index - 1
                    Next
                    _list(0).Previous = _size - 1
                    _last = _size - 1
                End Sub

                Private Sub MoveToFront(ByVal item As Integer)
                    'Remove Item from its position in the list and move it to the front.
                    If item = _first Then Return

                    Dim [next] As Integer = _list(item).Next
                    Dim previous As Integer = _list(item).Previous

                    _list(previous).Next = [next]
                    _list([next]).Previous = previous

                    _list(_first).Previous = item
                    _list(_last).Next = item

                    _list(item).Next = _first
                    _list(item).Previous = _last

                    _first = item
                End Sub

                Friend Sub Insert(
                ByVal targetType As Type,
                ByVal sourceType As Type,
                ByVal classification As ConversionClass,
                ByVal operatorMethod As Method)

                    If _count < _size Then _count += 1

                    'Replace the least used conversion in the list with a new conversion, and move
                    'that entry to the front.

                    Dim item As Integer = _last
                    _first = item
                    _last = _list(_last).Previous

                    _list(item).TargetType = targetType
                    _list(item).SourceType = sourceType
                    _list(item).Classification = classification
                    _list(item).OperatorMethod = operatorMethod
                End Sub

                Friend Function Lookup(
                ByVal targetType As Type,
                ByVal sourceType As Type,
                ByRef classification As ConversionClass,
                ByRef operatorMethod As Method) As Boolean

                    Dim item As Integer = _first
                    Dim iteration As Integer = 0

                    Do While iteration < _count
                        If targetType Is _list(item).TargetType AndAlso sourceType Is _list(item).SourceType Then
                            classification = _list(item).Classification
                            operatorMethod = _list(item).OperatorMethod
                            MoveToFront(item)
                            Return True
                        End If
                        item = _list(item).Next
                        iteration += 1
                    Loop

                    classification = ConversionClass.Bad
                    operatorMethod = Nothing
                    Return False
                End Function

            End Class

            Friend NotInheritable Class FixedExistanceList

                Private Structure Entry
                    Friend Type As Type
                    Friend [Next] As Integer
                    Friend Previous As Integer
                End Structure

                Private ReadOnly _list As Entry()
                Private ReadOnly _size As Integer
                Private _first As Integer
                Private _last As Integer
                Private _count As Integer

                Private Const s_defaultSize As Integer = 50

                Friend Sub New()
                    MyClass.New(s_defaultSize)
                End Sub

                Friend Sub New(ByVal size As Integer)
                    'Populate the list with the maximum number of entires.
                    'This simplifies the insertion code for a small upfront cost.
                    _size = size

                    _list = New Entry(_size - 1) {}
                    For index As Integer = 0 To _size - 2
                        _list(index).Next = index + 1
                    Next
                    For index As Integer = _size - 1 To 1 Step -1
                        _list(index).Previous = index - 1
                    Next
                    _list(0).Previous = _size - 1
                    _last = _size - 1
                End Sub

                Private Sub MoveToFront(ByVal item As Integer)
                    'Remove Item from its position in the list and move it to the front.
                    If item = _first Then Return

                    Dim [next] As Integer = _list(item).Next
                    Dim previous As Integer = _list(item).Previous

                    _list(previous).Next = [next]
                    _list([next]).Previous = previous

                    _list(_first).Previous = item
                    _list(_last).Next = item

                    _list(item).Next = _first
                    _list(item).Previous = _last

                    _first = item
                End Sub

                Friend Sub Insert(ByVal type As Type)

                    If _count < _size Then _count += 1

                    'Replace the least used conversion in the cache with a new conversion, and move
                    'that entry to the front.

                    Dim item As Integer = _last
                    _first = item
                    _last = _list(_last).Previous

                    _list(item).Type = type
                End Sub

                Friend Function Lookup(ByVal type As Type) As Boolean

                    Dim item As Integer = _first
                    Dim iteration As Integer = 0

                    Do While iteration < _count
                        If type Is _list(item).Type Then
                            MoveToFront(item)
                            Return True
                        End If
                        item = _list(item).Next
                        iteration += 1
                    Loop

                    Return False
                End Function

            End Class

            Friend Shared ReadOnly ConversionCache As FixedList
            Friend Shared ReadOnly UnconvertibleTypeCache As FixedExistanceList

            Shared Sub New()
                ConversionCache = New FixedList
                UnconvertibleTypeCache = New FixedExistanceList
            End Sub

        End Class
    End Class

End Namespace
