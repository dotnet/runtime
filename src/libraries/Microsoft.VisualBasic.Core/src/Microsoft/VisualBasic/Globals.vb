' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System

Namespace Global.Microsoft.VisualBasic

    Public Enum CompareMethod
        [Binary] = 0
        [Text] = 1
    End Enum

    Public Enum DateFormat
        GeneralDate = 0
        LongDate = 1
        ShortDate = 2
        LongTime = 3
        ShortTime = 4
    End Enum

    <Flags()> Public Enum VbStrConv
        [None] = 0
        [Uppercase] = 1
        [Lowercase] = 2
        [ProperCase] = 3
        [Wide] = 4
        [Narrow] = 8
        [Katakana] = 16
        [Hiragana] = 32
        '[Unicode]      = 64 'OBSOLETE
        '[FromUnicode]   = 128 'OBSOLETE
        [SimplifiedChinese] = 256
        [TraditionalChinese] = 512
        [LinguisticCasing] = 1024
    End Enum

    Public Enum TriState
        [False] = 0
        [True] = -1
        [UseDefault] = -2
    End Enum

End Namespace
