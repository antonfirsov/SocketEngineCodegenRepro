#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace SocketEngineCodegenRepro
{
    class FakeSocket
    {
        public static int TotalCount;

        public FakeSocket()
        {
            Interlocked.Increment(ref TotalCount);
        }

        ~FakeSocket()
        {
            Interlocked.Decrement(ref TotalCount);
        }
    }

    [Flags]
    internal enum SocketEvents : int
    {
        None = 0x00,
        Read = 0x01,
    }

    class FakeSocketEngine
    {
        private readonly ConcurrentQueue<SocketIOEvent> _eventQueue = new ConcurrentQueue<SocketIOEvent>();
        private SocketAsyncContextWrapper[]? _socketWrappers = null;

        private readonly int[] _buffer = new int[1];

        private static bool _noMoreEvents;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WaitForSocketEvents()
        {
            if (_noMoreEvents)
            {
                Thread.Sleep(60000 * 100);
            }
            else
            {
                Thread.Sleep(1);
            }
        }

        public void AddSocket()
        {
            _socketWrappers = new[] {new SocketAsyncContextWrapper(new FakeSocket())};
        }

        public void CleanupSockets()
        {
            _socketWrappers = null;
            _eventQueue.Clear();
            _buffer[0] = default;
            _noMoreEvents = true;
        }

        public void Start()
        {
            Thread thread = new Thread(s => ((FakeSocketEngine)s!).EventLoop());
            thread.IsBackground = true;
            thread.Name = "EngineRepro";
            thread.Start(this);
        }

        private void EventLoop()
        {
            while (true)
            {
                WaitForSocketEvents();
                foreach (var socketEvent in _buffer)
                {
                    if (_socketWrappers != null)
                    {
                        var contextWrapper = _socketWrappers[0];
                        var ev = new SocketIOEvent(contextWrapper.Context, SocketEvents.Read);
                        _eventQueue.Enqueue(ev);

                        ev = default;
                        contextWrapper = default;
                    }
                }
            }
        }
        private readonly struct SocketIOEvent
        {
            public FakeSocket Context { get; }
            public SocketEvents Events { get; }

            public SocketIOEvent(FakeSocket context, SocketEvents events)
            {
                Context = context;
                Events = events;
            }
        }

        private readonly struct SocketAsyncContextWrapper
        {
            public SocketAsyncContextWrapper(FakeSocket context) => Context = context;

            internal FakeSocket Context { get; }
        }
    }
}
