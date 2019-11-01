// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.UnitTesting;

namespace System
{
    public class ReferenceTracker
    {
        public readonly List<WeakReference> ReferencesExpectedToBeCollected = new List<WeakReference>();
        public readonly List<WeakReference> ReferencesNotExpectedToBeCollected = new List<WeakReference>();

        public void AddReferencesExpectedToBeCollected(params object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                ReferencesExpectedToBeCollected.Add(new WeakReference(objects[i]));
                objects[i] = null;
            }
        }

        public void AddReferencesNotExpectedToBeCollected(params object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                ReferencesNotExpectedToBeCollected.Add(new WeakReference(objects[i]));
                objects[i] = null;
            }
        }

        public void CollectAndAssert()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            EqualityExtensions.IsTrueForAll(ReferencesExpectedToBeCollected, wr => wr.Target == null, "Object should have been collected.");
            EqualityExtensions.IsTrueForAll(ReferencesNotExpectedToBeCollected, wr => wr.Target != null, "Object should be have NOT been collected.");
        }
    }
}
