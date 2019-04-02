// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;
using System.Linq;

namespace System.IO.Packaging
{
    /// <summary>
    /// This class has the utility methods for composing and parsing an Uri of pack:// scheme
    /// </summary>
    public static partial class PackUriHelper
    {
        #region Public Methods

        /// <summary>
        /// This method is used to create a valid pack Uri
        /// </summary>
        /// <param name="packageUri">This is the uri that points to the entire package.
        /// This parameter should be an absolute Uri. This parameter cannot be null or empty 
        /// This method will create a valid pack uri that references the entire package</param>
        /// <returns>A Uri with the "pack://" scheme</returns>
        /// <exception cref="ArgumentNullException">If packageUri parameter is null</exception>
        /// <exception cref="ArgumentException">If packageUri parameter is not an absolute Uri</exception>
        public static Uri Create(Uri packageUri)
        {
            return Create(packageUri, null, null);
        }

        /// <summary>
        /// This method is used to create a valid pack Uri
        /// </summary>
        /// <param name="packageUri">This is the uri that points to the entire package.
        /// This parameter should be an absolute Uri. This parameter cannot be null or empty </param>
        /// <param name="partUri">This is the uri that points to the part within the package
        /// This parameter should be a relative Uri.
        /// This parameter can be null in which case we will create a valid pack uri
        /// that references the entire package</param>
        /// <returns>A Uri with the "pack://" scheme</returns>
         /// <exception cref="ArgumentNullException">If packageUri parameter is null</exception>
        /// <exception cref="ArgumentException">If packageUri parameter is not an absolute Uri</exception>
        /// <exception cref="ArgumentException">If partUri parameter does not conform to the valid partUri syntax</exception>
        public static Uri Create(Uri packageUri, Uri partUri)
        {
            return Create(packageUri, partUri, null);
        }

        /// <summary>
        /// This method is used to create a valid pack Uri
        /// </summary>
        /// <param name="packageUri">This is the uri that points to the entire package.
        /// This parameter should be an absolute Uri. This parameter cannot be null or empty </param>
        /// <param name="partUri">This is the uri that points to the part within the package
        /// This parameter should be a relative Uri.
        /// This parameter can be null in which case we will create a valid pack uri
        /// that references the entire package</param>
        /// <param name="fragment">Fragment for the resulting Pack URI. This parameter can be null
        /// The fragment string must start with a "#"</param>
        /// <returns>A Uri with the "pack://" scheme</returns>
        /// <exception cref="ArgumentNullException">If packageUri parameter is null</exception>
        /// <exception cref="ArgumentException">If packageUri parameter is not an absolute Uri</exception>
        /// <exception cref="ArgumentException">If partUri parameter does not conform to the valid partUri syntax</exception>
        /// <exception cref="ArgumentException">If fragment parameter is empty or does not start with a "#"</exception>
        public static Uri Create(Uri packageUri, Uri partUri, string fragment)
        {
            // Step 1 - Validate input parameters
            packageUri = ValidatePackageUri(packageUri);

            if (partUri != null)
                partUri = ValidatePartUri(partUri);

            if (fragment != null)
            {
                if (fragment == string.Empty || fragment[0] != '#')
                    throw new ArgumentException(SR.Format(SR.FragmentMustStartWithHash, nameof(fragment)));
            }

            // Step 2 - Remove fragment identifier from the package URI, if it is present
            // Since '#" is an excluded character in Uri syntax, it can only occur as the 
            // fragment identifier, in all other places it should be escaped.
            // Hence we can safely use IndexOf to find the begining of the fragment.
            string absolutePackageUri = packageUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped);

            if (!string.IsNullOrEmpty(packageUri.Fragment))
            {
                absolutePackageUri = absolutePackageUri.Substring(0, absolutePackageUri.IndexOf('#'));
            }

            // Step 3 - Escape: "%", "?", "@", "#" and "," in the package URI 
            absolutePackageUri = EscapeSpecialCharacters(absolutePackageUri);

