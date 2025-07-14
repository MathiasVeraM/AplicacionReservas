using DinkToPdf;
using DinkToPdf.Contracts;
using System.Runtime.Loader;
using System.Runtime.InteropServices;

namespace AplicacionReservas.Extensions
{
    public class CustomAssemblyLoadContext : AssemblyLoadContext
    {
        private IntPtr _nativeLibraryPtr;
        public CustomAssemblyLoadContext() : base(isCollectible: true) { }

        public void LoadUnmanagedLibrary(string path)
        {
            _nativeLibraryPtr = LoadUnmanagedDll(path);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            return LoadUnmanagedDllFromPath(unmanagedDllName);
        }
    }
}
