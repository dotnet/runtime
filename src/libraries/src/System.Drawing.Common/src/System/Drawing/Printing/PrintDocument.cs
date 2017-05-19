// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    using System.ComponentModel;

    /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument"]/*' />
    /// <devdoc>
    ///    <para>Defines a reusable object that sends output to the
    ///       printer.</para>
    /// </devdoc>
    [SRDescription(nameof(SR.PrintDocumentDesc))]
    public class PrintDocument : Component
    {
        private string _documentName = "document";

        private PrintEventHandler _beginPrintHandler;
        private PrintEventHandler _endPrintHandler;
        private PrintPageEventHandler _printPageHandler;
        private QueryPageSettingsEventHandler _queryHandler;

        private PrinterSettings _printerSettings = new PrinterSettings();
        private PageSettings _defaultPageSettings;

        private PrintController _printController;

        private bool _originAtMargins;
        private bool _userSetPageSettings;

        /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument.PrintDocument"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Drawing.Printing.PrintDocument'/>
        /// class.</para>
        /// </devdoc>
        public PrintDocument()
        {
            _defaultPageSettings = new PageSettings(_printerSettings);
        }

        /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument.DefaultPageSettings"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the
        ///       default
        ///       page settings for the document being printed.</para>
        /// </devdoc>
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

        /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument.DocumentName"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the name to display to the user while printing the document;
        ///       for example, in a print status dialog or a printer
        ///       queue.</para>
        /// </devdoc>
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

        /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument.OriginAtMargins"]/*' />
        // If true, positions the origin of the graphics object 
        // associated with the page at the point just inside
        // the user-specified margins of the page.
        // If false, the graphics origin is at the top-left
        // corner of the printable area of the page.
        //
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

        /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument.PrintController"]/*' />
        /// <devdoc>
        /// <para>Gets or sets the <see cref='System.Drawing.Printing.PrintController'/> 
        /// that guides the printing process.</para>
        /// </devdoc>
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

        /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument.PrinterSettings"]/*' />
        /// <devdoc>
        ///    <para> Gets or sets the printer on which the
        ///       document is printed.</para>
        /// </devdoc>
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

        /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument.BeginPrint"]/*' />
        /// <devdoc>
        /// <para>Occurs when the <see cref='System.Drawing.Printing.PrintDocument.Print'/> method is called, before 
        ///    the
        ///    first page prints.</para>
        /// </devdoc>
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

        /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument.EndPrint"]/*' />
        /// <devdoc>
        /// <para>Occurs when <see cref='System.Drawing.Printing.PrintDocument.Print'/> is
        ///    called, after the last page is printed.</para>
        /// </devdoc>
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

        /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument.PrintPage"]/*' />
        /// <devdoc>
        ///    <para>Occurs when a page is printed. </para>
        /// </devdoc>
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

        /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument.QueryPageSettings"]/*' />
        /// <devdoc>
        ///    <para>Occurs</para>
        /// </devdoc>
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

        internal void _OnBeginPrint(PrintEventArgs e)
        {
            OnBeginPrint(e);
        }

        /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument.OnBeginPrint"]/*' />
        /// <devdoc>
        /// <para>Raises the <see cref='E:System.Drawing.Printing.PrintDocument.BeginPrint'/>
        /// event.</para>
        /// </devdoc>
        protected virtual void OnBeginPrint(PrintEventArgs e)
        {
            if (_beginPrintHandler != null)
                _beginPrintHandler(this, e);
        }

        internal void _OnEndPrint(PrintEventArgs e)
        {
            OnEndPrint(e);
        }

        /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument.OnEndPrint"]/*' />
        /// <devdoc>
        /// <para>Raises the <see cref='E:System.Drawing.Printing.PrintDocument.EndPrint'/>
        /// event.</para>
        /// </devdoc>
        protected virtual void OnEndPrint(PrintEventArgs e)
        {
            if (_endPrintHandler != null)
                _endPrintHandler(this, e);
        }

        internal void _OnPrintPage(PrintPageEventArgs e)
        {
            OnPrintPage(e);
        }

        /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument.OnPrintPage"]/*' />
        /// <devdoc>
        /// <para>Raises the <see cref='E:System.Drawing.Printing.PrintDocument.PrintPage'/>
        /// event.</para>
        /// </devdoc>
        protected virtual void OnPrintPage(PrintPageEventArgs e)
        {
            if (_printPageHandler != null)
                _printPageHandler(this, e);
        }

        internal void _OnQueryPageSettings(QueryPageSettingsEventArgs e)
        {
            OnQueryPageSettings(e);
        }

        /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument.OnQueryPageSettings"]/*' />
        /// <devdoc>
        /// <para>Raises the <see cref='E:System.Drawing.Printing.PrintDocument.QueryPageSettings'/> event.</para>
        /// </devdoc>
        protected virtual void OnQueryPageSettings(QueryPageSettingsEventArgs e)
        {
            if (_queryHandler != null)
                _queryHandler(this, e);
        }

        /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument.Print"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Prints the document.
        ///    </para>
        /// </devdoc>
        public void Print()
        {
            PrintController controller = PrintController;
            controller.Print(this);
        }

        /// <include file='doc\PrintDocument.uex' path='docs/doc[@for="PrintDocument.ToString"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///    <para>
        ///       Provides some interesting information about the PrintDocument in
        ///       String form.
        ///    </para>
        /// </devdoc>
        public override string ToString()
        {
            return "[PrintDocument " + DocumentName + "]";
        }
    }
}

