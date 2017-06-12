using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace Exflection
{
    /// <summary>
    /// Converts strings to and from the CLR type <typeparamref name="T"/>. Supported types include:
    /// <list type="bullet">
    ///     <item>
    ///         <term>Any types with a globally registered <see cref="TypeConverter"/></term>
    ///     </item>
    ///     <item>
    ///         <term>Any types having a public static <code><typeparamref name="T"/> Parse(<see cref="string"/>)</code></term> method
    ///     </item>
    ///     <item>
    ///         <term><see cref="FileSystemInfo"/></term>
    ///         <description>Base class for <see cref="FileInfo"/> and <see cref="DirectoryInfo"/></description>
    ///     </item>
    /// </list>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class StringParser<T>
    {
        static StringParser()
        {
            // try type descriptor
            var d = TypeDescriptor.GetConverter(typeof(T));
            if (d.CanConvertFrom(typeof(string)) && d.CanConvertTo(typeof(string)))
            {
                _ConvertFromString = text => (T)d.ConvertFromInvariantString(text);
                _ConvertToString = value => d.ConvertToInvariantString(value);
                return;
            }

            // look for a static Parse(string) method
            var parseMethod = typeof(T).GetMethod("Parse",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);
            if (parseMethod != null)
            {
                // we assume that Parse and ToString are complementary. this may not be valid for all types.
                _ConvertFromString = (Func<string, T>)parseMethod.CreateDelegate(typeof(Func<string, T>));
                _ConvertToString = value => value.ToString();
                return;
            }

            // see if it's a type that requires special logic
            if (typeof (FileSystemInfo).IsAssignableFrom(typeof (T)))
            {
                // parse environment variables from path info
                // todo: persist the pre-expanded string so it can be used when saving
                _ConvertFromString = s => (T)Activator.CreateInstance(typeof (T), Environment.ExpandEnvironmentVariables(s));
                _ConvertToString = info =>
                {
                    var fsi = info as FileSystemInfo;
                    return fsi?.FullName;
                };
                return;
            }

            throw new NotSupportedException("No conversion logic can be found for type " + typeof(T).FullName);
        }

        private static readonly Func<string, T> _ConvertFromString;
        private static readonly Func<T, string> _ConvertToString;

        public static T ConvertFromInvariantString(string input)
        {
            return _ConvertFromString(input);
        }

        public static string ConvertToInvariantString(T input)
        {
            return _ConvertToString(input);
        }
    }
}
