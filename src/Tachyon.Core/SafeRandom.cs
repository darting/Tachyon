using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Tachyon.Core
{
    /// <summary>
    /// Thread-safe random number generator.
    /// Has same API as System.Random but is thread safe, similar to the implementation by Steven Toub: http://blogs.msdn.com/b/pfxteam/archive/2014/10/20/9434171.aspx
    /// </summary>
    public static class SafeRandom
    {
        private static readonly RandomNumberGenerator globalCryptoProvider = RandomNumberGenerator.Create();

        [ThreadStatic]
        private static Random random;

        private static Random Random
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (random == null)
                {
                    var buffer = new byte[4];
                    globalCryptoProvider.GetBytes(buffer);
                    random = new Random(BitConverter.ToInt32(buffer, 0));
                }

                return random;
            }
        }

        public static int Next()
        {
            return Random.Next();
        }

        public static int Next(int maxValue)
        {
            return Random.Next(maxValue);
        }

        public static int Next(int minValue, int maxValue)
        {
            return Random.Next(minValue, maxValue);
        }

        public static void NextBytes(byte[] buffer)
        {
            Random.NextBytes(buffer);
        }

        public static double NextDouble()
        {
            return Random.NextDouble();
        }
    }
}