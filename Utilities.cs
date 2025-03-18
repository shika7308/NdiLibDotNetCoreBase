using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

// Utility functions outside of the NDILib SDK itself,
// but useful for working with NDI from managed languages.

namespace NewTek.NDI
{
    [SuppressUnmanagedCodeSecurity]
    public static partial class UTF
    {
        private static class NativeLibrary
        {
            private const string LibraryName = "libc";

            [DllImport(LibraryName, EntryPoint = "strlen", CallingConvention = CallingConvention.Cdecl)]
            public static extern UIntPtr strlen(IntPtr str);

            public static uint GetStringLength(IntPtr utf8String)
            {
                if (utf8String == IntPtr.Zero)
                    return 0;
                    
                return (uint)strlen(utf8String);
            }
        }


        // This REQUIRES you to use Marshal.FreeHGlobal() on the returned pointer!
        public static IntPtr StringToUtf8(string managedString)
        {
            return StringToUtf8(managedString, out _);
        }

        // this version will also return the length of the utf8 string.
        // The length does not include the null terminator.
        // This REQUIRES you to use Marshal.FreeHGlobal() on the returned pointer!
        public static IntPtr StringToUtf8(string managedString, out int utf8Length)
        {
            utf8Length = Encoding.UTF8.GetByteCount(managedString);

            IntPtr nativeUtf8 = Marshal.AllocHGlobal(utf8Length + 1);

            unsafe {
                Span<byte> buffer = new(nativeUtf8.ToPointer(), utf8Length + 1);
                Encoding.UTF8.GetBytes(managedString, buffer);
            }

            return nativeUtf8;
        }

        // Length is optional, but recommended
        // This is all potentially dangerous
        public static string Utf8ToString(IntPtr nativeUtf8, uint? length = null)
        {
            if (nativeUtf8 == IntPtr.Zero)
                return string.Empty;

            uint len = 0;

            if (length.HasValue)
            {
                len = length.Value;
            }
            else
            {
                len = NativeLibrary.GetStringLength(nativeUtf8);
            }
            
            unsafe {
                return Encoding.UTF8.GetString(new ReadOnlySpan<byte>(nativeUtf8.ToPointer(), (int)len));
            }
        }

    } // class NDILib

} // namespace NewTek.NDI
