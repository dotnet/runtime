// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.TCEAdapterGen {
    using System.Runtime.InteropServices;
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Collections;
    using System.Diagnostics.Contracts;
    internal class EventSinkHelperWriter
    {
        public static readonly String GeneratedTypeNamePostfix = "_SinkHelper";
        
        public EventSinkHelperWriter( ModuleBuilder OutputModule, Type InputType, Type EventItfType )
        {
            m_InputType = InputType;
            m_OutputModule = OutputModule;
            m_EventItfType = EventItfType;
        }
        
        public Type Perform()
        {
            // Create the output Type.
            Type[] aInterfaces = new Type[1];
            aInterfaces[0] = m_InputType;
            String strFullName = null;
            String strNameSpace = NameSpaceExtractor.ExtractNameSpace( m_EventItfType.FullName );
            
            if (strNameSpace != "")
                strFullName = strNameSpace  + ".";
            
            strFullName += m_InputType.Name + GeneratedTypeNamePostfix;
            TypeBuilder OutputTypeBuilder = TCEAdapterGenerator.DefineUniqueType( 
                                                             strFullName,
                                                             TypeAttributes.Sealed | TypeAttributes.Public, 
                                                             null,
                                                             aInterfaces,
                                                             m_OutputModule
                                                            );
            // Hide the _SinkProvider interface
            TCEAdapterGenerator.SetHiddenAttribute(OutputTypeBuilder);

            // Set the class interface to none.
            TCEAdapterGenerator.SetClassInterfaceTypeToNone(OutputTypeBuilder);

            // Retrieve the property methods on the input interface and give them a dummy implementation.
            MethodInfo[] pMethods = TCEAdapterGenerator.GetPropertyMethods(m_InputType);
            foreach (MethodInfo method in pMethods)
            {
                DefineBlankMethod(OutputTypeBuilder, method);
            }

            // Retrieve the non-property methods on the input interface.
            MethodInfo[] aMethods = TCEAdapterGenerator.GetNonPropertyMethods(m_InputType);
    
            // Allocate an array to contain the delegate fields.
            FieldBuilder[] afbDelegates = new FieldBuilder[aMethods.Length];   
            // Process all the methods on the input interface.
            for ( int cMethods = 0; cMethods < aMethods.Length; cMethods++ )
            {
                if ( m_InputType == aMethods[cMethods].DeclaringType )
                {
                    // Retrieve the delegate type from the add_XXX method.
                    MethodInfo AddMeth = m_EventItfType.GetMethod( "add_" + aMethods[cMethods].Name );
                    ParameterInfo[] aParams = AddMeth.GetParameters();
                    Contract.Assert(aParams.Length == 1, "All event interface methods must take a single delegate derived type and have a void return type");    
                    Type DelegateCls = aParams[0].ParameterType;

                    // Define the delegate instance field.
                    afbDelegates[cMethods] = OutputTypeBuilder.DefineField( 
                                                  "m_" + aMethods[cMethods].Name + "Delegate", 
                                                  DelegateCls,
                                                  FieldAttributes.Public 
                                                 );
                    
                    // Define the event method itself.
                    DefineEventMethod( OutputTypeBuilder, aMethods[cMethods], DelegateCls, afbDelegates[cMethods] );                
                }
            }

            // Create the cookie field.
            FieldBuilder fbCookie = OutputTypeBuilder.DefineField( 
                                                  "m_dwCookie", 
                                                  typeof(Int32), 
                                                  FieldAttributes.Public 
                                                 );
    
            // Define the constructor.      
            DefineConstructor( OutputTypeBuilder, fbCookie, afbDelegates );
    
            return OutputTypeBuilder.CreateType();
        }

        private void DefineBlankMethod(TypeBuilder OutputTypeBuilder, MethodInfo Method)
        {
            ParameterInfo[] PIs = Method.GetParameters();
            Type[] parameters = new Type[PIs.Length];
            for (int i=0; i < PIs.Length; i++)
            {
                parameters[i] = PIs[i].ParameterType;
            }
           
            MethodBuilder Meth = OutputTypeBuilder.DefineMethod(Method.Name,
                                                                Method.Attributes & ~MethodAttributes.Abstract,
                                                                Method.CallingConvention,
                                                                Method.ReturnType,
                                                                parameters);

            ILGenerator il = Meth.GetILGenerator();

            AddReturn(Method.ReturnType, il, Meth);
            
            il.Emit(OpCodes.Ret);
        }

        private void DefineEventMethod( TypeBuilder OutputTypeBuilder, MethodInfo Method, Type DelegateCls, FieldBuilder fbDelegate )
        {
            // Retrieve the method info for the invoke method on the delegate.
            MethodInfo DelegateInvokeMethod = DelegateCls.GetMethod( "Invoke" );
            Contract.Assert(DelegateInvokeMethod != null, "Unable to find method Delegate.Invoke()");    
    
            // Retrieve the return type.
            Type ReturnType = Method.ReturnType;
        
            // Define the actual event method.
            ParameterInfo[] paramInfos = Method.GetParameters();
            Type[]          parameterTypes;
            if (paramInfos != null)
            {
                parameterTypes = new Type[paramInfos.Length];
                for (int i = 0; i < paramInfos.Length; i++)
                {
                    parameterTypes[i] = paramInfos[i].ParameterType;
                }
            }
            else
                parameterTypes = null;

            MethodAttributes attr = MethodAttributes.Public | MethodAttributes.Virtual;
            MethodBuilder Meth = OutputTypeBuilder.DefineMethod( Method.Name, 
                                                                 attr, 
                                                                 CallingConventions.Standard,
                                                                 ReturnType,
                                                                 parameterTypes);

            // We explicitly do not specify parameter name and attributes since this Type
            // is not meant to be exposed to the user. It is only used internally to do the
            // connection point to TCE mapping.
            
            ILGenerator il = Meth.GetILGenerator();
                        
            // Create the exit branch.
            Label ExitLabel = il.DefineLabel();
        
            // Generate the code that verifies that the delegate is not null.
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbDelegate );    
            il.Emit( OpCodes.Brfalse, ExitLabel );
                            
            // The delegate is not NULL so we need to invoke it.
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldfld, fbDelegate );
        
            // Generate the code to load the arguments before we call invoke.
            ParameterInfo[] aParams = Method.GetParameters();
            for ( int cParams = 0; cParams < aParams.Length; cParams++ )
            {
                il.Emit( OpCodes.Ldarg, (short)(cParams + 1) );
            }
        
            // Generate a tail call to invoke. This will cause the callvirt to return 
            // directly to the caller of the current method instead of actually coming
            // back to the current method and returning. This will cause the value returned
            // from the call to the COM server to be returned to the caller of this method.
                
            il.Emit( OpCodes.Callvirt, DelegateInvokeMethod );
            il.Emit( OpCodes.Ret );
        
            // This is the label that will be jumped to if no delegate is present.
            il.MarkLabel( ExitLabel );
            
            AddReturn(ReturnType, il, Meth);
            
            il.Emit( OpCodes.Ret );
        
        }

        private void AddReturn(Type ReturnType, ILGenerator il, MethodBuilder Meth)
        {
            // Place a dummy return value on the stack before we return.
            if ( ReturnType == typeof(void) )
            {
                // There is nothing to place on the stack.
            }
            else if ( ReturnType.IsPrimitive )
            {
                switch (System.Type.GetTypeCode(ReturnType)) 
                {
                    case TypeCode.Boolean:
                    case TypeCode.Char:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        il.Emit( OpCodes.Ldc_I4_0 );
                        break;
                        
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        il.Emit( OpCodes.Ldc_I4_0 );
                        il.Emit( OpCodes.Conv_I8 );
                        break;
                        
                    case TypeCode.Single:
                        il.Emit( OpCodes.Ldc_R4, 0 );
                        break;
                    
                    case TypeCode.Double:
                        il.Emit( OpCodes.Ldc_R4, 0 );
                        il.Emit( OpCodes.Conv_R8 );
                        break;
                    
                    default:
                        // "TypeCode" does not include IntPtr, so special case it.
                        if ( ReturnType == typeof(IntPtr) )
                            il.Emit( OpCodes.Ldc_I4_0 );
                        else
                            Contract.Assert(false, "Unexpected type for Primitive type.");    
                        break;
                }
            }
            else if ( ReturnType.IsValueType )
            {
                // Allocate stack space for the return value type.  Zero-init.
                Meth.InitLocals = true;
                LocalBuilder ltRetVal = il.DeclareLocal( ReturnType );        
                
                // Load the value class on the stack.
                il.Emit( OpCodes.Ldloc_S, ltRetVal );
                
            }
            else
            {
                // The return type is a normal type.
                il.Emit( OpCodes.Ldnull );
            }
        }
        
        private void DefineConstructor( TypeBuilder OutputTypeBuilder, FieldBuilder fbCookie, FieldBuilder[] afbDelegates )
        {
            // Retrieve the constructor info for the base classe's constructor.
            ConstructorInfo DefaultBaseClsCons = typeof(Object).GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, Array.Empty<Type>(), null );
            Contract.Assert(DefaultBaseClsCons != null, "Unable to find the constructor for class " + m_InputType.Name);    
        
            // Define the default constructor.
            MethodBuilder Cons = OutputTypeBuilder.DefineMethod( ".ctor", 
                                                                 MethodAttributes.Assembly | MethodAttributes.SpecialName, 
                                                                 CallingConventions.Standard,
                                                                 null,
                                                                 null);

            ILGenerator il = Cons.GetILGenerator();
    
            // Generate the code to call the constructor of the base class.
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Call, DefaultBaseClsCons );
    
            // Generate the code to set the cookie field to 0.
            il.Emit( OpCodes.Ldarg, (short)0 );
            il.Emit( OpCodes.Ldc_I4, 0 );
            il.Emit( OpCodes.Stfld, fbCookie );
    
            // Generate the code to set all the delegates to NULL.
            for ( int cDelegates = 0; cDelegates < afbDelegates.Length; cDelegates++ )
            {
                if (afbDelegates[cDelegates] != null)
                {
                    il.Emit( OpCodes.Ldarg,(short)0 );
                    il.Emit( OpCodes.Ldnull );
                    il.Emit( OpCodes.Stfld, afbDelegates[cDelegates] );
                }
            }
    
            // Emit the return opcode.
            il.Emit( OpCodes.Ret );
    
        }

        private Type m_InputType;  
        private Type m_EventItfType;
        private ModuleBuilder m_OutputModule;
    }
}
