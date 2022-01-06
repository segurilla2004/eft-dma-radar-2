using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace eft_dma_radar
{
    /// <summary>
    /// Handles Disposals and mem free() on a separate worker thread to keep the other threads moving quickly.
    /// </summary>
    internal static class Recycler
    {
        private static readonly ConcurrentBag<IntPtr> _memToFree = new ConcurrentBag<IntPtr>();
        private static readonly ConcurrentBag<Bitmap> _bitmapToDispose = new ConcurrentBag<Bitmap>();
        private static readonly Thread _worker;

        public static ConcurrentBag<IntPtr> Pointers
        {
            get
            {
                return _memToFree;
            }
        }

        public static ConcurrentBag<Bitmap> Bitmaps
        {
            get
            {
                return _bitmapToDispose;
            }
        }

        static Recycler()
        {
            _worker = new Thread(() => Worker()) { IsBackground = true };
            _worker.Start(); // Start new background thread to do disposals on
        }

        private static void Worker()
        {
            while (true)
            {
                if (_bitmapToDispose.TryTake(out var bitmap))
                {
                    bitmap?.Dispose();
                }
                if (_memToFree.TryTake(out var mem))
                {
                    Marshal.FreeHGlobal(mem);
                }
            }
        }
    }
}
