using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Utility
{
   
    public static class Library
    {
        [Flags]
        public enum LoadFlags
        {
            RTLD_LAZY = 0x00001,
            RTLD_NOW = 0x00002,
            RTLD_BINDING_MASK = 0x00003,
            RTLD_NOLOAD = 0x00004,
            RTLD_DEEPBIND = 0x00008,
            RTLD_GLOBAL = 0x00100,
            RTLD_LOCAL = 0x00000,
            RTLD_NODELETE = 0x01000
        }

        [DllImport("libdl.so.2", EntryPoint = "dlopen")]
        private static extern IntPtr dlopen(string library, LoadFlags flags);

        /// <summary>
        /// Loads a library with flags to use with dlopen. Uses <see cref="LoadFlags"/> for the flags
        ///
        /// Uses NATIVE_DLL_SEARCH_DIRECTORIES and then ld.so for library paths
        /// </summary>
        /// <param name="library">Full name of the library</param>
        /// <param name="flags">See 'man dlopen' for more information.</param>
        public static void Load(string library, LoadFlags flags, string additionalPaths = null)
        {
            var paths = (string)AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES");
            if (!string.IsNullOrEmpty(additionalPaths)) {
                paths =paths.TrimEnd(':');
                paths = $"{paths}:{additionalPaths}";
            }

            foreach (var path in paths.Split(':'))
            {
                if (dlopen(Path.Combine(path, library), flags) != IntPtr.Zero)
                    break;
            }
        }
    }
}