# DatadogSharp
Yet another C# Datadog client that supports DogStatsD and APM.

# What is this

A [Datadog](https://www.datadoghq.com/) client for C# which transport metrics to datadog agent. Datadog has [official C# client](https://github.com/DataDog/dogstatsd-csharp-client) but it is not performant. DatadogSharp is more performant and has zero-overhead api.

Additionaly supports [Datadog APM](https://www.datadoghq.com/apm/) API for [Datadog trace agent](https://github.com/DataDog/datadog-trace-agent). This is only C# SDK which supports APM.

> not yet implemented, please wait a moment.

# Installation

The library provides in NuGet for .NET Standard 1.4(.NET Framework(4.6.1) and .NET Core).

```
Install-Package DatadogSharp
```

# How to use

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


DatadogStats.Default.Gauge();

DatadogStats.Default.Histogram();

DatadogStats.Default.Set();

DatadogStats.Default.Timer();

DatadogStats.Default.BeginTimer();


DatadogStats.Default.Event();


DatadogStats.Default.ServiceCheck();

```








http://docs.datadoghq.com/guides/dogstatsd/#datagram-format

TODO:
