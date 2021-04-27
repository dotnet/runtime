// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Speech.Internal;
using System.Speech.Internal.SrgsCompiler;
using System.Speech.Recognition.SrgsGrammar;
using System.Text;

#pragma warning disable 56500 // Remove all the catch all statements warnings used by the interop layer

namespace System.Speech.Recognition
{
    // Class for grammars which are to be loaded from SRGS or CFG.
    // In contrast to dictation grammars which inherit from this.
    [DebuggerDisplay("Grammar: {(_uri != null ? \"uri=\" + _uri.ToString () + \" \" : \"\") + \"rule=\" + _ruleName }")]
    public class Grammar
    {
        #region Constructors

#pragma warning disable 6504
#pragma warning disable 6507
        internal Grammar(Uri uri, string ruleName, object[] parameters)
        {
            Helpers.ThrowIfNull(uri, nameof(uri));

            _uri = uri;
            InitialGrammarLoad(ruleName, parameters, false);
        }
        public Grammar(string path)
            : this(path, (string)null, null)
        {
        }
        public Grammar(string path, string ruleName)
            : this(path, ruleName, null)
        {
        }
        public Grammar(string path, string ruleName, object[] parameters)
        {
            try
            {
                _uri = new Uri(path, UriKind.Relative);
            }
            catch (UriFormatException e)
            {
                throw new ArgumentException(SR.Get(SRID.RecognizerGrammarNotFound), nameof(path), e);
            }

            InitialGrammarLoad(ruleName, parameters, false);
        }
        public Grammar(SrgsDocument srgsDocument)
            : this(srgsDocument, null, null, null)
        {
        }
        public Grammar(SrgsDocument srgsDocument, string ruleName)
            : this(srgsDocument, ruleName, null, null)
        {
        }
        public Grammar(SrgsDocument srgsDocument, string ruleName, object[] parameters)
            : this(srgsDocument, ruleName, null, parameters)
        {
        }
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public Grammar(SrgsDocument srgsDocument, string ruleName, Uri baseUri)
            : this(srgsDocument, ruleName, baseUri, null)
        {
        }
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public Grammar(SrgsDocument srgsDocument, string ruleName, Uri baseUri, object[] parameters)
        {
            Helpers.ThrowIfNull(srgsDocument, nameof(srgsDocument));

            _srgsDocument = srgsDocument;
            _isSrgsDocument = srgsDocument != null;
            _baseUri = baseUri;
            InitialGrammarLoad(ruleName, parameters, false);
        }
        public Grammar(Stream stream)
            : this(stream, null, null, null)
        {
        }
        public Grammar(Stream stream, string ruleName)
            : this(stream, ruleName, null, null)
        {
        }
        public Grammar(Stream stream, string ruleName, object[] parameters)
            : this(stream, ruleName, null, parameters)
        {
        }
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public Grammar(Stream stream, string ruleName, Uri baseUri)
            : this(stream, ruleName, baseUri, null)
        {
        }
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public Grammar(Stream stream, string ruleName, Uri baseUri, object[] parameters)
        {
            Helpers.ThrowIfNull(stream, nameof(stream));

            if (!stream.CanRead)
            {
                throw new ArgumentException(SR.Get(SRID.StreamMustBeReadable), nameof(stream));
            }
            _appStream = stream;
            _baseUri = baseUri;
            InitialGrammarLoad(ruleName, parameters, false);
        }

        public Grammar(GrammarBuilder builder)
        {
            Helpers.ThrowIfNull(builder, nameof(builder));

            _grammarBuilder = builder;
            InitialGrammarLoad(null, null, false);
        }

        private Grammar(string onInitParameters, Stream stream, string ruleName)
        {
            _appStream = stream;
            _onInitParameters = onInitParameters;
            InitialGrammarLoad(ruleName, null, true);
        }
        protected Grammar()
        {
        }
        protected void StgInit(object[] parameters)
        {
            _parameters = parameters;
            LoadAndCompileCfgData(false, true);
        }

#pragma warning restore 6504
#pragma warning restore 6507

        #endregion

