// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text
{
    public sealed class EncodingInfo
    {
        /// <summary>
        /// Construct an <see cref="EncodingInfo"/> object.
        /// </summary>
        /// <param name="provider">The <see cref="EncodingProvider"/> object which created this <see cref="EncodingInfo"/> object</param>
        /// <param name="codePage">The encoding codepage</param>
        /// <param name="name">The encoding name</param>
        /// <param name="displayName">The encoding display name</param>
        /// <returns></returns>
        public EncodingInfo(EncodingProvider provider, int codePage, string name, string displayName) : this(codePage, name, displayName)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(displayName);

            Provider = provider;
        }

        internal EncodingInfo(int codePage, string name, string displayName)
        {
            CodePage = codePage;
            Name = name;
            DisplayName = displayName;
        }

        /// <summary>
        /// Get the encoding codepage number
        /// </summary>
        /// <value>The codepage integer number</value>
        public int CodePage { get; }

        /// <summary>
        /// Get the encoding name
        /// </summary>
        /// <value>The encoding name string</value>
        public string Name { get; }

        /// <summary>
        /// Get the encoding display name
        /// </summary>
        /// <value>The encoding display name string</value>
        public string DisplayName { get; }

        /// <summary>
        /// Get the <see cref="Encoding"/> object match the information in the <see cref="EncodingInfo"/> object
        /// </summary>
        /// <returns>The <see cref="Encoding"/> object</returns>
        public Encoding GetEncoding() => Provider?.GetEncoding(CodePage) ?? Encoding.GetEncoding(CodePage);

        /// <summary>
        /// Compare this <see cref="EncodingInfo"/> object to other object.
        /// </summary>
        /// <param name="value">The other object to compare with this object</param>
        /// <returns>True if the value object is EncodingInfo object and has a codepage equals to this EncodingInfo object codepage. Otherwise, it returns False</returns>
        public override bool Equals([NotNullWhen(true)] object? value) => value is EncodingInfo that && CodePage == that.CodePage;

        /// <summary>
        /// Get a hashcode representing the current EncodingInfo object.
        /// </summary>
        /// <returns>The integer value representing the hash code of the EncodingInfo object.</returns>
        public override int GetHashCode()
        {
            return CodePage;
        }

        internal EncodingProvider? Provider { get; }
    }
}
