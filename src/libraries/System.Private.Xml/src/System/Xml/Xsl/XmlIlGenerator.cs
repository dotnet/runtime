// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Xml.XPath;
using System.Xml.Xsl.IlGen;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Xsl
{
    internal delegate void ExecuteDelegate(XmlQueryRuntime runtime);


    /// <summary>
    /// This internal class is the entry point for creating Msil assemblies from QilExpression.
    /// </summary>
    /// <remarks>
    /// Generate will return an AssemblyBuilder with the following setup:
    /// Assembly Name = "MS.Internal.Xml.CompiledQuery"
    /// Module Dll Name = "MS.Internal.Xml.CompiledQuery.dll"
    /// public class MS.Internal.Xml.CompiledQuery.Test {
    ///     public static void Execute(XmlQueryRuntime runtime);
    ///     public static void Root(XmlQueryRuntime runtime);
    ///     private static ... UserMethod1(XmlQueryRuntime runtime, ...);
    ///     ...
    ///     private static ... UserMethodN(XmlQueryRuntime runtime, ...);
    /// }
    ///
    /// XmlILGenerator incorporates a number of different technologies in order to generate efficient code that avoids caching
    /// large result sets in memory:
    ///
    /// 1. Code Iterators - Query results are computed using a set of composable, interlocking iterators that alone perform a
    /// simple task, but together execute complex queries.  The iterators are actually little blocks of code
    /// that are connected to each other using a series of jumps.  Because each iterator is not instantiated
    /// as a separate object, the number of objects and number of function calls is kept to a minimum during
    /// execution.  Also, large result sets are often computed incrementally, with each iterator performing one step in a
    /// pipeline of sequence items.
    ///
    /// 2. Analyzers - During code generation, QilToMsil traverses the semantic tree representation of the query (QIL) several times.
    /// As visits to each node in the tree start and end, various Analyzers are invoked.  These Analyzers incrementally
    /// collect and store information that is later used to generate faster and smaller code.
    /// </remarks>
    [RequiresDynamicCode("Creates DynamicMethods")]
    internal sealed class XmlILGenerator
    {
        private QilExpression? _qil;
        private GenerateHelper? _helper;
        private XmlILOptimizerVisitor? _optVisitor;
        private XmlILVisitor? _xmlIlVisitor;
        private XmlILModule? _module;

        /// <summary>
        /// Always output debug information in debug mode.
        /// </summary>
        public XmlILGenerator()
        {
        }

        /// <summary>
        /// Given the logical query plan (QilExpression) generate a physical query plan (MSIL) that can be executed.
        /// </summary>
        // SxS Note: The way the trace file names are created (hardcoded) is NOT SxS safe. However the files are
        // created only for internal tracing purposes. In addition XmlILTrace class is not compiled into retail
        // builds. As a result it is fine to suppress the FxCop SxS warning.
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This method will generate the IL methods using RefEmit at runtime, which will then try to call them " +
            "using methods that are annotated as RequiresUnreferencedCode. In this case, these uses can be suppressed as the " +
            "trimmer won't be able to trim any IL that gets generated at runtime.")]
        public XmlILCommand? Generate(QilExpression query, TypeBuilder? typeBldr)
        {
            _qil = query;

            bool useLRE = (
                !_qil.IsDebug &&
                (typeBldr == null)
#if DEBUG
                && !XmlILTrace.IsEnabled // Dump assembly to disk; can't do this when using LRE
#endif
            );
            bool emitSymbols = _qil.IsDebug;

            // In debug code, ensure that input QIL is correct
            QilValidationVisitor.Validate(_qil);

#if DEBUG
            // Trace Qil before optimization
            XmlILTrace.WriteQil(_qil, "qilbefore.xml");

            // Trace optimizations
            XmlILTrace.TraceOptimizations(_qil, "qilopt.xml");
#endif

            // Optimize and annotate the Qil graph
            _optVisitor = new XmlILOptimizerVisitor(_qil, !_qil.IsDebug);
            _qil = _optVisitor.Optimize();

            // In debug code, ensure that output QIL is correct
            QilValidationVisitor.Validate(_qil);

#if DEBUG
            // Trace Qil after optimization
            XmlILTrace.WriteQil(_qil, "qilafter.xml");
#endif

            // Create module in which methods will be generated
            if (typeBldr != null)
            {
                _module = new XmlILModule(typeBldr);
            }
            else
            {
                _module = new XmlILModule(useLRE, emitSymbols);
            }

            // Create a code generation helper for the module; enable optimizations if IsDebug is false
            _helper = new GenerateHelper(_module, _qil.IsDebug);

            // Create helper methods
            CreateHelperFunctions();

            // Create metadata for the Execute function, which is the entry point to the query
            // public static void Execute(XmlQueryRuntime);
            MethodInfo methExec = _module.DefineMethod("Execute", typeof(void), Type.EmptyTypes, Array.Empty<string>(), XmlILMethodAttributes.NonUser);

            // Create metadata for the root expression
            // public void Root()
            Debug.Assert(_qil.Root != null);
            XmlILMethodAttributes methAttrs = (_qil.Root.SourceLine == null) ? XmlILMethodAttributes.NonUser : XmlILMethodAttributes.None;
            MethodInfo methRoot = _module.DefineMethod("Root", typeof(void), Type.EmptyTypes, Array.Empty<string>(), methAttrs);

            // Declare all early bound function objects
            foreach (EarlyBoundInfo info in _qil.EarlyBoundTypes)
            {
                _helper.StaticData.DeclareEarlyBound(info.NamespaceUri, info.EarlyBoundType);
            }

            // Create metadata for each QilExpression function that has at least one caller
            CreateFunctionMetadata(_qil.FunctionList);

            // Create metadata for each QilExpression global variable and parameter
            CreateGlobalValueMetadata(_qil.GlobalVariableList);
            CreateGlobalValueMetadata(_qil.GlobalParameterList);

            // Generate Execute method
            GenerateExecuteFunction(methExec, methRoot);

            // Visit the QilExpression graph
            _xmlIlVisitor = new XmlILVisitor();
            _xmlIlVisitor.Visit(_qil, _helper, methRoot);

            // Collect all static information required by the runtime
            XmlQueryStaticData staticData = new XmlQueryStaticData(
                _qil.DefaultWriterSettings,
                _qil.WhitespaceRules,
                _helper.StaticData
            );

            // Create static constructor that initializes XmlQueryStaticData instance at runtime
            if (typeBldr != null)
            {
                CreateTypeInitializer(staticData);

                // Finish up creation of the type
                _module.BakeMethods();

                return null;
            }
            else
            {
                // Finish up creation of the type
                _module.BakeMethods();

                // Create delegate over "Execute" method
                ExecuteDelegate delExec = (ExecuteDelegate)_module.CreateDelegate("Execute", typeof(ExecuteDelegate));
                return new XmlILCommand(delExec, staticData);
            }
        }

        /// <summary>
        /// Create MethodBuilder metadata for the specified QilExpression function.  Annotate ndFunc with the
        /// MethodBuilder.  Also, each QilExpression argument type should be converted to a corresponding Clr type.
        /// Each argument QilExpression node should be annotated with the resulting ParameterBuilder.
        /// </summary>
        private void CreateFunctionMetadata(IList<QilNode> funcList)
        {
            MethodInfo methInfo;
            Type[] paramTypes;
            string[] paramNames;
            Type typReturn;
            XmlILMethodAttributes methAttrs;

            foreach (QilFunction ndFunc in funcList)
            {
                paramTypes = new Type[ndFunc.Arguments.Count];
                paramNames = new string[ndFunc.Arguments.Count];

                // Loop through all other parameters and save their types in the array
                for (int arg = 0; arg < ndFunc.Arguments.Count; arg++)
                {
                    QilParameter ndParam = (QilParameter)ndFunc.Arguments[arg];
                    Debug.Assert(ndParam.NodeType == QilNodeType.Parameter);

                    // Get the type of each argument as a Clr type
                    paramTypes[arg] = XmlILTypeHelper.GetStorageType(ndParam.XmlType!);

                    // Get the name of each argument
                    if (ndParam.DebugName != null)
                        paramNames[arg] = ndParam.DebugName;
                }

                // Get the type of the return value
                if (XmlILConstructInfo.Read(ndFunc).PushToWriterLast)
                {
                    // Push mode functions do not have a return value
                    typReturn = typeof(void);
                }
                else
                {
                    // Pull mode functions have a return value
                    typReturn = XmlILTypeHelper.GetStorageType(ndFunc.XmlType!);
                }

                // Create the method metadata
                methAttrs = ndFunc.SourceLine == null ? XmlILMethodAttributes.NonUser : XmlILMethodAttributes.None;
                methInfo = _module!.DefineMethod(ndFunc.DebugName!, typReturn, paramTypes, paramNames, methAttrs);

                for (int arg = 0; arg < ndFunc.Arguments.Count; arg++)
                {
                    // Set location of parameter on Let node annotation
                    XmlILAnnotation.Write(ndFunc.Arguments[arg]).ArgumentPosition = arg;
                }

                // Annotate function with the MethodInfo
                XmlILAnnotation.Write(ndFunc).FunctionBinding = methInfo;
            }
        }

        /// <summary>
        /// Generate metadata for a method that calculates a global value.
        /// </summary>
        private void CreateGlobalValueMetadata(IList<QilNode> globalList)
        {
            MethodInfo methInfo;
            Type typReturn;
            XmlILMethodAttributes methAttrs;

            foreach (QilReference ndRef in globalList)
            {
                // public T GlobalValue()
                typReturn = XmlILTypeHelper.GetStorageType(ndRef.XmlType!);
                methAttrs = ndRef.SourceLine == null ? XmlILMethodAttributes.NonUser : XmlILMethodAttributes.None;
                methInfo = _module!.DefineMethod(ndRef.DebugName!.ToString(), typReturn, Type.EmptyTypes, Array.Empty<string>(), methAttrs);

                // Annotate function with MethodBuilder
                XmlILAnnotation.Write(ndRef).FunctionBinding = methInfo;
            }
        }

        /// <summary>
        /// Generate the "Execute" method, which is the entry point to the query.
        /// </summary>
        private MethodInfo GenerateExecuteFunction(MethodInfo methExec, MethodInfo methRoot)
        {
            _helper!.MethodBegin(methExec, null, false);

            // Force some or all global values to be evaluated at start of query
            EvaluateGlobalValues(_qil!.GlobalVariableList);
            EvaluateGlobalValues(_qil.GlobalParameterList);

            // Root(runtime);
            _helper.LoadQueryRuntime();
            _helper.Call(methRoot);

            _helper.MethodEnd();

            return methExec;
        }

        /// <summary>
        /// Create and generate various helper methods, which are called by the generated code.
        /// </summary>
        private void CreateHelperFunctions()
        {
            MethodInfo meth;
            Label lblClone;

            // public static XPathNavigator SyncToNavigator(XPathNavigator, XPathNavigator);
            meth = _module!.DefineMethod(
                            "SyncToNavigator",
                            typeof(XPathNavigator),
                            new Type[] { typeof(XPathNavigator), typeof(XPathNavigator) },
                            new string?[] { null, null },
                            XmlILMethodAttributes.NonUser | XmlILMethodAttributes.Raw);

            _helper!.MethodBegin(meth, null, false);

            // if (navigatorThis != null && navigatorThis.MoveTo(navigatorThat))
            //     return navigatorThis;
            lblClone = _helper.DefineLabel();
            _helper.Emit(OpCodes.Ldarg_0);
            _helper.Emit(OpCodes.Brfalse, lblClone);
            _helper.Emit(OpCodes.Ldarg_0);
            _helper.Emit(OpCodes.Ldarg_1);
            _helper.Call(XmlILMethods.NavMoveTo);
            _helper.Emit(OpCodes.Brfalse, lblClone);
            _helper.Emit(OpCodes.Ldarg_0);
            _helper.Emit(OpCodes.Ret);

            // LabelClone:
            // return navigatorThat.Clone();
            _helper.MarkLabel(lblClone);
            _helper.Emit(OpCodes.Ldarg_1);
            _helper.Call(XmlILMethods.NavClone);

            _helper.MethodEnd();
        }

        /// <summary>
        /// Generate code to force evaluation of some or all global variables and/or parameters.
        /// </summary>
        private void EvaluateGlobalValues(IList<QilNode> iterList)
        {
            MethodInfo? methInfo;

            foreach (QilIterator ndIter in iterList)
            {
                // Evaluate global if generating debug code, or if global might have side effects
                if (_qil!.IsDebug || OptimizerPatterns.Read(ndIter).MatchesPattern(OptimizerPatternName.MaybeSideEffects))
                {
                    // Get MethodInfo that evaluates the global value and discard its return value
                    methInfo = XmlILAnnotation.Write(ndIter).FunctionBinding;
                    Debug.Assert(methInfo != null, "MethodInfo for global value should have been created previously.");

                    _helper!.LoadQueryRuntime();
                    _helper.Call(methInfo);
                    _helper.Emit(OpCodes.Pop);
                }
            }
        }

        /// <summary>
        /// Create static constructor that initializes XmlQueryStaticData instance at runtime.
        /// </summary>
        public void CreateTypeInitializer(XmlQueryStaticData staticData)
        {
            byte[] data;
            Type[]? ebTypes;
            FieldInfo fldInitData, fldData, fldTypes;
            ConstructorInfo cctor;

            staticData.GetObjectData(out data, out ebTypes);
            fldInitData = _module!.DefineInitializedData($"__{XmlQueryStaticData.DataFieldName}", data);
            fldData = _module.DefineField(XmlQueryStaticData.DataFieldName, typeof(object));
            fldTypes = _module.DefineField(XmlQueryStaticData.TypesFieldName, typeof(Type[]));

            cctor = _module.DefineTypeInitializer();
            _helper!.MethodBegin(cctor, null, false);

            // s_data = new byte[s_initData.Length] { s_initData };
            _helper.LoadInteger(data.Length);
            _helper.Emit(OpCodes.Newarr, typeof(byte));
            _helper.Emit(OpCodes.Dup);
            _helper.Emit(OpCodes.Ldtoken, fldInitData);
            _helper.Call(XmlILMethods.InitializeArray);
            _helper.Emit(OpCodes.Stsfld, fldData);

            if (ebTypes != null)
            {
                // Type[] types = new Type[s_ebTypes.Length];
                LocalBuilder locTypes = _helper.DeclareLocal("$$$types", typeof(Type[]));
                _helper.LoadInteger(ebTypes.Length);
                _helper.Emit(OpCodes.Newarr, typeof(Type));
                _helper.Emit(OpCodes.Stloc, locTypes);

                for (int idx = 0; idx < ebTypes.Length; idx++)
                {
                    // types[idx] = ebTypes[idx];
                    _helper.Emit(OpCodes.Ldloc, locTypes);
                    _helper.LoadInteger(idx);
                    _helper.LoadType(ebTypes[idx]);
                    _helper.Emit(OpCodes.Stelem_Ref);
                }

                // s_types = types;
                _helper.Emit(OpCodes.Ldloc, locTypes);
                _helper.Emit(OpCodes.Stsfld, fldTypes);
            }

            _helper.MethodEnd();
        }
    }
}
