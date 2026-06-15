// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    public sealed partial class PseudoTerminal
    {
        /// <summary>
        /// Gets the input pipe that can be written to communicate with the pseudo-terminal.
        /// </summary>
        internal SafeFileHandle Input { get; }

        /// <summary>
        /// Gets the output pipe that can be read to receive output from the pseudo-terminal.
        /// </summary>
        internal SafeFileHandle Output { get; }

        /// <summary>
        /// Gets the pseudo console handle.
        /// </summary>
        internal SafePseudoConsoleHandle PseudoConsole { get; }

        private PseudoTerminal(SafeFileHandle input, SafeFileHandle output, SafePseudoConsoleHandle pseudoConsole)
        {
            Input = input;
            Output = output;
            PseudoConsole = pseudoConsole;
        }

        private static PseudoTerminal CreateCore(PseudoTerminalOptions? options)
        {
            // Windows CreatePseudoConsole requires a non-zero size.
            // Default to 120x30 if no size is specified, matching the default conhost window.
            short columns = (short)(options?.Columns ?? 120);
            short rows = (short)(options?.Rows ?? 30);

            // Create pipes for communication with the pseudo console.
            // The "input" pipe: we write to inputWritePipe, the console reads from inputReadPipe.
            // The "output" pipe: the console writes to outputWritePipe, we read from outputReadPipe.
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle inputReadPipe, out SafeFileHandle inputWritePipe);
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle outputReadPipe, out SafeFileHandle outputWritePipe);

            try
            {
                Interop.Kernel32.PseudoConsoleCoord size = new()
                {
                    X = columns,
                    Y = rows
                };

                int hr = Interop.Kernel32.CreatePseudoConsole(size, inputReadPipe, outputWritePipe, 0, out IntPtr hPC);
                if (hr != 0)
                {
                    throw new Win32Exception(hr);
                }

                SafePseudoConsoleHandle pseudoConsole = new(hPC);

                // Close the pipe ends that are now owned by the pseudo console.
                inputReadPipe.Dispose();
                outputWritePipe.Dispose();

                return new PseudoTerminal(inputWritePipe, outputReadPipe, pseudoConsole);
            }
            catch
            {
                inputReadPipe.Dispose();
                inputWritePipe.Dispose();
                outputReadPipe.Dispose();
                outputWritePipe.Dispose();
                throw;
            }
        }

        private void ResizeCore(int columns, int rows)
        {
            Interop.Kernel32.PseudoConsoleCoord size = new()
            {
                X = (short)columns,
                Y = (short)rows
            };

            int hr = Interop.Kernel32.ResizePseudoConsole(PseudoConsole, size);
            if (hr != 0)
            {
                throw new Win32Exception(hr);
            }
        }

        private void DisposeCore()
        {
            Input.Dispose();
            Output.Dispose();
            PseudoConsole.Dispose();
        }
    }
}
