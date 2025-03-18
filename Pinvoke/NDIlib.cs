// NOTE : The following MIT license applies to this file ONLY and not to the SDK as a whole. Please review the SDK documentation 
// for the description of the full license terms, which are also provided in the file "NDI License Agreement.pdf" within the SDK or 
// online at http://new.tk/ndisdk_license/. Your use of any part of this SDK is acknowledgment that you agree to the SDK license 
// terms. The full NDI SDK may be downloaded at http://ndi.tv/
//
//*************************************************************************************************************************************
// 
// Copyright (C)2014-2023, NewTek, inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
// files(the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, 
// merge, publish, distribute, sublicense, and / or sell copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION 
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security;

namespace NewTek;

[SuppressUnmanagedCodeSecurity]
public static partial class NDIlib
{
    // Based on how SDLSharp does things.
    // https://github.com/GabrielFrigo4/SDL-Sharp/blob/3daad4b05c11c1a3987ae24c12c78092be3aa9c3/SDL-Sharp/SDL/SDL.Loader.cs#L11

    private const string LibraryName = "NDILib";

	static NDIlib()
	{
		NativeLibrary.SetDllImportResolver(typeof(NDIlib).Assembly, ResolveDllImport);
	}

    public static string GetRuntimeIdentifier() 
    {
        var arch = "unknown";
        switch(RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86: 
                arch = "x86";
                break;
            case Architecture.X64:
                arch = "x86-64";
                break;
            case Architecture.Arm:
                arch = "arm32";
                break;
            case Architecture.Arm64:
                arch = "arm64";
                break;
            case Architecture.Wasm:
                arch = "wasm";
                break;
            default:
                arch = RuntimeInformation.ProcessArchitecture.ToString();
                break;
        }

        var platform = "unknown";
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            platform = "win";
        }
        else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            platform = "osx";
        }
        else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            platform = "linux";
        }
        else if(RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            platform = "freebsd";
        }

        return $"{platform}-{arch}";
    }

	private static nint ResolveDllImport(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
	{
        if (libraryName != LibraryName)
        {
            return IntPtr.Zero;
        }
        
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
		    var libName = string.Empty;
			if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
			{
				libName = "Processing.NDI.Lib.x64.dll";
			}
			else if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
			{
				libName = "Processing.NDI.Lib.x86.dll";
			}
			else
			{
				throw new NotImplementedException("Non-x86-based arch not supported on Windows.");
			}
    		return NativeLibrary.TryLoad(libName, out var handle) ? handle : IntPtr.Zero;
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
            var dllDir = AppDomain.CurrentDomain.BaseDirectory;
            IntPtr handle = IntPtr.Zero;
    		if (NativeLibrary.TryLoad(Path.Combine(dllDir, "libndi.so"), out handle)) return handle;
            var arch = RuntimeInformation.ProcessArchitecture switch {
                Architecture.X86 => "linux-x86",
                Architecture.X64 => "linux-x86-64",
                Architecture.Arm => "linux-arm",
                Architecture.Arm64 => "linux-arm64",
                _ => throw new NotImplementedException("Unsupported architecture.")
            };
            if (NativeLibrary.TryLoad(Path.Combine(dllDir, $"lib/{arch}/libndi.so"), out handle)) return handle;
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
            IntPtr handle = IntPtr.Zero;
            if (NativeLibrary.TryLoad("/Library/NDI SDK for Apple/lib/macOS/libndi.dylib", out handle)) return handle;
            var dllDir = AppDomain.CurrentDomain.BaseDirectory;
			// libName = "libndi.dylib";
            if (NativeLibrary.TryLoad(Path.Combine(dllDir, $"lib/osx-arm64/libndi.dylib"), out handle)) return handle;
		}
		else
		{
			throw new NotImplementedException($"{RuntimeInformation.OSDescription} not supported.");
		}
        
        return IntPtr.Zero;
    }
} // namespace NewTek

