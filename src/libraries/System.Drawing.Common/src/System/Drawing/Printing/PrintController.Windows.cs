// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Drawing.Printing
{
    /// <summary>
    /// Controls how a document is printed.
    /// </summary>
    public abstract class PrintController
    {
        // DEVMODEs are pretty expensive, so we cache one here and share it with the 
        // Standard and Preview print controllers.  If it weren't for all the rules about API changes,
        // I'd consider making this protected.

        #region SafeDeviceModeHandle Class

        /// <summary>
        /// Represents a SafeHandle for a Printer's Device Mode struct handle (DEVMODE)
        /// </summary>
        internal sealed class SafeDeviceModeHandle : SafeHandle
        {
            // This constructor is used by the P/Invoke marshaling layer
            // to allocate a SafeHandle instance.  P/Invoke then does the
            // appropriate method call, storing the handle in this class.
            private SafeDeviceModeHandle() : base(IntPtr.Zero, true) { return; }

            internal SafeDeviceModeHandle(IntPtr handle)
                : base(IntPtr.Zero, true)  // "true" means "owns the handle"
            {
                SetHandle(handle);
            }

            public override bool IsInvalid
            {
                get { return handle == IntPtr.Zero; }
            }

            // Specifies how to free the handle.
            // The boolean returned should be true for success and false if the runtime
            // should fire a SafeHandleCriticalFailure MDA (CustomerDebugProbe) if that
            // MDA is enabled.
            protected override bool ReleaseHandle()
            {
                if (!IsInvalid)
                {
                    SafeNativeMethods.GlobalFree(new HandleRef(this, handle));
                }
                handle = IntPtr.Zero;

                return true;
            }

            public static implicit operator IntPtr(SafeDeviceModeHandle handle)
            {
                return (handle == null) ? IntPtr.Zero : handle.handle;
            }

            public static explicit operator SafeDeviceModeHandle(IntPtr handle)
            {
                return new SafeDeviceModeHandle(handle);
            }
        }

        #endregion

        internal SafeDeviceModeHandle modeHandle = null;

        /// <summary>
        /// Initializes a new instance of the <see cref='PrintController'/> class.
        /// </summary>
        protected PrintController()
        {
        }


        /// <summary>
        /// This is new public property which notifies if this controller is used for PrintPreview.
        /// </summary>
        public virtual bool IsPreview
        {
            get
            {
                return false;
            }
        }

        // WARNING: if you have nested PrintControllers, this method won't get called on the inner one.
        // Add initialization code to StartPrint or StartPage instead.
        internal void Print(PrintDocument document)
        {
            //
            // Get the PrintAction for this event
            PrintAction printAction;
            if (IsPreview)
            {
                printAction = PrintAction.PrintToPreview;
            }
            else
            {
                printAction = document.PrinterSettings.PrintToFile ? PrintAction.PrintToFile : PrintAction.PrintToPrinter;
            }

            // Check that user has permission to print to this particular printer
            PrintEventArgs printEvent = new PrintEventArgs(printAction);
            document._OnBeginPrint(printEvent);
            if (printEvent.Cancel)
            {
                document._OnEndPrint(printEvent);
                return;
            }

            OnStartPrint(document, printEvent);
            if (printEvent.Cancel)
            {
                document._OnEndPrint(printEvent);
                OnEndPrint(document, printEvent);
                return;
            }

            bool canceled = true;

            try
            {
                // To enable optimization of the preview dialog, add the following to the config file:
                // <runtime >
                //     <!-- AppContextSwitchOverrides values are in the form of 'key1=true|false;key2=true|false  -->
                //     <AppContextSwitchOverrides value = "Switch.System.Drawing.Printing.OptimizePrintPreview=true" />
                // </runtime >
                canceled = LocalAppContextSwitches.OptimizePrintPreview ? PrintLoopOptimized(document) : PrintLoop(document);
            }
            finally
            {
                try
                {
                    document._OnEndPrint(printEvent);
                    printEvent.Cancel = canceled | printEvent.Cancel;
                }
                finally
                {
                    OnEndPrint(document, printEvent);
                }
            }
        }

        // Returns true if print was aborted.
        // WARNING: if you have nested PrintControllers, this method won't get called on the inner one
        // Add initialization code to StartPrint or StartPage instead.
        private bool PrintLoop(PrintDocument document)
        {
            QueryPageSettingsEventArgs queryEvent = new QueryPageSettingsEventArgs((PageSettings)document.DefaultPageSettings.Clone());
            for (;;)
            {
                document._OnQueryPageSettings(queryEvent);
                if (queryEvent.Cancel)
                {
                    return true;
                }

                PrintPageEventArgs pageEvent = CreatePrintPageEvent(queryEvent.PageSettings);
                Graphics graphics = OnStartPage(document, pageEvent);
                pageEvent.SetGraphics(graphics);

                try
                {
                    document._OnPrintPage(pageEvent);
                    OnEndPage(document, pageEvent);
                }
                finally
                {
                    pageEvent.Dispose();
                }

                if (pageEvent.Cancel)
                {
                    return true;
                }
                else if (!pageEvent.HasMorePages)
                {
                    return false;
                }
                else
                {
                    // loop
                }
            }
        }

        private bool PrintLoopOptimized(PrintDocument document)
        {
            PrintPageEventArgs pageEvent = null;
            PageSettings documentPageSettings = (PageSettings)document.DefaultPageSettings.Clone();
            QueryPageSettingsEventArgs queryEvent = new QueryPageSettingsEventArgs(documentPageSettings);
            for (;;)
            {
                queryEvent.PageSettingsChanged = false;
                document._OnQueryPageSettings(queryEvent);
                if (queryEvent.Cancel)
                {
                    return true;
                }

                if (!queryEvent.PageSettingsChanged)
                {
                    // QueryPageSettings event handler did not change the page settings,
                    // thus we use default page settings from the document object.
                    if (pageEvent == null)
                    {
                        pageEvent = CreatePrintPageEvent(queryEvent.PageSettings);
                    }
                    else
                    {
                        // This is not the first page and the settings had not changed since the previous page, 
                        // thus don't re-apply them.
                        pageEvent.CopySettingsToDevMode = false;
                    }

                    Graphics graphics = OnStartPage(document, pageEvent);
                    pageEvent.SetGraphics(graphics);
                }
                else
                {
                    // Page settings were customized, so use the customized ones in the start page event.
                    pageEvent = CreatePrintPageEvent(queryEvent.PageSettings);
                    Graphics graphics = OnStartPage(document, pageEvent);
                    pageEvent.SetGraphics(graphics);
                }

                try
                {
                    document._OnPrintPage(pageEvent);
                    OnEndPage(document, pageEvent);
                }
                finally
                {
                    pageEvent.Graphics.Dispose();
                    pageEvent.SetGraphics(null);
                }

                if (pageEvent.Cancel)
                {
                    return true;
                }
                else if (!pageEvent.HasMorePages)
                {
                    return false;
                }
            }
        }

        private PrintPageEventArgs CreatePrintPageEvent(PageSettings pageSettings)
        {
            Debug.Assert((modeHandle != null), "modeHandle is null.  Someone must have forgot to call base.StartPrint");


            Rectangle pageBounds = pageSettings.GetBounds(modeHandle);
            Rectangle marginBounds = new Rectangle(pageSettings.Margins.Left,
                                                   pageSettings.Margins.Top,
                                                   pageBounds.Width - (pageSettings.Margins.Left + pageSettings.Margins.Right),
                                                   pageBounds.Height - (pageSettings.Margins.Top + pageSettings.Margins.Bottom));

            PrintPageEventArgs pageEvent = new PrintPageEventArgs(null, marginBounds, pageBounds, pageSettings);
            return pageEvent;
        }


        /// <summary>
        /// When overridden in a derived class, begins the control sequence of when and how to print a document.
        /// </summary>
        public virtual void OnStartPrint(PrintDocument document, PrintEventArgs e)
        {
            modeHandle = (SafeDeviceModeHandle)document.PrinterSettings.GetHdevmode(document.DefaultPageSettings);
        }

        /// <summary>
        /// When overridden in a derived class, begins the control sequence of when and how to print a page in a document.
        /// </summary>
        public virtual Graphics OnStartPage(PrintDocument document, PrintPageEventArgs e)
        {
            return null;
        }

        /// <summary>
        /// When overridden in a derived class, completes the control sequence of when and how to print a page in a document.
        /// </summary>
        public virtual void OnEndPage(PrintDocument document, PrintPageEventArgs e)
        {
        }

        /// <summary>
        /// When overridden in a derived class, completes the control sequence of when and how to print a document.
        /// </summary>
        public virtual void OnEndPrint(PrintDocument document, PrintEventArgs e)
        {
            Debug.Assert((modeHandle != null), "modeHandle is null.  Someone must have forgot to call base.StartPrint");
            if (modeHandle != null)
            {
                modeHandle.Close();
            }
        }
    }
}
