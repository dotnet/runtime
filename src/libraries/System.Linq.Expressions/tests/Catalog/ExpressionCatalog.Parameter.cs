// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> Parameter()
        {
            // Shadowing with lambdas
            {
                var p = Expression.Parameter(typeof(int));

                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Parameter,
                    Expression.Invoke(
                        Expression.Invoke(
                            Expression.Lambda(
                                Expression.Lambda(p, p),
                                p
                            ),
                            Expression.Constant(1)
                        ),
                        Expression.Constant(2)
                    )
                );
            }

            // Shadowing with lambdas and blocks
            {
                var p = Expression.Parameter(typeof(int));

                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Parameter,
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(
                            p,
                            Expression.Constant(1)
                        ),
                        Expression.Invoke(
                            Expression.Lambda(p, p),
                            Expression.Constant(2)
                        )
                    )
                );
            }

            // Shadowing with blocks
            {
                var p = Expression.Parameter(typeof(int));

                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Parameter,
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(
                            p,
                            Expression.Constant(1)
                        ),
                        Expression.Block(
                            new[] { p },
                            Expression.Assign(
                                p,
                                Expression.Constant(2)
                            )
                        ),
                        p
                    )
                );
            }

            // Closures with lambdas
            {
                var p = Expression.Parameter(typeof(int));

                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Parameter,
                    Expression.Invoke(
                        Expression.Invoke(
                            Expression.Lambda(
                                Expression.Lambda(p),
                                p
                            ),
                            Expression.Constant(1)
                        )
                    )
                );
            }

            // Closures with lambdas and blocks
            {
                var p = Expression.Parameter(typeof(int));

                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Parameter,
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(
                            p,
                            Expression.Constant(1)
                        ),
                        Expression.Invoke(
                            Expression.Lambda(p)
                        )
                    )
                );
            }
        }
    }
}