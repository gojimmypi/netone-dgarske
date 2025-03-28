using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Net;
using wolfSSL.CSharp;
using System.IO;
public static class WOLFSSLWrapper
    {
        private static int myVerify(int preverify, IntPtr x509_ctx)
        {
            /* Use the provided verification */
            /* Can optionally override failures by returning non-zero value */
            return preverify;
        }
        private static void clean(IntPtr ssl, IntPtr ctx)
        {
            wolfssl.free(ssl);
            wolfssl.CTX_free(ctx);
            wolfssl.Cleanup();
        }
        public static uint my_psk_client_cb(IntPtr ssl, string hint, IntPtr identity, uint id_max, IntPtr key, uint max_key)
        {
            /* C# client */
            byte[] id = { 67, 35, 32, 99, 108, 105, 101, 110, 116 };
            if (id_max < 9)
                return 0;
            Marshal.Copy(id, 0, identity, 9);
            /* Use desired key, note must be a key smaller than max key size parameter
                Replace this with desired key. Is trivial one for testing */
            if (max_key < 4)
                return 0;
            byte[] tmp = { 26, 43, 60, 77 };
            Marshal.Copy(tmp, 0, key, 4);
            return (uint)4;
        }
        public static void ConnectToServer()
        {
            /* See new set_WOLFSSL_DLL_PATH() and set_WOLFSSL_CERTS_PATH() in wolfSSL.cs */
            wolfssl.set_WOLFSSL_DLL_PATH("C:\\workspace\\wolfssl_demo\\bin");
            wolfssl.set_WOLFSSL_CERTS_PATH("C:\\workspace\\wolfssl_demo\\certs");

            StringBuilder myPath = new StringBuilder("C:\\workspace\\wolfssl-gojimmypi\\certs\\");
            StringBuilder caCert = new StringBuilder(myPath.ToString() + "ca-cert.pem");
            StringBuilder dhparam = new StringBuilder(myPath.ToString() + "dh2048.pem");
            // Initialize WolfSSL
            if (wolfssl.Init() == wolfssl.SUCCESS)
            {
                Console.WriteLine("Successfully initialized wolfssl");
            }
            else
            {
                Console.WriteLine("ERROR: Failed to initialize wolfssl");
            }
            // Create a new WolfSSL context
            IntPtr ctx = wolfssl.CTX_new(wolfssl.useTLSv1_2_client());
            if (ctx == IntPtr.Zero)
            {
                Console.WriteLine("Error in creating ctx structure");
            }
            Console.WriteLine("Finished init of ctx .... now load in CA");
            if (!File.Exists(caCert.ToString()))
            {
                Console.WriteLine("Could not find CA cert file ");
                wolfssl.CTX_free(ctx);
            }
            if (!File.Exists(dhparam.ToString()))
            {
                Console.WriteLine("Could not find dh file");
                wolfssl.CTX_free(ctx);
            }
            /* 3rd parameter: optionally try certs in this directory, otherwise null  */
            if (wolfssl.CTX_load_verify_locations(ctx, caCert.ToString(), null) != wolfssl.SUCCESS)
            {
                Console.WriteLine("Error loading CA cert");
                wolfssl.CTX_free(ctx);
            }
            StringBuilder ciphers = new StringBuilder(new String(' ', 4096));
            wolfssl.get_ciphers(ciphers, 4096);
            short minDhKey = 128;
            wolfssl.CTX_SetMinDhKey_Sz(ctx, minDhKey);

            /* TESTING ONLY, SSL_VERIFY_NONE */
            if (wolfssl.CTX_set_verify(ctx, wolfssl.SSL_VERIFY_NONE, myVerify)
                != wolfssl.SUCCESS)
            {
                Console.WriteLine("Error setting verify callback!");
            }

/* disable normal checking since we have a test certificate
            if (wolfssl.CTX_set_verify(ctx, wolfssl.SSL_VERIFY_PEER, myVerify) != wolfssl.SUCCESS)
            {
                Console.WriteLine("Error setting verify callback!");
            }
 */
            Socket tcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                      ProtocolType.Tcp);
            IPEndPoint endPoint = GetEndPoint("stratus-clock-n2a.cloud.paychex.com", 443);
            try
            {
                //tcp.Connect("localhost", 11111);
                tcp.Connect(endPoint);
            }
            catch (Exception e)
            {
                Console.WriteLine("tcp.Connect() error " + e.ToString());
                wolfssl.CTX_free(ctx);
            }
            IntPtr ssl = wolfssl.new_ssl(ctx);
            string socketData = SerializeSocket(tcp);
            wolfssl.set_fd_new(ssl, socketData);
            //wolfssl.set_fd(ssl, tcp);
            //if (wolfssl.set_fd(ssl, tcp) != wolfssl.SUCCESS)
            //{
            //    /* get and print out the error */
            //    Console.WriteLine(wolfssl.get_error(ssl));
            //    //tcp.Close();
            //    //clean(ssl, ctx);
            //    //Environment.Exit(1);
            //}

            wolfssl.SetTmpDH_file(ssl, dhparam, wolfssl.SSL_FILETYPE_PEM);
            if (wolfssl.connect(ssl) != wolfssl.SUCCESS)
            {
                /* get and print out the error */
                Console.WriteLine(wolfssl.get_error(ssl));
                //tcp.Close();
                //clean(ssl, ctx);
                //Environment.Exit(1);
            }
            Console.WriteLine("SSL version is " + wolfssl.get_version(ssl));
        }
        //public static void ConnectToServerWithoutCert()
        //{
        //    IntPtr ctx;
        //    IntPtr ssl;
        //    Socket tcp;
        //    StringBuilder dhparam = new StringBuilder("\\dh2048.pem");
        //    wolfssl.psk_client_delegate psk_cb = new wolfssl.psk_client_delegate(my_psk_client_cb);
        //    if (wolfssl.Init() == wolfssl.SUCCESS)
        //    {
        //        Console.WriteLine("Successfully initialized wolfssl");
        //    }
        //    else
        //    {
        //        Console.WriteLine("ERROR: Failed to initialize wolfssl");
        //    }
        //    ctx = wolfssl.CTX_new(wolfssl.useTLSv1_2_client());
        //    if (ctx == IntPtr.Zero)
        //    {
        //        Console.WriteLine("Error creating ctx structure");
        //    }
        //    StringBuilder ciphers = new StringBuilder(new String(' ', 4096));
        //    wolfssl.get_ciphers(ciphers, 4096);
        //    Console.WriteLine("Ciphers : " + ciphers.ToString());
        //    short minDhKey = 128;
        //    wolfssl.CTX_SetMinDhKey_Sz(ctx, minDhKey);
        //    Console.Write("Setting cipher suite to ");
        //    /* In order to use static PSK build wolfSSL with the preprocessor flag WOLFSSL_STATIC_PSK */
        //    StringBuilder set_cipher = new StringBuilder("DHE-PSK-AES128-CBC-SHA256");
        //    Console.WriteLine(set_cipher);
        //    if (wolfssl.CTX_set_cipher_list(ctx, set_cipher) != wolfssl.SUCCESS)
        //    {
        //        Console.WriteLine("Failed to set cipher suite");
        //    }
        //    /* Test psk use with DHE */
        //    wolfssl.CTX_set_psk_client_callback(ctx, psk_cb);
        //    /* set up TCP socket */
        //    tcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
        //                          ProtocolType.Tcp);
        //    IPEndPoint endPoint = GetEndPoint("stratus-clock-n2a.cloud.paychex.com", 443);
        //    try
        //    {
        //        //tcp.Connect("localhost", 11111);
        //        tcp.Connect(endPoint);
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("tcp.Connect() error " + e.ToString());
        //        wolfssl.CTX_free(ctx);
        //    }
        //    if (!tcp.Connected)
        //    {
        //        Console.WriteLine("tcp.Connect() failed!");
        //        tcp.Close();
        //        wolfssl.CTX_free(ctx);
        //    }
        //    ssl = wolfssl.new_ssl(ctx);
        //    if (ssl == IntPtr.Zero)
        //    {
        //        Console.WriteLine("Error in creating ssl object");
        //        wolfssl.CTX_free(ctx);
        //    }
        //    if (wolfssl.set_fd(ssl, tcp) != wolfssl.SUCCESS)
        //    {
        //        /* get and print out the error */
        //        Console.WriteLine(wolfssl.get_error(ssl));
        //        tcp.Close();
        //        clean(ssl, ctx);
        //    }
        //    if (!File.Exists(dhparam.ToString()))
        //    {
        //        Console.WriteLine("Could not find dh file");
        //        wolfssl.CTX_free(ctx);
        //    }

        //    /* TESTING ONLY, SSL_VERIFY_NONE */
        //    if (wolfssl.CTX_set_verify(ctx, wolfssl.SSL_VERIFY_NONE, myVerify)
        //        != wolfssl.SUCCESS)
        //    {
        //        Console.WriteLine("Error setting verify callback!");
        //    }

        //    wolfssl.SetTmpDH_file(ssl, dhparam, wolfssl.SSL_FILETYPE_PEM);
        //    if (wolfssl.connect(ssl) != wolfssl.SUCCESS)
        //    {
        //        /* get and print out the error */
        //        Console.WriteLine(wolfssl.get_error(ssl));
        //        tcp.Close();
        //        clean(ssl, ctx);
        //    }
        //    /* print out results of TLS/SSL accept */
        //    Console.WriteLine("SSL version is " + wolfssl.get_version(ssl));
        //}

        public static string SerializeSocket(Socket socket)
        {
            IPEndPoint endPoint = (IPEndPoint)socket.RemoteEndPoint;


            // Convert to a string format: "IP:Port"
            return String.Format("{0}:{1}", endPoint.Address, endPoint.Port);
        }

        static IPEndPoint GetEndPoint(string hostname, int port)
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(hostname);
            IPAddress ipAddress = hostEntry.AddressList[0]; // Get the first IP address
            return new IPEndPoint(ipAddress, port);
        }
    }
