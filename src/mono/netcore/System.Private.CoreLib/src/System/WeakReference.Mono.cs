// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System
{
	partial class WeakReference
	{
		bool trackResurrection;
		GCHandle handle;

		public virtual bool IsAlive => Target != null;

		public virtual object Target {
			get {
				if (!handle.IsAllocated)
					return null;
				return handle.Target;
			}
			set {
				handle.Target = value;
			}
		}

		public virtual bool TrackResurrection => IsTrackResurrection ();

		~WeakReference ()
		{
			handle.Free ();
		}

		void Create (object target, bool trackResurrection)
		{
			if (trackResurrection) {
				this.trackResurrection = true;
				handle = GCHandle.Alloc (target, GCHandleType.WeakTrackResurrection);
			} else {
				handle = GCHandle.Alloc (target, GCHandleType.Weak);
			}
		}

		bool IsTrackResurrection () => trackResurrection;
	}
}
