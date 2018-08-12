' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.VisualBasic
Imports System
Imports Xunit

Namespace Microsoft.VisualBasic.Tests

    Public Class Dummy

        <Fact>
        Public Sub Dummy()

            Dim dateTimeNowBefore As DateTime = DateTime.Now()
            Dim now As DateTime = DateAndTime.Now()
            Dim dateTimeNowAfter As DateTime = DateTime.Now()

            Assert.InRange(now, dateTimeNowBefore, dateTimeNowAfter)

        End Sub

    End Class

End Namespace
