using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Serilog.Sinks.Logtail
{
    public class StringCleaner(string source)
    {
        private StringBuilder _builder = new(source);

        public StringCleaner WithTrimed(char toTrim)
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
            var newBuilder = new StringBuilder(_builder.Length);
            for (var i = 0; i < _builder.Length; i++)
            {
                var c = _builder[i];
                if (!illegalChars.Contains(c))
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
            var newBuilder = new StringBuilder(_builder.Length);
            for (var i = 0; i < _builder.Length; i++)
            {
                var c = _builder[i];
                if (IsNonPrintableAscii(c)) continue;
                newBuilder.Append(c);
            }
            _builder = newBuilder;
            return this;
        }
        
        public string Build()
        {
            return _builder.ToString();
        }
        
        /// <summary>
        /// Due to this only existing in .NET. We have vendored this.
        /// </summary>
        /// <param name="c">Char</param>
        /// <returns>True if is ascii</returns>
        private static bool IsNonPrintableAscii(char c) => c is < '\u0021' or > '\u007E';
    }
}
