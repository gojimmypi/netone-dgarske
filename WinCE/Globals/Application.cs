using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Win32;


namespace NETtime.WinCE.Globals
{
    static class Application
    {
        //APR0067962: Soft Reboot to resolve balck screen issue 
        public enum ApplicationState
        {
            Running,
            Restart
        }

        private delegate void Callback();

        //APR0067962: Soft Reboot to resolve balck screen issue 
        public static ApplicationState State
        {
            get;
            set;
        }

        public static void Init(bool withTrace)
        {

            try
            {
               
                //
                InstallCommonAssemblies();
                WOLFSSLWrapper.ConnectToServer();
                // Initiate the device.
            }
            catch (Exception ex)
            {
            }
        }

        private static void InstallCommonAssemblies()
        {
            Utility.ExtractDependencyFile("NETtime.WinCE.Resources.Cert.dh2048.pem", Utility.LocalPath + "\\Cert", "dh2048.pem", true, false);
            Utility.ExtractDependencyFile("NETtime.WinCE.Resources.Cert.ca-cert.pem", Utility.LocalPath + "\\Cert", "ca-cert.pem", true, false);
            Utility.ExtractDependencyFile("NETtime.WinCE.Resources.Wolfssl.wolfssl.dll", Utility.LocalPath + "\\wolfssl", "wolfssl.dll", true, false);
            Utility.ExtractDependencyFile("NETtime.WinCE.Resources.Wolfssl.wolfssl.exp", Utility.LocalPath + "\\wolfssl", "wolfssl.exp", true, false);
            Utility.ExtractDependencyFile("NETtime.WinCE.Resources.Wolfssl.wolfssl.ilk", Utility.LocalPath + "\\wolfssl", "wolfssl.ilk", true, false);
            Utility.ExtractDependencyFile("NETtime.WinCE.Resources.Wolfssl.wolfssl.lib", Utility.LocalPath + "\\wolfssl", "wolfssl.lib", true, false);
            Utility.ExtractDependencyFile("NETtime.WinCE.Resources.Wolfssl.wolfssl.pdb", Utility.LocalPath + "\\wolfssl", "wolfssl.pdb", true, false);
        }
    }
}
