// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Threading;
using Xunit;

public class Directory_GetSetTimes
{
    private enum TimeProperty
    {
        CreationTime,
        LastAccessTime,
        LastWriteTime
    }

    [Fact]
    public static void ConsistencyTest()
    {
        String dirName = Path.Combine(TestInfo.CurrentDirectory, "Directory_GetSetTimes");

        Directory.CreateDirectory(dirName);

        foreach (TimeProperty timeProperty in Enum.GetValues(typeof(TimeProperty)))
        {
            foreach (DateTimeKind kind in Enum.GetValues(typeof(DateTimeKind)))
            {
                DateTime dt = new DateTime(2014, 12, 1, 12, 0, 0, kind);
                foreach (bool setUtc in new[] { false, true })
                {
                    if (setUtc)
                    {
                        switch (timeProperty)
                        {
                            case TimeProperty.CreationTime:
                                Directory.SetCreationTimeUtc(dirName, dt);
                                break;
                            case TimeProperty.LastAccessTime:
                                Directory.SetLastAccessTimeUtc(dirName, dt);
                                break;
                            case TimeProperty.LastWriteTime:
                                Directory.SetLastWriteTimeUtc(dirName, dt);
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        switch (timeProperty)
                        {
                            case TimeProperty.CreationTime:
                                Directory.SetCreationTime(dirName, dt);
                                break;
                            case TimeProperty.LastAccessTime:
                                Directory.SetLastAccessTime(dirName, dt);
                                break;
                            case TimeProperty.LastWriteTime:
                                Directory.SetLastWriteTime(dirName, dt);
                                break;
                            default:
                                break;
                        }
                    }

                    DateTime actual, actualUtc;
                    switch (timeProperty)
                    {
                        case TimeProperty.CreationTime:
                            actual = Directory.GetCreationTime(dirName);
                            actualUtc = Directory.GetCreationTimeUtc(dirName);
                            break;
                        case TimeProperty.LastAccessTime:
                            actual = Directory.GetLastAccessTime(dirName);
                            actualUtc = Directory.GetLastAccessTimeUtc(dirName);
                            break;
                        case TimeProperty.LastWriteTime:
                            actual = Directory.GetLastWriteTime(dirName);
                            actualUtc = Directory.GetLastWriteTimeUtc(dirName);
                            break;
                        default:
                            throw new ArgumentException("Invalid time property type");
                    }

                    DateTime expected = dt.ToLocalTime();
                    DateTime expectedUtc = dt.ToUniversalTime();

                    if (dt.Kind == DateTimeKind.Unspecified)
                    {
                        if (setUtc)
                        {
                            expectedUtc = dt;
                        }
                        else
                        {
                            expected = dt;
                        }
                    }

                    Assert.Equal(expected, actual); //, "Local {0} should be correct for DateTimeKind.{1} when set with Set{0}{2}", timeProperty, kind, setUtc ? "Utc" : "");
                    Assert.Equal(expectedUtc, actualUtc); //, "Universal {0} should be correct for DateTimeKind.{1} when set with Set{0}{2}", timeProperty, kind, setUtc ? "Utc" : "");
                }
            }
        }

        Directory.Delete(dirName);
    }
}