using System.Collections.Generic;
using System.Linq;

namespace Serilog.Sinks.Logtail;

public static class EnumerableExtensions
{
    /// <summary>
    /// An enumerable that yields the current index and the element during enumeration.
    /// </summary>
    /// <typeparam name="T">Typeof item</typeparam>
    /// <param name="input">Enumerable of T</param>
    /// <returns>Tuple with current idx and the item at that idx</returns>
    public static IEnumerable<(int Index, T Item)> Enumerate<T>(this IEnumerable<T> input)
    {
        int i = 0;
        return input.Select(x => (i++, x));
    }
}