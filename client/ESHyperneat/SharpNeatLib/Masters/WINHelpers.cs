using System;
using System.Collections.Generic;
using System.Text;

namespace SharpNeatLib.Masters
{
    public static class TimeUtility
    {
        private static long _lastTime; // records the 64-bit tick value of the last time
        private static object _timeLock = new object();

        internal static DateTime GetCurrentTime()
        {
            lock (_timeLock)
            { // prevent concurrent access to ensure uniqueness
                DateTime result = DateTime.UtcNow;
                if (result.Ticks <= _lastTime)
                    result = new DateTime(_lastTime + 1);
                _lastTime = result.Ticks;
                return result;
            }
        }
        public static long GetNowTicks()
        {
            return GetCurrentTime().Ticks;
        }
    }
}
