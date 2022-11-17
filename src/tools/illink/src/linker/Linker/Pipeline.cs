﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// Pipeline.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Mono.Linker.Steps;

namespace Mono.Linker
{

	public class Pipeline
	{

		readonly List<IStep> _steps;
		public List<IMarkHandler> MarkHandlers { get; }

		public Pipeline ()
		{
			_steps = new List<IStep> ();
			MarkHandlers = new List<IMarkHandler> ();
		}

		public void PrependStep (IStep step)
		{
			_steps.Insert (0, step);
		}

		public void AppendStep (IStep step)
		{
			_steps.Add (step);
		}

		public void AppendMarkHandler (IMarkHandler step)
		{
			MarkHandlers.Add (step);
		}

		public void AddStepBefore (Type target, IStep step)
		{
			for (int i = 0; i < _steps.Count; i++) {
				if (target.IsInstanceOfType (_steps[i])) {
					_steps.Insert (i, step);
					return;
				}
			}
			throw new InternalErrorException ($"Step {step} could not be inserted before (not found) {target}");
		}

		public void AddStepBefore (IStep target, IStep step)
		{
			for (int i = 0; i < _steps.Count; i++) {
				if (_steps[i] == target) {
					_steps.Insert (i, step);
					return;
				}
			}
			throw new InternalErrorException ($"Step {step} could not be inserted before (not found) {target}");
		}

		public void AddMarkHandlerBefore (IMarkHandler target, IMarkHandler step)
		{
			for (int i = 0; i < MarkHandlers.Count; i++) {
				if (MarkHandlers[i] == target) {
					MarkHandlers.Insert (i, step);
					return;
				}
			}
			throw new InternalErrorException ($"Step {step} could not be inserted before (not found) {target}");
		}

		public void ReplaceStep (Type target, IStep step)
		{
			AddStepBefore (target, step);
			RemoveStep (target);
		}

		public void AddStepAfter (Type target, IStep step)
		{
			for (int i = 0; i < _steps.Count; i++) {
				if (target.IsInstanceOfType (_steps[i])) {
					if (i == _steps.Count - 1)
						_steps.Add (step);
					else
						_steps.Insert (i + 1, step);
					return;
				}
			}
			throw new InternalErrorException ($"Step {step} could not be inserted after (not found) {target}");
		}

		public void AddStepAfter (IStep target, IStep step)
		{
			for (int i = 0; i < _steps.Count; i++) {
				if (_steps[i] == target) {
					if (i == _steps.Count - 1)
						_steps.Add (step);
					else
						_steps.Insert (i + 1, step);
					return;
				}
			}
			throw new InternalErrorException ($"Step {step} could not be inserted after (not found) {target}");
		}

		public void AddMarkHandlerAfter (IMarkHandler target, IMarkHandler step)
		{
			for (int i = 0; i < MarkHandlers.Count; i++) {
				if (MarkHandlers[i] == target) {
					if (i == MarkHandlers.Count - 1)
						MarkHandlers.Add (step);
					else
						MarkHandlers.Insert (i + 1, step);
					return;
				}
			}
			throw new InternalErrorException ($"Step {step} could not be inserted after (not found) {target}");
		}

		public void RemoveStep (Type target)
		{
			for (int i = 0; i < _steps.Count; i++) {
				if (_steps[i].GetType () != target)
					continue;

				_steps.RemoveAt (i);
				break;
			}
		}

		public void Process (LinkContext context)
		{
			while (_steps.Count > 0) {
				IStep step = _steps[0];
				string? stepName = null;
				if (LinkerEventSource.Log.IsEnabled ()) {
					stepName = step.GetType ().Name;
					LinkerEventSource.Log.LinkerStepStart (stepName);
				}
				ProcessStep (context, step);
				if (LinkerEventSource.Log.IsEnabled ()) {
					stepName ??= step.GetType ().Name;
					LinkerEventSource.Log.LinkerStepStop (stepName);
				}
				_steps.Remove (step);
			}
		}

		protected virtual void ProcessStep (LinkContext context, IStep step)
		{
			step.Process (context);
		}

		public IStep[] GetSteps ()
		{
			return _steps.ToArray ();
		}

		public void InitializeMarkHandlers (LinkContext context, MarkContext markContext)
		{
			while (MarkHandlers.Count > 0) {
				IMarkHandler markHandler = MarkHandlers[0];
				markHandler.Initialize (context, markContext);
				MarkHandlers.Remove (markHandler);
			}
		}

		public bool ContainsStep (Type type)
		{
			foreach (IStep step in _steps)
				if (step.GetType () == type)
					return true;

			return false;
		}
	}
}
