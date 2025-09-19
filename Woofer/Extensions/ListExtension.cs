using System;
using System.Collections.Generic;

namespace Woofer.Extensions;

public static class ListExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        Random rng = new(); // Consider reusing a single Random instance for better randomness if called frequently.
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
}