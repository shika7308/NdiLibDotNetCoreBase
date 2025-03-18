﻿using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace NewTek.NDI
{
    public class Finder : IDisposable
    {
        public ImmutableList<Source> Sources
        { get; private set; }
            = ImmutableList<Source>.Empty;

        public Finder(bool showLocalSources = false, string[]? groups = null, string[]? extraIps = null)
        {
            IntPtr groupsNamePtr = IntPtr.Zero;

            // make a flat list of groups if needed
            if (groups != null)
            {
                groupsNamePtr = UTF.StringToUtf8(string.Join(',', groups));
            }

            // This is also optional.
            // The list of additional IP addresses that exist that we should query for 
            // sources on. For instance, if you want to find the sources on a remote machine
            // that is not on your local sub-net then you can put a comma seperated list of 
            // those IP addresses here and those sources will be available locally even though
            // they are not mDNS discoverable. An example might be "12.0.0.8,13.0.12.8".
            // When none is specified (IntPtr.Zero) the registry is used.
            // Create a UTF-8 buffer from our string
            // Must use Marshal.FreeHGlobal() after use!
            // IntPtr extraIpsPtr = NDI.Common.StringToUtf8("12.0.0.8,13.0.12.8")
            IntPtr extraIpsPtr = IntPtr.Zero;

            // make a flat list of ip addresses as comma separated strings
            if (extraIps != null)
            {
                extraIpsPtr = UTF.StringToUtf8(string.Join(',', extraIps));
            }

            // how we want our find to operate
            NDIlib.find_create_t findDesc = new NDIlib.find_create_t()
            {
                p_groups = groupsNamePtr,
                show_local_sources = showLocalSources,
                p_extra_ips = extraIpsPtr

            };

            // create our find instance
            _findInstancePtr = NDIlib.find_create_v2(ref findDesc);

            // free our UTF-8 buffer if we created one
            if (groupsNamePtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(groupsNamePtr);
            }

            if (extraIpsPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(extraIpsPtr);
            }

            // start up a thread to update on
            _findThread = new Thread(FindThreadProc) { IsBackground = true, Name = "NdiFindThread" };
            _findThread.Start();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Finder()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // tell the thread to exit
                    _exitThread = true;

                    // wait for it to exit
                    if (_findThread != null)
                    {
                        _findThread.Join();

                        _findThread = null;
                    }
                }

                if (_findInstancePtr != IntPtr.Zero)
                {
                    NDIlib.find_destroy(_findInstancePtr);
                    _findInstancePtr = IntPtr.Zero;
                }

                _disposed = true;
            }
        }

        private bool _disposed = false;

        private void FindThreadProc()
        {
            // the size of an NDIlib.source_t, for pointer offsets
            int SourceSizeInBytes = Marshal.SizeOf<NDIlib.source_t>();

            while (!_exitThread)
            {
                // Wait up to 500ms sources to change
                if (NDIlib.find_wait_for_sources(_findInstancePtr, 500))
                {
                    uint NumSources = 0;
                    IntPtr SourcesPtr = NDIlib.find_get_current_sources(_findInstancePtr, ref NumSources);

                    var sourceList = new List<Source>();
                    // convert each unmanaged ptr into a managed NDIlib.source_t
                    for (int i = 0; i < NumSources; i++)
                    {
                        // source ptr + (index * size of a source)
                        IntPtr p = IntPtr.Add(SourcesPtr, i * SourceSizeInBytes);
                        sourceList.Add(new Source(p));
                    }
                    
                    foreach (var source in sourceList)
                    {
                        if (!this.Sources.Any(item => item.Name == source.Name))
                        {
                            this.NdiSourceFound?.Invoke(source);
                        }
                    }

                    foreach (var source in this.Sources)
                    {
                        if (!sourceList.Any(item => item.Name == source.Name))
                        {
                            this.NdiSourceLost?.Invoke(source);
                        }
                    }

                    this.Sources = sourceList.ToImmutableList();
                }
            }
        }

        public void ForceRefresh()
        {
            this.Sources = this.Sources.Clear();
        }

        private IntPtr _findInstancePtr = IntPtr.Zero;

        private object _sourceLock = new object();

        // a thread to find on so that the UI isn't dragged down
        Thread? _findThread = null;

        // a way to exit the thread safely
        bool _exitThread = false;

        public event NdiSourceChanged? NdiSourceFound;
        public event NdiSourceChanged? NdiSourceLost;
    }

    public delegate void NdiSourceChanged(Source source);
}
