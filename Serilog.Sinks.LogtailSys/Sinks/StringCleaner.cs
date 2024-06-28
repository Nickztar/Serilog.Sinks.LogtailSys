using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Serilog.Sinks.Logtail
{
    public class StringCleaner(string source)
    {
        private StringBuilder _builder = new(source);

        public StringCleaner WithTrimmed(char toTrim)
        {
            if (_builder.Length != 0 && _builder[0] == toTrim) 
                _builder.Remove(0, 1);
            if (_builder.Length != 0 && _builder[_builder.Length - 1] == toTrim)
                _builder.Remove(_builder.Length - 1, 1);
            return this;
        }
        
        public StringCleaner WithUnescapeQuotes()
        {
            _builder.Replace(@"\""", @"""");
            return this;
        }
        
        public StringCleaner WithEscapedChar(char toEscape)
        {
            return WithEscapedChars(toEscape);
        }
        
        public StringCleaner WithEscapedChars(params char[] illegalChars)
        {
#if NETSTANDARD2_0
            var toEscape = new HashSet<char>(illegalChars);
#else
            var toEscape = illegalChars.ToHashSet();
#endif
            return WithEscapedChars(toEscape);
        }
    
        public StringCleaner WithEscapedChars(HashSet<char> toEscape)
        {
            var newBuilder = new StringBuilder(_builder.Length);
            for (var i = 0; i < _builder.Length; i++)
            {
                var c = _builder[i];
                if (!toEscape.Contains(c))
                {
                    newBuilder.Append(c);
                    continue;
                }
                newBuilder.Append('\\');
                newBuilder.Append(c);
            }
            
            _builder = newBuilder;
            return this;
        }
        
        /// <summary>
        /// Truncates the string so that it is no longer than the specified number of characters.
        /// If the truncated string ends with a space, it will be removed
        /// </summary>
        /// <param name="maxLength">Maximum string length before truncation will occur</param>
        /// <returns>StringCleaner</returns>
        public StringCleaner WithMaxLength(int maxLength)
        {
            if (_builder.Length <= maxLength)
                return this;
            _builder.Remove(maxLength, _builder.Length - maxLength);
            if (_builder.Length != 0 && _builder[_builder.Length - 1] == ' ')
                _builder.Remove(_builder.Length - 1, 1);
            return this;
        }
        
        public StringCleaner WithAsciiPrintable()
        {
            for (var i = 0; i < _builder.Length; i++)
            {
                if (_builder[i] is >= '\u0021' and <= '\u007E') continue;
                _builder.Remove(i, 1);
                i--;
            }

            return this;
        }
        
        public string Build()
        {
            return _builder.ToString();
        }
    }
}
