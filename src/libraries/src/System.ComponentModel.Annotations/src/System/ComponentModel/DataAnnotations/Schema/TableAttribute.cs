// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace System.ComponentModel.DataAnnotations.Schema
{
    /// <summary>
    ///     Specifies the database table that a class is mapped to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TableAttribute : Attribute
    {
        private readonly string _name;
        private string _schema;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TableAttribute" /> class.
        /// </summary>
        /// <param name="name">The name of the table the class is mapped to.</param>
        public TableAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                    SR.ArgumentIsNullOrWhitespace, "name"));
            }
            _name = name;
        }

        /// <summary>
        ///     The name of the table the class is mapped to.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        ///     The schema of the table the class is mapped to.
        /// </summary>
        public string Schema
        {
            get { return _schema; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                        SR.ArgumentIsNullOrWhitespace, "value"));
                }
                _schema = value;
            }
        }
    }
}
