// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.ObjectiveC
{
    /// <summary>
    /// Callbacks used to initialize Objective-C marshalling.
    /// </summary>
    /// <remarks>
    /// The unmanaged callbacks must be written in native code since they will be called by the GC and
    /// managed code is not able to run at that time.
    /// </remarks>
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("ios")]
    [SupportedOSPlatform("tvos")]
    [SupportedOSPlatform("maccatalyst")]
    [CLSCompliant(false)]
    public unsafe struct ObjectiveCReferenceTrackingCallbacks
    {
        /// <summary>
        /// Gets or sets the callback invoked when reference tracking begins and ends.
        /// </summary>
        /// <remarks>
        /// The associated begin/end pair will never be nested. When using Workstation GC, the begin/end pair
        /// will be called on the same thread. When using Server GC, the begin/end pair is not guaranteed to
        /// be called on the same thread.
        /// </remarks>
        public delegate* unmanaged<void> BeginEndCallback { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked to determine if a managed object instance is referenced elsewhere,
        /// and must not be collected by the GC.
        /// </summary>
        /// <remarks>
        /// The callback is passed the tagged memory associated with the object and should return 0 for not
        /// referenced or 1 for referenced. Any other value has undefined behavior.
        /// </remarks>
        public delegate* unmanaged<IntPtr, int> IsReferencedCallback { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked to determine if references have changed since the
        /// <see cref="IsReferencedCallback"/> calls made during the current GC.
        /// </summary>
        /// <remarks>
        /// The callback is invoked after the last <see cref="IsReferencedCallback"/> call and before the GC
        /// commits to the results. It should return 0 if no <see cref="IsReferencedCallback"/> calls have changed
        /// or 1 if references have changed. Any other value has undefined behavior.
        ///
        /// If references have changed, the GC will not call <see cref="IsReferencedCallback"/> again during the
        /// current GC and instead treats every tracked object not otherwise found to be alive as referenced.
        /// Those objects will be reconsidered during a later GC.
        /// </remarks>
        public delegate* unmanaged<int> HaveReferencesChangedCallback { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when a tracked object enters the finalization queue.
        /// </summary>
        /// <remarks>
        /// The callback is passed the tagged memory associated with the object.
        /// </remarks>
        public delegate* unmanaged<IntPtr, void> TrackedObjectEnteredFinalization { get; set; }

        /// <summary>
        /// Gets or sets the handler for propagating unhandled exceptions across a reverse P/Invoke boundary.
        /// </summary>
        public ObjectiveCMarshal.UnhandledExceptionPropagationHandler UnhandledExceptionPropagationHandler { get; set; }
    }
}
