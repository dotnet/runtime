// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class StandardCommandsTests
    {
        public static IEnumerable<object[]> StandardCommands_TestData()
        {
            yield return new object[] { StandardCommands.AlignBottom, 1, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.AlignHorizontalCenters, 2, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.AlignLeft, 3, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.AlignRight, 4, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.AlignToGrid, 5, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.AlignTop, 6, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.AlignVerticalCenters, 7, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.ArrangeBottom, 8, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.ArrangeIcons, 0x300A, new Guid("74d21313-2aee-11d1-8bfb-00a0c90f26f7") };
            yield return new object[] { StandardCommands.ArrangeRight, 9, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };

            yield return new object[] { StandardCommands.BringForward, 10, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.BringToFront, 11, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };

            yield return new object[] { StandardCommands.CenterHorizontally, 12, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.CenterVertically, 13, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.Copy, 15, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.Cut, 16, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };

            yield return new object[] { StandardCommands.Delete, 17, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.DocumentOutline, 239, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };

            yield return new object[] { StandardCommands.F1Help, 377, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };

            yield return new object[] { StandardCommands.Group, 20, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };

            yield return new object[] { StandardCommands.HorizSpaceConcatenate, 21, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.HorizSpaceDecrease, 22, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.HorizSpaceIncrease, 23, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.HorizSpaceMakeEqual, 24, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };

            yield return new object[] { StandardCommands.LineupIcons, 0x300B, new Guid("74d21313-2aee-11d1-8bfb-00a0c90f26f7") };
            yield return new object[] { StandardCommands.LockControls, 369, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };

            yield return new object[] { StandardCommands.MultiLevelRedo, 30, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.MultiLevelUndo, 44, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };

            yield return new object[] { StandardCommands.Paste, 26, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.Properties, 28, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.PropertiesWindow, 235, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };

            yield return new object[] { StandardCommands.Redo, 29, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.Replace, 230, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };

            yield return new object[] { StandardCommands.SelectAll, 31, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.SendBackward, 32, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.SendToBack, 33, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.ShowGrid, 103, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.ShowLargeIcons, 0x300C, new Guid("74d21313-2aee-11d1-8bfb-00a0c90f26f7") };
            yield return new object[] { StandardCommands.SizeToControl, 35, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.SizeToControlHeight, 36, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.SizeToControlWidth, 37, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.SizeToFit, 38, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.SizeToGrid, 39, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.SnapToGrid, 40, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };

            yield return new object[] { StandardCommands.TabOrder, 41, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };

            yield return new object[] { StandardCommands.Undo, 43, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.Ungroup, 45, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };

            yield return new object[] { StandardCommands.VerbFirst, 0x2000, new Guid("74D21313-2AEE-11d1-8BFB-00A0C90F26F7") };
            yield return new object[] { StandardCommands.VerbLast, 0x2100, new Guid("74D21313-2AEE-11d1-8BFB-00A0C90F26F7") };
            yield return new object[] { StandardCommands.VertSpaceConcatenate, 46, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.VertSpaceDecrease, 47, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.VertSpaceIncrease, 48, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.VertSpaceMakeEqual, 49, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.ViewCode, 333, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
            yield return new object[] { StandardCommands.ViewGrid, 125, new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819") };
        }

        [Theory]
        [MemberData(nameof(StandardCommands_TestData))]
        public void StandardCommands_Get_ReturnsExpected(CommandID command, int expectedId, Guid expectedGuid)
        {
            Assert.Equal(expectedId, command.ID);
            Assert.Equal(expectedGuid, command.Guid);
        }
    }
}
