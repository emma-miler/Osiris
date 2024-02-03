using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osiris.Extensions;
public static class CharExtensions
{
    public static string Repeat(this char target, int amount)
    {
        return string.Join("", Enumerable.Repeat(target, amount));
    }
}