        #region Public Methods
        public static Grammar LoadLocalizedGrammarFromType(Type type, params object[] onInitParameters)
        {
            Helpers.ThrowIfNull(type, nameof(type));

            if (type == typeof(Grammar) || !type.IsSubclassOf(typeof(Grammar)))
            {
                throw new ArgumentException(SR.Get(SRID.StrongTypedGrammarNotAGrammar), nameof(type));
            }

            Assembly assembly = Assembly.GetAssembly(type);

            foreach (Type typeTarget in assembly.GetTypes())
            {
                string cultureId = null;
                if (typeTarget == type || typeTarget.IsSubclassOf(type))
                {
                    if (typeTarget.GetField("__cultureId") != null)
                    {
                        // Get the association table
                        try
                        {
                            cultureId = (string)typeTarget.InvokeMember("__cultureId", BindingFlags.GetField, null, null, null, null);
                        }
                        catch (Exception e)
                        {
                            if (!(e is System.MissingFieldException))
                            {
                                throw;
                            }
                        }
                        if (Helpers.CompareInvariantCulture(new CultureInfo(int.Parse(cultureId, CultureInfo.InvariantCulture)), CultureInfo.CurrentUICulture))
                        {
                            try
                            {
                                return (Grammar)assembly.CreateInstance(typeTarget.FullName, false, BindingFlags.CreateInstance, null, onInitParameters, null, null);
                            }
                            catch (MissingMemberException)
                            {
                                throw new ArgumentException(SR.Get(SRID.RuleScriptInvalidParameters, typeTarget.Name, typeTarget.Name));
                            }
                        }
                    }
                }
            }
            return null;
        }

        #endregion

        #region public Properties

        // Standard properties to control grammar:

