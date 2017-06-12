using System;
using System.Runtime.Serialization;

namespace Exflection
{
    /// <summary>
    /// Represents an exception raised while configuring or executing a <see cref="PropertyMap"/>
    /// </summary>
    [Serializable]
    public class PropertyMappingException : Exception
    {
        /// <summary>
        /// Creates a new instance of <see cref="PropertyMappingException"/>
        /// </summary>
        public PropertyMappingException()
        { }

        /// <inheritdoc/>
        public PropertyMappingException(string message) 
            : base(message)
        { }

        /// <inheritdoc/>
        public PropertyMappingException(string message, Exception inner)
            : base(message, inner)
        { }

        /// <inheritdoc/>
        /// <paramref name="map">The <see cref="IPropertyMap"/> with which this exception is associated</paramref>
        public PropertyMappingException(IPropertyMap map, string message, Exception inner)
            : this(map.Name, message, inner)
        { }

        /// <inheritdoc/>
        /// <paramref name="mapName">The name of the property map with which this exception is associated</paramref>
        public PropertyMappingException(string mapName, string message, Exception inner)
            : base(message, inner)
        {
            this.MapName = mapName;
        }

        /// <inheritdoc/>
        protected PropertyMappingException(
            SerializationInfo info,
            StreamingContext context) 
            : base(info, context)
        {
            info.GetString("MapName");
        }

        /// <summary>
        /// The name of the <see cref="PropertyMap"/> with which this exception is associated
        /// </summary>
        public string MapName { get; private set; }

        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("MapName", this.MapName);
        }
    }
}
