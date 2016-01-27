// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.TCEAdapterGen {
    using System.Runtime.InteropServices.ComTypes;
    using ubyte = System.Byte;
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Collections;
    using System.Threading;
    using System.Diagnostics.Contracts;
    
    internal class EventProviderWriter
    {
        private const BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

        private readonly Type[] MonitorEnterParamTypes = new Type[] { typeof(Object), Type.GetType("System.Boolean&") };
        
        public EventProviderWriter( ModuleBuilder OutputModule, String strDestTypeName, Type EventItfType, Type SrcItfType, Type SinkHelperType )
        {
            m_OutputModule = OutputModule;  
            m_strDestTypeName = strDestTypeName;
            m_EventItfType = EventItfType;
            m_SrcItfType = SrcItfType;
            m_SinkHelperType = SinkHelperType;
        }
        
        public Type Perform()
        {       
            // Create the event provider class.
            TypeBuilder OutputTypeBuilder = m_OutputModule.DefineType(
                m_strDestTypeName,
                TypeAttributes.Sealed | TypeAttributes.NotPublic, 
                typeof(Object),
                new Type[]{m_EventItfType, typeof(IDisposable)}
                );

            // Create the event source field.
            FieldBuilder fbCPC = OutputTypeBuilder.DefineField( 
                "m_ConnectionPointContainer", 
                typeof(IConnectionPointContainer), 
                FieldAttributes.Private
                );
            
            // Create array of event sink helpers.
            FieldBuilder fbSinkHelper = OutputTypeBuilder.DefineField( 
                "m_aEventSinkHelpers", 
                typeof(ArrayList), 
                FieldAttributes.Private
                );
            
            // Define the connection point field.
            FieldBuilder fbEventCP = OutputTypeBuilder.DefineField( 
                "m_ConnectionPoint", 
                typeof(IConnectionPoint),
                FieldAttributes.Private
                );
            
            // Define the InitXXX method.
            MethodBuilder InitSrcItfMethodBuilder = 
                DefineInitSrcItfMethod( OutputTypeBuilder, m_SrcItfType, fbSinkHelper, fbEventCP, fbCPC );

            // Process all the methods in the event interface.
            MethodInfo[] aMethods = TCEAdapterGenerator.GetNonPropertyMethods(m_SrcItfType);
            for ( int cMethods = 0; cMethods < aMethods.Length; cMethods++ )
            {
                if ( m_SrcItfType == aMethods[cMethods].DeclaringType )
                {
                    // Define the add_XXX method.
                    MethodBuilder AddEventMethodBuilder = DefineAddEventMethod( 
                        OutputTypeBuilder, aMethods[cMethods], m_SinkHelperType, fbSinkHelper, fbEventCP, InitSrcItfMethodBuilder );
                    
                    // Define the remove_XXX method.
                    MethodBuilder RemoveEventMethodBuilder = DefineRemoveEventMethod( 
                        OutputTypeBuilder, aMethods[cMethods], m_SinkHelperType, fbSinkHelper, fbEventCP );
                }
            }
            
            // Define the constructor.
            DefineConstructor( OutputTypeBuilder, fbCPC );
            
            // Define the finalize method.
            MethodBuilder FinalizeMethod = DefineFinalizeMethod( OutputTypeBuilder, m_SinkHelperType, fbSinkHelper, fbEventCP );
            
            // Define the Dispose method.
            DefineDisposeMethod( OutputTypeBuilder, FinalizeMethod);

            return OutputTypeBuilder.CreateType();
        }
        
        private MethodBuilder DefineAddEventMethod( TypeBuilder OutputTypeBuilder, MethodInfo SrcItfMethod, Type SinkHelperClass, FieldBuilder fbSinkHelperArray, FieldBuilder fbEventCP, MethodBuilder mbInitSrcItf )
        {
            Type[] aParamTypes;
            
            // Find the delegate on the event sink helper.
            FieldInfo DelegateField = SinkHelperClass.GetField( "m_" + SrcItfMethod.Name + "Delegate" );
            Contract.Assert(DelegateField != null, "Unable to find the field m_" + SrcItfMethod.Name + "Delegate on the sink helper");
            
            // Find the cookie on the event sink helper.
            FieldInfo CookieField = SinkHelperClass.GetField( "m_dwCookie" );
            Contract.Assert(CookieField != null, "Unable to find the field m_dwCookie on the sink helper");
            
            // Retrieve the sink helper's constructor.
            ConstructorInfo SinkHelperCons = SinkHelperClass.GetConstructor(EventProviderWriter.DefaultLookup | BindingFlags.NonPublic, null, Array.Empty<Type>(), null );    
            Contract.Assert(SinkHelperCons != null, "Unable to find the constructor for the sink helper");
            
            // Retrieve the IConnectionPoint.Advise method.
            MethodInfo CPAdviseMethod = typeof(IConnectionPoint).GetMethod( "Advise" );
            Contract.Assert(CPAdviseMethod != null, "Unable to find the method ConnectionPoint.Advise");
           
            // Retrieve the ArrayList.Add method.
            aParamTypes = new Type[1];
            aParamTypes[0] = typeof(Object);
            MethodInfo ArrayListAddMethod = typeof(ArrayList).GetMethod( "Add", aParamTypes, null );
            Contract.Assert(ArrayListAddMethod != null, "Unable to find the method ArrayList.Add");

            // Retrieve the Monitor.Enter() method.
            MethodInfo MonitorEnterMethod = typeof(Monitor).GetMethod( "Enter", MonitorEnterParamTypes, null );
            Contract.Assert(MonitorEnterMethod != null, "Unable to find the method Monitor.Enter()");
            
            // Retrieve the Monitor.Exit() method.
            aParamTypes[0] = typeof(Object);
            MethodInfo MonitorExitMethod = typeof(Monitor).GetMethod( "Exit", aParamTypes, null );
            Contract.Assert(MonitorExitMethod != null, "Unable to find the method Monitor.Exit()");
            
            // Define the add_XXX method.
            Type[] parameterTypes;
            parameterTypes = new Type[1];
            parameterTypes[0] = DelegateField.FieldType;
            MethodBuilder Meth = OutputTypeBuilder.DefineMethod(
                "add_" + SrcItfMethod.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                null,
                parameterTypes );
            
            ILGenerator il = Meth.GetILGenerator();
            
            // Define a label for the m_IFooEventsCP comparision.
            Label EventCPNonNullLabel = il.DefineLabel();
            
            // Declare the local variables.
            LocalBuilder ltSinkHelper = il.DeclareLocal( SinkHelperClass );
            LocalBuilder ltCookie = il.DeclareLocal( typeof(Int32) );
            LocalBuilder ltLockTaken = il.DeclareLocal( typeof(bool) );

            // Generate the following code:
            //   try {
            il.BeginExceptionBlock();

            // Generate the following code:
            //   Monitor.Enter(this, ref lockTaken);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldloca_S, ltLockTaken);
            il.Emit(OpCodes.Call, MonitorEnterMethod);

            // Generate the following code:
            //   if ( m_IFooEventsCP != null ) goto EventCPNonNullLabel;
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbEventCP );
            il.Emit( OpCodes.Brtrue, EventCPNonNullLabel );
            
            // Generate the following code:
            //   InitIFooEvents();
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Call, mbInitSrcItf );
            
            // Mark this as label to jump to if the CP is not null.
            il.MarkLabel( EventCPNonNullLabel );
            
            // Generate the following code:
            //   IFooEvents_SinkHelper SinkHelper = new IFooEvents_SinkHelper;  
            il.Emit( OpCodes.Newobj, SinkHelperCons );
            il.Emit( OpCodes.Stloc, ltSinkHelper );
            
            // Generate the following code:
            //   dwCookie = 0;
            il.Emit( OpCodes.Ldc_I4_0 );
            il.Emit( OpCodes.Stloc, ltCookie );
            
            // Generate the following code:
            //   m_IFooEventsCP.Advise( SinkHelper, dwCookie );
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbEventCP );
            il.Emit( OpCodes.Ldloc, ltSinkHelper );
            il.Emit( OpCodes.Castclass, typeof(Object) );
            il.Emit( OpCodes.Ldloca, ltCookie );
            il.Emit( OpCodes.Callvirt, CPAdviseMethod );
            
            // Generate the following code:
            //   SinkHelper.m_dwCookie = dwCookie;
            il.Emit( OpCodes.Ldloc, ltSinkHelper );
            il.Emit( OpCodes.Ldloc, ltCookie );
            il.Emit( OpCodes.Stfld, CookieField );
            
            // Generate the following code:
            //   SinkHelper.m_FooDelegate = d;
            il.Emit( OpCodes.Ldloc, ltSinkHelper );
            il.Emit( OpCodes.Ldarg, (short)1 );
            il.Emit( OpCodes.Stfld, DelegateField );
            
            // Generate the following code:
            //   m_aIFooEventsHelpers.Add( SinkHelper );
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbSinkHelperArray );
            il.Emit( OpCodes.Ldloc, ltSinkHelper );
            il.Emit( OpCodes.Castclass, typeof(Object) );
            il.Emit( OpCodes.Callvirt, ArrayListAddMethod );
            il.Emit( OpCodes.Pop );
            
            // Generate the following code:
            //   } finally {
            il.BeginFinallyBlock();

            // Generate the following code:
            //   if (lockTaken)
            //      Monitor.Exit(this);
            Label skipExit = il.DefineLabel();
            il.Emit( OpCodes.Ldloc, ltLockTaken );
            il.Emit( OpCodes.Brfalse_S, skipExit );
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Call, MonitorExitMethod );
            il.MarkLabel(skipExit);

            // Generate the following code:
            //   }
            il.EndExceptionBlock();            

            // Generate the return opcode.
            il.Emit( OpCodes.Ret );
            
            return Meth;
        }
        
        private MethodBuilder DefineRemoveEventMethod( TypeBuilder OutputTypeBuilder, MethodInfo SrcItfMethod, Type SinkHelperClass, FieldBuilder fbSinkHelperArray, FieldBuilder fbEventCP )
        {
            Type[] aParamTypes;
            
            // Find the delegate on the event sink helper.
            FieldInfo DelegateField = SinkHelperClass.GetField( "m_" + SrcItfMethod.Name + "Delegate" );
            Contract.Assert(DelegateField != null, "Unable to find the field m_" + SrcItfMethod.Name + "Delegate on the sink helper");
            
            // Find the cookie on the event sink helper.
            FieldInfo CookieField = SinkHelperClass.GetField( "m_dwCookie" );
            Contract.Assert(CookieField != null, "Unable to find the field m_dwCookie on the sink helper");
            
            // Retrieve the ArrayList.RemoveAt method.
            aParamTypes = new Type[1];
            aParamTypes[0] = typeof(Int32);
            MethodInfo ArrayListRemoveMethod = typeof(ArrayList).GetMethod( "RemoveAt", aParamTypes, null );
            Contract.Assert(ArrayListRemoveMethod != null, "Unable to find the method ArrayList.RemoveAt()");
            
            // Retrieve the ArrayList.Item property get method.
            PropertyInfo ArrayListItemProperty = typeof(ArrayList).GetProperty( "Item" );
            Contract.Assert(ArrayListItemProperty != null, "Unable to find the property ArrayList.Item");
            MethodInfo ArrayListItemGetMethod = ArrayListItemProperty.GetGetMethod();
            Contract.Assert(ArrayListItemGetMethod != null, "Unable to find the get method for property ArrayList.Item");
            
            // Retrieve the ArrayList.Count property get method.
            PropertyInfo ArrayListSizeProperty = typeof(ArrayList).GetProperty( "Count" );
            Contract.Assert(ArrayListSizeProperty != null, "Unable to find the property ArrayList.Count");
            MethodInfo ArrayListSizeGetMethod = ArrayListSizeProperty.GetGetMethod();
            Contract.Assert(ArrayListSizeGetMethod != null, "Unable to find the get method for property ArrayList.Count");
            
            // Retrieve the Delegate.Equals() method.
            aParamTypes[0] = typeof(Delegate);
            MethodInfo DelegateEqualsMethod = typeof(Delegate).GetMethod( "Equals", aParamTypes, null );
            Contract.Assert(DelegateEqualsMethod != null, "Unable to find the method Delegate.Equlals()");

            // Retrieve the Monitor.Enter() method.
            MethodInfo MonitorEnterMethod = typeof(Monitor).GetMethod("Enter", MonitorEnterParamTypes, null);
            Contract.Assert(MonitorEnterMethod != null, "Unable to find the method Monitor.Enter()");
            
            // Retrieve the Monitor.Exit() method.
            aParamTypes[0] = typeof(Object);
            MethodInfo MonitorExitMethod = typeof(Monitor).GetMethod( "Exit", aParamTypes, null );
            Contract.Assert(MonitorExitMethod != null, "Unable to find the method Monitor.Exit()");
            
            // Retrieve the ConnectionPoint.Unadvise() method.
            MethodInfo CPUnadviseMethod = typeof(IConnectionPoint).GetMethod( "Unadvise" );
            Contract.Assert(CPUnadviseMethod != null, "Unable to find the method ConnectionPoint.Unadvise()");
            
            // Retrieve the Marshal.ReleaseComObject() method.
            MethodInfo ReleaseComObjectMethod = typeof(Marshal).GetMethod( "ReleaseComObject" );
            Contract.Assert(ReleaseComObjectMethod != null, "Unable to find the method Marshal.ReleaseComObject()");
            
            // Define the remove_XXX method.
            Type[] parameterTypes;
            parameterTypes = new Type[1];
            parameterTypes[0] = DelegateField.FieldType;
            MethodBuilder Meth = OutputTypeBuilder.DefineMethod(
                "remove_" + SrcItfMethod.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                null,
                parameterTypes );
            
            ILGenerator il = Meth.GetILGenerator();
            
            // Declare the local variables.
            LocalBuilder ltNumSinkHelpers = il.DeclareLocal( typeof(Int32) );
            LocalBuilder ltSinkHelperCounter = il.DeclareLocal( typeof(Int32) );
            LocalBuilder ltCurrSinkHelper = il.DeclareLocal( SinkHelperClass );
            LocalBuilder ltLockTaken = il.DeclareLocal(typeof(bool));
            
            // Generate the labels for the for loop.
            Label ForBeginLabel = il.DefineLabel();
            Label ForEndLabel = il.DefineLabel();
            Label FalseIfLabel = il.DefineLabel();
            Label MonitorExitLabel = il.DefineLabel();
            
            // Generate the following code:
            //   try {
            il.BeginExceptionBlock();

            // Generate the following code:
            //   Monitor.Enter(this, ref lockTaken);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldloca_S, ltLockTaken);
            il.Emit(OpCodes.Call, MonitorEnterMethod);

            // Generate the following code:
            //   if ( m_aIFooEventsHelpers == null ) goto ForEndLabel;        
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbSinkHelperArray );
            il.Emit( OpCodes.Brfalse, ForEndLabel );

            // Generate the following code:
            //   int NumEventHelpers = m_aIFooEventsHelpers.Count;
            //   int cEventHelpers = 0;
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbSinkHelperArray );
            il.Emit( OpCodes.Callvirt, ArrayListSizeGetMethod );
            il.Emit( OpCodes.Stloc, ltNumSinkHelpers );
            il.Emit( OpCodes.Ldc_I4, 0 );
            il.Emit( OpCodes.Stloc, ltSinkHelperCounter );
            
            // Generate the following code:
            //   if ( 0 >= NumEventHelpers ) goto ForEndLabel;        
            il.Emit( OpCodes.Ldc_I4, 0 );
            il.Emit( OpCodes.Ldloc, ltNumSinkHelpers );
            il.Emit( OpCodes.Bge, ForEndLabel );
            
            // Mark this as the beginning of the for loop's body.
            il.MarkLabel( ForBeginLabel );
            
            // Generate the following code:
            //   CurrentHelper = (IFooEvents_SinkHelper)m_aIFooEventsHelpers.Get( cEventHelpers );
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbSinkHelperArray );
            il.Emit( OpCodes.Ldloc, ltSinkHelperCounter );
            il.Emit( OpCodes.Callvirt, ArrayListItemGetMethod );
            il.Emit( OpCodes.Castclass, SinkHelperClass );
            il.Emit( OpCodes.Stloc, ltCurrSinkHelper );
            
            // Generate the following code:
            //   if ( CurrentHelper.m_FooDelegate )
            il.Emit( OpCodes.Ldloc, ltCurrSinkHelper );
            il.Emit( OpCodes.Ldfld, DelegateField );
            il.Emit( OpCodes.Ldnull );
            il.Emit( OpCodes.Beq, FalseIfLabel );
            
            // Generate the following code:
            //   if ( CurrentHelper.m_FooDelegate.Equals( d ) )
            il.Emit( OpCodes.Ldloc, ltCurrSinkHelper );
            il.Emit( OpCodes.Ldfld, DelegateField );
            il.Emit( OpCodes.Ldarg, (short)1 );
            il.Emit( OpCodes.Castclass, typeof(Object) );
            il.Emit( OpCodes.Callvirt, DelegateEqualsMethod );
            il.Emit( OpCodes.Ldc_I4, 0xff );
            il.Emit( OpCodes.And );
            il.Emit( OpCodes.Ldc_I4, 0 );
            il.Emit( OpCodes.Beq, FalseIfLabel );
            
            // Generate the following code:
            //   m_aIFooEventsHelpers.RemoveAt( cEventHelpers );
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbSinkHelperArray );
            il.Emit( OpCodes.Ldloc, ltSinkHelperCounter );
            il.Emit( OpCodes.Callvirt, ArrayListRemoveMethod );
            
            // Generate the following code:
            //   m_IFooEventsCP.Unadvise( CurrentHelper.m_dwCookie );
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbEventCP );
            il.Emit( OpCodes.Ldloc, ltCurrSinkHelper );
            il.Emit( OpCodes.Ldfld, CookieField );
            il.Emit( OpCodes.Callvirt, CPUnadviseMethod );
            
            // Generate the following code:
            //   if ( NumEventHelpers > 1) break;
            il.Emit( OpCodes.Ldloc, ltNumSinkHelpers );
            il.Emit( OpCodes.Ldc_I4, 1 );
            il.Emit( OpCodes.Bgt, ForEndLabel );
                       
            // Generate the following code:
            //   Marshal.ReleaseComObject(m_IFooEventsCP);
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbEventCP );
            il.Emit( OpCodes.Call, ReleaseComObjectMethod );            
            il.Emit( OpCodes.Pop );

            // Generate the following code:
            //   m_IFooEventsCP = null;
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldnull );
            il.Emit( OpCodes.Stfld, fbEventCP );
            
            // Generate the following code:
            //   m_aIFooEventsHelpers = null;      
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldnull );
            il.Emit( OpCodes.Stfld, fbSinkHelperArray );
            
            // Generate the following code:
            //   break;
            il.Emit( OpCodes.Br, ForEndLabel );
            
            // Mark this as the label to jump to when the if statement is false.
            il.MarkLabel( FalseIfLabel );       
            
            // Generate the following code:
            //   cEventHelpers++;
            il.Emit( OpCodes.Ldloc, ltSinkHelperCounter );
            il.Emit( OpCodes.Ldc_I4, 1 );
            il.Emit( OpCodes.Add );
            il.Emit( OpCodes.Stloc, ltSinkHelperCounter );
            
            // Generate the following code:
            //   if ( cEventHelpers < NumEventHelpers ) goto ForBeginLabel;
            il.Emit( OpCodes.Ldloc, ltSinkHelperCounter );
            il.Emit( OpCodes.Ldloc, ltNumSinkHelpers );
            il.Emit( OpCodes.Blt, ForBeginLabel );
            
            // Mark this as the end of the for loop's body.
            il.MarkLabel( ForEndLabel );

            // Generate the following code:
            //   } finally {
            il.BeginFinallyBlock();

            // Generate the following code:
            //   if (lockTaken)
            //      Monitor.Exit(this);
            Label skipExit = il.DefineLabel();
            il.Emit(OpCodes.Ldloc, ltLockTaken);
            il.Emit(OpCodes.Brfalse_S, skipExit);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Call, MonitorExitMethod);
            il.MarkLabel(skipExit);

            // Generate the following code:
            //   }
            il.EndExceptionBlock();            

            // Generate the return opcode.
            il.Emit( OpCodes.Ret );
            
            return Meth;
        }

        private MethodBuilder DefineInitSrcItfMethod( TypeBuilder OutputTypeBuilder, Type SourceInterface, FieldBuilder fbSinkHelperArray, FieldBuilder fbEventCP, FieldBuilder fbCPC )
        {
            // Retrieve the constructor info for the array list's default constructor.
            ConstructorInfo DefaultArrayListCons = typeof(ArrayList).GetConstructor(EventProviderWriter.DefaultLookup, null, Array.Empty<Type>(), null );
            Contract.Assert(DefaultArrayListCons != null, "Unable to find the constructor for class ArrayList");    
            
            // Temp byte array for Guid
            ubyte[] rgByteGuid = new ubyte[16];
            
            // Retrieve the constructor info for the Guid constructor.
            Type[] aParamTypes = new Type[1];
            aParamTypes[0] = typeof(Byte[]);
            ConstructorInfo ByteArrayGUIDCons = typeof(Guid).GetConstructor(EventProviderWriter.DefaultLookup, null, aParamTypes, null );
            Contract.Assert(ByteArrayGUIDCons != null, "Unable to find the constructor for GUID that accepts a string as argument");    
            
            // Retrieve the IConnectionPointContainer.FindConnectionPoint() method.
            MethodInfo CPCFindCPMethod = typeof(IConnectionPointContainer).GetMethod( "FindConnectionPoint" );
            Contract.Assert(CPCFindCPMethod != null, "Unable to find the method ConnectionPointContainer.FindConnectionPoint()");    
            
            // Define the Init method itself.
            MethodBuilder Meth = OutputTypeBuilder.DefineMethod(
                "Init", 
                MethodAttributes.Private, 
                null, 
                null );
            
            ILGenerator il = Meth.GetILGenerator();
            
            // Declare the local variables.
            LocalBuilder ltCP = il.DeclareLocal( typeof(IConnectionPoint) );
            LocalBuilder ltEvGuid = il.DeclareLocal( typeof(Guid) );
            LocalBuilder ltByteArrayGuid = il.DeclareLocal( typeof(Byte[]) );
            
            // Generate the following code:
            //   IConnectionPoint CP = NULL;
            il.Emit( OpCodes.Ldnull );
            il.Emit( OpCodes.Stloc, ltCP );
            
            // Get unsigned byte array for the GUID of the event interface.
            rgByteGuid = SourceInterface.GUID.ToByteArray();
            
            // Generate the following code:
            //  ubyte rgByteArray[] = new ubyte [16];
            il.Emit( OpCodes.Ldc_I4, 0x10 );
            il.Emit( OpCodes.Newarr, typeof(Byte) ); 
            il.Emit( OpCodes.Stloc, ltByteArrayGuid );
            
            // Generate the following code:
            //  rgByteArray[i] = rgByteGuid[i];
            for (int i = 0; i < 16; i++ )
            {
                il.Emit( OpCodes.Ldloc, ltByteArrayGuid );
                il.Emit( OpCodes.Ldc_I4, i );
                il.Emit( OpCodes.Ldc_I4, (int) (rgByteGuid[i]) );
                il.Emit( OpCodes.Stelem_I1);
            }
            
            // Generate the following code:
            //   EventItfGuid = Guid( ubyte b[] );          
            il.Emit( OpCodes.Ldloca, ltEvGuid );
            il.Emit( OpCodes.Ldloc, ltByteArrayGuid );
            il.Emit( OpCodes.Call, ByteArrayGUIDCons );
            
            // Generate the following code:
            //   m_ConnectionPointContainer.FindConnectionPoint( EventItfGuid, CP );
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbCPC );
            il.Emit( OpCodes.Ldloca, ltEvGuid );
            il.Emit( OpCodes.Ldloca, ltCP );
            il.Emit( OpCodes.Callvirt, CPCFindCPMethod );
            
            // Generate the following code:
            //   m_ConnectionPoint = (IConnectionPoint)CP;
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldloc, ltCP );
            il.Emit( OpCodes.Castclass, typeof(IConnectionPoint) );
            il.Emit( OpCodes.Stfld, fbEventCP );
            
            // Generate the following code:
            //   m_aEventSinkHelpers = new ArrayList;      
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Newobj, DefaultArrayListCons );
            il.Emit( OpCodes.Stfld, fbSinkHelperArray );   
            
            // Generate the return opcode.
            il.Emit( OpCodes.Ret );
            
            return Meth;
        }
        
        private void DefineConstructor( TypeBuilder OutputTypeBuilder, FieldBuilder fbCPC )
        {
            // Retrieve the constructor info for the base class's constructor.
            ConstructorInfo DefaultBaseClsCons = typeof(Object).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, Array.Empty<Type>(), null );
            Contract.Assert(DefaultBaseClsCons != null, "Unable to find the object's public default constructor");
            
            // Define the default constructor.
            MethodAttributes ctorAttributes = MethodAttributes.SpecialName | (DefaultBaseClsCons.Attributes & MethodAttributes.MemberAccessMask);
            MethodBuilder Cons = OutputTypeBuilder.DefineMethod( 
                ".ctor", 
                ctorAttributes, 
                null, 
                new Type[]{typeof(Object)} );
            
            ILGenerator il = Cons.GetILGenerator();
            
            // Generate the call to the base class constructor.
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Call, DefaultBaseClsCons );
            
            // Generate the following code:
            //   m_ConnectionPointContainer = (IConnectionPointContainer)EventSource;
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldarg, (short)1 );
            il.Emit( OpCodes.Castclass, typeof(IConnectionPointContainer) );
            il.Emit( OpCodes.Stfld, fbCPC );

            // Generate the return opcode.
            il.Emit( OpCodes.Ret );
        }
               
        private MethodBuilder DefineFinalizeMethod( TypeBuilder OutputTypeBuilder, Type SinkHelperClass, FieldBuilder fbSinkHelper, FieldBuilder fbEventCP )
        {
            // Find the cookie on the event sink helper.
            FieldInfo CookieField = SinkHelperClass.GetField( "m_dwCookie" );
            Contract.Assert(CookieField != null, "Unable to find the field m_dwCookie on the sink helper");    

            // Retrieve the ArrayList.Item property get method.
            PropertyInfo ArrayListItemProperty = typeof(ArrayList).GetProperty( "Item" );
            Contract.Assert(ArrayListItemProperty != null, "Unable to find the property ArrayList.Item");    
            MethodInfo ArrayListItemGetMethod = ArrayListItemProperty.GetGetMethod();
            Contract.Assert(ArrayListItemGetMethod != null, "Unable to find the get method for property ArrayList.Item");    
            
            // Retrieve the ArrayList.Count property get method.
            PropertyInfo ArrayListSizeProperty = typeof(ArrayList).GetProperty( "Count" );
            Contract.Assert(ArrayListSizeProperty != null, "Unable to find the property ArrayList.Count");    
            MethodInfo ArrayListSizeGetMethod = ArrayListSizeProperty.GetGetMethod();
            Contract.Assert(ArrayListSizeGetMethod != null, "Unable to find the get method for property ArrayList.Count");    
            
            // Retrieve the ConnectionPoint.Unadvise() method.
            MethodInfo CPUnadviseMethod = typeof(IConnectionPoint).GetMethod( "Unadvise" );
            Contract.Assert(CPUnadviseMethod != null, "Unable to find the method ConnectionPoint.Unadvise()");    

            // Retrieve the Marshal.ReleaseComObject() method.
            MethodInfo ReleaseComObjectMethod = typeof(Marshal).GetMethod( "ReleaseComObject" );
            Contract.Assert(ReleaseComObjectMethod != null, "Unable to find the method Marshal.ReleaseComObject()");

            // Retrieve the Monitor.Enter() method.
            MethodInfo MonitorEnterMethod = typeof(Monitor).GetMethod("Enter", MonitorEnterParamTypes, null);
            Contract.Assert(MonitorEnterMethod != null, "Unable to find the method Monitor.Enter()");
            
            // Retrieve the Monitor.Exit() method.
            Type[] aParamTypes = new Type[1];
            aParamTypes[0] = typeof(Object);
            MethodInfo MonitorExitMethod = typeof(Monitor).GetMethod( "Exit", aParamTypes, null );
            Contract.Assert(MonitorExitMethod != null, "Unable to find the method Monitor.Exit()");
                        
            // Define the Finalize method itself.
            MethodBuilder Meth = OutputTypeBuilder.DefineMethod( "Finalize", MethodAttributes.Public | MethodAttributes.Virtual, null, null );
            
            ILGenerator il = Meth.GetILGenerator();
            
            // Declare the local variables.
            LocalBuilder ltNumSinkHelpers = il.DeclareLocal( typeof(Int32) );
            LocalBuilder ltSinkHelperCounter = il.DeclareLocal( typeof(Int32) );
            LocalBuilder ltCurrSinkHelper = il.DeclareLocal( SinkHelperClass );
            LocalBuilder ltLockTaken = il.DeclareLocal(typeof(bool));
                        
            // Generate the following code:
            //   try {
            il.BeginExceptionBlock();

            // Generate the following code:
            //   Monitor.Enter(this, ref lockTaken);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldloca_S, ltLockTaken);
            il.Emit(OpCodes.Call, MonitorEnterMethod);

            // Generate the labels.
            Label ForBeginLabel = il.DefineLabel();
            Label ReleaseComObjectLabel = il.DefineLabel();
            Label AfterReleaseComObjectLabel = il.DefineLabel();

            // Generate the following code:
            //   if ( m_IFooEventsCP == null ) goto AfterReleaseComObjectLabel;        
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbEventCP );
            il.Emit( OpCodes.Brfalse, AfterReleaseComObjectLabel );

            // Generate the following code:
            //   int NumEventHelpers = m_aIFooEventsHelpers.Count;
            //   int cEventHelpers = 0;
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbSinkHelper );
            il.Emit( OpCodes.Callvirt, ArrayListSizeGetMethod );
            il.Emit( OpCodes.Stloc, ltNumSinkHelpers );
            il.Emit( OpCodes.Ldc_I4, 0 );
            il.Emit( OpCodes.Stloc, ltSinkHelperCounter );
            
            // Generate the following code:
            //   if ( 0 >= NumEventHelpers ) goto ReleaseComObjectLabel;        
            il.Emit( OpCodes.Ldc_I4, 0 );
            il.Emit( OpCodes.Ldloc, ltNumSinkHelpers );
            il.Emit( OpCodes.Bge, ReleaseComObjectLabel );
            
            // Mark this as the beginning of the for loop's body.
            il.MarkLabel( ForBeginLabel );
            
            // Generate the following code:
            //   CurrentHelper = (IFooEvents_SinkHelper)m_aIFooEventsHelpers.Get( cEventHelpers );
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbSinkHelper );
            il.Emit( OpCodes.Ldloc, ltSinkHelperCounter );
            il.Emit( OpCodes.Callvirt, ArrayListItemGetMethod );
            il.Emit( OpCodes.Castclass, SinkHelperClass );
            il.Emit( OpCodes.Stloc, ltCurrSinkHelper );
            
            // Generate the following code:
            //   m_IFooEventsCP.Unadvise( CurrentHelper.m_dwCookie );
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbEventCP );
            il.Emit( OpCodes.Ldloc, ltCurrSinkHelper );
            il.Emit( OpCodes.Ldfld, CookieField );
            il.Emit( OpCodes.Callvirt, CPUnadviseMethod );
            
            // Generate the following code:
            //   cEventHelpers++;
            il.Emit( OpCodes.Ldloc, ltSinkHelperCounter );
            il.Emit( OpCodes.Ldc_I4, 1 );
            il.Emit( OpCodes.Add );
            il.Emit( OpCodes.Stloc, ltSinkHelperCounter );
            
            // Generate the following code:
            //   if ( cEventHelpers < NumEventHelpers ) goto ForBeginLabel;
            il.Emit( OpCodes.Ldloc, ltSinkHelperCounter );
            il.Emit( OpCodes.Ldloc, ltNumSinkHelpers );
            il.Emit( OpCodes.Blt, ForBeginLabel );
            
            // Mark this as the end of the for loop's body.
            il.MarkLabel( ReleaseComObjectLabel );           

            // Generate the following code:
            //   Marshal.ReleaseComObject(m_IFooEventsCP);
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbEventCP );
            il.Emit( OpCodes.Call, ReleaseComObjectMethod );            
            il.Emit( OpCodes.Pop );

            // Mark this as the end of the for loop's body.
            il.MarkLabel( AfterReleaseComObjectLabel );           
            
            // Generate the following code:
            //   } catch {
            il.BeginCatchBlock(typeof(System.Exception));
            il.Emit( OpCodes.Pop );

            // Generate the following code:
            //   } finally {
            il.BeginFinallyBlock();

            // Generate the following code:
            //   if (lockTaken)
            //      Monitor.Exit(this);
            Label skipExit = il.DefineLabel();
            il.Emit(OpCodes.Ldloc, ltLockTaken);
            il.Emit(OpCodes.Brfalse_S, skipExit);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Call, MonitorExitMethod);
            il.MarkLabel(skipExit);

            // Generate the following code:
            //   }
            il.EndExceptionBlock();            
            
            // Generate the return opcode.
            il.Emit( OpCodes.Ret );
            
            return Meth;
        }   
        
        private void DefineDisposeMethod( TypeBuilder OutputTypeBuilder, MethodBuilder FinalizeMethod )
        {
            // Retrieve the method info for GC.SuppressFinalize().
            MethodInfo SuppressFinalizeMethod = typeof(GC).GetMethod("SuppressFinalize");
            Contract.Assert(SuppressFinalizeMethod != null, "Unable to find the GC.SuppressFinalize");    
            
            // Define the Finalize method itself.
            MethodBuilder Meth = OutputTypeBuilder.DefineMethod( "Dispose", MethodAttributes.Public | MethodAttributes.Virtual, null, null );
            
            ILGenerator il = Meth.GetILGenerator();
            
            // Generate the following code:
            //   Finalize()
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Callvirt, FinalizeMethod );
            
            // Generate the following code:
            //   GC.SuppressFinalize()
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Call, SuppressFinalizeMethod );    
            
            // Generate the return opcode.
            il.Emit( OpCodes.Ret );   
        }      

        private ModuleBuilder m_OutputModule;
        private String m_strDestTypeName;
        private Type m_EventItfType;
        private Type m_SrcItfType;
        private Type m_SinkHelperType;
    }
}
