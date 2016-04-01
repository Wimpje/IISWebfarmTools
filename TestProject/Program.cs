using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomApplicationRequestRouting;
using System.Timers;

namespace TestProject
{
   class Program
   {
      
      static void Main(string[] args)
      {
         try {
            var counterTimer = new Timer(5000);
            counterTimer.Elapsed += CounterTimer_Elapsed;
            foreach (var w in WebFarm.GetWebfarms())
            {
               PerformanceCounters.Initialize(w);
            }

            counterTimer.Start();
            Console.ReadKey();
            counterTimer.Stop();
            PerformanceCounters.Stop();
         }
         finally
         {
            PerformanceCounters.Stop();
         }
      }

      private static void CounterTimer_Elapsed(object sender, ElapsedEventArgs e)
      {
         var ws = WebFarm.GetWebfarms();
         foreach (var w in ws)
         {
            PerformanceCounters.Update(w);
            Console.WriteLine(w.Name);
            Console.WriteLine("- State: " + w.State);
            Console.WriteLine("- Minimum Servers: " + w.MinimumServers);
            Console.WriteLine("- Max Stopped Servers: " + w.MaxStoppedServers);
            Console.WriteLine("- Servers:");
            foreach (var s in w.Servers)
            {
               Console.WriteLine("-- " + s.Name);
               Console.WriteLine("--- State: " + s.ServerState);
               Console.WriteLine("--- Healthy: " + s.Healthy);
               Console.WriteLine("--- Enabled: " + s.Enabled);
               Console.WriteLine("--- Counters:");
               foreach (var c in s.Counters)
               {
                  Console.WriteLine(String.Format("---- {0}: {1} => {2}", c.Name, c.Value, c.Type));
                  
               }
            }
         }
      }
   }
}
