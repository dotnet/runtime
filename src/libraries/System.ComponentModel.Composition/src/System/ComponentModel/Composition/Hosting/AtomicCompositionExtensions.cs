// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace System.ComponentModel.Composition.Hosting
{
    internal static class AtomicCompositionExtensions
    {
        internal static T GetValueAllowNull<T>(this AtomicComposition? atomicComposition, T defaultResultAndKey) where T : class
        {
            ArgumentNullException.ThrowIfNull(defaultResultAndKey);

            return GetValueAllowNull<T>(atomicComposition, defaultResultAndKey, defaultResultAndKey);
        }

        internal static T GetValueAllowNull<T>(this AtomicComposition? atomicComposition, object key, T defaultResult)
        {
            T? result;
            if (atomicComposition != null && atomicComposition.TryGetValue<T>(key, out result))
            {
                return result;
            }

            return defaultResult;
        }

        internal static void AddRevertActionAllowNull(this AtomicComposition? atomicComposition, Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (atomicComposition == null)
            {
                action();
            }
            else
            {
                atomicComposition.AddRevertAction(action);
            }
        }

        internal static void AddCompleteActionAllowNull(this AtomicComposition? atomicComposition, Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (atomicComposition == null)
            {
                action();
            }
            else
            {
                atomicComposition.AddCompleteAction(action);
            }
        }
    }
}
