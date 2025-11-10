using System;

namespace Mapper
{
    public static class Log
    {
        public enum Level
        {
            Error = 1,
            Communication = 2,
        }
        
        internal static Action<string, Level> Print;

        public static void SetMessageCallback(Action<string, Level> callback)
        {
            Print = callback;
        }

        internal static void Error(string message)
        {
            Print?.Invoke(message, Level.Error);
        }

        internal static void Communication(string message)
        {
            Print?.Invoke(message, Level.Communication);
        }
    }
}