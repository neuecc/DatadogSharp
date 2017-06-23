# DatadogSharp
Yet another C# Datadog client that supports DogStatsD and APM.

# What is this

A [Datadog](https://www.datadoghq.com/) client for C# which transport metrics to datadog agent. Datadog has [official C# client](https://github.com/DataDog/dogstatsd-csharp-client) but it is not performant. DatadogSharp is more performant and has zero-overhead api.

Additionaly supports [Datadog APM](https://www.datadoghq.com/apm/) API for [Datadog trace agent](https://github.com/DataDog/datadog-trace-agent) with my fastest [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp) implements. This is only C# SDK which supports APM.

# Installation

The library provides in NuGet for .NET Standard 1.4(.NET Framework(4.6.1) and .NET Core).

```
Install-Package DatadogSharp
```

# How to use(DogStatsd)

At first, configure transport address on application startup.

```csharp
using DatadogSharp.DogStatsd;

DatadogStats.ConfigureDefault(
    address: "127.0.0.1",
    port: 8125, // Optional, default is 8125
    metricNamePrefix: null, // Optinal, if exists prefix, append "prefix." on every metrics call
    defaultTags: null // Optional, append this tag with called tags
);
```

And instrument 

```csharp
// DatadogStats.Default is configured default invoker.

// Increment and Decrement is special case of `Counter`.
DatadogStats.Default.Increment("metricA");
DatadogStats.Default.Decrement("metricA", 3);

// Metrics is follows to http://docs.datadoghq.com/guides/dogstatsd/
DatadogStats.Default.Gauge();
DatadogStats.Default.Histogram();
DatadogStats.Default.Set();
DatadogStats.Default.Timer();

// sampleRate and tags are optional
DatadogStats.Default.Histogram("merticB", sampleRate:0.5);
DatadogStats.Default.Histogram("merticB", tags:new[] { "mygroup:foo", "foobar" });

// BeginTimer is scoped verison of Timer, you can measure executing time easily
using(DatadogStats.Default.BeginTimer())
{
}

// You can Event and ServiceCheck. message is automaticaly truncated max 4096.
DatadogStats.Default.Event("MyApp.Exception", "Stacktrace and messdages",  alertType:AlertType.Error);
DatadogStats.Default.ServiceCheck();
```

If you want to use another configuration(meric prefix, default tags), you can create DatadogStats instance and use it.

```csharp
var anotherStats = new DatadogStats("127.0.0.1", 9999);
```

# How to use(APM)

APM's entrypoint is `TracingManager`. Here is the simple sample of APM Client.

```csharp
using (var trace = TracingManager.Default.BeginTracing("Request", "/Home/Index", "webservice", "web"))
{
    Thread.Sleep(TimeSpan.FromSeconds(1));

    using (trace.BeginSpan("QuerySql", trace.Resource, "sqlserver", "db"))
    {
        Thread.Sleep(TimeSpan.FromSeconds(2));
    }

    Thread.Sleep(TimeSpan.FromSeconds(2));
}
```

![image](https://user-images.githubusercontent.com/46207/27419958-8e6e5faa-575e-11e7-8cab-1c93878d031b.png)

> Windows binary of trace agent is in [datadog-trace-agent's releases page](https://github.com/DataDog/datadog-trace-agent/releases).

`BeginTracing` returns `TracingScope`, it starts tracing as root, when calls `TracingScope.Dispose`(or use using scope) finish measurement and enqueue to send worker.

`BeginSpan` returns `SpanScope`, it starts child tracing, when calls `SpanScope.Dispose`(or use using scope) finish measurement and add span to root tracing.

# Metadata and nested scope

`TracingScope` and `SpanScope` have `WithMeta` and `WithError` methods for append additional info.

```csharp
using (var trace = TracingManager.Default.BeginTracing("Request", "/Home/Index", "webservice", "web"))
{
    try
    {
        // do anything...
    }
    catch (Exception ex)
    {
        trace.WithError(); // mark error.
        trace.WithMeta(new Dictionary<string, string>
        {
            {"exception", ex.ToString() }
        });
    }
}
```




# Real sample

In web, store `TraceScope` in `HttpContext`.

you can store in [AsyncLocal](https://msdn.microsoft.com/en-us/library/dn906268.aspx).







# Advanced(APM)

`DatadogClient` is wrapper of HttpClient to access APM endpoint.






Author Info
---
Yoshifumi Kawai(a.k.a. neuecc) is a software developer in Japan.  
He is the Director/CTO at Grani, Inc.  
Grani is a mobile game developer company in Japan and well known for using C#.  
He is awarding Microsoft MVP for Visual C# since 2011.  
He is known as the creator of [UniRx](http://github.com/neuecc/UniRx/)(Reactive Extensions for Unity)  

Blog: [https://medium.com/@neuecc](https://medium.com/@neuecc) (English)  
Blog: [http://neue.cc/](http://neue.cc/) (Japanese)  
Twitter: [https://twitter.com/neuecc](https://twitter.com/neuecc) (Japanese)   

License
---
This library is under the MIT License.
