// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.CSharp;
using Microsoft.VisualBasic;

namespace System.Management
{
    /// <summary>
    ///     <para>Defines the languages supported by the code generator.</para>
    /// </summary>
    public enum CodeLanguage
    {
        /// <summary>
        ///    A value for generating C# code.
        /// </summary>
        CSharp,
        /// <summary>
        ///    <para>A value for generating JScript code.</para>
        /// </summary>
        JScript,
        /// <summary>
        ///    <para>A value for generating Visual Basic code.</para>
        /// </summary>
        VB,
        /// <summary>
        ///    <para>A value for generating Visual J# code.</para>
        /// </summary>
        VJSharp,
        /// <summary>
        ///    <para>A value for generating Managed C++ code.</para>
        /// </summary>
        Mcpp
    };

    /// <summary>
    ///    Used to generate a strongly-typed code class for a given WMI class.
    /// </summary>
    internal sealed class ManagementClassGenerator
    {
        private readonly string VSVERSION = "8.0.0.0";
        private string OriginalServer = string.Empty;
        private string OriginalNamespace = string.Empty;
        private string OriginalClassName = string.Empty;
        private readonly string OriginalPath = string.Empty;
        private bool bSingletonClass;
        private bool bUnsignedSupported = true;
        private string NETNamespace = string.Empty;
        private string arrConvFuncName = string.Empty;
        private string enumType = string.Empty;
        private const int DMTF_DATETIME_STR_LENGTH = 25;
        private bool bDateConversionFunctionsAdded;
        private bool bTimeSpanConversionFunctionsAdded;




        private ManagementClass classobj;
        private CodeDomProvider cp;
        private StreamWriter tw;
        private readonly string genFileName = string.Empty;
        private CodeTypeDeclaration cc;
        private CodeTypeDeclaration ccc;
        private CodeTypeDeclaration ecc;
        private CodeTypeDeclaration EnumObj;
        private CodeNamespace cn;
        private CodeMemberProperty cmp;
        private CodeConstructor cctor;
        private CodeMemberField cf;
        private CodeObjectCreateExpression coce;
        private CodeParameterDeclarationExpression cpde;
        private CodeIndexerExpression cie;
        private CodeMemberField cmf;
        private CodeMemberMethod cmm;
        private CodePropertyReferenceExpression cpre;
        private CodeMethodInvokeExpression cmie;
        private CodeExpressionStatement cmis;
        private CodeConditionStatement cis;
        private CodeBinaryOperatorExpression cboe;
        private CodeIterationStatement cfls;
        private CodeAttributeArgument caa;
        private CodeAttributeDeclaration cad;


        private readonly ArrayList arrKeyType = new ArrayList(5);
        private readonly ArrayList arrKeys = new ArrayList(5);
        private readonly ArrayList BitMap = new ArrayList(5);
        private ArrayList BitValues = new ArrayList(5);
        private readonly ArrayList ValueMap = new ArrayList(5);
        private ArrayList Values = new ArrayList(5);

        private SortedList PublicProperties = new SortedList(StringComparer.OrdinalIgnoreCase);
        private SortedList PublicMethods = new SortedList(StringComparer.OrdinalIgnoreCase);
        private SortedList PublicNamesUsed = new SortedList(StringComparer.OrdinalIgnoreCase);
        private SortedList PrivateNamesUsed = new SortedList(StringComparer.OrdinalIgnoreCase);

        private bool bHasEmbeddedProperties;


        /// <summary>
        ///    <para>Creates an empty generator object. This is the default constructor.</para>
        /// </summary>
        public ManagementClassGenerator()
        {
        }

        /// <summary>
        ///    <para>Creates a generator object and initializes it
        ///     with the specified <see cref="System.Management.ManagementClass"/>.</para>
        /// </summary>
        /// <param name='cls'><see cref="System.Management.ManagementClass"/> object for which the code is to be generated.</param>
        public ManagementClassGenerator(ManagementClass cls)
        {
            this.classobj = cls;
        }

        /// <summary>
        ///    <para>
        ///       Returns a <see cref="System.CodeDom.CodeTypeDeclaration"/> for
        ///       this class.</para>
        /// </summary>
        /// <param name='includeSystemProperties'>Indicates if a class for handling system properties should be included.</param>
        /// <param name='systemPropertyClass'>Indicates if the generated code is for a class that handles system properties.</param>
        /// <returns>
        ///    <para>Returns the <see cref="System.CodeDom.CodeTypeDeclaration"/> for the WMI class.</para>
        /// </returns>
        /// <remarks>
        ///    <para>If includeSystemProperties is <see langword="true"/>,
        ///       the ManagementSystemProperties class is included in the generated class definition.
        ///       This parameter is ignored if systemPropertyClass is <see langword="true"/>.</para>
        /// </remarks>
        public CodeTypeDeclaration GenerateCode(bool includeSystemProperties, bool systemPropertyClass)
        {
            CodeTypeDeclaration retType;

            if (systemPropertyClass)
            {
                //Initialize the public attributes . private variables
                InitilializePublicPrivateMembers();
                retType = GenerateSystemPropertiesClass();
            }
            else
            {
                CheckIfClassIsProperlyInitialized();
                InitializeCodeGeneration();
                retType = GetCodeTypeDeclarationForClass(includeSystemProperties);
            }

            return retType;
        }

        /// <summary>
        /// Generates a strongly-typed code class for the specified language provider (C#, Visual Basic or JScript)
        /// and writes it to the specified file.
        /// </summary>
        /// <param name="lang">The language to generate in.</param>
        /// <param name="filePath">The path to the file where the generated code should be stored.</param>
        /// <param name="netNamespace">The .NET namespace into which the class is generated.</param>
        public bool GenerateCode(CodeLanguage lang, string filePath, string netNamespace)
        {
            // check for proper arguments
            if (filePath == null)
            {
                throw new ArgumentOutOfRangeException(SR.NullFilePathException);
            }

            if (filePath.Length == 0)
            {
                throw new ArgumentOutOfRangeException(SR.EmptyFilePathException);
            }

            NETNamespace = netNamespace;
            CheckIfClassIsProperlyInitialized();
            // Initialize Code Generator
            InitializeCodeGeneration();

            //Now create the filestream (output file)
            tw = new StreamWriter(new FileStream(filePath, FileMode.Create), System.Text.Encoding.UTF8);

            return GenerateAndWriteCode(lang);

        }

        /// <summary>
        /// Checks if mandatory properties are properly initialized.
        /// </summary>
        private void CheckIfClassIsProperlyInitialized()
        {
            if (classobj == null)
            {
                if (OriginalNamespace == null || (OriginalNamespace != null && OriginalNamespace.Length == 0))
                {
                    throw new ArgumentOutOfRangeException(SR.NamespaceNotInitializedException);
                }

                if (OriginalClassName == null || (OriginalClassName != null && OriginalClassName.Length == 0))
                {
                    throw new ArgumentOutOfRangeException(SR.ClassNameNotInitializedException);
                }
            }
        }
        private void InitializeCodeGeneration()
        {

            //First try to get the class object for the given WMI Class.
            //If we cannot get it then there is no point in continuing
            //as we won't have any information for the code generation.
            InitializeClassObject();

            //Initialize the public attributes . private variables
            InitilializePublicPrivateMembers();

            //First form the namespace for the generated class.
            //The namespace will look like System.Wmi.Root.Cimv2.Win32
            //for the path \\root\cimv2:Win32_Service and the class name will be
            //Service.
            ProcessNamespaceAndClassName();

            //First we will sort out the different naming collision that might occur
            //in the generated code.
            ProcessNamingCollisions();
        }

        /// <summary>
        /// This function will generate the code. This is the function which
        /// should be called for generating the code.
        /// </summary>
        /// <param name="bIncludeSystemClassinClassDef">
        /// Flag to indicate if system properties are to be included or not
        /// </param>
        private CodeTypeDeclaration GetCodeTypeDeclarationForClass(bool bIncludeSystemClassinClassDef)
        {
            //Create type defination for the class
            cc = new CodeTypeDeclaration(PrivateNamesUsed["GeneratedClassName"].ToString());
            // Adding Component as base class so as to enable drag and drop
            cc.BaseTypes.Add(new CodeTypeReference(PrivateNamesUsed["ComponentClass"].ToString()));


            AddClassComments(cc);
            //Generate the code for defaultNamespace
            //public string defNamespace {
            //    get {
            //            return (<defNamespace>);
            //        }
            //}
            GeneratePublicReadOnlyProperty(PublicNamesUsed["NamespaceProperty"].ToString(), "System.String",
                OriginalNamespace, false, true, SR.CommentOriginNamespace);

            /*
                        Generate the following code for className
                        [Browsable(true)]
                        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
                        public string ManagementClassName {
                            get {
                                string strRet = CreatedClassName;
                                if (((PrivateLateBoundObject != null)
                                            && (PrivateLateBoundObject.Path != null))) {
                                    strRet = ((string)(PrivateLateBoundObject["__CLASS"]));
                                    if (((strRet == null)
                                                || (strRet.Length == 0))) {
                                        strRet = CreatedClassName;
                                    }
                                }
                                return strRet;
                            }
                        }
                */


            // Add a private member variable to hold the namespace of the created class
            // Generate a private member variable for storing the generated class Name
            GeneratePrivateMember(PrivateNamesUsed["CreationWmiNamespace"].ToString(), "System.String",
                new CodePrimitiveExpression(OriginalNamespace), true, SR.CommentCreatedWmiNamespace);

            GenerateClassNameProperty();

            // Generate a private member variable for storing the generated class Name
            GeneratePrivateMember(PrivateNamesUsed["CreationClassName"].ToString(), "System.String",
                new CodePrimitiveExpression(OriginalClassName), true, SR.CommentCreatedClass);

            //public SystemPropertiesClass _SystemProps{
            //    get {
            //            return (privSysProps);
            //        }
            //}
            GeneratePublicReadOnlyProperty(PublicNamesUsed["SystemPropertiesProperty"].ToString(), PublicNamesUsed["SystemPropertiesClass"].ToString(),
                PrivateNamesUsed["SystemPropertiesObject"].ToString(), true, true,
                SR.CommentSystemObject);

            //public wmiObjectClass _Object{
            //    get {
            //            return (privWmiObject);
            //        }
            //}
            GeneratePublicReadOnlyProperty(PublicNamesUsed["LateBoundObjectProperty"].ToString(), PublicNamesUsed["BaseObjClass"].ToString(),
                PrivateNamesUsed["CurrentObject"].ToString(), true, false,
                SR.CommentLateBoundProperty);

            //public ManagementScope Scope {
            //    get {
            //            return privScope;
            //        }
            //    set {
            //            privScope = value;
            //        }
            //}

            GenerateScopeProperty();

            //public bool AutoCommit {
            //    get {
            //            return AutoCommitProp;
            //        }
            //    set {
            //            AutoCommitProp; = value;
            //        }
            //}

            GeneratePublicProperty(PublicNamesUsed["AutoCommitProperty"].ToString(), "System.Boolean",
                new CodeSnippetExpression(PrivateNamesUsed["AutoCommitProperty"].ToString()), false,
                SR.CommentAutoCommitProperty, false);

            //public ManagementPath Path {
            //    get {
            //            return privWmiObject.Path;
            //        }
            //    set {
            //            if (String.Compare(value.ClassName,className,true,CultureInfo.InvariantCulture) != 0)
            //                throw new ArgumentException("Class name doesn\'t match.");
            //            privWmiObject.Path = value;
            //        }
            //}
            GeneratePathProperty();

            // Adding a private member for storing the ManagementScope to be used by various static methods
            GeneratePrivateMember(PrivateNamesUsed["statMgmtScope"].ToString(), PublicNamesUsed["ScopeClass"].ToString(),
                new CodePrimitiveExpression(null), true, SR.CommentStaticManagementScope);

            // Generate a property "StaticScope" to set and get the static ManagementScope for the class
            GeneratePublicProperty(PrivateNamesUsed["staticScope"].ToString(), PublicNamesUsed["ScopeClass"].ToString(),
                new CodeVariableReferenceExpression(PrivateNamesUsed["statMgmtScope"].ToString()), true, SR.CommentStaticScopeProperty, true);

            // Generate a function to check if a given class can be represented
            // by the generated class
            GenerateIfClassvalidFunction();

            //Now generate properties of the WMI Class
            GenerateProperties();

            // Add Function to Initialize Member variables
            GenerateMethodToInitializeVariables();

            //Now Generate static ConstructPath()
            GenerateConstructPath();

            //Now create the default constructor
            GenerateDefaultConstructor();

            GenerateInitializeObject();
            if (bSingletonClass)
            {
                //Now Generate a constructor which accepts only the scope
                GenerateConstructorWithScope();

                //Now Generate a constructor which accepts only the get options
                GenerateConstructorWithOptions();

                //Now generate a constructor which accepts both scope and options
                GenerateConstructorWithScopeOptions();
            }
            else
            {
                //Now create the constructor which accepts the key values
                GenerateConstructorWithKeys();

                //Also generate a constructor which accepts a scope and keys
                GenerateConstructorWithScopeKeys();

                //Now generate constructor with Path & Options
                GenerateConstructorWithPathOptions();

                //Now Generate a constructor with scope & path
                GenerateConstructorWithScopePath();

                //Now Generate the GetInstances()
                GenerateGetInstancesWithNoParameters();

                //Now Generate the GetInstances(condition)
                GenerateGetInstancesWithCondition();

                //Now Generate the GetInstances(propertylist)
                GenerateGetInstancesWithProperties();

                //Now Generate the GetInstances(condition,propertylist)
                GenerateGetInstancesWithWhereProperties();

                //Now Generate the GetInstances(scope)
                GenerateGetInstancesWithScope();

                //Now Generate the GetInstances(scope,condition)
                GenerateGetInstancesWithScopeCondition();

                //Now Generate the GetInstances(scope,propertylist)
                GenerateGetInstancesWithScopeProperties();

                //Now Generate the GetInstances(scope,condition,propertylist)
                GenerateGetInstancesWithScopeWhereProperties();

                //Generate the Collection Class
                GenerateCollectionClass();
            }

            //Now create constructor with path object
            GenerateConstructorWithPath();

            //Now Generate the constructor with path,scope,options
            GenerateConstructorWithScopePathOptions();

            //Now generate Constructor with latebound Object
            GenarateConstructorWithLateBound();

            //Now generate Constructor with latebound Object
            // and the object is an embedded object
            GenarateConstructorWithLateBoundForEmbedded();

            // Generate static CreateInstance() to create instance of the class
            GenerateCreateInstance();

            // Generate static DeleteInstance() to delete instance
            GenerateDeleteInstance();

            //Now Enumerate all the methods
            GenerateMethods();

            //Now declare the private sealed class variables
            //private Wmi_SystemProps SystemProps
            GeneratePrivateMember(PrivateNamesUsed["SystemPropertiesObject"].ToString(), PublicNamesUsed["SystemPropertiesClass"].ToString(), null);

            //private WmiObject privObject
            GeneratePrivateMember(PrivateNamesUsed["LateBoundObject"].ToString(), PublicNamesUsed["LateBoundClass"].ToString(), SR.CommentLateBoundObject);

            //private Internal AutoCommitProperty
            GeneratePrivateMember(PrivateNamesUsed["AutoCommitProperty"].ToString(), "System.Boolean", new CodePrimitiveExpression(true), false, SR.CommentPrivateAutoCommit);

            //private WmiObject Embedded object
            GeneratePrivateMember(PrivateNamesUsed["EmbeddedObject"].ToString(), PublicNamesUsed["BaseObjClass"].ToString(), SR.CommentEmbeddedObject);

            //private WmiObject for current object used
            GeneratePrivateMember(PrivateNamesUsed["CurrentObject"].ToString(), PublicNamesUsed["BaseObjClass"].ToString(), SR.CommentCurrentObject);

            //private WmiObject for current object used
            GeneratePrivateMember(PrivateNamesUsed["IsEmbedded"].ToString(), "System.Boolean", new CodePrimitiveExpression(false), false, SR.CommentFlagForEmbedded);

            //Now generate the Type Converter class also
            cc.Members.Add(GenerateTypeConverterClass());

            if (bIncludeSystemClassinClassDef)
            {
                cc.Members.Add(GenerateSystemPropertiesClass());
            }

            if (bHasEmbeddedProperties)
            {
                AddCommentsForEmbeddedProperties();
            }
            // Added at the end so that this comment is the last comment just before declaring the class
            cc.Comments.Add(new CodeCommentStatement(SR.CommentClassBegin +
                OriginalClassName));
            return cc;
        }

        private bool GenerateAndWriteCode(CodeLanguage lang)
        {

            if (InitializeCodeGenerator(lang) == false)
            {
                return false;
            }

            //Now Initialize the code class for generation
            InitializeCodeTypeDeclaration(lang);

            // Call this function to create CodeTypeDeclaration for the WMI class
            GetCodeTypeDeclarationForClass(true);

            // Trying to resolve the classname with a identifier
            cc.Name = cp.CreateValidIdentifier(cc.Name);

            //As we have finished the class definition, generate the class code NOW!!!!!
            cn.Types.Add(cc);

            try
            {
                cp.GenerateCodeFromNamespace(cn, tw, new CodeGeneratorOptions());
            }
            finally
            {
                tw.Close();
            }

            return true;

        }

        /// <summary>
        /// Function for initializing the class object that will be used to get all the
        /// method and properties of the WMI Class for generating the code.
        /// </summary>
        private void InitializeClassObject()
        {
            //First try to connect to WMI and get the class object.
            // If it fails then no point in continuing

            // If object is not initialized by the constructor
            if (classobj == null)
            {
                ManagementPath thePath;
                if (OriginalPath.Length != 0)
                {
                    thePath = new ManagementPath(OriginalPath);
                }
                else
                {
                    thePath = new ManagementPath();
                    if (OriginalServer.Length != 0)
                        thePath.Server = OriginalServer;
                    thePath.ClassName = OriginalClassName;
                    thePath.NamespacePath = OriginalNamespace;

                    /*
                        throw new Exception("OriginalServer is " + OriginalServer +
                            " OriginalNamespace is " + OriginalNamespace +
                            " OriginalClassName is " + OriginalClassName +
                            " results in " + thePath.Path);
                            */
                }
                classobj = new ManagementClass(thePath);
            }
            else
            {
                // Get the common properties
                ManagementPath thePath = classobj.Path;
                OriginalServer = thePath.Server;
                OriginalClassName = thePath.ClassName;
                OriginalNamespace = thePath.NamespacePath;

                char[] arrString = OriginalNamespace.ToCharArray();

                // Remove the server from the namespace
                if (arrString.Length >= 2 && arrString[0] == '\\' && arrString[1] == '\\')
                {
                    bool bStart = false;
                    int Len = OriginalNamespace.Length;
                    OriginalNamespace = string.Empty;
                    for (int i = 2; i < Len; i++)
                    {
                        if (bStart)
                        {
                            OriginalNamespace += arrString[i];
                        }
                        else
                            if (arrString[i] == '\\')
                        {
                            bStart = true;
                        }
                    }
                }

            }

            try
            {
                classobj.Get();
            }
            catch (ManagementException)
            {
                throw;
            }
            //By default all classes are non-singleton(???)
            bSingletonClass = false;
            foreach (QualifierData q in classobj.Qualifiers)
            {
                if (string.Equals(q.Name, "singleton", StringComparison.OrdinalIgnoreCase))
                {
                    //This is a singleton class
                    bSingletonClass = true;
                    break;
                }
            }

        }
        /// <summary>
        /// This functrion initializes the public attributes and private variables
        /// list that will be used in the generated code.
        /// </summary>
        private void InitilializePublicPrivateMembers()
        {
            //Initialize the public members
            PublicNamesUsed.Add("SystemPropertiesProperty", "SystemProperties");
            PublicNamesUsed.Add("LateBoundObjectProperty", "LateBoundObject");
            PublicNamesUsed.Add("NamespaceProperty", "OriginatingNamespace");
            PublicNamesUsed.Add("ClassNameProperty", "ManagementClassName");
            PublicNamesUsed.Add("ScopeProperty", "Scope");
            PublicNamesUsed.Add("PathProperty", "Path");
            PublicNamesUsed.Add("SystemPropertiesClass", "ManagementSystemProperties");
            PublicNamesUsed.Add("LateBoundClass", "System.Management.ManagementObject");
            PublicNamesUsed.Add("PathClass", "System.Management.ManagementPath");
            PublicNamesUsed.Add("ScopeClass", "System.Management.ManagementScope");
            PublicNamesUsed.Add("QueryOptionsClass", "System.Management.EnumerationOptions");
            PublicNamesUsed.Add("GetOptionsClass", "System.Management.ObjectGetOptions");
            PublicNamesUsed.Add("ArgumentExceptionClass", "System.ArgumentException");
            PublicNamesUsed.Add("QueryClass", "SelectQuery");
            PublicNamesUsed.Add("ObjectSearcherClass", "System.Management.ManagementObjectSearcher");
            PublicNamesUsed.Add("FilterFunction", "GetInstances");
            PublicNamesUsed.Add("ConstructPathFunction", "ConstructPath");
            PublicNamesUsed.Add("TypeConverter", "TypeConverter");
            PublicNamesUsed.Add("AutoCommitProperty", "AutoCommit");
            PublicNamesUsed.Add("CommitMethod", "CommitObject");
            PublicNamesUsed.Add("ManagementClass", "System.Management.ManagementClass");
            PublicNamesUsed.Add("NotSupportedExceptClass", "System.NotSupportedException");
            PublicNamesUsed.Add("BaseObjClass", "System.Management.ManagementBaseObject");
            PublicNamesUsed.Add("OptionsProp", "Options");
            PublicNamesUsed.Add("ClassPathProperty", "ClassPath");
            PublicNamesUsed.Add("CreateInst", "CreateInstance");
            PublicNamesUsed.Add("DeleteInst", "Delete");
            // Adding this so that the namespace resolving routine does not name
            // any properties with the name "System"
            PublicNamesUsed.Add("SystemNameSpace", "System");
            PublicNamesUsed.Add("ArgumentOutOfRangeException", "System.ArgumentOutOfRangeException");
            PublicNamesUsed.Add("System", "System");
            PublicNamesUsed.Add("Other", "Other");
            PublicNamesUsed.Add("Unknown", "Unknown");
            PublicNamesUsed.Add("PutOptions", "System.Management.PutOptions");
            PublicNamesUsed.Add("Type", "System.Type");
            PublicNamesUsed.Add("Boolean", "System.Boolean");
            PublicNamesUsed.Add("ValueType", "System.ValueType");
            PublicNamesUsed.Add("Events1", "Events");
            PublicNamesUsed.Add("Component1", "Component");

            //Initialize the Private Members
            PrivateNamesUsed.Add("SystemPropertiesObject", "PrivateSystemProperties");
            PrivateNamesUsed.Add("LateBoundObject", "PrivateLateBoundObject");
            PrivateNamesUsed.Add("AutoCommitProperty", "AutoCommitProp");
            PrivateNamesUsed.Add("Privileges", "EnablePrivileges");
            PrivateNamesUsed.Add("ComponentClass", "System.ComponentModel.Component");
            PrivateNamesUsed.Add("ScopeParam", "mgmtScope");
            PrivateNamesUsed.Add("NullRefExcep", "System.NullReferenceException");
            PrivateNamesUsed.Add("ConverterClass", "WMIValueTypeConverter");
            PrivateNamesUsed.Add("EnumParam", "enumOptions");
            PrivateNamesUsed.Add("CreationClassName", "CreatedClassName");
            PrivateNamesUsed.Add("CreationWmiNamespace", "CreatedWmiNamespace");
            PrivateNamesUsed.Add("ClassNameCheckFunc", "CheckIfProperClass");
            PrivateNamesUsed.Add("EmbeddedObject", "embeddedObj");
            PrivateNamesUsed.Add("CurrentObject", "curObj");
            PrivateNamesUsed.Add("IsEmbedded", "isEmbedded");
            PrivateNamesUsed.Add("ToDateTimeMethod", "ToDateTime");
            PrivateNamesUsed.Add("ToDMTFDateTimeMethod", "ToDmtfDateTime");
            PrivateNamesUsed.Add("ToDMTFTimeIntervalMethod", "ToDmtfTimeInterval");
            PrivateNamesUsed.Add("ToTimeSpanMethod", "ToTimeSpan");
            PrivateNamesUsed.Add("SetMgmtScope", "SetStaticManagementScope");
            PrivateNamesUsed.Add("statMgmtScope", "statMgmtScope");
            PrivateNamesUsed.Add("staticScope", "StaticScope");
            PrivateNamesUsed.Add("initVariable", "Initialize");
            PrivateNamesUsed.Add("putOptions", "putOptions");
            PrivateNamesUsed.Add("InitialObjectFunc", "InitializeObject");


        }

        /// <summary>
        /// This function will solve the naming collisions that might occur
        /// due to the collision between the local objects of the generated
        /// class and the properties/methods of the original WMI Class.
        /// </summary>
        private void ProcessNamingCollisions()
        {
            if (classobj.Properties != null)
            {
                foreach (PropertyData prop in classobj.Properties)
                {
                    PublicProperties.Add(prop.Name, prop.Name);
                }
            }

            if (classobj.Methods != null)
            {
                foreach (MethodData meth in classobj.Methods)
                {
                    PublicMethods.Add(meth.Name, meth.Name);
                }
            }

            int nIndex;

            //Process the collisions here
            //We will check each public names with the property names here.
            foreach (string s in PublicNamesUsed.Values)
            {
                nIndex = IsContainedIn(s, ref PublicProperties);
                if (nIndex != -1)
                {
                    //We had found a collision with a public property
                    //So we will resolve the collision by changing the property name
                    //and continue
                    PublicProperties.SetByIndex(nIndex, ResolveCollision(s, false));
                    continue;
                }

                nIndex = IsContainedIn(s, ref PublicMethods);
                if (nIndex != -1)
                {
                    //We had found a collision with a public method
                    //So we will resolve the collision by changing the method name
                    //and continue
                    PublicMethods.SetByIndex(nIndex, ResolveCollision(s, false));
                    continue;
                }
            }

            //Now we will check for collision against private variables
            foreach (string s in PublicProperties.Values)
            {
                nIndex = IsContainedIn(s, ref PrivateNamesUsed);
                if (nIndex != -1)
                {
                    //We had found a collision with a public property
                    //So we will resolve the collision by changing the private name
                    //and continue
                    PrivateNamesUsed.SetByIndex(nIndex, ResolveCollision(s, false));
                }
            }

            foreach (string s in PublicMethods.Values)
            {
                nIndex = IsContainedIn(s, ref PrivateNamesUsed);
                if (nIndex != -1)
                {
                    //We had found a collision with a public method
                    //So we will resolve the collision by changing the private name
                    //and continue
                    PrivateNamesUsed.SetByIndex(nIndex, ResolveCollision(s, false));
                }
            }

            //Now we will check for collision against Methods and Public Properties
            foreach (string s in PublicProperties.Values)
            {
                nIndex = IsContainedIn(s, ref PublicMethods);
                if (nIndex != -1)
                {
                    //We had found a collision with a public property
                    //So we will resolve the collision by changing the Public Property name
                    //and continue
                    PublicMethods.SetByIndex(nIndex, ResolveCollision(s, false));
                }
            }

            //Now we will create the CollectionClass and Enumerator Class names as they are dependent on the
            //generated class name and the generated class name might have changed due to collision
            string strTemp = PrivateNamesUsed["GeneratedClassName"].ToString() + "Collection";
            PrivateNamesUsed.Add("CollectionClass", ResolveCollision(strTemp, true));

            strTemp = PrivateNamesUsed["GeneratedClassName"].ToString() + "Enumerator";
            PrivateNamesUsed.Add("EnumeratorClass", ResolveCollision(strTemp, true));
        }

        /// <summary>
        /// This function is used to resolve (actually generate a new name) collision
        /// between the generated class properties/variables with WMI methods/properties.
        /// This function safely assumes that there will be atleast one string left
        /// in the series prop0, prop1 ...prop(maxInt) . Otherwise this function will
        /// enter an infinite loop. May be we can avoid this through something, which
        /// i will think about it later
        /// </summary>
        /// <param name="inString"> </param>
        /// <param name="bCheckthisFirst"></param>
        private string ResolveCollision(string inString, bool bCheckthisFirst)
        {
            string strTemp = inString;
            bool bCollision = true;
            int k = -1;
            string strToAdd = "";
            if (bCheckthisFirst == false)
            {
                k++;
                strTemp = strTemp + strToAdd + k.ToString((IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(int)));
            }

            while (bCollision)
            {
                if (IsContainedIn(strTemp, ref PublicProperties) == -1)
                {
                    if (IsContainedIn(strTemp, ref PublicMethods) == -1)
                    {
                        if (IsContainedIn(strTemp, ref PublicNamesUsed) == -1)
                        {
                            if (IsContainedIn(strTemp, ref PrivateNamesUsed) == -1)
                            {
                                //So this is not colliding with anything.
                                bCollision = false;
                                break;
                            }
                        }
                    }
                }
                try
                {
                    k++;
                }
                catch (OverflowException)
                {
                    strToAdd += "_";
                    k = 0;
                }
                strTemp = inString + strToAdd + k.ToString((IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(int)));
            }

            if (strTemp.Length > 0)
            {
                string strFirstChar = strTemp.Substring(0, 1).ToUpperInvariant();
                strTemp = strFirstChar + strTemp.Substring(1);
            }

            return strTemp;
        }

        /// <summary>
        /// This function processes the WMI namespace and WMI classname and converts them to
        /// the namespace used to generate the class and the classname.
        /// </summary>
        private void ProcessNamespaceAndClassName()
        {
            string strClass = string.Empty;
            string strNs = string.Empty;

            // if Namespace is not alread set then construct the namespace
            if (NETNamespace.Length == 0)
            {
                strNs = OriginalNamespace;
                strNs = strNs.Replace('\\', '.');
                strNs = strNs.ToUpperInvariant();
            }
            else
            {
                strNs = NETNamespace;
            }

            if (OriginalClassName.IndexOf('_') > 0)
            {
                strClass = OriginalClassName.Substring(0, OriginalClassName.IndexOf('_'));
                // if Namespace is not alread set then construct the namespace
                if (NETNamespace.Length == 0)
                {
                    strNs += ".";
                    strNs += strClass;
                }
                //Now trim the class name without the first '_'
                strClass = OriginalClassName.Substring(OriginalClassName.IndexOf('_') + 1);
            }
            else
            {
                strClass = OriginalClassName;
            }

            // Check if the name of the class starts with a charachter. If not add "C" to the beginning of the name
            if (char.IsLetter(strClass[0]) == false)
            {
                strClass = "C" + strClass;
            }

            strClass = ResolveCollision(strClass, true);

            // Try to get a type from any of the namespace which are used in the generated code and see if
            // it collides with any of the standard classes.
            if (Type.GetType("System." + strClass) != null ||
                Type.GetType("System.ComponentModel." + strClass) != null ||
                Type.GetType("System.Management." + strClass) != null ||
                Type.GetType("System.Collections." + strClass) != null ||
                Type.GetType("System.Globalization." + strClass) != null)
            {
                PublicNamesUsed.Add(strClass, strClass);
                strClass = ResolveCollision(strClass, true);
            }

            PrivateNamesUsed.Add("GeneratedClassName", strClass);
            PrivateNamesUsed.Add("GeneratedNamespace", strNs);
        }



        private void InitializeCodeTypeDeclaration(CodeLanguage lang)
        {
            //Now add the import statements
            cn = new CodeNamespace(PrivateNamesUsed["GeneratedNamespace"].ToString());
            cn.Imports.Add(new CodeNamespaceImport("System"));
            cn.Imports.Add(new CodeNamespaceImport("System.ComponentModel"));
            cn.Imports.Add(new CodeNamespaceImport("System.Management"));
            cn.Imports.Add(new CodeNamespaceImport("System.Collections"));
            cn.Imports.Add(new CodeNamespaceImport("System.Globalization"));

            if (lang == CodeLanguage.VB)
            {
                cn.Imports.Add(new CodeNamespaceImport("Microsoft.VisualBasic"));
            }

        }
        /// <summary>
        /// This function generates the code for the read only property.
        /// The generated code will be of the form
        ///        public &lt;propType&gt; &lt;propName&gt;{
        ///            get {
        ///                    return (&lt;propValue&gt;);
        ///                }
        ///        }
        /// </summary>
        /// <param name="propName"> </param>
        /// <param name="propType"> </param>
        /// <param name="propValue"> </param>
        /// <param name="isLiteral"></param>
        /// <param name="isBrowsable"></param>
        /// <param name="Comment"></param>
        private void GeneratePublicReadOnlyProperty(string propName, string propType, string propValue, bool isLiteral, bool isBrowsable, string Comment)
        {
            cmp = new CodeMemberProperty();
            cmp.Name = propName;
            cmp.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            cmp.Type = new CodeTypeReference(propType);

            caa = new CodeAttributeArgument();
            caa.Value = new CodePrimitiveExpression(isBrowsable);
            cad = new CodeAttributeDeclaration();
            cad.Name = "Browsable";
            cad.Arguments.Add(caa);
            cmp.CustomAttributes = new CodeAttributeDeclarationCollection();
            cmp.CustomAttributes.Add(cad);

            caa = new CodeAttributeArgument();
            caa.Value = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("DesignerSerializationVisibility"), "Hidden");
            cad = new CodeAttributeDeclaration();
            cad.Name = "DesignerSerializationVisibility";
            cad.Arguments.Add(caa);
            cmp.CustomAttributes.Add(cad);

