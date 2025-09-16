using System;
using System.IO;
using System.Security.Cryptography;

namespace TapoDevices
{

    /// <summary>Backports SHA256.HashData for net48.</summary>
    public static class SHA256Compat
    {
#if NET5_0_OR_GREATER
        public static byte[] HashData(ReadOnlySpan<byte> source) => SHA256.HashData(source);
        public static byte[] HashData(byte[] source) => SHA256.HashData(source);
        public static byte[] HashData(Stream source) => SHA256.HashData(source);

        public static bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
            => SHA256.TryHashData(source, destination, out bytesWritten);

        // Incremental hashing (same shape)
        public static IncrementalHash CreateIncremental() => IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
#else
    // net48 shims
    public static byte[] HashData(byte[] source)
    {
        using (var sha = SHA256.Create())
            return sha.ComputeHash(source);
    }

    public static byte[] HashData(ReadOnlySpan<byte> source)
    {
        // Requires System.Memory for Span on net48 (add package if you use this overload)
        byte[] arr = source.ToArray();
        using (var sha = SHA256.Create())
            return sha.ComputeHash(arr);
    }

    public static byte[] HashData(Stream source)
    {
        using (var sha = SHA256.Create())
            return sha.ComputeHash(source);
    }

    public static bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
    {
        var hash = HashData(source); // 32 bytes for SHA-256
        if (destination.Length >= hash.Length)
        {
            hash.CopyTo(destination);
            bytesWritten = hash.Length;
            return true;
        }
        bytesWritten = 0;
        return false;
    }

    // Incremental equivalent
    public static IncrementalHash CreateIncremental()
        => IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
#endif
    }
}