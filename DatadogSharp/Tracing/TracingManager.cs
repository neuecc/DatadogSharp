using System;
using System.Collections.Generic;
using System.Text;

namespace DatadogSharp.Tracing
{
    public class TracingManager
    {
        public static TracingManager Default = new TracingManager();
        internal AsyncQueueWorker worker;

        //public AsyncQueueWorker(DatadogClient client, int bufferingCount = 10, int bufferingTimeMilliseconds = 5000, Action<Exception> logException = null)
        //{
        //    var client = new DatadogClient();

        //    new AsyncQueueWorker(client, 
        //}

        public void GetTracingScope()
        {


        }
    }

    public class TracingScope : IDisposable
    {
        public TracingScope()
        {


        }





        public void Dispose()
        {
            // TracingManager.Default.worker.Enqueue(
        }
    }
}
