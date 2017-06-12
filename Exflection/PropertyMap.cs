using System;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

namespace Exflection
{
    /// <inheritdoc/>
    public class PropertyMap<TObj, TProp> : IPropertyMap<TObj, TProp>
    {
        #region Constructors, Fields, and Initialization Logic
        /// <summary>
        /// Creates a <see cref="PropertyMap"/> instance from the given <paramref name="getter"/> expression tree
        /// </summary>
        /// <exception cref="PropertyMappingException">Unexpected error generating getter and setter delegates.</exception>
        /// <exception cref="ArgumentException">The provided expression tree must have a single parameter and must return the value of an instance property</exception>
        public PropertyMap([NotNull] Expression<Func<TObj, TProp>> getter)
        {
            if (getter.Parameters.Count != 1
                || (this.member = getter.Body as MemberExpression) == null)
                throw new ArgumentException("The provided expression tree must have a single parameter and must return the value of an instance property", "getter");
            this.objParameter = getter.Parameters[0];

            try
            {
                // getter needs no transformation
                this.getter = getter.Compile();

                // build setter from getter
                this.valueParameter = Expression.Parameter(typeof (TProp), "value");
                var setterExpression = Expression.Lambda<Action<TObj, TProp>>(
                    Expression.Assign(
                        this.member,
                        valueParameter
                        ),
                    this.objParameter,
                    this.valueParameter);
                this.setter = setterExpression.Compile();
                this.Name = this.member.Member.Name;
            }
            catch (Exception e)
            {
                throw new PropertyMappingException(this.member.Member.Name, "Failed to initialize PropertyMap", e);
            }
        }

        /// <summary>
        /// Creates a <see cref="PropertyMap"/> instance from the given <see cref="PropertyInfo"/> instance
        /// </summary>
        /// <exception cref="PropertyMappingException">Unexpected error generating getter and setter delegates.</exception>
        public PropertyMap([NotNull] PropertyInfo property)
            : this(MakeGetterFromPropertyInfo(property))
        { }

        private static Expression<Func<TObj, TProp>> MakeGetterFromPropertyInfo(PropertyInfo property)
        {
            var parameter = Expression.Parameter(typeof(TObj), typeof(TObj).Name);
            var member = Expression.Property(parameter, property);
            return Expression.Lambda<Func<TObj, TProp>>(member, parameter);
        }

        private readonly MemberExpression member;
        private readonly ParameterExpression objParameter;
        private readonly ParameterExpression valueParameter;

        private readonly Func<TObj, TProp> getter;
        private readonly Action<TObj, TProp> setter;

        [NotNull]
        public string Name { get; private set; }
        #endregion

        #region Equality Operators and Methods
        public override int GetHashCode()
        {
            return member?.GetHashCode() ?? 0;
        }

