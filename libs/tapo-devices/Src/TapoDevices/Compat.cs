using System;
using System.Security.Cryptography;
using System.Formats.Asn1;

namespace TapoDevices
{
    /// <summary>
    /// Backport of RSA.ExportSubjectPublicKeyInfo for netstandard2.0.
    /// Returns DER-encoded SubjectPublicKeyInfo (X.509 PUBLIC KEY).
    /// </summary>
    public static class Compat
    {
        public static byte[] ExportSubjectPublicKeyInfo(this RSA rsa)
        {
            if (rsa == null) throw new ArgumentNullException(nameof(rsa));
            var p = rsa.ExportParameters(false); // public only

            // Build RSAPublicKey (PKCS#1): SEQUENCE { modulus INTEGER, publicExponent INTEGER }
            var pk = new AsnWriter(AsnEncodingRules.DER);
            pk.PushSequence();
            pk.WriteInteger(EnsurePositive(p.Modulus));
            pk.WriteInteger(EnsurePositive(p.Exponent));
            pk.PopSequence();
            byte[] rsaPublicKeyDer = pk.Encode();

            // AlgorithmIdentifier: SEQUENCE { rsaEncryption OID, NULL }
            var algId = new AsnWriter(AsnEncodingRules.DER);
            algId.PushSequence();
            algId.WriteObjectIdentifier("1.2.840.113549.1.1.1"); // rsaEncryption
            algId.WriteNull();
            algId.PopSequence();
            byte[] algIdDer = algId.Encode();

            // SubjectPublicKeyInfo: SEQUENCE { algId, BIT STRING (RSAPublicKey) }
            var spki = new AsnWriter(AsnEncodingRules.DER);
            spki.PushSequence();
            spki.WriteEncodedValue(algIdDer);
            spki.WriteBitString(rsaPublicKeyDer);
            spki.PopSequence();

            return spki.Encode();
        }

        private static byte[] EnsurePositive(byte[] value)
        {
            // ASN.1 INTEGER is signed; if MSB set, prepend 0x00 to make it positive.
            if (value.Length > 0 && (value[0] & 0x80) != 0)
            {
                var unsigned = new byte[value.Length + 1];
                Buffer.BlockCopy(value, 0, unsigned, 1, value.Length);
                return unsigned;
            }
            return value;
        }
    }
}
