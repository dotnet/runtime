// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.Runtime.Tests
{
    // NOTE: DependentHandle is already heavily tested indirectly through ConditionalWeakTable<,>.
    // This class contains some specific tests for APIs that are only relevant when used directly.
    public class DependentHandleTests
    {
        [Fact]
        public void GetNullTarget()
        {
            object target = new();
            DependentHandle handle = new(null, null);

            Assert.True(handle.IsAllocated);
            Assert.Null(handle.Target);
            Assert.Null(handle.Dependent);

            handle.Dispose();
        }

        [Fact]
        public void GetNotNullTarget()
        {
            object target = new();
            DependentHandle handle = new(target, null);

            // A handle with a set target and no dependent is valid
            Assert.True(handle.IsAllocated);
            Assert.Same(target, handle.Target);
            Assert.Null(handle.Dependent);

            handle.Dispose();
        }

        [Fact]
        public void SetTargetToNull_StateIsConsistent()
        {
            object target = new(), dependent = new();
            DependentHandle handle = new(target, dependent);

            Assert.True(handle.IsAllocated);
            Assert.Same(handle.Target, target);
            Assert.Same(handle.Dependent, dependent);

            handle.Target = null;

            Assert.True(handle.IsAllocated);
            Assert.Null(handle.Target);
            Assert.Null(handle.Dependent);

            handle.Dispose();
        }

        [Fact]
        public void SetTargetToNull_RepeatedCallsAreFine()
        {
            object target = new(), dependent = new();
            DependentHandle handle = new(target, dependent);

            handle.Target = null;

            Assert.True(handle.IsAllocated);
            Assert.Null(handle.Target);
            Assert.Null(handle.Dependent);

            handle.Target = null;
            handle.Target = null;
            handle.Target = null;

            Assert.True(handle.IsAllocated);
            Assert.Null(handle.Target);
            Assert.Null(handle.Dependent);

            handle.Dispose();
        }

        [Fact]
        public void GetSetDependent()
        {
            object target = new(), dependent = new();
            DependentHandle handle = new(target, null);

            // The target can be retrieved correctly
            Assert.True(handle.IsAllocated);
            Assert.Same(target, handle.Target);
            Assert.Null(handle.Dependent);

            handle.Dependent = dependent;

            // The dependent can also be retrieved correctly
            Assert.Same(target, handle.Target);
            Assert.Same(dependent, handle.Dependent);

            handle.Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public void TargetKeepsDependentAlive()
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static DependentHandle Initialize(out object target, out WeakReference weakDependent)
            {
                target = new object();

                object dependent = new();

                weakDependent = new WeakReference(dependent);

                return new DependentHandle(target, dependent);
            }

            DependentHandle handle = Initialize(out object target, out WeakReference dependent);

            GC.Collect();

            // The dependent has to still be alive as the target has a strong reference
            Assert.Same(target, handle.Target);
            Assert.True(dependent.IsAlive);
            Assert.Same(dependent.Target, handle.Dependent);

            GC.KeepAlive(target);

            handle.Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public void DependentDoesNotKeepTargetAlive()
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static DependentHandle Initialize(out WeakReference weakTarget, out object dependent)
            {
                dependent = new object();

                object target = new();

                weakTarget = new WeakReference(target);

                return new DependentHandle(target, dependent);
            }

            DependentHandle handle = Initialize(out WeakReference target, out object dependent);

            GC.Collect();

            // The target has to be collected, as there were no strong references to it
            Assert.Null(handle.Target);
            Assert.False(target.IsAlive);

            GC.KeepAlive(target);

            handle.Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public void DependentIsCollectedOnTargetNotReachable()
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static DependentHandle Initialize(out WeakReference weakTarget, out WeakReference weakDependent)
            {
                object target = new(), dependent = new();

                weakTarget = new WeakReference(target);
                weakDependent = new WeakReference(dependent);

                return new DependentHandle(target, dependent);
            }

            DependentHandle handle = Initialize(out WeakReference target, out WeakReference dependent);

            GC.Collect();

            // Both target and dependent have to be collected, as there were no strong references to either
            Assert.Null(handle.Target);
            Assert.Null(handle.Dependent);
            Assert.False(target.IsAlive);
            Assert.False(dependent.IsAlive);

            handle.Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public void DependentIsCollectedOnTargetNotReachable_EvenWithReferenceCycles()
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static DependentHandle Initialize(out WeakReference weakTarget, out WeakReference weakDependent)
            {
                object target = new();
                ObjectWithReference dependent = new() { Reference = target };

                weakTarget = new WeakReference(target);
                weakDependent = new WeakReference(dependent);

                return new DependentHandle(target, dependent);
            }

            DependentHandle handle = Initialize(out WeakReference target, out WeakReference dependent);

            GC.Collect();

            // Both target and dependent have to be collected, as there were no strong references to either.
            // The fact that the dependent has a strong reference back to the target should not affect this.
            Assert.Null(handle.Target);
            Assert.Null(handle.Dependent);
            Assert.False(target.IsAlive);
            Assert.False(dependent.IsAlive);

            handle.Dispose();
        }

        private sealed class ObjectWithReference
        {
            public object Reference;
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public void DependentIsCollectedAfterTargetIsSetToNull()
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static DependentHandle Initialize(out object target, out WeakReference weakDependent)
            {
                target = new();

                object dependent = new();

                weakDependent = new WeakReference(dependent);

                return new DependentHandle(target, dependent);
            }

            DependentHandle handle = Initialize(out object target, out WeakReference dependent);

            handle.Target = null;

            GC.Collect();

            // After calling StopTracking, the dependent is collected even if
            // target is still alive and the handle itself has not been disposed
            Assert.True(handle.IsAllocated);
            Assert.Null(handle.Target);
            Assert.Null(handle.Dependent);
            Assert.False(dependent.IsAlive);

            GC.KeepAlive(target);

            handle.Dispose();
        }

        [Fact]
        public void GetTarget_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => default(DependentHandle).Target);
        }

        [Fact]
        public void GetDependent_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => default(DependentHandle).Dependent);
        }

        [Fact]
        public void SetTarget_NotAllocated_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DependentHandle handle = default;
                handle.Target = new();
            });
        }

        [Fact]
        public void SetTarget_NotNullObject_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DependentHandle handle = default;

                try
                {
                    handle.Target = new();
                }
                finally
                {
                    handle.Dispose();
                }                
            });
        }

        [Fact]
        public void SetDependent_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DependentHandle handle = default;
                handle.Dependent = new();
            });
        }

        [Fact]
        public void Dispose_RepeatedCallsAreFine()
        {
            object target = new(), dependent = new();
            DependentHandle handle = new(target, dependent);

            Assert.True(handle.IsAllocated);

            handle.Dispose();

            Assert.False(handle.IsAllocated);

            handle.Dispose();

            Assert.False(handle.IsAllocated);

            handle.Dispose();
            handle.Dispose();
            handle.Dispose();

            Assert.False(handle.IsAllocated);
        }

        [Fact]
        public void Dispose_ValidOnDefault()
        {
            DependentHandle handle = default;
            Assert.False(handle.IsAllocated);
            handle.Dispose();
        }
    }
}
