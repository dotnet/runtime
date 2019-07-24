// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Runtime.Serialization
{
    /// <summary>
    /// Provides access to portions of the Serialization Guard APIs since they're not publicly exposed via contracts.
    /// </summary>
    internal static partial class SerializationGuard
    {
        private delegate void ThrowIfDeserializationInProgressWithSwitchDel(string switchName, ref int cachedValue);
        private static readonly ThrowIfDeserializationInProgressWithSwitchDel s_throwIfDeserializationInProgressWithSwitch = CreateThrowIfDeserializationInProgressWithSwitchDelegate();

        /// <summary>
        /// Builds a wrapper delegate for SerializationInfo.ThrowIfDeserializationInProgress(string, ref int),
        /// since it is not exposed via contracts.
        /// </summary>
        private static ThrowIfDeserializationInProgressWithSwitchDel CreateThrowIfDeserializationInProgressWithSwitchDelegate()
        {
            ThrowIfDeserializationInProgressWithSwitchDel throwIfDeserializationInProgressDelegate = null;
            MethodInfo throwMethod = typeof(SerializationInfo).GetMethod("ThrowIfDeserializationInProgress",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[] { typeof(string), typeof(int).MakeByRefType() }, Array.Empty<ParameterModifier>());

            if (throwMethod != null)
            {
                throwIfDeserializationInProgressDelegate = (ThrowIfDeserializationInProgressWithSwitchDel)throwMethod.CreateDelegate(typeof(ThrowIfDeserializationInProgressWithSwitchDel));
            }

            return throwIfDeserializationInProgressDelegate;
        }

        /// <summary>
        /// Provides access to the internal "ThrowIfDeserializationInProgress" method on <see cref="SerializationInfo"/>.
        /// No-ops if the Serialization Guard feature is disabled or unavailable.
        /// </summary>
        public static void ThrowIfDeserializationInProgress(string switchSuffix, ref int cachedValue)
        {
            s_throwIfDeserializationInProgressWithSwitch?.Invoke(switchSuffix, ref cachedValue);
        }
    }
}