            if (isLiteral)
            {
                cmp.GetStatements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression(propValue)));
            }
            else
            {
                cmp.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(propValue)));
            }
            cc.Members.Add(cmp);
            if (!string.IsNullOrEmpty(Comment))
            {
                cmp.Comments.Add(new CodeCommentStatement(Comment));
            }
        }

        private void GeneratePublicProperty(string propName, string propType, CodeExpression Value, bool isBrowsable, string Comment, bool isStatic)
        {
            cmp = new CodeMemberProperty();
            cmp.Name = propName;
            cmp.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            cmp.Type = new CodeTypeReference(propType);

            if (isStatic)
            {
                cmp.Attributes |= MemberAttributes.Static;
            }

            caa = new CodeAttributeArgument();
            caa.Value = new CodePrimitiveExpression(isBrowsable);
            cad = new CodeAttributeDeclaration();
            cad.Name = "Browsable";
            cad.Arguments.Add(caa);
            cmp.CustomAttributes = new CodeAttributeDeclarationCollection();
            cmp.CustomAttributes.Add(cad);

            // If the property is not Path then add an attribb DesignerSerializationVisibility
            // to indicate that the property is to be hidden for designer serilization.
            if (IsDesignerSerializationVisibilityToBeSet(propName))
            {
                caa = new CodeAttributeArgument();
                caa.Value = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("DesignerSerializationVisibility"), "Hidden");
                cad = new CodeAttributeDeclaration();
                cad.Name = "DesignerSerializationVisibility";
                cad.Arguments.Add(caa);
                cmp.CustomAttributes.Add(cad);
            }

            cmp.GetStatements.Add(new CodeMethodReturnStatement(Value));

            cmp.SetStatements.Add(new CodeAssignStatement(Value,
                new CodeSnippetExpression("value")));
            cc.Members.Add(cmp);

            if (!string.IsNullOrEmpty(Comment))
            {
                cmp.Comments.Add(new CodeCommentStatement(Comment));
            }
        }

        private void GeneratePathProperty()
        {
            cmp = new CodeMemberProperty();
            cmp.Name = PublicNamesUsed["PathProperty"].ToString();
            cmp.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            cmp.Type = new CodeTypeReference(PublicNamesUsed["PathClass"].ToString());

            caa = new CodeAttributeArgument();
            caa.Value = new CodePrimitiveExpression(true);
            cad = new CodeAttributeDeclaration();
            cad.Name = "Browsable";
            cad.Arguments.Add(caa);
            cmp.CustomAttributes = new CodeAttributeDeclarationCollection();
            cmp.CustomAttributes.Add(cad);

            cpre = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(
                PrivateNamesUsed["LateBoundObject"].ToString()),
                "Path");

            cis = new CodeConditionStatement();
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(PrivateNamesUsed["IsEmbedded"].ToString());
            cboe.Right = new CodePrimitiveExpression(false);
            cboe.Operator = CodeBinaryOperatorType.ValueEquality;
            cis.Condition = cboe;

            cis.TrueStatements.Add(new CodeMethodReturnStatement(cpre));
            cis.FalseStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
            cmp.GetStatements.Add(cis);



            cis = new CodeConditionStatement();
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(PrivateNamesUsed["IsEmbedded"].ToString());
            cboe.Right = new CodePrimitiveExpression(false);
            cboe.Operator = CodeBinaryOperatorType.ValueEquality;
            cis.Condition = cboe;

            CodeConditionStatement cis1 = new CodeConditionStatement();
            cmie = new CodeMethodInvokeExpression();
            cmie.Method.MethodName = PrivateNamesUsed["ClassNameCheckFunc"].ToString();


            cmie.Parameters.Add(new CodePrimitiveExpression(null));
            cmie.Parameters.Add(new CodeVariableReferenceExpression("value"));
            cmie.Parameters.Add(new CodePrimitiveExpression(null));

            CodeBinaryOperatorExpression cboe1 = new CodeBinaryOperatorExpression();
            cboe1.Left = cmie;
            cboe1.Right = new CodePrimitiveExpression(true);
            cboe1.Operator = CodeBinaryOperatorType.IdentityInequality;
            cis1.Condition = cboe1;
            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["ArgumentExceptionClass"].ToString());
            coce.Parameters.Add(new CodePrimitiveExpression(SR.ClassNameNotFoundException));
            cis1.TrueStatements.Add(new CodeThrowExceptionStatement(coce));
            cis.TrueStatements.Add(cis1);


            cis.TrueStatements.Add(new CodeAssignStatement(cpre,
                new CodeSnippetExpression("value")));

            cmp.SetStatements.Add(cis);
            cc.Members.Add(cmp);

            cmp.Comments.Add(new CodeCommentStatement(SR.CommentManagementPath));
        }

        /// <summary>
        /// Function for generating the helper class "ManagementSystemProperties" which is
        /// used for separating the system properties from the other properties. This is used
        /// just to make the drop down list in the editor to look good.
        /// </summary>
        private CodeTypeDeclaration GenerateSystemPropertiesClass()
        {
            CodeTypeDeclaration SysPropsClass = new CodeTypeDeclaration(PublicNamesUsed["SystemPropertiesClass"].ToString());
            SysPropsClass.TypeAttributes = TypeAttributes.NestedPublic;

            //First create the constructor
            //    public ManagementSystemProperties(ManagementObject obj)

            cctor = new CodeConstructor();
            cctor.Attributes = MemberAttributes.Public;
            cpde = new CodeParameterDeclarationExpression();
            cpde.Type = new CodeTypeReference(PublicNamesUsed["BaseObjClass"].ToString());
            cpde.Name = "ManagedObject";
            cctor.Parameters.Add(cpde);
            cctor.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(PrivateNamesUsed["LateBoundObject"].ToString()), new CodeVariableReferenceExpression("ManagedObject")));
            SysPropsClass.Members.Add(cctor);

            caa = new CodeAttributeArgument();
            caa.Value = new CodeTypeOfExpression(typeof(System.ComponentModel.ExpandableObjectConverter));
            cad = new CodeAttributeDeclaration();
            cad.Name = PublicNamesUsed["TypeConverter"].ToString();
            cad.Arguments.Add(caa);
            SysPropsClass.CustomAttributes.Add(cad);

            char[] strPropTemp;
            char[] strPropName;
            int i = 0;

            foreach (PropertyData prop in classobj.SystemProperties)
            {
                cmp = new CodeMemberProperty();
                //All properties are browsable by default.
                caa = new CodeAttributeArgument();
                caa.Value = new CodePrimitiveExpression(true);
                cad = new CodeAttributeDeclaration();
                cad.Name = "Browsable";
                cad.Arguments.Add(caa);
                cmp.CustomAttributes = new CodeAttributeDeclarationCollection();
                cmp.CustomAttributes.Add(cad);

                //Now we will have to find the occurrence of the first character and trim all the characters before that
                strPropTemp = prop.Name.ToCharArray();
                for (i = 0; i < strPropTemp.Length; i++)
                {
                    if (char.IsLetterOrDigit(strPropTemp[i]))
                    {
                        break;
                    }
                }
                if (i == strPropTemp.Length)
                {
                    i = 0;
                }
                strPropName = new char[strPropTemp.Length - i];
                for (int j = i; j < strPropTemp.Length; j++)
                {
                    strPropName[j - i] = strPropTemp[j];
                }

                cmp.Name = (new string(strPropName)).ToUpperInvariant();
                cmp.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                cmp.Type = ConvertCIMType(prop.Type, prop.IsArray);

                cie = new CodeIndexerExpression(
                    new CodeVariableReferenceExpression(PrivateNamesUsed["LateBoundObject"].ToString()),
                    new CodeExpression[] { new CodePrimitiveExpression(prop.Name) });

                cmp.GetStatements.Add(new CodeMethodReturnStatement(new CodeCastExpression(cmp.Type, cie)));
                SysPropsClass.Members.Add(cmp);
            }
            //private WmiObject _privObject
            cf = new CodeMemberField();
            cf.Name = PrivateNamesUsed["LateBoundObject"].ToString();
            cf.Attributes = MemberAttributes.Private | MemberAttributes.Final;
            cf.Type = new CodeTypeReference(PublicNamesUsed["BaseObjClass"].ToString());
            SysPropsClass.Members.Add(cf);

            SysPropsClass.Comments.Add(new CodeCommentStatement(SR.CommentSystemPropertiesClass));
            return SysPropsClass;

        }
        /// <summary>
        /// This function will enumerate all the properties (except systemproperties)
        /// of the WMI class and will generate them as properties of the managed code
        /// wrapper class.
        /// </summary>
        private void GenerateProperties()
        {
            bool bRead;
            bool bWrite;
            bool bStatic;
            bool bDynamicClass = IsDynamicClass();
            CodeMemberMethod cmm2 = null;
            CodeMemberProperty cmp2 = null;
            string IsValidPropName = string.Empty;
            bool bDateIsTimeInterval = false;

            for (int i = 0; i < PublicProperties.Count; i++)
            {
                bDateIsTimeInterval = false;
                PropertyData prop = classobj.Properties[PublicProperties.GetKey(i).ToString()];
                bRead = true;        //All properties are readable by default
                bWrite = true;        //All properties are writeable by default
                bStatic = false;    //By default all properties are non static

                cmp = new CodeMemberProperty();
                cmp.Name = PublicProperties[prop.Name].ToString();
                cmp.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                cmp.Type = ConvertCIMType(prop.Type, prop.IsArray);

                if (prop.Type == CimType.DateTime)
                {
                    CodeTypeReference dateType = cmp.Type;
                    // Check if it is Time interval and if so change the type to Time Interval
                    bDateIsTimeInterval = GetDateTimeType(prop, ref dateType);
                    cmp.Type = dateType;
                }

                // Check if the property is a of type ManagementBaseObject
                // or array of ManagementBaseObject. If so then the property
                // is of type embedded object
                if ((cmp.Type.ArrayRank == 0 && cmp.Type.BaseType == new CodeTypeReference(PublicNamesUsed["BaseObjClass"].ToString()).BaseType) ||
                    cmp.Type.ArrayRank > 0 && cmp.Type.ArrayElementType.BaseType == new CodeTypeReference(PublicNamesUsed["BaseObjClass"].ToString()).BaseType)
                {
                    bHasEmbeddedProperties = true;
                }

                // Method for Is<PropertyName>Null property
                IsValidPropName = "Is" + PublicProperties[prop.Name].ToString() + "Null";

                cmp2 = new CodeMemberProperty();
                cmp2.Name = IsValidPropName;
                cmp2.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                cmp2.Type = new CodeTypeReference("System.Boolean");

                //All properties are browsable, by default
                caa = new CodeAttributeArgument();
                caa.Value = new CodePrimitiveExpression(true);
                cad = new CodeAttributeDeclaration();
                cad.Name = "Browsable";
                cad.Arguments.Add(caa);
                cmp.CustomAttributes = new CodeAttributeDeclarationCollection();
                cmp.CustomAttributes.Add(cad);

                caa = new CodeAttributeArgument();
                caa.Value = new CodePrimitiveExpression(false);
                cad = new CodeAttributeDeclaration();
                cad.Name = "Browsable";
                cad.Arguments.Add(caa);
                cmp2.CustomAttributes = new CodeAttributeDeclarationCollection();
                cmp2.CustomAttributes.Add(cad);

                // None of the properties are seriazable thru designer
                caa = new CodeAttributeArgument();
                caa.Value = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("DesignerSerializationVisibility"), "Hidden");
                cad = new CodeAttributeDeclaration();
                cad.Name = "DesignerSerializationVisibility";
                cad.Arguments.Add(caa);
                cmp.CustomAttributes.Add(cad);
                cmp2.CustomAttributes.Add(cad);

                cie = new CodeIndexerExpression(
                    new CodeVariableReferenceExpression(PrivateNamesUsed["CurrentObject"].ToString()),
                    new CodeExpression[] { new CodePrimitiveExpression(prop.Name) });


                bool bNullable = false;
                string description = ProcessPropertyQualifiers(prop, ref bRead, ref bWrite, ref bStatic, bDynamicClass, out bNullable);

                // If the property is not readable and not writable then don't generate the property
                if (bRead == false && bWrite == false)
                {
                    continue;
                }

                if (description.Length != 0)
                {
                    //All properties are Description, by default
                    caa = new CodeAttributeArgument();
                    caa.Value = new CodePrimitiveExpression(description);
                    cad = new CodeAttributeDeclaration();
                    cad.Name = "Description";
                    cad.Arguments.Add(caa);
                    cmp.CustomAttributes.Add(cad);
                }


                //WMI Values qualifier values cannot be used as
                //enumerator constants: they contain spaces, dots, dashes, etc.
                //These need to be modified, otherwise the generated file won't compile.
                //Uncomment the line below when that is fixed.
                bool isPropertyEnum = GeneratePropertyHelperEnums(prop, PublicProperties[prop.Name].ToString(), bNullable);

                if (bRead)
                {
                    if (IsPropertyValueType(prop.Type) && prop.IsArray == false)
                    {

                        /*
                                [Browsable(false)]
                                [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
                                public <Type> Is<PropertyName>Null {
                                    get {
                                        if ((PrivateLateBoundObject[<PropertyName>] == null)) {
                                            return true;
                                        }
                                        else {
                                            return false;
                                        }
                                    }
                                }
                        */
                        cis = new CodeConditionStatement();
                        cis.Condition = new CodeBinaryOperatorExpression(cie,
                            CodeBinaryOperatorType.IdentityEquality,
                            new CodePrimitiveExpression(null));


                        cis.TrueStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(true)));
                        cis.FalseStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(false)));
                        cmp2.GetStatements.Add(cis);
                        cc.Members.Add(cmp2);

                        // Adding TypeConverter Attribute
                        caa = new CodeAttributeArgument();
                        caa.Value = new CodeTypeOfExpression(PrivateNamesUsed["ConverterClass"].ToString());
                        cad = new CodeAttributeDeclaration();
                        cad.Name = PublicNamesUsed["TypeConverter"].ToString();
                        cad.Arguments.Add(caa);
                        cmp.CustomAttributes.Add(cad);

                        // Since functions handling Datetime, TimeSpan returns MinValue, there is no need to check for null in the property
                        // accessor for Datetime
                        if (prop.Type != CimType.DateTime)
                        {
                            cis = new CodeConditionStatement();
                            cis.Condition = new CodeBinaryOperatorExpression(cie,
                                CodeBinaryOperatorType.IdentityEquality,
                                new CodePrimitiveExpression(null));

                            if (isPropertyEnum)
                            {
                                if (prop.IsArray)
                                {
                                    cis.TrueStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
                                }
                                else
                                {
                                    //if ((curObj["<PropertyName>"] == null))
                                    //{
                                    // return ((<EnumName>)(System.Convert.ToInt32(0)));
                                    //}
                                    //return (<EnumName>)System.Convert.ToInt32(0);
                                    cmie = new CodeMethodInvokeExpression();
                                    cmie.Method.TargetObject = new CodeTypeReferenceExpression("System.Convert");
                                    cmie.Parameters.Add(new CodePrimitiveExpression(prop.NullEnumValue));
                                    cmie.Method.MethodName = arrConvFuncName;
                                    cis.TrueStatements.Add(new CodeMethodReturnStatement(new CodeCastExpression(cmp.Type, cmie)));
                                }
                            }
                            else
                            {
                                // if ((curObj["<PropertyName>"] == null))
                                //{
                                //     return System.Convert.<Type>(0);
                                //}
                                //return ((<Type>)(curObj["<PropertyName>"])) ;
                                cmie = new CodeMethodInvokeExpression();
                                cmie.Parameters.Add(new CodePrimitiveExpression(prop.NullEnumValue));
                                cmie.Method.MethodName = GetConversionFunction(prop.Type);
                                cmie.Method.TargetObject = new CodeTypeReferenceExpression("System.Convert");
                                if (prop.IsArray)
                                {
                                    CodeExpression[] cInit = { cmie };
                                    cis.TrueStatements.Add(new CodeMethodReturnStatement(
                                        new CodeArrayCreateExpression(cmp.Type, cInit)));

                                }
                                else
                                {
                                    // return (<EnumName>)System.Convert.<ConvertFuncName>(0);
                                    cis.TrueStatements.Add(new CodeMethodReturnStatement(cmie));
                                }
                            }
                            cmp.GetStatements.Add(cis);
                        }

                        /*
                            private bool ShouldSerialize<propertyName>()
                            {
                                if (Is<PropertyName>Null == true)
                                    return false;

                                return true;


                            }
                        */
                        cmm = new CodeMemberMethod();
                        cmm.Name = "ShouldSerialize" + PublicProperties[prop.Name].ToString();
                        cmm.Attributes = MemberAttributes.Private | MemberAttributes.Final;
                        cmm.ReturnType = new CodeTypeReference("System.Boolean");

                        CodeConditionStatement cis2 = new CodeConditionStatement();

                        cis2.Condition = new CodeBinaryOperatorExpression(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), IsValidPropName),
                            CodeBinaryOperatorType.ValueEquality,
                            new CodePrimitiveExpression(false));

                        cis2.TrueStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(true)));

                        cmm.Statements.Add(cis2);
                        cmm.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(false)));
                        cc.Members.Add(cmm);
                    }

                    if (prop.Type == CimType.Reference)
                    {
                        //Call this function to add code for PropertyGet for properties like
                        // DateTime,TimeSpan and Reference( ie. properties which require some object
                        // to be created before returning)
                        GenerateCodeForRefAndDateTimeTypes(cie, prop.IsArray, cmp.GetStatements, PublicNamesUsed["PathClass"].ToString(), null, false);
                    }
                    else
                        if (prop.Type == CimType.DateTime)
                    {
                        //Call this function to add code for PropertyGet for properties like
                        // DateTime ,TimeSpan and Reference( ie. properties which require some object
                        // to be created before returning)
                        if (bDateIsTimeInterval)
                        {
                            GenerateCodeForRefAndDateTimeTypes(cie, prop.IsArray, cmp.GetStatements, "System.TimeSpan", null, false);
                        }
                        else
                        {
                            GenerateCodeForRefAndDateTimeTypes(cie, prop.IsArray, cmp.GetStatements, "System.DateTime", null, false);
                        }
                    }
                    else
                    {
                        if (isPropertyEnum)
                        {
                            if (prop.IsArray)
                            {
                                AddGetStatementsForEnumArray(cie, cmp);
                            }
                            else
                            {
                                cmie = new CodeMethodInvokeExpression();
                                cmie.Method.TargetObject = new CodeTypeReferenceExpression("System.Convert");
                                cmie.Parameters.Add(cie);
                                cmie.Method.MethodName = arrConvFuncName;
                                cmp.GetStatements.Add(new CodeMethodReturnStatement(new CodeCastExpression(cmp.Type, cmie)));
                            }
                        }
                        else
                        {
                            cmp.GetStatements.Add(new CodeMethodReturnStatement(new CodeCastExpression(cmp.Type, cie)));
                        }
                    }

                }


                if (bWrite)
                {
                    if (bNullable)
                    {
                        cmm2 = new CodeMemberMethod();
                        cmm2.Name = "Reset" + PublicProperties[prop.Name].ToString();
                        cmm2.Attributes = MemberAttributes.Private | MemberAttributes.Final;
                        cmm2.Statements.Add(new CodeAssignStatement(cie, new CodePrimitiveExpression(null)));
                    }

                    // if the type of the property is CIM_REFERENCE then just get the
                    // path as string and update the property
                    if (prop.Type == CimType.Reference)
                    {
                        //Call this function to add code for PropertySet for properties like
                        // DateTime,TimeSpan and Reference( ie. properties which require some object
                        // to be created before returning)
                        AddPropertySet(cie, prop.IsArray, cmp.SetStatements, PublicNamesUsed["PathClass"].ToString(), null);
                    }
                    else
                        if (prop.Type == CimType.DateTime)
                    {
                        //Call this function to add code for PropertySet for properties like
                        // DateTime ,TimeSpan and Reference( ie. properties which require some object
                        // to be created before returning)
                        if (bDateIsTimeInterval)
                        {
                            AddPropertySet(cie, prop.IsArray, cmp.SetStatements, "System.TimeSpan", null);
                        }
                        else
                        {
                            AddPropertySet(cie, prop.IsArray, cmp.SetStatements, "System.DateTime", null);
                        }
                    }
                    else
                    {
                        if (isPropertyEnum && bNullable)
                        {
                            /*
                            if (<PropertyName>Values.NULL_ENUM_VALUE == value)
                             {
                                 curObj[<PropertyName>] = null;
                             }
                             else
                             {
                                 curObj[<PropertyName>] = value;
                             }

                             */

                            CodeConditionStatement ccs = new CodeConditionStatement();
                            if (prop.IsArray)
                            {
                                ccs.Condition = new CodeBinaryOperatorExpression(
                                    new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference(PublicProperties[prop.Name].ToString() + "Values")), "NULL_ENUM_VALUE"),
                                    CodeBinaryOperatorType.ValueEquality,
                                    new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("value"),
                                        new CodePrimitiveExpression(0)));
                            }
                            else
                            {
                                ccs.Condition = new CodeBinaryOperatorExpression(
                                    new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference(PublicProperties[prop.Name].ToString() + "Values")), "NULL_ENUM_VALUE"),
                                    CodeBinaryOperatorType.ValueEquality,
                                    new CodeSnippetExpression("value"));
                            }
                            ccs.TrueStatements.Add(new CodeAssignStatement(cie, new CodePrimitiveExpression(null)));
                            ccs.FalseStatements.Add(new CodeAssignStatement(cie, new CodeSnippetExpression("value")));
                            cmp.SetStatements.Add(ccs);
                        }
                        else
                        {
                            //curObj[<PropertyName>] = value;
                            cmp.SetStatements.Add(new CodeAssignStatement(cie, new CodeSnippetExpression("value")));
                        }
                    }

                    cmie = new CodeMethodInvokeExpression();
                    cmie.Method.TargetObject = new CodeVariableReferenceExpression(PrivateNamesUsed["LateBoundObject"].ToString());
                    cmie.Method.MethodName = "Put";

                    cboe = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression(PrivateNamesUsed["AutoCommitProperty"].ToString()),
                        CodeBinaryOperatorType.ValueEquality,
                        new CodePrimitiveExpression(true));

                    CodeBinaryOperatorExpression cboe1 = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression(PrivateNamesUsed["IsEmbedded"].ToString()),
                        CodeBinaryOperatorType.ValueEquality,
                        new CodePrimitiveExpression(false));

                    CodeBinaryOperatorExpression cboe2 = new CodeBinaryOperatorExpression();
                    cboe2.Right = cboe;
                    cboe2.Left = cboe1;
                    cboe2.Operator = CodeBinaryOperatorType.BooleanAnd;


                    cis = new CodeConditionStatement();
                    cis.Condition = cboe2;

                    cis.TrueStatements.Add(new CodeExpressionStatement(cmie));

                    cmp.SetStatements.Add(cis);
                    if (bNullable)
                    {
                        cmm2.Statements.Add(cis);
                    }

                }
                cc.Members.Add(cmp);
                if (bNullable & bWrite)
                {
                    cc.Members.Add(cmm2);
                }



            }
            // Add a function to commit the changes of the objects to WMI
            GenerateCommitMethod();
        }
        /// <summary>
        /// This function will process the qualifiers for a given WMI property and set the
        /// attributes of the generated property accordingly.
        /// </summary>
        private string ProcessPropertyQualifiers(PropertyData prop, ref bool bRead, ref bool bWrite, ref bool bStatic, bool bDynamicClass, out bool nullable)
        {
            bool hasWrite = false;
            bool writeValue = false;
            bool bMapsFailed = false;
            nullable = true;

            // property is always readable
            bRead = true;
            bWrite = false;
            arrConvFuncName = "ToInt32";
            enumType = "System.Int32";

            string description = string.Empty;
            foreach (QualifierData q in prop.Qualifiers)
            {
                if (string.Equals(q.Name, "description", StringComparison.OrdinalIgnoreCase))
                {
                    description = q.Value.ToString();
                }
                else
                    if (string.Equals(q.Name, "Not_Null", StringComparison.OrdinalIgnoreCase))
                {
                    nullable = false;
                }
                else
                    if (string.Equals(q.Name, "key", StringComparison.OrdinalIgnoreCase))
                {
                    //This is a key. So push it in to the key array
                    arrKeyType.Add(cmp.Type);
                    arrKeys.Add(prop.Name);
                    nullable = false;
                    break;
                }
                else if (string.Equals(q.Name, "static", StringComparison.OrdinalIgnoreCase))
                {
                    //This property is static. So add static to the Type of the object
                    bStatic = true;
                    cmp.Attributes |= MemberAttributes.Static;
                }
                else if (string.Equals(q.Name, "read", StringComparison.OrdinalIgnoreCase))
                {
                    if ((bool)q.Value == false)
                    {
                        bRead = false;
                    }
                    else
                    {
                        bRead = true;
                    }
                }
                else if (string.Equals(q.Name, "write", StringComparison.OrdinalIgnoreCase))
                {
                    hasWrite = true;
                    if ((bool)q.Value)
                    {
                        writeValue = true;
                    }
                    else
                    {
                        writeValue = false;
                    }
                }
                // check for ValueMap/Values and BitMap/BitValues pair and create
                // Enum Accordingly
                else if (string.Equals(q.Name, "ValueMap", StringComparison.OrdinalIgnoreCase) && bMapsFailed == false)
                {
                    try
                    {
                        ValueMap.Clear();
                        //Now check whether the type of the property is int
                        if (isTypeInt(prop.Type))
                        {
                            if (q.Value != null)
                            {
                                string[] strArray = (string[])q.Value;
                                for (int i = 0; i < strArray.Length; i++)
                                {
                                    try
                                    {
                                        arrConvFuncName = ConvertToNumericValueAndAddToArray(prop.Type, strArray[i], ValueMap, out enumType);
                                    }
                                    catch (OverflowException)
                                    {
                                    }
                                }
                            }
                        }
                    }
                    // if the value is not a numerical, then we cannot construct a enum
                    catch (System.FormatException)
                    {
                        bMapsFailed = true;
                        ValueMap.Clear();
                    }
                    catch (System.InvalidCastException)
                    {
                        // This exception may occur if the qualifier value is not an array as expected
                        ValueMap.Clear();
                    }
                }
                else if (string.Equals(q.Name, "Values", StringComparison.OrdinalIgnoreCase) && bMapsFailed == false)
                {
                    try
                    {
                        Values.Clear();
                        if (isTypeInt(prop.Type))
                        {
                            if (q.Value != null)
                            {
                                ArrayList arTemp = new ArrayList(5);
                                string[] strArray = (string[])q.Value;
                                for (int i = 0; i < strArray.Length; i++)
                                {
                                    if (strArray[i].Length == 0)
                                    {
                                        Values.Clear();
                                        bMapsFailed = true;
                                        break;
                                    }
                                    string strName = ConvertValuesToName(strArray[i]);
                                    arTemp.Add(strName);
                                }
                                ResolveEnumNameValues(arTemp, ref Values);
                            }
                        }
                    }
                    catch (System.InvalidCastException)
                    {
                        // This exception may occur if the qualifier value is not an array as expected
                        Values.Clear();
                    }

                }
                else if (string.Equals(q.Name, "BitMap", StringComparison.OrdinalIgnoreCase) && bMapsFailed == false)
                {
                    try
                    {
                        BitMap.Clear();
                        if (isTypeInt(prop.Type))
                        {
                            if (q.Value != null)
                            {
                                string[] strArray = (string[])q.Value;
                                for (int i = 0; i < strArray.Length; i++)
                                {
                                    BitMap.Add(ConvertBitMapValueToInt32(strArray[i]));
                                }
                            }
                        }
                    }
                    // if the value is not a numerical, then we cannot construct a enum
                    catch (System.FormatException)
                    {
                        BitMap.Clear();
                        bMapsFailed = true;
                    }
                    catch (System.InvalidCastException)
                    {
                        // This exception may occur if the qualifier value is not an array as expected
                        BitMap.Clear();
                    }
                }
                else if (string.Equals(q.Name, "BitValues", StringComparison.OrdinalIgnoreCase) && bMapsFailed == false)
                {
                    try
                    {
                        BitValues.Clear();
                        if (isTypeInt(prop.Type))
                        {
                            if (q.Value != null)
                            {
                                ArrayList arTemp = new ArrayList(5);
                                string[] strArray = (string[])q.Value;
                                for (int i = 0; i < strArray.Length; i++)
                                {
                                    if (strArray[i].Length == 0)
                                    {
                                        BitValues.Clear();
                                        bMapsFailed = true;
                                        break;
                                    }
                                    string strName = ConvertValuesToName(strArray[i]);
                                    arTemp.Add(strName);
                                }
                                ResolveEnumNameValues(arTemp, ref BitValues);
                            }
                        }
                    }
                    catch (System.InvalidCastException)
                    {
                        // This exception may occur if the qualifier value is not an array as expected
                        BitValues.Clear();
                    }


                }
            }



            // Property is not writeable only if "read" qualifier is present and its value is "true"
            // Also, for dynamic classes, absence of "write" qualifier means that the property is read-only.
            if ((!bDynamicClass && !hasWrite) ||
                (!bDynamicClass && hasWrite && writeValue) ||
                (bDynamicClass && hasWrite && writeValue))
            {
                bWrite = true;
            }

            return description;
        }
        /// <summary>
        /// This function will generate enums corresponding to the Values/Valuemap pair
        /// and for the BitValues/Bitmap pair.
        /// </summary>
        /// <returns>
        /// returns if the property is an enum. This is checked by if enum is added or not
        /// </returns>
        private bool GeneratePropertyHelperEnums(PropertyData prop, string strPropertyName, bool bNullable)
        {
            bool isEnumAdded = false;
            bool bZeroFieldInEnum = false;

            //Only if the property is of type int and there is atleast values qualifier on it
            //then we will generate an enum for the values/valuemap(if available)
            //Here we don't have to check explicitly for type of the property as the size of
            //values array will be zero if the type is not int.

            string strEnum = ResolveCollision(strPropertyName + "Values", true);

            // if there is a mismatch in the number of values and ValueMaps then
            // there is an error in the value maps and so don't add enum

            if (Values.Count > 0 && (ValueMap.Count == 0 || ValueMap.Count == Values.Count))
            {
                if (ValueMap.Count == 0)
                {
                    bZeroFieldInEnum = true;
                }

                //Now we will have to create an enum.

                EnumObj = new CodeTypeDeclaration(strEnum);

                //Now convert the type to the generated enum type

                if (prop.IsArray)
                {
                    cmp.Type = new CodeTypeReference(strEnum, 1);
                }
                else
                {
                    cmp.Type = new CodeTypeReference(strEnum);
                }

                EnumObj.IsEnum = true;
                EnumObj.TypeAttributes = TypeAttributes.Public;
                long maxValue = 0;

                for (int i = 0; i < Values.Count; i++)
                {
                    cmf = new CodeMemberField();
                    cmf.Name = Values[i].ToString();

                    if (ValueMap.Count > 0)
                    {
                        cmf.InitExpression = new CodePrimitiveExpression(ValueMap[i]);
                        long test = System.Convert.ToInt64(ValueMap[i], (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(ulong)));
                        if (test > maxValue) maxValue = test;

                        if (bZeroFieldInEnum == false)
                        {
                            if (System.Convert.ToInt64(
                                ValueMap[i],
                                (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(
                                typeof(ulong))) == 0)
                            {
                                bZeroFieldInEnum = true;
                            }
                        }
                    }
                    else
                    {
                        cmf.InitExpression = new CodePrimitiveExpression(i);
                        if (i > maxValue) maxValue = i;
                    }
                    EnumObj.Members.Add(cmf);
                }

                // If there is no 0 valued field in enum, just add a invalid for enum
                // This is just to show in property browser

                if (bNullable && !bZeroFieldInEnum)
                {
                    // use the 0 enum position for NULL
                    cmf = new CodeMemberField();
                    cmf.Name = "NULL_ENUM_VALUE";
                    cmf.InitExpression = new CodePrimitiveExpression(0);
                    EnumObj.Members.Add(cmf);
                    prop.NullEnumValue = 0;
                }
                else if (bNullable && bZeroFieldInEnum)
                {
                    // must create an entry for NULL that is not zero and is not used
                    // use the another unused enum position for NULL
                    cmf = new CodeMemberField();
                    cmf.Name = "NULL_ENUM_VALUE";
                    cmf.InitExpression = new CodePrimitiveExpression((int)(maxValue + 1));
                    EnumObj.Members.Add(cmf);
                    prop.NullEnumValue = (int)(maxValue + 1);
                }
                else if (!bNullable && !bZeroFieldInEnum)
                {
                    // add an entry for 0 valued enum
                    cmf = new CodeMemberField();
                    cmf.Name = "INVALID_ENUM_VALUE";
                    cmf.InitExpression = new CodePrimitiveExpression(0);
                    EnumObj.Members.Add(cmf);
                    prop.NullEnumValue = 0;
                }

                cc.Members.Add(EnumObj);
                isEnumAdded = true;
            }

            //Now clear the Values & ValueMap Array

            Values.Clear();
            ValueMap.Clear();

            bZeroFieldInEnum = false;

            //Only if the property is of type int and there is atleast values qualifier on it
            //then we will generate an enum for the values/valuemap(if available)
            //Here we don't have to check explicitly for type of the property as the size of
            //values array will be zero if the type is not int.

            if (BitValues.Count > 0 && (BitMap.Count == 0 || BitMap.Count == BitValues.Count))
            {
                if (BitMap.Count == 0)
                {
                    bZeroFieldInEnum = true;
                }

                //Now we will create the enum

                EnumObj = new CodeTypeDeclaration(strEnum);

                //Now convert the type to the generated enum type

                if (prop.IsArray)
                {
                    cmp.Type = new CodeTypeReference(strEnum, 1);
                }
                else
                {
                    cmp.Type = new CodeTypeReference(strEnum);
                }

                EnumObj.IsEnum = true;
                EnumObj.TypeAttributes = TypeAttributes.Public;
                int bitValue = 1;
                long maxBitValue = 0;

                for (int i = 0; i < BitValues.Count; i++)
                {
                    cmf = new CodeMemberField();
                    cmf.Name = BitValues[i].ToString();
                    if (BitMap.Count > 0)
                    {
                        cmf.InitExpression = new CodePrimitiveExpression(BitMap[i]);
                        long test = System.Convert.ToInt64(BitMap[i], (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(ulong)));
                        if (test > maxBitValue) maxBitValue = test;
                    }
                    else
                    {
                        cmf.InitExpression = new CodePrimitiveExpression(bitValue);
                        if (bitValue > maxBitValue) maxBitValue = bitValue;

                        // Now shift 1 more bit so that we can put it for the
                        // next element in the enum

                        bitValue <<= 1;
                    }

                    if (bZeroFieldInEnum == false)
                    {
                        if ((System.Convert.ToInt64(BitMap[i], (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(ulong))) == 0))
                        {
                            bZeroFieldInEnum = true;
                        }
                    }
                    EnumObj.Members.Add(cmf);
                }

                // If there is no 0 valued field in enum, just add a invalid for enum
                // This is just to show in property browser

                if (bNullable && !bZeroFieldInEnum)
                {
                    // use the 0 enum position for NULL
                    cmf = new CodeMemberField();
                    cmf.Name = "NULL_ENUM_VALUE";
                    cmf.InitExpression = new CodePrimitiveExpression(0);
                    EnumObj.Members.Add(cmf);
                    prop.NullEnumValue = 0;
                }
                else if (bNullable && bZeroFieldInEnum)
                {
                    // must create an entry for NULL that is not zero and is not used
                    // use the another unused enum position for NULL
                    cmf = new CodeMemberField();
                    cmf.Name = "NULL_ENUM_VALUE";
                    if (BitValues.Count > 30)
                    {
                        maxBitValue++;
                    }
                    else
                    {
                        maxBitValue <<= 1;
                    }
                    cmf.InitExpression = new CodePrimitiveExpression((int)(maxBitValue));
                    EnumObj.Members.Add(cmf);
                    // just add one - we won't preserve the bit shifting but this won't be used in CIM anyway.
                    prop.NullEnumValue = (int)(maxBitValue);

                }
                else if (!bNullable && !bZeroFieldInEnum)
                {
                    // add an entry for 0 valued enum
                    cmf = new CodeMemberField();
                    cmf.Name = "INVALID_ENUM_VALUE";
                    cmf.InitExpression = new CodePrimitiveExpression(0);
                    EnumObj.Members.Add(cmf);
                    prop.NullEnumValue = 0;
                }

                cc.Members.Add(EnumObj);
                isEnumAdded = true;
            }
            //Now clear the Bitmap and BitValues Array
            BitValues.Clear();
            BitMap.Clear();
            return isEnumAdded;

        }
        /// <summary>
        /// This function generated the static function which s used to construct the path
        ///     private static String ConstructPath(String keyName)
        ///        {
        ///            //FOR NON SINGLETON CLASSES
        ///            String strPath;
        ///            strPath = ((("\\&lt;defNamespace&gt;:&lt;defClassName&gt;";
        ///            strPath = ((_strPath) + (((".Key1=") + (key_Key1))));
        ///            strPath = ((_strPath) + (((",Key2=") + ((("\"") + (((key_Key2) + ("\""))))))));
        ///            return strPath;
        ///
        ///            //FOR SINGLETON CLASS
        ///            return "\\&lt;defNameSpace&gt;:&lt;defClassName&gt;=@";
        ///        }
        /// </summary>
        private void GenerateConstructPath()
        {
            string strType;
            cmm = new CodeMemberMethod();
            cmm.Name = PublicNamesUsed["ConstructPathFunction"].ToString();
            cmm.Attributes = MemberAttributes.Private | MemberAttributes.Static | MemberAttributes.Final;
            cmm.ReturnType = new CodeTypeReference("System.String");

            for (int i = 0; i < arrKeys.Count; i++)
            {
                strType = ((CodeTypeReference)arrKeyType[i]).BaseType;
                cmm.Parameters.Add(new CodeParameterDeclarationExpression(strType,
                    "key" + arrKeys[i].ToString()));
            }

            string strPath = OriginalNamespace + ":" + OriginalClassName;
            if (bSingletonClass)
            {
                strPath += "=@";
                cmm.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(strPath)));
            }
            else
            {
                string strPathObject = "strPath";
                //Declare the String strPath;
                cmm.Statements.Add(new CodeVariableDeclarationStatement("System.String", strPathObject, new CodePrimitiveExpression(strPath)));
                CodeMethodInvokeExpression cmietoAdd;

                for (int i = 0; i < arrKeys.Count; i++)
                {
                    if (((CodeTypeReference)arrKeyType[i]).BaseType == "System.String")
                    {
                        CodeMethodInvokeExpression cmie1 = GenerateConcatStrings(new CodeVariableReferenceExpression("key" + arrKeys[i]), new CodePrimitiveExpression("\""));

                        CodeMethodInvokeExpression cmie2 = GenerateConcatStrings(new CodePrimitiveExpression("\""), cmie1);

                        CodeMethodInvokeExpression cmie3 = GenerateConcatStrings(new CodePrimitiveExpression(((i == 0) ? ("." + arrKeys[i] + "=") : ("," + arrKeys[i] + "="))), cmie2);

                        cmietoAdd = GenerateConcatStrings(new CodeVariableReferenceExpression(strPathObject), cmie3);

                    }
                    else
                    {
                        cmie = new CodeMethodInvokeExpression();
                        cmie.Method.TargetObject = new CodeCastExpression(new CodeTypeReference(((CodeTypeReference)arrKeyType[i]).BaseType + " "), new CodeVariableReferenceExpression("key" + arrKeys[i]));
                        cmie.Method.MethodName = "ToString";

                        CodeMethodInvokeExpression cmie1 = GenerateConcatStrings(new CodePrimitiveExpression(((i == 0) ? ("." + arrKeys[i] + "=") : ("," + arrKeys[i] + "="))), cmie);

                        cmietoAdd = GenerateConcatStrings(new CodeVariableReferenceExpression(strPathObject), cmie1);

                    }
                    cmm.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(strPathObject), cmietoAdd));
                }
                cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(strPathObject)));
            }
            cc.Members.Add(cmm);
        }
        /// <summary>
        /// This function generates the default constructor.
        /// public Cons() {
        ///        _privObject = new ManagementObject();
        ///     _privSystemProps = new ManagementSystemProperties(_privObject);
        /// }
        /// </summary>
        private void GenerateDefaultConstructor()
        {
            cctor = new CodeConstructor();
            cctor.Attributes = MemberAttributes.Public;

            CodeMethodInvokeExpression cmieInit = new CodeMethodInvokeExpression();
            cmieInit.Method.MethodName = PrivateNamesUsed["InitialObjectFunc"].ToString();
            cmieInit.Method.TargetObject = new CodeThisReferenceExpression();

            cmieInit.Parameters.Add(new CodePrimitiveExpression(null));
            //If it is a singleton class, then we will make the default constructor to point to the
            //only object available
            if (bSingletonClass)
            {
                cmie = new CodeMethodInvokeExpression();
                cmie.Method.TargetObject = new CodeTypeReferenceExpression(PrivateNamesUsed["GeneratedClassName"].ToString());
                cmie.Method.MethodName = PublicNamesUsed["ConstructPathFunction"].ToString();

                coce = new CodeObjectCreateExpression();
                coce.CreateType = new CodeTypeReference(PublicNamesUsed["PathClass"].ToString());
                coce.Parameters.Add(cmie);

                cmieInit.Parameters.Add(coce);
            }
            else
            {
                cmieInit.Parameters.Add(new CodePrimitiveExpression(null));
            }
            cmieInit.Parameters.Add(new CodePrimitiveExpression(null));

            cctor.Statements.Add(new CodeExpressionStatement(cmieInit));
            cc.Members.Add(cctor);

            cctor.Comments.Add(new CodeCommentStatement(SR.CommentConstructors));
        }
        /// <summary>
        ///This function create the constructor which accepts the key values.
        ///public cons(UInt32 key_Key1, String key_Key2) :this(null,&lt;ClassName&gt;.ConstructPath(&lt;key1,key2&gt;),null) {
        /// }
        ///</summary>
        private void GenerateConstructorWithKeys()
        {
            if (arrKeyType.Count > 0)
            {
                cctor = new CodeConstructor();
                cctor.Attributes = MemberAttributes.Public;

                CodeMethodInvokeExpression cmieInit = new CodeMethodInvokeExpression();
                cmieInit.Method.MethodName = PrivateNamesUsed["InitialObjectFunc"].ToString();
                cmieInit.Method.TargetObject = new CodeThisReferenceExpression();

                for (int i = 0; i < arrKeys.Count; i++)
                {
                    cpde = new CodeParameterDeclarationExpression();
                    cpde.Type = new CodeTypeReference(((CodeTypeReference)arrKeyType[i]).BaseType);
                    cpde.Name = "key" + arrKeys[i].ToString();
                    cctor.Parameters.Add(cpde);
                }

                // if the key of the class maps to "System.Management.ManagementPath" type then add a dummy param
                // to avoid duplicate constructors
                if (cctor.Parameters.Count == 1 && cctor.Parameters[0].Type.BaseType == new CodeTypeReference(PublicNamesUsed["PathClass"].ToString()).BaseType)
                {
                    cpde = new CodeParameterDeclarationExpression();
                    cpde.Type = new CodeTypeReference("System.Object");
                    cpde.Name = "dummyParam";
                    cctor.Parameters.Add(cpde);
                    cctor.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("dummyParam"), new CodePrimitiveExpression(null)));
                }

                cmieInit.Parameters.Add(new CodePrimitiveExpression(null));

                cmie = new CodeMethodInvokeExpression();
                cmie.Method.TargetObject = new CodeTypeReferenceExpression(PrivateNamesUsed["GeneratedClassName"].ToString());
                cmie.Method.MethodName = PublicNamesUsed["ConstructPathFunction"].ToString();

                for (int i = 0; i < arrKeys.Count; i++)
                {
                    cmie.Parameters.Add(new CodeVariableReferenceExpression("key" + arrKeys[i]));
                }

                coce = new CodeObjectCreateExpression();
                coce.CreateType = new CodeTypeReference(PublicNamesUsed["PathClass"].ToString());
                coce.Parameters.Add(cmie);

                cmieInit.Parameters.Add(coce);
                cmieInit.Parameters.Add(new CodePrimitiveExpression(null));
                cctor.Statements.Add(new CodeExpressionStatement(cmieInit));

                cc.Members.Add(cctor);
            }
        }

        /// <summary>
        ///This function create the constructor which accepts a scope and key values.
        ///public cons(ManagementScope scope,UInt32 key_Key1, String key_Key2) :this(scope,&lt;ClassName&gt;.ConstructPath(&lt;key1,key2&gt;),null) {
        /// }
        ///</summary>
        private void GenerateConstructorWithScopeKeys()
        {
            cctor = new CodeConstructor();
            cctor.Attributes = MemberAttributes.Public;
            cctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["ScopeClass"].ToString()), PrivateNamesUsed["ScopeParam"].ToString()));

            CodeMethodInvokeExpression cmieInit = new CodeMethodInvokeExpression();
            cmieInit.Method.MethodName = PrivateNamesUsed["InitialObjectFunc"].ToString();
            cmieInit.Method.TargetObject = new CodeThisReferenceExpression();

            if (arrKeyType.Count > 0)
            {
                for (int i = 0; i < arrKeys.Count; i++)
                {
                    cpde = new CodeParameterDeclarationExpression();
                    cpde.Type = new CodeTypeReference(((CodeTypeReference)arrKeyType[i]).BaseType);
                    cpde.Name = "key" + arrKeys[i].ToString();
                    cctor.Parameters.Add(cpde);
                }

                // if the key of the class maps to "System.Management.ManagementPath" type then add a dummy param
                // to avoid duplicate constructors
                if (cctor.Parameters.Count == 2 && cctor.Parameters[1].Type.BaseType == new CodeTypeReference(PublicNamesUsed["PathClass"].ToString()).BaseType)
                {
                    cpde = new CodeParameterDeclarationExpression();
                    cpde.Type = new CodeTypeReference("System.Object");
                    cpde.Name = "dummyParam";
                    cctor.Parameters.Add(cpde);
                    cctor.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("dummyParam"), new CodePrimitiveExpression(null)));
                }

                cmieInit.Parameters.Add(new CodeCastExpression(
                    new CodeTypeReference(PublicNamesUsed["ScopeClass"].ToString()),
                    new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString())));

                cmie = new CodeMethodInvokeExpression();
                cmie.Method.TargetObject = new CodeTypeReferenceExpression(PrivateNamesUsed["GeneratedClassName"].ToString());
                cmie.Method.MethodName = PublicNamesUsed["ConstructPathFunction"].ToString();

                for (int i = 0; i < arrKeys.Count; i++)
                {
                    cmie.Parameters.Add(new CodeVariableReferenceExpression("key" + arrKeys[i]));
                }

                coce = new CodeObjectCreateExpression();
                coce.CreateType = new CodeTypeReference(PublicNamesUsed["PathClass"].ToString());
                coce.Parameters.Add(cmie);
                cmieInit.Parameters.Add(coce);
                cmieInit.Parameters.Add(new CodePrimitiveExpression(null));
                cctor.Statements.Add(new CodeExpressionStatement(cmieInit));

                cc.Members.Add(cctor);
            }
        }


        /// <summary>
        /// This function generates code for the constructor which accepts ManagementPath as the parameter.
        /// The generated code will look something like this
        ///        public Cons(ManagementPath path) : this (null, path,null){
        ///        }
        /// </summary>
        private void GenerateConstructorWithPath()
        {
            string strPathObject = "path";
            cctor = new CodeConstructor();
            cctor.Attributes = MemberAttributes.Public;


            cpde = new CodeParameterDeclarationExpression();
            cpde.Type = new CodeTypeReference(PublicNamesUsed["PathClass"].ToString());
            cpde.Name = strPathObject;
            cctor.Parameters.Add(cpde);

            CodeMethodInvokeExpression cmieInit = new CodeMethodInvokeExpression();
            cmieInit.Method.MethodName = PrivateNamesUsed["InitialObjectFunc"].ToString();
            cmieInit.Method.TargetObject = new CodeThisReferenceExpression();

            cmieInit.Parameters.Add(new CodePrimitiveExpression(null));
            cmieInit.Parameters.Add(new CodeVariableReferenceExpression(strPathObject));
            cmieInit.Parameters.Add(new CodePrimitiveExpression(null));
            cctor.Statements.Add(new CodeExpressionStatement(cmieInit));

            cc.Members.Add(cctor);


        }
        /// <summary>
        /// This function generates code for the constructor which accepts ManagementPath and GetOptions
        /// as parameters.
        /// The generated code will look something like this
        ///        public Cons(ManagementPath path, ObjectGetOptions options) : this (null, path,options){
        ///        }
        /// </summary>
        private void GenerateConstructorWithPathOptions()
        {
            string strPathObject = "path";
            string strGetOptions = "getOptions";

            cctor = new CodeConstructor();
            cctor.Attributes = MemberAttributes.Public;
            cctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["PathClass"].ToString()), strPathObject));
            cctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["GetOptionsClass"].ToString()), strGetOptions));

            CodeMethodInvokeExpression cmieInit = new CodeMethodInvokeExpression();
            cmieInit.Method.MethodName = PrivateNamesUsed["InitialObjectFunc"].ToString();
            cmieInit.Method.TargetObject = new CodeThisReferenceExpression();

            cmieInit.Parameters.Add(new CodePrimitiveExpression(null));
            cmieInit.Parameters.Add(new CodeVariableReferenceExpression(strPathObject));
            cmieInit.Parameters.Add(new CodeVariableReferenceExpression(strGetOptions));
            cctor.Statements.Add(new CodeExpressionStatement(cmieInit));

            cc.Members.Add(cctor);
        }
        /// <summary>
        /// This function generates code for the constructor which accepts Scope as a string, path as a
        /// string and GetOptions().
        /// The generated code will look something like this
        ///        public Cons(String scope, String path, ObjectGetOptions options) :
        ///                            this (new ManagementScope(scope), new ManagementPath(path),options){
        ///        }
        /// </summary>
        private void GenerateConstructorWithScopePath()
        {
            string strPathObject = "path";

            cctor = new CodeConstructor();
            cctor.Attributes = MemberAttributes.Public;
            cctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["ScopeClass"].ToString()), PrivateNamesUsed["ScopeParam"].ToString()));
            cctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["PathClass"].ToString()), strPathObject));

            CodeMethodInvokeExpression cmieInit = new CodeMethodInvokeExpression();
            cmieInit.Method.MethodName = PrivateNamesUsed["InitialObjectFunc"].ToString();
            cmieInit.Method.TargetObject = new CodeThisReferenceExpression();

            cmieInit.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()));
            cmieInit.Parameters.Add(new CodeVariableReferenceExpression(strPathObject));
            cmieInit.Parameters.Add(new CodePrimitiveExpression(null));
            cctor.Statements.Add(new CodeExpressionStatement(cmieInit));

            cc.Members.Add(cctor);
        }
        /// <summary>
        /// This function generates code for the constructor which accepts ManagementScope as parameters.
        /// The generated code will look something like this
        ///        public Cons(ManagementScope scope, ObjectGetOptions options) : this (scope, &lt;ClassName&gt;.ConstructPath(),null){
        ///        }
        /// </summary>
        private void GenerateConstructorWithScope()
        {

            cctor = new CodeConstructor();
            cctor.Attributes = MemberAttributes.Public;
            cctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["ScopeClass"].ToString()),
                PrivateNamesUsed["ScopeParam"].ToString()));

            CodeMethodInvokeExpression cmieInit = new CodeMethodInvokeExpression();
            cmieInit.Method.MethodName = PrivateNamesUsed["InitialObjectFunc"].ToString();
            cmieInit.Method.TargetObject = new CodeThisReferenceExpression();

            cmieInit.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()));
            cmie = new CodeMethodInvokeExpression();
            cmie.Method.TargetObject = new CodeTypeReferenceExpression(PrivateNamesUsed["GeneratedClassName"].ToString());
            cmie.Method.MethodName = PublicNamesUsed["ConstructPathFunction"].ToString();

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["PathClass"].ToString());
            coce.Parameters.Add(cmie);

            cmieInit.Parameters.Add(coce);

            cmieInit.Parameters.Add(new CodePrimitiveExpression(null));
            cctor.Statements.Add(new CodeExpressionStatement(cmieInit));

            cc.Members.Add(cctor);
        }

        /// <summary>
        /// This function generates code for the constructor which accepts GetOptions
        /// as parameters.
        /// The generated code will look something like this
        ///        public Cons(ObjectGetOptions options) : this (null, &lt;ClassName&gt;.ConstructPath(),options){
        ///        }
        /// </summary>
        private void GenerateConstructorWithOptions()
        {
            string strGetOptions = "getOptions";

            cctor = new CodeConstructor();
            cctor.Attributes = MemberAttributes.Public;
            cctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["GetOptionsClass"].ToString()),
                strGetOptions));

            CodeMethodInvokeExpression cmieInit = new CodeMethodInvokeExpression();
            cmieInit.Method.MethodName = PrivateNamesUsed["InitialObjectFunc"].ToString();
            cmieInit.Method.TargetObject = new CodeThisReferenceExpression();

            cmieInit.Parameters.Add(new CodePrimitiveExpression(null));
            cmie = new CodeMethodInvokeExpression();
            cmie.Method.TargetObject = new CodeTypeReferenceExpression(PrivateNamesUsed["GeneratedClassName"].ToString());
            cmie.Method.MethodName = PublicNamesUsed["ConstructPathFunction"].ToString();

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["PathClass"].ToString());
            coce.Parameters.Add(cmie);

            cmieInit.Parameters.Add(coce);
            cmieInit.Parameters.Add(new CodeVariableReferenceExpression(strGetOptions));
            cctor.Statements.Add(new CodeExpressionStatement(cmieInit));
            cc.Members.Add(cctor);
        }

        /// <summary>
        /// This function generates code for the constructor which accepts ManagementScope and GetOptions
        /// as parameters.
        /// The generated code will look something like this
        ///        public Cons(ManagementScope scope, ObjectGetOptions options) : this (scope, &lt;ClassName&gt;.ConstructPath(),options){
        ///        }
        /// </summary>
        private void GenerateConstructorWithScopeOptions()
        {
            string strGetOptions = "getOptions";

            cctor = new CodeConstructor();
            cctor.Attributes = MemberAttributes.Public;
            cctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["ScopeClass"].ToString()),
                PrivateNamesUsed["ScopeParam"].ToString()));
            cctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["GetOptionsClass"].ToString()),
                strGetOptions));



            CodeMethodInvokeExpression cmieInit = new CodeMethodInvokeExpression();
            cmieInit.Method.MethodName = PrivateNamesUsed["InitialObjectFunc"].ToString();
            cmieInit.Method.TargetObject = new CodeThisReferenceExpression();

            cmieInit.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()));

            cmie = new CodeMethodInvokeExpression();
            cmie.Method.TargetObject = new CodeTypeReferenceExpression(PrivateNamesUsed["GeneratedClassName"].ToString());
            cmie.Method.MethodName = PublicNamesUsed["ConstructPathFunction"].ToString();

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["PathClass"].ToString());
            coce.Parameters.Add(cmie);

            cmieInit.Parameters.Add(coce);

            cmieInit.Parameters.Add(new CodeVariableReferenceExpression(strGetOptions));
            cctor.Statements.Add(new CodeExpressionStatement(cmieInit));
            cc.Members.Add(cctor);
        }


        /// <summary>
        /// This function generated the constructor like
        ///        public cons(ManagementScope scope, ManagamentPath path,ObjectGetOptions getOptions)
        ///        {
        ///            PrivateObject = new ManagementObject(scope,path,getOptions);
        ///            PrivateSystemProperties = new ManagementSystemProperties(PrivateObject);
        ///        }
        /// </summary>
        private void GenerateConstructorWithScopePathOptions()
        {
            string strPathObject = "path";
            string strGetOptions = "getOptions";
            /*            bool bPrivileges = true;

                        try
                        {
                            classobj.Qualifiers["privileges"].ToString();
                        }
                        catch (ManagementException e)
                        {
                            if (e.ErrorCode == ManagementStatus.NotFound)
                            {
                                bPrivileges = false;
                            }
                            else
                            {
                                throw;
                            }
                        }
            */
            cctor = new CodeConstructor();
            cctor.Attributes = MemberAttributes.Public;
            cctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["ScopeClass"].ToString()), PrivateNamesUsed["ScopeParam"].ToString()));
            cctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["PathClass"].ToString()), strPathObject));
            cctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["GetOptionsClass"].ToString()), strGetOptions));

            CodeMethodInvokeExpression cmieInit = new CodeMethodInvokeExpression();
            cmieInit.Method.MethodName = PrivateNamesUsed["InitialObjectFunc"].ToString();
            cmieInit.Method.TargetObject = new CodeThisReferenceExpression();

            cmieInit.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()));
            cmieInit.Parameters.Add(new CodeVariableReferenceExpression(strPathObject));
            cmieInit.Parameters.Add(new CodeVariableReferenceExpression(strGetOptions));
            cctor.Statements.Add(new CodeExpressionStatement(cmieInit));

            cc.Members.Add(cctor);
        }
        /// <summary>
        /// This function generates code for the constructor which accepts ManagementObject as the parameter.
        /// The generated code will look something like this
        ///        public Cons(ManagementObject theObject) {
        ///        if (CheckIfProperClass(theObject.Scope, theObject.Path, theObject.Options) = true) {
        ///                privObject = theObject;
        ///                privSystemProps = new WmiSystemProps(privObject);
        ///                curObj = privObject;
        ///            }
        ///            else {
        ///                throw new ArgumentException("Class name doesn't match");
        ///            }
        ///        }
        /// </summary>
        private void GenarateConstructorWithLateBound()
        {
            string strLateBoundObject = "theObject";
            string LateBoundSystemProperties = "SystemProperties";

            cctor = new CodeConstructor();
            cctor.Attributes = MemberAttributes.Public;
            cpde = new CodeParameterDeclarationExpression();
            cpde.Type = new CodeTypeReference(PublicNamesUsed["LateBoundClass"].ToString());
            cpde.Name = strLateBoundObject;
            cctor.Parameters.Add(cpde);

            // call this to call function to initialize member variables
            InitPrivateMemberVariables(cctor);

            cis = new CodeConditionStatement();
            cpre = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(strLateBoundObject), LateBoundSystemProperties);
            cie = new CodeIndexerExpression(cpre, new CodePrimitiveExpression("__CLASS"));
            cpre = new CodePropertyReferenceExpression(cie, "Value");


            cmie = new CodeMethodInvokeExpression();
            cmie.Method.MethodName = PrivateNamesUsed["ClassNameCheckFunc"].ToString();
            cmie.Parameters.Add(new CodeVariableReferenceExpression(strLateBoundObject));

            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = cmie;
            cboe.Right = new CodePrimitiveExpression(true);
            cboe.Operator = CodeBinaryOperatorType.ValueEquality;
            cis.Condition = cboe;

            cis.TrueStatements.Add(new CodeAssignStatement(
                new CodeVariableReferenceExpression(PrivateNamesUsed["LateBoundObject"].ToString()),
                new CodeVariableReferenceExpression(strLateBoundObject)));

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["SystemPropertiesClass"].ToString());
            coce.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["LateBoundObject"].ToString()));
            cis.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(PrivateNamesUsed["SystemPropertiesObject"].ToString()), coce));
            cis.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(PrivateNamesUsed["CurrentObject"].ToString()),
                new CodeVariableReferenceExpression(PrivateNamesUsed["LateBoundObject"].ToString())));

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["ArgumentExceptionClass"].ToString());
            coce.Parameters.Add(new CodePrimitiveExpression(SR.ClassNameNotFoundException));
            cis.FalseStatements.Add(new CodeThrowExceptionStatement(coce));


            cctor.Statements.Add(cis);
            cc.Members.Add(cctor);
        }


        /// <summary>
        /// This function generates code for the constructor which accepts ManagementObject as the parameter.
        /// The generated code will look something like this
        ///        public Cons(ManagementBaseObject theObject) {
        ///        if (CheckIfProperClass(theObject) = true)
        ///        {
        ///            embeddedObj = theObject
        ///            PrivateSystemProperties = New ManagementSystemProperties(theObject)
        ///            curObj = embeddedObj
        ///            isEmbedded = true
        ///        }
        ///        else
        ///        {
        ///            throw new ArgumentException("Class name doesn't match");
        ///        }
        ///    }
        ///
        /// </summary>
        private void GenarateConstructorWithLateBoundForEmbedded()
        {
            string strLateBoundObject = "theObject";

            cctor = new CodeConstructor();
            cctor.Attributes = MemberAttributes.Public;
            cpde = new CodeParameterDeclarationExpression();
            cpde.Type = new CodeTypeReference(PublicNamesUsed["BaseObjClass"].ToString());
            cpde.Name = strLateBoundObject;
            cctor.Parameters.Add(cpde);

            // call this to call function to initialize member variables
            InitPrivateMemberVariables(cctor);

            cmie = new CodeMethodInvokeExpression();
            cmie.Method.MethodName = PrivateNamesUsed["ClassNameCheckFunc"].ToString();


            cmie.Parameters.Add(new CodeVariableReferenceExpression(strLateBoundObject));

            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = cmie;
            cboe.Right = new CodePrimitiveExpression(true);
            cboe.Operator = CodeBinaryOperatorType.ValueEquality;
            cis = new CodeConditionStatement();
            cis.Condition = cboe;

            cis.TrueStatements.Add(new CodeAssignStatement(
                new CodeVariableReferenceExpression(PrivateNamesUsed["EmbeddedObject"].ToString()),
                new CodeVariableReferenceExpression(strLateBoundObject)));

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["SystemPropertiesClass"].ToString());
            coce.Parameters.Add(new CodeVariableReferenceExpression(strLateBoundObject));
            cis.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(PrivateNamesUsed["SystemPropertiesObject"].ToString()), coce));
            cis.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(PrivateNamesUsed["CurrentObject"].ToString()),
                new CodeVariableReferenceExpression(PrivateNamesUsed["EmbeddedObject"].ToString())));

            cis.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(PrivateNamesUsed["IsEmbedded"].ToString()),
                new CodePrimitiveExpression(true)));

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["ArgumentExceptionClass"].ToString());
            coce.Parameters.Add(new CodePrimitiveExpression(SR.ClassNameNotFoundException));
            cis.FalseStatements.Add(new CodeThrowExceptionStatement(coce));


            cctor.Statements.Add(cis);
            cc.Members.Add(cctor);
        }



        /// <summary>
        /// This function generated the constructor like
        ///        public cons(ManagementScope scope, ManagamentPath path,ObjectGetOptions getOptions)
        ///        {
        ///            PrivateObject = new ManagementObject(scope,path,getOptions);
        ///            PrivateSystemProperties = new ManagementSystemProperties(PrivateObject);
        ///        }
        /// </summary>
        private void GenerateInitializeObject()
        {
            string strPathObject = "path";
            string strGetOptions = "getOptions";
            bool bPrivileges = true;

            try
            {
                classobj.Qualifiers["privileges"].ToString();
            }
            catch (ManagementException e)
            {
                if (e.ErrorCode == ManagementStatus.NotFound)
                {
                    bPrivileges = false;
                }
                else
                {
                    throw;
                }
            }

            CodeMemberMethod cmmInit = new CodeMemberMethod();
            cmmInit.Name = PrivateNamesUsed["InitialObjectFunc"].ToString();
            cmmInit.Attributes = MemberAttributes.Private | MemberAttributes.Final;

            cmmInit.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["ScopeClass"].ToString()), PrivateNamesUsed["ScopeParam"].ToString()));
            cmmInit.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["PathClass"].ToString()), strPathObject));
            cmmInit.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["GetOptionsClass"].ToString()), strGetOptions));

            // call this to call function to initialize member variables
            InitPrivateMemberVariables(cmmInit);

            //First if path is not null, then we will check whether the class name is the same.
            //if it is not the same, then we will throw an exception
            cis = new CodeConditionStatement();
            cis.Condition = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression(strPathObject),
                CodeBinaryOperatorType.IdentityInequality,
                new CodePrimitiveExpression(null));
            CodeConditionStatement cis1 = new CodeConditionStatement();

            cmie = new CodeMethodInvokeExpression();
            cmie.Method.MethodName = PrivateNamesUsed["ClassNameCheckFunc"].ToString();


            cmie.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(strPathObject));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(strGetOptions));

            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = cmie;
            cboe.Right = new CodePrimitiveExpression(true);
            cboe.Operator = CodeBinaryOperatorType.IdentityInequality;
            cis1.Condition = cboe;
            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["ArgumentExceptionClass"].ToString());
            coce.Parameters.Add(new CodePrimitiveExpression(SR.ClassNameNotFoundException));
            cis1.TrueStatements.Add(new CodeThrowExceptionStatement(coce));

            cis.TrueStatements.Add(cis1);
            cmmInit.Statements.Add(cis);

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["LateBoundClass"].ToString());
            coce.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()));
            coce.Parameters.Add(new CodeVariableReferenceExpression(strPathObject));
            coce.Parameters.Add(new CodeVariableReferenceExpression(strGetOptions));
            cmmInit.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(
                PrivateNamesUsed["LateBoundObject"].ToString()),
                coce));
            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["SystemPropertiesClass"].ToString());
            coce.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["LateBoundObject"].ToString()));
            cmmInit.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(
                PrivateNamesUsed["SystemPropertiesObject"].ToString()),
                coce));

            cmmInit.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(PrivateNamesUsed["CurrentObject"].ToString()),
                new CodeVariableReferenceExpression(PrivateNamesUsed["LateBoundObject"].ToString())));
            cc.Members.Add(cmmInit);
            // Enable the privileges if the class has privileges qualifier
            if (bPrivileges)
            {
                //Generate the statement
                //    Boolean bPrivileges = PrivateLateBoundObject.Scope.Options.EnablePrivileges;
                cpre = new CodePropertyReferenceExpression(new CodePropertyReferenceExpression(
                    new CodePropertyReferenceExpression(
                    new CodeVariableReferenceExpression(PrivateNamesUsed["LateBoundObject"].ToString()),
                    PublicNamesUsed["ScopeProperty"].ToString()),
                    "Options"),
                    "EnablePrivileges");

                cctor.Statements.Add(new CodeAssignStatement(cpre, new CodePrimitiveExpression(true)));

            }


        }

        /// <summary>
        /// This function generates the WMI methods as the methods in the generated class.
        /// The generated code will look something like this
        ///        public &lt;retType&gt; Method1(&lt;type&gt; param1, &lt;type&gt; param2,...) {
        ///            ManagementBaseObject inParams = null;
        ///            inParams = _privObject.GetMethodParameters("ChangeStartMode");
        ///            inParams["&lt;inparam1&gt;"] = &lt;Value&gt;;
        ///            inParams["&lt;inoutparam2&gt;"] = &lt;Value&gt;;
        ///            ................................
        ///            ManagementBaseObject outParams = _privObject.InvokeMethod("ChangeStartMode", inParams, null);
        ///            inoutParam3 = (&lt;type&gt;)(outParams.Properties["&lt;inoutParam3&gt;"]);
        ///            outParam4 = (String)(outParams.Properties["&lt;outParam4&gt;"]);
        ///            ................................
        ///            return (&lt;retType&gt;)(outParams.Properties["ReturnValue"].Value);
        ///     }
        ///
        ///     The code generated changes if the method is static function
        ///        public &lt;retType&gt; Method1(&lt;type&gt; param1, &lt;type&gt; param2,...) {
        ///            ManagementBaseObject inParams = null;
        ///            ManagementObject classObj = new ManagementObject(null, "WIN32_SHARE", null); // the classname
        ///            inParams = classObj.GetMethodParameters("Create");
        ///            inParams["&lt;inparam1&gt;"] = &lt;Value&gt;;
        ///            inParams["&lt;inoutparam2&gt;"] = &lt;Value&gt;;
        ///            ................................
        ///            ManagementBaseObject outParams = classObj.InvokeMethod("ChangeStartMode", inParams, null);
        ///            inoutParam3 = (&lt;type&gt;)(outParams.Properties["&lt;inoutParam3&gt;"]);
        ///            outParam4 = (String)(outParams.Properties["&lt;outParam4&gt;"]);
        ///            ................................
        ///            return (&lt;retType&gt;)(outParams.Properties["ReturnValue"].Value);
        ///     }
        ///
        /// </summary>
        private void GenerateMethods()
        {
            string strInParams = "inParams";
            string strOutParams = "outParams";
            string strClassObj = "classObj";
            bool bStatic = false;
            bool bPrivileges = false;
            CodePropertyReferenceExpression cprePrivileges = null;
            CimType cimRetType = CimType.SInt8;                        // Initialized to remove warnings
            CodeTypeReference retRefType = null;
            bool isRetArray = false;
            bool bIsCimDateTimeInterval = false;

            ArrayList outParamsName = new ArrayList(5);
            ArrayList inoutParams = new ArrayList(5);
            ArrayList inoutParamsType = new ArrayList(5);
            for (int k = 0; k < PublicMethods.Count; k++)
            {

                bStatic = false;
                MethodData meth = classobj.Methods[PublicMethods.GetKey(k).ToString()];
                string strTemp = PrivateNamesUsed["LateBoundObject"].ToString();
                if (meth.OutParameters != null)
                {
                    if (meth.OutParameters.Properties != null)
                    {
                        //First Populate the out Params name so that we can find in/out parameters
                        foreach (PropertyData prop in meth.OutParameters.Properties)
                        {
                            outParamsName.Add(prop.Name);
                        }
                    }
                }
                cmm = new CodeMemberMethod();
                cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                cmm.Name = PublicMethods[meth.Name].ToString();

                //Check if the method is static
                foreach (QualifierData q in meth.Qualifiers)
                {
                    if (string.Equals(q.Name, "static", StringComparison.OrdinalIgnoreCase))
                    {
                        //It is a static function
                        cmm.Attributes |= MemberAttributes.Static;
                        bStatic = true;
                        break;
                    }
                    else
                        if (string.Equals(q.Name, "privileges", StringComparison.OrdinalIgnoreCase))
                    {
                        //It is a function which needs privileges to be set
                        bPrivileges = true;
                    }
                }

                // For Static method , the member variable "IsEmbedded" cannot be accessed
                cis = new CodeConditionStatement();
                cboe = new CodeBinaryOperatorExpression();

                if (bStatic)
                {
                    cmm.Statements.Add(new CodeVariableDeclarationStatement("System.Boolean", "IsMethodStatic", new CodePrimitiveExpression(bStatic)));
                    cboe.Left = new CodeVariableReferenceExpression("IsMethodStatic");
                    cboe.Right = new CodePrimitiveExpression(true);
                }
                else
                {
                    cboe.Left = new CodeVariableReferenceExpression(PrivateNamesUsed["IsEmbedded"].ToString());
                    cboe.Right = new CodePrimitiveExpression(false);
                }

                cboe.Operator = CodeBinaryOperatorType.ValueEquality;
                cis.Condition = cboe;

                bool bfirst = true;
                //Generate the statement
                //    ManagementBaseObject inParams = null;
                cis.TrueStatements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(PublicNamesUsed["BaseObjClass"].ToString()),
                    strInParams, new CodePrimitiveExpression(null)));


                if (bStatic)
                {
                    string strPath = "mgmtPath";
                    CodeObjectCreateExpression cocePath = new CodeObjectCreateExpression();
                    cocePath.CreateType = new CodeTypeReference(PublicNamesUsed["PathClass"].ToString());
                    cocePath.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["CreationClassName"].ToString()));
                    cis.TrueStatements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(PublicNamesUsed["PathClass"].ToString()), strPath, cocePath));

                    CodeObjectCreateExpression coce1 = new CodeObjectCreateExpression();
                    coce1.CreateType = new CodeTypeReference(PublicNamesUsed["ManagementClass"].ToString());
                    coce1.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["statMgmtScope"].ToString()));
                    coce1.Parameters.Add(new CodeVariableReferenceExpression(strPath));
                    coce1.Parameters.Add(new CodePrimitiveExpression(null));

                    coce = new CodeObjectCreateExpression();
                    coce.CreateType = new CodeTypeReference(PublicNamesUsed["ManagementClass"].ToString());
                    coce.Parameters.Add(coce1);
                    cis.TrueStatements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(PublicNamesUsed["ManagementClass"].ToString()), strClassObj, coce1));
                    strTemp = strClassObj;
                }

                if (bPrivileges)
                {
                    //Generate the statement
                    //    Boolean bPrivileges = PrivateLateBoundObject.Scope.Options.EnablePrivileges;
                    cprePrivileges = new CodePropertyReferenceExpression(new CodePropertyReferenceExpression(
                        new CodePropertyReferenceExpression(
                        new CodeVariableReferenceExpression(bStatic ? strClassObj : PrivateNamesUsed["LateBoundObject"].ToString()),
                        PublicNamesUsed["ScopeProperty"].ToString()),
                        "Options"),
                        "EnablePrivileges");

                    cis.TrueStatements.Add(new CodeVariableDeclarationStatement("System.Boolean",
                        PrivateNamesUsed["Privileges"].ToString(), cprePrivileges));

                    cis.TrueStatements.Add(new CodeAssignStatement(cprePrivileges, new CodePrimitiveExpression(true)));

                }

                //Do these things only when there is a valid InParameters
                if (meth.InParameters != null)
                {
                    //Now put the in parameters
                    if (meth.InParameters.Properties != null)
                    {
                        foreach (PropertyData prop in meth.InParameters.Properties)
                        {
                            bIsCimDateTimeInterval = false;
                            if (bfirst)
                            {
                                //Now Generate the statement
                                //    inParams = privObject.GetMethodParameters(<MethodName>);
                                cmie = new CodeMethodInvokeExpression(
                                    new CodeVariableReferenceExpression(strTemp),
                                    "GetMethodParameters",
                                    new CodePrimitiveExpression(meth.Name));
                                cis.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(strInParams), cmie));
                                bfirst = false;
                            }

                            cpde = new CodeParameterDeclarationExpression();
                            cpde.Name = prop.Name;
                            cpde.Type = ConvertCIMType(prop.Type, prop.IsArray);
                            cpde.Direction = FieldDirection.In;


                            if (prop.Type == CimType.DateTime)
                            {
                                CodeTypeReference dateType = cpde.Type;
                                // Check if it is Time interval and if so change the type to Time Interval
                                bIsCimDateTimeInterval = GetDateTimeType(prop, ref dateType);
                                cpde.Type = dateType;
                            }

                            //Find out whether it is a in/out Parameter
                            for (int i = 0; i < outParamsName.Count; i++)
                            {
                                if (string.Equals(prop.Name, outParamsName[i].ToString(), StringComparison.OrdinalIgnoreCase))
                                {
                                    //It is an in/out Parameter
                                    cpde.Direction = FieldDirection.Ref;
                                    inoutParams.Add(prop.Name);
                                    inoutParamsType.Add(cpde.Type);
                                }
                            }

                            cmm.Parameters.Add(cpde);
                            //Also generate the statement
                            //inParams["PropName"] = Value;
                            cie = new CodeIndexerExpression(new CodeVariableReferenceExpression(strInParams), new CodePrimitiveExpression(prop.Name));


                            // if the type of the property is CIM_REFERENCE then just get the
                            // path as string set the property to that string
                            if (prop.Type == CimType.Reference)
                            {
                                //Call this function to add code for converting the path to
                                // string and assigning it to parameter
                                AddPropertySet(cie, prop.IsArray, cis.TrueStatements, PublicNamesUsed["PathClass"].ToString(), new CodeVariableReferenceExpression(cpde.Name));
                            }
                            else
                                if (prop.Type == CimType.DateTime)
                            {
                                //Call this function to add code for converting the DateTime,TimeSpan to string
                                // and assigning it to the parameter
                                if (bIsCimDateTimeInterval)
                                {
                                    AddPropertySet(cie, prop.IsArray, cis.TrueStatements, "System.TimeSpan", new CodeVariableReferenceExpression(cpde.Name));
                                }
                                else
                                {
                                    AddPropertySet(cie, prop.IsArray, cis.TrueStatements, "System.DateTime", new CodeVariableReferenceExpression(cpde.Name));
                                }
                            }
                            else
                            {
                                if (cpde.Type.ArrayRank == 0)
                                {
                                    // Work around
                                    cis.TrueStatements.Add(new CodeAssignStatement(cie, new CodeCastExpression(new CodeTypeReference(cpde.Type.BaseType + " "),
                                        new CodeVariableReferenceExpression(cpde.Name))));
                                }
                                else
                                {
                                    cis.TrueStatements.Add(new CodeAssignStatement(cie, new CodeCastExpression(cpde.Type, new CodeVariableReferenceExpression(cpde.Name))));
                                }
                            }
                        }
                    }
                }
                //Now clear the outParamsName array
                outParamsName.Clear();
                bool bInOut;
                bool bRetVal = false;
                bfirst = true;
                bool bInvoke = false;
                CodeMethodInvokeExpression cmie2 = null;

                //Do these only when the outParams is Valid
                if (meth.OutParameters != null)
                {
                    if (meth.OutParameters.Properties != null)
                    {
                        foreach (PropertyData prop in meth.OutParameters.Properties)
                        {
                            bIsCimDateTimeInterval = false;
                            if (bfirst)
                            {
                                //Now generate the statement
                                //    ManagementBaseObject outParams = privObject.InvokeMethod(<methodName>,inParams,options);
                                cmie = new CodeMethodInvokeExpression(
                                    new CodeVariableReferenceExpression(strTemp),
                                    "InvokeMethod");

                                cmie.Parameters.Add(new CodePrimitiveExpression(meth.Name));
                                cmie.Parameters.Add(new CodeVariableReferenceExpression(strInParams));
                                cmie.Parameters.Add(new CodePrimitiveExpression(null));
                                cis.TrueStatements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(PublicNamesUsed["BaseObjClass"].ToString()), strOutParams, cmie));
                                bfirst = false;
                                bInvoke = true;
                            }

                            bInOut = false;
                            for (int i = 0; i < inoutParams.Count; i++)
                            {
                                if (string.Equals(prop.Name, inoutParams[i].ToString(), StringComparison.OrdinalIgnoreCase))
                                {
                                    bInOut = true;
                                }
                            }
                            if (bInOut)
                                continue;

                            if (string.Equals(prop.Name, "ReturnValue", StringComparison.OrdinalIgnoreCase))
                            {
                                cmm.ReturnType = ConvertCIMType(prop.Type, prop.IsArray);
                                bRetVal = true;
                                cimRetType = prop.Type;

                                if (prop.Type == CimType.DateTime)
                                {
                                    CodeTypeReference dateType = cmm.ReturnType;
                                    // Check if it is Time interval and if so change the type to Time Interval
                                    GetDateTimeType(prop, ref dateType);
                                    cmm.ReturnType = dateType;
                                }
                                retRefType = cmm.ReturnType;
                                isRetArray = prop.IsArray;
                            }
                            else
                            {
                                cpde = new CodeParameterDeclarationExpression();
                                cpde.Name = prop.Name;
                                cpde.Type = ConvertCIMType(prop.Type, prop.IsArray);
                                cpde.Direction = FieldDirection.Out;
                                cmm.Parameters.Add(cpde);

                                if (prop.Type == CimType.DateTime)
                                {
                                    CodeTypeReference dateType = cpde.Type;
                                    // Check if it is Time interval and if so change the type to Time Interval
                                    bIsCimDateTimeInterval = GetDateTimeType(prop, ref dateType);
                                    cpde.Type = dateType;
                                }

                                //Now for each out params generate the statement
                                //    <outParam> = outParams.Properties["<outParam>"];
                                cpre = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(strOutParams), "Properties");
                                cie = new CodeIndexerExpression(cpre, new CodePrimitiveExpression(prop.Name));

                                if (prop.Type == CimType.Reference)
                                {
                                    //Call this function to add code for converting string CIM_REFERENCE
                                    // to ManagementPath
                                    GenerateCodeForRefAndDateTimeTypes(cie, prop.IsArray, cis.TrueStatements, PublicNamesUsed["PathClass"].ToString(), new CodeVariableReferenceExpression(prop.Name), true);
                                }
                                else
                                    if (prop.Type == CimType.DateTime)
                                {
                                    //Call this function to add code for converting datetime,TimeSpan in DMTF formate
                                    // to System.DateTime
                                    if (bIsCimDateTimeInterval)
                                    {
                                        GenerateCodeForRefAndDateTimeTypes(cie, prop.IsArray, cis.TrueStatements, "System.TimeSpan", new CodeVariableReferenceExpression(prop.Name), true);
                                    }
                                    else
                                    {
                                        GenerateCodeForRefAndDateTimeTypes(cie, prop.IsArray, cis.TrueStatements, "System.DateTime", new CodeVariableReferenceExpression(prop.Name), true);
                                    }
                                }
                                else
                                {
                                    if (prop.IsArray || prop.Type == CimType.Object)
                                    {
                                        cis.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(prop.Name),
                                            new CodeCastExpression(ConvertCIMType(prop.Type, prop.IsArray),
                                            new CodePropertyReferenceExpression(cie, "Value"))));
                                    }
                                    else
                                    {
                                        cmie2 = new CodeMethodInvokeExpression();
                                        cmie2.Parameters.Add(new CodePropertyReferenceExpression(cie, "Value"));
                                        cmie2.Method.MethodName = GetConversionFunction(prop.Type);
                                        cmie2.Method.TargetObject = new CodeTypeReferenceExpression("System.Convert");

                                        cis.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(prop.Name), cmie2));
                                    }

                                }


                                //Now for each out params generate the statement if it is an embedded instance
                                if (prop.Type == CimType.DateTime && prop.IsArray == false)
                                {
                                    if (bIsCimDateTimeInterval)
                                    {
                                        coce = new CodeObjectCreateExpression();
                                        coce.CreateType = new CodeTypeReference("System.TimeSpan");
                                        coce.Parameters.Add(new CodePrimitiveExpression(0));
                                        coce.Parameters.Add(new CodePrimitiveExpression(0));
                                        coce.Parameters.Add(new CodePrimitiveExpression(0));
                                        coce.Parameters.Add(new CodePrimitiveExpression(0));
                                        coce.Parameters.Add(new CodePrimitiveExpression(0));

                                        cis.FalseStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(prop.Name), coce));
                                    }
                                    else
                                    {
                                        cis.FalseStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(prop.Name),
                                            new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.DateTime"),
                                            "MinValue")));
                                    }

                                }
                                else
                                    if (IsPropertyValueType(prop.Type) && prop.IsArray == false)
                                {
                                    cmie2 = new CodeMethodInvokeExpression();
                                    cmie2.Parameters.Add(new CodePrimitiveExpression(0));
                                    cmie2.Method.MethodName = GetConversionFunction(prop.Type);
                                    cmie2.Method.TargetObject = new CodeTypeReferenceExpression("System.Convert");
                                    cis.FalseStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(prop.Name), cmie2));
                                }
                                else
                                {
                                    cis.FalseStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(prop.Name),
                                        new CodePrimitiveExpression(null)));
                                }

                            }
                        }
                    }
                }

                if (bInvoke == false)
                {
                    //Now there is no out parameters to invoke the function
                    //So just call Invoke.
                    cmie = new CodeMethodInvokeExpression(
                        new CodeVariableReferenceExpression(strTemp),
                        "InvokeMethod"
                        );

                    cmie.Parameters.Add(new CodePrimitiveExpression(meth.Name));
                    cmie.Parameters.Add(new CodeVariableReferenceExpression(strInParams));
                    cmie.Parameters.Add(new CodePrimitiveExpression(null));

                    cmis = new CodeExpressionStatement(cmie);
                    cis.TrueStatements.Add(cmis);
                }

                //Now for each in/out params generate the statement
                //    <inoutParam> = outParams.Properties["<inoutParam>"];
                for (int i = 0; i < inoutParams.Count; i++)
                {
                    cpre = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(strOutParams), "Properties");
                    cie = new CodeIndexerExpression(cpre, new CodePrimitiveExpression(inoutParams[i].ToString()));
                    cis.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(inoutParams[i].ToString()),
                        new CodeCastExpression((CodeTypeReference)inoutParamsType[i],
                        new CodePropertyReferenceExpression(cie, "Value"))));
                }
                inoutParams.Clear();

                // Assign the privileges back
                if (bPrivileges)
                {
                    cis.TrueStatements.Add(new CodeAssignStatement(cprePrivileges, new CodeVariableReferenceExpression(PrivateNamesUsed["Privileges"].ToString())));
                }

                //Now check if there is a return value. If there is one then return it from the function
                if (bRetVal)
                {
                    CodeVariableDeclarationStatement cRetVal = new CodeVariableDeclarationStatement(retRefType, "retVar");
                    cpre = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(strOutParams), "Properties");
                    cie = new CodeIndexerExpression(cpre, new CodePrimitiveExpression("ReturnValue"));

                    if (retRefType.BaseType == new CodeTypeReference(PublicNamesUsed["PathClass"].ToString()).BaseType)
                    {
                        cmm.Statements.Add(cRetVal);
                        cmm.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("retVar"), new CodePrimitiveExpression(null)));

                        //Call this function to add code for converting string CIM_REFERENCE
                        // to ManagementPath and return
                        GenerateCodeForRefAndDateTimeTypes(cie, isRetArray, cis.TrueStatements, PublicNamesUsed["PathClass"].ToString(), new CodeVariableReferenceExpression("retVar"), true);
                        cis.TrueStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("retVar")));
                        cis.FalseStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
                    }
                    else
                        if (retRefType.BaseType == "System.DateTime")
                    {
                        cmm.Statements.Add(cRetVal);
                        cmm.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("retVar"),
                            new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.DateTime"), "MinValue")));

                        //Call this function to add code for converting DMTF format string datetime to System.DateTime before returning
                        GenerateCodeForRefAndDateTimeTypes(cie, isRetArray, cis.TrueStatements, "System.DateTime", new CodeVariableReferenceExpression("retVar"), true);
                        cis.TrueStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("retVar")));
                        cis.FalseStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("retVar")));
                    }
                    else
                        if (retRefType.BaseType == "System.TimeSpan")
                    {
                        cmm.Statements.Add(cRetVal);
                        coce = new CodeObjectCreateExpression();
                        coce.CreateType = new CodeTypeReference("System.TimeSpan");
                        coce.Parameters.Add(new CodePrimitiveExpression(0));
                        coce.Parameters.Add(new CodePrimitiveExpression(0));
                        coce.Parameters.Add(new CodePrimitiveExpression(0));
                        coce.Parameters.Add(new CodePrimitiveExpression(0));
                        coce.Parameters.Add(new CodePrimitiveExpression(0));

                        cmm.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("retVar"), coce));

                        //Call this function to add code for converting DMTF format string Time Interval to System.TimeSpan before returning
                        GenerateCodeForRefAndDateTimeTypes(cie, isRetArray, cis.TrueStatements, "System.TimeSpan", new CodeVariableReferenceExpression("retVar"), true);
                        cis.TrueStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("retVar")));
                        cis.FalseStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("retVar")));
                    }
                    else
                        // if the return value is not array and not of type CimType.Object
                        if (retRefType.ArrayRank == 0 && retRefType.BaseType != new CodeTypeReference(PublicNamesUsed["BaseObjClass"].ToString()).BaseType)
                    {
                        cmie = new CodeMethodInvokeExpression();
                        cmie.Parameters.Add(new CodePropertyReferenceExpression(cie, "Value"));
                        cmie.Method.MethodName = GetConversionFunction(cimRetType);
                        cmie.Method.TargetObject = new CodeTypeReferenceExpression("System.Convert");

                        cis.TrueStatements.Add(new CodeMethodReturnStatement(cmie));

                        cmie = new CodeMethodInvokeExpression();
                        cmie.Parameters.Add(new CodePrimitiveExpression(0));
                        cmie.Method.MethodName = GetConversionFunction(cimRetType);
                        cmie.Method.TargetObject = new CodeTypeReferenceExpression("System.Convert");

                        cis.FalseStatements.Add(new CodeMethodReturnStatement(cmie));
                    }
                    // if the return type is array, then just do type casting before returning
                    else
                    {
                        cis.TrueStatements.Add(new CodeMethodReturnStatement(
                            new CodeCastExpression(retRefType, new CodePropertyReferenceExpression(cie, "Value"))));

                        cis.FalseStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
                    }
                }

                cmm.Statements.Add(cis);
                cc.Members.Add(cmm);
            }
        }

        /// <summary>
        /// This function returns a Collectionclass for the query
        ///        "Select * from &lt;ClassName&gt;"
        ///    This is a static method. The output is like this
        ///        public static ServiceCollection All()
        ///        {
        ///            return GetInstances((System.Management.ManagementScope)null,(System.Management.EnumerateionOptions)null);
        ///        }
        /// </summary>
        private void GenerateGetInstancesWithNoParameters()
        {
            cmm = new CodeMemberMethod();

            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
            cmm.Name = PublicNamesUsed["FilterFunction"].ToString();
            cmm.ReturnType = new CodeTypeReference(PrivateNamesUsed["CollectionClass"].ToString());

            cmie = new CodeMethodInvokeExpression();

            cmie.Method.MethodName = PublicNamesUsed["FilterFunction"].ToString();
            cmie.Parameters.Add(new CodePrimitiveExpression(null));
            cmie.Parameters.Add(new CodePrimitiveExpression(null));
            cmie.Parameters.Add(new CodePrimitiveExpression(null));

            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));

            cc.Members.Add(cmm);
            cmm.Comments.Add(new CodeCommentStatement(SR.CommentGetInstances));
        }

        /// <summary>
        /// This function will accept the condition and will return collection for the query
        ///        "select * from &lt;ClassName&gt; where &lt;condition&gt;"
        ///    The generated code will be like
        ///        public static ServiceCollection GetInstances(String Condition) {
        ///            return GetInstances(null,Condition,null);
        ///     }
        /// </summary>
        private void GenerateGetInstancesWithCondition()
        {
            string strCondition = "condition";
            cmm = new CodeMemberMethod();

            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
            cmm.Name = PublicNamesUsed["FilterFunction"].ToString();
            cmm.ReturnType = new CodeTypeReference(PrivateNamesUsed["CollectionClass"].ToString());
            cmm.Parameters.Add(new CodeParameterDeclarationExpression("System.String", strCondition));

            cmie = new CodeMethodInvokeExpression(
                null, //no TargetObject?
                PublicNamesUsed["FilterFunction"].ToString()
                );

            cmie.Parameters.Add(new CodePrimitiveExpression(null));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(strCondition));
            cmie.Parameters.Add(new CodePrimitiveExpression(null));
            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));

            cc.Members.Add(cmm);
        }

        /// <summary>
        /// This function returns the collection for the query
        ///        "select &lt;parameterList&gt; from &lt;ClassName&gt;"
        ///    The generated output is like
        ///        public static ServiceCollection GetInstances(String []selectedProperties) {
        ///            return GetInstances(null,null,selectedProperties);
        ///        }
        /// </summary>
        private void GenerateGetInstancesWithProperties()
        {
            string strSelectedProperties = "selectedProperties";
            cmm = new CodeMemberMethod();

            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
            cmm.Name = PublicNamesUsed["FilterFunction"].ToString();
            cmm.ReturnType = new CodeTypeReference(PrivateNamesUsed["CollectionClass"].ToString());
            cmm.Parameters.Add(new CodeParameterDeclarationExpression("System.String []", strSelectedProperties));

            cmie = new CodeMethodInvokeExpression(
                null,
                PublicNamesUsed["FilterFunction"].ToString()
                );
            cmie.Parameters.Add(new CodePrimitiveExpression(null));
            cmie.Parameters.Add(new CodePrimitiveExpression(null));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(strSelectedProperties));
            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));

            cc.Members.Add(cmm);
        }

        /// <summary>
        /// This function returns the collection for the query
        ///        "select &lt;parameterList> from &lt;ClassName&gt; where &lt;WhereClause&gt;"
        ///    The generated output is like
        ///        public static ServiceCollection GetInstances(String condition, String []selectedProperties) {
        ///            return GetInstances(null,condition,selectedProperties);
        ///        }
        /// </summary>
        private void GenerateGetInstancesWithWhereProperties()
        {
            string strSelectedProperties = "selectedProperties";
            string strCondition = "condition";
            cmm = new CodeMemberMethod();

            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
            cmm.Name = PublicNamesUsed["FilterFunction"].ToString();
            cmm.ReturnType = new CodeTypeReference(PrivateNamesUsed["CollectionClass"].ToString());
            cmm.Parameters.Add(new CodeParameterDeclarationExpression("System.String", strCondition));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression("System.String []", strSelectedProperties));

            cmie = new CodeMethodInvokeExpression(
                null, //no TargetObject?
                PublicNamesUsed["FilterFunction"].ToString()
                );

            cmie.Parameters.Add(new CodePrimitiveExpression(null));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(strCondition));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(strSelectedProperties));
            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));

            cc.Members.Add(cmm);
        }

        /// <summary>
        /// This function returns a Collectionclass for the query
        ///        "Select * from &lt;ClassName&gt;"
        ///    This is a static method. The output is like this
        ///    public static (ObjectCollection)GetInstances(System.Management.ManagementScope mgmtScope, System.Management.EnumerationOptions enumOptions)
        ///    {
        ///        if ((mgmtScope == null))
        ///        {
        ///            mgmtScope = new System.Management.ManagementScope();
        ///            mgmtScope.Path.NamespacePath = "root\\CimV2";
        ///        }
        ///        System.Management.ManagementPath pathObj = new System.Management.ManagementPath();
        ///        pathObj.ClassName = "CIM_LogicalDisk";
        ///        pathObj.NamespacePath = "root\\CimV2";
        ///        System.Management.ManagementClass clsObject = new System.Management.ManagementClass(mgmtScope, pathObj, null);
        ///        if ((enumOptions == null))
        ///        {
        ///            enumOptions = new System.Management.EnumerationOptions();
        ///            enumOptions.EnsureLocatable = true;
        ///        }
        ///        return new ObjectCollection(clsObject.GetInstances(enumOptions));
        ///    }
        ///    This method takes the scope which is useful for connection to remote machine
        /// </summary>
        private void GenerateGetInstancesWithScope()
        {
            cmm = new CodeMemberMethod();

            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
            cmm.Name = PublicNamesUsed["FilterFunction"].ToString();
            cmm.ReturnType = new CodeTypeReference(PrivateNamesUsed["CollectionClass"].ToString());
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["ScopeClass"].ToString()), PrivateNamesUsed["ScopeParam"].ToString()));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["QueryOptionsClass"].ToString()),
                PrivateNamesUsed["EnumParam"].ToString()));


            string strClass = "clsObject";
            string pathObj = "pathObj";


            cis = new CodeConditionStatement();
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString());
            cboe.Right = new CodePrimitiveExpression(null);
            cboe.Operator = CodeBinaryOperatorType.IdentityEquality;
            cis.Condition = cboe;

            CodeConditionStatement cis1 = new CodeConditionStatement();
            CodeBinaryOperatorExpression cboe1 = new CodeBinaryOperatorExpression();
            cboe1.Left = new CodeVariableReferenceExpression(PrivateNamesUsed["statMgmtScope"].ToString());
            cboe1.Right = new CodePrimitiveExpression(null);
            cboe1.Operator = CodeBinaryOperatorType.IdentityEquality;
            cis1.Condition = cboe1;

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["ScopeClass"].ToString());
            cis1.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()), coce));

            cis1.TrueStatements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(
                new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()),
                "Path"), "NamespacePath"),
                new CodePrimitiveExpression(classobj.Scope.Path.NamespacePath)));


            cis1.FalseStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()),
                new CodeVariableReferenceExpression(PrivateNamesUsed["statMgmtScope"].ToString())));

            cis.TrueStatements.Add(cis1);
            cmm.Statements.Add(cis);

            // Create a path object for the class
            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["PathClass"].ToString());

            cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(PublicNamesUsed["PathClass"].ToString()), pathObj, coce));

            cmm.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(pathObj), "ClassName"),
                new CodePrimitiveExpression(OriginalClassName)));

            cmm.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(pathObj), "NamespacePath"),
                new CodePrimitiveExpression(classobj.Scope.Path.NamespacePath)));


            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["ManagementClass"].ToString());
            coce.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()));
            coce.Parameters.Add(new CodeVariableReferenceExpression(pathObj));
            coce.Parameters.Add(new CodePrimitiveExpression(null));

            cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(PublicNamesUsed["ManagementClass"].ToString()),
                strClass, coce));

            cis = new CodeConditionStatement();
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(PrivateNamesUsed["EnumParam"].ToString());
            cboe.Right = new CodePrimitiveExpression(null);
            cboe.Operator = CodeBinaryOperatorType.IdentityEquality;
            cis.Condition = cboe;

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["QueryOptionsClass"].ToString());
            cis.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(PrivateNamesUsed["EnumParam"].ToString()),
                coce));
            cis.TrueStatements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(PrivateNamesUsed["EnumParam"].ToString()),
                "EnsureLocatable"),
                new CodePrimitiveExpression(true)));

            cmm.Statements.Add(cis);


            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PrivateNamesUsed["CollectionClass"].ToString());

            cmie = new CodeMethodInvokeExpression();
            cmie.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(strClass), "GetInstances");
            cmie.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["EnumParam"].ToString()));
            coce.Parameters.Add(cmie);
            cmm.Statements.Add(new CodeMethodReturnStatement(coce));

            cc.Members.Add(cmm);

        }

        /// <summary>
        /// This function will accept the condition and will return collection for the query
        ///        "select * from &lt;ClassName&gt; where &lt;condition&gt;"
        ///    The generated code will be like
        ///        public static ServiceCollection GetInstances(String Condition) {
        ///            return GetInstances(scope,Condition,null);
        ///     }
        /// </summary>
        private void GenerateGetInstancesWithScopeCondition()
        {
            string strCondition = "condition";
            cmm = new CodeMemberMethod();

            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
            cmm.Name = PublicNamesUsed["FilterFunction"].ToString();
            cmm.ReturnType = new CodeTypeReference(PrivateNamesUsed["CollectionClass"].ToString());
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["ScopeClass"].ToString()), PrivateNamesUsed["ScopeParam"].ToString()));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("System.String"), strCondition));

            cmie = new CodeMethodInvokeExpression(
                null,
                PublicNamesUsed["FilterFunction"].ToString()
                );
            cmie.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(strCondition));
            cmie.Parameters.Add(new CodePrimitiveExpression(null));
            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));

            cc.Members.Add(cmm);
        }

        /// <summary>
        /// This function returns the collection for the query
        ///        "select &lt;parameterList&gt; from &lt;ClassName&gt;"
        ///    The generated output is like
        ///        public static ServiceCollection GetInstances(String []selectedProperties) {
        ///            return GetInstances(scope,null,selectedProperties);
        ///        }
        /// </summary>
        private void GenerateGetInstancesWithScopeProperties()
        {
            string strSelectedProperties = "selectedProperties";
            cmm = new CodeMemberMethod();

            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
            cmm.Name = PublicNamesUsed["FilterFunction"].ToString();
            cmm.ReturnType = new CodeTypeReference(PrivateNamesUsed["CollectionClass"].ToString());
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(PublicNamesUsed["ScopeClass"].ToString(), PrivateNamesUsed["ScopeParam"].ToString()));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression("System.String []", strSelectedProperties));

            cmie = new CodeMethodInvokeExpression(
                null, //no TargetObject?
                PublicNamesUsed["FilterFunction"].ToString()
                );

            cmie.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()));
            cmie.Parameters.Add(new CodePrimitiveExpression(null));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(strSelectedProperties));
            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));

            cc.Members.Add(cmm);
        }

        /// <summary>
        /// This function generates the code like
        ///     public static ServiceCollection GetInstances(ManagementScope scope,String Condition, String[] selectedProperties)    {
        ///            if (scope == null)
        ///            {
        ///                scope = new ManagementScope();
        ///                scope.Path.NamespacePath = WMINamespace;
        ///            }
        ///         ManagementObjectSearcher ObjectSearcher = new ManagementObjectSearcher(scope,new SelectQuery("Win32_Service",Condition,selectedProperties));
        ///            QueryOptions query = new QueryOptions();
        ///            query.EnsureLocatable = true;
        ///            ObjectSearcher.Options = query;
        ///            return new ServiceCollection(ObjectSearcher.Get());
        ///        }
        /// </summary>
        private void GenerateGetInstancesWithScopeWhereProperties()
        {
            string strCondition = "condition";
            string strSelectedProperties = "selectedProperties";
            string strObjectSearcher = "ObjectSearcher";

            cmm = new CodeMemberMethod();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
            cmm.Name = PublicNamesUsed["FilterFunction"].ToString();
            cmm.ReturnType = new CodeTypeReference(PrivateNamesUsed["CollectionClass"].ToString());

            cmm.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["ScopeClass"].ToString()), PrivateNamesUsed["ScopeParam"].ToString()));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression("System.String", strCondition));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression("System.String []", strSelectedProperties));

            cis = new CodeConditionStatement();
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString());
            cboe.Right = new CodePrimitiveExpression(null);
            cboe.Operator = CodeBinaryOperatorType.IdentityEquality;
            cis.Condition = cboe;


            CodeConditionStatement cis1 = new CodeConditionStatement();
            CodeBinaryOperatorExpression cboe1 = new CodeBinaryOperatorExpression();
            cboe1.Left = new CodeVariableReferenceExpression(PrivateNamesUsed["statMgmtScope"].ToString());
            cboe1.Right = new CodePrimitiveExpression(null);
            cboe1.Operator = CodeBinaryOperatorType.IdentityEquality;
            cis1.Condition = cboe1;

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["ScopeClass"].ToString());
            cis1.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()), coce));

            cis1.TrueStatements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(
                new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()),
                "Path"), "NamespacePath"),
                new CodePrimitiveExpression(classobj.Scope.Path.NamespacePath)));

            cis1.FalseStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()),
                new CodeVariableReferenceExpression(PrivateNamesUsed["statMgmtScope"].ToString())));


            cis.TrueStatements.Add(cis1);
            cmm.Statements.Add(cis);
            CodeObjectCreateExpression coce1 = new CodeObjectCreateExpression();
            coce1.CreateType = new CodeTypeReference(PublicNamesUsed["QueryClass"].ToString());
            coce1.Parameters.Add(new CodePrimitiveExpression(OriginalClassName));
            coce1.Parameters.Add(new CodeVariableReferenceExpression(strCondition));
            coce1.Parameters.Add(new CodeVariableReferenceExpression(strSelectedProperties));

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["ObjectSearcherClass"].ToString());
            coce.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()));
            coce.Parameters.Add(coce1);

            cmm.Statements.Add(new CodeVariableDeclarationStatement(PublicNamesUsed["ObjectSearcherClass"].ToString(),
                strObjectSearcher, coce));

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["QueryOptionsClass"].ToString());

            cmm.Statements.Add(new CodeVariableDeclarationStatement(
                new CodeTypeReference(PublicNamesUsed["QueryOptionsClass"].ToString()),
                PrivateNamesUsed["EnumParam"].ToString(), coce));

            cmm.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(PrivateNamesUsed["EnumParam"].ToString()),
                "EnsureLocatable"),
                new CodePrimitiveExpression(true)));



            cmm.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(strObjectSearcher),
                "Options"),
                new CodeVariableReferenceExpression(PrivateNamesUsed["EnumParam"].ToString())));

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PrivateNamesUsed["CollectionClass"].ToString());
            coce.Parameters.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(strObjectSearcher),
                "Get"));
            cmm.Statements.Add(new CodeMethodReturnStatement(coce));

            cc.Members.Add(cmm);
        }

        /// <summary>
        /// This function will add the variable as a private member to the class.
        /// The generated code will look like this
        ///         private &lt;MemberType&gt; &lt;MemberName&gt;;
        /// </summary>
        private void GeneratePrivateMember(string memberName, string MemberType, string Comment)
        {
            GeneratePrivateMember(memberName, MemberType, null, false, Comment);
        }

        /// <summary>
        /// This function will add the variable as a private member to the class.
        /// The generated code will look like this
        ///         private &lt;MemberType&gt; &lt;MemberName&gt; = &lt;initValue&gt;;
        /// </summary>
        private void GeneratePrivateMember(string memberName, string MemberType, CodeExpression initExpression, bool isStatic, string Comment)
        {
            cf = new CodeMemberField();
            cf.Name = memberName;
            cf.Attributes = MemberAttributes.Private | MemberAttributes.Final;
            if (isStatic)
            {
                cf.Attributes |= MemberAttributes.Static;
            }
            cf.Type = new CodeTypeReference(MemberType);
            if (initExpression != null && isStatic)
            {
                cf.InitExpression = initExpression;
            }
            cc.Members.Add(cf);

            if (!string.IsNullOrEmpty(Comment))
            {
                cf.Comments.Add(new CodeCommentStatement(Comment));
            }
        }

        private CodeTypeDeclaration GenerateTypeConverterClass()
        {
            string TypeDescriptorContextClass = "System.ComponentModel.ITypeDescriptorContext";
            string contextObject = "context";
            string TypeDstObject = "destinationType";
            string ValueVar = "value";
            string CultureInfoClass = "System.Globalization.CultureInfo";
            string CultureInfoVar = "culture";
            string IDictionary = "System.Collections.IDictionary";
            string DictVar = "dictionary";
            string propColl = "PropertyDescriptorCollection";
            string AttributeVar = "attributeVar";


            string baseTypeParam = "inBaseType";
            string baseTypeMemberVariable = "baseConverter";
            string typeMemberVariable = "baseType";
            string TypeDescriptorClass = "TypeDescriptor";
            string srcType = "srcType";


            /*
            // TypeConverter to handle null values for ValueType properties
            public class WMIValueTypeConverter : TypeConverter
            */
            CodeTypeDeclaration CodeConvertorClass = new CodeTypeDeclaration(PrivateNamesUsed["ConverterClass"].ToString());
            CodeConvertorClass.BaseTypes.Add(PublicNamesUsed["TypeConverter"].ToString());

            /*
                private TypeConverter baseConverter;
                private Type baseType;
            */
            cf = new CodeMemberField();
            cf.Name = baseTypeMemberVariable;
            cf.Attributes = MemberAttributes.Private | MemberAttributes.Final;
            cf.Type = new CodeTypeReference(PublicNamesUsed["TypeConverter"].ToString());

            CodeConvertorClass.Members.Add(cf);

            cf = new CodeMemberField();
            cf.Name = typeMemberVariable;
            cf.Attributes = MemberAttributes.Private | MemberAttributes.Final;
            cf.Type = new CodeTypeReference(PublicNamesUsed["Type"].ToString());

            CodeConvertorClass.Members.Add(cf);

            /*
                public WMIValueTypeConverter(System.Type inBaseType)
                {
                    baseConverter = TypeDescriptor.GetConverter(inBaseType);
                    baseType = inBaseType;
                }
            */
            cctor = new CodeConstructor();
            cctor.Attributes = MemberAttributes.Public;
            cpde = new CodeParameterDeclarationExpression();
            cpde.Name = baseTypeParam;
            cpde.Type = new CodeTypeReference("System.Type");
            cctor.Parameters.Add(cpde);

            cmie = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(TypeDescriptorClass), "GetConverter");

            cmie.Parameters.Add(new CodeVariableReferenceExpression(baseTypeParam));

            cctor.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(baseTypeMemberVariable), cmie));

            // second assignment in ctor
            cctor.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(typeMemberVariable), new CodeVariableReferenceExpression(baseTypeParam)));
            // add the ctor to the class
            CodeConvertorClass.Members.Add(cctor);

            /*
            public virtual bool CanConvertFrom(ITypeDescriptorContext context, Type srcType);
            {
                return baseType.CanConvertFrom(srcType);
            }
            */

            cmm = new CodeMemberMethod();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Override | MemberAttributes.Overloaded;
            cmm.Name = "CanConvertFrom";
            cmm.ReturnType = new CodeTypeReference("System.Boolean");

            cmm.Parameters.Add(new CodeParameterDeclarationExpression(TypeDescriptorContextClass, contextObject));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression("System.Type", srcType));

            cmie = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(baseTypeMemberVariable), "CanConvertFrom");
            cmie.Parameters.Add(new CodeVariableReferenceExpression(contextObject));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(srcType));

            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));
            CodeConvertorClass.Members.Add(cmm);



            /*
            public virtual bool CanConvertTo(ITypeDescriptorContext context, Type TypeDstObject);
            {
                return baseType.CanConvertTo(context,TypeDstObject);
            }
            */

            cmm = new CodeMemberMethod();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Override | MemberAttributes.Overloaded;
            cmm.Name = "CanConvertTo";
            cmm.ReturnType = new CodeTypeReference("System.Boolean");

            cmm.Parameters.Add(new CodeParameterDeclarationExpression(TypeDescriptorContextClass, contextObject));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression("System.Type", TypeDstObject));

            cmie = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(baseTypeMemberVariable), "CanConvertTo");
            cmie.Parameters.Add(new CodeVariableReferenceExpression(contextObject));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(TypeDstObject));

            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));
            CodeConvertorClass.Members.Add(cmm);



            /*
            public virtual object ConvertFrom(ITypeDescriptorContext context,CultureInfo culInfo, object value);
            {
                return baseType.ConvertFrom(context,culInfo,value);
            }
            */

            cmm = new CodeMemberMethod();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Override | MemberAttributes.Overloaded;
            cmm.Name = "ConvertFrom";
            cmm.ReturnType = new CodeTypeReference("System.Object");

            cmm.Parameters.Add(new CodeParameterDeclarationExpression(TypeDescriptorContextClass, contextObject));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(CultureInfoClass, CultureInfoVar));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("System.Object"), ValueVar));

            cmie = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(baseTypeMemberVariable), "ConvertFrom");
            cmie.Parameters.Add(new CodeVariableReferenceExpression(contextObject));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(CultureInfoVar));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(ValueVar));

            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));
            CodeConvertorClass.Members.Add(cmm);



            /*
                public virtual object CreateInstance(ITypeDescriptorContext,IDictionary dictionary);
                {
                    return baseType.CreateInstance(context,dictionary);
                }
                */

            cmm = new CodeMemberMethod();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Override | MemberAttributes.Overloaded;
            cmm.ReturnType = new CodeTypeReference("System.Object");

            cmm.Name = "CreateInstance";
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(TypeDescriptorContextClass, contextObject));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(IDictionary, DictVar));

            cmie = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(baseTypeMemberVariable), "CreateInstance");
            cmie.Parameters.Add(new CodeVariableReferenceExpression(contextObject));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(DictVar));

            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));
            CodeConvertorClass.Members.Add(cmm);


            /*
                public virtual bool GetCreateInstanceSupported(ITypeDescriptorContext context);
                {
                    return baseType.GetCreateInstanceSupported(context);
                }
                */

            cmm = new CodeMemberMethod();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Override | MemberAttributes.Overloaded;
            cmm.Name = "GetCreateInstanceSupported";
            cmm.ReturnType = new CodeTypeReference("System.Boolean");

            cmm.Parameters.Add(new CodeParameterDeclarationExpression(TypeDescriptorContextClass, contextObject));

            cmie = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(baseTypeMemberVariable), "GetCreateInstanceSupported");
            cmie.Parameters.Add(new CodeVariableReferenceExpression(contextObject));

            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));
            CodeConvertorClass.Members.Add(cmm);

            /*
                public virtual PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context,object value,Attribute [] attributes);
                {
                    return baseType.GetProperties(context,value);
                }
                */

            cmm = new CodeMemberMethod();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Override | MemberAttributes.Overloaded;
            cmm.Name = "GetProperties";
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(TypeDescriptorContextClass, contextObject));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("System.Object"), ValueVar));

            CodeTypeReference crt = new CodeTypeReference(new CodeTypeReference("System.Attribute"), 1);
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(crt, AttributeVar));
            cmm.ReturnType = new CodeTypeReference(propColl);

            cmie = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(baseTypeMemberVariable), "GetProperties");
            cmie.Parameters.Add(new CodeVariableReferenceExpression(contextObject));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(ValueVar));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(AttributeVar));

            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));
            CodeConvertorClass.Members.Add(cmm);

            /*
                public virtual GetPropertiesSupported(ITypeDescriptorContext context);
                {
                    return baseType.GetPropertiesSupported(context);
                }
                */

            cmm = new CodeMemberMethod();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Override | MemberAttributes.Overloaded;
            cmm.Name = "GetPropertiesSupported";
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(TypeDescriptorContextClass, contextObject));
            cmm.ReturnType = new CodeTypeReference("System.Boolean");

            cmie = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(baseTypeMemberVariable), "GetPropertiesSupported");
            cmie.Parameters.Add(new CodeVariableReferenceExpression(contextObject));

            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));
            CodeConvertorClass.Members.Add(cmm);

            /*
                public StandardValuesCollection virtual GetStandardValues(ITypeDescriptorContext context);
                {
                    return baseType.GetStandardValues(context);
                }
                */

            cmm = new CodeMemberMethod();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Override | MemberAttributes.Overloaded;
            cmm.Name = "GetStandardValues";
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(TypeDescriptorContextClass, contextObject));
            cmm.ReturnType = new CodeTypeReference("System.ComponentModel.TypeConverter.StandardValuesCollection");

            cmie = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(baseTypeMemberVariable), "GetStandardValues");
            cmie.Parameters.Add(new CodeVariableReferenceExpression(contextObject));

            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));
            CodeConvertorClass.Members.Add(cmm);

            /*
                public virtual GetStandardValuesExclusive(ITypeDescriptorContext context);
                {
                    return baseType.GetStandardValuesExclusive(context);
                }
                */

            cmm = new CodeMemberMethod();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Override | MemberAttributes.Overloaded;
            cmm.Name = "GetStandardValuesExclusive";
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(TypeDescriptorContextClass, contextObject));
            cmm.ReturnType = new CodeTypeReference("System.Boolean");

            cmie = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(baseTypeMemberVariable), "GetStandardValuesExclusive");
            cmie.Parameters.Add(new CodeVariableReferenceExpression(contextObject));

            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));
            CodeConvertorClass.Members.Add(cmm);

            /*
                public virtual GetStandardValuesSupported(ITypeDescriptorContext context);
                {
                    return baseType.GetStandardValuesSupported(context);
                }
                */

            cmm = new CodeMemberMethod();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Override | MemberAttributes.Overloaded;
            cmm.Name = "GetStandardValuesSupported";
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(TypeDescriptorContextClass, contextObject));
            cmm.ReturnType = new CodeTypeReference("System.Boolean");

            cmie = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(baseTypeMemberVariable), "GetStandardValuesSupported");
            cmie.Parameters.Add(new CodeVariableReferenceExpression(contextObject));

            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));
            CodeConvertorClass.Members.Add(cmm);

            // if we have nullable enums we need this code
            /*
            public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType)
            {
                if ( baseType.BaseType == typeof(System.Enum) )
                {
                    if (value.GetType() == destinationType) return value;
                    if ((value == null) && (context != null)
                        && (context.PropertyDescriptor.ShouldSerializeValue(context.Instance) == false))
                    {
                        return "NULL_ENUM_VALUE";
                    }
                    return baseConverter.ConvertTo(context, culture, value, destinationType);
                }

                if ( (baseType == typeof(System.Boolean)) && (baseType.BaseType == typeof(System.ValueType)) )
                {
                    if ((value == null) && (context != null)
                       && (context.PropertyDescriptor.ShouldSerializeValue(context.Instance) == false))
                    {
                        return "";
                    }
                    return baseConverter.ConvertTo(context, culture, value, destinationType);
                }

                if ((context != null) && (context.PropertyDescriptor.ShouldSerializeValue(context.Instance) == false))
                {
                    return "";
                }

                return baseConverter.ConvertTo(context, culture, value, destinationType);
            }

            */

            // make the member method
            cmm = new CodeMemberMethod();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Override | MemberAttributes.Overloaded;
            cmm.Name = "ConvertTo";
            // add the 3 parameters
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(TypeDescriptorContextClass, contextObject));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(CultureInfoClass, CultureInfoVar));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("System.Object"), ValueVar));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression("System.Type", TypeDstObject));
            cmm.ReturnType = new CodeTypeReference("System.Object");

            // make the generic return statement we'll need all over
            /*
                return baseConverter.ConvertTo(context, culture, value, destinationType);
            */
            cmie = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(baseTypeMemberVariable), "ConvertTo");
            cmie.Parameters.Add(new CodeVariableReferenceExpression(contextObject));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(CultureInfoVar));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(ValueVar));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(TypeDstObject));
            CodeMethodReturnStatement returnStatement = new CodeMethodReturnStatement(cmie);

            // if ( baseType.BaseType == typeof(System.Enum) )
            cis = new CodeConditionStatement();
            CodeBinaryOperatorExpression cboe1 = new CodeBinaryOperatorExpression();
            cboe1.Left = new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(typeMemberVariable),
                "BaseType");

            cboe1.Right = new CodeTypeOfExpression(typeof(System.Enum));
            cboe1.Operator = CodeBinaryOperatorType.IdentityEquality;

            cis.Condition = cboe1;

            // true statements:
            /*
                    if (value.GetType() == destinationType) return value;
                    if ((value == null) && (context != null)
                        && (context.PropertyDescriptor.ShouldSerializeValue(context.Instance) == false))
                    {
                        return "NULL_ENUM_VALUE";
                    }
                    return baseConverter.ConvertTo(context, culture, value, destinationType);

            */

            CodeBinaryOperatorExpression cboe2 = new CodeBinaryOperatorExpression();
            cboe2.Left = new CodeMethodInvokeExpression(
                new CodeVariableReferenceExpression("value"), "GetType");
            cboe2.Right = new CodeVariableReferenceExpression("destinationType");
            cboe2.Operator = CodeBinaryOperatorType.IdentityEquality;

            cis.TrueStatements.Add(new CodeConditionStatement(cboe2, new CodeMethodReturnStatement(new CodeVariableReferenceExpression("value"))));

            // work on second true statement
            CodeBinaryOperatorExpression cboe3 = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("value"),
                CodeBinaryOperatorType.IdentityEquality,
                new CodePrimitiveExpression(null));

            CodeBinaryOperatorExpression cboe4 = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression(contextObject),
                CodeBinaryOperatorType.IdentityInequality,
                new CodePrimitiveExpression(null));

            CodeBinaryOperatorExpression cboe5 = new CodeBinaryOperatorExpression();
            cboe5.Left = cboe3;
            cboe5.Right = cboe4;
            cboe5.Operator = CodeBinaryOperatorType.BooleanAnd;

            cmie = new CodeMethodInvokeExpression(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(contextObject), "PropertyDescriptor"), "ShouldSerializeValue");
            cmie.Parameters.Add(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(contextObject), "Instance"));

            CodeBinaryOperatorExpression cboe6 = new CodeBinaryOperatorExpression(cmie,
                CodeBinaryOperatorType.ValueEquality,
                new CodePrimitiveExpression(false));

            CodeBinaryOperatorExpression cboe7 = new CodeBinaryOperatorExpression();
            cboe7.Left = cboe5;
            cboe7.Right = cboe6;
            cboe7.Operator = CodeBinaryOperatorType.BooleanAnd;

            cis.TrueStatements.Add(new CodeConditionStatement(cboe7, new CodeMethodReturnStatement(new CodeSnippetExpression(" \"NULL_ENUM_VALUE\" "))));
            // add the final returnstatement
            cis.TrueStatements.Add(returnStatement);

            // add the condition statement to the method and work on the next condition section
            cmm.Statements.Add(cis);

            //   if ( (baseType == typeof(System.Boolean)) && (baseType.BaseType == typeof(System.ValueType)) )
            cis = new CodeConditionStatement();
            cboe1 = new CodeBinaryOperatorExpression();
            cboe1.Left = new CodeVariableReferenceExpression(typeMemberVariable);
            cboe1.Right = new CodeTypeOfExpression(PublicNamesUsed["Boolean"].ToString());
            cboe1.Operator = CodeBinaryOperatorType.IdentityEquality;

            cboe2 = new CodeBinaryOperatorExpression();
            cboe2.Left = new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(typeMemberVariable),
                "BaseType");

            cboe2.Right = new CodeTypeOfExpression(PublicNamesUsed["ValueType"].ToString());
            cboe2.Operator = CodeBinaryOperatorType.IdentityEquality;

            cboe3 = new CodeBinaryOperatorExpression();
            cboe3.Left = cboe1;
            cboe3.Right = cboe2;
            cboe3.Operator = CodeBinaryOperatorType.BooleanAnd;

            cis.Condition = cboe3;

            /* true statements

                    if ((value == null) && (context != null)
                       && (context.PropertyDescriptor.ShouldSerializeValue(context.Instance) == false))
                    {
                        return "";
                    }
                    return baseConverter.ConvertTo(context, culture, value, destinationType);

            */

            cboe3 = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("value"),
                CodeBinaryOperatorType.IdentityEquality,
                new CodePrimitiveExpression(null));

            cboe4 = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression(contextObject),
                CodeBinaryOperatorType.IdentityInequality,
                new CodePrimitiveExpression(null));

            cboe5 = new CodeBinaryOperatorExpression();
            cboe5.Left = cboe3;
            cboe5.Right = cboe4;
            cboe5.Operator = CodeBinaryOperatorType.BooleanAnd;

            cmie = new CodeMethodInvokeExpression(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(contextObject), "PropertyDescriptor"), "ShouldSerializeValue");
            cmie.Parameters.Add(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(contextObject), "Instance"));

            cboe6 = new CodeBinaryOperatorExpression(cmie,
                CodeBinaryOperatorType.ValueEquality,
                new CodePrimitiveExpression(false));

            cboe7 = new CodeBinaryOperatorExpression();
            cboe7.Left = cboe5;
            cboe7.Right = cboe6;
            cboe7.Operator = CodeBinaryOperatorType.BooleanAnd;

            cis.TrueStatements.Add(new CodeConditionStatement(cboe7, new CodeMethodReturnStatement(new CodePrimitiveExpression(""))));
            // add the final returnstatement
            cis.TrueStatements.Add(returnStatement);

            // add the condition statement to the method and work on the next condition section
            cmm.Statements.Add(cis);
            /*
                if ((context != null) && (context.PropertyDescriptor.ShouldSerializeValue(context.Instance) == false))
                {
                    return "";
                }

                return baseConverter.ConvertTo(context, culture, value, destinationType);
            */

            cis = new CodeConditionStatement();
            cboe1 = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression(contextObject),
                CodeBinaryOperatorType.IdentityInequality,
                new CodePrimitiveExpression(null));

            cmie = new CodeMethodInvokeExpression(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(contextObject), "PropertyDescriptor"), "ShouldSerializeValue");
            cmie.Parameters.Add(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(contextObject), "Instance"));

            cboe2 = new CodeBinaryOperatorExpression(cmie,
                CodeBinaryOperatorType.ValueEquality,
                new CodePrimitiveExpression(false));

            cboe3 = new CodeBinaryOperatorExpression();
            cboe3.Left = cboe1;
            cboe3.Right = cboe2;
            cboe3.Operator = CodeBinaryOperatorType.BooleanAnd;

            cis.Condition = cboe3;
            cis.TrueStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression("")));

            cmm.Statements.Add(cis);

            // add the extra return at the end
            cmm.Statements.Add(returnStatement);

            CodeConvertorClass.Members.Add(cmm);

            CodeConvertorClass.Comments.Add(new CodeCommentStatement(SR.CommentPrototypeConverter));

            return CodeConvertorClass;

        }



        private void GenerateCollectionClass()
        {
            string strManagementObjectCollectionType = "ManagementObjectCollection";
            string strObjectCollection = "privColObj";
            string strobjCollection = "objCollection";

            //public class ServiceCollection : ICollection, IEnumerable
            ccc = new CodeTypeDeclaration(PrivateNamesUsed["CollectionClass"].ToString());

            ccc.BaseTypes.Add("System.Object");
            ccc.BaseTypes.Add("ICollection");
            ccc.TypeAttributes = TypeAttributes.NestedPublic;

            cf = new CodeMemberField();
            cf.Name = strObjectCollection;
            cf.Attributes = MemberAttributes.Private | MemberAttributes.Final;
            cf.Type = new CodeTypeReference(strManagementObjectCollectionType);
            ccc.Members.Add(cf);

            //internal ServiceCollection(ManagementObjectCollection obj)
            //{
            //    objCollection = obj;
            //}

            cctor = new CodeConstructor();
            cctor.Attributes = MemberAttributes.Public;
            cpde = new CodeParameterDeclarationExpression();
            cpde.Name = strobjCollection;
            cpde.Type = new CodeTypeReference(strManagementObjectCollectionType);
            cctor.Parameters.Add(cpde);

            cctor.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(strObjectCollection),
                new CodeVariableReferenceExpression(strobjCollection)));
            ccc.Members.Add(cctor);


            //public Int32 Count {
            //    get {
            //            return objCollection.Count;
            //        }
            //}

            cmp = new CodeMemberProperty();
            cmp.Type = new CodeTypeReference("System.Int32");
            cmp.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Override;
            cmp.Name = "Count";
            cmp.ImplementationTypes.Add("System.Collections.ICollection");
            cmp.GetStatements.Add(new CodeMethodReturnStatement(new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(strObjectCollection),
                "Count")));
            ccc.Members.Add(cmp);


            //public bool IsSynchronized {
            //    get {
            //        return objCollection.IsSynchronized;
            //    }
            //}

            cmp = new CodeMemberProperty();
            cmp.Type = new CodeTypeReference("System.Boolean");
            cmp.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Override;
            cmp.Name = "IsSynchronized";
            cmp.ImplementationTypes.Add("System.Collections.ICollection");
            cmp.GetStatements.Add(new CodeMethodReturnStatement(new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(strObjectCollection),
                "IsSynchronized")));
            ccc.Members.Add(cmp);

            //public Object SyncRoot {
            //    get {
            //        return this;
            //    }
            //}

            cmp = new CodeMemberProperty();
            cmp.Type = new CodeTypeReference("System.Object");
            cmp.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Override;
            cmp.Name = "SyncRoot";
            cmp.ImplementationTypes.Add("System.Collections.ICollection");
            cmp.GetStatements.Add(new CodeMethodReturnStatement(new CodeThisReferenceExpression()));
            ccc.Members.Add(cmp);

            //public void CopyTo (Array array, Int32 index)
            //{
            //    objCollection.CopyTo(array,index);
            //    for (int iCtr=0;iCtr < array.Length ;iCtr++)
            //    {
            //        array.SetValue(new Service((ManagementObject)array.GetValue(iCtr)),iCtr);
            //    }
            //}

            string strArray = "array";
            string strIndex = "index";
            string strnCtr = "nCtr";

            cmm = new CodeMemberMethod();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Override;
            cmm.Name = "CopyTo";
            cmm.ImplementationTypes.Add("System.Collections.ICollection");

            cpde = new CodeParameterDeclarationExpression();
            cpde.Name = strArray;
            cpde.Type = new CodeTypeReference("System.Array");
            cmm.Parameters.Add(cpde);

            cpde = new CodeParameterDeclarationExpression();
            cpde.Name = strIndex;
            cpde.Type = new CodeTypeReference("System.Int32");
            cmm.Parameters.Add(cpde);

            cmie = new CodeMethodInvokeExpression(
                new CodeVariableReferenceExpression(strObjectCollection),
                "CopyTo"
                );

            cmie.Parameters.Add(new CodeVariableReferenceExpression(strArray));
            cmie.Parameters.Add(new CodeVariableReferenceExpression(strIndex));
            cmm.Statements.Add(new CodeExpressionStatement(cmie));

            cmm.Statements.Add(new CodeVariableDeclarationStatement("System.Int32", strnCtr));
            cfls = new CodeIterationStatement();

            cfls.InitStatement = new CodeAssignStatement(new CodeVariableReferenceExpression(strnCtr), new CodePrimitiveExpression(0));
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(strnCtr);
            cboe.Operator = CodeBinaryOperatorType.LessThan;
            cboe.Right = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(strArray), "Length");
            cfls.TestExpression = cboe;
            cfls.IncrementStatement = new CodeAssignStatement(new CodeVariableReferenceExpression(strnCtr),
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression(strnCtr),
                CodeBinaryOperatorType.Add,
                new CodePrimitiveExpression(1)));

            cmie = new CodeMethodInvokeExpression(
                new CodeVariableReferenceExpression(strArray),
                "SetValue");

            CodeMethodInvokeExpression cmie1 = new CodeMethodInvokeExpression(
                new CodeVariableReferenceExpression(strArray),
                "GetValue",
                new CodeVariableReferenceExpression(strnCtr));

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PrivateNamesUsed["GeneratedClassName"].ToString());
            coce.Parameters.Add(new CodeCastExpression(new CodeTypeReference(PublicNamesUsed["LateBoundClass"].ToString()), cmie1));

            cmie.Parameters.Add(coce);
            cmie.Parameters.Add(new CodeVariableReferenceExpression(strnCtr));
            cfls.Statements.Add(new CodeExpressionStatement(cmie));

            cmm.Statements.Add(cfls);
            ccc.Members.Add(cmm);

            //ServiceEnumerator GetEnumerator()
            //{
            //    return new ServiceEnumerator (objCollection.GetEnumerator());
            //}

            cmm = new CodeMemberMethod();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Override;
            cmm.Name = "GetEnumerator";
            cmm.ImplementationTypes.Add("System.Collections.IEnumerable");
            cmm.ReturnType = new CodeTypeReference("System.Collections.IEnumerator");
            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PrivateNamesUsed["EnumeratorClass"].ToString());
            coce.Parameters.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(strObjectCollection), "GetEnumerator"));
            cmm.Statements.Add(new CodeMethodReturnStatement(coce));
            ccc.Members.Add(cmm);

            //Now generate the Enumerator Class
            GenerateEnumeratorClass();

            ccc.Comments.Add(new CodeCommentStatement(SR.CommentEnumeratorImplementation));
            cc.Members.Add(ccc);
        }

        private void GenerateEnumeratorClass()
        {
            string strObjectEnumerator = "privObjEnum";
            string strManagementObjectEnumeratorType = "ManagementObjectEnumerator";
            string strManagementObjectCollectionType = "ManagementObjectCollection";
            string strobjEnum = "objEnum";

            //public class ServiceEnumerator : IEnumerator
            ecc = new CodeTypeDeclaration(PrivateNamesUsed["EnumeratorClass"].ToString());
            ecc.TypeAttributes = TypeAttributes.NestedPublic;

            ecc.BaseTypes.Add("System.Object");
            ecc.BaseTypes.Add("System.Collections.IEnumerator");

            //private ManagementObjectCollection.ManagementObjectEnumerator ObjectEnumerator;
            cf = new CodeMemberField();
            cf.Name = strObjectEnumerator;
            cf.Attributes = MemberAttributes.Private | MemberAttributes.Final;
            cf.Type = new CodeTypeReference(strManagementObjectCollectionType + "." +
                strManagementObjectEnumeratorType);
            ecc.Members.Add(cf);

            //constructor
            //internal ServiceEnumerator(ManagementObjectCollection.ManagementObjectEnumerator objEnum)
            //{
            //    ObjectEnumerator = objEnum;
            //}
            cctor = new CodeConstructor();
            cctor.Attributes = MemberAttributes.Public;
            cpde = new CodeParameterDeclarationExpression();
            cpde.Name = strobjEnum;
            cpde.Type = new CodeTypeReference(strManagementObjectCollectionType + "." +
                strManagementObjectEnumeratorType);
            cctor.Parameters.Add(cpde);

            cctor.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(strObjectEnumerator),
                new CodeVariableReferenceExpression(strobjEnum)));
            ecc.Members.Add(cctor);

            //public Service Current {
            //get {
            //        return new Service((ManagementObject)ObjectEnumerator.Current);
            //    }
            //}

            cmp = new CodeMemberProperty();
            cmp.Type = new CodeTypeReference("System.Object");
            cmp.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Override;
            cmp.Name = "Current";
            cmp.ImplementationTypes.Add("System.Collections.IEnumerator");
            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PrivateNamesUsed["GeneratedClassName"].ToString());
            coce.Parameters.Add(new CodeCastExpression(new CodeTypeReference(PublicNamesUsed["LateBoundClass"].ToString()),
                new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(strObjectEnumerator),
                "Current")));
            cmp.GetStatements.Add(new CodeMethodReturnStatement(coce));
            ecc.Members.Add(cmp);

            //public bool MoveNext ()
            //{
            //    return ObjectEnumerator.MoveNext();
            //}

            cmm = new CodeMemberMethod();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Override;
            cmm.Name = "MoveNext";
            cmm.ImplementationTypes.Add("System.Collections.IEnumerator");
            cmm.ReturnType = new CodeTypeReference("System.Boolean");
            cmie = new CodeMethodInvokeExpression(
                new CodeVariableReferenceExpression(strObjectEnumerator),
                "MoveNext"
                );

            cmm.Statements.Add(new CodeMethodReturnStatement(cmie));
            ecc.Members.Add(cmm);

            //public void Reset ()
            //{
            //    ObjectEnumerator.Reset();
            //}

            cmm = new CodeMemberMethod();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Override;
            cmm.Name = "Reset";
            cmm.ImplementationTypes.Add("System.Collections.IEnumerator");
            cmie = new CodeMethodInvokeExpression(
                new CodeVariableReferenceExpression(strObjectEnumerator),
                "Reset"
                );
            cmm.Statements.Add(new CodeExpressionStatement(cmie));
            ecc.Members.Add(cmm);

            ccc.Members.Add(ecc);
        }

        /// <summary>
        /// This function will find a given string in the passed
        /// in a case insensitive manner and will return true if the string is found.
        /// </summary>
        private int IsContainedIn(string strToFind, ref SortedList sortedList)
        {
            int nIndex = -1;
            for (int i = 0; i < sortedList.Count; i++)
            {
                if (string.Equals(sortedList.GetByIndex(i).ToString(), strToFind, StringComparison.OrdinalIgnoreCase))
                {
                    //The string is found. This is the index
                    nIndex = i;
                    break;
                }
            }
            return nIndex;
        }


        /// <summary>
        /// This function will convert the given CIMTYPE to an acceptable .NET type.
        /// Since CLS doesn't support lotz of the basic types, we are using .NET helper
        /// classes here. We safely assume that there won't be any problem using them
        /// since .NET has to be there for the System.Management.Dll to work.
        /// </summary>
        /// <param name="cType"> </param>
        /// <param name="isArray"> </param>
        private CodeTypeReference ConvertCIMType(CimType cType, bool isArray)
        {
            string strType;
            switch (cType)
            {
                case CimType.SInt8:
                    {
                        strType = "System.SByte";
                        break;
                    }
                case CimType.UInt8:
                    {
                        strType = "System.Byte";
                        break;
                    }
                case CimType.SInt16:
                    {
                        strType = "System.Int16";
                        break;
                    }
                case CimType.UInt16:
                    {
                        if (bUnsignedSupported == false)
                        {
                            strType = "System.Int16";
                        }
                        else
                        {
                            strType = "System.UInt16";
                        }
                        break;
                    }
                case CimType.SInt32:
                    {
                        strType = "System.Int32";
                        break;
                    }
                case CimType.UInt32:
                    {
                        if (bUnsignedSupported == false)
                        {
                            strType = "System.Int32";
                        }
                        else
                        {
                            strType = "System.UInt32";
                        }
                        break;
                    }
                case CimType.SInt64:
                    {
                        strType = "System.Int64";
                        break;
                    }
                case CimType.UInt64:
                    {
                        if (bUnsignedSupported == false)
                        {
                            strType = "System.Int64";
                        }
                        else
                        {
                            strType = "System.UInt64";
                        }
                        break;
                    }
                case CimType.Real32:
                    {
                        strType = "System.Single";
                        break;
                    }
                case CimType.Real64:
                    {
                        strType = "System.Double";
                        break;
                    }
                case CimType.Boolean:
                    {
                        strType = "System.Boolean";
                        break;
                    }
                case CimType.String:
                    {
                        strType = "System.String";
                        break;
                    }
                case CimType.DateTime:
                    {
                        strType = "System.DateTime";
                        break;
                    }
                case CimType.Reference:
                    {
                        strType = PublicNamesUsed["PathClass"].ToString();
                        break;
                    }
                case CimType.Char16:
                    {
                        strType = "System.Char";
                        break;
                    }
                case CimType.Object:
                default:
                    strType = PublicNamesUsed["BaseObjClass"].ToString();
                    break;
            }

            if (isArray)
            {
                return new CodeTypeReference(strType, 1);
            }
            else
            {
                return new CodeTypeReference(strType);
            }
        }
        /// <summary>
        /// This function is used to determine whether the given CIMTYPE can be represented as an integer.
        /// This helper function is mainly used to determine whether this type will be support by enums.
        /// </summary>
        /// <param name="cType"> </param>
        private static bool isTypeInt(CimType cType)
        {
            bool retVal;
            switch (cType)
            {
                case CimType.UInt8:
                case CimType.UInt16:
                case CimType.UInt32:        // FIXX VB code generator cannot have Long enumerators
                case CimType.SInt8:
                case CimType.SInt16:
                case CimType.SInt32:
                    {
                        retVal = true;
                        break;
                    }
                case CimType.SInt64:
                case CimType.UInt64:
                case CimType.Real32:
                case CimType.Real64:
                case CimType.Boolean:
                case CimType.String:
                case CimType.DateTime:
                case CimType.Reference:
                case CimType.Char16:
                case CimType.Object:
                default:
                    retVal = false;
                    break;
            }

            return retVal;

        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public string GeneratedFileName
        {
            get
            {
                return genFileName;
            }
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public string GeneratedTypeName
        {
            get
            {
                return PrivateNamesUsed["GeneratedNamespace"].ToString() + "." +
                    PrivateNamesUsed["GeneratedClassName"].ToString();
            }
        }

        /// <summary>
        /// Function to convert a given ValueMap or BitMap name to property enum name
        /// </summary>
        private static string ConvertValuesToName(string str)
        {
            string strRet = string.Empty;
            string strToReplace = "_";
            string strToAdd = string.Empty;
            bool bAdd = true;
            if (str.Length == 0)
            {
                return string.Empty;
            }

            char[] arrString = str.ToCharArray();
            // First character
            if (char.IsLetter(arrString[0]) == false)
            {
                strRet = "Val_";
                strToAdd = "l";
            }

            for (int i = 0; i < str.Length; i++)
            {
                bAdd = true;
                if (char.IsLetterOrDigit(arrString[i]) == false)
                {
                    // if the previous character added is "_" then
                    // don't add that to the output string again
                    if (strToAdd == strToReplace)
                    {
                        bAdd = false;
                    }
                    else
                    {
                        strToAdd = strToReplace;
                    }
                }
                else
                {
                    strToAdd = new string(arrString[i], 1);
                }

                if (bAdd)
                {
                    strRet = string.Concat(strRet, strToAdd);
                }
            }
            return strRet;
        }

        /// <summary>
        /// This function goes thru the names in array list and resolves any duplicates
        /// if any so that these names can be added as values of enum
        /// </summary>
        private void ResolveEnumNameValues(ArrayList arrIn, ref ArrayList arrayOut)
        {
            arrayOut.Clear();
            int nCurIndex = 0;
            string strToAdd = string.Empty;
            IFormatProvider formatProv = (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(int));

            for (int i = 0; i < arrIn.Count; i++)
            {
                strToAdd = arrIn[i].ToString();
                strToAdd = ResolveCollision(strToAdd, true);
                if (IsContainedInArray(strToAdd, arrayOut))
                {
                    nCurIndex = 0;
                    strToAdd = arrIn[i].ToString() + nCurIndex.ToString(formatProv);
                    while (IsContainedInArray(strToAdd, arrayOut))
                    {
                        nCurIndex++;
                        strToAdd = arrIn[i].ToString() + nCurIndex.ToString(formatProv);
                    }

                }
                arrayOut.Add(strToAdd);
            }

        }

        /// <summary>
        /// This function will find a given string in the passed
        /// array list.
        /// </summary>
        private static bool IsContainedInArray(string strToFind, ArrayList arrToSearch)
        {
            for (int i = 0; i < arrToSearch.Count; i++)
            {
                if (string.Equals(arrToSearch[i].ToString(), strToFind, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Function to create a appropriate generator
        /// </summary>
        private bool InitializeCodeGenerator(CodeLanguage lang)
        {
            string strProvider = "";
            Assembly asm = null;
            Type codeProvType = null;
            bool bSucceeded = true;
            AssemblyName curAssemblyName = null;
            AssemblyName assemblyName = null;

            try
            {
                switch (lang)
                {
                    case CodeLanguage.VB:
                        strProvider = "Visual Basic.";
                        cp = new VBCodeProvider();
                        break;

                    case CodeLanguage.JScript:
                        strProvider = "JScript.NET.";
                        bSucceeded = false; // JScriptCodeProvider does not exist on .NET Core
                        break;

                    case CodeLanguage.CSharp:
                        strProvider = "C#.";
                        cp = new CSharpCodeProvider();
                        break;

                    case CodeLanguage.VJSharp:
                        strProvider = "Visual J#.";
                        bSucceeded = false;
                        curAssemblyName = Assembly.GetExecutingAssembly().GetName();
                        assemblyName = new AssemblyName();

                        assemblyName.CultureInfo = new CultureInfo("");
                        assemblyName.Name = "VJSharpCodeProvider";
                        assemblyName.SetPublicKey(curAssemblyName.GetPublicKey());
                        assemblyName.Version = curAssemblyName.Version;
                        asm = Assembly.Load(assemblyName);
                        //asm = Assembly.LoadWithPartialName("VJSharpCodeProvider");
                        if (asm != null)
                        {
                            codeProvType = asm.GetType("Microsoft.VJSharp.VJSharpCodeProvider");
                            if (codeProvType != null)
                            {
                                cp = (System.CodeDom.Compiler.CodeDomProvider)Activator.CreateInstance(codeProvType);
                                bSucceeded = true;
                            }
                        }
                        break;
                    case CodeLanguage.Mcpp:
                        strProvider = "Managed C++.";
                        bSucceeded = false;
                        curAssemblyName = Assembly.GetExecutingAssembly().GetName();
                        assemblyName = new AssemblyName();

                        assemblyName.CultureInfo = new CultureInfo("");
                        assemblyName.SetPublicKey(curAssemblyName.GetPublicKey());
                        assemblyName.Name = "CppCodeProvider";
                        assemblyName.Version = new Version(VSVERSION);
                        asm = Assembly.Load(assemblyName);

                        if (asm != null)
                        {
                            codeProvType = asm.GetType("Microsoft.VisualC.CppCodeProvider");
                            if (codeProvType != null)
                            {
                                cp = (System.CodeDom.Compiler.CodeDomProvider)Activator.CreateInstance(codeProvType);
                                bSucceeded = true;
                            }
                        }
                        break;
                }
            }
            catch
            {
                throw new ArgumentOutOfRangeException(SR.Format(SR.UnableToCreateCodeGeneratorException, strProvider));
            }

            if (bSucceeded)
            {
                GetUnsignedSupport(lang);
            }
            else
            {
                throw new ArgumentOutOfRangeException(SR.Format(SR.UnableToCreateCodeGeneratorException, strProvider));
            }
            return true;
        }

        /// <summary>
        /// Function which checks if the language supports Unsigned numbers
        /// </summary>
        /// <param name="Language">Language</param>
        private void GetUnsignedSupport(CodeLanguage Language)
        {
            switch (Language)
            {
                case CodeLanguage.CSharp:
                    bUnsignedSupported = true;
                    break;

                case CodeLanguage.VB:
                case CodeLanguage.JScript:
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Function which adds commit function to commit all the changes
        /// to the object to WMI
        /// </summary>
        private void GenerateCommitMethod()
        {
            cmm = new CodeMemberMethod();
            cmm.Name = PublicNamesUsed["CommitMethod"].ToString();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final;

            caa = new CodeAttributeArgument();
            caa.Value = new CodePrimitiveExpression(true);
            cad = new CodeAttributeDeclaration();
            cad.Name = "Browsable";
            cad.Arguments.Add(caa);
            cmm.CustomAttributes = new CodeAttributeDeclarationCollection();
            cmm.CustomAttributes.Add(cad);

            cis = new CodeConditionStatement();
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(PrivateNamesUsed["IsEmbedded"].ToString());
            cboe.Right = new CodePrimitiveExpression(false);
            cboe.Operator = CodeBinaryOperatorType.ValueEquality;
            cis.Condition = cboe;

            cmie = new CodeMethodInvokeExpression();
            cmie.Method.TargetObject = new CodeVariableReferenceExpression(PrivateNamesUsed["LateBoundObject"].ToString());
            cmie.Method.MethodName = "Put";

            cis.TrueStatements.Add(new CodeExpressionStatement(cmie));
            cmm.Statements.Add(cis);
            cc.Members.Add(cmm);


            // Adding a overloaded method for PutOptions parameter
            cmm = new CodeMemberMethod();
            cmm.Name = PublicNamesUsed["CommitMethod"].ToString();
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final;

            CodeParameterDeclarationExpression cpde = new CodeParameterDeclarationExpression();
            cpde.Type = new CodeTypeReference(PublicNamesUsed["PutOptions"].ToString());
            cpde.Name = PrivateNamesUsed["putOptions"].ToString();
            cmm.Parameters.Add(cpde);

            caa = new CodeAttributeArgument();
            caa.Value = new CodePrimitiveExpression(true);
            cad = new CodeAttributeDeclaration();
            cad.Name = "Browsable";
            cad.Arguments.Add(caa);
            cmm.CustomAttributes = new CodeAttributeDeclarationCollection();
            cmm.CustomAttributes.Add(cad);

            cis = new CodeConditionStatement();
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(PrivateNamesUsed["IsEmbedded"].ToString());
            cboe.Right = new CodePrimitiveExpression(false);
            cboe.Operator = CodeBinaryOperatorType.ValueEquality;
            cis.Condition = cboe;

            cmie = new CodeMethodInvokeExpression();
            cmie.Method.TargetObject = new CodeVariableReferenceExpression(PrivateNamesUsed["LateBoundObject"].ToString());
            cmie.Method.MethodName = "Put";
            cmie.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["putOptions"].ToString()));

            cis.TrueStatements.Add(new CodeExpressionStatement(cmie));
            cmm.Statements.Add(cis);
            cc.Members.Add(cmm);
        }

        /// <summary>
        /// Function to convert a value in format "0x..." to a integer
        /// to the object to WMI
        /// </summary>
        private static int ConvertBitMapValueToInt32(string bitMap)
        {
            string strTemp = "0x";
            int ret = 0;

            if (bitMap.StartsWith(strTemp, StringComparison.Ordinal) || bitMap.StartsWith(strTemp.ToUpperInvariant(), StringComparison.Ordinal))
            {
                strTemp = string.Empty;
                char[] arrString = bitMap.ToCharArray();
                int Len = bitMap.Length;
                for (int i = 2; i < Len; i++)
                {
                    strTemp += arrString[i];
                }
                ret = System.Convert.ToInt32(strTemp, (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(int)));
            }
            else
            {
                ret = System.Convert.ToInt32(bitMap, (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(int)));
            }

            return ret;
        }


        /// <summary>
        /// Function to get the Converstion function to be used for Numeric datatypes
        /// </summary>
        private string GetConversionFunction(CimType cimType)
        {
            string retFunctionName = string.Empty;

            switch (cimType)
            {
                case CimType.UInt8:
                    retFunctionName = "ToByte";
                    break;

                case CimType.SInt8:
                    retFunctionName = "ToSByte";
                    break;

                case CimType.SInt16:
                    retFunctionName = "ToInt16";
                    break;

                case CimType.UInt16:
                    if (bUnsignedSupported == false)
                    {
                        retFunctionName = "ToInt16";
                    }
                    else
                    {
                        retFunctionName = "ToUInt16";
                    }
                    break;

                case CimType.SInt32:

                    retFunctionName = "ToInt32";
                    break;

                case CimType.UInt32:
                    {
                        if (bUnsignedSupported == false)
                        {
                            retFunctionName = "ToInt32";
                        }
                        else
                        {
                            retFunctionName = "ToUInt32";
                        }
                        break;
                    }
                case CimType.SInt64:
                    {
                        retFunctionName = "ToInt64";
                        break;
                    }
                case CimType.UInt64:
                    {
                        if (bUnsignedSupported == false)
                        {
                            retFunctionName = "ToInt64";
                        }
                        else
                        {
                            retFunctionName = "ToUInt64";
                        }
                        break;
                    }
                case CimType.Real32:
                    {
                        retFunctionName = "ToSingle";
                        break;
                    }
                case CimType.Real64:
                    {
                        retFunctionName = "ToDouble";
                        break;
                    }
                case CimType.Boolean:
                    {
                        retFunctionName = "ToBoolean";
                        break;
                    }

                case CimType.Char16:
                    {
                        retFunctionName = "ToChar";
                        break;
                    }

                case CimType.String:
                    {
                        retFunctionName = "ToString";
                        break;
                    }

            }
            return retFunctionName;
        }

        /// <summary>
        /// Checks if a given property is to be visible for Designer seriliazation
        /// </summary>
        private static bool IsDesignerSerializationVisibilityToBeSet(string propName)
        {
            if (!string.Equals(propName, "Path", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// Checks if the given property type is represented as ValueType
        /// </summary>
        private static bool IsPropertyValueType(CimType cType)
        {
            bool ret = true;
            switch (cType)
            {
                case CimType.String:
                case CimType.Reference:
                case CimType.Object:
                    ret = false;
                    break;

            }
            return ret;
        }

        /// <summary>
        /// Gets the dynamic qualifier on the class to find if the
        /// class is a dynamic class
        /// </summary>
        private bool IsDynamicClass()
        {
            bool ret = false;
            try
            {
                ret = System.Convert.ToBoolean(classobj.Qualifiers["dynamic"].Value, (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(bool)));
            }
            catch (ManagementException)
            {
                // do nothing. This may be due to dynamic qualifer not present which is equivalent
                // dynamic qualifier absent
            }
            return ret;
        }


        /// <summary>
        /// Converts a numeric value to appropriate type and adds it to array
        /// </summary>
        private static string ConvertToNumericValueAndAddToArray(CimType cimType, string numericValue, ArrayList arrayToAdd, out string enumType)
        {
            string retFunctionName = string.Empty;
            enumType = string.Empty;

            switch (cimType)
            {
                case CimType.UInt8:
                case CimType.SInt8:
                case CimType.SInt16:
                case CimType.UInt16:
                case CimType.SInt32:
                case CimType.UInt32:
                    arrayToAdd.Add(System.Convert.ToInt32(numericValue, (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(int))));
                    retFunctionName = "ToInt32";
                    enumType = "System.Int32";
                    break;
            }
            return retFunctionName;
        }

        /*
        const int IDS_CommentShouldSerialize        = 0;
        const int IDS_CommentIsPropNull            = 1;
        const int IDS_CommentResetProperty                = 2;
        const int IDS_CommentAttributeProperty            = 3;
        const int IDS_CommentDateConversionFunction            = 4;
        const int IDS_CommentGetInstances            = 5;
        const int IDS_CommentClassBegin            = 6;
        const int IDS_COMMENT_PRIV_AUTOCOMMIT        = 7;
        const int IDS_CommentConstructors            = 8;
        const int IDS_COMMENT_ORIG_NAMESPACE        = 9;
        const int IDS_COMMENT_CLASSNAME                = 10;
        const int IDS_CommentSystemObject                = 11;
        const int IDS_CommentLateBoundObject            = 12;
        const int IDS_CommentManagementScope                = 13;
        const int IDS_CommentAutoCommitProperty        = 14;
        const int IDS_CommentManagementPath                = 15;
        const int IDS_COMMENT_PROP_TYPECONVERTER    = 16;
        const int IDS_CommentSystemPropertiesClass            = 17;
        const int IDS_CommentEnumeratorImplementation                = 18;
        const int IDS_CommentLateBoundProperty            = 19;
        const int IDS_COMMENTS_CREATEDCLASS            = 20;
        const int IDS_CommentEmbeddedObject            = 21;
        const int IDS_CommentCurrentObject            = 22;
        const int IDS_CommentFlagForEmbedded        = 23;

        /// <summary>
        /// Function to initialize the comments to be put in the generated code
        /// Later can be moved to Resource once resource for System.management.dll is setup
        /// </summary>
        private void InitializeComments()
        {
            string strComment = "Functions ShouldSerialize<PropertyName> are functions used by VS property browser to check if";

            strComment = strComment  + " a particular property has to be serialized. These functions are added for all ValueType";
            strComment = strComment  + " properties ( properties of type Int32, BOOL etc.. which cannot be set to null). These functions";
            strComment = strComment  + " uses Is<PropertyName>Null function. These functions are also used in the TypeConverter implementation";
            strComment = strComment  + " for the properties to check for NULL value of property so that a empty value can be shown";
            strComment = strComment  + " in Property browser in case of Drag and Drop in Visual studio.";

            CommentsString.Add(strComment);    // IDS_CommentShouldSerialize

            strComment = "Functions Is<PropertyName>Null() are functions . These functions are to be used to check if a property is NULL.";
            CommentsString.Add(strComment);    // IDS_CommentIsPropNull

            strComment = "Functions Reset<PropertyName> are added for Nullable Read/Write properties. These functions are used in VS designere in";
            strComment = strComment + " property browser to set a property to NULL.";
            CommentsString.Add(strComment);    // IDS_CommentResetProperty

            strComment = "Every property added to the class for WMI property has attributes set to define its behaviour in Visual Studio";
            strComment = strComment + " designer and as well as to define a TypeConverter to be used.";
            CommentsString.Add(strComment);    // IDS_CommentAttributeProperty

            strComment = "DateTime Conversions are added for the class to convert DMTF date to System.DateTime and Vise-versa. Conversion from DMTF";
            strComment = strComment + " to System.DateTime conversion ignores the microseconds as System.DateTime doesn't have the microseconds part in it.";
            CommentsString.Add(strComment);    // IDS_CommentDateConversionFunction

            strComment = "Different flavours of GetInstances() help in enumerating instances of the WMI class.";
            CommentsString.Add(strComment);    // IDS_CommentGetInstances

            strComment = "An Early Bound class generated for the WMI class ";
            CommentsString.Add(strComment);    // IDS_CommentClassBegin

            CommentsString.Add("Member variable to store the autocommit behaviour for the class"); // IDS_COMMENT_PRIV_AUTOCOMMIT

            CommentsString.Add("Below are different flavours of constructors to initialize the instance with a WMI object"); // IDS_CommentConstructors

            CommentsString.Add("Property returns the namespace of the WMI class"); // IDS_COMMENT_ORIG_NAMESPACE

            CommentsString.Add("Name of the WMI class");    // IDS_COMMENT_CLASSNAME;

            CommentsString.Add("Property pointing to a embedded object to get System properties of the WMI object"); // IDS_CommentSystemObject

            CommentsString.Add("Underlying lateBound WMI object"); // IDS_CommentLateBoundObject

            CommentsString.Add(" ManagementScope of the object"); //  IDS_CommentManagementScope

            strComment = "Property to show the autocommit behaviour for the WMI object. If this is";
            strComment = strComment + "true then WMI object is saved to WMI then for change in every";
            strComment = strComment + "property (ie Put is called after modification of a property) ";
            CommentsString.Add(strComment); // IDS_CommentAutoCommitProperty

            CommentsString.Add("The ManagementPath of the underlying WMI object"); // IDS_CommentManagementPath

            CommentsString.Add("TypeConverter to handle null values for ValueType properties"); // IDS_COMMENT_PROP_TYPECONVERTER

            CommentsString.Add(" Embedded class to represent WMI system Properties"); // IDS_CommentSystemPropertiesClass

            CommentsString.Add("Enumerator implementation for enumerating instances of the class"); // IDS_CommentEnumeratorImplementation
            CommentsString.Add("Property returning the underlying lateBound object"); // IDS_CommentLateBoundProperty

            CommentsString.Add("Private property to hold the name of WMI class which created this class"); // IDS_COMMENTS_CREATEDCLASS
            CommentsString.Add("Private variable to hold the embedded property representing the instance"); // IDS_CommentEmbeddedObject
            CommentsString.Add("The current WMI object used"); //IDS_CommentCurrentObject
            CommentsString.Add("Flag to indicate if an instance is an embedded object"); // IDS_CommentFlagForEmbedded
        }
        */

        /// <summary>
        /// Adds comments at the beginning of the class defination
        /// </summary>
        private void AddClassComments(CodeTypeDeclaration cc)
        {
            cc.Comments.Add(new CodeCommentStatement(SR.CommentShouldSerialize));
            cc.Comments.Add(new CodeCommentStatement(SR.CommentIsPropNull));
            cc.Comments.Add(new CodeCommentStatement(SR.CommentResetProperty));
            cc.Comments.Add(new CodeCommentStatement(SR.CommentAttributeProperty));

        }

        /// <summary>
        /// Generates code for ManagementClassName Property
        /// </summary>
        private void GenerateClassNameProperty()
        {
            string strRetVar = "strRet";
            cmp = new CodeMemberProperty();
            cmp.Name = PublicNamesUsed["ClassNameProperty"].ToString();
            cmp.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            cmp.Type = new CodeTypeReference("System.String");

            caa = new CodeAttributeArgument();
            caa.Value = new CodePrimitiveExpression(true);
            cad = new CodeAttributeDeclaration();
            cad.Name = "Browsable";
            cad.Arguments.Add(caa);
            cmp.CustomAttributes = new CodeAttributeDeclarationCollection();
            cmp.CustomAttributes.Add(cad);

            caa = new CodeAttributeArgument();
            caa.Value = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("DesignerSerializationVisibility"), "Hidden");
            cad = new CodeAttributeDeclaration();
            cad.Name = "DesignerSerializationVisibility";
            cad.Arguments.Add(caa);
            cmp.CustomAttributes.Add(cad);

            cmp.GetStatements.Add(new CodeVariableDeclarationStatement("System.String", strRetVar,
                new CodeVariableReferenceExpression(PrivateNamesUsed["CreationClassName"].ToString())));


            cis = new CodeConditionStatement();
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(PrivateNamesUsed["CurrentObject"].ToString());
            cboe.Right = new CodePrimitiveExpression(null);
            cboe.Operator = CodeBinaryOperatorType.IdentityInequality;
            cis.Condition = cboe;

            CodeConditionStatement cis1 = new CodeConditionStatement();

            CodeBinaryOperatorExpression cboe1 = new CodeBinaryOperatorExpression();
            cboe1.Left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(PrivateNamesUsed["CurrentObject"].ToString()),
                PublicNamesUsed["ClassPathProperty"].ToString());
            cboe1.Right = new CodePrimitiveExpression(null);
            cboe1.Operator = CodeBinaryOperatorType.IdentityInequality;
            cis1.Condition = cboe1;

            cis.TrueStatements.Add(cis1);

            cis1.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(strRetVar),
                new CodeCastExpression(new CodeTypeReference("System.String"),
                new CodeIndexerExpression(new CodeVariableReferenceExpression(PrivateNamesUsed["CurrentObject"].ToString()),
                new CodePrimitiveExpression("__CLASS")))));

            CodeConditionStatement cis2 = new CodeConditionStatement();

            CodeBinaryOperatorExpression cboe3 = new CodeBinaryOperatorExpression();
            cboe3.Left = new CodeVariableReferenceExpression(strRetVar);
            cboe3.Right = new CodePrimitiveExpression(null);
            cboe3.Operator = CodeBinaryOperatorType.IdentityEquality;

            CodeBinaryOperatorExpression cboe4 = new CodeBinaryOperatorExpression();
            cboe4.Left = new CodeVariableReferenceExpression(strRetVar);
            cboe4.Right = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.String"), "Empty");
            cboe4.Operator = CodeBinaryOperatorType.IdentityEquality;

            CodeBinaryOperatorExpression cboe5 = new CodeBinaryOperatorExpression();
            cboe5.Left = cboe3;
            cboe5.Right = cboe4;
            cboe5.Operator = CodeBinaryOperatorType.BooleanOr;

            cis2.Condition = cboe5;

            cis2.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(strRetVar),
                new CodeVariableReferenceExpression(PrivateNamesUsed["CreationClassName"].ToString())));

            cis1.TrueStatements.Add(cis2);

            cmp.GetStatements.Add(cis);

            cmp.GetStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(strRetVar)));
            cc.Members.Add(cmp);
        }

        /// <summary>
        /// Generates the functions CheckIfProperClass() which checks if the given path
        /// can be represented with the generated code
        /// </summary>
        private void GenerateIfClassvalidFuncWithAllParams()
        {
            string strPathParam = "path";
            string strGetOptions = "OptionsParam";

            cmm = new CodeMemberMethod();
            cmm.Name = PrivateNamesUsed["ClassNameCheckFunc"].ToString();
            cmm.Attributes = MemberAttributes.Private | MemberAttributes.Final;
            cmm.ReturnType = new CodeTypeReference("System.Boolean");

            cmm.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["ScopeClass"].ToString()), PrivateNamesUsed["ScopeParam"].ToString()));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["PathClass"].ToString()), strPathParam));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["GetOptionsClass"].ToString()), strGetOptions));


            CodeExpression[] parms = new CodeExpression[]
            {
                new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(strPathParam), "ClassName"),
                //            new CodeVariableReferenceExpression(PublicNamesUsed["ClassNameProperty"].ToString()),
                new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), PublicNamesUsed["ClassNameProperty"].ToString()),
                new CodePrimitiveExpression(true),
                new CodePropertyReferenceExpression(new CodeTypeReferenceExpression("System.Globalization.CultureInfo"), "InvariantCulture")
            };

            cmie = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("System.String"),
                "Compare",
                parms
                );

            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = cmie;
            cboe.Right = new CodePrimitiveExpression(0);
            cboe.Operator = CodeBinaryOperatorType.ValueEquality;

            CodeBinaryOperatorExpression cboe1 = new CodeBinaryOperatorExpression();
            cboe1.Left = new CodeVariableReferenceExpression(strPathParam);
            cboe1.Right = new CodePrimitiveExpression(null);
            cboe1.Operator = CodeBinaryOperatorType.IdentityInequality;

            CodeBinaryOperatorExpression cboe2 = new CodeBinaryOperatorExpression();
            cboe2.Left = cboe1;
            cboe2.Right = cboe;
            cboe2.Operator = CodeBinaryOperatorType.BooleanAnd;

            cis = new CodeConditionStatement();
            cis.Condition = cboe2;

            cis.TrueStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(true)));

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["LateBoundClass"].ToString());
            coce.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["ScopeParam"].ToString()));
            coce.Parameters.Add(new CodeVariableReferenceExpression(strPathParam));
            coce.Parameters.Add(new CodeVariableReferenceExpression(strGetOptions));

            CodeMethodReferenceExpression cmre = new CodeMethodReferenceExpression();
            cmre.MethodName = PrivateNamesUsed["ClassNameCheckFunc"].ToString();

            cis.FalseStatements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(cmre, coce)));
            cmm.Statements.Add(cis);

            cc.Members.Add(cmm);
        }
        /// <summary>
        /// Generates the functions CheckIfProperClass() which checks if the given path
        /// can be represented with the generated code
        /// </summary>
        private void GenerateIfClassvalidFunction()
        {
            // Call this function to generate the first overload of this function
            GenerateIfClassvalidFuncWithAllParams();

            string strTempObj = "theObj";
            string strnCtr = "count";
            string strDerivation = "parentClasses";

            cmm = new CodeMemberMethod();
            cmm.Name = PrivateNamesUsed["ClassNameCheckFunc"].ToString();
            cmm.Attributes = MemberAttributes.Private | MemberAttributes.Final;
            cmm.ReturnType = new CodeTypeReference("System.Boolean");

            cmm.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(PublicNamesUsed["BaseObjClass"].ToString()), strTempObj));

            CodeExpression[] parms = new CodeExpression[]
            {
                new CodeCastExpression(new CodeTypeReference("System.String"),
                new CodeIndexerExpression(new CodeVariableReferenceExpression(strTempObj),
                new CodePrimitiveExpression("__CLASS"))),
                new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), PublicNamesUsed["ClassNameProperty"].ToString()),
                new CodePrimitiveExpression(true),
                new CodePropertyReferenceExpression(new CodeTypeReferenceExpression("System.Globalization.CultureInfo"), "InvariantCulture")
            };

            cmie = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("System.String"),
                "Compare",
                parms
                );

            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = cmie;
            cboe.Right = new CodePrimitiveExpression(0);
            cboe.Operator = CodeBinaryOperatorType.ValueEquality;

            CodeBinaryOperatorExpression cboe1 = new CodeBinaryOperatorExpression();
            cboe1.Left = new CodeVariableReferenceExpression(strTempObj);
            cboe1.Right = new CodePrimitiveExpression(null);
            cboe1.Operator = CodeBinaryOperatorType.IdentityInequality;

            CodeBinaryOperatorExpression cboe2 = new CodeBinaryOperatorExpression();
            cboe2.Left = cboe1;
            cboe2.Right = cboe;
            cboe2.Operator = CodeBinaryOperatorType.BooleanAnd;

            cis = new CodeConditionStatement();
            cis.Condition = cboe2;

            cis.TrueStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(true)));

            CodeExpression cs = new CodeCastExpression(new CodeTypeReference("System.Array"),
                new CodeIndexerExpression(new CodeVariableReferenceExpression(strTempObj),
                new CodePrimitiveExpression("__DERIVATION")));


            cis.FalseStatements.Add(new CodeVariableDeclarationStatement("System.Array", strDerivation, cs));

            CodeConditionStatement cis1 = new CodeConditionStatement();
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(strDerivation);
            cboe.Right = new CodePrimitiveExpression(null);
            cboe.Operator = CodeBinaryOperatorType.IdentityInequality;
            cis1.Condition = cboe;

            cfls = new CodeIterationStatement();

            cis1.TrueStatements.Add(new CodeVariableDeclarationStatement("System.Int32", strnCtr, new CodePrimitiveExpression(0)));
            cfls.InitStatement = new CodeAssignStatement(new CodeVariableReferenceExpression(strnCtr), new CodePrimitiveExpression(0));
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(strnCtr);
            cboe.Operator = CodeBinaryOperatorType.LessThan;
            cboe.Right = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(strDerivation), "Length");
            cfls.TestExpression = cboe;
            cfls.IncrementStatement = new CodeAssignStatement(new CodeVariableReferenceExpression(strnCtr),
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression(strnCtr),
                CodeBinaryOperatorType.Add,
                new CodePrimitiveExpression(1)));

            CodeMethodInvokeExpression cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method.MethodName = "GetValue";
            cmie1.Method.TargetObject = new CodeVariableReferenceExpression(strDerivation);
            cmie1.Parameters.Add(new CodeVariableReferenceExpression(strnCtr));

            CodeExpression[] parms1 = new CodeExpression[]
            {
                new CodeCastExpression(new CodeTypeReference("System.String"), cmie1),
                new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), PublicNamesUsed["ClassNameProperty"].ToString()),
                new CodePrimitiveExpression(true),
                new CodePropertyReferenceExpression(new CodeTypeReferenceExpression("System.Globalization.CultureInfo"), "InvariantCulture")
            };

            CodeMethodInvokeExpression cmie2 = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("System.String"),
                "Compare",
                parms1);

            CodeConditionStatement cis2 = new CodeConditionStatement();
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = cmie2;
            cboe.Right = new CodePrimitiveExpression(0);
            cboe.Operator = CodeBinaryOperatorType.ValueEquality;
            cis2.Condition = cboe;

            cis2.TrueStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(true)));

            cis1.TrueStatements.Add(cfls);
            cfls.Statements.Add(cis2);

            cis.FalseStatements.Add(cis1);

            cmm.Statements.Add(cis);
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(false)));
            cc.Members.Add(cmm);

        }

        /// <summary>
        /// Generates code for Property Get for Cimtype.Reference and CimType.DateTime type property
        /// Also generated code to initialize a variable after converting a property to DateTime and ManagementPathProperty
        /// </summary>
        private void GenerateCodeForRefAndDateTimeTypes(CodeIndexerExpression prop, bool bArray, CodeStatementCollection statColl, string strType, CodeVariableReferenceExpression varToAssign, bool bIsValueProprequired)
        {

            if (bArray == false)
            {

                CodeConditionStatement cis1 = new CodeConditionStatement();
                CodeBinaryOperatorExpression cboe1 = new CodeBinaryOperatorExpression();
                cboe1.Left = prop;
                cboe1.Operator = CodeBinaryOperatorType.IdentityInequality;
                cboe1.Right = new CodePrimitiveExpression(null);
                cis1.Condition = cboe1;

                // if the type is string then check for null is to be done
                // otherwise, the DateTime Conversion function will do for DateTime types
                if (string.Equals(strType, PublicNamesUsed["PathClass"].ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    CodeMethodReferenceExpression cmre = new CodeMethodReferenceExpression();
                    cmre.MethodName = "ToString";
                    cmre.TargetObject = prop;

                    cmie = new CodeMethodInvokeExpression();
                    cmie.Method = cmre;

                    if (varToAssign == null)
                    {
                        cis1.TrueStatements.Add(new CodeMethodReturnStatement(CreateObjectForProperty(strType, cmie)));
                        statColl.Add(cis1);
                        statColl.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
                    }
                    else
                    {
                        // Assign null to variable
                        statColl.Add(new CodeAssignStatement(varToAssign, new CodePrimitiveExpression(null)));
                        cis1.TrueStatements.Add(new CodeAssignStatement(varToAssign, CreateObjectForProperty(strType, cmie)));
                        statColl.Add(cis1);
                    }
                }
                else
                {

                    statColl.Add(cis1);
                    CodeExpression ce = null;
                    if (bIsValueProprequired)
                    {
                        ce = new CodeCastExpression(new CodeTypeReference("System.String"), new CodePropertyReferenceExpression(prop, "Value"));
                    }
                    else
                    {
                        ce = new CodeCastExpression(new CodeTypeReference("System.String"), prop);
                    }

                    if (varToAssign == null)
                    {
                        cis1.TrueStatements.Add(new CodeMethodReturnStatement(CreateObjectForProperty(strType, ce)));
                        cis1.FalseStatements.Add(new CodeMethodReturnStatement(CreateObjectForProperty(strType, null)));
                    }
                    else
                    {
                        cis1.TrueStatements.Add(new CodeAssignStatement(varToAssign, CreateObjectForProperty(strType, ce)));
                        cis1.FalseStatements.Add(new CodeAssignStatement(varToAssign, CreateObjectForProperty(strType, null)));
                    }
                }

            }
            else
            {
                string strLength = "len";
                string strnCtr = "iCounter";
                string strArray = "arrToRet";

                CodeConditionStatement cis1 = new CodeConditionStatement();
                CodeBinaryOperatorExpression cboe1 = new CodeBinaryOperatorExpression();
                cboe1.Left = prop;
                cboe1.Operator = CodeBinaryOperatorType.IdentityInequality;
                cboe1.Right = new CodePrimitiveExpression(null);
                cis1.Condition = cboe1;

                CodePropertyReferenceExpression LenProp = null;

                if (bIsValueProprequired)
                {
                    LenProp = new CodePropertyReferenceExpression(
                        new CodeCastExpression(
                        new CodeTypeReference("System.Array"),
                        new CodePropertyReferenceExpression(prop, "Value")
                        ),
                        "Length"
                        );
                }
                else
                {
                    LenProp = new CodePropertyReferenceExpression(
                        new CodeCastExpression(
                        new CodeTypeReference("System.Array"),
                        prop
                        ),
                        "Length"
                        );
                }
                cis1.TrueStatements.Add(
                    new CodeVariableDeclarationStatement(
                    new CodeTypeReference("System.Int32"),
                    strLength,
                    LenProp
                    )
                    );

                CodeTypeReference arrPathType = new CodeTypeReference(
                    new CodeTypeReference(strType),
                    1
                    );
                cis1.TrueStatements.Add(
                    new CodeVariableDeclarationStatement(
                    arrPathType,
                    strArray,
                    new CodeArrayCreateExpression(
                    new CodeTypeReference(strType),
                    new CodeVariableReferenceExpression(strLength)
                    )
                    )
                    );

                cfls = new CodeIterationStatement();

                cfls.InitStatement = new CodeVariableDeclarationStatement(
                    new CodeTypeReference("System.Int32"),
                    strnCtr,
                    new CodePrimitiveExpression(0)
                    );

                cboe1 = new CodeBinaryOperatorExpression();
                cboe1.Left = new CodeVariableReferenceExpression(strnCtr);
                cboe1.Operator = CodeBinaryOperatorType.LessThan;
                cboe1.Right = new CodeVariableReferenceExpression(strLength);

                cfls.TestExpression = cboe1;

                cfls.IncrementStatement = new CodeAssignStatement(
                    new CodeVariableReferenceExpression(strnCtr),
                    new CodeBinaryOperatorExpression(
                    new CodeVariableReferenceExpression(strnCtr),
                    CodeBinaryOperatorType.Add,
                    new CodePrimitiveExpression(1)
                    )
                    );

                CodeMethodInvokeExpression cmie1 = new CodeMethodInvokeExpression();
                cmie1.Method.MethodName = "GetValue";
                if (bIsValueProprequired)
                {
                    cmie1.Method.TargetObject = new CodeCastExpression(new CodeTypeReference("System.Array"), new CodePropertyReferenceExpression(prop, "Value"));
                }
                else
                {
                    cmie1.Method.TargetObject = new CodeCastExpression(new CodeTypeReference("System.Array"), prop);
                }
                cmie1.Parameters.Add(new CodeVariableReferenceExpression(strnCtr));

                CodeMethodInvokeExpression cmie2 = new CodeMethodInvokeExpression();
                cmie2.Method.MethodName = "ToString";
                cmie2.Method.TargetObject = cmie1;

                cfls.Statements.Add(new CodeAssignStatement(new CodeIndexerExpression(new CodeVariableReferenceExpression(strArray),
                    new CodeVariableReferenceExpression(strnCtr)), CreateObjectForProperty(strType, cmie2)));

                cis1.TrueStatements.Add(cfls);
                if (varToAssign == null)
                {
                    cis1.TrueStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(strArray)));
                    statColl.Add(cis1);
                    statColl.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
                }
                else
                {
                    // Assign null to variable
                    statColl.Add(new CodeAssignStatement(varToAssign, new CodePrimitiveExpression(null)));
                    cis1.TrueStatements.Add(new CodeAssignStatement(varToAssign, new CodeVariableReferenceExpression(strArray)));
                    statColl.Add(cis1);
                }

            }
        }


        /// <summary>
        /// Generates code for Property Set for Cimtype.DateTime and CimType.Reference type property
        /// </summary>
        private void AddPropertySet(CodeIndexerExpression prop, bool bArray, CodeStatementCollection statColl, string strType, CodeVariableReferenceExpression varValue)
        {
            varValue ??= new CodeVariableReferenceExpression("value");

            if (bArray == false)
            {
                statColl.Add(new CodeAssignStatement(prop,
                    ConvertPropertyToString(strType, varValue)));
            }
            else
            {
                string strLength = "len";
                string strnCtr = "iCounter";
                string strArray = "arrProp";

                CodeConditionStatement cis1 = new CodeConditionStatement();
                CodeBinaryOperatorExpression cboe1 = new CodeBinaryOperatorExpression();
                cboe1.Left = varValue;
                cboe1.Operator = CodeBinaryOperatorType.IdentityInequality;
                cboe1.Right = new CodePrimitiveExpression(null);
                cis1.Condition = cboe1;

                CodePropertyReferenceExpression LenProp =
                    new CodePropertyReferenceExpression(
                    new CodeCastExpression(
                    new CodeTypeReference("System.Array"),
                    varValue
                    ),
                    "Length"
                    );

                cis1.TrueStatements.Add(
                    new CodeVariableDeclarationStatement(
                    new CodeTypeReference("System.Int32"),
                    strLength,
                    LenProp
                    )
                    );

                CodeTypeReference arrPathType =
                    new CodeTypeReference(new CodeTypeReference("System.String"), 1);

                cis1.TrueStatements.Add(
                    new CodeVariableDeclarationStatement(
                    arrPathType,
                    strArray,
                    new CodeArrayCreateExpression(
                    new CodeTypeReference("System.String"),
                    new CodeVariableReferenceExpression(strLength)
                    )
                    )
                    );

                cfls = new CodeIterationStatement();

                cfls.InitStatement = new CodeVariableDeclarationStatement(
                    new CodeTypeReference("System.Int32"),
                    strnCtr,
                    new CodePrimitiveExpression(0)
                    );

                cboe1 = new CodeBinaryOperatorExpression();
                cboe1.Left = new CodeVariableReferenceExpression(strnCtr);
                cboe1.Operator = CodeBinaryOperatorType.LessThan;
                cboe1.Right = new CodeVariableReferenceExpression(strLength);

                cfls.TestExpression = cboe1;

                cfls.IncrementStatement =
                    new CodeAssignStatement(
                    new CodeVariableReferenceExpression(strnCtr),
                    new CodeBinaryOperatorExpression(
                    new CodeVariableReferenceExpression(strnCtr),
                    CodeBinaryOperatorType.Add,
                    new CodePrimitiveExpression(1)
                    )
                    );

                CodeMethodInvokeExpression cmie1 = new CodeMethodInvokeExpression();
                cmie1.Method.MethodName = "GetValue";
                cmie1.Method.TargetObject = new CodeCastExpression(new CodeTypeReference("System.Array"), varValue);

                cmie1.Parameters.Add(new CodeVariableReferenceExpression(strnCtr));

                cfls.Statements.Add(new CodeAssignStatement(new CodeIndexerExpression(new CodeVariableReferenceExpression(strArray),
                    new CodeVariableReferenceExpression(strnCtr)), ConvertPropertyToString(strType, cmie1)));

                cis1.TrueStatements.Add(cfls);

                cis1.TrueStatements.Add(new CodeAssignStatement(prop, new CodeVariableReferenceExpression(strArray)));
                cis1.FalseStatements.Add(new CodeAssignStatement(prop, new CodePrimitiveExpression(null)));
                statColl.Add(cis1);
            }
        }

        /// <summary>
        /// Internal function used to create object. Used in adding code for Property Get for DateTime and Reference properties
        /// </summary>
        private CodeExpression CreateObjectForProperty(string strType, CodeExpression param)
        {
            switch (strType)
            {
                case "System.DateTime":
                    if (param == null)
                    {
                        return new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.DateTime"), "MinValue");
                    }
                    else
                    {
                        cmie = new CodeMethodInvokeExpression();
                        cmie.Parameters.Add(param);
                        cmie.Method.MethodName = PrivateNamesUsed["ToDateTimeMethod"].ToString();
                        return cmie;
                    }

                case "System.TimeSpan":
                    if (param == null)
                    {
                        coce = new CodeObjectCreateExpression();
                        coce.CreateType = new CodeTypeReference("System.TimeSpan");
                        coce.Parameters.Add(new CodePrimitiveExpression(0));
                        coce.Parameters.Add(new CodePrimitiveExpression(0));
                        coce.Parameters.Add(new CodePrimitiveExpression(0));
                        coce.Parameters.Add(new CodePrimitiveExpression(0));
                        coce.Parameters.Add(new CodePrimitiveExpression(0));
                        return coce;
                    }
                    else
                    {
                        cmie = new CodeMethodInvokeExpression();
                        cmie.Parameters.Add(param);
                        cmie.Method.MethodName = PrivateNamesUsed["ToTimeSpanMethod"].ToString();
                        return cmie;
                    }

                case "System.Management.ManagementPath":
                    coce = new CodeObjectCreateExpression();
                    coce.CreateType = new CodeTypeReference(PublicNamesUsed["PathClass"].ToString());
                    coce.Parameters.Add(param);
                    return coce;


            }

            return null;
        }
        /// <summary>
        /// Internal function used to create code to convert DateTime or ManagementPath to String
        /// convert a expression. Used in adding code for Property Set for DateTime and Reference properties
        /// </summary>
        private CodeExpression ConvertPropertyToString(string strType, CodeExpression beginningExpression)
        {
            switch (strType)
            {
                case "System.DateTime":

                    CodeMethodInvokeExpression cmie1 = new CodeMethodInvokeExpression();
                    cmie1.Parameters.Add(new CodeCastExpression(new CodeTypeReference("System.DateTime"), beginningExpression));
                    cmie1.Method.MethodName = PrivateNamesUsed["ToDMTFDateTimeMethod"].ToString();
                    return cmie1;

                case "System.TimeSpan":

                    CodeMethodInvokeExpression cmie2 = new CodeMethodInvokeExpression();
                    cmie2.Parameters.Add(new CodeCastExpression(new CodeTypeReference("System.TimeSpan"), beginningExpression));
                    cmie2.Method.MethodName = PrivateNamesUsed["ToDMTFTimeIntervalMethod"].ToString();
                    return cmie2;

                case "System.Management.ManagementPath":
                    return new CodePropertyReferenceExpression(new CodeCastExpression(
                        new CodeTypeReference(PublicNamesUsed["PathClass"].ToString()),
                        beginningExpression), PublicNamesUsed["PathProperty"].ToString());


            }

            return null;
        }

        private void GenerateScopeProperty()
        {
            cmp = new CodeMemberProperty();
            cmp.Name = PublicNamesUsed["ScopeProperty"].ToString();
            cmp.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            cmp.Type = new CodeTypeReference(PublicNamesUsed["ScopeClass"].ToString());

            caa = new CodeAttributeArgument();
            caa.Value = new CodePrimitiveExpression(true);
            cad = new CodeAttributeDeclaration();
            cad.Name = "Browsable";
            cad.Arguments.Add(caa);
            cmp.CustomAttributes = new CodeAttributeDeclarationCollection();
            cmp.CustomAttributes.Add(cad);

            // If the property is not Path then add an attribb DesignerSerializationVisibility
            // to indicate that the property is to be hidden for designer serilization.
            if (IsDesignerSerializationVisibilityToBeSet(PublicNamesUsed["ScopeProperty"].ToString()))
            {
                caa = new CodeAttributeArgument();
                caa.Value = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("DesignerSerializationVisibility"), "Hidden");
                cad = new CodeAttributeDeclaration();
                cad.Name = "DesignerSerializationVisibility";
                cad.Arguments.Add(caa);
                cmp.CustomAttributes.Add(cad);
            }

            cis = new CodeConditionStatement();
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(PrivateNamesUsed["IsEmbedded"].ToString());
            cboe.Right = new CodePrimitiveExpression(false);
            cboe.Operator = CodeBinaryOperatorType.ValueEquality;
            cis.Condition = cboe;

            CodeExpression Value = new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(PrivateNamesUsed["LateBoundObject"].ToString()), "Scope");
            cis.TrueStatements.Add(new CodeMethodReturnStatement(Value));
            cis.FalseStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));

            cmp.GetStatements.Add(cis);

            cis = new CodeConditionStatement();
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(PrivateNamesUsed["IsEmbedded"].ToString());
            cboe.Right = new CodePrimitiveExpression(false);
            cboe.Operator = CodeBinaryOperatorType.ValueEquality;
            cis.Condition = cboe;

            cis.TrueStatements.Add(new CodeAssignStatement(Value,
                new CodeSnippetExpression("value")));

            cmp.SetStatements.Add(cis);
            cc.Members.Add(cmp);

            cmp.Comments.Add(new CodeCommentStatement(SR.CommentManagementScope));
        }

        private void AddGetStatementsForEnumArray(CodeIndexerExpression ciProp, CodeMemberProperty cmProp)
        {
            string strArray = "arrEnumVals";
            string ArrToRet = "enumToRet";
            string strnCtr = "counter";
            string strEnumName = cmProp.Type.BaseType;


            cmProp.GetStatements.Add(new CodeVariableDeclarationStatement("System.Array", strArray,
                new CodeCastExpression(new CodeTypeReference("System.Array"), ciProp)));

            cmProp.GetStatements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(strEnumName, 1), ArrToRet,
                new CodeArrayCreateExpression(new CodeTypeReference(strEnumName),
                new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(strArray), "Length"))));

            cfls = new CodeIterationStatement();

            cmProp.GetStatements.Add(new CodeVariableDeclarationStatement("System.Int32", strnCtr, new CodePrimitiveExpression(0)));
            cfls.InitStatement = new CodeAssignStatement(new CodeVariableReferenceExpression(strnCtr), new CodePrimitiveExpression(0));
            CodeBinaryOperatorExpression cboe1 = new CodeBinaryOperatorExpression();
            cboe1.Left = new CodeVariableReferenceExpression(strnCtr);
            cboe1.Operator = CodeBinaryOperatorType.LessThan;
            cboe1.Right = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(strArray), "Length");
            cfls.TestExpression = cboe1;
            cfls.IncrementStatement = new CodeAssignStatement(new CodeVariableReferenceExpression(strnCtr),
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression(strnCtr),
                CodeBinaryOperatorType.Add,
                new CodePrimitiveExpression(1)));


            CodeMethodInvokeExpression cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method.MethodName = "GetValue";
            cmie1.Method.TargetObject = new CodeVariableReferenceExpression(strArray);
            cmie1.Parameters.Add(new CodeVariableReferenceExpression(strnCtr));

            CodeMethodInvokeExpression cmie2 = new CodeMethodInvokeExpression();
            cmie2.Method.TargetObject = new CodeTypeReferenceExpression("System.Convert");
            cmie2.Parameters.Add(cmie1);
            cmie2.Method.MethodName = arrConvFuncName;
            cfls.Statements.Add(new CodeAssignStatement(new CodeIndexerExpression(new CodeVariableReferenceExpression(ArrToRet),
                new CodeVariableReferenceExpression(strnCtr)),
                new CodeCastExpression(new CodeTypeReference(strEnumName), cmie2)));


            cmProp.GetStatements.Add(cfls);

            cmProp.GetStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(ArrToRet)));

        }

        private void AddCommentsForEmbeddedProperties()
        {
            cc.Comments.Add(new CodeCommentStatement(string.Empty));
            cc.Comments.Add(new CodeCommentStatement(string.Empty));
            cc.Comments.Add(new CodeCommentStatement(string.Empty));
            cc.Comments.Add(new CodeCommentStatement(string.Empty));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedComment));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedComment2));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedComment3));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedComment4));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedComment5));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedComment6));
            cc.Comments.Add(new CodeCommentStatement(string.Empty));

            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedComment7));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedVisualBasicComment1));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedVisualBasicComment2));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedVisualBasicComment3));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedVisualBasicComment4));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedVisualBasicComment5));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedVisualBasicComment6));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedVisualBasicComment7));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedVisualBasicComment8));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedVisualBasicComment9));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedVisualBasicComment10));
            cc.Comments.Add(new CodeCommentStatement(string.Empty));

            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedComment8));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedCSharpComment1));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedCSharpComment2));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedCSharpComment3));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedCSharpComment4));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedCSharpComment5));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedCSharpComment6));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedCSharpComment7));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedCSharpComment8));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedCSharpComment9));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedCSharpComment10));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedCSharpComment11));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedCSharpComment12));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedCSharpComment13));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedCSharpComment14));
            cc.Comments.Add(new CodeCommentStatement(SR.EmbeddedCSharpComment15));
        }

        // This function checks the "SubType" Qualifier and if the value of this qualifies
        // is "interval" then the returned CodeTypeReference is of type System.TimeSpan
        // otherwise the returned type will be System.DateTime.
        // This functions is called only for cimtype.DateTime type properties
        private bool GetDateTimeType(PropertyData prop, ref CodeTypeReference codeType)
        {
            bool isTimeInterval = false;
            if (prop.IsArray)
            {
                codeType = new CodeTypeReference("System.DateTime", 1);
            }
            else
            {
                codeType = new CodeTypeReference("System.DateTime");
            }

            try
            {
                if (string.Equals(prop.Qualifiers["SubType"].Value.ToString(), "interval", StringComparison.OrdinalIgnoreCase))
                {
                    isTimeInterval = true;
                    if (prop.IsArray)
                    {
                        codeType = new CodeTypeReference("System.TimeSpan", 1);
                    }
                    else
                    {
                        codeType = new CodeTypeReference("System.TimeSpan");
                    }
                }

            }
            catch (ManagementException)
            {
                // Qualifier may not be present then ignore it
            }

            if (isTimeInterval)
            {
                if (bTimeSpanConversionFunctionsAdded == false)
                {
                    cc.Comments.Add(new CodeCommentStatement(SR.CommentTimeSpanConversionFunction));
                    bTimeSpanConversionFunctionsAdded = true;
                    // Call this function to generate conversion function
                    GenerateTimeSpanConversionFunction();
                }
            }
            else
            {
                if (bDateConversionFunctionsAdded == false)
                {
                    cc.Comments.Add(new CodeCommentStatement(SR.CommentDateConversionFunction));
                    bDateConversionFunctionsAdded = true;
                    // Call this function to generate conversion function
                    GenerateDateTimeConversionFunction();
                }
            }

            return isTimeInterval;
        }

        /// <summary>
        /// This function generates static CreateInstance to create an WMI instance.
        /// public static GenClass CreateInstance() {
        ///        return new GenClass(new ManagementClass(new System.Management.ManagementClass(CreatedWmiNamespace, CreatedClassName, null).CreateInstance()));
        /// }
        /// </summary>
        private void GenerateCreateInstance()
        {
            string strTemp = "tmpMgmtClass";
            cmm = new CodeMemberMethod();
            string strScope = "mgmtScope";
            string strPath = "mgmtPath";

            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
            cmm.Name = PublicNamesUsed["CreateInst"].ToString();
            cmm.ReturnType = new CodeTypeReference(PrivateNamesUsed["GeneratedClassName"].ToString());

            caa = new CodeAttributeArgument();
            caa.Value = new CodePrimitiveExpression(true);
            cad = new CodeAttributeDeclaration();
            cad.Name = "Browsable";
            cad.Arguments.Add(caa);
            cmm.CustomAttributes = new CodeAttributeDeclarationCollection();
            cmm.CustomAttributes.Add(cad);




            cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(PublicNamesUsed["ScopeClass"].ToString()),
                strScope,
                new CodePrimitiveExpression(null)));

            CodeConditionStatement cis1 = new CodeConditionStatement();
            CodeBinaryOperatorExpression cboe1 = new CodeBinaryOperatorExpression();
            cboe1.Left = new CodeVariableReferenceExpression(PrivateNamesUsed["statMgmtScope"].ToString());
            cboe1.Right = new CodePrimitiveExpression(null);
            cboe1.Operator = CodeBinaryOperatorType.IdentityEquality;
            cis1.Condition = cboe1;

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PublicNamesUsed["ScopeClass"].ToString());
            cis1.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(strScope), coce));

            cis1.TrueStatements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(
                new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(strScope),
                "Path"), "NamespacePath"),
                new CodeVariableReferenceExpression(PrivateNamesUsed["CreationWmiNamespace"].ToString())));


            cis1.FalseStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(strScope),
                new CodeVariableReferenceExpression(PrivateNamesUsed["statMgmtScope"].ToString())));


            cmm.Statements.Add(cis1);

            CodeObjectCreateExpression cocePath = new CodeObjectCreateExpression();
            cocePath.CreateType = new CodeTypeReference(PublicNamesUsed["PathClass"].ToString());
            cocePath.Parameters.Add(new CodeVariableReferenceExpression(PrivateNamesUsed["CreationClassName"].ToString()));
            cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(PublicNamesUsed["PathClass"].ToString()), strPath, cocePath));

            CodeObjectCreateExpression coce1 = new CodeObjectCreateExpression();
            coce1.CreateType = new CodeTypeReference(PublicNamesUsed["ManagementClass"].ToString());
            coce1.Parameters.Add(new CodeVariableReferenceExpression(strScope));
            coce1.Parameters.Add(new CodeVariableReferenceExpression(strPath));
            coce1.Parameters.Add(new CodePrimitiveExpression(null));

            cmm.Statements.Add(new CodeVariableDeclarationStatement(PublicNamesUsed["ManagementClass"].ToString(), strTemp, coce1));


            CodeMethodInvokeExpression cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method.MethodName = "CreateInstance";
            cmie1.Method.TargetObject = new CodeVariableReferenceExpression(strTemp);

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference(PrivateNamesUsed["GeneratedClassName"].ToString());
            coce.Parameters.Add(cmie1);

            cmm.Statements.Add(new CodeMethodReturnStatement(coce));

            cc.Members.Add(cmm);

        }

        /// <summary>
        /// This function generates static CreateInstance to create an WMI instance.
        /// public static GenClass CreateInstance() {
        ///        PrivateLateBoundObject.Delete();
        /// }
        /// </summary>
        private void GenerateDeleteInstance()
        {
            cmm = new CodeMemberMethod();

            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            cmm.Name = PublicNamesUsed["DeleteInst"].ToString();

            caa = new CodeAttributeArgument();
            caa.Value = new CodePrimitiveExpression(true);
            cad = new CodeAttributeDeclaration();
            cad.Name = "Browsable";
            cad.Arguments.Add(caa);
            cmm.CustomAttributes = new CodeAttributeDeclarationCollection();
            cmm.CustomAttributes.Add(cad);


            CodeMethodInvokeExpression cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method.MethodName = "Delete";
            cmie1.Method.TargetObject = new CodeVariableReferenceExpression(PrivateNamesUsed["LateBoundObject"].ToString());

            cmm.Statements.Add(cmie1);

            cc.Members.Add(cmm);

        }

        /// <summary>
        /// Function to genreate helper function for DMTF to DateTime and DateTime to DMTF
        /// </summary>
        private void GenerateDateTimeConversionFunction()
        {
            AddToDateTimeFunction();
            AddToDMTFDateTimeFunction();
        }

        /// <summary>
        /// Function to genreate helper function for DMTF Time interval to TimeSpan and vice versa
        /// </summary>
        private void GenerateTimeSpanConversionFunction()
        {
            AddToTimeSpanFunction();
            AddToDMTFTimeIntervalFunction();

        }


        /// <summary>
        /// Generated code for function to do conversion of date from DMTF format to DateTime format
        /// </summary>
        private void AddToDateTimeFunction()
        {
            string dmtfParam = "dmtfDate";
            string year = "year";
            string month = "month";
            string day = "day";
            string hour = "hour";
            string minute = "minute";
            string second = "second";
            string ticks = "ticks";
            string dmtf = "dmtf";
            string tempStr = "tempString";
            string datetimeVariable = "datetime";

            CodeCastExpression cast = null;

            CodeMemberMethod cmmdt = new CodeMemberMethod();
            cmmdt.Name = PrivateNamesUsed["ToDateTimeMethod"].ToString();
            cmmdt.Attributes = MemberAttributes.Final | MemberAttributes.Static;
            cmmdt.ReturnType = new CodeTypeReference("System.DateTime");
            cmmdt.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("System.String"), dmtfParam));
            cmmdt.Comments.Add(new CodeCommentStatement(SR.CommentToDateTime));

            // create a local variable to initialize from - fixed warnings in MCPP which doesn't
            // like you copying sub items (like year) out of MinValue
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.DateTime"), "initializer", new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.DateTime"), "MinValue")));
            CodeVariableReferenceExpression cvreInitializer = new CodeVariableReferenceExpression("initializer");
            //Int32 year = initializer.Year;
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Int32"), year, new CodePropertyReferenceExpression(cvreInitializer, "Year")));

            //Int32 month = initializer.Month;
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Int32"), month, new CodePropertyReferenceExpression(cvreInitializer, "Month")));

            //Int32 day = initializer.Day;
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Int32"), day, new CodePropertyReferenceExpression(cvreInitializer, "Day")));

            //Int32 hour = initializer.Hour;
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Int32"), hour, new CodePropertyReferenceExpression(cvreInitializer, "Hour")));

            //Int32 minute = Sinitializer.Minute;
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Int32"), minute, new CodePropertyReferenceExpression(cvreInitializer, "Minute")));

            //Int32 second = initializer.Second;
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Int32"), second, new CodePropertyReferenceExpression(cvreInitializer, "Second")));

            //Int32 millisec = 0;
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Int64"), ticks, new CodePrimitiveExpression(0)));

            //String dmtf = dmtfDate ;
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.String"), dmtf, new CodeVariableReferenceExpression(dmtfParam)));

            //System.DateTime datetime = System.DateTime.MinValue ;
            CodeFieldReferenceExpression cpreMinVal = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.DateTime"), "MinValue");
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.DateTime"), datetimeVariable, cpreMinVal));

            //String tempString = String.Empty ;
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.String"), tempStr, new CodeFieldReferenceExpression(
                new CodeTypeReferenceExpression("System.String"), "Empty")));

            CodeBinaryOperatorExpression cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(dmtf);
            cboe.Right = new CodePrimitiveExpression(null);
            cboe.Operator = CodeBinaryOperatorType.IdentityEquality;

            CodeConditionStatement cis = new CodeConditionStatement();
            cis.Condition = cboe;

            CodeObjectCreateExpression codeThrowException = new CodeObjectCreateExpression();
            codeThrowException.CreateType = new CodeTypeReference(PublicNamesUsed["ArgumentOutOfRangeException"].ToString());
            cis.TrueStatements.Add(new CodeThrowExceptionStatement(codeThrowException));

            cmmdt.Statements.Add(cis);

            /*
                if (dmtf.Length == 0)
                {
                    throw new System.ArgumentOutOfRangeException();
                }
            */

            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(dmtf), "Length");
            cboe.Right = new CodePrimitiveExpression(0);
            cboe.Operator = CodeBinaryOperatorType.ValueEquality;

            cis = new CodeConditionStatement();
            cis.Condition = cboe;

            cis.TrueStatements.Add(new CodeThrowExceptionStatement(codeThrowException));

            cmmdt.Statements.Add(cis);

            /*
                if (str.Length != DMTF_DATETIME_STR_LENGTH )
                {
                    throw new System.ArgumentOutOfRangeException();
                }
            */
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(dmtf), "Length");
            cboe.Right = new CodePrimitiveExpression(DMTF_DATETIME_STR_LENGTH);
            cboe.Operator = CodeBinaryOperatorType.IdentityInequality;

            cis = new CodeConditionStatement();
            cis.Condition = cboe;
            cis.TrueStatements.Add(new CodeThrowExceptionStatement(codeThrowException));

            cmmdt.Statements.Add(cis);

            CodeTryCatchFinallyStatement tryblock = new CodeTryCatchFinallyStatement();
            DateTimeConversionFunctionHelper(tryblock.TryStatements, "****", tempStr, dmtf, year, 0, 4);
            DateTimeConversionFunctionHelper(tryblock.TryStatements, "**", tempStr, dmtf, month, 4, 2);
            DateTimeConversionFunctionHelper(tryblock.TryStatements, "**", tempStr, dmtf, day, 6, 2);
            DateTimeConversionFunctionHelper(tryblock.TryStatements, "**", tempStr, dmtf, hour, 8, 2);
            DateTimeConversionFunctionHelper(tryblock.TryStatements, "**", tempStr, dmtf, minute, 10, 2);
            DateTimeConversionFunctionHelper(tryblock.TryStatements, "**", tempStr, dmtf, second, 12, 2);

            /*
                tempString = dmtf.Substring(15, 6);
                if (("******" != tempString))
                {
                    ticks = (System.Int64.Parse(tempString)) * (System.TimeSpan.TicksPerMillisecond/1000);
                }
            */

            CodeMethodReferenceExpression cmre = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(dmtf), "Substring");
            CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
            cmie.Method = cmre;
            cmie.Parameters.Add(new CodePrimitiveExpression(15));
            cmie.Parameters.Add(new CodePrimitiveExpression(6));
            tryblock.TryStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(tempStr), cmie));


            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodePrimitiveExpression("******");
            cboe.Right = new CodeVariableReferenceExpression(tempStr);
            cboe.Operator = CodeBinaryOperatorType.IdentityInequality;
            cis = new CodeConditionStatement();
            cis.Condition = cboe;

            CodeMethodReferenceExpression cmre1 = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression("System.Int64"), "Parse");
            CodeMethodInvokeExpression cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method = cmre1;
            cmie1.Parameters.Add(new CodeVariableReferenceExpression(tempStr));

            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.TimeSpan"), "TicksPerMillisecond");
            cboe.Right = new CodePrimitiveExpression(1000);
            cboe.Operator = CodeBinaryOperatorType.Divide;
            cast = new CodeCastExpression("System.Int64", cboe);

            CodeBinaryOperatorExpression cboe2 = new CodeBinaryOperatorExpression();
            cboe2.Left = cmie1;
            cboe2.Right = cast;
            cboe2.Operator = CodeBinaryOperatorType.Multiply;

            cis.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(ticks), cboe2));

            tryblock.TryStatements.Add(cis);

            /*
                if ( year < 0 || month < 0 || day < 0 || hour < 0 || minute < 0 || second < 0 || ticks < 0)
                {
                    throw new System.ArgumentOutOfRangeException();
                }
            */

            CodeBinaryOperatorExpression cboeYear = new CodeBinaryOperatorExpression();
            cboeYear.Left = new CodeVariableReferenceExpression(year);
            cboeYear.Right = new CodePrimitiveExpression(0);
            cboeYear.Operator = CodeBinaryOperatorType.LessThan;


            CodeBinaryOperatorExpression cboeMonth = new CodeBinaryOperatorExpression();
            cboeMonth.Left = new CodeVariableReferenceExpression(month);
            cboeMonth.Right = new CodePrimitiveExpression(0);
            cboeMonth.Operator = CodeBinaryOperatorType.LessThan;

            CodeBinaryOperatorExpression cboeDay = new CodeBinaryOperatorExpression();
            cboeDay.Left = new CodeVariableReferenceExpression(day);
            cboeDay.Right = new CodePrimitiveExpression(0);
            cboeDay.Operator = CodeBinaryOperatorType.LessThan;

            CodeBinaryOperatorExpression cboeHour = new CodeBinaryOperatorExpression();
            cboeHour.Left = new CodeVariableReferenceExpression(hour);
            cboeHour.Right = new CodePrimitiveExpression(0);
            cboeHour.Operator = CodeBinaryOperatorType.LessThan;

            CodeBinaryOperatorExpression cboeMinute = new CodeBinaryOperatorExpression();
            cboeMinute.Left = new CodeVariableReferenceExpression(minute);
            cboeMinute.Right = new CodePrimitiveExpression(0);
            cboeMinute.Operator = CodeBinaryOperatorType.LessThan;

            CodeBinaryOperatorExpression cboeSecond = new CodeBinaryOperatorExpression();
            cboeSecond.Left = new CodeVariableReferenceExpression(second);
            cboeSecond.Right = new CodePrimitiveExpression(0);
            cboeSecond.Operator = CodeBinaryOperatorType.LessThan;

            CodeBinaryOperatorExpression cboeTicks = new CodeBinaryOperatorExpression();
            cboeTicks.Left = new CodeVariableReferenceExpression(ticks);
            cboeTicks.Right = new CodePrimitiveExpression(0);
            cboeTicks.Operator = CodeBinaryOperatorType.LessThan;

            CodeBinaryOperatorExpression cboetemp1 = new CodeBinaryOperatorExpression();
            cboetemp1.Left = cboeYear;
            cboetemp1.Right = cboeMonth;
            cboetemp1.Operator = CodeBinaryOperatorType.BooleanOr;

            CodeBinaryOperatorExpression cboetemp2 = new CodeBinaryOperatorExpression();
            cboetemp2.Left = cboetemp1;
            cboetemp2.Right = cboeDay;
            cboetemp2.Operator = CodeBinaryOperatorType.BooleanOr;

            CodeBinaryOperatorExpression cboetemp3 = new CodeBinaryOperatorExpression();
            cboetemp3.Left = cboetemp2;
            cboetemp3.Right = cboeHour;
            cboetemp3.Operator = CodeBinaryOperatorType.BooleanOr;

            CodeBinaryOperatorExpression cboetemp4 = new CodeBinaryOperatorExpression();
            cboetemp4.Left = cboetemp3;
            cboetemp4.Right = cboeMinute;
            cboetemp4.Operator = CodeBinaryOperatorType.BooleanOr;

            CodeBinaryOperatorExpression cboetemp5 = new CodeBinaryOperatorExpression();
            cboetemp5.Left = cboetemp4;
            cboetemp5.Right = cboeMinute;
            cboetemp5.Operator = CodeBinaryOperatorType.BooleanOr;

            CodeBinaryOperatorExpression cboetemp6 = new CodeBinaryOperatorExpression();
            cboetemp6.Left = cboetemp5;
            cboetemp6.Right = cboeSecond;
            cboetemp6.Operator = CodeBinaryOperatorType.BooleanOr;

            CodeBinaryOperatorExpression cboetemp7 = new CodeBinaryOperatorExpression();
            cboetemp7.Left = cboetemp6;
            cboetemp7.Right = cboeTicks;
            cboetemp7.Operator = CodeBinaryOperatorType.BooleanOr;

            cis = new CodeConditionStatement();
            cis.Condition = cboetemp7;

            cis.TrueStatements.Add(new CodeThrowExceptionStatement(codeThrowException));

            tryblock.TryStatements.Add(cis);
            /*
                catch
                {
                    throw new System.ArgumentOutOfRangeException(null, e.Message);
                }
            */
            string exceptVar = "e";
            CodeCatchClause catchblock = new CodeCatchClause(exceptVar);

            CodeObjectCreateExpression codeThrowExceptionWithArgs = new CodeObjectCreateExpression();
            codeThrowExceptionWithArgs.CreateType = new CodeTypeReference
                (PublicNamesUsed["ArgumentOutOfRangeException"].ToString());
            codeThrowExceptionWithArgs.Parameters.Add(new CodePrimitiveExpression(null));
            codeThrowExceptionWithArgs.Parameters.Add
                (
                new CodePropertyReferenceExpression
                (
                new CodeVariableReferenceExpression(exceptVar),
                "Message"
                )
                );
            catchblock.Statements.Add(new CodeThrowExceptionStatement(codeThrowExceptionWithArgs));
            //
            // add the catch block to the try block
            //
            tryblock.CatchClauses.Add(catchblock);
            //
            // add the try block to cmmdt
            //
            cmmdt.Statements.Add(tryblock);

            /*
                datetime = new System.DateTime(year, month, day, hour, minute, second, millisec);
            */

            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference("System.DateTime");
            coce.Parameters.Add(new CodeVariableReferenceExpression(year));
            coce.Parameters.Add(new CodeVariableReferenceExpression(month));
            coce.Parameters.Add(new CodeVariableReferenceExpression(day));
            coce.Parameters.Add(new CodeVariableReferenceExpression(hour));
            coce.Parameters.Add(new CodeVariableReferenceExpression(minute));
            coce.Parameters.Add(new CodeVariableReferenceExpression(second));
            coce.Parameters.Add(new CodePrimitiveExpression(0));

            cmmdt.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(datetimeVariable), coce));

            /*
                datetime = datetime.AddTicks(ticks);
            */
            CodeMethodReferenceExpression cmre2 = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(datetimeVariable), "AddTicks");
            CodeMethodInvokeExpression cmie2 = new CodeMethodInvokeExpression();
            cmie2.Method = cmre2;
            cmie2.Parameters.Add(new CodeVariableReferenceExpression(ticks));

            cmmdt.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(datetimeVariable), cmie2));

            /*
                System.TimeSpan tickOffset = System.TimeZone.CurrentTimeZone.GetUtcOffset(datetime);
            */
            cmre1 = new CodeMethodReferenceExpression(new CodePropertyReferenceExpression(new CodeTypeReferenceExpression("System.TimeZone"), "CurrentTimeZone"),
                "GetUtcOffset");
            cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method = cmre1;
            cmie1.Parameters.Add(new CodeVariableReferenceExpression(datetimeVariable));

            string tickoffset = "tickOffset";
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.TimeSpan"), tickoffset, cmie1));

            /*
                System.Int32 UTCOffset =  0;
                System.Int32 OffsetToBeAdjusted = 0;
                long OffsetMins = tickOffset.Ticks / System.TimeSpan.TicksPerMinute;
                tempString = dmtf.Substring(22, 3);
            */
            string utcOffset = "UTCOffset";
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Int32"), utcOffset, new CodePrimitiveExpression(0)));
            string offsetAdjust = "OffsetToBeAdjusted";
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Int32"), offsetAdjust, new CodePrimitiveExpression(0)));

            string OffsetMins = "OffsetMins";
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(tickoffset), "Ticks");
            cboe.Right = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.TimeSpan"), "TicksPerMinute");
            cboe.Operator = CodeBinaryOperatorType.Divide;
            cast = new CodeCastExpression("System.Int64", cboe);
            cmmdt.Statements.Add(
                new CodeVariableDeclarationStatement(
                new CodeTypeReference("System.Int64"),
                OffsetMins,
                cast
                )
                );

            cmre = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(dmtf), "Substring");
            cmie = new CodeMethodInvokeExpression();
            cmie.Method = cmre;
            cmie.Parameters.Add(new CodePrimitiveExpression(22));
            cmie.Parameters.Add(new CodePrimitiveExpression(3));
            cmmdt.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(tempStr), cmie));

            /*
                if (("***" != tempString1))
                {
                    tempString1 = dmtf.Substring(21, 4);
                    try
                    {
                        UTCOffset = System.Int32.Parse(tempString1);
                    }
                    catch
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }

                    OffsetToBeAdjusted = UTCOffset-OffsetMins;

                    // We have to subtract the minutes from the time
                    datetime = datetime.AddMinutes((System.Double)(OffsetToBeAdjusted));

                }
            */
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(tempStr);
            cboe.Right = new CodePrimitiveExpression("******");
            cboe.Operator = CodeBinaryOperatorType.IdentityInequality;

            cis = new CodeConditionStatement();
            cis.Condition = cboe;

            cmre = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(dmtf), "Substring");
            cmie = new CodeMethodInvokeExpression();
            cmie.Method = cmre;
            cmie.Parameters.Add(new CodePrimitiveExpression(21));
            cmie.Parameters.Add(new CodePrimitiveExpression(4));
            cis.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(tempStr), cmie));

            CodeTryCatchFinallyStatement tryblock2 = new CodeTryCatchFinallyStatement();

            cmre = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression("System.Int32"), "Parse");
            cmie = new CodeMethodInvokeExpression();
            cmie.Method = cmre;
            cmie.Parameters.Add(new CodeVariableReferenceExpression(tempStr));
            tryblock2.TryStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(utcOffset), cmie));
            //
            // add the catch block
            //
            tryblock2.CatchClauses.Add(catchblock);
            //
            // add tryblock2 to cis
            //
            cis.TrueStatements.Add(tryblock2);

            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(OffsetMins);
            cboe.Right = new CodeVariableReferenceExpression(utcOffset);
            cboe.Operator = CodeBinaryOperatorType.Subtract;
            cis.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(offsetAdjust), new CodeCastExpression(new CodeTypeReference("System.Int32"), cboe)));

            cmre = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(datetimeVariable), "AddMinutes");
            cmie = new CodeMethodInvokeExpression();
            cmie.Method = cmre;
            cmie.Parameters.Add(new CodeCastExpression("System.Double", new CodeVariableReferenceExpression(offsetAdjust)));
            cis.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(datetimeVariable), cmie));

            cmmdt.Statements.Add(cis);
            /*
                    return datetime;

            */
            cmmdt.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(datetimeVariable)));
            cc.Members.Add(cmmdt);
        }

        /// <summary>
        /// Generates some common code used in conversion function for DateTime
        /// </summary>
        private static void DateTimeConversionFunctionHelper(CodeStatementCollection cmmdt,
            string toCompare,
            string tempVarName,
            string dmtfVarName,
            string toAssign,
            int SubStringParam1,
            int SubStringParam2)
        {
            CodeMethodReferenceExpression cmre = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(dmtfVarName), "Substring");
            CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
            cmie.Method = cmre;
            cmie.Parameters.Add(new CodePrimitiveExpression(SubStringParam1));
            cmie.Parameters.Add(new CodePrimitiveExpression(SubStringParam2));
            cmmdt.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(tempVarName), cmie));

            CodeBinaryOperatorExpression cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodePrimitiveExpression(toCompare);
            cboe.Right = new CodeVariableReferenceExpression(tempVarName);
            cboe.Operator = CodeBinaryOperatorType.IdentityInequality;

            CodeConditionStatement cis = new CodeConditionStatement();
            cis.Condition = cboe;
            cmre = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression("System.Int32"), "Parse");
            cmie = new CodeMethodInvokeExpression();
            cmie.Method = cmre;
            cmie.Parameters.Add(new CodeVariableReferenceExpression(tempVarName));

            cis.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(toAssign), cmie));

            cmmdt.Add(cis);
        }

        private void AddToDMTFTimeIntervalFunction()
        {
            string dmtfTimeSpan = "dmtftimespan";
            string timespan = "timespan";
            string tsTemp = "tsTemp";
            string microsec = "microsec";
            string strmicrosec = "strMicroSec";

            CodeMemberMethod cmmts = new CodeMemberMethod();
            cmmts.Name = PrivateNamesUsed["ToDMTFTimeIntervalMethod"].ToString();
            cmmts.Attributes = MemberAttributes.Final | MemberAttributes.Static;
            cmmts.ReturnType = new CodeTypeReference("System.String");
            cmmts.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("System.TimeSpan"), timespan));
            cmmts.Comments.Add(new CodeCommentStatement(SR.CommentToDmtfTimeInterval));

            /*
                string dmtftimespan = timespan.Days.ToString().PadLeft(8,'0');
            */

            CodePropertyReferenceExpression cpre1 =
                new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(timespan), "Days"
                );
            cmie = new CodeMethodInvokeExpression();
            cmie.Method =
                new CodeMethodReferenceExpression(
                new CodeCastExpression(
                new CodeTypeReference("System.Int32 "),
                cpre1
                ),
                "ToString"
                );

            CodeMethodInvokeExpression cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method = new CodeMethodReferenceExpression(cmie, "PadLeft");
            cmie1.Parameters.Add(new CodePrimitiveExpression(8));
            cmie1.Parameters.Add(new CodePrimitiveExpression('0'));

            cmmts.Statements.Add(
                new CodeVariableDeclarationStatement(
                new CodeTypeReference("System.String"),
                dmtfTimeSpan,
                cmie1
                )
                );

            CodeObjectCreateExpression codeThrowException = new CodeObjectCreateExpression();
            codeThrowException.CreateType =
                new CodeTypeReference(PublicNamesUsed["ArgumentOutOfRangeException"].ToString());

            /*
                System.Timespan maxTimeSpan = System.TimeSpan.MaxValue ;
                if (timespan.Days > maxTimeSpan.Days)
                {
                    throw new System.ArgumentOutOfRangeException();
                }
            */
            CodeFieldReferenceExpression cpreMaxVal = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.TimeSpan"), "MaxValue");
            cmmts.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.TimeSpan"), "maxTimeSpan", cpreMaxVal));

            CodeBinaryOperatorExpression cboe = new CodeBinaryOperatorExpression();

            cboe.Left = new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(timespan),
                "Days"
                );

            cboe.Operator = CodeBinaryOperatorType.GreaterThan;

            cboe.Right = new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression("maxTimeSpan"),
                "Days"
                );

            CodeConditionStatement cis = new CodeConditionStatement();
            cis.Condition = cboe;
            cis.TrueStatements.Add(new CodeThrowExceptionStatement(codeThrowException));

            cmmts.Statements.Add(cis);

            /*
                System.TimeSpan minTimeSpan = System.TimeSpan.MinValue ;
                if (timespan.Days < minTimeSpan.Days)
                {
                    throw new System.ArgumentOutOfRangeException();
                }
            */
            CodeFieldReferenceExpression cpreMinVal = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.TimeSpan"), "MinValue");
            cmmts.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.TimeSpan"), "minTimeSpan", cpreMinVal));

            CodeBinaryOperatorExpression cboe3 = new CodeBinaryOperatorExpression();

            cboe3.Left = new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(timespan),
                "Days"
                );

            cboe3.Operator = CodeBinaryOperatorType.LessThan;

            cboe3.Right = new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression("minTimeSpan"),
                "Days"
                );

            CodeConditionStatement cis2 = new CodeConditionStatement();
            cis2.Condition = cboe3;
            cis2.TrueStatements.Add(new CodeThrowExceptionStatement(codeThrowException));

            cmmts.Statements.Add(cis2);

            /*
                dmtftimespan = (dmtftimespan + timespan.Hours.ToString().PadLeft(2, '0'));
            */

            cpre1 = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(timespan), "Hours");
            cmie = new CodeMethodInvokeExpression();
            cmie.Method = new CodeMethodReferenceExpression(new CodeCastExpression(new CodeTypeReference("System.Int32 "), cpre1), "ToString");

            cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method = new CodeMethodReferenceExpression(cmie, "PadLeft");
            cmie1.Parameters.Add(new CodePrimitiveExpression(2));
            cmie1.Parameters.Add(new CodePrimitiveExpression('0'));

            CodeMethodInvokeExpression cmie2 = GenerateConcatStrings(new CodeVariableReferenceExpression(dmtfTimeSpan), cmie1);

            cmmts.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(dmtfTimeSpan), cmie2));

            /*
                dmtftimespan = (dmtftimespan + timespan.Minutes.ToString().PadLeft(2, '0'));
            */

            cpre1 = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(timespan), "Minutes");
            cmie = new CodeMethodInvokeExpression();
            cmie.Method = new CodeMethodReferenceExpression(new CodeCastExpression(new CodeTypeReference("System.Int32 "), cpre1), "ToString");

            cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method = new CodeMethodReferenceExpression(cmie, "PadLeft");
            cmie1.Parameters.Add(new CodePrimitiveExpression(2));
            cmie1.Parameters.Add(new CodePrimitiveExpression('0'));

            cmie2 = GenerateConcatStrings(new CodeVariableReferenceExpression(dmtfTimeSpan), cmie1);

            cmmts.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(dmtfTimeSpan), cmie2));

            /*
                dmtftimespan = (dmtftimespan + timespan.Seconds.ToString().PadLeft(2, '0'));
            */

            cpre1 = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(timespan), "Seconds");
            cmie = new CodeMethodInvokeExpression();
            cmie.Method = new CodeMethodReferenceExpression(new CodeCastExpression(new CodeTypeReference("System.Int32 "), cpre1), "ToString");

            cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method = new CodeMethodReferenceExpression(cmie, "PadLeft");
            cmie1.Parameters.Add(new CodePrimitiveExpression(2));
            cmie1.Parameters.Add(new CodePrimitiveExpression('0'));

            cmie2 = GenerateConcatStrings(new CodeVariableReferenceExpression(dmtfTimeSpan), cmie1);

            cmmts.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(dmtfTimeSpan), cmie2));

            /*
                dmtftimespan = dmtftimespan + ".";
            */
            cmie2 = GenerateConcatStrings(new CodeVariableReferenceExpression(dmtfTimeSpan), new CodePrimitiveExpression("."));

            cmmts.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(dmtfTimeSpan), cmie2));

            /*
                TimeSpan tsTemp = new TimeSpan(timespan.Days ,timespan.Hours,timespan.Minutes ,timespan.Seconds ,0);
            */
            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference("System.TimeSpan");
            coce.Parameters.Add(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(timespan), "Days"));
            coce.Parameters.Add(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(timespan), "Hours"));
            coce.Parameters.Add(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(timespan), "Minutes"));
            coce.Parameters.Add(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(timespan), "Seconds"));
            coce.Parameters.Add(new CodePrimitiveExpression(0));
            cmmts.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.TimeSpan"), tsTemp, coce));

            /*
                System.Int64 microsec = ((timespan.Ticks-tsTemp.Ticks) * 1000) / System.TimeSpan.TicksPerMillisecond;
            */

            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(timespan), "Ticks");
            cboe.Right = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(tsTemp), "Ticks");
            cboe.Operator = CodeBinaryOperatorType.Subtract;

            CodeBinaryOperatorExpression cboe1 = new CodeBinaryOperatorExpression();
            cboe1.Left = cboe;
            cboe1.Right = new CodePrimitiveExpression(1000);
            cboe1.Operator = CodeBinaryOperatorType.Multiply;

            CodeBinaryOperatorExpression cboe2 = new CodeBinaryOperatorExpression();
            cboe2.Left = cboe1;
            cboe2.Right = new CodeFieldReferenceExpression(
                new CodeTypeReferenceExpression("System.TimeSpan"),
                "TicksPerMillisecond"
                );
            cboe2.Operator = CodeBinaryOperatorType.Divide;
            cmmts.Statements.Add(
                new CodeVariableDeclarationStatement(
                new CodeTypeReference("System.Int64"),
                microsec,
                new CodeCastExpression("System.Int64", cboe2)
                )
                );

            /*
                System.String strMicrosec = microsec.ToString();
            */
            cmie = new CodeMethodInvokeExpression();
            cmie.Method = new CodeMethodReferenceExpression(new CodeCastExpression(new CodeTypeReference("System.Int64 "), new CodeVariableReferenceExpression(microsec)), "ToString");
            cmmts.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.String"), strmicrosec, cmie));

            /*
                if (strMicrosec.Length > 6)
                {
                    strMicrosec = strMicrosec.Substring(0,6);
                }
            */
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(strmicrosec), "Length");
            cboe.Right = new CodePrimitiveExpression(6);
            cboe.Operator = CodeBinaryOperatorType.GreaterThan;

            cis = new CodeConditionStatement();
            cis.Condition = cboe;


            cmie = new CodeMethodInvokeExpression();
            cmie.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(strmicrosec), "Substring");
            cmie.Parameters.Add(new CodePrimitiveExpression(0));
            cmie.Parameters.Add(new CodePrimitiveExpression(6));

            cis.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(strmicrosec), cmie));
            cmmts.Statements.Add(cis);

            /*
                dmtftimespan = dmtftimespan + strMicrosec.PadLeft(6,'0');
            */

            cmie = new CodeMethodInvokeExpression();
            cmie.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(strmicrosec), "PadLeft");
            cmie.Parameters.Add(new CodePrimitiveExpression(6));
            cmie.Parameters.Add(new CodePrimitiveExpression('0'));


            cmie2 = GenerateConcatStrings(new CodeVariableReferenceExpression(dmtfTimeSpan), cmie);

            cmmts.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(dmtfTimeSpan), cmie2));

            cmie2 = GenerateConcatStrings(new CodeVariableReferenceExpression(dmtfTimeSpan), new CodePrimitiveExpression(":000"));

            cmmts.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(dmtfTimeSpan), cmie2));

            cmmts.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(dmtfTimeSpan)));

            cc.Members.Add(cmmts);

        }

        private void AddToDMTFDateTimeFunction()
        {
            string strUtc = "utcString";
            string dateParam = "date";

            CodeCastExpression cast = null;

            CodeMemberMethod cmmdt = new CodeMemberMethod();
            cmmdt.Name = PrivateNamesUsed["ToDMTFDateTimeMethod"].ToString();
            cmmdt.Attributes = MemberAttributes.Final | MemberAttributes.Static;
            cmmdt.ReturnType = new CodeTypeReference("System.String");
            cmmdt.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("System.DateTime"), dateParam));
            cmmdt.Comments.Add(new CodeCommentStatement(SR.CommentToDmtfDateTime));

            /*
                 string UtcString = String.Empty;
            */
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.String"), strUtc,
                new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.String"), "Empty")));
            /*
                System.TimeSpan tickOffset = System.TimeZone.CurrentTimeZone.GetUtcOffset(date);
                long OffsetMins = tickOffset.Ticks / System.TimeSpan.TicksPerMinute;
            */

            CodeMethodReferenceExpression cmre =
                new CodeMethodReferenceExpression(
                new CodePropertyReferenceExpression(
                new CodeTypeReferenceExpression("System.TimeZone"),
                "CurrentTimeZone"
                ),
                "GetUtcOffset"
                );
            CodeMethodInvokeExpression cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method = cmre;
            cmie1.Parameters.Add(new CodeVariableReferenceExpression(dateParam));

            string tickoffset = "tickOffset";
            cmmdt.Statements.Add(
                new CodeVariableDeclarationStatement(
                new CodeTypeReference("System.TimeSpan"),
                tickoffset,
                cmie1
                )
                );

            string OffsetMins = "OffsetMins";
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(tickoffset), "Ticks");
            cboe.Right = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.TimeSpan"), "TicksPerMinute");
            cboe.Operator = CodeBinaryOperatorType.Divide;
            cast = new CodeCastExpression("System.Int64", cboe);
            cmmdt.Statements.Add(
                new CodeVariableDeclarationStatement(
                new CodeTypeReference("System.Int64"),
                OffsetMins,
                cast
                )
                );
            /*
                if (Math.Abs(OffsetMins) > MAXSIZE_UTC_DMTF)
                {
                    date = date.ToUniversalTime();
                    UtcString = "+000";
                }
            */

            cmre = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression("System.Math"), "Abs");
            cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method = cmre;
            cmie1.Parameters.Add(new CodeVariableReferenceExpression(OffsetMins));

            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = cmie1;
            cboe.Right = new CodePrimitiveExpression(999);
            cboe.Operator = CodeBinaryOperatorType.GreaterThan;

            CodeConditionStatement cis1 = new CodeConditionStatement();
            cis1.Condition = cboe;

            cmre = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(dateParam), "ToUniversalTime");
            cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method = cmre;

            cis1.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(dateParam), cmie1));
            cis1.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(strUtc), new CodePrimitiveExpression("+000")));
            /*
                else
                if ((tickOffset.Ticks >= 0))
                {
                    UtcString = "+" + ((tickOffset.Ticks / System.TimeSpan.TicksPerMinute)).ToString().PadLeft(3,'0');
                }
            */
            CodeBinaryOperatorExpression cboe1 = new CodeBinaryOperatorExpression();
            cboe1.Left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(tickoffset), "Ticks");
            cboe1.Right = new CodePrimitiveExpression(0);
            cboe1.Operator = CodeBinaryOperatorType.GreaterThanOrEqual;

            CodeConditionStatement cis2 = new CodeConditionStatement();
            cis2.Condition = cboe1;

            CodeBinaryOperatorExpression cboe2 = new CodeBinaryOperatorExpression();
            cboe2.Left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(tickoffset), "Ticks");
            cboe2.Right = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.TimeSpan"), "TicksPerMinute");
            cboe2.Operator = CodeBinaryOperatorType.Divide;

            cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method = new CodeMethodReferenceExpression(new CodeCastExpression(new CodeTypeReference("System.Int64 "), cboe2), "ToString");

            CodeMethodInvokeExpression cmie2 = new CodeMethodInvokeExpression();
            cmie2.Method = new CodeMethodReferenceExpression(cmie1, "PadLeft");
            cmie2.Parameters.Add(new CodePrimitiveExpression(3));
            cmie2.Parameters.Add(new CodePrimitiveExpression('0'));

            cis2.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(strUtc),
                GenerateConcatStrings(new CodePrimitiveExpression("+"), cmie2)));
            /*
                else
                {
                    string strTemp = OffsetMins.ToString();
                    UtcString = "-" + strTemp.Substring(1).PadLeft(3,'0');
                }
            */

            cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method = new CodeMethodReferenceExpression(new CodeCastExpression(new CodeTypeReference("System.Int64 "), new CodeVariableReferenceExpression(OffsetMins)), "ToString");
            cis2.FalseStatements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.String"), "strTemp", cmie1));

            cmie2 = new CodeMethodInvokeExpression();
            cmie2.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("strTemp"), "Substring");
            cmie2.Parameters.Add(new CodePrimitiveExpression(1));
            cmie2.Parameters.Add(new CodeBinaryOperatorExpression(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("strTemp"), "Length"),
                CodeBinaryOperatorType.Subtract,
                new CodePrimitiveExpression(1)));


            CodeMethodInvokeExpression cmie3 = new CodeMethodInvokeExpression();
            cmie3.Method = new CodeMethodReferenceExpression(cmie2, "PadLeft");
            cmie3.Parameters.Add(new CodePrimitiveExpression(3));
            cmie3.Parameters.Add(new CodePrimitiveExpression('0'));

            cis2.FalseStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(strUtc),
                GenerateConcatStrings(new CodePrimitiveExpression("-"), cmie3)));
            cis1.FalseStatements.Add(cis2);
            cmmdt.Statements.Add(cis1);

            /*
                string dmtfDateTime = date.Year.ToString().PadLeft(4,'0');
            */

            string dmtfDateTime = "dmtfDateTime";
            cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method = new CodeMethodReferenceExpression(new CodeCastExpression(new CodeTypeReference("System.Int32 "),
                new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(dateParam),
                "Year")), "ToString");


            cmie2 = new CodeMethodInvokeExpression();
            cmie2.Method = new CodeMethodReferenceExpression(cmie1, "PadLeft");
            cmie2.Parameters.Add(new CodePrimitiveExpression(4));
            cmie2.Parameters.Add(new CodePrimitiveExpression('0'));

            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.String"), dmtfDateTime, cmie2));

            /*
                dmtfDateTime = (dmtfDateTime + date.Month.ToString().PadLeft(2, '0'));
            */

            ToDMTFDateHelper("Month", cmmdt, "System.Int32 ");

            /*
                dmtfDateTime = (dmtfDateTime + date.Day.ToString().PadLeft(2, '0'));
            */
            ToDMTFDateHelper("Day", cmmdt, "System.Int32 ");

            /*
                dmtfDateTime = (dmtfDateTime + date.Hour.ToString().PadLeft(2, '0'));
            */
            ToDMTFDateHelper("Hour", cmmdt, "System.Int32 ");

            /*
                dmtfDateTime = (dmtfDateTime + date.Minute.ToString().PadLeft(2, '0'));
            */

            ToDMTFDateHelper("Minute", cmmdt, "System.Int32 ");

            /*
                dmtfDateTime = (dmtfDateTime + date.Second.ToString().PadLeft(2, '0'));
            */

            ToDMTFDateHelper("Second", cmmdt, "System.Int32 ");

            /*
                dmtfDateTime = (dmtfDateTime + ".");
            */

            cmmdt.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(dmtfDateTime),
                GenerateConcatStrings(new CodeVariableReferenceExpression(dmtfDateTime), new CodePrimitiveExpression("."))));
            /*
                DateTime dtTemp = new DateTime(date.Year ,date.Month,date.Day ,date.Hour ,date.Minute ,date.Second,0);
            */

            string dtTemp = "dtTemp";
            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference("System.DateTime");
            coce.Parameters.Add(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(dateParam), "Year"));
            coce.Parameters.Add(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(dateParam), "Month"));
            coce.Parameters.Add(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(dateParam), "Day"));
            coce.Parameters.Add(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(dateParam), "Hour"));
            coce.Parameters.Add(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(dateParam), "Minute"));
            coce.Parameters.Add(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(dateParam), "Second"));
            coce.Parameters.Add(new CodePrimitiveExpression(0));
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.DateTime"), dtTemp, coce));

            /*
                System.Int64 microsec = ((date.Ticks-dtTemp.Ticks) * 1000) / System.TimeSpan.TicksPerMillisecond;
            */

            string microsec = "microsec";
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(dateParam), "Ticks");
            cboe.Right = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(dtTemp), "Ticks");
            cboe.Operator = CodeBinaryOperatorType.Subtract;

            cboe1 = new CodeBinaryOperatorExpression();
            cboe1.Left = cboe;
            cboe1.Right = new CodePrimitiveExpression(1000);
            cboe1.Operator = CodeBinaryOperatorType.Multiply;

            cboe2 = new CodeBinaryOperatorExpression();
            cboe2.Left = cboe1;
            cboe2.Right = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.TimeSpan"), "TicksPerMillisecond");
            cboe2.Operator = CodeBinaryOperatorType.Divide;
            cast = new CodeCastExpression("System.Int64", cboe2);
            cmmdt.Statements.Add(
                new CodeVariableDeclarationStatement(
                new CodeTypeReference("System.Int64"),
                microsec,
                cast
                )
                );
            /*
                System.String strMicrosec = microsec.ToString();
            */
            string strmicrosec = "strMicrosec";
            cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method = new CodeMethodReferenceExpression(new CodeCastExpression(new CodeTypeReference("System.Int64 "),
                new CodeVariableReferenceExpression(microsec)), "ToString");
            cmmdt.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.String"), strmicrosec, cmie1));

            /*
                if (strMicrosec.Length > 6)
                {
                    strMicrosec = strMicrosec.Substring(0,6);
                }
            */
            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(strmicrosec), "Length");
            cboe.Right = new CodePrimitiveExpression(6);
            cboe.Operator = CodeBinaryOperatorType.GreaterThan;

            cis1 = new CodeConditionStatement();
            cis1.Condition = cboe;


            cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(strmicrosec), "Substring");
            cmie1.Parameters.Add(new CodePrimitiveExpression(0));
            cmie1.Parameters.Add(new CodePrimitiveExpression(6));

            cis1.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(strmicrosec), cmie1));
            cmmdt.Statements.Add(cis1);

            /*
                dmtfDateTime = dmtfDateTime + strMicrosec.PadLeft(6,'0');
            */

            cmie1 = new CodeMethodInvokeExpression();
            cmie1.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(strmicrosec), "PadLeft");
            cmie1.Parameters.Add(new CodePrimitiveExpression(6));
            cmie1.Parameters.Add(new CodePrimitiveExpression('0'));

            cmie2 = GenerateConcatStrings(new CodeVariableReferenceExpression(dmtfDateTime), cmie1);
            cmmdt.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(dmtfDateTime), cmie2));


            /*
                dmtfDateTime = dmtfDateTime + UtcString;
            */

            cmmdt.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(dmtfDateTime),
                GenerateConcatStrings(new CodeVariableReferenceExpression(dmtfDateTime),
                new CodeVariableReferenceExpression(strUtc))));

            cmmdt.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(dmtfDateTime)));

            cc.Members.Add(cmmdt);

        }

        // Helper function exclusively added to be used from AddToDMTFFunction function
        private void ToDMTFDateHelper(string dateTimeMember, CodeMemberMethod cmmdt, string strType)
        {
            string dmtfDateTime = "dmtfDateTime";
            string dateParam = "date";
            CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
            cmie.Method = new CodeMethodReferenceExpression(new CodeCastExpression(new CodeTypeReference(strType),
                new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(dateParam),
                dateTimeMember)), "ToString");


            CodeMethodInvokeExpression cmie2 = new CodeMethodInvokeExpression();
            cmie2.Method = new CodeMethodReferenceExpression(cmie, "PadLeft");
            cmie2.Parameters.Add(new CodePrimitiveExpression(2));
            cmie2.Parameters.Add(new CodePrimitiveExpression('0'));

            cmmdt.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(dmtfDateTime),
                GenerateConcatStrings(new CodeVariableReferenceExpression(dmtfDateTime),
                cmie2)));
        }

        private void AddToTimeSpanFunction()
        {
            string tsParam = "dmtfTimespan";
            string days = "days";
            string hours = "hours";
            string minutes = "minutes";
            string seconds = "seconds";
            string ticks = "ticks";

            CodeMemberMethod cmmts = new CodeMemberMethod();
            cmmts.Name = PrivateNamesUsed["ToTimeSpanMethod"].ToString();
            cmmts.Attributes = MemberAttributes.Final | MemberAttributes.Static;
            cmmts.ReturnType = new CodeTypeReference("System.TimeSpan");
            cmmts.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("System.String"), tsParam));
            cmmts.Comments.Add(new CodeCommentStatement(SR.CommentToTimeSpan));


            //Int32 days = 0;
            cmmts.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Int32"), days, new CodePrimitiveExpression(0)));
            //Int32 hours = 0;
            cmmts.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Int32"), hours, new CodePrimitiveExpression(0)));
            //Int32 minutes = 0;
            cmmts.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Int32"), minutes, new CodePrimitiveExpression(0)));
            //Int32 seconds = 0;
            cmmts.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Int32"), seconds, new CodePrimitiveExpression(0)));
            //Int32 ticks = 0;
            cmmts.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Int64"), ticks, new CodePrimitiveExpression(0)));

            /*
                if (dmtfTimespan == null)
                {
                    throw new System.ArgumentOutOfRangeException();
                }
            */

            CodeBinaryOperatorExpression cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodeVariableReferenceExpression(tsParam);
            cboe.Right = new CodePrimitiveExpression(null);
            cboe.Operator = CodeBinaryOperatorType.IdentityEquality;

            CodeConditionStatement cis = new CodeConditionStatement();
            cis.Condition = cboe;

            CodeObjectCreateExpression codeThrowException = new CodeObjectCreateExpression();
            codeThrowException.CreateType = new CodeTypeReference(PublicNamesUsed["ArgumentOutOfRangeException"].ToString());
            cis.TrueStatements.Add(new CodeThrowExceptionStatement(codeThrowException));

            cmmts.Statements.Add(cis);

            /*
                if (dmtfTimespan.Length == 0)
                {
                    throw new System.ArgumentOutOfRangeException();
                }
            */

            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(tsParam), "Length");
            cboe.Right = new CodePrimitiveExpression(0);
            cboe.Operator = CodeBinaryOperatorType.ValueEquality;

            cis = new CodeConditionStatement();
            cis.Condition = cboe;

            cis.TrueStatements.Add(new CodeThrowExceptionStatement(codeThrowException));

            cmmts.Statements.Add(cis);

            /*
                if (dmtfTimespan.Length != DMTF_DATETIME_STR_LENGTH )
                {
                    throw new System.ArgumentOutOfRangeException();
                }
            */

            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(tsParam), "Length");
            cboe.Right = new CodePrimitiveExpression(DMTF_DATETIME_STR_LENGTH);
            cboe.Operator = CodeBinaryOperatorType.IdentityInequality;

            cis = new CodeConditionStatement();
            cis.Condition = cboe;
            cis.TrueStatements.Add(new CodeThrowExceptionStatement(codeThrowException));

            cmmts.Statements.Add(cis);

            /*
                if (dmtfTimespan.Substring(21,4) != ":000")
                {
                    throw new System.ArgumentOutOfRangeException();
                }
            */

            CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
            cmie.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(tsParam), "Substring");
            cmie.Parameters.Add(new CodePrimitiveExpression(21));
            cmie.Parameters.Add(new CodePrimitiveExpression(4));

            cboe = new CodeBinaryOperatorExpression();
            cboe.Left = cmie;
            cboe.Right = new CodePrimitiveExpression(":000");
            cboe.Operator = CodeBinaryOperatorType.IdentityInequality;

            cis = new CodeConditionStatement();
            cis.Condition = cboe;
            cis.TrueStatements.Add(new CodeThrowExceptionStatement(codeThrowException));

            cmmts.Statements.Add(cis);

            CodeTryCatchFinallyStatement tryblock = new CodeTryCatchFinallyStatement();

            /*
                string tempString = System.String.Empty;
            */

            string strTemp = "tempString";
            tryblock.TryStatements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.String"), strTemp,
                new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.String"), "Empty")));
            /*
                tempString = dmtfTimespan.Substring(0, 8);
                days = System.Int32.Parse(tempString);

                tempString = dmtfTimespan.Substring(8, 2);
                hours = System.Int32.Parse(tempString);

                tempString = dmtfTimespan.Substring(10, 2);
                minutes = System.Int32.Parse(tempString);

                tempString = dmtfTimespan.Substring(12, 2);
                seconds = System.Int32.Parse(tempString);
            */

            ToTimeSpanHelper(0, 8, days, tryblock.TryStatements);
            ToTimeSpanHelper(8, 2, hours, tryblock.TryStatements);
            ToTimeSpanHelper(10, 2, minutes, tryblock.TryStatements);
            ToTimeSpanHelper(12, 2, seconds, tryblock.TryStatements);

            /*
                tempString = dmtfTimespan.Substring(15, 6);
            */

            cmie = new CodeMethodInvokeExpression();
            cmie.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(tsParam), "Substring");
            cmie.Parameters.Add(new CodePrimitiveExpression(15));
            cmie.Parameters.Add(new CodePrimitiveExpression(6));

            tryblock.TryStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(strTemp), cmie));

            cmie = new CodeMethodInvokeExpression();
            cmie.Method = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression("System.Int64"), "Parse");
            cmie.Parameters.Add(new CodeVariableReferenceExpression(strTemp));

            /*
                ticks = (System.Int64.Parse(tempString)) * (System.TimeSpan.TicksPerMillisecond/1000);
            */
            tryblock.TryStatements.Add
                (
                new CodeAssignStatement(
                new CodeVariableReferenceExpression(ticks),
                new CodeBinaryOperatorExpression(
                cmie,
                CodeBinaryOperatorType.Multiply,
                new CodeCastExpression(
                "System.Int64",
                new CodeBinaryOperatorExpression(
                new CodeFieldReferenceExpression(
                new CodeTypeReferenceExpression("System.TimeSpan"),
                "TicksPerMillisecond"
                ),
                CodeBinaryOperatorType.Divide,
                new CodePrimitiveExpression(1000)
                )
                )
                )
                )
                );

            /*
                if ( days < 0 || hours < 0 || minutes < 0 || seconds < 0 || ticks < 0)
                {
                    throw new System.ArgumentOutOfRangeException();
                }
            */

            CodeBinaryOperatorExpression cboeDays = new CodeBinaryOperatorExpression();
            cboeDays.Left = new CodeVariableReferenceExpression(days);
            cboeDays.Right = new CodePrimitiveExpression(0);
            cboeDays.Operator = CodeBinaryOperatorType.LessThan;


            CodeBinaryOperatorExpression cboeHours = new CodeBinaryOperatorExpression();
            cboeHours.Left = new CodeVariableReferenceExpression(hours);
            cboeHours.Right = new CodePrimitiveExpression(0);
            cboeHours.Operator = CodeBinaryOperatorType.LessThan;

            CodeBinaryOperatorExpression cboeMinutes = new CodeBinaryOperatorExpression();
            cboeMinutes.Left = new CodeVariableReferenceExpression(minutes);
            cboeMinutes.Right = new CodePrimitiveExpression(0);
            cboeMinutes.Operator = CodeBinaryOperatorType.LessThan;

            CodeBinaryOperatorExpression cboeSeconds = new CodeBinaryOperatorExpression();
            cboeSeconds.Left = new CodeVariableReferenceExpression(seconds);
            cboeSeconds.Right = new CodePrimitiveExpression(0);
            cboeSeconds.Operator = CodeBinaryOperatorType.LessThan;

            CodeBinaryOperatorExpression cboeTicks = new CodeBinaryOperatorExpression();
            cboeTicks.Left = new CodeVariableReferenceExpression(ticks);
            cboeTicks.Right = new CodePrimitiveExpression(0);
            cboeTicks.Operator = CodeBinaryOperatorType.LessThan;

            CodeBinaryOperatorExpression cboetemp1 = new CodeBinaryOperatorExpression();
            cboetemp1.Left = cboeDays;
            cboetemp1.Right = cboeHours;
            cboetemp1.Operator = CodeBinaryOperatorType.BooleanOr;

            CodeBinaryOperatorExpression cboetemp2 = new CodeBinaryOperatorExpression();
            cboetemp2.Left = cboetemp1;
            cboetemp2.Right = cboeMinutes;
            cboetemp2.Operator = CodeBinaryOperatorType.BooleanOr;

            CodeBinaryOperatorExpression cboetemp3 = new CodeBinaryOperatorExpression();
            cboetemp3.Left = cboetemp2;
            cboetemp3.Right = cboeSeconds;
            cboetemp3.Operator = CodeBinaryOperatorType.BooleanOr;

            CodeBinaryOperatorExpression cboetemp4 = new CodeBinaryOperatorExpression();
            cboetemp4.Left = cboetemp3;
            cboetemp4.Right = cboeTicks;
            cboetemp4.Operator = CodeBinaryOperatorType.BooleanOr;

            cis = new CodeConditionStatement();
            cis.Condition = cboetemp4;

            cis.TrueStatements.Add(new CodeThrowExceptionStatement(codeThrowException));

            /*
                catch
                {
                    throw new System.ArgumentOutOfRangeException(null, e.Message);
                }
            */
            string exceptVar = "e";
            CodeCatchClause catchblock = new CodeCatchClause(exceptVar);

            CodeObjectCreateExpression codeThrowExceptionWithArgs = new CodeObjectCreateExpression();
            codeThrowExceptionWithArgs.CreateType = new CodeTypeReference
                (PublicNamesUsed["ArgumentOutOfRangeException"].ToString());
            codeThrowExceptionWithArgs.Parameters.Add(new CodePrimitiveExpression(null));
            codeThrowExceptionWithArgs.Parameters.Add
                (
                new CodePropertyReferenceExpression
                (
                new CodeVariableReferenceExpression(exceptVar),
                "Message"
                )
                );
            catchblock.Statements.Add(new CodeThrowExceptionStatement(codeThrowExceptionWithArgs));
            //
            // add the catch block to the try block
            //
            tryblock.CatchClauses.Add(catchblock);
            //
            // add the try block to cmmts
            //
            cmmts.Statements.Add(tryblock);

            /*
                timespan = new System.TimeSpan(days, hours, minutes, seconds, 0);
            */

            string timespan = "timespan";
            coce = new CodeObjectCreateExpression();
            coce.CreateType = new CodeTypeReference("System.TimeSpan");
            coce.Parameters.Add(new CodeVariableReferenceExpression(days));
            coce.Parameters.Add(new CodeVariableReferenceExpression(hours));
            coce.Parameters.Add(new CodeVariableReferenceExpression(minutes));
            coce.Parameters.Add(new CodeVariableReferenceExpression(seconds));
            coce.Parameters.Add(new CodePrimitiveExpression(0));

            cmmts.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.TimeSpan"), timespan, coce));

            /*
                TimeSpan tsTemp = System.TimeSpan.FromTicks(ticks);
                timespan = timespan.Add(tsTemp);
            */
            string tsTemp = "tsTemp";
            cmie = new CodeMethodInvokeExpression();
            cmie.Method = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression("System.TimeSpan"), "FromTicks");
            cmie.Parameters.Add(new CodeVariableReferenceExpression(ticks));

            cmmts.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.TimeSpan"), tsTemp, cmie));

            cmie = new CodeMethodInvokeExpression();
            cmie.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(timespan), "Add");
            cmie.Parameters.Add(new CodeVariableReferenceExpression(tsTemp));

            cmmts.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(timespan), cmie));


            /*
                return timespan;
            */
            cmmts.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(timespan)));

            cc.Members.Add(cmmts);
        }

        // Exclusive helper function to be used from AddToTimeSpanFunction
        private static void ToTimeSpanHelper(int start, int numOfCharacters, string strVarToAssign, CodeStatementCollection statCol)
        {
            string strTemp = "tempString";
            string tsParam = "dmtfTimespan";

            CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
            cmie.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(tsParam), "Substring");
            cmie.Parameters.Add(new CodePrimitiveExpression(start));
            cmie.Parameters.Add(new CodePrimitiveExpression(numOfCharacters));

            statCol.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(strTemp), cmie));

            cmie = new CodeMethodInvokeExpression();
            cmie.Method = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression("System.Int32"), "Parse");
            cmie.Parameters.Add(new CodeVariableReferenceExpression(strTemp));

            statCol.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(strVarToAssign), cmie));
        }

        private void InitPrivateMemberVariables(CodeMemberMethod cmMethod)
        {
            CodeMethodInvokeExpression cmieInit = new CodeMethodInvokeExpression();
            cmieInit.Method.MethodName = PrivateNamesUsed["initVariable"].ToString();

            cmMethod.Statements.Add(cmieInit);
        }

        private void GenerateMethodToInitializeVariables()
        {

            CodeMemberMethod cmmInit = new CodeMemberMethod();
            cmmInit.Name = PrivateNamesUsed["initVariable"].ToString();
            cmmInit.Attributes = MemberAttributes.Private | MemberAttributes.Final;



            cmmInit.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(PrivateNamesUsed["AutoCommitProperty"].ToString()),
                new CodePrimitiveExpression(true)));
            cmmInit.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(PrivateNamesUsed["IsEmbedded"].ToString()),
                new CodePrimitiveExpression(false)));

            cc.Members.Add(cmmInit);
        }


        //
        private static CodeMethodInvokeExpression GenerateConcatStrings(CodeExpression ce1, CodeExpression ce2)
        {
            CodeExpression[] cmieParams = { ce1, ce2 };

            CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("System.String"),
                "Concat",
                cmieParams);
            return cmie;
        }


    }

}
