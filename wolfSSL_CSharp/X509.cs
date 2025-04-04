﻿#define COMPACT_FRAMEWORK

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace wolfSSL.CSharp
{
    public class X509
    {
        private const string wolfssl_dll = "wolfssl.dll";

#if COMPACT_FRAMEWORK
        private static string PtrToStringAnsiCE(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return string.Empty;

            int length = 0;
            while (Marshal.ReadByte(ptr, length) != 0) length++; // Find null terminator

            byte[] buffer = new byte[length];
            Marshal.Copy(ptr, buffer, 0, length);

            return Encoding.UTF8.GetString(buffer, 0, buffer.Length); // Use UTF-8 decoding for X.509 fields
        }

        [DllImport(wolfssl_dll)]
        private extern static int wolfSSL_X509_get_pubkey_buffer(IntPtr x509, IntPtr buf, IntPtr bufSz);
        [DllImport(wolfssl_dll)]
        private extern static IntPtr wolfSSL_X509_get_der(IntPtr x509, IntPtr bufSz);
        [DllImport(wolfssl_dll)]
        private extern static void wolfSSL_X509_free(IntPtr x509);
        [DllImport(wolfssl_dll)]
        private extern static int wc_DerToPem(IntPtr der, int derSz, IntPtr pem, int pemSz, int type);

        [DllImport(wolfssl_dll)]
        private extern static IntPtr wolfSSL_X509_get_name_oneline(IntPtr x509Name, IntPtr buf, int bufSz);
        [DllImport(wolfssl_dll)]
        private extern static IntPtr wolfSSL_X509_get_subject_name(IntPtr x509);
        [DllImport(wolfssl_dll)]
        private extern static IntPtr wolfSSL_X509_get_issuer_name(IntPtr x509);
#else
        [DllImport(wolfssl_dll, CallingConvention = CallingConvention.Cdecl)]
        private extern static int wolfSSL_X509_get_pubkey_buffer(IntPtr x509, IntPtr buf, IntPtr bufSz);
        [DllImport(wolfssl_dll, CallingConvention = CallingConvention.Cdecl)]
        private extern static IntPtr wolfSSL_X509_get_der(IntPtr x509, IntPtr bufSz);
        [DllImport(wolfssl_dll, CallingConvention = CallingConvention.Cdecl)]
        private extern static void wolfSSL_X509_free(IntPtr x509);
        [DllImport(wolfssl_dll, CallingConvention = CallingConvention.Cdecl)]
        private extern static int wc_DerToPem(IntPtr der, int derSz, IntPtr pem, int pemSz, int type);


        [DllImport(wolfssl_dll, CallingConvention = CallingConvention.Cdecl)]
        private extern static IntPtr wolfSSL_X509_get_name_oneline(IntPtr x509Name, IntPtr buf, int bufSz);
        [DllImport(wolfssl_dll, CallingConvention = CallingConvention.Cdecl)]
        private extern static IntPtr wolfSSL_X509_get_subject_name(IntPtr x509);
        [DllImport(wolfssl_dll, CallingConvention = CallingConvention.Cdecl)]
        private extern static IntPtr wolfSSL_X509_get_issuer_name(IntPtr x509);
#endif

        private IntPtr x509;
        private int    type;
        private bool isDynamic;

        /* public properties */
        public string Issuer;
        public string Subject;


        /* enum from wolfssl */
        private readonly int CERT_TYPE = 0;

        /// <summary>
        /// Creates a new X509 class
        /// </summary>
        /// <param name="x509">Pointer to wolfSSL structure</param>
        /// <param name="isDynamic">Should the lower level x509 be free'd? </param>
        public X509(IntPtr x509, bool isDynamic)
        {
            IntPtr ret;

            this.type = wolfssl.SSL_FILETYPE_PEM;
            this.x509 = x509;
            ret = wolfSSL_X509_get_name_oneline(
                wolfSSL_X509_get_issuer_name(this.x509), IntPtr.Zero, 0);
#if COMPACT_FRAMEWORK
            /* Only for Unicode: */
            this.Issuer = Marshal.PtrToStringUni(ret);
            this.Issuer = PtrToStringAnsiCE(ret);
#else
            this.Issuer = Marshal.PtrToStringAnsi(ret);
#endif

            ret = wolfSSL_X509_get_name_oneline(
                wolfSSL_X509_get_subject_name(this.x509), IntPtr.Zero, 0);

#if COMPACT_FRAMEWORK
            /* Only for Unicode: */
            this.Subject = Marshal.PtrToStringUni(ret);
#else
            this.Subject = Marshal.PtrToStringAnsi(ret);
#endif

            this.isDynamic = isDynamic;
        }

        /// <summary>
        /// Free up the C level WOLFSSL_X509 struct if needed
        /// </summary>
        ~X509()
        {
            if (this.isDynamic)
            {
                wolfSSL_X509_free(this.x509);
            }
        }


        /// <summary>
        /// Used for getting the public key buffer
        /// </summary>
        /// <returns>DER public key on success</returns>
        public byte[] GetPublicKey()
        {
            if (this.x509 == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                IntPtr bufSz;
                IntPtr buf;

                int keySz = 0;
                int ret;
                byte[] key = null;

                bufSz = Marshal.AllocHGlobal(4); /* pointer to 4 bytes */
                ret = wolfSSL_X509_get_pubkey_buffer(this.x509, IntPtr.Zero, bufSz);
                if (ret == wolfssl.SUCCESS)
                {
                    keySz = Marshal.ReadInt32(bufSz, 0);
                    buf = Marshal.AllocHGlobal(keySz);
                    ret = wolfSSL_X509_get_pubkey_buffer(this.x509, buf, bufSz);
                    if (ret == wolfssl.SUCCESS)
                    {
                        key = new byte[keySz];
                        Marshal.Copy(buf, key, 0, keySz);
                    }
                    Marshal.FreeHGlobal(buf);
                }
                Marshal.FreeHGlobal(bufSz);
                return key;
            }
            catch (Exception e)
            {
                wolfssl.log(wolfssl.ERROR_LOG, "error getting public key" + e.ToString());
                return null;
            }
        }

        /// <summary>
        /// Gets the X509 buffer
        /// </summary>
        /// <returns>X509 buffer on success</returns>
        public byte[] Export(int type)
        {
            if (this.x509 == IntPtr.Zero)
                return null;
            try
            {
                IntPtr bufSz;
                IntPtr buf;
                byte[] ret = null;

                bufSz = Marshal.AllocHGlobal(4); /* pointer to 4 bytes */
                buf = wolfSSL_X509_get_der(this.x509, bufSz);
                if (buf != IntPtr.Zero)
                {
                    int derSz = Marshal.ReadInt32(bufSz, 0);
                    if (type == wolfssl.SSL_FILETYPE_ASN1)
                    {
                        ret = new byte[derSz];
                        Marshal.Copy(buf, ret, 0, derSz);
                    }
                    else if (type == wolfssl.SSL_FILETYPE_PEM)
                    {
                        int pemSz;

                        pemSz = wc_DerToPem(buf, derSz, IntPtr.Zero, 0, CERT_TYPE);
                        if (pemSz > 0)
                        {
                            IntPtr pem = Marshal.AllocHGlobal(pemSz);
                            pemSz = wc_DerToPem(buf, derSz, pem, pemSz, CERT_TYPE);
                            ret = new byte[pemSz];
                            Marshal.Copy(pem, ret, 0, pemSz);
                            Marshal.FreeHGlobal(pem);
                        }

                    }
                    else
                    {
                        wolfssl.log(wolfssl.ERROR_LOG, "unsupported export type");
                    }
                    Marshal.FreeHGlobal(bufSz);
                    return ret;
                }
                {
                    wolfssl.log(wolfssl.ERROR_LOG, "unable to get buffer");
                }
                Marshal.FreeHGlobal(bufSz);
                return ret;
            }
            catch (Exception e)
            {
                wolfssl.log(wolfssl.ERROR_LOG, "error getting x509 DER" + e.ToString());
                return null;
            }
        }

        /// <summary>
        /// Gets the X509 buffer using this.type set (default PEM)
        /// </summary>
        /// <returns>X509 buffer on success</returns>
        public byte[] Export()
        {
            return Export(this.type);
        }

        /// <summary>
        /// Gets the X509 format
        /// </summary>
        /// <returns>X509 format on success</returns>
        public string GetFormat()
        {
            if (this.type == wolfssl.SSL_FILETYPE_PEM)
            {
                return "PEM";
            }
            if (this.type == wolfssl.SSL_FILETYPE_ASN1)
            {
                return "DER";
            }
            return "Unknown";
        }
    }
}