        // Controls whether this grammar is actually included in the recognition. True by default. Can be set at any point.
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                // Note: you can still set or get this property regardless of whether the Grammar is loaded or not.
                // In theory we could throw in certain scenarios but this is probably simplest.
                if (_grammarState != GrammarState.Unloaded && _enabled != value)
                {
                    _recognizer.SetGrammarState(this, value);
                }
                _enabled = value; // Only on success
            }
        }

        // Relative weight of this Grammar/Rule.
        public float Weight
        {
            get { return _weight; }
            set
            {
                if (value < 0.0 || value > 1.0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Get(SRID.GrammarInvalidWeight));
                }
                // Note: you can still set or get this property regardless of whether the Grammar is loaded or not.
                // In theory we could throw in certain scenarios but this is probably simplest.
                if (_grammarState != GrammarState.Unloaded && !_weight.Equals(value))
                {
                    _recognizer.SetGrammarWeight(this, value);
                }
                _weight = value; // Only on success
            }
        }

        // Priority of this Grammar/Rule.
        // If different grammars have paths which match the same words,
        // then the result will be returned for the grammar with the highest priority.
        // Default value zero {lowest value}.
        public int Priority
        {
            get { return _priority; }
            set
            {
                if (value < -128 || value > 127)
                {
                    // We could have used sbyte in the signature of this property but int is probably simpler.
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Get(SRID.GrammarInvalidPriority));
                }
                if (_grammarState != GrammarState.Unloaded && _priority != value)
                {
                    _recognizer.SetGrammarPriority(this, value);
                }
                _priority = value; // Only on success.
            }
        }

        // Simple property that allows a name to be attached to the Grammar.
        // This has no effect but could be convenient for certain apps.
        public string Name
        {
            get { return _grammarName; }
            set
            {
#pragma warning disable 6507
#pragma warning disable 6526
                if (value == null) { value = string.Empty; }
                _grammarName = value;
#pragma warning restore 6507
#pragma warning restore 6526
            }
        }
        public string RuleName
        {
            get { return _ruleName; }
        }
        public bool Loaded
        {
            get { return _grammarState == GrammarState.Loaded; }
        }
        internal Uri Uri
        {
            get { return _uri; }
        }

        #endregion

        #region public Events

        // The event fired upon a recognition.
        public event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;

        #endregion

        #region Internal Properties

        internal IRecognizerInternal Recognizer
        {
            get { return _recognizer; }
            set { _recognizer = value; }
        }

        // The load-state of the grammar.
        // - Set to New by constructor and also kept as New if a synchronous load fails.
        // - Set to Loaded when any grammar load completes.
        // - Set to Unloaded when a grammar is unloaded from the Recognizer.
        // There are two additional states used for async grammar loading:
        // - Set to Loading when an Async load is in progress.
        // - Set to LoadFailed when an async load fails but the grammar is still in the Grammars collection.
        internal GrammarState State
        {
            get { return _grammarState; }
            set
            {
                Debug.Assert(value >= GrammarState.Unloaded && value <= GrammarState.LoadFailed);

                // Check state diagram for State. Possible paths:
                // Unloaded -> Loaded -> Unloaded {LoadGrammar succeeded}.
                // Unloaded {LoadGrammar failed}.
                // Unloaded -> Loading -> Loaded -> Unloaded {LoadGrammarAsync succeeded}.
                // Unloaded -> Loading -> Unloaded {LoadGrammarAsync cancelled}.
                // Unloaded -> Loading -> LoadFailed -> Unloaded {LoadGrammarAsync failed}.
                Debug.Assert((_grammarState == GrammarState.Unloaded && (value == GrammarState.Unloaded || value == GrammarState.Loading || value == GrammarState.Loaded)) ||
                    (_grammarState == GrammarState.Loading && (value == GrammarState.LoadFailed || value == GrammarState.Loaded || value == GrammarState.Unloaded)) ||
                    (_grammarState == GrammarState.Loaded && value == GrammarState.Unloaded) ||
                    (_grammarState == GrammarState.LoadFailed && value == GrammarState.Unloaded)
                    );

                // If we are unloaded also reset these parameters.
                if (value == GrammarState.Unloaded)
                {
                    // Remove references to these objects so they can be garbage collected.
                    _loadException = null;
                    _recognizer = null;

                    // Don't reset _uri and _ruleName - allows re-use.
                    // Don't reset _internalData - leave this to the recognizer.

                    // Note: After a Grammar is unloaded you can still get and set the Weight, Enabled etc.
                }
                else if (value == GrammarState.Loaded || value == GrammarState.LoadFailed)
                {
                    Debug.Assert(_recognizer != null); // Must be set before changing state.

                    // Don't update any properties - the recognizer owns pulling this data from the Grammar.
                }

                _grammarState = value; // On success
            }
        }

        internal Exception LoadException
        {
            get { return _loadException; }
            set { _loadException = value; }
        }

        // There properties are read-only:

        internal byte[] CfgData
        {
            get { return _cfgData; }
        }

        internal Uri BaseUri
        {
            get { return _baseUri; }
        }

        internal bool Sapi53Only
        {
            get { return _sapi53Only; }
        }

        internal uint SapiGrammarId
        {
            get { return _sapiGrammarId; }
            set { _sapiGrammarId = value; }
        }

        /// <summary>
        /// Is the grammar a strongly typed grammar?
        /// </summary>
        protected internal virtual bool IsStg
        {
            get { return _isStg; }
        }

        /// <summary>
        /// Is the grammar built from an srgs document?
        /// </summary>
        internal bool IsSrgsDocument
        {
            get { return _isSrgsDocument; }
        }

        // Arbitrary data that is attached and removed by the RecognizerBase.
        // This allow RecognizerBase.Grammars to be a simple list without the extra data being stored separately.
        internal InternalGrammarData InternalData
        {
            get { return _internalData; }
            set { _internalData = value; }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Called by the grammar resource loader to load ruleref. Ruleref have a name, a rule name et eventually
        /// parameters.
        ///
        /// The grammar name can be either pointing to a CFG, an Srgs or DLL (stand alone or GAC).
        /// </summary>
        internal static Grammar Create(string grammarName, string ruleName, string onInitParameter, out Uri redirectUri)
        {
            redirectUri = null;

            // Look for tell-tell sign that it is an assembly
            grammarName = grammarName.Trim();

            // Get an Uri for the grammar. Could fail for GACed values.
            Uri uriGrammar;
            bool hasUri = Uri.TryCreate(grammarName, UriKind.Absolute, out uriGrammar);

            int posDll = grammarName.IndexOf(".dll", StringComparison.OrdinalIgnoreCase);
            if (!hasUri || (posDll > 0 && posDll == grammarName.Length - 4))
            {
                Assembly assembly;
                if (hasUri)
                {
                    // regular dll, should use LoadFrom ()
                    if (uriGrammar.IsFile)
                    {
                        assembly = Assembly.LoadFrom(uriGrammar.LocalPath);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                else
                {
                    // Dll in the GAC use Load ()
                    assembly = Assembly.Load(grammarName);
                }
                return LoadGrammarFromAssembly(assembly, ruleName, onInitParameter);
            }

            try
            {
                // Standard Srgs or CFG, just create the grammar
                string localPath;
                using (Stream stream = s_resourceLoader.LoadFile(uriGrammar, out localPath, out redirectUri))
                {
                    try
                    {
                        return new Grammar(onInitParameter, stream, ruleName);
                    }
                    finally
                    {
                        s_resourceLoader.UnloadFile(localPath);
                    }
                }
            }
            catch
            {
                // It was not a CFG or an Srgs, try again as dll
                Assembly assembly = Assembly.LoadFrom(grammarName);
                return LoadGrammarFromAssembly(assembly, ruleName, onInitParameter);
            }
        }

        // Method called from the recognizer when a recognition has occurred.
        // Only called for SpeechRecognition events, not SpeechRecognitionRejected.
        internal void OnRecognitionInternal(SpeechRecognizedEventArgs eventArgs)
        {
            Debug.Assert(eventArgs.Result.Grammar == this);

            EventHandler<SpeechRecognizedEventArgs> recognitionHandler = SpeechRecognized;
            if (recognitionHandler != null)
            {
                recognitionHandler(this, eventArgs);
            }
        }

        // Helper method used to indicate if this grammar has a dictation Uri or not.
        // This is here because the functionality needs to be a common place.
        internal static bool IsDictationGrammar(Uri uri)
        {
            // Note that must check IsAbsoluteUri before Scheme because Uri.Scheme may throw on a relative Uri
            if (uri == null || !uri.IsAbsoluteUri || uri.Scheme != "grammar" ||
                !string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.Authority) ||
                !string.IsNullOrEmpty(uri.Query) || uri.PathAndQuery != "dictation")
            {
                return false;
            }
            return true;
        }

        // Helper method used to indicate if this grammar has a dictation Uri or not.
        // This is here because the functionality needs to be a common place.
        internal bool IsDictation(Uri uri)
        {
            bool isDictationGrammar = IsDictationGrammar(uri);

            // Note that must check IsAbsoluteUri before Scheme because Uri.Scheme may throw on a relative Uri
            if (!isDictationGrammar && this is DictationGrammar)
            {
                throw new ArgumentException(SR.Get(SRID.DictationInvalidTopic), nameof(uri));
            }
            return isDictationGrammar;
        }

        /// <summary>
        /// Find a grammar in a tree or rule refs grammar from the SAPI grammar Id
        /// </summary>
        /// <param name="grammarId">SAPI id</param>
        /// <returns>null if not found</returns>
        internal Grammar Find(long grammarId)
        {
            if (_ruleRefs != null)
            {
                foreach (Grammar ruleRef in _ruleRefs)
                {
                    Grammar found;

                    if (grammarId == ruleRef._sapiGrammarId)
                    {
                        return ruleRef;
                    }
                    if ((found = ruleRef.Find(grammarId)) != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Find a grammar in a tree or rule refs grammar from a rule name
        /// </summary>
        /// <returns>null if not found</returns>
        internal Grammar Find(string ruleName)
        {
            if (_ruleRefs != null)
            {
                foreach (Grammar ruleRef in _ruleRefs)
                {
                    Grammar found;

                    if (ruleName == ruleRef.RuleName)
                    {
                        return ruleRef;
                    }
                    if ((found = ruleRef.Find(ruleName)) != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Add a rule ref grammar to a grammar.
        /// </summary>
        internal void AddRuleRef(Grammar ruleRef, uint grammarId)
        {
            if (_ruleRefs == null)
            {
                _ruleRefs = new Collection<Grammar>();
            }
            _ruleRefs.Add(ruleRef);
            _sapiGrammarId = grammarId;
        }

        internal MethodInfo MethodInfo(string method)
        {
            return GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        #endregion

        #region Internal Fields

        internal GrammarOptions _semanticTag;

        internal System.Speech.Internal.SrgsCompiler.AppDomainGrammarProxy _proxy;

        internal ScriptRef[] _scripts;

        #endregion

        #region Protected Methods
        protected string ResourceName
        {
            get
            {
                return _resources;
            }
            set
            {
                Helpers.ThrowIfEmptyOrNull(value, nameof(value));
                _resources = value;
            }
        }

        #endregion

        #region Private Methods

        // Called to initialize the grammar from the passed in data.
        // In SpeechFX this is called at construction time.
        // In MSS this is {currently} called when GetCfg is called.
        // The cfg data is stored in the _cfgData field, which is not currently reset to null ever.
        // After calling this method the passed in Stream / SrgsDocument are set to null.
        private void LoadAndCompileCfgData(bool isImportedGrammar, bool stgInit)
        {
#if DEBUG
            Debug.Assert(!_loaded);
            _loaded = true;
#endif

            // If strongly typed grammar, load the cfg from the resources otherwise load the IL from within the CFG
            Stream stream = IsStg ? LoadCfgFromResource(stgInit) : LoadCfg(isImportedGrammar, stgInit);

            // Check if the grammar needs to be rebuilt
            SrgsRule[] extraRules = RunOnInit(IsStg); // list of extra rule to append to the current CFG
            if (extraRules != null)
            {
                MemoryStream streamCombined = CombineCfg(_ruleName, stream, extraRules);

                // Release the old stream since a new one contains the CFG
                stream.Close();
                stream = streamCombined;
            }
            // Note LoadCfg, LoadCfgFromResource and CombineCfg all reset Stream position to zero.

            _cfgData = Helpers.ReadStreamToByteArray(stream, (int)stream.Length);
            stream.Close();

            // Reset these - no longer needed
            _srgsDocument = null;
            _appStream = null;
        }

        /// <summary>
        /// Returns a stream object for a grammar.
        /// </summary>
        private MemoryStream LoadCfg(bool isImportedGrammar, bool stgInit)
        {
            // No parameters to the constructors
            Uri uriGrammar = Uri;
            MemoryStream stream = new();

            if (uriGrammar != null)
            {
                throw new PlatformNotSupportedException();
            }
            else if (_srgsDocument != null)
            {
                // If srgs, compile to a stream
                SrgsGrammarCompiler.Compile(_srgsDocument, stream);
                if (_baseUri == null && _srgsDocument.BaseUri != null)
                {
                    // If we loaded the SrgsDocument from a file then that should be used as the base path.
                    // But it should not override any baseUri supplied directly to the Grammar constructor or in the xmlBase attribute in the xml.
                    _baseUri = _srgsDocument.BaseUri;

                    // So the priority order for getting the base path is:
                    // 1. The xml:base attribute in the xml.
                    // 2. The baseUri passed to the Grammar constructor.
                    // 3. The path the xml was originally loaded from.
                }
            }
            else if (_grammarBuilder != null)
            {
                // If GrammarBuilder, compile to a stream
                _grammarBuilder.Compile(stream);
            }
            else
            {
                // If stream, load
                SrgsGrammarCompiler.CompileXmlOrCopyCfg(_appStream, stream, null);
            }

            stream.Position = 0;

            // Update the rule name
            _ruleName = CheckRuleName(stream, _ruleName, isImportedGrammar, stgInit, out _sapi53Only, out _semanticTag);

            // Create an app domain for the grammar code if any
            CreateSandbox(stream);

            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Look for a grammar by rule name in a loaded assembly.
        ///
        /// The search goes over the base type for the grammar "rule name" and all of its derived language
        /// dependent classes.
        /// The matching algorithm pick a class that match the culture.
        /// </summary>
        private static Grammar LoadGrammarFromAssembly(Assembly assembly, string ruleName, string onInitParameters)
        {
            Type grammarType = typeof(Grammar);
            Type matchingType = null;

            foreach (Type typeTarget in assembly.GetTypes())
            {
                // must be a grammar object
                if (typeTarget.IsSubclassOf(grammarType))
                {
                    string cultureId = null;

                    // Set the base class for this rule
                    if (typeTarget.Name == ruleName)
                    {
                        matchingType = typeTarget;
                    }

                    // Pick a class that derives from rulename
                    if (typeTarget == matchingType || (matchingType != null && typeTarget.IsSubclassOf(matchingType)))
                    {
                        // Check if the language match
                        if (typeTarget.GetField("__cultureId") != null)
                        {
                            // Get the association table
                            try
                            {
                                cultureId = (string)typeTarget.InvokeMember("__cultureId", BindingFlags.GetField, null, null, null, null);
                            }
                            catch (Exception e)
                            {
                                if (!(e is System.MissingFieldException))
                                {
                                    throw;
                                }
                            }

                            // Check for the current culture or any compatible culture (parent en-us or en for e.g.)
                            if (Helpers.CompareInvariantCulture(new CultureInfo(int.Parse(cultureId, CultureInfo.InvariantCulture)), CultureInfo.CurrentUICulture))
                            {
                                try
                                {
                                    object[] initParams = MatchInitParameters(typeTarget, onInitParameters, assembly.GetName().Name, ruleName);

                                    // The CLR does the match for the right constructor based on the onInitParameters types
                                    return (Grammar)assembly.CreateInstance(typeTarget.FullName, false, BindingFlags.CreateInstance, null, initParams, null, null);
                                }
                                catch (MissingMemberException)
                                {
                                    throw new ArgumentException(SR.Get(SRID.RuleScriptInvalidParameters, typeTarget.Name, typeTarget.Name));
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Construct a list of parameters from a sapi:params string.
        /// </summary>
        private static object[] MatchInitParameters(Type type, string onInitParameters, string grammar, string rule)
        {
            ConstructorInfo[] cis = type.GetConstructors();
            NameValuePair[] pairs = ParseInitParams(onInitParameters);
            object[] values = new object[pairs.Length];
            bool foundConstructor = false;
            for (int iCtor = 0; iCtor < cis.Length && !foundConstructor; iCtor++)
            {
                ParameterInfo[] paramInfo = cis[iCtor].GetParameters();

                // Check if enough parameters are provided.
                if (paramInfo.Length > pairs.Length)
                {
                    continue;
                }
                foundConstructor = true;
                for (int i = 0; i < pairs.Length && foundConstructor; i++)
                {
                    NameValuePair pair = pairs[i];

                    // anonymous
                    if (pair._name == null)
                    {
                        values[i] = pair._value;
                    }
                    else
                    {
                        bool foundParameter = false;
                        for (int j = 0; j < paramInfo.Length; j++)
                        {
                            if (paramInfo[j].Name == pair._name)
                            {
                                values[j] = ParseValue(paramInfo[j].ParameterType, pair._value);
                                foundParameter = true;
                                break;
                            }
                        }
                        if (!foundParameter)
                        {
                            foundConstructor = false;
                        }
                    }
                }
            }
            if (!foundConstructor)
            {
                throw new FormatException(SR.Get(SRID.CantFindAConstructor, grammar, rule, FormatConstructorParameters(cis)));
            }
            return values;
        }

        /// <summary>
        /// Parse the value for a type from a string to a strong type.
        /// If the type does not support the Parse method then the operation fails.
        /// </summary>
        private static object ParseValue(Type type, string value)
        {
            if (type == typeof(string))
            {
                return value;
            }
            return type.InvokeMember("Parse", BindingFlags.InvokeMethod, null, null, new object[] { value }, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns the list of the possible parameter names and type for a grammar
        /// </summary>
        private static string FormatConstructorParameters(ConstructorInfo[] cis)
        {
            StringBuilder sb = new();
            for (int iCtor = 0; iCtor < cis.Length; iCtor++)
            {
                sb.Append(iCtor > 0 ? " or sapi:parms=\"" : "sapi:parms=\"");
                ParameterInfo[] pis = cis[iCtor].GetParameters();
                for (int i = 0; i < pis.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(';');
                    }
                    ParameterInfo pi = pis[i];
                    sb.Append(pi.Name);
                    sb.Append(':');
                    sb.Append(pi.ParameterType.Name);
                }
                sb.Append('"');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Split the init parameter strings into an array of name/values
        /// The format must be "name:value". If the ':' then parameter is anonymous.
        /// </summary>
        private static NameValuePair[] ParseInitParams(string initParameters)
        {
            if (string.IsNullOrEmpty(initParameters))
            {
                return Array.Empty<NameValuePair>();
            }

            string[] parameters = initParameters.Split(new char[] { ';' }, StringSplitOptions.None);
            NameValuePair[] pairs = new NameValuePair[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                string parameter = parameters[i];
                int posColon = parameter.IndexOf(':');
                if (posColon >= 0)
                {
                    pairs[i]._name = parameter.Substring(0, posColon);
                    pairs[i]._value = parameter.Substring(posColon + 1);
                }
                else
                {
                    pairs[i]._value = parameter;
                }
            }
            return pairs;
        }

        private void InitialGrammarLoad(string ruleName, object[] parameters, bool isImportedGrammar)
        {
            _ruleName = ruleName;
            _parameters = parameters;

            // Bail out if it is a dictation grammar
            if (!IsDictation(_uri))
            {
                LoadAndCompileCfgData(isImportedGrammar, false);
            }
        }

        private void CreateSandbox(MemoryStream stream)
        {
            // Checks if it contains .NET Semantic code
            byte[] assemblyContent;
            byte[] assemblyDebugSymbols;
            ScriptRef[] scripts;
            stream.Position = 0;

            // This must be before the SAPI load to avoid some conflict with SAPI server when getting at the
            // the stream
            if (System.Speech.Internal.SrgsCompiler.CfgGrammar.LoadIL(stream, out assemblyContent, out assemblyDebugSymbols, out scripts))
            {
                // Check all methods referenced in the rule; availability, public and arguments
                Assembly executingAssembly = Assembly.GetExecutingAssembly();
                _proxy = new AppDomainGrammarProxy();
                _proxy.Init(_ruleName, assemblyContent, assemblyDebugSymbols);
                _scripts = scripts;
            }
        }

        // Loads a strongly typed grammar from a resource in the Assembly.
        private Stream LoadCfgFromResource(bool stgInit)
        {
            // Strongly typed grammar get the Cfg data
            Assembly assembly = Assembly.GetAssembly(GetType());

            Stream stream = assembly.GetManifestResourceStream(ResourceName);

            if (stream == null)
            {
                throw new FormatException(SR.Get(SRID.RecognizerInvalidBinaryGrammar));
            }
            try
            {
                ScriptRef[] scripts = CfgGrammar.LoadIL(stream);
                if (scripts == null)
                {
                    throw new ArgumentException(SR.Get(SRID.CannotLoadDotNetSemanticCode));
                }
                _scripts = scripts;
            }
            catch (Exception e)
            {
                throw new ArgumentException(SR.Get(SRID.CannotLoadDotNetSemanticCode), e);
            }
            stream.Position = 0;

            // Update the rule name
            _ruleName = CheckRuleName(stream, GetType().Name, false, stgInit, out _sapi53Only, out _semanticTag);

            _isStg = true;
            return stream;
        }

        private static MemoryStream CombineCfg(string rule, Stream stream, SrgsRule[] extraRules)
        {
            using (MemoryStream streamExtra = new())
            {
                // Create an SrgsDocument from the set of rules
                SrgsDocument sgrsDocument = new();
                sgrsDocument.TagFormat = SrgsTagFormat.KeyValuePairs;
                foreach (SrgsRule srgsRule in extraRules)
                {
                    sgrsDocument.Rules.Add(srgsRule);
                }

                SrgsGrammarCompiler.Compile(sgrsDocument, streamExtra);

                using (StreamMarshaler streamMarshaler = new(stream))
                {
                    long endSeekPosition = stream.Position;
                    Backend backend = new(streamMarshaler);
                    stream.Position = endSeekPosition;

                    streamExtra.Position = 0;
                    MemoryStream streamCombined = new();
                    using (StreamMarshaler streamExtraMarshaler = new(streamExtra))
                    {
                        Backend extra = new(streamExtraMarshaler);
                        Backend combined = Backend.CombineGrammar(rule, backend, extra);

                        using (StreamMarshaler streamCombinedMarshaler = new(streamCombined))
                        {
                            combined.Commit(streamCombinedMarshaler);
                            streamCombined.Position = 0;
                            return streamCombined;
                        }
                    }
                }
            }
        }

#pragma warning disable 56507 // check for null or empty strings

        private SrgsRule[] RunOnInit(bool stg)
        {
            SrgsRule[] extraRules = null;
            bool onInitInvoked = false;

            // Get the name of the onInit method to run
            string methodName = ScriptRef.OnInitMethod(_scripts, _ruleName);

            if (methodName != null)
            {
                if (_proxy != null)
                {
                    Exception appDomainException;
                    extraRules = _proxy.OnInit(methodName, _parameters, _onInitParameters, out appDomainException);
                    onInitInvoked = true;
                    if (appDomainException != null)
                    {
                        ExceptionDispatchInfo.Throw(appDomainException);
                    }
                }
                else
                {
                    // call OnInit if any - should be based on Rule
                    Type[] types = new Type[_parameters.Length];

                    for (int i = 0; i < _parameters.Length; i++)
                    {
                        types[i] = _parameters[i].GetType();
                    }
                    MethodInfo onInit = GetType().GetMethod(methodName, types);

                    // If somehow we failed to find a constructor, let the system handle it
                    if (onInit != null)
                    {
                        System.Diagnostics.Debug.Assert(_parameters != null);
                        extraRules = (SrgsRule[])onInit.Invoke(this, _parameters);
                        onInitInvoked = true;
                    }
                    else
                    {
                        throw new ArgumentException(SR.Get(SRID.RuleScriptInvalidParameters, _ruleName, _ruleName));
                    }
                }
            }

            // Cannot have onInit parameters if onInit has not been invoked.
            if (!stg && !onInitInvoked && _parameters != null)
            {
                throw new ArgumentException(SR.Get(SRID.RuleScriptInvalidParameters, _ruleName, _ruleName));
            }
            return extraRules;
        }

        // Pulls the required data out of a stream containing a cfg.
        // Stream must point to start of cfg on entry and is reset to same point on exit.
        private static string CheckRuleName(Stream stream, string rulename, bool isImportedGrammar, bool stgInit, out bool sapi53Only, out GrammarOptions grammarOptions)
        {
            sapi53Only = false;
            long initialPosition = stream.Position;

            CfgGrammar.CfgHeader header;
            using (StreamMarshaler streamHelper = new(stream)) // Use StreamMarshaler which helps deserialize certain data types
            {
                CfgGrammar.CfgSerializedHeader serializedHeader = null;
                header = CfgGrammar.ConvertCfgHeader(streamHelper, false, true, out serializedHeader);

                StringBlob symbols = header.pszSymbols;

                // Calc the root rule
                string rootRule = header.ulRootRuleIndex != 0xffffffff && header.ulRootRuleIndex < header.rules.Length ? symbols.FromOffset(header.rules[header.ulRootRuleIndex]._nameOffset) : null;

                // Get if we have semantic interpretation
                sapi53Only = (header.GrammarOptions & (GrammarOptions.MssV1 | GrammarOptions.W3cV1 | GrammarOptions.STG | GrammarOptions.IpaPhoneme)) != 0;

                // Check that the rule name is valid
                if (rootRule == null && string.IsNullOrEmpty(rulename))
                {
                    throw new ArgumentException(SR.Get(SRID.SapiErrorNoRulesToActivate));
                }

                if (!string.IsNullOrEmpty(rulename))
                {
                    // Convert the CFG script reference to ScriptRef
                    bool fFoundRule = false;
                    foreach (CfgRule cfgRule in header.rules)
                    {
                        if (symbols.FromOffset(cfgRule._nameOffset) == rulename)
                        {
                            // Private rule are not allowed
                            fFoundRule = cfgRule.Export || stgInit || (!isImportedGrammar ? cfgRule.TopLevel || rulename == rootRule : false);
                            break;
                        }
                    }

                    // check that the name exists
                    if (!fFoundRule)
                    {
                        throw new ArgumentException(SR.Get(SRID.RecognizerRuleNotFoundStream, rulename));
                    }
                }
                else
                {
                    rulename = rootRule;
                }

                grammarOptions = header.GrammarOptions & GrammarOptions.TagFormat;
            }
            stream.Position = initialPosition;
            return rulename;
        }

        #endregion

        #region Private Fields

#pragma warning disable 56524 // You cannot dispose an object we don't create

        private byte[] _cfgData;

        private Stream _appStream;
        private bool _isSrgsDocument;
        private SrgsDocument _srgsDocument;

        private GrammarBuilder _grammarBuilder;

#pragma warning restore 56524

        private IRecognizerInternal _recognizer;
        private GrammarState _grammarState;
        private Exception _loadException;
        private Uri _uri;
        private Uri _baseUri;
        private string _ruleName;
        private string _resources;
        private object[] _parameters;
        private string _onInitParameters;
        private bool _enabled = true;
        private bool _isStg;
        private bool _sapi53Only;
        private uint _sapiGrammarId;
        private float _weight = 1.0f;
        private int _priority;
        private InternalGrammarData _internalData;
        private string _grammarName = string.Empty;
        private Collection<Grammar> _ruleRefs;
        private static ResourceLoader s_resourceLoader = new();

#if DEBUG
        private bool _loaded;
#endif

        #endregion

        #region Private Types

        private struct NameValuePair
        {
            internal string _name;
            internal string _value;
        }

        #endregion
    }

    // Grammar load-state. Not public.
    internal enum GrammarState
    {
        Unloaded,
        Loading,
        Loaded,
        LoadFailed,
    }
}