        public bool Equals(PropertyMap<TObj, TProp> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(member.Member, other.member.Member);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PropertyMap<TObj, TProp>)obj);
        }

        public static bool operator ==(PropertyMap<TObj, TProp> left, PropertyMap<TObj, TProp> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PropertyMap<TObj, TProp> left, PropertyMap<TObj, TProp> right)
        {
            return !Equals(left, right);
        }
        #endregion

        #region Public API

        /// <inheritdoc/>
        /// <exception cref="PropertyMappingException">Error getting property value</exception>
        public TProp GetValue(TObj @object)
        {
            try
            {
                return this.getter(@object);
            }
            catch (Exception e)
            {
                throw new PropertyMappingException(this, "Error getting value of property '" + this.Name + "'", e);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="PropertyMappingException">Error setting property value</exception>
        public void SetValue(TObj @object, TProp value)
        {
            try
            {
                this.setter(@object, value);
            }
            catch (Exception e)
            {
                throw new PropertyMappingException(this, "Error setting value of property '" + this.Name + "'", e);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="PropertyMappingException">Error getting property value</exception>
        string IStringPropertyMap.GetAsInvariantString(object @object)
        {
            return this.GetAsInvariantString((TObj) @object);
        }

        /// <inheritdoc/>
        /// <exception cref="PropertyMappingException">Error getting property value</exception>
        public string GetAsInvariantString(TObj @object)
        {
            var val = this.GetValue(@object);
            try
            {
                return StringParser<TProp>.ConvertToInvariantString(val);
            }
            catch (Exception e)
            {
                throw new PropertyMappingException(this, "Failed to convert value of property '" + this.Name + "' to invariant string", e);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="PropertyMappingException">Error setting property value</exception>
        void IStringPropertyMap.SetFromInvariantString(object @object, string input, object @default)
        {
            this.SetFromInvariantString((TObj)@object, input, (TProp)@default);
        }

        /// <inheritdoc/>
        /// <exception cref="PropertyMappingException">Error setting property value</exception>
        void IStringPropertyMap<TObj>.SetFromInvariantString(TObj @object, string input, object @default)
        {
            this.SetFromInvariantString(@object, input, (TProp)@default);
        }

        /// <inheritdoc/>
        /// <exception cref="PropertyMappingException">Error setting property value</exception>
        void IStringPropertyMap<TObj>.SetFromInvariantString(TObj @object, string input)
        {
            this.SetFromInvariantString(@object, input, default(TProp), true);
        }

        /// <inheritdoc/>
        /// <exception cref="PropertyMappingException">Error setting property value</exception>
        void IStringPropertyMap.SetFromInvariantString(object @object, string input)
        {
            this.SetFromInvariantString((TObj) @object, input, default(TProp), true);
        }

        /// <inheritdoc/>
        /// <exception cref="PropertyMappingException">Error setting property value</exception>
        public void SetFromInvariantString(TObj @object, string input, TProp @default)
        {
            this.SetFromInvariantString(@object, input, @default, false);
        }

        /// <inheritdoc/>
        /// <exception cref="PropertyMappingException">Error setting property value</exception>
        private void SetFromInvariantString(TObj @object, string input, TProp @default, bool throwOnFailure)
        {
            TProp value;
            try
            {
                value = StringParser<TProp>.ConvertFromInvariantString(input);
            }
            catch (Exception e)
            {
                if (throwOnFailure)
                    throw new PropertyMappingException(this, "Failed to convert input string '" + input + "' to type '" + typeof(TProp) + "' for property '" + this.Name + "'", e);
                value = @default;
            }

            this.SetValue(@object, value);
        }

        /// <inheritdoc/>
        /// <exception cref="PropertyMappingException">Error getting property value</exception>
        object IPropertyMap.GetValue(object @object)
        {
            return this.GetValue((TObj) @object);
        }

        /// <inheritdoc/>
        /// <exception cref="PropertyMappingException">Error setting property value</exception>
        void IPropertyMap.SetValue(object @object, object value)
        {
            this.SetValue((TObj)@object, (TProp)value);
        }

        /// <inheritdoc/>
        /// <exception cref="PropertyMappingException">Error setting property value</exception>
        void IPropertyMap<TObj>.SetValue(TObj @object, object value)
        {
            this.SetValue(@object, (TProp)value);
        }

        /// <inheritdoc/>
        /// <exception cref="PropertyMappingException">Error getting property value</exception>
        object IPropertyMap<TObj>.GetValueBoxed(TObj @object)
        {
            return this.GetValue(@object);
        }
        #endregion
    }

    public static class PropertyMap
    {
        /// <summary>
        /// Creates a <see cref="IPropertyMap"/> from a given <see cref="PropertyInfo"/> instance
        /// </summary>
        [NotNull]
        public static IPropertyMap CreatePropertyMap([NotNull] this PropertyInfo propertyInfo)
        {
            var mapType = typeof(PropertyMap<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
            return (IPropertyMap)Activator.CreateInstance(mapType, propertyInfo);
        }
    }
}
