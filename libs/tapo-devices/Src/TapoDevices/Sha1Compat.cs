using System;
using System.IO;
using System.Security.Cryptography;

namespace TapoDevices
{
    /// <summary>
    /// Backport of SHA1.HashData for .NET Framework 4.8.
    /// </summary>
    public static class Sha1Compat
    {
#if NET5_0_OR_GREATER
        public static byte[] HashData(ReadOnlySpan<byte> source) => SHA1.HashData(source);
        public static byte[] HashData(byte[] source) => SHA1.HashData(source);
        public static byte[] HashData(Stream source) => SHA1.HashData(source);

        public static bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
            => SHA1.TryHashData(source, destination, out bytesWritten);
#else
        public static byte[] HashData(byte[] source)
        {
            using (var sha = SHA1.Create())
                return sha.ComputeHash(source);
        }

        public static byte[] HashData(ReadOnlySpan<byte> source)
        {
            // Requires System.Memory for Span support on net48
            byte[] arr = source.ToArray();
            using (var sha = SHA1.Create())
                return sha.ComputeHash(arr);
        }

        public static byte[] HashData(Stream source)
        {
            using (var sha = SHA1.Create())
                return sha.ComputeHash(source);
        }

        public static bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            var hash = HashData(source); // 20 bytes for SHA-1
            if (destination.Length >= hash.Length)
            {
                hash.CopyTo(destination);
                bytesWritten = hash.Length;
                return true;
            }
            bytesWritten = 0;
            return false;
        }
#endif
    }
}