            // Step 4 - Replace all '/' with ',' in the resulting string
            absolutePackageUri = absolutePackageUri.Replace('/', ',');

            // Step 5 - Append pack:// at the begining and a '/' at the end of the pack uri obtained so far            
            absolutePackageUri = String.Concat(PackUriHelper.UriSchemePack, Uri.SchemeDelimiter, absolutePackageUri);

            Uri packUri = new Uri(absolutePackageUri);

            // Step 6 - Append the part Uri if present.
            if (partUri != null)
                packUri = new Uri(packUri, partUri);

            // Step 7 - Append fragment if present
            if (fragment != null)
                packUri = new Uri(String.Concat(packUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped), fragment));

            // We want to ensure that internal content of resulting Uri has canonical form
            // i.e.  result.OrignalString would appear as perfectly formatted Uri string 
            // so we roundtrip the result.

            return new Uri(packUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped));
        }

        /// <summary>
        /// This method parses the pack uri and returns the inner
        /// Uri that points to the package as a whole.
        /// </summary>
        /// <param name="packUri">Uri which has pack:// scheme</param>
        /// <returns>Returns the inner uri that points to the entire package</returns>
        /// <exception cref="ArgumentNullException">If packUri parameter is null</exception>
        /// <exception cref="ArgumentException">If packUri parameter is not an absolute Uri</exception>
        /// <exception cref="ArgumentException">If packUri parameter does not have "pack://" scheme</exception>
        /// <exception cref="ArgumentException">If inner packageUri extracted from the packUri has a fragment component</exception>
        public static Uri GetPackageUri(Uri packUri)
        {
            //Parameter Validation is done in the following method
            ValidateAndGetPackUriComponents(packUri, out Uri packageUri, out _);

            return packageUri;
        }

        /// <summary>
        /// This method compares two pack uris and returns an int to indicate the equivalence. 
        /// </summary>
        /// <param name="firstPackUri">First Uri of pack:// scheme to be compared</param>
        /// <param name="secondPackUri">Second Uri of pack:// scheme to be compared</param>
        /// <returns>A 32-bit signed integer indicating the lexical relationship between the compared Uri components.
        /// Value - Less than zero means firstUri is less than secondUri
        /// Value - Equal to zero means both the Uris are equal
        /// Value - Greater than zero means firstUri is greater than secondUri </returns>
        /// <exception cref="ArgumentException">If either of the Uris are not absolute or if either of the Uris are not with pack:// scheme</exception>
        /// <exception cref="ArgumentException">If firstPackUri or secondPackUri parameter is not an absolute Uri</exception>
        /// <exception cref="ArgumentException">If firstPackUri or secondPackUri parameter does not have "pack://" scheme</exception>
        public static int ComparePackUri(Uri firstPackUri, Uri secondPackUri)
        {
            //If any of the operands are null then we simply call System.Uri compare to return the correct value
            if (firstPackUri == null || secondPackUri == null)
            {
                return CompareUsingSystemUri(firstPackUri, secondPackUri);
            }
            else
            {
                int compareResult;

                ValidateAndGetPackUriComponents(firstPackUri, out Uri firstPackageUri, out Uri firstPartUri);
                ValidateAndGetPackUriComponents(secondPackUri, out Uri secondPackageUri, out Uri secondPartUri);

                if (firstPackageUri.Scheme == PackUriHelper.UriSchemePack && secondPackageUri.Scheme == PackUriHelper.UriSchemePack)
                {
                    compareResult = ComparePackUri(firstPackageUri, secondPackageUri);
                }
                else
                {
                    compareResult = CompareUsingSystemUri(firstPackageUri, secondPackageUri);
                }

                //Iff the PackageUri match do we compare the part uris.
                if (compareResult == 0)
                {
                    compareResult = System.IO.Packaging.PackUriHelper.ComparePartUri(firstPartUri, secondPartUri);
                }

                return compareResult;
            }
        }

        #endregion Public Methods
        
        #region Internal Methods

        //This method validates the packUri and returns its two components if they are valid-
        //1. Package Uri
        //2. Part Uri
        internal static void ValidateAndGetPackUriComponents(Uri packUri, out Uri packageUri, out Uri partUri)
        {
            //Validate if its not null and is an absolute Uri, has pack:// Scheme.
            packUri = ValidatePackUri(packUri);
            packageUri = GetPackageUriComponent(packUri);
            partUri = GetPartUriComponent(packUri);
        }

        #endregion Internal Methods

        #region Private Constructor

        static PackUriHelper()
        {
            EnsurePackSchemeRegistered();
        }

        #endregion Private Constructor

        #region Private Methods

        private static void EnsurePackSchemeRegistered()
        {
            if (!UriParser.IsKnownScheme(UriSchemePack))
            {
                // Indicate that we want a default hierarchical parser with a registry based authority
                UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority), UriSchemePack, -1);
            }
        }

        /// <summary>
        /// This method is used to validate the package uri
        /// </summary>
        /// <param name="packageUri"></param>
        /// <returns></returns>
        private static Uri ValidatePackageUri(Uri packageUri)
        {
            if (packageUri == null)
                throw new ArgumentNullException(nameof(packageUri));

            if (!packageUri.IsAbsoluteUri)
                throw new ArgumentException(SR.UriShouldBeAbsolute, nameof(packageUri));

            return packageUri;
        }

        //validates is a given uri has pack:// scheme
        private static Uri ValidatePackUri(Uri packUri)
        {
            if (packUri == null)
                throw new ArgumentNullException(nameof(packUri));

            if (!packUri.IsAbsoluteUri)
                throw new ArgumentException(SR.UriShouldBeAbsolute, nameof(packUri));

            if (packUri.Scheme != PackUriHelper.UriSchemePack)
                throw new ArgumentException(SR.UriShouldBePackScheme, nameof(packUri));

            return packUri;
        }

        /// <summary>
        /// Escapes -  %', '@', ',', '?' in the package URI 
        /// This method modifies the string in a culture safe and case safe manner. 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string EscapeSpecialCharacters(string path)
        {            
            // Escaping for the following - '%'; '@'; ',' and '?'
            // !!Important!! - The order is important - The '%' sign should be escaped first.
            // This is currently enforced by the order of characters in the s_specialCharacterChars array
            foreach (char c in s_specialCharacterChars)
            {
                if (path.Contains(c))
                {
                    path = path.Replace(c.ToString(), Uri.HexEscape(c));
                }
            }

            return path;
        }

        //This method validates and returns the PackageUri component
        private static Uri GetPackageUriComponent(Uri packUri)
        {
            Debug.Assert(packUri != null, "packUri parameter cannot be null");

            //Step 1 - Get the authority part of the URI. This section represents that package URI
            String hostAndPort = packUri.GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped);

            //Step 2 - Replace the ',' with '/' to reconstruct the package URI
            hostAndPort = hostAndPort.Replace(',', '/');

            //Step 3 - Unescape the special characters that we had escaped to construct the packUri
            Uri packageUri = new Uri(Uri.UnescapeDataString(hostAndPort));

            if (packageUri.Fragment != String.Empty)
                throw new ArgumentException(SR.InnerPackageUriHasFragment);

            return packageUri;
        }

        //This method validates and returns the PartUri component.
        private static PackUriHelper.ValidatedPartUri GetPartUriComponent(Uri packUri)
        {
            Debug.Assert(packUri != null, "packUri parameter cannot be null");
            
            string partName = GetStringForPartUriFromAnyUri(packUri);

            if (partName == String.Empty)
                return null;
            else
                return ValidatePartUri(new Uri(partName, UriKind.Relative));
        }

        #endregion Private Methods
    }
}
