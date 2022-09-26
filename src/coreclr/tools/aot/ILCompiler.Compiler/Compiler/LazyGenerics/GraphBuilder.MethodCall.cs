// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    internal static partial class LazyGenericsSupport
    {
        private sealed partial class GraphBuilder
        {
            /// <summary>
            /// Found a method call inside a method body. Method calls may bind generic parameters of the target method. If so,
            /// we have to record that fact.
            /// </summary>
            private void ProcessMethodCall(MethodDesc target, Instantiation typeContext, Instantiation methodContext)
            {
                if (!target.HasInstantiation)
                    return;

                //
                // Collect all the generic parameters that have to be bound. There are two (non-mutually-exclusive) cases to consider:
                //
                //    - The target method is declared on a generic type.
                //    - The target method is itself generic.
                //
                // We already took care of any generic type parameters in ProcessTypeReference. So we only need to handle
                // any generic method parameters here.
                //

                Instantiation genericTypeParameters = target.GetTypicalMethodDefinition().Instantiation;
                Instantiation genericTypeArguments = target.Instantiation;

                Debug.Assert(genericTypeParameters.Length == genericTypeArguments.Length);

                // We've now collected all the generic parameters for the target and the generic arguments that the caller is binding them to.
                // Recursively walk each generic argument to see if we're passing along one of the caller's generic parameters, and if so,
                // are we embedding it into a more complex type.
                for (int i = 0; i < genericTypeParameters.Length; i++)
                {
                    ForEachEmbeddedGenericFormal(
                        genericTypeArguments[i],
                        typeContext,
                        methodContext,
                        delegate(EcmaGenericParameter embedded, bool isProperEmbedding)
                        {
                            // If we got here, we found a method with generic arity (either from itself or its declaring type or both)
                            // that invokes a generic method. The caller is binding one of the target's generic formals to a type expression
                            // involving one of the caller's own formals.
                            //
                            // e.g.
                            //
                            //  void Caller<G>()
                            //  {
                            //      Target<IList<G>>();
                            //      return;
                            //  }
                            //
                            RecordBinding((EcmaGenericParameter)genericTypeParameters[i], embedded, isProperEmbedding);
                        }
                    );
                }
            }
        }
    }
}
