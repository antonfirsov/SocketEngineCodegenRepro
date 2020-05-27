using System;
using System.Diagnostics;
using System.Threading;

namespace SocketEngineCodegenRepro
{
    class Program
    {
        static void Main(string[] args)
        {
            FakeSocketEngine e = new FakeSocketEngine();
            e.Start();
            e.AddSocket();
            Thread.Sleep(5);
            e.CleanupSockets();
            Thread.Sleep(5);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Console.WriteLine("Socket count (0 expected) == " + FakeSocket.TotalCount);
            Debugger.Break();
        }
    }
}
