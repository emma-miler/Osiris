using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osiris.Extensions;
public static class StringExtensions
{
    public static string Repeat(this string target, int amount)
    {
        return string.Join("", Enumerable.Repeat(target, amount));
    }

    public static string ReplaceLast(this string target, string pattern, string value)
    {
        int place = target.LastIndexOf(pattern);
        if (place == -1)
            return target;
        else
            return target.Remove(place, pattern.Length).Insert(place, value);
    }
}