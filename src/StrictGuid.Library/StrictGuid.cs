using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace StrictGuid.Library
{
    /// <summary>
    /// Entropy source selection strategy for GUID generation
    /// </summary>
    public enum StrictGuidEntropy
    {
        /// <summary> Uses Random.Shared (fast, non-cryptographic, random order) </summary>
        Fast,
        /// <summary> Uses RandomNumberGenerator (slower, cryptographically secure, random order) </summary>
        Secure,
        /// <summary> Uses Timestamp + Random (sequential order, ideal for databases) </summary>
        Sequential
    }

    public class StrictGuidException(string message) : ArgumentException(message);

    public static class StrictGuidExtensions
    {
        /// <summary>
        /// Creates a StrictGuid (UUID v8) based on an enum value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid NewStrictGuid<TEnum>(this TEnum value, StrictGuidEntropy entropy = StrictGuidEntropy.Fast) 
            where TEnum : struct, Enum
        {
            if (Unsafe.SizeOf<TEnum>() != sizeof(byte))
                ThrowNotByteEnum<TEnum>();

            byte rawValue = Unsafe.As<TEnum, byte>(ref value);
            return StrictGuidGenerator.Generate(rawValue, entropy);
        }

        /// <summary>
        /// Extracts the entity type from a StrictGuid without allocations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TEnum GetEntityType<TEnum>(this in Guid id) where TEnum : struct, Enum
        {
            if (Unsafe.SizeOf<TEnum>() != sizeof(byte))
                ThrowNotByteEnum<TEnum>();

            if (!StrictGuidGenerator.TryExtractCustomByte(in id, out byte rawValue))
                throw new StrictGuidException("The provided Guid is not a valid StrictGuid (UUID v8).");

            return Unsafe.As<byte, TEnum>(ref rawValue);
        }

        /// <summary>
        /// Validates that the entity type matches the expected type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateEntityType<TEnum>(this in Guid id, TEnum expectedType) where TEnum : struct, Enum
        {
            TEnum actualType = id.GetEntityType<TEnum>();
            if (!EqualityComparer<TEnum>.Default.Equals(actualType, expectedType))
            {
                throw new StrictGuidException($"Type mismatch. Expected {expectedType}, but ID contains {actualType}.");
            }
        }

        /// <summary>
        /// Returns true if the ID corresponds to the specified entity type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEntityType<TEnum>(this in Guid id, TEnum expectedType) where TEnum : struct, Enum
        {
            if (Unsafe.SizeOf<TEnum>() != sizeof(byte))
                ThrowNotByteEnum<TEnum>();

            if (!StrictGuidGenerator.TryExtractCustomByte(in id, out byte rawValue))
                return false;

            TEnum actualType = Unsafe.As<byte, TEnum>(ref rawValue);
            return EqualityComparer<TEnum>.Default.Equals(actualType, expectedType);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotByteEnum<TEnum>() => 
            throw new StrictGuidException($"Enum {typeof(TEnum).Name} must be based on 'byte'.");
    }

    internal static class StrictGuidGenerator
    {
        private const int TypeByteIndex = 10;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static Guid Generate(byte typeByte, StrictGuidEntropy entropy)
        {
            Span<byte> bytes = stackalloc byte[16];
            
            if (entropy == StrictGuidEntropy.Secure)
            {
                RandomNumberGenerator.Fill(bytes);
            }
            else
            {
                Random.Shared.NextBytes(bytes);
            }

            if (entropy == StrictGuidEntropy.Sequential)
            {
                // Fill the first 48 bits (6 bytes) with Unix time in milliseconds
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                bytes[0] = (byte)(timestamp >> 40);
                bytes[1] = (byte)(timestamp >> 32);
                bytes[2] = (byte)(timestamp >> 24);
                bytes[3] = (byte)(timestamp >> 16);
                bytes[4] = (byte)(timestamp >> 8);
                bytes[5] = (byte)timestamp;
            }

            // RFC 9562 UUID v8 (Version bits 48-51, Variant bits 64-65)
            bytes[6] = (byte)((bytes[6] & 0x0F) | 0x80); 
            bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); 
            bytes[TypeByteIndex] = typeByte;

            return new Guid(bytes, bigEndian: true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryExtractCustomByte(in Guid id, out byte typeByte)
        {
            typeByte = 0;
            Span<byte> bytes = stackalloc byte[16];

            if (!id.TryWriteBytes(bytes, bigEndian: true, out _)) return false;

            if ((bytes[6] & 0xF0) != 0x80) return false;

            typeByte = bytes[TypeByteIndex];
            return true;
        }
    }
}
