using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Diagnostics;
using System.ComponentModel;
using Microsoft.Web.Administration;


namespace CustomApplicationRequestRouting
{
   public class PerformanceCounters
   {
      public readonly static String CategoryName = "ApplicationRequestRouting_Custom";
      public static Dictionary<String, List<PerformanceCounter>> CounterList;
      private WebFarm _Webfarm;

      public PerformanceCounters()
      {
         CounterList = new Dictionary<string, List<PerformanceCounter>>();
      }

      private static String getInstanceName(WebFarm f, WebFarm.Server server)
      {
         var instanceName = f.Name + "/" + server.Name;

         foreach (var invalidChar in System.IO.Path.GetInvalidFileNameChars())
            instanceName = instanceName.Replace(invalidChar, '_');

         return instanceName;
      }

      public static void Initialize(WebFarm f)
      {
         Console.WriteLine("Initializing for farm: " + f.Name + "...");

         CreateCategory();


         foreach (var server in f.Servers)
         {
            var instanceName = getInstanceName(f, server);

            if (CounterList.ContainsKey(instanceName))
            {
               Console.WriteLine("Already initialized ...");
               return;
            }

            List<PerformanceCounter> counterList = new List<PerformanceCounter>();
            CounterList.Add(instanceName, counterList);
            // helper to open a counter and 0 its value
            Func<String, PerformanceCounter> newCounter = name =>
            {
               var counter = new PerformanceCounter(CategoryName, name, instanceName, readOnly: false);
               counter.RawValue = 0;
               counterList.Add(counter);
               return counter;
            };


            newCounter("FailedRequests");
            newCounter("CurrentRequests");
            newCounter("RequestPerSecond");
            newCounter("BytesSent");
            newCounter("BytesReceived");
            newCounter("ResponseTime");
            newCounter("TotalWebSocketRequests");
            newCounter("CurrentWebSocketRequests");
            newCounter("FailedWebSocketRequests");
            newCounter("WebSocketBytesSent");
            newCounter("WebSocketBytesReceived");
         }
      }

      public static void Update(WebFarm f)
      {
         foreach (var server in f.Servers)
         {
            var instanceName = getInstanceName(f, server);
            foreach (var counter in CounterList[instanceName])
            {
               //super efficient :D
               counter.RawValue = long.Parse(f.Servers[0].Counters.Where(s => s.Name == counter.CounterName).First().Value.ToString());
            }
         }
      }

      internal static void CreateCategory()
      {
         Console.WriteLine("Creating performance categories...");

         PerformanceCounterCategory perfCategory = null;
         var instanceNames = new List<String>();

         try
         {
            // Create the performance category.
            var counterCollection = new CounterCreationDataCollection()
            {
               new CounterCreationData("TotalRequests", "Total Requests", PerformanceCounterType.NumberOfItems64),
               new CounterCreationData("FailedRequests", "Failed Requests", PerformanceCounterType.NumberOfItems64),
               new CounterCreationData("CurrentRequests", "Current Requests", PerformanceCounterType.NumberOfItems64),
               new CounterCreationData("RequestPerSecond", "Requests per Second", PerformanceCounterType.NumberOfItems64),
               new CounterCreationData("BytesSent", "Bytes sent", PerformanceCounterType.NumberOfItems64),
               new CounterCreationData("BytesReceived", "Bytes Received", PerformanceCounterType.NumberOfItems64),
               new CounterCreationData("ResponseTime", "Reponse time in miliseconds", PerformanceCounterType.NumberOfItems64),
               new CounterCreationData("TotalWebSocketRequests", "Total websocket requests", PerformanceCounterType.NumberOfItems64),
               new CounterCreationData("CurrentWebSocketRequests", "Current websocket requests", PerformanceCounterType.NumberOfItems64),
               new CounterCreationData("FailedWebSocketRequests", "Failed websocket requests", PerformanceCounterType.NumberOfItems64),
               new CounterCreationData("WebSocketBytesSent", "Bytes sent by Websockets", PerformanceCounterType.NumberOfItems64),
               new CounterCreationData("WebSocketBytesReceived", "Bytes received by Websockets", PerformanceCounterType.NumberOfItems64)
            };

            // Validate that if the category exists, all counters are defined. If not, the category will need to be wiped.
            if (PerformanceCounterCategory.Exists(CategoryName))
            {
               var perfCat = new PerformanceCounterCategory(CategoryName);
               var anyMissing = (from CounterCreationData data in counterCollection where !perfCat.CounterExists(data.CounterName) select data).Any();
               if (anyMissing)
               {
                  Console.WriteLine("WARNING: New performance counter(s) defined that do(es) not exist yet locally, performance counter category will be reinitialized.");
                  PerformanceCounterCategory.Delete(CategoryName);
               }
            }

            Console.WriteLine("Adding: ");
            foreach (CounterCreationData counter in counterCollection)
               Console.WriteLine("- " + counter.CounterName + " " + counter.CounterType);

            try
            {
               // Create the category if it doesn't exist yet.
               if (!PerformanceCounterCategory.Exists(CategoryName))
               {
                  perfCategory = PerformanceCounterCategory.Create(CategoryName, "Application Request Routing performance metrics", PerformanceCounterCategoryType.MultiInstance, counterCollection);
               }
               else
               {
                  // The category already exists, track all current instances.
                  perfCategory = new PerformanceCounterCategory(CategoryName);
                  instanceNames.AddRange(perfCategory.GetInstanceNames());
               }
            }
            catch
            {
               perfCategory = null;
            }

            Console.WriteLine("Done");
         }
         catch (Win32Exception e)
         {
            Console.Error.WriteLine("Windows ERROR (" + e.NativeErrorCode + "): " + e.Message);
            Console.Error.WriteLine(e.StackTrace);
         }
         catch (Exception e)
         {
            Console.Error.WriteLine("ERROR (" + e.GetType().Name + "): " + e.Message);
            Console.Error.WriteLine(e.StackTrace);
         }

      }
      public static void Stop()
      {
         if (CounterList == null) return;
         foreach (var counterList in CounterList.Values.Where(l => l != null))
            foreach (var counter in counterList)
               counter.Dispose();

         CounterList.Clear();
      }
   }
}