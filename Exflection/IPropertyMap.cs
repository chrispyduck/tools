using System;

namespace Exflection
{
    /// <summary>
    /// Provides methods for getting and setting the value of a single instance-level property of a given type. The value may be set using managed types or invariant strings.
    /// </summary>
    public interface IPropertyMap : IStringPropertyMap
    {
        /// <summary>
        /// The name of the property represented by this <see cref="IPropertyMap"/>
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the managed value of this <see cref="IPropertyMap"/> from the given <paramref name="object"/>
        /// </summary>
        /// <param name="object">An object containing the property value to be retrieved</param>
        /// <returns>The value of the property for the given object</returns>
        object GetValue(object @object);

        /// <summary>
        /// Sets the managed value of this <see cref="IPropertyMap"/> to the given <paramref name="value"/>
        /// </summary>
        /// <param name="object">An object containing the property to be assigned</param>
        /// <param name="value">The value to be assigned to the property represented by this <see cref="IPropertyMap"/> of the given <paramref name="object"/></param>
        void SetValue(object @object, object value);
    }

    /// <summary>
    /// Provides methods for getting and setting the value of a single instance-level property of a given type. The value may be set using managed types or invariant strings.
    /// </summary>
    /// <typeparam name="TObj">The type of the object whose instance contains the represented property</typeparam>
    public interface IPropertyMap<in TObj> : IPropertyMap, IStringPropertyMap<TObj>
    {
        /// <summary>
        /// Sets the managed value of this <see cref="IPropertyMap"/> to the given <paramref name="value"/>
        /// </summary>
        /// <param name="object">An object containing the property to be assigned</param>
        /// <param name="value">The value to be assigned to the property represented by this <see cref="IPropertyMap"/> of the given <paramref name="object"/></param>
        void SetValue(TObj @object, object value);

        /// <summary>
        /// Gets the managed value of this <see cref="IPropertyMap"/> from the given <paramref name="object"/>
        /// </summary>
        /// <param name="object">An object containing the property value to be retrieved</param>
        /// <returns>The value of the property for the given object, boxed as an <see cref="object"/></returns>
        object GetValueBoxed(TObj @object);
    }

    /// <summary>
    /// Provides methods for getting and setting the value of a single instance-level property of a given type. The value may be set using managed types or invariant strings.
    /// </summary>
    /// <typeparam name="TObj">The type of the object whose instance contains the represented property</typeparam>
    /// <typeparam name="TProp">The type of the property value</typeparam>
    public interface IPropertyMap<TObj, TProp> : IEquatable<PropertyMap<TObj, TProp>>, IStringPropertyMap<TObj, TProp>, IPropertyMap<TObj>
    {
        /// <summary>
        /// Gets the managed value of this <see cref="IPropertyMap"/> from the given <paramref name="object"/>
        /// </summary>
        /// <param name="object">An object containing the property value to be retrieved</param>
        /// <returns>The value of the property for the given object</returns>
        TProp GetValue(TObj @object);

        /// <summary>
        /// Sets the managed value of this <see cref="IPropertyMap"/> to the given <paramref name="value"/>
        /// </summary>
        /// <param name="object">An object containing the property to be assigned</param>
        /// <param name="value">The value to be assigned to the property represented by this <see cref="IPropertyMap"/> of the given <paramref name="object"/></param>
        void SetValue(TObj @object, TProp value);
    }
}