using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Exflection
{
    public static class ExpressionExtensions
    {
        #region Parameter Replacement
        /// <summary>
        /// Replaces all <see cref="ParameterExpression"/> instances in the given expression tree with the provided <see cref="ParameterExpression"/>
        /// </summary>
        public static Expression<T> ReplaceParameter<T>(this Expression<T> expr, ParameterExpression parameter,
            Type parameterType)
        {
            var newBody = expr.Body.ReplaceParameter(parameter, parameterType);
            var parameters = expr.Parameters.ToArray();
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Type == parameterType)
                    parameters[i] = parameter;
            }
            return Expression.Lambda<T>(newBody, parameters);
        }

        /// <summary>
        /// Replaces all <see cref="ParameterExpression"/> instances in the given expression tree 
        /// of <see cref="parameterType"/> type with the provided <see cref="ParameterExpression"/>
        /// </summary>
        public static Expression ReplaceParameter(this Expression expr, ParameterExpression parameter,
            Type parameterType)
        {
            var v = new ParameterVisitor(parameterType, parameter);
            return v.Visit(expr);
        }

        class ParameterVisitor : ExpressionVisitor
        {
            public ParameterVisitor(Type type, ParameterExpression replacement)
            {
                this.type = type;
                this.replacement = replacement;
            }

            private readonly Type type;
            private readonly ParameterExpression replacement;

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node.Type == this.type)
                    return this.replacement;
                return base.VisitParameter(node);
            }
        }
        #endregion

        /// <summary>
        /// Combines one or more unique expression trees using the binary operator <paramref name="type"/>
        /// </summary>
        /// <typeparam name="T"><inheritdoc cref="Expression{T}"/></typeparam>
        /// <param name="expressions">A collection of expressions to be merged</param>
        /// <param name="type">The binary operator used to merge the expresssions</param>
        /// <returns>A single expression tree consisting of all the input expressions</returns>
        public static Expression<T> MergeExpressions<T>(this IEnumerable<Expression<T>> expressions, ExpressionType type = ExpressionType.And)
        {
            if (expressions == null)
                throw new ArgumentNullException("expressions");
            Expression body = null;
            ParameterExpression param = null;
            foreach (var expr in expressions)
            {
                if (body == null)
                {
                    body = expr.Body;
                    param = expr.Parameters[0];
                }
                else
                    body = Expression.MakeBinary(type, body, expr.Body);
            }
            if (body == null)
                throw new ArgumentException("expressions must contain at least one expression", "expressions");

            return Expression.Lambda<T>(body, param);
        }
    }
}
