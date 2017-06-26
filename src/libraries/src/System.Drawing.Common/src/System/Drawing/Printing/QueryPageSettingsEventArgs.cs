// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <summary>
    /// Provides data for the <see cref='PrintDocument.QueryPageSettings'/> event.
    /// </summary>
    public class QueryPageSettingsEventArgs : PrintEventArgs
    {
        private PageSettings _pageSettings;

        /// <summary>
        /// It's too expensive to compare 2 instances of PageSettings class, as the getters
        /// are accessing the printer spooler, thus we track any explicit invocations of the setters or getters on this class,
        /// and this field tracks if PageSettings property was accessed. It will return a false 
        /// positive when the user is reading property values, but we'll take a perf hit in this case assuming this event is not 
        /// used often.
        internal bool PageSettingsChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref='QueryPageSettingsEventArgs'/> class.
        /// </summary>
        public QueryPageSettingsEventArgs(PageSettings pageSettings) : base()
        {
            _pageSettings = pageSettings;
        }

        /// <summary>
        /// Gets or sets the page settings for the page to be printed.
        /// </summary>
        public PageSettings PageSettings
        {
            get
            {
                PageSettingsChanged = true;
                return _pageSettings;
            }
            set
            {
                if (value == null)
                {
                    value = new PageSettings();
                }
                _pageSettings = value;
                PageSettingsChanged = true;
            }
        }
    }
}

