// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal readonly record struct GcScanSlotLocation(int Reg, int RegOffset, bool TargetPtr);
