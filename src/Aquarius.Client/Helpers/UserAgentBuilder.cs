﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
#if NETFRAMEWORK
using System.Web.Compilation;
#endif

namespace Aquarius.Helpers
{
    public class UserAgentBuilder
    {
        public static string GetSdkComponent()
        {
            return GetAgentComponent(GetSdkAssemblyName());
        }

        public static string GetApplicationComponent()
        {
            var path = GetExecutingAssemblyPath();

            return GetAgentComponent(path);
        }

        private static string GetAgentComponent(AssemblyName assemblyName)
        {
            return $"{assemblyName.Name} {assemblyName.Version}";
        }

        private static string GetAgentComponent(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            return $"{Path.GetFileNameWithoutExtension(path)} {FileVersionInfo.GetVersionInfo(path).FileVersion}";
        }

        private static AssemblyName GetSdkAssemblyName()
        {
            return typeof(SdkServiceClient).Assembly.GetName();
        }

        public static string GetExecutingAssemblyPath()
        {
            var path = GetTrueAssemblyPath(GetEntryAssembly());

            if (string.IsNullOrEmpty(path))
            {
                // When all else fails, just use the process info to identify the application
                path = Process.GetCurrentProcess().MainModule.FileName;
            }

            return path;
        }

        private static Assembly GetEntryAssembly()
        {
            var assembly = Assembly.GetEntryAssembly();

            if (assembly != null)
                return assembly;

            // We will get here if the host process is not a .NET assembly, but some unmanaged code (like an IIS app pool)

            try
            {
#if NETFRAMEWORK
                // This will identify an ASP.NET entry point
                assembly = BuildManager.GetGlobalAsaxType().BaseType?.Assembly;
#else
                // This will never be hit for .NET Core
#endif
            }
            catch (InvalidOperationException)
            {
                // The expected exception when the process is not an ASP.NET application
            }

            return assembly;
        }

        private static string GetTrueAssemblyPath(Assembly assembly)
        {
            if (assembly == null)
                return string.Empty;

            // Lifted from http://stackoverflow.com/questions/864484/getting-the-path-of-the-current-assembly
            var uri = new Uri(assembly.CodeBase);
            return Path.GetFullPath(Uri.UnescapeDataString(uri.AbsolutePath));
        }
    }
}
