// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Diagnostics
{
	partial class StackFrame
	{
		internal StackFrame (MonoStackFrame monoStackFrame, bool needFileInfo)
		{
			_method = monoStackFrame.methodBase;
			_nativeOffset = monoStackFrame.nativeOffset;
			_ilOffset = monoStackFrame.ilOffset;

			if (needFileInfo) {
				_fileName = monoStackFrame.fileName;
				_lineNumber = monoStackFrame.lineNumber;
				_columnNumber = monoStackFrame.columnNumber;
			}

			_isLastFrameFromForeignExceptionStackTrace = monoStackFrame.isLastFrameFromForeignException;
		}

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		void BuildStackFrame (int skipFrames, bool needFileInfo)
		{
			const int SystemDiagnosticsStackDepth = 3;

			if (skipFrames + SystemDiagnosticsStackDepth < 0 || !get_frame_info (skipFrames + SystemDiagnosticsStackDepth, needFileInfo, out var method, out var ilOffset, out var nativeOffset, out var fileName, out var line, out var column))
				return;

			_method = method;
			_ilOffset = ilOffset;
			_nativeOffset = nativeOffset;

			if (needFileInfo) {
				_fileName = fileName;
				_lineNumber = line;
				_columnNumber = column;
			}
		}

		bool AppendStackFrameWithoutMethodBase (StringBuilder sb) => false;

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern bool get_frame_info (int skipFrames, bool needFileInfo,
			out MethodBase method, out int ilOffset, out int nativeOffset, out string file, out int line, out int column);

	}
}
