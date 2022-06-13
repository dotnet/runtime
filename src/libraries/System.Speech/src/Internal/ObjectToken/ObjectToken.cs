// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Speech.Internal.SapiInterop;

namespace System.Speech.Internal.ObjectTokens
{
    [DebuggerDisplay("{Name}")]
    internal class ObjectToken : RegistryDataKey, ISpObjectToken
    {
        #region Constructors

        protected ObjectToken(ISpObjectToken sapiObjectToken, bool disposeSapiToken)
            : base(sapiObjectToken)
        {
            ArgumentNullException.ThrowIfNull(sapiObjectToken);

            _sapiObjectToken = sapiObjectToken;
            _disposeSapiObjectToken = disposeSapiToken;
        }

        /// <summary>
        /// Creates a ObjectToken from an already-existing ISpObjectToken.
        /// Assumes the token was created through enumeration, thus should not be disposed by us.
        /// </summary>
        /// <returns>ObjectToken object</returns>
        internal static ObjectToken Open(ISpObjectToken sapiObjectToken)
        {
            return new ObjectToken(sapiObjectToken, false);
        }

        /// <summary>
        /// Creates a new ObjectToken from a category
        /// Unlike the other Open overload, this one creates a new SAPI object, so Dispose must be called if
        /// you are creating ObjectTokens with this function.
        /// </summary>
        /// <returns>ObjectToken object</returns>
        internal static ObjectToken Open(string sCategoryId, string sTokenId, bool fCreateIfNotExist)
        {
            ISpObjectToken sapiObjectToken = (ISpObjectToken)new SpObjectToken();

            try
            {
                sapiObjectToken.SetId(sCategoryId, sTokenId, fCreateIfNotExist);
            }
            catch (Exception)
            {
                Marshal.ReleaseComObject(sapiObjectToken);
                return null;
            }

            return new ObjectToken(sapiObjectToken, true);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (_disposeSapiObjectToken == true && _sapiObjectToken != null)
                    {
                        Marshal.ReleaseComObject(_sapiObjectToken);
                        _sapiObjectToken = null;
                    }
                    if (_attributes != null)
                    {
                        _attributes.Dispose();
                        _attributes = null;
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #endregion

        #region public Methods

        /// <summary>
        /// Tests whether two AutomationIdentifier objects are equivalent
        /// </summary>
        public override bool Equals(object obj)
        {
            ObjectToken token = obj as ObjectToken;
            return token != null && string.Equals(Id, token.Id, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Overrides Object.GetHashCode()
        /// </summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion

        #region Internal Properties

        internal RegistryDataKey Attributes
        {
            get
            {
                return _attributes ??= OpenKey("Attributes");
            }
        }

        internal ISpObjectToken SAPIToken
        {
            get
            {
                return _sapiObjectToken;
            }
        }

        /// <summary>
        /// Returns the Age from a voice token
        /// </summary>
        internal string Age
        {
            get
            {
                string age;
                if (Attributes == null || !Attributes.TryGetString("Age", out age))
                {
                    age = string.Empty;
                }
                return age;
            }
        }

        /// <summary>
        /// Returns the gender
        /// </summary>
        internal string Gender
        {
            get
            {
                string gender;
                if (Attributes == null || !Attributes.TryGetString("Gender", out gender))
                {
                    gender = string.Empty;
                }
                return gender;
            }
        }

        /// <summary>
        /// Returns the Name for the voice
        /// Look first in the Name attribute, if not available then get the default string
        /// </summary>
        internal string TokenName()
        {
            string name = string.Empty;
            if (Attributes != null)
            {
                Attributes.TryGetString("Name", out name);

                if (string.IsNullOrEmpty(name))
                {
                    TryGetString(null, out name);
                }
            }
            return name;
        }

        /// <summary>
        /// Returns the Culture defined in the Language field for a token
        /// </summary>
        internal CultureInfo Culture
        {
            get
            {
                CultureInfo culture = null;
                string langId;
                if (Attributes.TryGetString("Language", out langId))
                {
                    culture = SapiAttributeParser.GetCultureInfoFromLanguageString(langId);
                }
                return culture;
            }
        }

        /// <summary>
        /// Returns the Culture defined in the Language field for a token
        /// </summary>
        internal string Description
        {
            get
            {
                string description = string.Empty;
                string sCultureId = string.Format(CultureInfo.InvariantCulture, "{0:x}", CultureInfo.CurrentUICulture.LCID);
                if (!TryGetString(sCultureId, out description))
                {
                    TryGetString(null, out description);
                }
                return description;
            }
        }

        #endregion

        #region internal Methods

        #region ISpObjectToken Implementation

        public void SetId([MarshalAs(UnmanagedType.LPWStr)] string pszCategoryId, [MarshalAs(UnmanagedType.LPWStr)] string pszTokenId, [MarshalAs(UnmanagedType.Bool)] bool fCreateIfNotExist)
        {
            throw new NotImplementedException();
        }

        public void GetId([MarshalAs(UnmanagedType.LPWStr)] out IntPtr ppszCoMemTokenId)
        {
            ppszCoMemTokenId = Marshal.StringToCoTaskMemUni(Id);
        }

        public void Slot15() { throw new NotImplementedException(); } // void GetCategory(out ISpObjectTokenCategory ppTokenCategory);
        public void Slot16() { throw new NotImplementedException(); } // void CreateInstance(object pUnkOuter, UInt32 dwClsContext, ref Guid riid, ref IntPtr ppvObject);
        public void Slot17() { throw new NotImplementedException(); } // void GetStorageFileName(ref Guid clsidCaller, [MarshalAs(UnmanagedType.LPWStr)] string pszValueName, [MarshalAs(UnmanagedType.LPWStr)] string pszFileNameSpecifier, UInt32 nFolder, [MarshalAs(UnmanagedType.LPWStr)] out string ppszFilePath);
        public void Slot18() { throw new NotImplementedException(); } // void RemoveStorageFileName(ref Guid clsidCaller, [MarshalAs(UnmanagedType.LPWStr)] string pszKeyName, int fDeleteFile);
        public void Slot19() { throw new NotImplementedException(); } // void Remove(ref Guid pclsidCaller);
        public void Slot20() { throw new NotImplementedException(); } // void IsUISupported([MarshalAs(UnmanagedType.LPWStr)] string pszTypeOfUI, IntPtr pvExtraData, UInt32 cbExtraData, object punkObject, ref Int32 pfSupported);
        public void Slot21() { throw new NotImplementedException(); } // void DisplayUI(UInt32 hWndParent, [MarshalAs(UnmanagedType.LPWStr)] string pszTitle, [MarshalAs(UnmanagedType.LPWStr)] string pszTypeOfUI, IntPtr pvExtraData, UInt32 cbExtraData, object punkObject);
        public void MatchesAttributes([MarshalAs(UnmanagedType.LPWStr)] string pszAttributes, [MarshalAs(UnmanagedType.Bool)] out bool pfMatches) { throw new NotImplementedException(); }

        #endregion

        /// <summary>
        /// Check if the token supports the attributes list given in. The
        /// attributes list has the same format as the required attributes given to
        /// SpEnumTokens.
        /// </summary>
        internal bool MatchesAttributes(string[] sAttributes)
        {
            bool fMatch = true;

            for (int iAttribute = 0; iAttribute < sAttributes.Length; iAttribute++)
            {
                string s = sAttributes[iAttribute];
                fMatch &= HasValue(s) || (Attributes != null && Attributes.HasValue(s));
                if (!fMatch)
                {
                    break;
                }
            }
            return fMatch;
        }

        internal T CreateObjectFromToken<T>(string name)
        {
            T instanceValue = default(T);
            string clsid;

            if (!TryGetString(name, out clsid))
            {
                throw new ArgumentException(SR.Get(SRID.TokenCannotCreateInstance));
            }

            try
            {
                // Application Class Id
                Type type = Type.GetTypeFromCLSID(new Guid(clsid));

                // Create the object instance
                instanceValue = (T)Activator.CreateInstance(type);

                // Initialize the instance
                ISpObjectWithToken objectWithToken = instanceValue as ISpObjectWithToken;
                if (objectWithToken != null)
                {
                    int hresult = objectWithToken.SetObjectToken(this);
                    if (hresult < 0)
                    {
                        throw new ArgumentException(SR.Get(SRID.TokenCannotCreateInstance));
                    }
                }
                else
                {
                    Debug.Fail("Cannot query for interface " + typeof(ISpObjectWithToken).GUID + " from COM class " + clsid);
                }
            }
            catch (Exception e)
            {
                if (e is MissingMethodException || e is TypeLoadException || e is FileLoadException || e is FileNotFoundException || e is MethodAccessException || e is MemberAccessException || e is TargetInvocationException || e is InvalidComObjectException || e is NotSupportedException || e is FormatException)
                {
                    throw new ArgumentException(SR.Get(SRID.TokenCannotCreateInstance));
                }
                throw;
            }
            return instanceValue;
        }

        #endregion

        #region private Methods

        #endregion

        #region Private Types

        //--- ISpObjectWithToken ----------------------------------------------------
        [ComImport, Guid("5B559F40-E952-11D2-BB91-00C04F8EE6C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ISpObjectWithToken
        {
            [PreserveSig]
            int SetObjectToken(ISpObjectToken pToken);
            [PreserveSig]
            int GetObjectToken(IntPtr ppToken);
        }

        #endregion
        #region private Fields

        private ISpObjectToken _sapiObjectToken;

        private bool _disposeSapiObjectToken;

        private RegistryDataKey _attributes;

        #endregion
    }
}
