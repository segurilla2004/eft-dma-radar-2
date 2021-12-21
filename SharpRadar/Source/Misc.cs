using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpRadar
{
    public static class Extensions
    {
        public static void Reset(this System.Timers.Timer t)
        {
            t.Stop();
            t.Start();
        }
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
    }
}
