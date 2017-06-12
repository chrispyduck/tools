namespace Exflection
{
    /// <summary>
    /// Provides methods for getting and setting the value of a single instance-level property of a given type using invariant strings.
    /// <seealso cref="StringParser{T}"/>
    /// </summary>
    public interface IStringPropertyMap
    {
        void SetFromInvariantString(object @object, string input);
        void SetFromInvariantString(object @object, string input, object @default);
        string GetAsInvariantString(object @object);
    }

    /// <summary>
    /// Provides methods for getting and setting the value of a single instance-level property of a given type using invariant strings. Value conversion is provided by <see cref="StringParser{TObj}"/>.
    /// </summary>
    /// <typeparam name="TObj">The type of the object whose instance contains the represented property</typeparam>
    public interface IStringPropertyMap<in TObj> : IStringPropertyMap
    {
        void SetFromInvariantString(TObj @object, string input);
        void SetFromInvariantString(TObj @object, string input, object @default);
        string GetAsInvariantString(TObj @object);
    }

    /// <summary>
    /// Provides methods for getting and setting the value of a single instance-level property of a given type using invariant strings. Value conversion is provided by <see cref="StringParser{TObj}"/>.
    /// </summary>
    /// <typeparam name="TObj">The type of the object whose instance contains the represented property</typeparam>
    /// <typeparam name="TProp">The type of the property value</typeparam>
    public interface IStringPropertyMap<in TObj, in TProp> : IStringPropertyMap<TObj>
    {
        void SetFromInvariantString(TObj @object, string input, TProp @default);
    }
}
