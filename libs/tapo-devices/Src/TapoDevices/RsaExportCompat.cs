using System;
using System.Security.Cryptography;
using System.Formats.Asn1;

namespace TapoDevices
{
    public static class RsaExportCompat
    {
        public static byte[] ExportSubjectPublicKeyInfoCompat(RSA rsa)
        {
#if NET5_0_OR_GREATER
            return rsa.ExportSubjectPublicKeyInfo();
#else
            if (rsa == null) throw new ArgumentNullException(nameof(rsa));

            var p = rsa.ExportParameters(false); // public only

            // Ensure positive INTEGERs (prepend 0x00 if MSB is set)
            byte[] modulus = EnsurePositiveInteger(p.Modulus);
            byte[] exponent = EnsurePositiveInteger(p.Exponent);

            // RSAPublicKey ::= SEQUENCE { modulus INTEGER, publicExponent INTEGER }
            var pkWriter = new AsnWriter(AsnEncodingRules.DER);
            pkWriter.PushSequence();
            pkWriter.WriteInteger(modulus);
            pkWriter.WriteInteger(exponent);
            pkWriter.PopSequence();
            byte[] rsaPublicKey = pkWriter.Encode();

            // AlgorithmIdentifier ::= SEQUENCE { OID rsaEncryption, NULL }
            var algWriter = new AsnWriter(AsnEncodingRules.DER);
            algWriter.PushSequence();
            algWriter.WriteObjectIdentifier("1.2.840.113549.1.1.1"); // rsaEncryption
            algWriter.WriteNull();
            algWriter.PopSequence();
            byte[] algId = algWriter.Encode();

            // SubjectPublicKeyInfo ::= SEQUENCE { AlgorithmIdentifier, BIT STRING }
            var spkiWriter = new AsnWriter(AsnEncodingRules.DER);
            spkiWriter.PushSequence();
            spkiWriter.WriteEncodedValue(algId);
            spkiWriter.WriteBitString(rsaPublicKey);
            spkiWriter.PopSequence();

            return spkiWriter.Encode();
#endif
        }

        private static byte[] EnsurePositiveInteger(byte[] value)
        {
            if (value.Length > 0 && (value[0] & 0x80) != 0)
            {
                // Prepend a 0x00 byte so ASN.1 treats it as a positive INTEGER
                byte[] unsigned = new byte[value.Length + 1];
                Buffer.BlockCopy(value, 0, unsigned, 1, value.Length);
                return unsigned;
            }
            return value;
        }
    }
}
