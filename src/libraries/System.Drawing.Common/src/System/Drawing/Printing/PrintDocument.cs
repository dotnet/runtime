// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Drawing.Printing
{
    /// <summary>
    /// Defines a reusable object that sends output to the printer.
    /// </summary>
    [DefaultProperty("DocumentName"), DefaultEvent("PrintPage"), SRDescription(nameof(SR.PrintDocumentDesc))]
    public class PrintDocument : Component
    {
        private string _documentName = "document";

        private PrintEventHandler? _beginPrintHandler;
        private PrintEventHandler? _endPrintHandler;
        private PrintPageEventHandler? _printPageHandler;
        private QueryPageSettingsEventHandler? _queryHandler;

        private PrinterSettings _printerSettings = new PrinterSettings();
        private PageSettings _defaultPageSettings;

        private PrintController? _printController;

        private bool _originAtMargins;
        private bool _userSetPageSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref='PrintDocument'/> class.
        /// </summary>
        public PrintDocument()
        {
            _defaultPageSettings = new PageSettings(_printerSettings);
        }

        /// <summary>
        /// Gets or sets the default page settings for the document being printed.
        /// </summary>
        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        SRDescription(nameof(SR.PDOCdocumentPageSettingsDescr))
        ]
        public PageSettings DefaultPageSettings
        {
            get { return _defaultPageSettings; }
            set
            {
                if (value == null)
                    value = new PageSettings();
                _defaultPageSettings = value;
                _userSetPageSettings = true;
            }
        }

        /// <summary>
        /// Gets or sets the name to display to the user while printing the document; for example, in a print status
        /// dialog or a printer queue.
        /// </summary>
        [
        DefaultValue("document"),
        SRDescription(nameof(SR.PDOCdocumentNameDescr))
        ]
        public string DocumentName
        {
            get { return _documentName; }

            set
            {
                if (value == null)
                    value = "";
                _documentName = value;
            }
        }

        // If true, positions the origin of the graphics object
        // associated with the page at the point just inside
        // the user-specified margins of the page.
        // If false, the graphics origin is at the top-left
        // corner of the printable area of the page.
        [
        DefaultValue(false),
        SRDescription(nameof(SR.PDOCoriginAtMarginsDescr))
        ]
        public bool OriginAtMargins
        {
            get
            {
                return _originAtMargins;
            }
            set
            {
                _originAtMargins = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref='Printing.PrintController'/>  that guides the printing process.
        /// </summary>
        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        SRDescription(nameof(SR.PDOCprintControllerDescr))
        ]
        public PrintController PrintController
        {
            get
            {
                if (_printController == null)
                {
                    _printController = new StandardPrintController();
                }
                return _printController;
            }
            set
            {
                _printController = value;
            }
        }

        /// <summary>
        /// Gets or sets the printer on which the document is printed.
        /// </summary>
        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        SRDescription(nameof(SR.PDOCprinterSettingsDescr))
        ]
        public PrinterSettings PrinterSettings
        {
            get { return _printerSettings; }
            set
            {
                if (value == null)
                    value = new PrinterSettings();
                _printerSettings = value;
                // reset the PageSettings that match the PrinterSettings only if we have created the defaultPageSettings..
                if (!_userSetPageSettings)
                {
                    _defaultPageSettings = _printerSettings.DefaultPageSettings;
                }
            }
        }

        /// <summary>
        /// Occurs when the <see cref='Print'/> method is called, before the first page prints.
        /// </summary>
        [SRDescription(nameof(SR.PDOCbeginPrintDescr))]
        public event PrintEventHandler BeginPrint
        {
            add
            {
                _beginPrintHandler += value;
            }
            remove
            {
                _beginPrintHandler -= value;
            }
        }

        /// <summary>
        /// Occurs when <see cref='Print'/> is called, after the last page is printed.
        /// </summary>
        [SRDescription(nameof(SR.PDOCendPrintDescr))]
        public event PrintEventHandler EndPrint
        {
            add
            {
                _endPrintHandler += value;
            }
            remove
            {
                _endPrintHandler -= value;
            }
        }

        /// <summary>
        /// Occurs when a page is printed.
        /// </summary>
        [SRDescription(nameof(SR.PDOCprintPageDescr))]
        public event PrintPageEventHandler PrintPage
        {
            add
            {
                _printPageHandler += value;
            }
            remove
            {
                _printPageHandler -= value;
            }
        }

        [SRDescription(nameof(SR.PDOCqueryPageSettingsDescr))]
        public event QueryPageSettingsEventHandler QueryPageSettings
        {
            add
            {
                _queryHandler += value;
            }
            remove
            {
                _queryHandler -= value;
            }
        }

        /// <summary>
        /// Raises the <see cref='BeginPrint'/> event.
        /// </summary>
        protected internal virtual void OnBeginPrint(PrintEventArgs e)
        {
            _beginPrintHandler?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref='EndPrint'/> event.
        /// </summary>
        protected internal virtual void OnEndPrint(PrintEventArgs e)
        {
            _endPrintHandler?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref='PrintPage'/> event.
        /// </summary>
        protected internal virtual void OnPrintPage(PrintPageEventArgs e)
        {
            _printPageHandler?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref='QueryPageSettings'/> event.
        /// </summary>
        protected internal virtual void OnQueryPageSettings(QueryPageSettingsEventArgs e)
        {
            _queryHandler?.Invoke(this, e);
        }

        /// <summary>
        /// Prints the document.
        /// </summary>
        public void Print()
        {
            PrintController controller = PrintController;
            controller.Print(this);
        }

        /// <summary>
        /// Provides some interesting information about the PrintDocument in String form.
        /// </summary>
        public override string ToString() => $"[PrintDocument {DocumentName}]";
    }
}
